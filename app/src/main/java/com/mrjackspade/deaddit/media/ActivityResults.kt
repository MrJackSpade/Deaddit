package com.mrjackspade.deaddit.media

import androidx.activity.ComponentActivity
import androidx.activity.result.contract.ActivityResultContract
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.concurrent.atomic.AtomicLong

/**
 * Awaitable activity results (system pickers, permission prompts) for code that has no activity
 * of its own. [MainActivity][com.mrjackspade.deaddit.MainActivity] attaches itself on creation.
 */
class ActivityResults {
    private var activity: ComponentActivity? = null
    private val nextKey = AtomicLong()

    fun attach(activity: ComponentActivity) {
        this.activity = activity
    }

    fun detach(activity: ComponentActivity) {
        if (this.activity === activity) this.activity = null
    }

    /** Launches [contract] with [input] and waits for its result. */
    suspend fun <I, O> launch(contract: ActivityResultContract<I, O>, input: I): O = withContext(Dispatchers.Main) {
        val host = activity ?: throw IllegalStateException("No activity is showing to launch ${contract.javaClass.simpleName} from.")
        val result = CompletableDeferred<O>()
        val launcher = host.activityResultRegistry.register("deaddit-result-${nextKey.incrementAndGet()}", contract) { result.complete(it) }
        try {
            launcher.launch(input)
            result.await()
        } finally {
            launcher.unregister()
        }
    }
}
