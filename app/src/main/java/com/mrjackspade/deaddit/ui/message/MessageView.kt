package com.mrjackspade.deaddit.ui.message

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
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
import com.mrjackspade.deaddit.ui.FlexRow
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.AuthorName
import com.mrjackspade.deaddit.ui.html.CssMediumFontSize
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.html.InlineFlow
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/**
 * A private message: author, subject and body; when selected, its actions. Selecting it marks it
 * read (the C# RedditMessageWebComponent). Not [selectable] where C# gave it no selection group.
 * Its box is padded 5px left, top and bottom; what its buttons start runs in the app's scope.
 */
@Composable
fun MessageView(services: Services, message: Message, isSelected: Boolean, selectable: Boolean = true, onSelect: () -> Unit) {
    val styling = LocalStyling.current
    val blocks = remember(message.name) { message.bodyHtml?.let(RedditHtml::parse) ?: emptyList() }

    Column(
        Modifier.fillMaxWidth().padding(start = 5.dp, top = 5.dp, bottom = 5.dp)
            .background(if (isSelected) styling.highlightColor.color else Color.Transparent)
            .clickable(enabled = selectable) {
                onSelect()
                services.launchAction { services.reddit.markRead(message, true) }
            },
    ) {
        Header(message)

        HtmlBody(blocks, styling.commentFontSize.px, onLink = { url -> services.appScope.launch { Links.open(services, url) } })

        if (isSelected) {
            FlexRow(Modifier.fillMaxWidth().background(styling.highlightColor.color)) {
                BarButton("...", "More") {
                    services.launchAction {
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
                }
                // The C# app showed a share button here that did nothing.
                BarButton("⢔", "Share", onClick = null)
                BarButton("↩", "Reply") {
                    services.launchAction {
                        val author = services.reddit.userData(message.author!!)
                        Pages.openMessage(services, author!!, message)
                    }
                }
            }
        }
    }
}

/**
 * The C# MessageHeaderComponent: the author (marked as moderator or admin) and "- subject" in bold,
 * both 16px (no size given), as inline text that wraps.
 */
@Composable
private fun Header(message: Message) {
    val text = LocalStyling.current.textColor.color

    InlineFlow {
        AuthorName(message.author ?: "", message.distinguished, isOp = false)
        // A word at a time, so the subject wraps along the line as inline text does.
        for (word in words("- ${com.mrjackspade.deaddit.core.html.HtmlText.display(message.subject)}")) {
            Text(word, color = text, fontSize = CssMediumFontSize, fontWeight = FontWeight.Bold)
        }
    }
}

/** [text] split after each space, the spaces kept. */
private fun words(text: String): List<String> = Regex("\\S*\\s*").findAll(text).map { it.value }.filter { it.isNotEmpty() }.toList()

/** An action bar button: as wide as its label, sharing the bar's spare room (CSS flex-grow: 1). */
@Composable
private fun BarButton(text: String, description: String, onClick: (() -> Unit)?) {
    val styling = LocalStyling.current
    Text(
        text,
        Modifier.let { m -> if (onClick != null) m.clickable(role = Role.Button, onClick = onClick) else m }
            .padding(10.dp).semantics { contentDescription = description },
        color = styling.textColor.color,
        fontSize = styling.titleFontSize.px,
        textAlign = TextAlign.Center,
    )
}
