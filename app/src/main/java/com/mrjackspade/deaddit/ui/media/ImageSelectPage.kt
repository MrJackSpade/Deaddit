package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

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
                TopBarButton(Icons.AutoMirrored.Filled.ArrowBack, "Back") { services.navigator.remove(this@ImageSelectPage) }
                Text(
                    "Select Images",
                    Modifier.weight(1f),
                    color = styling.textColor.color,
                    // A MAUI Label's 14.
                    fontSize = 14.sp,
                    textAlign = TextAlign.Center,
                )
                TopBarButton(Icons.Filled.Check, "OK") { confirm() }
            }

            LazyColumn(Modifier.fillMaxSize()) {
                items(items.indices.toList()) { index ->
                    val item = items[index]
                    val isChecked = checked[index] == true
                    Column {
                        Row(
                            Modifier.fillMaxWidth()
                                .clickable(role = Role.Checkbox) { checked[index] = !isChecked }
                                .padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Checkbox(
                                checked = isChecked,
                                onCheckedChange = { checked[index] = it },
                                modifier = Modifier.size(24.dp),
                                colors = CheckboxDefaults.colors(
                                    checkedColor = styling.hyperlinkColor.color,
                                    uncheckedColor = styling.textColor.color,
                                ),
                            )
                            Box(Modifier.padding(start = 12.dp, end = 12.dp)) {
                                AsyncImage(
                                    model = ImageRequest.Builder(context).data(item.launchUrl).size(Size.ORIGINAL).build(),
                                    contentDescription = item.fileName,
                                    modifier = Modifier.sizeIn(maxWidth = 80.dp, maxHeight = 80.dp),
                                )
                            }
                            Text(item.fileName, Modifier.weight(1f), color = styling.textColor.color)
                        }
                        Box(Modifier.fillMaxWidth().height(1.dp).background(styling.tertiaryColor.color))
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
