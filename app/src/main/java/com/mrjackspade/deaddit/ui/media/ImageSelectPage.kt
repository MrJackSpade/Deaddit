package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.outlined.Check
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material3.Icon
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import com.mrjackspade.deaddit.ui.AccentAction
import com.mrjackspade.deaddit.ui.TopBarTitle
import com.mrjackspade.deaddit.ui.chipShape
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.px

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.sizeIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil3.compose.AsyncImage
import coil3.request.ImageRequest
import coil3.size.Size
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import kotlinx.coroutines.CompletableDeferred

/** Picks which of several images to share or save; all start checked (the C# ImageSelectPage). */
class ImageSelectPage private constructor(private val services: Services, private val items: List<FileDownload>) : Screen() {
    private val result = CompletableDeferred<List<FileDownload>?>()

    /** Checked state by position (items may repeat). */
    private val checked = mutableStateMapOf<Int, Boolean>().apply { items.indices.forEach { put(it, true) } }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                TopBarButton(Icons.AutoMirrored.Outlined.ArrowBack, "Back", color = styling.textColor.color) { services.navigator.remove(this@ImageSelectPage) }
                TopBarTitle("Select images")
                Text("${checked.values.count { it }} / ${items.size}", Modifier.padding(end = 4.dp), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
                AccentAction(Icons.Outlined.Check, "OK") { confirm() }
            }

            // Three across, each a tap to pick or unpick: picked ones edged in the accent, with a check.
            LazyVerticalGrid(
                GridCells.Fixed(3),
                Modifier.fillMaxSize().navigationBarsPadding(),
                contentPadding = PaddingValues(8.dp),
                horizontalArrangement = Arrangement.spacedBy(6.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                items(items.indices.toList()) { index ->
                    val item = items[index]
                    val isChecked = checked[index] == true
                    val shape = styling.innerShape
                    Box(
                        Modifier.aspectRatio(1f).clip(shape).background(styling.cardColor.color)
                            .border(if (isChecked) 2.dp else 1.dp, if (isChecked) styling.accentColor.color else styling.borderColor.color, shape)
                            .clickable(role = Role.Checkbox) { checked[index] = !isChecked }
                            .semantics {
                                contentDescription = item.fileName
                                stateDescription = if (isChecked) "Picked" else "Not picked"
                            },
                    ) {
                        AsyncImage(
                            model = ImageRequest.Builder(context).data(item.launchUrl).size(Size.ORIGINAL).build(),
                            contentDescription = null,
                            contentScale = ContentScale.Crop,
                            modifier = Modifier.fillMaxSize(),
                        )
                        Box(
                            Modifier.align(Alignment.TopEnd).padding(6.dp).size(24.dp).clip(styling.chipShape)
                                .background(if (isChecked) styling.accentColor.color else styling.secondaryColor.color.copy(alpha = 0.7f))
                                .border(if (isChecked) 0.dp else 1.5.dp, styling.subTextColor.color, styling.chipShape),
                            contentAlignment = Alignment.Center,
                        ) {
                            if (isChecked) Icon(Icons.Outlined.Check, contentDescription = null, tint = styling.secondaryColor.color, modifier = Modifier.size(16.dp))
                        }
                    }
                }
            }
        }
    }

    private fun confirm() {
        result.complete(items.filterIndexed { index, _ -> checked[index] == true })
        services.navigator.remove(this)
    }

    override fun onClosed() {
        result.complete(null)
    }

    companion object {
        /** The images the user picked, in their original order; null when they backed out. */
        suspend fun open(services: Services, items: List<FileDownload>): List<FileDownload>? {
            val page = ImageSelectPage(services, items)
            services.navigator.push(page)
            return page.result.await()
        }
    }
}
