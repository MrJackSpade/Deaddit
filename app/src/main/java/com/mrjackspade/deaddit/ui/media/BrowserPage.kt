package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.MenuBook

import android.annotation.SuppressLint
import android.content.Context
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

/**
 * A web page in the app (the C# EmbeddedBrowser): open it in the device's browser, try it
 * through removepaywall.com, read it in reader mode, or share its link.
 */
class BrowserPage(private val services: Services, private val url: String) : Screen() {
    private val busy = BusyCounter()

    /** The page being shown, followed as the user navigates; reader mode is built from it. */
    private var currentUrl: String = upgraded(url)

    private var readerMode by mutableStateOf(false)

    private val webView = RetainedView { context -> createWebView(context) }

    @SuppressLint("SetJavaScriptEnabled")
    private fun createWebView(context: Context): WebView = WebView(context).apply {
        // As MAUI's WebView set up.
        settings.javaScriptEnabled = true
        settings.domStorageEnabled = true
        webViewClient = object : WebViewClient() {
            /**
             * Tracks the live page as the user navigates, and drops out of reader mode when they
             * follow a link out of the extracted article.
             */
            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                if (!request.isForMainFrame) return false
                val target = request.url.toString()
                if (!target.startsWith("http", ignoreCase = true)) return false

                currentUrl = target
                if (readerMode) readerMode = false
                return false
            }
        }
        loadUrl(currentUrl)
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current

        Box(Modifier.fillMaxSize()) {
            Column(Modifier.fillMaxSize()) {
                TopBar {
                    Spacer(Modifier.weight(1f))
                    TopBarButton(Icons.Filled.OpenInBrowser, "Open in browser") { act { services.platform.openExternally(url) } }
                    TopBarButton(Icons.Filled.LockOpen, "Bypass paywall") {
                        webView.get(context).loadUrl("https://www.removepaywall.com/search?url=$url")
                    }
                    TopBarButton(
                        Icons.AutoMirrored.Filled.MenuBook, "Reader mode",
                        color = if (readerMode) styling.hyperlinkColor.color else styling.textColor.color,
                    ) { act { toggleReader(context) } }
                    TopBarButton(Icons.Filled.Share, "Share") { act { services.platform.shareText(url, null) } }
                    // The C# save button does nothing ("Logic to save current state or media").
                    TopBarButton(Icons.Filled.Save, "Save") {}
                }
                webView.Content(Modifier.fillMaxWidth().weight(1f))
            }
            BusyOverlay(busy)
        }
    }

    private fun act(action: suspend () -> Unit) {
        scope.launch {
            try {
                action()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    private suspend fun toggleReader(context: Context) {
        val view = webView.get(context)

        if (readerMode) {
            readerMode = false
            view.loadUrl(currentUrl)
            return
        }

        busy.run { enterReaderMode(context, view) }
    }

    private suspend fun enterReaderMode(context: Context, view: WebView) {
        val script = withContext(Dispatchers.IO) { ReaderMode.script(context) }
        val evaluated = CompletableDeferred<String?>()
        view.evaluateJavascript(script) { evaluated.complete(it) }

        val article = when (val result = ReaderMode.parse(evaluated.await())) {
            ReaderMode.Result.Unreadable -> {
                services.dialogs.message("Couldn't read the page contents.")
                return
            }

            ReaderMode.Result.NoArticle -> {
                services.dialogs.message("No article found on this page.")
                return
            }

            is ReaderMode.Result.Article -> result.article
        }

        readerMode = true
        val html = ReaderMode.render(context, article, services.settings.styling.value)
        // As MAUI's HtmlWebViewSource loaded it.
        view.loadDataWithBaseURL("file:///android_asset/", html, "text/html", "UTF-8", null)
    }

    override fun onClosed() {
        webView.release { it.destroy() }
    }

    private companion object {
        /** Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https */
        fun upgraded(url: String) = if (url.startsWith("http://")) url.replace("http://", "https://") else url
    }
}
