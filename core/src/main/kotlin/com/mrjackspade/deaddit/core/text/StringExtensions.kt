package com.mrjackspade.deaddit.core.text

// The C# TryTrim(out string? trimmed) methods return the trimmed string here, or null where C# returned false.

/** [this] without its first and last char when it starts and ends with [start]; otherwise null. */
fun String.tryTrim(start: Char): String? = tryTrim(start, start)

/** [this] without its first and last char when it starts with [start] and ends with [end]; otherwise null. */
fun String.tryTrim(start: Char, end: Char): String? {
    if (length < 2) {
        return null
    }

    if (this[0] != start) {
        return null
    }

    if (this[length - 1] != end) {
        return null
    }

    return substring(1, length - 1)
}

/** [this] without [start] at both ends when it starts and ends with [start]; otherwise null. */
fun String.tryTrim(start: String): String? = tryTrim(start, start)

/**
 * [this] without the leading [start] and trailing [end] when it has both; otherwise null.
 *
 * Compares ordinally. C#'s StartsWith(string)/EndsWith(string) used the current culture, which ignores zero-width
 * and other ignorable chars; that only differs for such chars, and then the C# sliced by ordinal lengths anyway.
 */
fun String.tryTrim(start: String, end: String): String? {
    if (length < start.length + end.length) {
        return null
    }

    if (!startsWith(start)) {
        return null
    }

    if (!endsWith(end)) {
        return null
    }

    return substring(start.length, length - end.length)
}
