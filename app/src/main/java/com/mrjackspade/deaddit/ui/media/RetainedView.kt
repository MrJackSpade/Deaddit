package com.mrjackspade.deaddit.ui.media

import android.content.Context
import android.view.View
import android.view.ViewGroup
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
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

/** Work in progress on a page, shown as that page's spinner overlay (the MAUI pages' busy/download overlay). */
class BusyCounter {
    var count by mutableIntStateOf(0)
        private set

    suspend fun <T> run(work: suspend () -> T): T {
        count++
        try {
            return work()
        } finally {
            count--
        }
    }
}

@Composable
fun BusyOverlay(busy: BusyCounter) {
    if (busy.count <= 0) return
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Box(Modifier.background(Color(0xAA000000), RoundedCornerShape(12.dp)).padding(30.dp)) {
            CircularProgressIndicator(color = Color.White)
        }
    }
}
