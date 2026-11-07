package com.mrjackspade.deaddit.session

import android.content.Context
import com.mrjackspade.deaddit.core.config.ExportedSession
import com.mrjackspade.deaddit.core.config.SettingsJson
import com.mrjackspade.deaddit.core.session.Session
import com.mrjackspade.deaddit.settings.AppPreferences

/** Keeps the logged-in session between launches, as the C# app's BearerToken JSON under its key. */
class SessionStore(context: Context) {
    private val preferences = AppPreferences.of(context)

    fun load(): Session? =
        preferences.getString(KEY, null)?.let { SettingsJson.decodeFromString(ExportedSession.serializer(), it).toSession() }

    fun save(session: Session) {
        preferences.edit().putString(KEY, SettingsJson.encodeToString(ExportedSession.serializer(), ExportedSession.of(session))).apply()
    }

    fun clear() {
        preferences.edit().remove(KEY).apply()
    }

    private companion object {
        const val KEY = "reddit_bearer_token"
    }
}
