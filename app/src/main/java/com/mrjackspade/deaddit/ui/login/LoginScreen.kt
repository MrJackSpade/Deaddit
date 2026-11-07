package com.mrjackspade.deaddit.ui.login

import android.annotation.SuppressLint
import android.os.Message
import android.view.ViewGroup
import android.webkit.CookieManager
import android.webkit.WebChromeClient
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.FrameLayout
import androidx.activity.compose.BackHandler
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.ui.viewinterop.AndroidView
import com.mrjackspade.deaddit.core.session.TokenV2
import com.mrjackspade.deaddit.ui.Navigator
import com.mrjackspade.deaddit.ui.Screen
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.delay
import java.time.format.DateTimeFormatter

/** What the login page hands back: the token_v2 value, when it expires, and every reddit.com cookie. */
data class WebLogin(val accessToken: String, val expiration: String, val cookies: String)

/** The login page on the back stack; [open] waits for it to log in, or for the user to leave it. */
class LoginPage private constructor(private val navigator: Navigator) : Screen() {
    private val result = CompletableDeferred<WebLogin?>()

    @Composable
    override fun Content() {
        LoginScreen(
            onLoggedIn = { login ->
                result.complete(login)
                navigator.remove(this)
            },
            // As the C# page: a failed check ends the login, and whoever waits on it gets the error.
            onFailed = { e ->
                result.completeExceptionally(e)
                navigator.remove(this)
            },
            modifier = Modifier.fillMaxSize(),
        )
    }

    override fun onClosed() {
        result.complete(null)
    }

    companion object {
        suspend fun open(navigator: Navigator): WebLogin? {
            val page = LoginPage(navigator)
            navigator.push(page)
            return page.result.await()
        }
    }
}

/**
 * Reddit's own login page. Login happens over XHR with no navigation to hook, so this polls the
 * cookie jar until it holds a logged-in, unexpired token_v2. A token left from an earlier session
 * may be expired; Reddit replaces it as the page loads, so polling continues past it. A cookie
 * that can't be read stops the polling and goes to [onFailed].
 */
@SuppressLint("SetJavaScriptEnabled")
@Composable
fun LoginScreen(onLoggedIn: (WebLogin) -> Unit, onFailed: (Exception) -> Unit, modifier: Modifier = Modifier) {
    // "Sign in with Google/Apple" opens a popup that reports back to this page with postMessage,
    // so popups open as real windows: a WebView over the login page, closed when the provider
    // closes it. The open popup, so back can close it.
    var popup by remember { mutableStateOf<WebView?>(null) }

    BackHandler(enabled = popup != null) {
        popup?.let { (it.parent as? ViewGroup)?.removeView(it); it.destroy() }
        popup = null
    }

    AndroidView(
        modifier = modifier,
        factory = { context ->
            val container = FrameLayout(context)

            fun configure(view: WebView) {
                view.settings.javaScriptEnabled = true
                view.settings.domStorageEnabled = true
                view.settings.setSupportMultipleWindows(true)
                view.settings.javaScriptCanOpenWindowsAutomatically = true
                // Identity providers' popups need their own cookies alongside Reddit's.
                CookieManager.getInstance().setAcceptThirdPartyCookies(view, true)
                view.webViewClient = WebViewClient()
                view.webChromeClient = object : WebChromeClient() {
                    override fun onCreateWindow(view: WebView, isDialog: Boolean, isUserGesture: Boolean, resultMsg: Message): Boolean {
                        val child = WebView(context)
                        configure(child)
                        container.addView(child, FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
                        popup = child
                        (resultMsg.obj as WebView.WebViewTransport).webView = child
                        resultMsg.sendToTarget()
                        return true
                    }

                    override fun onCloseWindow(window: WebView) {
                        container.removeView(window)
                        window.destroy()
                        if (popup === window) popup = null
                    }
                }
            }

            val main = WebView(context)
            configure(main)
            container.addView(main, FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
            main.loadUrl("https://www.reddit.com/login")
            container
        },
    )

    LaunchedEffect(Unit) {
        while (true) {
            val login = try {
                readLogin()
            } catch (e: Exception) {
                onFailed(e)
                return@LaunchedEffect
            }
            if (login != null) {
                onLoggedIn(login)
                return@LaunchedEffect
            }
            delay(1000)
        }
    }
}

private fun readLogin(): WebLogin? {
    val header = CookieManager.getInstance().getCookie("https://www.reddit.com") ?: return null
    val cookies = header.split(';').mapNotNull {
        val pair = it.trim().split('=', limit = 2)
        if (pair.size == 2) pair[0] to pair[1] else null
    }.toMap()

    val accessToken = cookies["token_v2"] ?: return null
    val token = TokenV2.parse(accessToken)
    if (!token.isLoggedIn || token.isExpired) return null

    return WebLogin(
        accessToken = accessToken,
        expiration = DateTimeFormatter.ISO_INSTANT.format(token.expiration),
        cookies = cookies.entries.joinToString("; ") { "${it.key}=${it.value}" },
    )
}
