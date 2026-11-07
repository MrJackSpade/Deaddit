package com.mrjackspade.deaddit.ui.media

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

import android.content.Context
import androidx.annotation.OptIn
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.media3.common.MediaItem
import androidx.media3.common.PlaybackException
import androidx.media3.common.Player
import androidx.media3.common.util.UnstableApi
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.ui.AspectRatioFrameLayout
import androidx.media3.ui.PlayerView
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.Request
import java.io.File
import java.io.IOException

/**
 * Plays a video, looping, with the player's controls (the C# EmbeddedVideo). DASH playlists
 * (Reddit videos) are downloaded at the top quality, muxed with their audio, and played from the
 * file, so playback doesn't start at the lowest quality on every play and loop. The screen stays
 * on while the page shows.
 */
@OptIn(UnstableApi::class)
class VideoPlayerPage(private val services: Services, private val item: FileDownload) : Screen() {
    private val busy = BusyCounter()

    // Shares outlive the page (the C# never cancelled them); saves stop when it closes, as the C# OnDisappearing cancelled them.
    private val shares = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private var download: Job? = null

    private var player: ExoPlayer? = null

    private val playerView = RetainedView { context -> createPlayerView(context) }

    private fun createPlayerView(context: Context): PlayerView {
        val exo = ExoPlayer.Builder(context).build().apply {
            repeatMode = Player.REPEAT_MODE_ONE
            playWhenReady = true
            addListener(object : Player.Listener {
                override fun onPlayerError(error: PlaybackException) {
                    scope.launch { reportFailure(error) }
                }
            })
        }
        player = exo

        return PlayerView(context).apply {
            this.player = exo
            useController = true
            resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIT
            setShutterBackgroundColor(services.settings.styling.value.secondaryColor.toArgb())
            setBackgroundColor(services.settings.styling.value.secondaryColor.toArgb())
            // DeviceDisplay.KeepScreenOn while the page shows.
            keepScreenOn = true
        }
    }

    private var loadStarted = false

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current

        LaunchedEffect(Unit) {
            if (loadStarted) return@LaunchedEffect
            loadStarted = true
            val exo = playerView.get(context).player as ExoPlayer
            // On the page's scope, so the download isn't tied to this composition.
            scope.launch { load(exo, context) }
        }

        Box(Modifier.fillMaxSize()) {
            Column(Modifier.fillMaxSize()) {
                TopBar {
                    Spacer(Modifier.weight(1f))
                    TopBarButton(Icons.Filled.Share, "Share") { share() }
                    TopBarButton(Icons.Filled.Save, "Save") { save() }
                }
                playerView.Content(Modifier.fillMaxWidth().weight(1f).background(styling.secondaryColor.color))
            }
            BusyOverlay(busy)
        }
    }

    private suspend fun load(exo: ExoPlayer, context: Context) {
        if (!UrlHelper.getExtension(item.launchUrl).equals(".mpd", ignoreCase = true)) {
            exo.setMediaItem(MediaItem.fromUri(item.launchUrl))
            exo.prepare()
            return
        }

        // Handed a DASH playlist, the player streams adaptively: it starts every play, and every
        // loop, at the lowest quality and only steps up part way through. Play the top quality
        // from the start instead, muxed with its audio the same way Save does.
        try {
            val file = File(File(context.cacheDir, "video"), item.fileName)
            busy.run { services.media.fetcher.fetch(item.launchUrl, file) }
            exo.setMediaItem(MediaItem.fromUri(android.net.Uri.fromFile(file)))
            exo.prepare()
        } catch (e: Exception) {
            services.dialogs.error(e)
        }
    }

    /**
     * Without this a video the player can't load is just a black screen. Ask the server why:
     * Reddit answers 403 for videos it has pulled while the post stays up (reddit.com shows
     * "This video is no longer available" for those), which the player only reports as a
     * generic source error.
     */
    private suspend fun reportFailure(error: PlaybackException) {
        val message = try {
            val (code, name) = withContext(Dispatchers.IO) {
                services.http.newCall(Request.Builder().url(item.launchUrl).build()).execute().use { it.code to statusName(it.code) }
            }
            if (code == 403) {
                "This video is no longer available. Reddit has removed the video file (the post itself is still up)."
            } else {
                "The video failed to play: ${error.message}\n\nServer response: $code $name"
            }
        } catch (e: IOException) {
            // Both failures are reported, as the C# did: the player's and the probe's.
            "The video failed to play: ${error.message}\n\nThe server could not be reached either: ${e.message}"
        }

        services.dialogs.message(message)
    }

    private fun share() {
        download?.cancel()
        download = shares.launch {
            try {
                busy.run { services.media.shareDownloads("", listOf(item), services.media.gifConverter()) }
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    private fun save() {
        download?.cancel()
        download = scope.launch {
            try {
                busy.run { services.media.save(listOf(item), services.media.gifConverter()) }
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    override fun onClosed() {
        player?.stop()
        player?.release()
        player = null
        playerView.release()
    }

    private companion object {
        /** .NET's HttpStatusCode names, which the C# message showed after the number. */
        val STATUS_NAMES = mapOf(
            400 to "BadRequest", 401 to "Unauthorized", 402 to "PaymentRequired", 404 to "NotFound",
            405 to "MethodNotAllowed", 406 to "NotAcceptable", 407 to "ProxyAuthenticationRequired", 408 to "RequestTimeout",
            409 to "Conflict", 410 to "Gone", 411 to "LengthRequired", 412 to "PreconditionFailed",
            413 to "RequestEntityTooLarge", 414 to "RequestUriTooLong", 415 to "UnsupportedMediaType",
            416 to "RequestedRangeNotSatisfiable", 417 to "ExpectationFailed", 421 to "MisdirectedRequest",
            422 to "UnprocessableEntity", 423 to "Locked", 424 to "FailedDependency", 426 to "UpgradeRequired",
            428 to "PreconditionRequired", 429 to "TooManyRequests", 431 to "RequestHeaderFieldsTooLarge",
            451 to "UnavailableForLegalReasons", 500 to "InternalServerError", 501 to "NotImplemented",
            502 to "BadGateway", 503 to "ServiceUnavailable", 504 to "GatewayTimeout", 505 to "HttpVersionNotSupported",
            506 to "VariantAlsoNegotiates", 507 to "InsufficientStorage", 508 to "LoopDetected", 510 to "NotExtended",
            511 to "NetworkAuthenticationRequired", 200 to "OK", 204 to "NoContent", 206 to "PartialContent",
            301 to "MovedPermanently", 302 to "Found", 304 to "NotModified", 307 to "TemporaryRedirect", 308 to "PermanentRedirect",
        )

        /** The name .NET printed for a status code: its enum name, or the number when it has none. */
        fun statusName(code: Int): String = STATUS_NAMES[code] ?: code.toString()
    }
}
