package com.mrjackspade.deaddit.ui.about

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.Subreddit
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/** A subreddit's sidebar, size and subscription (the C# SubRedditAboutPage). */
class SubredditAboutPage(private val services: Services, private val name: String) : Screen() {
    private var subreddit by mutableStateOf<Subreddit?>(null)
    private var subscribed by mutableStateOf(false)
    private var description by mutableStateOf<List<Block>>(emptyList())
    private var loading by mutableStateOf(false)
    private var started = false

    private fun start() {
        if (started) return
        started = true
        scope.launch {
            loading = true
            try {
                val about = services.reddit.about(name) ?: throw IllegalStateException("Reddit returned nothing for r/$name.")
                description = about.descriptionHtml?.let(RedditHtml::parse) ?: emptyList()
                subscribed = about.userIsSubscriber == true
                subreddit = about
            } catch (e: Throwable) {
                services.dialogs.error(e)
            } finally {
                loading = false
            }
        }
    }

    /** With multis, a choice of the home feed and each multi; otherwise subscribes or unsubscribes. */
    private fun onSubscribe() {
        // In the app's scope: a (un)subscription isn't dropped by leaving the page.
        services.appScope.launch {
            try {
                val about = subreddit ?: throw IllegalStateException("r/$name hasn't loaded.")

                if (services.reddit.isLoggedIn) {
                    val multis = services.reddit.multis()

                    if (multis.isNotEmpty()) {
                        val options = mutableListOf(
                            Option(if (subscribed) "✓ Home" else "Home") { toggleSubscription(about) },
                        )

                        for (multi in multis) {
                            val inMulti = multi.subreddits.any { it.name.equals(name, ignoreCase = true) }
                            options += Option(if (inMulti) "✓ ${multi.displayName}" else multi.displayName) {
                                if (inMulti) {
                                    services.reddit.removeSubredditFromMulti(multi, name)
                                } else {
                                    services.reddit.addSubredditToMulti(multi, name)
                                }
                            }
                        }

                        services.dialogs.choose("Manage subscription:", *options.toTypedArray())
                        return@launch
                    }
                }

                toggleSubscription(about)
            } catch (e: Throwable) {
                services.dialogs.error(e)
            }
        }
    }

    private suspend fun toggleSubscription(about: Subreddit) {
        services.reddit.setSubscribed(about, !subscribed)
        subscribed = !subscribed
    }

    /** A MAUI Button at its defaults, on a transparent background: 14 text, 14 by 10 padding, 44 tall at least. */
    @Composable
    private fun MauiButton(text: String, onClick: () -> Unit) {
        Box(
            Modifier.heightIn(min = 44.dp).clickable(role = Role.Button, onClick = onClick).padding(horizontal = 14.dp, vertical = 10.dp),
            contentAlignment = Alignment.Center,
        ) {
            Text(text, color = LocalStyling.current.textColor.color, fontSize = 14.sp)
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).statusBarsPadding().navigationBarsPadding()) {
            val about = subreddit
            val thumbnail = about?.iconImg ?: about?.communityIcon

            Row(Modifier.fillMaxWidth().heightIn(min = styling.thumbnailSize.dp)) {
                AsyncImage(
                    thumbnail,
                    contentDescription = about?.displayName,
                    contentScale = ContentScale.Crop,
                    modifier = Modifier.size(styling.thumbnailSize.dp),
                )
                Column(Modifier.weight(1f).padding(start = 10.dp)) {
                    // A MAUI Label's default size.
                    Text(about?.displayName ?: "", color = styling.textColor.color, fontSize = 14.sp)
                    Text(
                        about?.let { "${it.subscribers} subscribers, ${it.activeUserCount ?: ""} online" } ?: "",
                        color = styling.subTextColor.color,
                        fontSize = styling.subTextFontSize.px,
                    )
                }
            }

            Row(Modifier.fillMaxWidth().background(styling.highlightColor.color)) {
                // The C# Rules and More buttons did nothing.
                MauiButton("Rules") { }
                MauiButton("More") { }
                MauiButton(if (subscribed) "Unsubscribe" else "Subscribe") { onSubscribe() }
            }

            if (loading) {
                Spinner()
            } else {
                Column(Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState())) {
                    com.mrjackspade.deaddit.ui.html.HtmlBody(
                        description,
                        styling.commentFontSize.px,
                        onLink = { url -> services.appScope.launch { Links.open(services, url) } },
                        modifier = Modifier.padding(5.dp),
                    )
                }
            }
        }
    }
}
