package com.mrjackspade.deaddit.core.markdown

/** Inspects markdown for features that affect how it must be submitted to Reddit. */
object MarkdownAnalyzer {
    /**
     * True if any line in the markdown starts at blockquote depth 2 or greater
     * (i.e. begins with "> > "). Reddit's richtext endpoint mishandles nested
     * blockquotes — callers should route these through the markdown endpoint.
     */
    fun hasNestedBlockquote(markdown: String?): Boolean {
        if (markdown.isNullOrEmpty()) {
            return false
        }

        for (line in markdown.split('\n')) {
            var i = 0
            var depth = 0

            while (i + 1 < line.length && line[i] == '>' && line[i + 1] == ' ') {
                depth++

                if (depth >= 2) {
                    return true
                }

                i += 2
            }
        }

        return false
    }

    /**
     * True if the markdown contains an "![image](key)" placeholder. These are
     * only meaningful on Reddit's richtext endpoint; the markdown endpoint
     * cannot attach images.
     */
    fun containsImagePlaceholder(markdown: String?): Boolean =
        !markdown.isNullOrEmpty() && markdown.contains("![image](")
}
