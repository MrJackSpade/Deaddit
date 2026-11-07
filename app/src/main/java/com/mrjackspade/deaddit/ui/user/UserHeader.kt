package com.mrjackspade.deaddit.ui.user

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.wrapContentSize
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.px

/**
 * The top of a user's page: their avatar, profile description and a button to message them.
 * (The C# header also had karma and age lines, but never filled them in, so they showed nothing.)
 */
@Composable
fun UserHeader(services: Services, user: User, showMessage: Boolean = true) {
    val styling = LocalStyling.current
    val size = styling.thumbnailSize.dp

    Row(Modifier.fillMaxWidth()) {
        AsyncImage(user.iconImg, contentDescription = "/u/${user.name}", contentScale = ContentScale.Crop, modifier = Modifier.size(size))

        Column(Modifier.weight(1f).padding(5.dp)) {
            Text(user.subreddit?.publicDescription ?: "", color = styling.textColor.color, fontSize = styling.commentFontSize.px)
        }

        // The message page shows the header without it.
        if (showMessage) {
            Box(
                Modifier.size(size).clickable(role = Role.Button) { Pages.openMessage(services, user) }
                    .semantics { contentDescription = "Message /u/${user.name}" },
                contentAlignment = Alignment.Center,
            ) {
                // The C# button's font size was the thumbnail size; the glyph overflows its box,
                // centered, as it did there.
                Text(
                    "🗨",
                    Modifier.wrapContentSize(unbounded = true),
                    color = styling.textColor.color,
                    fontSize = styling.thumbnailSize.toDouble().px,
                )
            }
        }
    }
}
