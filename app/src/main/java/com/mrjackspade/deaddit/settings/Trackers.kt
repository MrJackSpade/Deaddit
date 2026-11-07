package com.mrjackspade.deaddit.settings

import android.content.Context
import com.mrjackspade.deaddit.core.model.RedditThing
import kotlinx.serialization.builtins.ListSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.json.Json
import java.io.File
import java.time.LocalDate
import java.time.temporal.ChronoUnit

/**
 * Posts opened from a feed, per subreddit, so feeds can dim them. Stored as the C#
 * PreferencesVisitTracker kept them, under "PreferencesVisitTracker_<subreddit id>": one string of
 * visits, each 4 bytes of date (days since 2000-01-01, little-endian), the name, then a NUL, all as
 * ASCII chars.
 */
class VisitTracker(context: Context) {
    private val preferences = AppPreferences.of(context)
    private val loaded = HashMap<String, MutableSet<String>>()

    @Synchronized
    fun hasVisited(thing: RedditThing): Boolean {
        val subredditId = subredditIdOf(thing) ?: return false
        return visits(subredditId).contains(thing.name)
    }

    @Synchronized
    fun visit(thing: RedditThing) {
        val subredditId = subredditIdOf(thing) ?: throw IllegalArgumentException("${thing.name} has no subreddit to record its visit under.")
        if (visits(subredditId).add(thing.name)) {
            val key = KEY_PREFIX + subredditId
            val packed = preferences.getString(key, "")!! + packVisit(LocalDate.now(), thing.name)
            preferences.edit().putString(key, packed).apply()
        }
    }

    private fun visits(subredditId: String): MutableSet<String> = loaded.getOrPut(subredditId) {
        visitNames(preferences.getString(KEY_PREFIX + subredditId, "")!!).toMutableSet()
    }

    companion object {
        private const val KEY_PREFIX = "PreferencesVisitTracker_"
        private val BASE_DATE = LocalDate.of(2000, 1, 1)

        /** One visit, as the C# Serialize wrote it (bytes over 127 become '?', as .NET's ASCII encoding does). */
        internal fun packVisit(date: LocalDate, name: String): String {
            val days = ChronoUnit.DAYS.between(BASE_DATE, date).toInt()
            val dateChars = (0 until 4).map { i -> ((days shr (8 * i)) and 0xFF).let { if (it > 127) '?' else it.toChar() } }
            return dateChars.joinToString("") + name + '\u0000'
        }

        /**
         * The post names in a packed string, split exactly as the C# SplitVisits did (a NUL only ends
         * a visit after its first 9 chars, since the date bytes can contain NULs).
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

    private fun subredditIdOf(thing: RedditThing): String? = when (thing) {
        is com.mrjackspade.deaddit.core.model.Post -> thing.subredditId
        is com.mrjackspade.deaddit.core.model.Comment -> thing.subredditId
        is com.mrjackspade.deaddit.core.model.Message -> null
    }
}

/** Posts the user opened, most recent first, for the History page: a JSON list, as the C# app kept it. */
class HistoryTracker(context: Context) {
    private val preferences = AppPreferences.of(context)
    private var history: MutableList<String>? = null

    @Synchronized
    fun add(postName: String) {
        val list = loaded()
        list.remove(postName)
        list.add(0, postName)
        while (list.size > MAX_SIZE) list.removeAt(list.size - 1)
        save()
    }

    @Synchronized
    fun get(): List<String> = loaded().toList()

    @Synchronized
    fun import(names: List<String>) {
        history = names.toMutableList()
        save()
    }

    private fun loaded(): MutableList<String> = history ?: (
        preferences.getString(KEY, null)?.let { Json.decodeFromString(ListSerializer(String.serializer()), it).toMutableList() } ?: mutableListOf()
        ).also { history = it }

    private fun save() {
        preferences.edit().putString(KEY, Json.encodeToString(ListSerializer(String.serializer()), history!!)).apply()
    }

    private companion object {
        const val KEY = "PostHistory"
        const val MAX_SIZE = 1000
    }
}

/** Notes the user attaches to usernames, shown next to them. Stored as "user\ttag" lines. */
class UserTags(private val file: File) {
    private val tags = sortedMapOf<String, String>(String.CASE_INSENSITIVE_ORDER)

    init {
        if (file.exists()) {
            for (line in file.readLines()) {
                val separator = line.indexOf('\t')
                if (separator > 0) tags[line.substring(0, separator)] = line.substring(separator + 1)
            }
        }
    }

    @Synchronized
    fun get(username: String): String? = tags[username]

    @Synchronized
    fun set(username: String, tag: String) {
        tags[username] = tag
        flush()
    }

    @Synchronized
    fun remove(username: String) {
        tags.remove(username)
        flush()
    }

    @Synchronized
    fun all(): Map<String, String> = tags.toMap()

    @Synchronized
    fun import(imported: Map<String, String>) {
        tags.clear()
        tags.putAll(imported)
        flush()
    }

    private fun flush() {
        file.parentFile?.mkdirs()
        file.writeText(tags.entries.joinToString("") { "${it.key}\t${it.value}\n" })
    }
}
