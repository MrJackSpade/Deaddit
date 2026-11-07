package com.mrjackspade.deaddit.media

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.provider.DocumentsContract
import android.webkit.MimeTypeMap
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import com.mrjackspade.deaddit.Platform
import com.mrjackspade.deaddit.core.config.SavePathConfiguration
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.settings.SettingsStore
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import java.io.File
import java.util.UUID

/**
 * Saving and sharing downloaded media (the C# FileStorage and IShareExtensions.ShareFiles).
 *
 * Saving goes to the user's save folder for the file's kind (SavePathConfiguration): images to
 * DefaultImageDirectory, videos to DefaultVideoDirectory. A folder is either a filesystem path,
 * as the MAUI app's folder picker stored (written to directly, as the MAUI app did), or a
 * `content://` document tree URI from Android's folder picker (written through it). With no
 * folder for the kind, the system "save as" dialog asks where, as the MAUI app's FileSaver did.
 */
class MediaService(
    private val context: Context,
    http: OkHttpClient,
    private val settings: SettingsStore,
    private val platform: Platform,
    private val activityResults: ActivityResults,
) {
    val fetcher = MediaFetcher(http, File(context.cacheDir, "media-temp"))

    /** The GIF to MP4 converter when the "Convert GIFs to MP4" hack is on (the C# `ConvertGifsToMp4 ? new GifToMp4Converter() : null`). */
    fun gifConverter(): StreamConverter? = if (settings.hacks.value.convertGifsToMp4) GifToMp4Converter() else null

    /** Downloads the files (converting those [converter] takes) and shares them. */
    suspend fun shareDownloads(title: String?, downloads: List<FileDownload>, converter: StreamConverter? = null) {
        val shareDir = File(context.cacheDir, SHARE_DIR)
        val files = downloads.map { item ->
            val (fileName, temp) = fetch(item, converter)
            val file = File(shareDir, fileName)
            withContext(Dispatchers.IO) {
                shareDir.mkdirs()
                temp.copyTo(file, overwrite = true)
                temp.delete()
            }
            file
        }

        // One type when the files share it, as MAUI's ShareMultipleFilesRequest sent; any type otherwise.
        val types = files.map { mimeTypeOf(it.name) }.distinct()
        platform.shareFiles(files, types.singleOrNull() ?: "*/*", title)
    }

    /** Downloads the files (converting those [converter] takes) into the user's save folders. */
    suspend fun save(downloads: List<FileDownload>, converter: StreamConverter? = null) {
        require(downloads.isNotEmpty()) { "Nothing to save." }
        val savePaths = settings.savePaths.value

        for (item in downloads) {
            val fileName = if (converter != null && converter.canConvert(item.fileName)) converter.convertFileName(item.fileName) else item.fileName
            val targetDirectory = resolveTargetDirectory(fileName, savePaths)

            val (_, temp) = fetch(item, converter)
            try {
                if (targetDirectory != null) {
                    writeToDirectory(targetDirectory, fileName, temp)
                } else {
                    saveWithDialog(fileName, temp)
                }
            } finally {
                temp.delete()
            }
        }
    }

    /** The item's file name after conversion, and a temp file holding it (fetched, then converted). */
    private suspend fun fetch(item: FileDownload, converter: StreamConverter?): Pair<String, File> = withContext(Dispatchers.IO) {
        val tempDir = File(context.cacheDir, "media-temp").apply { mkdirs() }
        val downloaded = File(tempDir, UUID.randomUUID().toString())
        fetcher.fetch(item.downloadUrl, downloaded)

        if (converter != null && converter.canConvert(item.fileName)) {
            val converted = File(tempDir, UUID.randomUUID().toString())
            try {
                converter.convert(downloaded, converted)
            } finally {
                downloaded.delete()
            }
            converter.convertFileName(item.fileName) to converted
        } else {
            item.fileName to downloaded
        }
    }

    private fun resolveTargetDirectory(fileName: String, savePaths: SavePathConfiguration): String? {
        if (UrlHelper.isImageFile(fileName) && !savePaths.defaultImageDirectory.isNullOrBlank()) return savePaths.defaultImageDirectory
        if (UrlHelper.isVideoFile(fileName) && !savePaths.defaultVideoDirectory.isNullOrBlank()) return savePaths.defaultVideoDirectory
        return null
    }

    /** Writes [source] into [directory] as [fileName], replacing a file of that name (File.Create in the C#). */
    private suspend fun writeToDirectory(directory: String, fileName: String, source: File) {
        when {
            directory.startsWith("content://") -> withContext(Dispatchers.IO) { writeToTree(Uri.parse(directory), fileName, source) }

            directory.startsWith("/") -> {
                ensureLegacyStoragePermission()
                withContext(Dispatchers.IO) {
                    val dir = File(directory)
                    dir.mkdirs()
                    source.copyTo(File(dir, fileName), overwrite = true)
                }
            }

            else -> throw IllegalArgumentException("The save folder '$directory' is neither a path nor a content:// folder.")
        }
    }

    private fun writeToTree(tree: Uri, fileName: String, source: File) {
        val resolver = context.contentResolver
        val parent = DocumentsContract.buildDocumentUriUsingTree(tree, DocumentsContract.getTreeDocumentId(tree))

        val existing = resolver.query(
            DocumentsContract.buildChildDocumentsUriUsingTree(tree, DocumentsContract.getDocumentId(parent)),
            arrayOf(DocumentsContract.Document.COLUMN_DOCUMENT_ID, DocumentsContract.Document.COLUMN_DISPLAY_NAME),
            null, null, null,
        )!!.use { cursor ->
            var found: Uri? = null
            while (cursor.moveToNext()) {
                if (cursor.getString(1) == fileName) {
                    found = DocumentsContract.buildDocumentUriUsingTree(tree, cursor.getString(0))
                    break
                }
            }
            found
        }

        val target = existing
            ?: DocumentsContract.createDocument(resolver, parent, mimeTypeOf(fileName), fileName)
            ?: throw IllegalStateException("Couldn't create $fileName in $tree")

        resolver.openOutputStream(target, "wt")!!.use { output -> source.inputStream().use { it.copyTo(output) } }
    }

    /** Asks where to save, with Android's "save as" dialog. Cancelling it skips the file. */
    private suspend fun saveWithDialog(fileName: String, source: File) {
        val target = activityResults.launch(ActivityResultContracts.CreateDocument(mimeTypeOf(fileName)), fileName) ?: return
        withContext(Dispatchers.IO) {
            context.contentResolver.openOutputStream(target, "wt")!!.use { output -> source.inputStream().use { it.copyTo(output) } }
        }
    }

    /** Writing to shared storage by path needs the storage permission up to Android 10. */
    private suspend fun ensureLegacyStoragePermission() {
        if (Build.VERSION.SDK_INT > Build.VERSION_CODES.Q) return
        val permission = Manifest.permission.WRITE_EXTERNAL_STORAGE
        if (ContextCompat.checkSelfPermission(context, permission) == PackageManager.PERMISSION_GRANTED) return
        if (!activityResults.launch(ActivityResultContracts.RequestPermission(), permission)) {
            throw SecurityException("Saving to the folder needs the storage permission, which was refused.")
        }
    }

    private fun mimeTypeOf(fileName: String): String {
        val extension = fileName.substringAfterLast('.', "").lowercase()
        return MimeTypeMap.getSingleton().getMimeTypeFromExtension(extension) ?: "application/octet-stream"
    }

    companion object {
        /** Where shared files are written; the FileProvider (res/xml/file_paths.xml) serves this folder. */
        const val SHARE_DIR = "share"
    }
}
