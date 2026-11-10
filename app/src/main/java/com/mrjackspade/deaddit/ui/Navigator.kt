package com.mrjackspade.deaddit.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.consumeWindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.GridView
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.MailOutline
import androidx.compose.material.icons.outlined.Tune
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveableStateHolder
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.compose.foundation.clickable
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

/** The bottom bar's tabs, each the root of the back stack while it's shown. */
enum class Tab(val label: String, val icon: ImageVector) {
    Home("Home", Icons.Outlined.Home),
    Subreddits("Subreddits", Icons.Outlined.GridView),
    Inbox("Inbox", Icons.Outlined.MailOutline),
    Settings("Settings", Icons.Outlined.Tune),
}

/**
 * The back stack, rooted at a tab's page. Each tab's page is made the first time the tab is shown
 * ([makeTab]) and kept for good, so going back to a tab finds it as it was; pages opened over it
 * close when another tab is picked. Back from another tab's root goes to [startTab].
 */
class Navigator(private val makeTab: (Tab) -> Screen, private val startTab: Tab) {
    private val roots = HashMap<Tab, Screen>()

    private fun root(tab: Tab): Screen = roots.getOrPut(tab) { makeTab(tab) }

    var tab by mutableStateOf(startTab)
        private set

    val stack = mutableStateListOf(root(startTab))

    val top: Screen get() = stack.last()

    /** Whether [screen] is a tab's page, which stays for good. */
    fun isTabRoot(screen: Screen): Boolean = roots.values.any { it === screen }

    /** Whether [key] is a tab's page's, whose saved state (its scroll position) stays with it. */
    fun isTabRootKey(key: String): Boolean = roots.values.any { it.key == key }

    /** The bottom bar shows on the tabs' own pages, not over pages opened from them. */
    val showsTabs: Boolean get() = stack.size == 1

    val canGoBack: Boolean get() = stack.size > 1 || tab != startTab

    fun push(screen: Screen) {
        stack.add(screen)
    }

    fun pop() {
        if (stack.size > 1) stack.removeAt(stack.size - 1).closeScreen()
    }

    /** Closes [screen] wherever it is in the stack; a tab's page stays. */
    fun remove(screen: Screen) {
        if (!isTabRoot(screen) && stack.remove(screen)) screen.closeScreen()
    }

    /** Shows [tab]'s page, closing what was opened over the current one. */
    fun showTab(tab: Tab) {
        while (stack.size > 1) pop()
        stack[0] = root(tab)
        this.tab = tab
    }

    fun back() {
        if (stack.size > 1) pop() else showTab(startTab)
    }
}

@Composable
fun NavigationHost(navigator: Navigator) {
    val holder = rememberSaveableStateHolder()
    val top = navigator.top
    val keys = navigator.stack.map { it.key }

    BackHandler(enabled = navigator.canGoBack) { navigator.back() }

    // Saved UI state (scroll positions) of closed pages is dropped with them; the tabs' pages stay.
    val known = remember { mutableSetOf<String>() }
    LaunchedEffect(keys) {
        val gone = (known - keys.toSet()).filterNot(navigator::isTabRootKey)
        gone.forEach(holder::removeState)
        known.removeAll(gone.toSet())
        known.addAll(keys)
    }

    val tabs = navigator.showsTabs
    Column(Modifier.fillMaxSize()) {
        // Under the bottom bar, the page doesn't pad for the system's navigation bar: the bar does.
        Box(Modifier.weight(1f).fillMaxWidth().let { if (tabs) it.consumeWindowInsets(WindowInsets.navigationBars) else it }) {
            holder.SaveableStateProvider(top.key) {
                top.Content()
            }
        }
        if (tabs) BottomBar(navigator)
    }
}

/** The tabs as icons, the shown one on a quiet chip. */
@Composable
private fun BottomBar(navigator: Navigator) {
    val styling = LocalStyling.current
    Column(Modifier.fillMaxWidth().background(styling.primaryColor.color)) {
        Divider()
        Row(
            Modifier.fillMaxWidth().navigationBarsPadding().padding(horizontal = 12.dp, vertical = 6.dp),
            horizontalArrangement = Arrangement.SpaceAround,
        ) {
            for (tab in Tab.entries) {
                val shown = navigator.tab == tab
                Box(
                    Modifier.size(width = 64.dp, height = 44.dp).clip(styling.cardShape)
                        .background(if (shown) styling.tertiaryColor.color else Color.Transparent)
                        .clickable(role = Role.Tab) { navigator.showTab(tab) }
                        .semantics {
                            contentDescription = tab.label
                            selected = shown
                        },
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(tab.icon, contentDescription = null, tint = if (shown) styling.textColor.color else styling.subTextColor.color, modifier = Modifier.size(24.dp))
                }
            }
        }
    }
}
