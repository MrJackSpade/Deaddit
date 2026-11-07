package com.mrjackspade.deaddit.ui.media

import android.content.Context
import android.view.View
import android.view.ViewGroup
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView

/**
 * A view owned by its page rather than by the composition. Only the top page is composed, so a
 * view made in the composition would be thrown away (a web page reloaded, scroll lost) whenever
 * another page covers this one; this one is kept until the page closes and [release]s it.
 */
class RetainedView<V : View>(private val create: (Context) -> V) {
    private var view: V? = null

    /** The view, made on first use. */
    fun get(context: Context): V = view ?: create(context).also { view = it }

    /** The view if it has been made. */
    val current: V? get() = view

    /** Drops the view, after [cleanup] (e.g. WebView.destroy). */
    fun release(cleanup: (V) -> Unit = {}) {
        view?.let(cleanup)
        view = null
    }

    @Composable
    fun Content(modifier: Modifier = Modifier) {
        val context = LocalContext.current
        AndroidView(
            modifier = modifier,
            factory = {
                val v = get(context)
                (v.parent as? ViewGroup)?.removeView(v)
                // Fill the space Compose gives it. Without this a WebView is laid out as
                // WRAP_CONTENT, and a page sized "height: 100%" (the image viewer) measures 0 tall.
                v.layoutParams = ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
                v
            },
        )
    }
}
