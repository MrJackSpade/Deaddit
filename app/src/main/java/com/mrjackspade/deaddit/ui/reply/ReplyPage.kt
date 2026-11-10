package com.mrjackspade.deaddit.ui.reply

import android.net.Uri
import android.provider.OpenableColumns
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.heightIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Send
import androidx.compose.material.icons.outlined.AutoAwesome
import androidx.compose.material.icons.outlined.ChatBubbleOutline
import androidx.compose.material.icons.outlined.Close
import androidx.compose.material.icons.outlined.ExpandLess
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Image
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextOverflow
import com.mrjackspade.deaddit.ui.AccentAction
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.TopBarTitle
import com.mrjackspade.deaddit.ui.accentEdge
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
import androidx.compose.ui.graphics.Color
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
import com.mrjackspade.deaddit.ui.MauiButton
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
    // This page's own, from the AI settings as it opens (the C# page's transient ClaudeService).
    private val claude = services.claude()
    private val showAi = claude.isConfigured && availablePrompts.isNotEmpty()

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

    private val postController = PostController(services, inList = false, fromHistory = false, onSelect = { _, _ -> })

    private val commentController = CommentController(
        services,
        post = null,
        // The C# page didn't listen for these: a deleted comment stays, a reply isn't shown.
        onDeleted = { },
        onReplied = { _, _ -> },
        onLink = { url -> services.launchAction { Links.open(services, url) } },
    )

    private fun leave() = services.navigator.remove(this)

    private fun launch(block: suspend () -> Unit) {
        scope.launch {
            try {
                block()
            } catch (e: Throwable) {
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
        services.dialogs.choose(
            "Select AI Prompt",
            *availablePrompts.map { prompt -> Option(prompt.displayName) { chosen = prompt } }.toTypedArray(),
            cancel = "Cancel",
        )
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
            val reply = claude.sendMessage(systemPrompt, history, prompt.model.id)
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

    /** Whether the thread above shows in full; collapsed, it's one line, leaving the editor the room. */
    private var threadShown by mutableStateOf(true)

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).navigationBarsPadding().imePadding()) {
            TopBar {
                TopBarButton(Icons.Outlined.Close, "Cancel", color = styling.textColor.color) { leave() }
                TopBarTitle(if (toEdit != null) "Edit" else "Reply")
                AccentAction(Icons.AutoMirrored.Outlined.Send, "Submit") { submit() }
            }

            if (rows.isNotEmpty()) {
                Card(Modifier.padding(horizontal = 8.dp).let { if (threadShown) it.weight(1f) else it }) {
                    Row(
                        Modifier.fillMaxWidth().heightIn(min = 44.dp).clickable(role = Role.Button) { threadShown = !threadShown }
                            .semantics { contentDescription = if (threadShown) "Hide thread" else "Show thread" }
                            .padding(start = 12.dp, end = 8.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        Icon(Icons.Outlined.ChatBubbleOutline, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(18.dp))
                        Text(if (threadShown) "Thread" else preview(), Modifier.weight(1f), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px, maxLines = 1, overflow = TextOverflow.Ellipsis)
                        Icon(if (threadShown) Icons.Outlined.ExpandLess else Icons.Outlined.ExpandMore, contentDescription = null, tint = styling.subTextColor.color)
                    }
                    if (threadShown) {
                        Divider()
                        Thread(Modifier.weight(1f))
                    }
                }
            }

            Column(
                Modifier.padding(start = 8.dp, end = 8.dp, top = 6.dp).let { if (!threadShown || rows.isEmpty()) it.weight(1f) else it },
            ) {
                // The box grows as it's typed into; with the thread collapsed it has the page.
                TextInput(
                    text,
                    { text = it },
                    background = styling.highlightColor.color,
                    border = styling.accentEdge,
                    singleLine = false,
                    placeholder = if (toEdit != null) "Edit" else "Reply",
                    modifier = Modifier.heightIn(min = 140.dp).let { if (!threadShown || rows.isEmpty()) it.weight(1f) else it },
                )

                Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                    IconAction(Icons.Outlined.Image, "Attach image") { attachImage() }
                    if (showAi) {
                        if (aiLoading) {
                            Box(Modifier.size(40.dp), contentAlignment = Alignment.Center) {
                                CircularProgressIndicator(Modifier.size(22.dp), color = styling.accentColor.color, strokeWidth = 2.dp)
                            }
                        } else {
                            IconAction(Icons.Outlined.AutoAwesome, "AI assist") { aiAssist() }
                        }
                    }
                }
            }
        }
    }

    /** The collapsed thread's one line: what's being replied to. */
    private fun preview(): String = when (val last = chain.lastOrNull()) {
        is Post -> last.title
        null -> ""
        else -> "${last.author ?: ""}: ${last.body.replace('\n', ' ')}"
    }

    @Composable
    private fun Thread(modifier: Modifier) {
        val styling = LocalStyling.current
        val hacks = services.settings.hacks.value

        LazyColumn(modifier.fillMaxWidth()) {
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
                                        styling.commentFontSize.px,
                                        onLink = { url -> services.launchAction { Links.open(services, url) } },
                                        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 4.dp),
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
                                modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
                            )
                        }
                    }
                }
        }
    }
}
