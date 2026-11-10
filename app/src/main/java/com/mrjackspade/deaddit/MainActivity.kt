package com.mrjackspade.deaddit

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.ui.Modifier
import com.mrjackspade.deaddit.ui.DeadditTheme
import com.mrjackspade.deaddit.ui.NavigationHost
import com.mrjackspade.deaddit.ui.Navigator
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Tab
import com.mrjackspade.deaddit.ui.color

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val services = (application as DeadditApp).services
        services.activityResults.attach(this)

        // "Keep Alive (Background Service)", as the C# MainActivity started it.
        if (services.settings.hacks.value.keepAlive) {
            if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU &&
                checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS) != android.content.pm.PackageManager.PERMISSION_GRANTED
            ) {
                requestPermissions(arrayOf(android.Manifest.permission.POST_NOTIFICATIONS), 0)
            }
            KeepAliveService.start(this)
        }

        // The pages outlive the activity (rotation, theme changes): the navigator is made once.
        // The app opens on the subreddit list, as it always has.
        if (!services.hasNavigator) {
            services.navigator = Navigator({ tab -> Pages.tabRoot(services, tab) }, Tab.Subreddits)
        }

        setContent {
            val styling = services.settings.styling.value
            DeadditTheme(styling, services.settings.hacks.value) {
                Box(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
                    NavigationHost(services.navigator)
                    services.dialogs.Host()
                }
            }
        }
    }

    override fun onDestroy() {
        (application as DeadditApp).services.activityResults.detach(this)
        super.onDestroy()
    }
}
