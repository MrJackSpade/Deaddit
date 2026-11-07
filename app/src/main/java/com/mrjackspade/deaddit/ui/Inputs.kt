package com.mrjackspade.deaddit.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/**
 * A MAUI Entry/Editor: text on [background], with [placeholder] while empty. Single-line unless
 * [minLines] or [singleLine] say otherwise.
 */
@Composable
fun TextInput(
    value: TextFieldValue,
    onValueChange: (TextFieldValue) -> Unit,
    background: Color,
    modifier: Modifier = Modifier,
    placeholder: String? = null,
    singleLine: Boolean = true,
    minLines: Int = 1,
    fontSize: TextUnit = 14.sp,
    border: Color? = null,
) {
    val styling = LocalStyling.current
    BasicTextField(
        value,
        onValueChange,
        modifier.fillMaxWidth()
            .let { m -> if (border != null) m.border(1.dp, border, RoundedCornerShape(4.dp)) else m }
            .background(background, RoundedCornerShape(4.dp)).padding(8.dp)
            .let { m -> if (placeholder != null) m.semantics { contentDescription = placeholder } else m },
        textStyle = TextStyle(color = styling.textColor.color, fontSize = fontSize),
        cursorBrush = SolidColor(styling.textColor.color),
        singleLine = singleLine,
        minLines = minLines,
        decorationBox = { inner ->
            Box {
                if (placeholder != null && value.text.isEmpty()) {
                    Text(placeholder, color = styling.subTextColor.color, fontSize = fontSize)
                }
                inner()
            }
        },
    )
}

/** A plain text button, as the MAUI pages' transparent Buttons. */
@Composable
fun FlatButton(text: String, modifier: Modifier = Modifier, fontSize: TextUnit = 14.sp, description: String? = null, enabled: Boolean = true, onClick: () -> Unit) {
    val styling = LocalStyling.current
    Text(
        text,
        modifier.clickable(enabled = enabled, role = Role.Button, onClick = onClick).padding(horizontal = 14.dp, vertical = 12.dp)
            .let { m -> if (description != null) m.semantics { contentDescription = description } else m },
        color = styling.textColor.color,
        fontSize = fontSize,
    )
}

/** A filled button, as the editor's MAUI/HTML buttons. */
@Composable
fun FilledButton(text: String, background: Color, modifier: Modifier = Modifier, textColor: Color = LocalStyling.current.textColor.color, onClick: () -> Unit) {
    Box(
        modifier.background(background, RoundedCornerShape(4.dp)).clickable(role = Role.Button, onClick = onClick)
            .padding(horizontal = 16.dp, vertical = 8.dp),
    ) {
        Text(text, color = textColor, fontSize = 14.sp)
    }
}
