package com.mrjackspade.deaddit.ui.user

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Article
import androidx.compose.material.icons.outlined.ChatBubbleOutline
import androidx.compose.material.icons.outlined.MailOutline
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.core.util.CompactCount
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.PillButton
import com.mrjackspade.deaddit.ui.cardShape
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.px

/**
 * The top of a user's page: their avatar and name, post and comment karma, profile description,
 * and a button to message them. (The C# header had karma lines too, but never filled them in.)
 */
@Composable
fun UserHeader(services: Services, user: User, showMessage: Boolean = true) {
    val styling = LocalStyling.current
    val shape = styling.cardShape

    Column(Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            Box(Modifier.size(52.dp).clip(shape).background(styling.tertiaryColor.color).border(1.dp, styling.borderColor.color, shape)) {
                AsyncImage(user.iconImg, contentDescription = "/u/${user.name}", contentScale = ContentScale.Crop, modifier = Modifier.size(52.dp))
            }
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(user.name, color = styling.textColor.color, fontSize = (styling.titleFontSize * 1.15).px, fontWeight = FontWeight.SemiBold)
                Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
                    Stat(Icons.Outlined.Article, "Post karma", user.linkKarma)
                    Stat(Icons.Outlined.ChatBubbleOutline, "Comment karma", user.commentKarma)
                }
            }
        }

        user.subreddit?.publicDescription?.takeIf { it.isNotBlank() }?.let { description ->
            Text(description, color = styling.textColor.color, fontSize = styling.commentFontSize.px, lineHeight = styling.commentFontSize.px * 1.45f)
        }

        // The message page shows the header without it.
        if (showMessage) {
            PillButton("Message", icon = Icons.Outlined.MailOutline) { Pages.openMessage(services, user) }
        }
    }
}

@Composable
private fun Stat(icon: ImageVector, description: String, value: Int) {
    val styling = LocalStyling.current
    Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(5.dp)) {
        Icon(icon, contentDescription = description, tint = styling.subTextColor.color, modifier = Modifier.size(16.dp))
        Text(CompactCount.format(value), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
    }
}
