package com.mrjackspade.deaddit.ui.reply

import android.net.Uri
import android.provider.OpenableColumns
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.AiPrompt
import com.mrjackspade.deaddit.core.config.ClaudeModel
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.markdown.HtmlMarkdownConverter
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.text.DotNetFormat
import com.mrjackspade.deaddit.ui.FlatButton
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TextInput
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.CommentController
import com.mrjackspade.deaddit.ui.comment.CommentModel
import com.mrjackspade.deaddit.ui.comment.CommentView
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.post.PostController
import com.mrjackspade.deaddit.ui.post.PostModel
import com.mrjackspade.deaddit.ui.post.PostRow
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

/**
 * Writing a reply, or editing the user's own comment (the C# ReplyPage). Above the editor, the
 * thread down to what's being replied to: [chain], root first. Replying, its last item is the
 * parent; editing ([toEdit] set), it's the edited comment's parent (and may be empty).
 */
class ReplyPage(
    private val services: Services,
    private val chain: List<RedditThing>,
    private val toEdit: Comment?,
    private val onSubmitted: (Comment) -> Unit,
) : Screen() {

    /** What the reply goes under; null only when editing a comment shown without its thread. */
    private val replyTo: RedditThing? = chain.lastOrNull()

    private var text by mutableStateOf(
        TextFieldValue(
            if (toEdit == null) "" else if (!toEdit.bodyHtml.isNullOrEmpty()) HtmlMarkdownConverter.convert(toEdit.bodyHtml) else toEdit.body,
        ),
    )

    private var aiLoading by mutableStateOf(false)
    private var selected by mutableStateOf<String?>(null)

    // Prompts whose model has since been removed read as None: they're never offered.
    private val availablePrompts: List<AiPrompt> = services.settings.ai.value.prompts.filter { it.model != ClaudeModel.None }
    private val showAi = services.claude.isConfigured && availablePrompts.isNotEmpty()

    private sealed interface ChainRow
    private class PostRowEntry(val model: PostModel) : ChainRow
    private class CommentRowEntry(val model: CommentModel) : ChainRow

    // A message can't be replied to here; the C# page drew nothing for one.
    private val rows: List<ChainRow> = chain.mapNotNull { thing ->
        when (thing) {
            is Post -> PostRowEntry(PostModel(thing, PostState.None))
            is Comment -> CommentRowEntry(CommentModel(thing))
            is Message -> null
        }
    }

    private val postController = PostController(services, scope, inList = false, fromHistory = false, onSelect = { _, _ -> })

    private val commentController = CommentController(
        services,
        scope,
        post = null,
        // The C# page didn't listen for these: a deleted comment stays, a reply isn't shown.
        onDeleted = { },
        onReplied = { _, _ -> },
        onLink = { url -> scope.launch { Links.open(services, url) } },
    )

    private fun leave() = services.navigator.remove(this)

    private fun launch(block: suspend () -> Unit) {
        scope.launch {
            try {
                block()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    private fun submit() = launch {
        val body = text.text
        val result = services.dialogs.busy {
            if (toEdit != null) services.reddit.update(toEdit, body) else services.reddit.comment(replyTo!!, body)
        }

        if (result != null) {
            onSubmitted(result)
            leave()
        }
    }

    /** Uploads a picked photo and puts its placeholder at the cursor. */
    private fun attachImage() = launch {
        val uri = services.activityResults.launch(
            ActivityResultContracts.PickVisualMedia(),
            PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly),
        ) ?: return@launch

        val mediaKey = services.dialogs.busy {
            val (bytes, name, type) = withContext(Dispatchers.IO) { read(uri) }
            services.reddit.uploadMedia(bytes, name, type)
        }

        val cursor = text.selection.min
        val placeholder = "![image]($mediaKey)"
        val current = text.text
        text = TextFieldValue(current.substring(0, cursor) + placeholder + current.substring(cursor), TextRange(cursor + placeholder.length))
    }

    /** The picked file's bytes, name and type ("image/png" when the picker gives none, as C#). */
    private fun read(uri: Uri): Triple<ByteArray, String, String> {
        val resolver = services.appContext.contentResolver
        val name = resolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)!!.use { cursor ->
            if (!cursor.moveToFirst()) throw IllegalStateException("The picked photo $uri has no name.")
            cursor.getString(0)
        }
        val type = resolver.getType(uri) ?: "image/png"
        val bytes = resolver.openInputStream(uri)!!.use { it.readBytes() }
        return Triple(bytes, name, type)
    }

    private fun aiAssist() = launch {
        var chosen: AiPrompt? = null
        services.dialogs.choose("Select AI Prompt", *availablePrompts.map { prompt -> Option(prompt.displayName) { chosen = prompt } }.toTypedArray())
        val prompt = chosen ?: return@launch

        var systemPrompt = prompt.textContent
        if (prompt.textContent.contains("{0}")) {
            // Cancelling gives null, which string.Format wrote as nothing.
            val additionalContext = services.dialogs.prompt("Additional Context", "Enter any additional instructions for the AI")
            systemPrompt = DotNetFormat.format(systemPrompt, additionalContext)
        }

        val history = aiHistory()

        aiLoading = true
        try {
            val reply = services.claude.sendMessage(systemPrompt, history, prompt.model.id)
            text = TextFieldValue(reply, TextRange(reply.length))
        } finally {
            aiLoading = false
        }
    }

    /** The thread down to what's being replied to, each as "**author**: text", root first. */
    private fun aiHistory(): String {
        if (replyTo == null) throw IllegalStateException("There's no thread above this comment to give the AI.")
        return chain.joinToString("") { thing ->
            when (thing) {
                is Post -> "**${thing.author ?: ""}**: ${thing.title}\n\n" + if (thing.body.isNotBlank()) thing.body + "\n\n" else ""
                else -> "**${thing.author ?: ""}**: ${thing.body}\n\n"
            }
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val hacks = services.settings.hacks.value

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).statusBarsPadding().navigationBarsPadding().imePadding()) {
            LazyColumn(Modifier.weight(1f).fillMaxWidth()) {
                for (row in rows) {
                    when (row) {
                        is PostRowEntry -> {
                            val post = row.model.post
                            item(key = post.name) {
                                PostRow(
                                    row.model,
                                    selected = false,
                                    inList = false,
                                    hideScore = hacks.hideSelfKarma && post.author.equals(services.reddit.loggedInUser, ignoreCase = true),
                                    actions = postController.actions(row.model),
                                )
                            }
                            if (post.body.isNotBlank()) {
                                item(key = "${post.name}-body") {
                                    HtmlBody(
                                        post.bodyHtml?.let(RedditHtml::parse) ?: emptyList(),
                                        styling.titleFontSize.px,
                                        onLink = { url -> scope.launch { Links.open(services, url) } },
                                        modifier = Modifier.fillMaxWidth().padding(2.dp)
                                            .border(1.dp, styling.textColor.color)
                                            .background(styling.primaryColor.color)
                                            .padding(5.dp),
                                    )
                                }
                            }
                        }

                        is CommentRowEntry -> item(key = row.model.comment.name) {
                            CommentView(
                                services,
                                row.model,
                                post = null,
                                isSelected = selected == row.model.comment.name,
                                onSelect = {
                                    selected = row.model.comment.name
                                    commentController.onSelected(row.model)
                                },
                                actions = commentController.actions(row.model),
                                modifier = Modifier.padding(5.dp),
                            )
                        }
                    }
                }
            }

            TextInput(
                text,
                { text = it },
                background = styling.primaryColor.color,
                singleLine = false,
                modifier = Modifier.weight(1f, fill = false),
            )

            Row(
                Modifier.fillMaxWidth().background(styling.secondaryColor.color).horizontalScroll(rememberScrollState()),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                FlatButton("Cancel") { leave() }
                // The C# Preview button did nothing.
                FlatButton("Preview") { }
                FlatButton("📷", description = "Attach image") { attachImage() }
                FlatButton("Submit") { submit() }
                if (showAi) {
                    FlatButton(
                        if (aiLoading) "Loading..." else "AI Assist",
                        Modifier.padding(4.dp),
                        fontSize = styling.titleFontSize.px,
                        enabled = !aiLoading,
                    ) { aiAssist() }
                    if (aiLoading) CircularProgressIndicator(Modifier.size(24.dp), color = styling.textColor.color)
                }
            }
        }
    }
}
