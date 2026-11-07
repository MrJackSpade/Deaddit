package com.mrjackspade.deaddit.media

import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.ensureActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import java.io.IOException
import java.io.InputStream
import java.io.OutputStream

/**
 * Sends [request] and hands the response to [block] (which must be done with it when it returns), off the main
 * thread. Cancelling the calling coroutine cancels the call, which also breaks off a body being read, as the C#
 * app's CancellationToken did; that cancellation then surfaces as the coroutine's CancellationException rather than
 * the IOException OkHttp throws for it.
 */
suspend fun <T> OkHttpClient.fetch(request: Request, block: suspend (Response) -> T): T = withContext(Dispatchers.IO) {
    val call = newCall(request)
    val canceller = launch(start = CoroutineStart.UNDISPATCHED) {
        try {
            awaitCancellation()
        } finally {
            call.cancel()
        }
    }

    try {
        call.execute().use { block(it) }
    } catch (e: IOException) {
        // When the IOException is the cancel's doing, this throws the CancellationException instead.
        ensureActive()
        throw e
    } finally {
        canceller.cancel()
    }
}

/** InputStream.copyTo that stops between buffers once the calling coroutine is cancelled (Stream.CopyToAsync with a token). */
suspend fun InputStream.copyToCancellable(out: OutputStream): Long {
    val buffer = ByteArray(DEFAULT_BUFFER_SIZE)
    var total = 0L
    while (true) {
        currentCoroutineContext().ensureActive()
        val read = read(buffer)
        if (read < 0) return total
        out.write(buffer, 0, read)
        total += read
    }
}
