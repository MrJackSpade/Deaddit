package com.mrjackspade.deaddit.settings

import android.content.Context
import android.content.SharedPreferences

/**
 * Where everything the app keeps is stored: the file the C# app's MAUI Preferences used
 * (PreferenceManager's default file), with the same keys and formats, so its data is used as-is.
 */
object AppPreferences {
    fun of(context: Context): SharedPreferences =
        context.getSharedPreferences("${context.packageName}_preferences", Context.MODE_PRIVATE)
}
