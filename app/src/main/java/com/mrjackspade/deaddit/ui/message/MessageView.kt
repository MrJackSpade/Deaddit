package com.mrjackspade.deaddit.ui.message

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Reply
import androidx.compose.material.icons.outlined.MailOutline
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.util.elapsed
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.AuthorName
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/**
 * A private message as a card: the mail mark, author and age (with a small accent dot while it's
 * unread), subject and body; when selected, its actions. Selecting it marks it read (the C#
 * RedditMessageWebComponent). Not [selectable] where C# gave it no selection group. What its
 * buttons start runs in the app's scope.
 */
@Composable
fun MessageView(services: Services, message: Message, isSelected: Boolean, selectable: Boolean = true, onSelect: () -> Unit) {
    val styling = LocalStyling.current
    val blocks = remember(message.name) { message.bodyHtml?.let(RedditHtml::parse) ?: emptyList() }
    var unread by remember(message.name) { mutableStateOf(message.new) }

    Card(Modifier.padding(horizontal = 8.dp, vertical = 3.dp), selected = isSelected) {
        Row(
            Modifier.fillMaxWidth()
                .clickable(enabled = selectable) {
                    onSelect()
                    unread = false
                    services.launchAction { services.reddit.markRead(message, true) }
                }
                .padding(horizontal = 12.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            Icon(Icons.Outlined.MailOutline, contentDescription = "Private message", tint = styling.subTextColor.color, modifier = Modifier.padding(top = 2.dp).size(20.dp))

            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    AuthorName(message.author ?: "", message.distinguished, isOp = false, modifier = Modifier.weight(1f, fill = false))
                    Text("· ${message.createdUtc.elapsed() ?: ""}", color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px, maxLines = 1)
                    Spacer(Modifier.weight(1f))
                    if (unread) {
                        Box(Modifier.size(8.dp).clip(CircleShape).background(styling.accentColor.color).semantics { contentDescription = "Unread" })
                    }
                }
                Text(
                    com.mrjackspade.deaddit.core.html.HtmlText.display(message.subject),
                    color = styling.textColor.color,
                    fontSize = styling.subTextFontSize.px,
                    fontWeight = FontWeight.SemiBold,
                )
                HtmlBody(blocks, styling.commentFontSize.px, onLink = { url -> services.appScope.launch { Links.open(services, url) } })
            }
        }

        if (isSelected) {
            Divider(Modifier.padding(horizontal = 8.dp))
            Row(Modifier.fillMaxWidth().padding(4.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                IconAction(Icons.AutoMirrored.Outlined.Reply, "Reply") {
                    services.launchAction {
                        val author = services.reddit.userData(message.author!!)
                        Pages.openMessage(services, author!!, message)
                    }
                }
                IconAction(Icons.Outlined.MoreHoriz, "More") {
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
            }
        }
    }
}
