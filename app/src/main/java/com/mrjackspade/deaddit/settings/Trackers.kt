package com.mrjackspade.deaddit.settings

import android.content.Context
import com.mrjackspade.deaddit.core.model.RedditThing
import kotlinx.serialization.builtins.ListSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.json.Json
import java.io.File

/** Posts opened from a feed, per subreddit, so feeds can dim them. */
class VisitTracker(context: Context) {
    private val preferences = context.getSharedPreferences("visits", Context.MODE_PRIVATE)
    private val loaded = HashMap<String, MutableSet<String>>()

    @Synchronized
    fun hasVisited(thing: RedditThing): Boolean {
        val subredditId = subredditIdOf(thing) ?: return false
        return visits(subredditId).contains(thing.name)
    }

    @Synchronized
    fun visit(thing: RedditThing) {
        val subredditId = subredditIdOf(thing) ?: throw IllegalArgumentException("${thing.name} has no subreddit to record its visit under.")
        val visits = visits(subredditId)
        if (visits.add(thing.name)) {
            preferences.edit().putString(subredditId, visits.joinToString("\n")).apply()
        }
    }

    private fun visits(subredditId: String): MutableSet<String> = loaded.getOrPut(subredditId) {
        preferences.getString(subredditId, null)?.split('\n')?.filter { it.isNotEmpty() }?.toMutableSet() ?: LinkedHashSet()
    }

    private fun subredditIdOf(thing: RedditThing): String? = when (thing) {
        is com.mrjackspade.deaddit.core.model.Post -> thing.subredditId
        is com.mrjackspade.deaddit.core.model.Comment -> thing.subredditId
        is com.mrjackspade.deaddit.core.model.Message -> null
    }
}

/** Posts the user opened, most recent first, for the History page. */
class HistoryTracker(context: Context) {
    private val preferences = context.getSharedPreferences("history", Context.MODE_PRIVATE)
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
