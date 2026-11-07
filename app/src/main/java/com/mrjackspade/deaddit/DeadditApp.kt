package com.mrjackspade.deaddit

import android.app.Application
import coil3.ImageLoader
import coil3.PlatformContext
import coil3.SingletonImageLoader
import coil3.gif.AnimatedImageDecoder
import coil3.network.okhttp.OkHttpNetworkFetcherFactory
import com.mrjackspade.deaddit.api.NativeRedditApi
import java.io.File

class DeadditApp : Application(), SingletonImageLoader.Factory {
    lateinit var services: Services
        private set

    override fun onCreate() {
        super.onCreate()

        // Debug builds expose their WebViews (login, browser, image viewer) to DevTools, for driving them.
        if (BuildConfig.DEBUG) android.webkit.WebView.setWebContentsDebuggingEnabled(true)

        // Reddit.Api's own default user agent, as the MAUI app sent.
        NativeRedditApi.init(userAgent = null)

        services = Services(this, testMode = File(filesDir, TEST_MODE_FLAG).exists())
    }

    /** Images load through the app's HTTP client (no time limits), and GIFs animate. */
    override fun newImageLoader(context: PlatformContext): ImageLoader =
        ImageLoader.Builder(context)
            // Failed loads go to logcat (Coil reports nothing otherwise).
            .logger(coil3.util.DebugLogger(coil3.util.Logger.Level.Error))
            .components {
                add(OkHttpNetworkFetcherFactory(callFactory = { services.http }))
                // Android's animated decoder exists from API 28; Coil's own GIF decoder before that.
                if (android.os.Build.VERSION.SDK_INT >= 28) add(AnimatedImageDecoder.Factory()) else add(coil3.gif.GifDecoder.Factory())
            }
            .build()

    companion object {
        /**
         * Test mode, for driving the app from a script: errors go to logcat instead of dialogs,
         * and nothing opens the login page. On when this file exists in the app's files directory
         * (`adb shell run-as com.companyname.deaddit touch files/test-mode`).
         */
        const val TEST_MODE_FLAG = "test-mode"
    }
}
