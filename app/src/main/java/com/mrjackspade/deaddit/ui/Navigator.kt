package com.mrjackspade.deaddit.ui

import androidx.activity.compose.BackHandler
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveableStateHolder
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import java.util.concurrent.atomic.AtomicLong

/**
 * A page, and its state. It lives as long as it's on the back stack, so going back returns to it
 * exactly as it was (loaded items, scroll position), like the MAUI app's navigation stack.
 */
abstract class Screen {
    val key: String = "screen-${nextKey.incrementAndGet()}"

    /** Work tied to this page: cancelled when it's closed. */
    val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)

    @Composable
    abstract fun Content()

    /** Called when the page leaves the back stack. */
    open fun onClosed() {}

    /** Ends the page: called by the navigator as it leaves the back stack. */
    internal fun closeScreen() {
        onClosed()
        scope.cancel()
    }

    private companion object {
        val nextKey = AtomicLong()
    }
}

class Navigator(root: Screen) {
    val stack = mutableStateListOf(root)

    val top: Screen get() = stack.last()

    fun push(screen: Screen) {
        stack.add(screen)
    }

    fun pop() {
        if (stack.size > 1) stack.removeAt(stack.size - 1).closeScreen()
    }

    /** Closes [screen] wherever it is in the stack. */
    fun remove(screen: Screen) {
        if (stack.size > 1 && stack.remove(screen)) screen.closeScreen()
    }
}

@Composable
fun NavigationHost(navigator: Navigator) {
    val holder = rememberSaveableStateHolder()
    val top = navigator.top
    val keys = navigator.stack.map { it.key }

    BackHandler(enabled = navigator.stack.size > 1) { navigator.pop() }

    // Saved UI state (scroll positions) of closed pages is dropped with them.
    val known = remember { mutableSetOf<String>() }
    LaunchedEffect(keys) {
        (known - keys.toSet()).forEach(holder::removeState)
        known.retainAll(keys.toSet())
        known.addAll(keys)
    }

    holder.SaveableStateProvider(top.key) {
        top.Content()
    }
}
