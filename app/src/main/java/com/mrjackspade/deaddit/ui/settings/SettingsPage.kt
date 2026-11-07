package com.mrjackspade.deaddit.ui.settings

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack

import android.content.Intent
import android.net.Uri
import android.content.Context
import androidx.activity.result.contract.ActivityResultContract
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.BuildConfig
import com.mrjackspade.deaddit.KeepAliveService
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.ExportedSession
import com.mrjackspade.deaddit.core.config.SavePathConfiguration
import com.mrjackspade.deaddit.core.config.SettingsExportData
import com.mrjackspade.deaddit.core.config.SettingsJson
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.MauiButton
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.editor.EditorConfiguration
import com.mrjackspade.deaddit.ui.editor.EditorPage
import com.mrjackspade.deaddit.ui.editor.Schemas
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.serialization.ExperimentalSerializationApi
import kotlinx.serialization.builtins.nullable
import kotlinx.serialization.json.Json

/**
 * Settings (the C# SettingsPage): the configuration editor, the default save folders, and
 * exporting or importing everything as the C# app's "deaddit_settings.json".
 */
class SettingsPage(private val services: Services) : Screen() {

    private fun launch(block: suspend () -> Unit) {
        scope.launch {
            try {
                block()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    /** "Edit Configuration": the four configurations together; saving also starts or stops Keep Alive. */
    private fun editConfiguration() {
        val settings = services.settings
        val current = EditorConfiguration(settings.ai.value, settings.hacks.value, settings.blocking.value, settings.styling.value)

        EditorPage.open(services, Schemas.configuration, current) { edited ->
            settings.hacks.save(edited.hacks)
            settings.blocking.save(edited.blocking)
            settings.styling.save(edited.styling)
            settings.ai.save(edited.ai)

            if (edited.hacks.keepAlive) KeepAliveService.start(services.appContext) else KeepAliveService.stop(services.appContext)
        }
    }

    /** A folder from the system picker, kept usable across restarts. */
    private fun pickFolderInto(assign: (SavePathConfiguration, String) -> SavePathConfiguration) = launch {
        val tree = services.activityResults.launch(ActivityResultContracts.OpenDocumentTree(), null) ?: return@launch
        services.appContext.contentResolver.takePersistableUriPermission(
            tree,
            Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_GRANT_WRITE_URI_PERMISSION,
        )
        services.settings.savePaths.update { assign(it, tree.toString()) }
    }

    private fun export() = launch {
        val data = SettingsExportData(
            styling = services.settings.styling.value,
            aiConfiguration = services.settings.ai.value,
            blockConfiguration = services.settings.blocking.value,
            applicationHacks = services.settings.hacks.value,
            savePaths = services.settings.savePaths.value,
            landingPageConfiguration = services.settings.landing.value,
            postHistory = services.history.get(),
            bearerTokenJson = services.sessions.load()?.let { SettingsJson.encodeToString(ExportedSession.serializer(), ExportedSession.of(it)) },
            userTags = services.userTags.all(),
        )

        val json = exportJson.encodeToString(SettingsExportData.serializer(), data)
        val target = services.activityResults.launch(ActivityResultContracts.CreateDocument("application/json"), "deaddit_settings.json") ?: return@launch
        write(target, json)
    }

    private fun import() = launch {
        val confirmed = services.dialogs.confirm(
            "Import Settings",
            "This will overwrite all current settings. The app will need to restart afterward.",
            "Continue",
            "Cancel",
        )
        if (!confirmed) return@launch

        val file = services.activityResults.launch(OpenDocumentTitled("Select settings file"), arrayOf("*/*")) ?: return@launch
        val json = read(file)

        val data = SettingsJson.decodeFromString(SettingsExportData.serializer().nullable, json)
        if (data == null) {
            services.dialogs.message("Failed to read settings file.")
            return@launch
        }

        services.settings.styling.save(data.styling)
        services.settings.ai.save(data.aiConfiguration)
        services.settings.blocking.save(data.blockConfiguration)
        services.settings.hacks.save(data.applicationHacks)
        services.settings.savePaths.save(data.savePaths)
        services.settings.landing.save(data.landingPageConfiguration)

        services.history.import(data.postHistory)

        data.bearerTokenJson?.let { token ->
            services.sessions.save(SettingsJson.decodeFromString(ExportedSession.serializer(), token).toSession())
        }

        services.userTags.import(data.userTags)

        services.dialogs.alert("Success", "Settings imported. Please restart the app for all changes to take effect.", "OK")
    }

    private suspend fun write(target: Uri, text: String) = withContext(Dispatchers.IO) {
        val stream = services.appContext.contentResolver.openOutputStream(target, "wt")
            ?: throw IllegalStateException("Couldn't open $target to write.")
        stream.use { it.write(text.toByteArray(Charsets.UTF_8)) }
    }

    private suspend fun read(source: Uri): String = withContext(Dispatchers.IO) {
        val stream = services.appContext.contentResolver.openInputStream(source)
            ?: throw IllegalStateException("Couldn't open $source to read.")
        stream.use { it.readBytes().toString(Charsets.UTF_8) }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val savePaths = services.settings.savePaths.value

        Column(Modifier.fillMaxSize().background(styling.tertiaryColor.color)) {
            TopBar {
                TopBarButton(Icons.AutoMirrored.Filled.ArrowBack, "Back") { services.navigator.remove(this@SettingsPage) }
                Text("Settings", Modifier.weight(1f), color = styling.textColor.color, fontSize = 14.sp, textAlign = TextAlign.Center)
            }

            Column(
                Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState()).navigationBarsPadding().padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                Text("Version ${BuildConfig.VERSION_NAME}", Modifier.fillMaxWidth(), color = styling.subTextColor.color, fontSize = 14.sp, textAlign = TextAlign.Center)

                SettingsButton("Edit Configuration", Modifier.fillMaxWidth()) { editConfiguration() }

                FolderSetting("Default Image Folder", savePaths.defaultImageDirectory,
                    onBrowse = { pickFolderInto { paths, folder -> paths.copy(defaultImageDirectory = folder) } },
                    onClear = { services.settings.savePaths.update { it.copy(defaultImageDirectory = null) } },
                )

                FolderSetting("Default Video Folder", savePaths.defaultVideoDirectory,
                    onBrowse = { pickFolderInto { paths, folder -> paths.copy(defaultVideoDirectory = folder) } },
                    onClear = { services.settings.savePaths.update { it.copy(defaultVideoDirectory = null) } },
                )

                SettingsButton("Export Settings", Modifier.fillMaxWidth()) { export() }
                SettingsButton("Import Settings", Modifier.fillMaxWidth()) { import() }
            }
        }
    }

    @Composable
    private fun FolderSetting(heading: String, folder: String?, onBrowse: () -> Unit, onClear: () -> Unit) {
        val styling = LocalStyling.current
        Text(heading, color = styling.textColor.color, fontSize = 14.sp)
        Text(
            if (folder.isNullOrBlank()) "(use system save dialog)" else folder,
            color = styling.subTextColor.color,
            fontSize = 12.sp,
            maxLines = 1,
            overflow = TextOverflow.MiddleEllipsis,
        )
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            SettingsButton("Browse...", description = "Browse for the ${heading.lowercase()}") { onBrowse() }
            SettingsButton("Clear", description = "Clear the ${heading.lowercase()}") { onClear() }
        }
    }

    /** A MAUI Button in the secondary color, as the C# page colored them. */
    @Composable
    private fun SettingsButton(text: String, modifier: Modifier = Modifier, description: String? = null, onClick: () -> Unit) {
        MauiButton(text, LocalStyling.current.secondaryColor.color, modifier, description = description, onClick = onClick)
    }

    /**
     * The system's document picker inside a chooser titled [title], as MAUI's FilePicker opened
     * it (Intent.createChooser with its PickerTitle).
     */
    private class OpenDocumentTitled(private val title: String) : ActivityResultContract<Array<String>, Uri?>() {
        private val open = ActivityResultContracts.OpenDocument()

        override fun createIntent(context: Context, input: Array<String>): Intent =
            Intent.createChooser(open.createIntent(context, input), title)

        override fun parseResult(resultCode: Int, intent: Intent?): Uri? = open.parseResult(resultCode, intent)
    }

    private companion object {
        /** As the C# export: indented two spaces, enums by name. */
        @OptIn(ExperimentalSerializationApi::class)
        val exportJson = Json(SettingsJson) {
            prettyPrint = true
            prettyPrintIndent = "  "
        }
    }
}
