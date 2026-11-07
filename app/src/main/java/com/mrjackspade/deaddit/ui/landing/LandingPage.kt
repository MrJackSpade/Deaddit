package com.mrjackspade.deaddit.ui.landing

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.PostSort
import com.mrjackspade.deaddit.core.config.SubRedditSubscription
import com.mrjackspade.deaddit.core.feed.Feed
import com.mrjackspade.deaddit.core.model.Multi
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import kotlinx.coroutines.launch

/** The first page: the fixed feeds, the user's subscriptions and their multis. */
class LandingPage(private val services: Services) : Screen() {
    /** One row: a feed to open, and (for removable rows) how to remove it. */
    private class Entry(val label: String, val open: () -> Unit, val remove: (suspend () -> Unit)? = null)

    private val subscriptions = mutableStateListOf<Entry>()
    private val multis = mutableStateListOf<Entry>()
    private var loadingMultis by mutableStateOf(false)
    private var selected by mutableStateOf<Entry?>(null)

    private val fixed = listOf(
        Entry(Feed.Subreddit("All").displayName, { Pages.openFeed(services, Feed.Subreddit("All")) }),
        Entry(Feed.Home.displayName, { Pages.openFeed(services, Feed.Home) }),
        Entry(Feed.Saved.displayName, { Pages.openFeed(services, Feed.Saved) }),
        Entry("History", { Pages.openHistory(services) }),
    )

    private var started = false

    /** Restores the session, then lists the subscriptions and multis. Runs once, when first shown. */
    private fun start() {
        if (started) return
        started = true

        scope.launch {
            try {
                services.restoreSession()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }

            services.settings.landing.value.subscriptions.forEach { subscriptions += entryFor(it) }

            loadMultis()
        }
    }

    private fun entryFor(subscription: SubRedditSubscription): Entry {
        val feed = Feed.fromName(subscription.thingName!!)
        lateinit var entry: Entry
        entry = Entry(feed.displayName, { Pages.openFeed(services, feed) }) {
            subscriptions.remove(entry)
            services.settings.landing.update { config ->
                val index = config.subscriptions.indexOfFirst { Feed.fromName(it.thingName!!) == feed }
                config.copy(subscriptions = config.subscriptions.filterIndexed { i, _ -> i != index })
            }
        }
        return entry
    }

    private fun entryFor(multi: Multi): Entry {
        val feed = Feed.Multi(multi.name)
        lateinit var entry: Entry
        entry = Entry(feed.displayName, { Pages.openFeed(services, feed) }) {
            val confirmed = services.dialogs.confirm(
                "Delete Multi",
                "Delete \"${multi.displayName}\"? This will permanently remove it from your Reddit account and cannot be undone.",
                "Delete",
                "Cancel",
            )
            if (confirmed) {
                services.reddit.deleteMulti(multi)
                multis.remove(entry)
            }
        }
        return entry
    }

    private suspend fun loadMultis() {
        if (!services.reddit.isLoggedIn) return
        loadingMultis = true
        try {
            multis.clear()
            services.reddit.multis().forEach { multis += entryFor(it) }
        } catch (e: Exception) {
            services.dialogs.error(e)
        } finally {
            loadingMultis = false
        }
    }

    private fun onAdd() = scope.launch {
        try {
            if (services.reddit.isLoggedIn) {
                services.dialogs.choose("Add:", Option("Subreddit") { addSubreddit() }, Option("Multi") { addMulti() })
            } else {
                addSubreddit()
            }
        } catch (e: Exception) {
            services.dialogs.error(e)
        }
    }

    private suspend fun addSubreddit() {
        var result = services.dialogs.prompt("", "Enter a SubReddit")
        if (result.isNullOrBlank()) return
        if (!result.contains('/')) result = "r/$result"

        val subscription = SubRedditSubscription(displayString = result, sort = PostSort.Hot, thingName = result)
        services.settings.landing.update { it.copy(subscriptions = it.subscriptions + subscription) }
        subscriptions += entryFor(subscription)
    }

    private suspend fun addMulti() {
        val name = services.dialogs.prompt("", "Enter a name for the new multi")
        if (name.isNullOrBlank()) return
        services.reddit.createMulti(name)?.let { multis += entryFor(it) }
    }

    private fun onProfile() = scope.launch {
        try {
            val user = services.reddit.loggedInUser
            if (user != null) {
                services.dialogs.choose(
                    "Profile:",
                    Option("/u/$user") { Pages.openFeed(services, Feed.User(user)) },
                    Option("Log Out") {
                        services.logout()
                        services.dialogs.alert("Logged Out", "Restart the app to complete logout.")
                    },
                )
            } else {
                val session = services.loginViaBrowser() ?: return@launch
                services.api.setSession(session)
                services.reddit.refreshUser()
                loadMultis()
            }
        } catch (e: Exception) {
            services.dialogs.error(e)
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        Column(Modifier.fillMaxSize().background(styling.tertiaryColor.color)) {
            TopBar {
                TopBarButton(Icons.Filled.Settings, "Settings") { Pages.openSettings(services) }
                TopBarButton(Icons.Filled.Mail, "Messages") { Pages.openMessages(services) }
                Spacer(Modifier.weight(1f))
                TopBarButton(Icons.Filled.Add, "Add") { onAdd() }
                TopBarButton(Icons.Filled.Person, "Profile") { onProfile() }
            }

            LazyColumn(Modifier.fillMaxSize().navigationBarsPadding(), rememberLazyListState()) {
                items(fixed) { Row(it) }
                items(subscriptions) { Row(it) }
                items(multis) { Row(it) }
                if (loadingMultis) item { Spinner() }
            }
        }
    }

    @Composable
    private fun Row(entry: Entry) {
        val styling = LocalStyling.current
        val isSelected = selected === entry

        Column(
            Modifier.fillMaxWidth()
                .background(if (isSelected) styling.highlightColor.color else styling.secondaryColor.color),
        ) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                Text(
                    entry.label,
                    Modifier.weight(1f).clickable(onClick = entry.open).padding(10.dp),
                    color = styling.textColor.color,
                )
                if (entry.remove != null) {
                    TopBarButton(Icons.Filled.Settings, "Options for ${entry.label}") {
                        selected = if (isSelected) null else entry
                    }
                }
            }

            if (isSelected && entry.remove != null) {
                Text(
                    "Remove",
                    Modifier.clickable {
                        scope.launch {
                            try {
                                entry.remove.invoke()
                                if (selected === entry) selected = null
                            } catch (e: Exception) {
                                services.dialogs.error(e)
                            }
                        }
                    }.padding(10.dp),
                    color = styling.textColor.color,
                )
            }
        }
    }
}
