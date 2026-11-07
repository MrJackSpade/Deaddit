package com.mrjackspade.deaddit.ui.message

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.AuthorName
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch

/**
 * A private message: author, subject and body; when selected, its actions. Selecting it marks it
 * read (the C# RedditMessageWebComponent). Not [selectable] where C# gave it no selection group.
 */
@Composable
fun MessageView(services: Services, scope: CoroutineScope, message: Message, isSelected: Boolean, selectable: Boolean = true, onSelect: () -> Unit) {
    val styling = LocalStyling.current
    val blocks = remember(message.name) { message.bodyHtml?.let(RedditHtml::parse) ?: emptyList() }

    fun run(block: suspend () -> Unit) {
        scope.launch {
            try {
                block()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    Column(
        Modifier.fillMaxWidth().padding(5.dp)
            .background(if (isSelected) styling.highlightColor.color else Color.Transparent)
            .clickable(enabled = selectable) {
                onSelect()
                run { services.reddit.markRead(message, true) }
            },
    ) {
        Row {
            AuthorName(message.author ?: "", null, isOp = false)
            Text("- ${message.subject}", color = styling.textColor.color, fontWeight = FontWeight.Bold)
        }

        HtmlBody(blocks, styling.commentFontSize.px, onLink = { url -> scope.launch { Links.open(services, url) } })

        if (isSelected) {
            Row(Modifier.fillMaxWidth().background(styling.highlightColor.color)) {
                Text(
                    "...",
                    Modifier.weight(1f).clickable(role = Role.Button) {
                        run {
                            if (!message.author.equals(services.reddit.loggedInUser, ignoreCase = true)) {
                                services.dialogs.choose(
                                    "Select:",
                                    Option("Block /u/${message.author}") {
                                        Pages.openBlockRuleEditor(services, Blocking.fromAuthor(message)) { }
                                    },
                                    Option("View /u/${message.author}") { Pages.openUser(services, message.author!!) },
                                )
                            }
                        }
                    }.padding(10.dp).semantics { contentDescription = "More" },
                    color = styling.textColor.color,
                    fontSize = styling.titleFontSize.px,
                    textAlign = TextAlign.Center,
                )
                // The C# app showed a share button here that did nothing.
                Text(
                    "⢔",
                    Modifier.weight(1f).padding(10.dp).semantics { contentDescription = "Share" },
                    color = styling.textColor.color,
                    fontSize = styling.titleFontSize.px,
                    textAlign = TextAlign.Center,
                )
                Text(
                    "↩",
                    Modifier.weight(1f).clickable(role = Role.Button) {
                        run {
                            val author = services.reddit.userData(message.author!!)
                            Pages.openMessage(services, author!!, message)
                        }
                    }.padding(10.dp).semantics { contentDescription = "Reply" },
                    color = styling.textColor.color,
                    fontSize = styling.titleFontSize.px,
                    textAlign = TextAlign.Center,
                )
            }
        }
    }
}
