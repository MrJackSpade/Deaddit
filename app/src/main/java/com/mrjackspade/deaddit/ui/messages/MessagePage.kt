package com.mrjackspade.deaddit.ui.messages

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.TextFieldValue
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.ui.FlatButton
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TextInput
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.message.MessageView
import com.mrjackspade.deaddit.ui.user.UserHeader
import kotlinx.coroutines.launch

/** Writing a private message to [user], under the message it replies to (the C# MessagePage). */
class MessagePage(private val services: Services, private val user: User, private val replyTo: Message?) : Screen() {
    private var subject by mutableStateOf(TextFieldValue(""))
    private var body by mutableStateOf(TextFieldValue(""))

    private fun leave() = services.navigator.remove(this)

    private fun submit() {
        scope.launch {
            try {
                services.reddit.message(user.name, subject.text, body.text)
                services.dialogs.message("Message sent")
                leave()
            } catch (e: Exception) {
                services.dialogs.error(e)
            }
        }
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).statusBarsPadding().navigationBarsPadding().imePadding()) {
            Column(Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState())) {
                UserHeader(services, user, showMessage = false)
                // Shown, not selectable, as the C# page added it without a selection group.
                if (replyTo != null) MessageView(services, replyTo, isSelected = false, onSelect = { }, selectable = false)
            }

            TextInput(subject, { subject = it }, background = styling.primaryColor.color, placeholder = "Subject")
            TextInput(body, { body = it }, background = styling.primaryColor.color, placeholder = "Body", singleLine = false)

            Row(Modifier.fillMaxWidth().background(styling.secondaryColor.color)) {
                FlatButton("Cancel") { leave() }
                // The C# Preview button did nothing.
                FlatButton("Preview") { }
                FlatButton("Submit") { submit() }
            }
        }
    }
}
