package com.mrjackspade.deaddit

import android.content.Context
import android.util.Log
import android.webkit.CookieManager
import com.mrjackspade.deaddit.api.NativeRedditApi
import com.mrjackspade.deaddit.core.api.RedditApi
import com.mrjackspade.deaddit.core.model.RedditJson
import com.mrjackspade.deaddit.core.service.RedditService
import com.mrjackspade.deaddit.core.session.Session
import com.mrjackspade.deaddit.session.SessionStore
import com.mrjackspade.deaddit.settings.HistoryTracker
import com.mrjackspade.deaddit.settings.SettingsStore
import com.mrjackspade.deaddit.settings.UserTags
import com.mrjackspade.deaddit.settings.VisitTracker
import com.mrjackspade.deaddit.ui.Dialogs
import com.mrjackspade.deaddit.ui.Navigator
import com.mrjackspade.deaddit.ui.login.LoginPage
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withContext
import java.io.File

/**
 * Everything the pages share. One per process, created by [DeadditApp]; [navigator] is set once
 * the activity draws its first page.
 */
class Services(context: Context, val testMode: Boolean) {
    val api = RedditApi(NativeRedditApi::call)
    val settings = SettingsStore(context)
    val reddit = RedditService(api) { settings.hacks.value }
    val sessions = SessionStore(context)
    val visits = VisitTracker(context)
    val history = HistoryTracker(context)
    val userTags = UserTags(File(context.filesDir, "user_tags.tsv"))
    val dialogs = Dialogs(testMode)

    /** For everything but Reddit.Api's own requests: media, ETags, Claude. */
    // OkHttp limits connect/read/write to 10s by default; 0 removes those limits.
    val http = okhttp3.OkHttpClient.Builder()
        .connectTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .readTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .writeTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .build()
    val platform = com.mrjackspade.deaddit.Platform(context)
    /** System pickers and permission prompts, awaited; MainActivity attaches itself. */
    val activityResults = com.mrjackspade.deaddit.media.ActivityResults()
    val media = com.mrjackspade.deaddit.media.MediaService(context, http, settings, platform, activityResults)

    /** The application context, for the settings page's files and the keep-alive service. */
    val appContext: Context = context.applicationContext

    /** AI Assist. Made on first use from the AI settings of that moment, as the C# DI singleton was. */
    val claude by lazy { com.mrjackspade.deaddit.ai.ClaudeService(http, settings.ai.value) }

    /** What opening, sharing or saving a post or link does, by kind. First match wins (the C# MauiProgram's order). */
    val posts: com.mrjackspade.deaddit.handlers.PostHandlers by lazy {
        com.mrjackspade.deaddit.handlers.PostHandlers(
            handlers = listOf(
                com.mrjackspade.deaddit.handlers.SelfPostHandler(this),
                com.mrjackspade.deaddit.handlers.RedditGalleryHandler(this),
                com.mrjackspade.deaddit.handlers.RedditVideoHandler(this),
                com.mrjackspade.deaddit.handlers.CrossPostHandler(),
            ),
            urls = com.mrjackspade.deaddit.handlers.UrlHandlers(
                listOf(
                    com.mrjackspade.deaddit.handlers.url.RedditInlineVideoUrlHandler(this),
                    com.mrjackspade.deaddit.handlers.url.RedditCommentsPostUrlHandler(this),
                    com.mrjackspade.deaddit.handlers.url.RedditSPostUrlHandler(this),
                    com.mrjackspade.deaddit.handlers.url.SubredditUrlHandler(this),
                    com.mrjackspade.deaddit.handlers.url.UserUrlHandler(this),
                    com.mrjackspade.deaddit.handlers.url.RedGifsHandler(this),
                    com.mrjackspade.deaddit.handlers.url.GiphyHandler(this),
                    com.mrjackspade.deaddit.handlers.url.GenericImageHandler(this),
                    com.mrjackspade.deaddit.handlers.url.GenericVideoHandler(this),
                    com.mrjackspade.deaddit.handlers.url.GenericUrlHandler(this),
                ),
            ),
            // A link's file is saved through the GIF converter when that hack is on (the C# AggregatePostHandler).
            saveDownload = { media.save(listOf(it), media.gifConverter()) },
        )
    }
    val etags = com.mrjackspade.deaddit.media.ETagCache(http)

    lateinit var navigator: Navigator

    val hasNavigator: Boolean get() = ::navigator.isInitialized

    init {
        // In test mode a rejected session is an error to log, not a reason to open the login page.
        if (!testMode) {
            // Reddit.Api asks for this from one of its own threads, mid-request, and waits.
            NativeRedditApi.sessionRefresher = {
                runBlocking { loginViaBrowser() }?.let { RedditJson.encodeToString(Session.serializer(), it) }
            }
        }
    }

    /**
     * Opens Reddit's login page, works out who logged in and keeps the session (token, expiry and
     * the browser's reddit.com cookies). Null when the login gave no usable token.
     */
    suspend fun loginViaBrowser(): Session? {
        val login = withContext(Dispatchers.Main) { LoginPage.open(navigator) } ?: return null
        val owner = api.getTokenOwner(login.accessToken, login.cookies) ?: return null
        val session = Session(login.accessToken, owner, login.expiration, login.cookies)
        sessions.save(session)
        return session
    }

    /**
     * Logs back in with the stored session. Sessions saved without cookies can't load comments,
     * and expired ones aren't accepted anywhere: those are renewed through the login page, which
     * completes itself since the browser still holds the Reddit login.
     */
    suspend fun restoreSession() {
        val stored = sessions.load() ?: return

        if (stored.sessionCookies == null || stored.isExpired) {
            sessions.clear()
            if (testMode) {
                Log.w("Deaddit", "Stored session is expired; test mode doesn't open the login page.")
                return
            }
            loginViaBrowser()?.let { api.setSession(it) }
        } else if (api.validateSession(stored)) {
            api.setSession(stored)
        } else {
            sessions.clear()
        }

        reddit.refreshUser()
    }

    fun logout() {
        sessions.clear()
        CookieManager.getInstance().removeAllCookies(null)
        CookieManager.getInstance().flush()
    }
}
