package com.mrjackspade.deaddit.core.util

import com.mrjackspade.deaddit.core.text.DotNetText

object UrlHelper {
    // The C# sets compare with StringComparer.OrdinalIgnoreCase; with ASCII entries that is ASCII case folding.
    private val hiddenHosts = setOf("i.redd.it", "v.redd.it", "www.reddit.com")

    private val imageExtensions = setOf(".jpg", ".jpeg", ".png", ".webp", ".bmp", ".heic", ".heif", ".gif")

    private val videoExtensions = setOf(".mp4", ".webm", ".mov", ".mkv", ".m4v", ".avi", ".gifv")

    private const val DEFAULT_MIME_TYPE = "application/octet-stream"

    private const val MIME_TYPES_RESOURCE = "/com/mrjackspade/deaddit/core/util/mimetypes.tsv"

    /**
     * MimeKit 4.15.1's MimeTypes table (extension to MIME type, looked up ignoring ASCII case), which the C#
     * GetMimeTypeFromUri used.
     */
    private val mimeTypes: Map<String, String> by lazy {
        val text = (UrlHelper::class.java.getResourceAsStream(MIME_TYPES_RESOURCE)
            ?: throw IllegalStateException("Resource '$MIME_TYPES_RESOURCE' not found"))
            .use { it.readBytes().toString(Charsets.UTF_8) }

        val map = HashMap<String, String>()

        for (line in text.lineSequence().filter { it.isNotEmpty() }) {
            val parts = line.split('\t')
            check(parts.size == 2) { "Malformed line in '$MIME_TYPES_RESOURCE': $line" }
            check(parts[0].all { it.code in 0x21..0x7E }) { "Non-ASCII extension in '$MIME_TYPES_RESOURCE': $line" }
            check(map.put(DotNetText.asciiFold(parts[0]), parts[1]) == null) { "Duplicate extension in '$MIME_TYPES_RESOURCE': $line" }
        }

        map
    }

    fun isImageFile(fileName: String): Boolean = DotNetText.asciiFold(getExtension(fileName)) in imageExtensions

    fun isVideoFile(fileName: String): Boolean = DotNetText.asciiFold(getExtension(fileName)) in videoExtensions

    /**
     * The host to show beside a link post, or null when there is none worth showing: not a web URL (relative
     * reddit paths parse as host-less file URIs on Unix), or a host reddit itself serves.
     *
     * Parses exactly as .NET's `Uri.TryCreate(url, UriKind.Absolute, ...)` and `Uri.Host` did.
     */
    fun getDisplayHost(url: String?): String? {
        val uri = DotNetUri.tryCreate(url) ?: return null

        if (uri.scheme != "http" && uri.scheme != "https") {
            return null
        }

        val host = uri.host

        if (host.isEmpty() || DotNetText.asciiFold(host) in hiddenHosts) {
            return null
        }

        return host
    }

    /** The extension of [fileName] (a path or URL) including the dot, ignoring any query string; "" when none. */
    fun getExtension(fileName: String): String {
        if (fileName.isEmpty()) {
            return ""
        }

        return pathGetExtension(stripQuery(fileName))
    }

    /** The file name of [fileName] (a path or URL), ignoring any query string. */
    fun getFileName(fileName: String): String {
        if (fileName.isEmpty()) {
            return ""
        }

        val path = stripQuery(fileName)

        return path.substring(path.lastIndexOf('/') + 1)
    }

    /**
     * The MIME type for the file a URL points at, from its path's extension; application/octet-stream when there is
     * no extension or it is unknown. Throws [UriFormatException] when .NET's `new Uri(s)` would have.
     */
    fun getMimeTypeFromUri(s: String): String {
        val uri = DotNetUri.create(s)

        // Strip the parameters from the URI if they exist
        val uriWithoutParams = DotNetUri.create(uri.getLeftPartPath())

        // Get the file extension from the URI
        val fileExtension = pathGetExtension(uriWithoutParams.absolutePath)

        if (fileExtension.isEmpty()) {
            return DEFAULT_MIME_TYPE // Default to a generic binary MIME type
        }

        // MimeTypes.GetMimeType(fileName) takes Path.GetExtension of its argument again.
        return mimeTypes[DotNetText.asciiFold(pathGetExtension(fileExtension))] ?: DEFAULT_MIME_TYPE
    }

    private fun stripQuery(fileName: String): String =
        if (fileName.contains('?')) fileName.substring(0, fileName.lastIndexOf('?')) else fileName

    // .NET's Path.GetExtension on Unix (Android): '/' is the only separator; a trailing '.' is no extension.
    private fun pathGetExtension(path: String): String {
        for (i in path.length - 1 downTo 0) {
            val ch = path[i]

            if (ch == '.') {
                return if (i != path.length - 1) path.substring(i) else ""
            }

            if (ch == '/') {
                break
            }
        }

        return ""
    }
}
