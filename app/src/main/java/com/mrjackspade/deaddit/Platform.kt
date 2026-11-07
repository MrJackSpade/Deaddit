package com.mrjackspade.deaddit

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import java.io.File

/** Android's share sheet and clipboard, from anywhere. */
class Platform(private val context: Context) {
    /**
     * Shares text, as MAUI's Android Share did a ShareTextRequest with Text (or Uri) and Title: [title] is only the
     * share sheet's title (the C# never set a Subject, so no EXTRA_SUBJECT), and blank text is sent as "".
     */
    fun shareText(text: String?, title: String?) {
        val send = Intent(Intent.ACTION_SEND).apply {
            type = "text/plain"
            putExtra(Intent.EXTRA_TEXT, if (text.isNullOrBlank()) "" else text)
        }
        context.startActivity(
            Intent.createChooser(send, title ?: "").setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_NEW_TASK),
        )
    }

    /** Shares files from the app's cache, through its FileProvider. */
    fun shareFiles(files: List<File>, mimeType: String, title: String?) {
        val uris = ArrayList(files.map { FileProvider.getUriForFile(context, "${context.packageName}.files", it) })
        val send = if (uris.size == 1) {
            Intent(Intent.ACTION_SEND).putExtra(Intent.EXTRA_STREAM, uris[0])
        } else {
            Intent(Intent.ACTION_SEND_MULTIPLE).putParcelableArrayListExtra(Intent.EXTRA_STREAM, uris)
        }
        send.type = mimeType
        send.putExtra(Intent.EXTRA_SUBJECT, title)
        send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        context.startActivity(Intent.createChooser(send, title).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_GRANT_READ_URI_PERMISSION))
    }

    fun copy(text: String) {
        val clipboard = context.getSystemService(ClipboardManager::class.java)
        clipboard.setPrimaryClip(ClipData.newPlainText("Deaddit", text))
    }

    /** Opens a link in the device's browser or the app that handles it. */
    fun openExternally(url: String) {
        context.startActivity(Intent(Intent.ACTION_VIEW, android.net.Uri.parse(url)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
    }
}
