package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.MenuBook

import android.annotation.SuppressLint
import android.app.Activity
import android.content.Context
import android.content.ContextWrapper
import android.content.Intent
import android.net.Uri
import android.view.View
import android.view.ViewGroup
import android.webkit.ValueCallback
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.FrameLayout
import androidx.activity.compose.BackHandler
import androidx.activity.result.contract.ActivityResultContract
import androidx.browser.customtabs.CustomTabsIntent
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
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.ui.BusyCounter
import com.mrjackspade.deaddit.ui.BusyOverlay
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

    /** A video the page has made fullscreen, over the whole window. */
    private class Fullscreen(val view: View, val callback: WebChromeClient.CustomViewCallback, val activity: Activity)

    private var fullscreen by mutableStateOf<Fullscreen?>(null)

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
        // As MAUI's MauiWebChromeClient: the WebView's own JavaScript alert, confirm and prompt
        // dialogs (any WebChromeClient enables them), file uploads, and fullscreen video.
        webChromeClient = object : WebChromeClient() {
            override fun onShowFileChooser(view: WebView, callback: ValueCallback<Array<Uri>>, params: FileChooserParams): Boolean {
                act {
                    // The page waits on an answer, whatever happens: none when the picker fails.
                    var picked: Array<Uri>? = null
                    try {
                        picked = services.activityResults.launch(FileChooser, params)
                    } finally {
                        callback.onReceiveValue(picked)
                    }
                }
                return true
            }

            override fun onShowCustomView(view: View, callback: CustomViewCallback) {
                enterFullscreen(view, callback, context.findActivity())
            }

            override fun onHideCustomView() {
                exitFullscreen()
            }
        }
        loadUrl(currentUrl)
    }

    /** As MAUI's: the video's view over the whole window, with the system bars hidden. */
    private fun enterFullscreen(view: View, callback: WebChromeClient.CustomViewCallback, activity: Activity) {
        val decor = activity.window.decorView as FrameLayout
        decor.addView(view, FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
        WindowCompat.getInsetsController(activity.window, decor).apply {
            systemBarsBehavior = WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
            hide(WindowInsetsCompat.Type.systemBars())
        }
        fullscreen = Fullscreen(view, callback, activity)
    }

    private fun exitFullscreen() {
        val shown = fullscreen ?: return
        fullscreen = null
        val decor = shown.activity.window.decorView as FrameLayout
        decor.removeView(shown.view)
        WindowCompat.getInsetsController(shown.activity.window, decor).show(WindowInsetsCompat.Type.systemBars())
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current

        // Back leaves a fullscreen video before the page; the WebView then calls onHideCustomView.
        BackHandler(enabled = fullscreen != null) { fullscreen?.callback?.onCustomViewHidden() }

        Box(Modifier.fillMaxSize()) {
            Column(Modifier.fillMaxSize()) {
                TopBar {
                    Spacer(Modifier.weight(1f))
                    TopBarButton(Icons.Filled.OpenInBrowser, "Open in browser") { act { openInBrowser(context) } }
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

    /**
     * As the C# Browser.OpenAsync(BrowserLaunchMode.SystemPreferred): a Custom Tab, which a
     * browser without them opens as an ordinary link. Unlike the C#, a failure is shown.
     */
    private fun openInBrowser(context: Context) {
        CustomTabsIntent.Builder().build().launchUrl(context, Uri.parse(url))
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
        exitFullscreen()
        webView.release { it.destroy() }
    }

    /** The page's file chooser in a chooser titled as the page asks, as MAUI's showed it. */
    private object FileChooser : ActivityResultContract<WebChromeClient.FileChooserParams, Array<Uri>?>() {
        override fun createIntent(context: Context, input: WebChromeClient.FileChooserParams): Intent =
            Intent.createChooser(input.createIntent(), input.title)

        override fun parseResult(resultCode: Int, intent: Intent?): Array<Uri>? =
            WebChromeClient.FileChooserParams.parseResult(resultCode, intent)
    }

    private companion object {
        /** Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https */
        fun upgraded(url: String) = if (url.startsWith("http://")) url.replace("http://", "https://") else url

        /** The activity a view's context belongs to. */
        tailrec fun Context.findActivity(): Activity = when (this) {
            is Activity -> this
            is ContextWrapper -> baseContext.findActivity()
            else -> throw IllegalStateException("$this isn't an activity's context.")
        }
    }
}
