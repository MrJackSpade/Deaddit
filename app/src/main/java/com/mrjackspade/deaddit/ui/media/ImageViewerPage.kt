package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

import android.annotation.SuppressLint
import android.text.TextUtils
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.ui.BusyCounter
import com.mrjackspade.deaddit.ui.BusyOverlay
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch

/**
 * Images, one under another, each fit to the width and loaded at full resolution (the C#
 * EmbeddedImage, same page): pinch or double-tap to zoom up to actual size and beyond, drag to
 * pan. Share and save ask which images when there are several.
 */
class ImageViewerPage(private val services: Services, private val items: List<FileDownload>) : Screen() {
    private val busy = BusyCounter()

    // Saves and shares outlive the page: the MAUI page only cancelled them from a back button it
    // never showed. The running save, which the next share or save cancels (the C# _downloadCts).
    private var runningSave: Job? = null

    private val webView = RetainedView { context -> createWebView(context) }

    @SuppressLint("SetJavaScriptEnabled")
    private fun createWebView(context: android.content.Context): WebView = WebView(context).apply {
        settings.javaScriptEnabled = true
        webViewClient = WebViewClient()

        val styling = services.settings.styling.value
        val images = StringBuilder()

        for (item in items) {
            val escapedUrl = TextUtils.htmlEncode(item.launchUrl)
            images.append("<div class='img-container'>")
            images.append("<img data-src='$escapedUrl' alt='Image' />")
            images.append("<div class='loading-overlay'><div class='spinner'></div></div>")
            images.append("<div class='error-overlay'><div>Image failed to load</div><button type='button'>Retry</button></div>")
            images.append("</div>")
        }

        val template = context.assets.open("media/image_viewer.html").use { it.readBytes().toString(Charsets.UTF_8) }
        val html = template.replace("%IMAGES%", images.toString()).replace("%BACKGROUND_COLOR%", styling.secondaryColor.toHex())

        // As MAUI's HtmlWebViewSource loaded it.
        loadDataWithBaseURL("file:///android_asset/", html, "text/html", "UTF-8", null)
    }

    @Composable
    override fun Content() {
        Box(Modifier.fillMaxSize()) {
            Column(Modifier.fillMaxSize()) {
                TopBar {
                    Spacer(Modifier.weight(1f))
                    TopBarButton(Icons.Filled.Share, "Share") { share() }
                    TopBarButton(Icons.Filled.Save, "Save") { save() }
                }
                webView.Content(Modifier.fillMaxWidth().weight(1f))
            }
            BusyOverlay(busy)
        }
    }

    /**
     * As the C# OnShareClicked: once images are picked, cancels a running save; the share itself
     * is never cancelled (C# gave it no token).
     */
    private fun share() {
        services.appScope.launch {
            try {
                val selected = promptForSelection()
                if (selected.isNullOrEmpty()) return@launch
                runningSave?.cancel()
                busy.run { services.media.shareDownloads("", selected, services.media.gifConverter()) }
            } catch (e: Throwable) {
                services.dialogs.error(e)
            }
        }
    }

    /**
     * As the C# OnSaveClicked: once images are picked, cancels a running save and becomes the
     * one the next share or save cancels. Backing out of the selection leaves a running save be.
     */
    private fun save() {
        services.appScope.launch {
            try {
                val selected = promptForSelection()
                if (selected.isNullOrEmpty()) return@launch
                runningSave?.cancel()
                runningSave = coroutineContext[Job]
                busy.run { services.media.save(selected, services.media.gifConverter()) }
            } catch (e: Throwable) {
                services.dialogs.error(e)
            }
        }
    }

    private suspend fun promptForSelection(): List<FileDownload>? {
        if (items.size <= 1) return items.toList()
        return Pages.selectImages(services, items)
    }

    override fun onClosed() {
        webView.release { it.destroy() }
    }
}
