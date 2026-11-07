package com.mrjackspade.deaddit.session

import android.content.Context
import com.mrjackspade.deaddit.core.model.RedditJson
import com.mrjackspade.deaddit.core.session.Session

/** Keeps the logged-in session between launches. */
class SessionStore(context: Context) {
    private val preferences = context.getSharedPreferences("session", Context.MODE_PRIVATE)

    fun load(): Session? =
        preferences.getString(KEY, null)?.let { RedditJson.decodeFromString(Session.serializer(), it) }

    fun save(session: Session) {
        preferences.edit().putString(KEY, RedditJson.encodeToString(Session.serializer(), session)).apply()
    }

    fun clear() {
        preferences.edit().remove(KEY).apply()
    }

    private companion object {
        const val KEY = "session"
    }
}
