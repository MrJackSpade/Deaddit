package com.mrjackspade.deaddit.api

import android.content.Context
import android.webkit.WebResourceError
import android.webkit.WebResourceRequest
import android.webkit.WebResourceResponse
import android.webkit.WebView
import android.webkit.WebViewClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonPrimitive
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * Loads a www thread page in a WebView nobody sees, and returns its HTML once the page is the thread.
 * Plain HTTP gets only Reddit's JavaScript challenge for these pages; a WebView runs it as a browser
 * does, then reloads into the real page. It shares the login page's cookies (CookieManager), so the
 * page is the logged-in one.
 */
class ThreadPageLoader(private val context: Context) {

    suspend fun load(url: String): String = withContext(Dispatchers.Main) {
        suspendCancellableCoroutine { continuation ->
            val view = WebView(context)
            var done = false

            fun finish(result: Result<String>) {
                if (done) return
                done = true
                view.stopLoading()
                view.destroy()
                result.onSuccess(continuation::resume).onFailure(continuation::resumeWithException)
            }

            view.settings.javaScriptEnabled = true
            view.settings.domStorageEnabled = true
            // The page's media URLs are in its markup; nothing needs them loaded.
            view.settings.blockNetworkImage = true
            view.settings.mediaPlaybackRequiresUserGesture = true

            view.webViewClient = object : WebViewClient() {
                override fun onPageFinished(view: WebView, finishedUrl: String) {
                    if (done) return
                    view.evaluateJavascript(READ_PAGE) { encoded ->
                        if (done) return@evaluateJavascript
                        val result = Json.parseToJsonElement(encoded)
                        if (result is JsonNull) {
                            // The challenge page: it reloads into the thread once its script has run.
                            return@evaluateJavascript
                        }
                        val text = result.jsonPrimitive.content
                        if (text.startsWith(NOT_A_THREAD)) {
                            finish(Result.failure(IllegalStateException("www's page for $url isn't a thread (at $finishedUrl, titled \"${text.removePrefix(NOT_A_THREAD)}\").")))
                        } else {
                            finish(Result.success(text))
                        }
                    }
                }

                override fun onReceivedError(view: WebView, request: WebResourceRequest, error: WebResourceError) {
                    if (request.isForMainFrame) finish(Result.failure(IllegalStateException("Loading $url failed: ${error.description} (${error.errorCode}).")))
                }

                override fun onReceivedHttpError(view: WebView, request: WebResourceRequest, response: WebResourceResponse) {
                    if (request.isForMainFrame) finish(Result.failure(IllegalStateException("Loading $url failed: HTTP ${response.statusCode} ${response.reasonPhrase}.")))
                }
            }

            continuation.invokeOnCancellation { view.post { if (!done) { done = true; view.stopLoading(); view.destroy() } } }
            view.loadUrl(url)
        }
    }

    private companion object {
        const val NOT_A_THREAD = "\u0000not-a-thread:"

        /**
         * The page's HTML once it has the thread's post; null on the challenge page (no www app, and
         * the challenge's script); otherwise — any other page — [NOT_A_THREAD] and the page's title.
         */
        val READ_PAGE = """
            (() => {
                if (document.querySelector('shreddit-post[view-context="CommentsPage"]')) return document.documentElement.outerHTML;
                if (!document.querySelector('shreddit-app') && document.documentElement.innerHTML.includes('js_challenge')) return null;
                return ${JsonPrimitive(NOT_A_THREAD)} + document.title;
            })()
        """.trimIndent()
    }
}
