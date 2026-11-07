package com.mrjackspade.deaddit.settings

import android.content.Context
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.mrjackspade.deaddit.core.config.AIConfiguration
import com.mrjackspade.deaddit.core.config.ApplicationHacks
import com.mrjackspade.deaddit.core.config.ApplicationStyling
import com.mrjackspade.deaddit.core.config.BlockConfiguration
import com.mrjackspade.deaddit.core.config.LandingPageConfiguration
import com.mrjackspade.deaddit.core.config.SavePathConfiguration
import com.mrjackspade.deaddit.core.config.SettingsJson
import kotlinx.serialization.KSerializer
import kotlinx.serialization.serializer

/**
 * The app's settings, each stored as JSON under its own key. Every value is Compose state, so
 * screens that read one redraw when it's saved.
 */
class SettingsStore(context: Context) {
    private val preferences = context.getSharedPreferences("settings", Context.MODE_PRIVATE)

    inner class Setting<T : Any>(private val key: String, private val serializer: KSerializer<T>, default: () -> T) {
        var value: T by mutableStateOf(
            preferences.getString(key, null)?.let { SettingsJson.decodeFromString(serializer, it) } ?: default(),
        )
            private set

        fun save(newValue: T) {
            preferences.edit().putString(key, SettingsJson.encodeToString(serializer, newValue)).apply()
            value = newValue
        }

        fun update(change: (T) -> T) = save(change(value))
    }

    val styling = Setting("ApplicationStyling", serializer<ApplicationStyling>()) { ApplicationStyling() }
    val hacks = Setting("ApplicationHacks", serializer<ApplicationHacks>()) { ApplicationHacks() }
    val blocking = Setting("BlockConfiguration", serializer<BlockConfiguration>()) { BlockConfiguration() }
    val ai = Setting("AIConfiguration", serializer<AIConfiguration>()) { AIConfiguration() }
    val savePaths = Setting("SavePathConfiguration", serializer<SavePathConfiguration>()) { SavePathConfiguration() }
    val landing = Setting("LandingPageConfiguration", serializer<LandingPageConfiguration>()) { LandingPageConfiguration() }
}
