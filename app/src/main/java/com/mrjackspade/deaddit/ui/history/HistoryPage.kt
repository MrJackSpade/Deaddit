package com.mrjackspade.deaddit.ui.history

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.post.PostController
import com.mrjackspade.deaddit.ui.post.PostModel
import com.mrjackspade.deaddit.ui.post.PostRow
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex

/**
 * The posts the user opened, most recent first, 25 at a time as the end is reached (the C#
 * HistoryPage). Opening one from here doesn't move it up the history.
 */
class HistoryPage(private val services: Services) : Screen() {
    private val posts = mutableStateListOf<PostModel>()
    private var historyIds: List<String> = emptyList()
    private var currentIndex = 0
    private var loading by mutableStateOf(false)
    private var selected by mutableStateOf<String?>(null)

    /** One load at a time; a reload or scroll while one runs is ignored, as the C# semaphore. */
    private val loadLock = Mutex()
    private var started = false

    private val controller = PostController(
        services,
        scope,
        inList = true,
        fromHistory = true,
        onSelect = { model, toggle ->
            val name = model.post.name
            selected = if (toggle && selected == name) null else name
        },
        onHidden = { model -> posts.remove(model) },
    )

    private fun start() {
        if (started) return
        started = true
        historyIds = services.history.get()
        currentIndex = 0
        // The first page loads as the empty list's end is "reached", below.
    }

    private suspend fun tryLoad() {
        if (!loadLock.tryLock()) return
        try {
            load()
        } catch (e: Exception) {
            services.dialogs.error(e)
        } finally {
            loadLock.unlock()
        }
    }

    private suspend fun reload() {
        if (!loadLock.tryLock()) return
        try {
            posts.clear()
            historyIds = services.history.get()
            currentIndex = 0
            selected = null
            load()
        } catch (e: Exception) {
            services.dialogs.error(e)
        } finally {
            loadLock.unlock()
        }
    }

    private suspend fun load() {
        val remaining = minOf(PAGE_SIZE, historyIds.size - currentIndex)
        if (remaining <= 0) return

        loading = true
        try {
            val ids = historyIds.drop(currentIndex).take(remaining)
            currentIndex += ids.size

            // One request for the page; shown in history order. Posts Reddit no longer returns
            // (deleted, or gone) are left out, as in C#.
            val byName = services.reddit.getPosts(ids).associateBy { it.name }
            for (id in ids) {
                val post = byName[if (id.startsWith("t3_")) id else "t3_$id"] ?: byName[id] ?: continue
                posts += PostModel(post, PostState.None)
            }
        } finally {
            loading = false
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val listState = rememberLazyListState()

        LaunchedEffect(Unit) { start() }

        // The next page loads when the bottom is reached.
        val atBottom by remember {
            derivedStateOf {
                val last = listState.layoutInfo.visibleItemsInfo.lastOrNull()?.index ?: 0
                last >= listState.layoutInfo.totalItemsCount - 1
            }
        }
        LaunchedEffect(atBottom, posts.size, loading) {
            // In the page's scope, so leaving for a post doesn't cancel a load halfway.
            if (atBottom && started && !loading) scope.launch { tryLoad() }
        }

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                TopBarButton(Icons.Filled.Refresh, "Reload") { scope.launch { reload() } }
                Text("History", Modifier.weight(1f), color = styling.textColor.color, textAlign = TextAlign.Center)
                TopBarButton(Icons.Filled.Settings, "Settings") { Pages.openSettings(services) }
            }

            LazyColumn(Modifier.fillMaxSize().navigationBarsPadding(), listState) {
                items(posts, key = { it.post.name }) { model ->
                    PostRow(
                        model,
                        selected = selected == model.post.name,
                        inList = true,
                        hideScore = services.settings.hacks.value.hideSelfKarma && model.post.author.equals(services.reddit.loggedInUser, ignoreCase = true),
                        actions = controller.actions(model),
                    )
                }

                if (loading) item(key = "spinner") { Spinner() }
            }
        }
    }

    private companion object {
        const val PAGE_SIZE = 25
    }
}
