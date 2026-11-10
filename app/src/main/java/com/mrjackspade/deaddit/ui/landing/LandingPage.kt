package com.mrjackspade.deaddit.ui.landing

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material.icons.outlined.Close
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.History
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.Layers
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material.icons.outlined.PersonOutline
import androidx.compose.material.icons.outlined.Public

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.material3.Icon
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.style.TextOverflow
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.ListGroup
import com.mrjackspade.deaddit.ui.SectionLabel
import com.mrjackspade.deaddit.ui.TopBarTitle
import com.mrjackspade.deaddit.ui.cardShape
import com.mrjackspade.deaddit.ui.chipShape
import com.mrjackspade.deaddit.ui.px
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
import androidx.compose.ui.semantics.Role
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
    private class Entry(
        val label: String,
        val open: () -> Unit,
        val isMulti: Boolean = false,
        val icon: ImageVector? = null,
        val remove: (suspend () -> Unit)? = null,
    )

    /**
     * The subscriptions and multis, in the order they were added to the page, as the C# page's
     * one list of rows: the stored subscriptions, then the multis, then whatever is added.
     */
    private val entries = mutableStateListOf<Entry>()
    private var loadingMultis by mutableStateOf(false)
    private var selected by mutableStateOf<Entry?>(null)

    private val fixed = listOf(
        Entry(Feed.Subreddit("All").displayName, { Pages.openFeed(services, Feed.Subreddit("All")) }, icon = Icons.Outlined.Public),
        Entry(Feed.Home.displayName, { Pages.openFeed(services, Feed.Home) }, icon = Icons.Outlined.Home),
        Entry(Feed.Saved.displayName, { Pages.openFeed(services, Feed.Saved) }, icon = Icons.Outlined.BookmarkBorder),
        Entry("History", { Pages.openHistory(services) }, icon = Icons.Outlined.History),
    )

    private var started = false

    /** Restores the session, then lists the subscriptions and multis. Runs once, when first shown. */
    private fun start() {
        if (started) return
        started = true

        scope.launch {
            // As the C# page: a failed session restore is shown, and nothing more is listed.
            try {
                services.restoreSession()
            } catch (e: Throwable) {
                services.dialogs.error(e)
                return@launch
            }

            services.settings.landing.value.subscriptions.forEach { entries += entryFor(it) }

            loadMultis()
        }
    }

    private fun entryFor(subscription: SubRedditSubscription): Entry {
        val feed = Feed.fromName(subscription.thingName!!)
        lateinit var entry: Entry
        entry = Entry(feed.displayName, { Pages.openFeed(services, feed) }) {
            entries.remove(entry)
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
        entry = Entry(feed.displayName, { Pages.openFeed(services, feed) }, isMulti = true) {
            val confirmed = services.dialogs.confirm(
                "Delete Multi",
                "Delete \"${multi.displayName}\"? This will permanently remove it from your Reddit account and cannot be undone.",
                "Delete",
                "Cancel",
            )
            // The row goes only once Reddit says the multi is gone.
            if (confirmed && services.reddit.deleteMulti(multi)) {
                entries.remove(entry)
            }
        }
        return entry
    }

    private suspend fun loadMultis() {
        if (!services.reddit.isLoggedIn) return
        loadingMultis = true
        try {
            entries.removeAll { it.isMulti }
            services.reddit.multis().forEach { entries += entryFor(it) }
        } catch (e: Throwable) {
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
        } catch (e: Throwable) {
            services.dialogs.error(e)
        }
    }

    private suspend fun addSubreddit() {
        var result = services.dialogs.prompt("", "Enter a SubReddit")
        if (result.isNullOrBlank()) return
        if (!result.contains('/')) result = "r/$result"

        val subscription = SubRedditSubscription(displayString = result, sort = PostSort.Hot, thingName = result)
        services.settings.landing.update { it.copy(subscriptions = it.subscriptions + subscription) }
        entries += entryFor(subscription)
    }

    private suspend fun addMulti() {
        val name = services.dialogs.prompt("", "Enter a name for the new multi")
        if (name.isNullOrBlank()) return
        services.reddit.createMulti(name)?.let { entries += entryFor(it) }
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
        } catch (e: Throwable) {
            services.dialogs.error(e)
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                Spacer(Modifier.width(8.dp))
                TopBarTitle("Subreddits")
                TopBarButton(Icons.Outlined.Add, "Add") { onAdd() }
                TopBarButton(Icons.Outlined.PersonOutline, "Profile") { onProfile() }
            }

            val subscriptions = entries.filter { !it.isMulti }
            val multis = entries.filter { it.isMulti }

            LazyColumn(
                Modifier.fillMaxSize().navigationBarsPadding(),
                rememberLazyListState(),
                contentPadding = PaddingValues(start = 8.dp, end = 8.dp, top = 4.dp, bottom = 12.dp),
            ) {
                item(key = "fixed") { Tiles() }

                if (subscriptions.isNotEmpty()) {
                    item(key = "subscriptions-label") { SectionLabel("Subscribed", Modifier.padding(top = 16.dp)) }
                    item(key = "subscriptions") { Group(subscriptions) }
                }

                if (multis.isNotEmpty()) {
                    item(key = "multis-label") { SectionLabel("Multis", Modifier.padding(top = 16.dp)) }
                    item(key = "multis") { Group(multis) }
                }

                if (loadingMultis) item(key = "spinner") { Spinner() }
            }
        }
    }

    /** The fixed feeds, two to a row, each an icon and its name. */
    @Composable
    private fun Tiles() {
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            for (pair in fixed.chunked(2)) {
                Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    for (entry in pair) Tile(entry, Modifier.weight(1f))
                }
            }
        }
    }

    @Composable
    private fun Tile(entry: Entry, modifier: Modifier) {
        val styling = LocalStyling.current
        val shape = styling.cardShape
        Row(
            modifier.heightIn(min = 52.dp).clip(shape).background(styling.cardColor.color).border(1.dp, styling.borderColor.color, shape)
                .clickable(onClick = entry.open).padding(horizontal = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            entry.icon?.let { Icon(it, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(22.dp)) }
            Text(entry.label, color = styling.textColor.color, fontSize = styling.titleFontSize.px, maxLines = 1)
        }
    }

    /** Rows in one card; a row's options button selects it, showing Remove. */
    @Composable
    private fun Group(group: List<Entry>) {
        ListGroup {
            group.forEachIndexed { index, entry -> EntryRow(entry, last = index == group.lastIndex) }
        }
    }

    @Composable
    private fun EntryRow(entry: Entry, last: Boolean) {
        val styling = LocalStyling.current
        val isSelected = selected === entry

        Column(Modifier.fillMaxWidth().background(if (isSelected) styling.highlightColor.color else Color.Transparent)) {
            Row(Modifier.fillMaxWidth().heightIn(min = 48.dp).padding(end = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                Row(
                    Modifier.weight(1f).clickable(onClick = entry.open).padding(start = 12.dp, top = 10.dp, bottom = 10.dp, end = 8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    Mark(entry)
                    Text(entry.label, color = styling.textColor.color, fontSize = styling.titleFontSize.px, maxLines = 1, overflow = TextOverflow.Ellipsis)
                }

                if (entry.remove != null) {
                    if (isSelected) {
                        IconAction(Icons.Outlined.DeleteOutline, "Remove ${entry.label}") { remove(entry) }
                        IconAction(Icons.Outlined.Close, "Close options") { selected = null }
                    } else {
                        IconAction(Icons.Outlined.MoreHoriz, "Options for ${entry.label}") { selected = entry }
                    }
                }
            }
            if (!last) Divider()
        }
    }

    /** A multi's layers mark, or a subreddit's first letter on a quiet chip. */
    @Composable
    private fun Mark(entry: Entry) {
        val styling = LocalStyling.current
        if (entry.isMulti) {
            Icon(Icons.Outlined.Layers, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(22.dp))
            return
        }
        // Labels read "/r/name" or "/u/name": the name's first letter.
        val letter = entry.label.substringAfterLast('/').firstOrNull()?.uppercaseChar()?.toString() ?: ""
        Box(
            Modifier.size(26.dp).clip(styling.chipShape).background(styling.tertiaryColor.color),
            contentAlignment = Alignment.Center,
        ) {
            Text(letter, color = styling.subTextColor.color, fontSize = (styling.subTextFontSize * 0.9).px)
        }
    }

    private fun remove(entry: Entry) {
        val remove = entry.remove ?: return
        scope.launch {
            try {
                remove()
                if (selected === entry) selected = null
            } catch (e: Throwable) {
                services.dialogs.error(e)
            }
        }
    }
}
