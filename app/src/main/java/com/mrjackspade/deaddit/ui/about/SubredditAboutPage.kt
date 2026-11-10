package com.mrjackspade.deaddit.ui.about

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Spacer
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.Groups
import androidx.compose.material3.Icon
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import com.mrjackspade.deaddit.core.util.CompactCount
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.PillButton
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.cardShape
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

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).navigationBarsPadding()) {
            TopBar {
                TopBarButton(Icons.AutoMirrored.Outlined.ArrowBack, "Back", color = styling.textColor.color) { services.navigator.remove(this@SubredditAboutPage) }
                Spacer(Modifier.weight(1f))
            }

            val about = subreddit
            val thumbnail = about?.iconImg ?: about?.communityIcon
            val shape = styling.cardShape

            Column(Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState()).padding(horizontal = 8.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Row(Modifier.padding(horizontal = 8.dp), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    Box(Modifier.size(52.dp).clip(shape).background(styling.tertiaryColor.color).border(1.dp, styling.borderColor.color, shape)) {
                        AsyncImage(thumbnail, contentDescription = about?.displayName, contentScale = ContentScale.Crop, modifier = Modifier.size(52.dp))
                    }
                    Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text(about?.displayNamePrefixed?.takeIf { it.isNotBlank() } ?: "r/$name", color = styling.textColor.color, fontSize = (styling.titleFontSize * 1.15).px, fontWeight = FontWeight.SemiBold)
                        if (about != null) {
                            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(5.dp)) {
                                Icon(Icons.Outlined.Groups, contentDescription = "Members", tint = styling.subTextColor.color, modifier = Modifier.size(16.dp))
                                Text(
                                    CompactCount.format(about.subscribers) + (about.activeUserCount?.let { " · ${CompactCount.format(it)} online" } ?: ""),
                                    color = styling.subTextColor.color,
                                    fontSize = styling.subTextFontSize.px,
                                )
                            }
                        }
                    }
                }

                Row(Modifier.padding(horizontal = 8.dp)) {
                    PillButton(
                        if (subscribed) "Subscribed" else "Subscribe",
                        icon = if (subscribed) Icons.Outlined.Check else Icons.Outlined.Add,
                        accent = subscribed,
                        enabled = about != null,
                    ) { onSubscribe() }
                }

                if (loading) {
                    Spinner()
                } else if (description.isNotEmpty()) {
                    Card {
                        com.mrjackspade.deaddit.ui.html.HtmlBody(
                            description,
                            styling.commentFontSize.px,
                            onLink = { url -> services.appScope.launch { Links.open(services, url) } },
                            modifier = Modifier.padding(horizontal = 12.dp, vertical = 10.dp),
                        )
                    }
                }
            }
        }
    }
}
