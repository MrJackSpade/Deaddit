package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.outlined.Download
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material.icons.outlined.ViewCarousel
import androidx.compose.material.icons.outlined.ViewDay

import android.annotation.SuppressLint
import android.text.TextUtils
import android.webkit.JavascriptInterface
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.px
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
 * Images loaded at full resolution (the C# EmbeddedImage, same page): pinch or double-tap to zoom
 * up to actual size and beyond, drag to pan. A gallery opens in page view, one image a screen,
 * swiped across; its toggle switches to scroll view, every image down one continuous column (for
 * comics drawn to scroll), and back. The choice is this gallery's only: the next opens in page
 * view again. A single image shows as it always has, fit to the width. Share and save ask which
 * images when there are several.
 */
class ImageViewerPage(private val services: Services, private val items: List<FileDownload>) : Screen() {
    private val busy = BusyCounter()

    // Saves and shares outlive the page: the MAUI page only cancelled them from a back button it
    // never showed. The running save, which the next share or save cancels (the C# _downloadCts).
    private var runningSave: Job? = null

    private val gallery = items.size > 1
    private var pages by mutableStateOf(gallery)

    /** The image showing in page view, as the page reports it. */
    private var page by mutableIntStateOf(0)

    private val webView = RetainedView { context -> createWebView(context) }

    /** Called from the page's script, off the main thread, as page view is swiped. */
    private inner class PageListener(private val view: WebView) {
        @JavascriptInterface
        fun onPage(index: Int) {
            view.post { page = index }
        }
    }

    @SuppressLint("SetJavaScriptEnabled", "JavascriptInterface")
    private fun createWebView(context: android.content.Context): WebView = WebView(context).apply {
        settings.javaScriptEnabled = true
        webViewClient = WebViewClient()
        addJavascriptInterface(PageListener(this), "Deaddit")

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
        val html = template.replace("%IMAGES%", images.toString())
            .replace("%BACKGROUND_COLOR%", styling.secondaryColor.toHex())
            .replace("%MODE%", if (gallery) "pages" else "")

        // As MAUI's HtmlWebViewSource loaded it.
        loadDataWithBaseURL("file:///android_asset/", html, "text/html", "UTF-8", null)
    }

    private fun setMode(pages: Boolean, context: android.content.Context) {
        this.pages = pages
        webView.get(context).evaluateJavascript("setMode('${if (pages) "pages" else "scroll"}')", null)
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current

        Box(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            Column(Modifier.fillMaxSize()) {
                TopBar {
                    TopBarButton(Icons.AutoMirrored.Outlined.ArrowBack, "Back", color = styling.textColor.color) { services.navigator.remove(this@ImageViewerPage) }
                    Text(
                        when {
                            !gallery -> ""
                            pages -> "${page + 1} / ${items.size}"
                            else -> "${items.size} images"
                        },
                        Modifier.weight(1f),
                        color = styling.subTextColor.color,
                        fontSize = styling.subTextFontSize.px,
                        textAlign = TextAlign.Center,
                    )
                    if (gallery) {
                        Row(
                            Modifier.padding(end = 8.dp).clip(styling.innerShape).background(styling.cardColor.color)
                                .border(1.dp, styling.borderColor.color, styling.innerShape).padding(2.dp),
                            horizontalArrangement = Arrangement.spacedBy(2.dp),
                        ) {
                            IconAction(Icons.Outlined.ViewCarousel, "Page view", size = 36.dp, active = pages, tint = if (pages) styling.textColor.color else styling.subTextColor.color) { setMode(true, context) }
                            IconAction(Icons.Outlined.ViewDay, "Scroll view", size = 36.dp, active = !pages, tint = if (!pages) styling.textColor.color else styling.subTextColor.color) { setMode(false, context) }
                        }
                    }
                }
                webView.Content(Modifier.fillMaxWidth().weight(1f))
                Divider()
                Row(Modifier.fillMaxWidth().navigationBarsPadding().padding(vertical = 6.dp), horizontalArrangement = Arrangement.SpaceEvenly) {
                    IconAction(Icons.Outlined.Share, "Share", size = 44.dp) { share() }
                    IconAction(Icons.Outlined.Download, "Save", size = 44.dp) { save() }
                }
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
