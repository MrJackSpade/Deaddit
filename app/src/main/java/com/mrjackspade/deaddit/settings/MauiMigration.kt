package com.mrjackspade.deaddit.settings

import android.content.Context
import android.util.Log
import com.mrjackspade.deaddit.core.config.ExportedSession
import com.mrjackspade.deaddit.core.config.SettingsJson
import com.mrjackspade.deaddit.core.model.RedditJson
import com.mrjackspade.deaddit.core.session.Session

/**
 * Brings over what the MAUI app stored, the first time this app runs in its place (same package,
 * so its files are still here). MAUI's Preferences wrote everything as strings into one
 * SharedPreferences file; this copies each piece to where this app keeps it. The browser's Reddit
 * login and user_tags.tsv (same path and format) need nothing.
 */
object MauiMigration {
    private const val MARKER_PREFERENCES = "migration"
    private const val MARKER = "maui-imported"

    fun run(context: Context) {
        val marker = context.getSharedPreferences(MARKER_PREFERENCES, Context.MODE_PRIVATE)
        if (marker.getBoolean(MARKER, false)) return

        // MAUI Essentials' default Preferences file.
        val maui = context.getSharedPreferences("${context.packageName}.microsoft.maui.essentials.preferences", Context.MODE_PRIVATE)
        val values = maui.all

        if (values.isNotEmpty()) {
            Log.i("Deaddit", "Importing the MAUI app's settings: ${values.keys.sorted()}")

            // Settings, each under its C# type's full name, in the same JSON this app reads.
            val settings = context.getSharedPreferences("settings", Context.MODE_PRIVATE).edit()
            for ((mauiKey, key) in SETTINGS) {
                val json = values[mauiKey] as String? ?: continue
                SettingsJson.parseToJsonElement(json) // Fails here, not later, if it isn't JSON.
                settings.putString(key, json)
            }
            settings.commit()

            // The session, as the C# BearerToken record.
            (values["reddit_bearer_token"] as String?)?.let { json ->
                val session = SettingsJson.decodeFromString(ExportedSession.serializer(), json).toSession()
                context.getSharedPreferences("session", Context.MODE_PRIVATE).edit()
                    .putString("session", RedditJson.encodeToString(Session.serializer(), session)).commit()
            }

            // History: the same JSON list of post names, under the same key.
            (values["PostHistory"] as String?)?.let { json ->
                context.getSharedPreferences("history", Context.MODE_PRIVATE).edit().putString("PostHistory", json).commit()
            }

            // Visited posts, per subreddit, in the C# tracker's packed format.
            val visits = context.getSharedPreferences("visits", Context.MODE_PRIVATE).edit()
            for ((key, value) in values) {
                if (!key.startsWith(VISITS_PREFIX)) continue
                val names = visitNames(value as String)
                if (names.isNotEmpty()) visits.putString(key.removePrefix(VISITS_PREFIX), names.joinToString("\n"))
            }
            visits.commit()
        }

        marker.edit().putBoolean(MARKER, true).commit()
    }

    /** MAUI preference key (the C# type's full name) → this app's SettingsStore key. */
    private val SETTINGS = mapOf(
        "Deaddit.Core.Configurations.Models.ApplicationStyling" to "ApplicationStyling",
        "Deaddit.Core.Configurations.Models.ApplicationHacks" to "ApplicationHacks",
        "Deaddit.Core.Configurations.Models.BlockConfiguration" to "BlockConfiguration",
        "Deaddit.Configurations.AIConfiguration" to "AIConfiguration",
        "Deaddit.Core.Configurations.Models.SavePathConfiguration" to "SavePathConfiguration",
        "Deaddit.Core.Configurations.Models.LandingPageConfiguration" to "LandingPageConfiguration",
    )

    private const val VISITS_PREFIX = "PreferencesVisitTracker_"

    /**
     * The post names in a C# PreferencesVisitTracker string: each visit is 4 bytes of date, then
     * the name, then a NUL, all as ASCII chars. Split exactly as the C# SplitVisits did (a NUL only
     * ends a visit after its first 9 chars, since the date bytes can contain NULs).
     */
    internal fun visitNames(packed: String): List<String> {
        val names = mutableListOf<String>()
        val part = StringBuilder()
        for (c in packed) {
            if (part.length <= 8) {
                part.append(c)
            } else if (c == '\u0000') {
                names += part.substring(4)
                part.clear()
            } else {
                part.append(c)
            }
        }
        return names.filter { it.isNotEmpty() }
    }
}
