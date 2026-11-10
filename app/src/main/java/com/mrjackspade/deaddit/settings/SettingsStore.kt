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
import com.mrjackspade.deaddit.core.config.Lantern
import com.mrjackspade.deaddit.core.config.SavePathConfiguration
import com.mrjackspade.deaddit.core.config.SettingsJson
import com.mrjackspade.deaddit.core.util.DotNetStrings
import kotlinx.serialization.KSerializer
import kotlinx.serialization.serializer

/**
 * The app's settings, each stored as JSON under its C# type's full name, as the C# app kept them.
 * Every value is Compose state, so screens that read one redraw when it's saved.
 */
class SettingsStore(context: Context) {
    private val preferences = AppPreferences.of(context)

    inner class Setting<T : Any>(private val key: String, private val serializer: KSerializer<T>, default: () -> T) {
        /** As the C# PreferencesConfigurationService.Read: a missing, empty or whitespace value is the defaults. */
        var value: T by mutableStateOf(
            preferences.getString(key, null)
                ?.takeUnless(DotNetStrings::isNullOrWhiteSpace)
                ?.let { SettingsJson.decodeFromString(serializer, it) }
                ?: default(),
        )
            private set

        fun save(newValue: T) {
            preferences.edit().putString(key, SettingsJson.encodeToString(serializer, newValue)).apply()
            value = newValue
        }

        fun update(change: (T) -> T) = save(change(value))
    }

    val styling = Setting("Deaddit.Core.Configurations.Models.ApplicationStyling", serializer<ApplicationStyling>()) { ApplicationStyling() }
    val hacks = Setting("Deaddit.Core.Configurations.Models.ApplicationHacks", serializer<ApplicationHacks>()) { ApplicationHacks() }
    val blocking = Setting("Deaddit.Core.Configurations.Models.BlockConfiguration", serializer<BlockConfiguration>()) { BlockConfiguration() }
    val ai = Setting("Deaddit.Configurations.AIConfiguration", serializer<AIConfiguration>()) { AIConfiguration() }
    val savePaths = Setting("Deaddit.Core.Configurations.Models.SavePathConfiguration", serializer<SavePathConfiguration>()) { SavePathConfiguration() }
    val landing = Setting("Deaddit.Core.Configurations.Models.LandingPageConfiguration", serializer<LandingPageConfiguration>()) { LandingPageConfiguration() }

    init {
        // A styling saved under older defaults gets the current ones, once (the owner's call when
        // the Lantern design replaced the old look): from then on, what the user sets stays.
        // The styling keeps its key and format; only its values are replaced.
        if (preferences.getInt(STYLING_VERSION_KEY, 1) < Lantern.STYLING_VERSION) {
            styling.save(ApplicationStyling())
            preferences.edit().putInt(STYLING_VERSION_KEY, Lantern.STYLING_VERSION).apply()
        }
    }

    private companion object {
        const val STYLING_VERSION_KEY = "Deaddit.StylingVersion"
    }
}
