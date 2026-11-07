package com.mrjackspade.deaddit.ui

import android.util.Log
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.mrjackspade.deaddit.api.RedditApiException
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CompletableDeferred
import kotlinx.serialization.json.jsonPrimitive

/** One choice in an action sheet, and what it does. */
class Option(val label: String?, val action: suspend () -> Unit)

/**
 * The app's dialogs, awaited like MAUI's DisplayAlert/DisplayActionSheet/DisplayPromptAsync.
 * One shows at a time; [Host] draws it.
 */
class Dialogs(private val testMode: Boolean) {
    /** A dialog on screen, and the answer it's waiting for; [R] is that answer's type. */
    private sealed class Request<R> {
        val result = CompletableDeferred<R>()
    }
    private class Alert(val title: String, val message: String, val accept: String, val cancel: String?) : Request<Boolean>()
    private class Sheet(val title: String?, val options: List<String>) : Request<String?>()
    private class Prompt(val title: String, val message: String, val initial: String) : Request<String?>()

    private var current by mutableStateOf<Request<*>?>(null)
    private var busy by mutableIntStateOf(0)

    suspend fun alert(title: String, message: String, accept: String = "OK") {
        show(Alert(title, message, accept, null))
    }

    suspend fun confirm(title: String, message: String, accept: String, cancel: String): Boolean =
        show(Alert(title, message, accept, cancel))

    /** Shows the options (blank ones left out) and runs the chosen one. */
    suspend fun choose(title: String?, vararg options: Option) {
        val shown = options.filter { !it.label.isNullOrBlank() }
        val picked = show(Sheet(title, shown.map { it.label!! })) ?: return
        shown.first { it.label == picked }.action()
    }

    /** The entered text, or null when cancelled. */
    suspend fun prompt(title: String, message: String = "", initial: String = ""): String? =
        show(Prompt(title, message, initial))

    /** Shows a spinner over the page while [work] runs. */
    suspend fun <T> busy(work: suspend () -> T): T {
        busy++
        try {
            return work()
        } finally {
            busy--
        }
    }

    /**
     * Reports a failure: always to the log, with its stack; to the screen unless in test mode.
     * Reddit's own errors show their message alone, others with their stack trace.
     */
    suspend fun error(e: Throwable) {
        if (e is CancellationException) throw e
        Log.e(TAG, "Error", e)
        if (testMode) return

        when (e) {
            is RedditApiException -> if (e.type?.startsWith("Reddit") == true) {
                alert("Reddit", e.error["message"]!!.jsonPrimitive.content)
            } else {
                alert("Error", "An error occurred: ${e.message}")
            }
            else -> alert("Error", "An error occurred: ${e.message}\n\nStack Trace:\n${e.stackTraceToString()}")
        }
    }

    suspend fun message(text: String) {
        Log.i(TAG, text)
        if (testMode) return
        alert("Alert", text)
    }

    /** Shows [request] and waits for its answer, typed by the request itself. */
    private suspend fun <R> show(request: Request<R>): R {
        if (testMode) {
            Log.w(TAG, "Dialog in test mode: ${describe(request)}")
        }
        current = request
        try {
            return request.result.await()
        } finally {
            if (current === request) current = null
        }
    }

    private fun describe(request: Request<*>) = when (request) {
        is Alert -> "alert '${request.title}': ${request.message}"
        is Sheet -> "choose '${request.title}': ${request.options}"
        is Prompt -> "prompt '${request.title}'"
    }

    @Composable
    fun Host() {
        if (busy > 0) {
            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Box(
                    Modifier.background(Color(0xAA000000), RoundedCornerShape(12.dp)).padding(30.dp),
                ) { CircularProgressIndicator(color = Color.White) }
            }
        }

        when (val request = current) {
            is Alert -> AlertDialog(
                onDismissRequest = { request.result.complete(false) },
                title = { Text(request.title) },
                text = { Text(request.message, Modifier.verticalScroll(rememberScrollState())) },
                confirmButton = { TextButton({ request.result.complete(true) }) { Text(request.accept) } },
                dismissButton = request.cancel?.let { cancel -> { TextButton({ request.result.complete(false) }) { Text(cancel) } } },
            )

            is Sheet -> Dialog(onDismissRequest = { request.result.complete(null) }) {
                Column(
                    Modifier.fillMaxWidth().background(Color(0xFF2B2B2B), RoundedCornerShape(8.dp))
                        .verticalScroll(rememberScrollState()),
                ) {
                    request.title?.let { Text(it, Modifier.padding(16.dp), color = Color.White) }
                    for (option in request.options) {
                        HorizontalDivider(color = Color(0xFF444444))
                        Text(
                            option,
                            Modifier.fillMaxWidth().clickable { request.result.complete(option) }.padding(16.dp),
                            color = Color.White,
                        )
                    }
                }
            }

            is Prompt -> {
                var text by remember(request) { mutableStateOf(request.initial) }
                AlertDialog(
                    onDismissRequest = { request.result.complete(null) },
                    title = { Text(request.title) },
                    text = {
                        Column {
                            if (request.message.isNotEmpty()) Text(request.message)
                            OutlinedTextField(text, { text = it }, singleLine = true)
                        }
                    },
                    confirmButton = { TextButton({ request.result.complete(text) }) { Text("OK") } },
                    dismissButton = { TextButton({ request.result.complete(null) }) { Text("Cancel") } },
                )
            }

            null -> Unit
        }
    }

    private companion object {
        const val TAG = "Deaddit"
    }
}
