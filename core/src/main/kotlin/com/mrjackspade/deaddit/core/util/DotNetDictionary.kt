package com.mrjackspade.deaddit.core.util

/**
 * A string-keyed map that enumerates in the order .NET's `Dictionary<string, TValue>` does: insertion order, except
 * that a removed entry's slot is reused by the next new key (the most recently freed slot first), as .NET's free
 * list does. Setting an existing key keeps its place and its original spelling. [keyOf] gives the key comparison:
 * two keys are the same key when [keyOf] maps them to the same string.
 */
class DotNetDictionary<V>(private val keyOf: (String) -> String) {
    private class Entry<V>(val key: String, var value: V)

    /** Slot order is enumeration order; null is a free slot. */
    private val slots = ArrayList<Entry<V>?>()
    private val freeList = ArrayDeque<Int>()
    private val indexOf = HashMap<String, Int>()

    val size: Int get() = indexOf.size

    operator fun get(key: String): V? = indexOf[keyOf(key)]?.let { slots[it]!!.value }

    /** `dictionary[key] = value`. */
    operator fun set(key: String, value: V) {
        val normalized = keyOf(key)
        val existing = indexOf[normalized]
        if (existing != null) {
            slots[existing]!!.value = value
            return
        }
        val slot = freeList.removeFirstOrNull() ?: slots.size.also { slots.add(null) }
        slots[slot] = Entry(key, value)
        indexOf[normalized] = slot
    }

    fun remove(key: String): Boolean {
        val slot = indexOf.remove(keyOf(key)) ?: return false
        slots[slot] = null
        freeList.addFirst(slot)
        return true
    }

    fun clear() {
        slots.clear()
        freeList.clear()
        indexOf.clear()
    }

    /** The entries in enumeration order. */
    fun toMap(): LinkedHashMap<String, V> {
        val map = LinkedHashMap<String, V>()
        for (entry in slots) {
            if (entry != null) map[entry.key] = entry.value
        }
        return map
    }

    companion object {
        /** Keys compared as StringComparer.OrdinalIgnoreCase does (see [DotNetStrings.ordinalIgnoreCaseUpper]). */
        fun <V> ordinalIgnoreCase(): DotNetDictionary<V> = DotNetDictionary { key ->
            val chars = CharArray(key.length)
            for (i in key.indices) chars[i] = DotNetStrings.ordinalIgnoreCaseUpper(key[i])
            String(chars)
        }
    }
}
