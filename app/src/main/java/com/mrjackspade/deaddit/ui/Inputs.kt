package com.mrjackspade.deaddit.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.defaultMinSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/**
 * A text box (the reply and message pages): on [background], edged in the border color (or
 * [border]), with [placeholder] in the subtext color while empty. Single-line unless [singleLine]
 * says otherwise.
 */
@Composable
fun TextInput(
    value: TextFieldValue,
    onValueChange: (TextFieldValue) -> Unit,
    background: Color,
    modifier: Modifier = Modifier,
    placeholder: String? = null,
    singleLine: Boolean = true,
    border: Color = LocalStyling.current.borderColor.color,
) {
    val styling = LocalStyling.current
    val shape = styling.innerShape
    val fontSize = styling.commentFontSize.px
    BasicTextField(
        value,
        onValueChange,
        modifier.fillMaxWidth().defaultMinSize(minHeight = 44.dp).clip(shape).background(background).border(1.dp, border, shape)
            .padding(horizontal = 12.dp, vertical = 11.dp)
            .let { m -> if (placeholder != null) m.semantics { contentDescription = placeholder } else m },
        textStyle = TextStyle(color = styling.textColor.color, fontSize = fontSize, fontFamily = AppFont, lineHeight = fontSize * 1.45f),
        cursorBrush = SolidColor(styling.accentColor.color),
        singleLine = singleLine,
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

/**
 * A settings editor input, on [background] with a 1px [border]. [keyboard] is the keyboard it
 * asks for ("type=number" fields want numbers).
 */
@Composable
fun FormInput(
    value: TextFieldValue,
    onValueChange: (TextFieldValue) -> Unit,
    background: Color,
    border: Color,
    modifier: Modifier = Modifier,
    placeholder: String? = null,
    singleLine: Boolean = true,
    minLines: Int = 1,
    keyboard: KeyboardType = KeyboardType.Text,
) {
    val styling = LocalStyling.current
    val shape = styling.innerShape
    BasicTextField(
        value,
        onValueChange,
        modifier.fillMaxWidth().clip(shape).background(background).border(1.dp, border, shape).padding(horizontal = 12.dp, vertical = 10.dp)
            .let { m -> if (placeholder != null) m.semantics { contentDescription = placeholder } else m },
        textStyle = TextStyle(color = styling.textColor.color, fontSize = 15.sp, fontFamily = AppFont),
        cursorBrush = SolidColor(styling.accentColor.color),
        singleLine = singleLine,
        minLines = minLines,
        keyboardOptions = KeyboardOptions(keyboardType = keyboard),
        decorationBox = { inner ->
            Box {
                if (placeholder != null && value.text.isEmpty()) {
                    Text(placeholder, color = styling.subTextColor.color, fontSize = 15.sp)
                }
                inner()
            }
        },
    )
}

/**
 * A button on [background]: the card corners, at least 44 by 44; disabled, it greys out.
 * [padding] and [fontSize] for the pages that set their own.
 */
@Composable
fun MauiButton(
    text: String,
    background: Color,
    modifier: Modifier = Modifier,
    textColor: Color = LocalStyling.current.textColor.color,
    fontSize: TextUnit = 15.sp,
    padding: PaddingValues = PaddingValues(horizontal = 14.dp, vertical = 10.dp),
    description: String? = null,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val styling = LocalStyling.current
    val shape = styling.cardShape
    Box(
        modifier.defaultMinSize(minWidth = 44.dp, minHeight = 44.dp)
            .clip(shape)
            .background(if (enabled) background else styling.tertiaryColor.color, shape)
            .border(1.dp, styling.borderColor.color, shape)
            .clickable(enabled = enabled, role = Role.Button, onClickLabel = description, onClick = onClick)
            .padding(padding),
        contentAlignment = Alignment.Center,
    ) {
        Text(text, color = if (enabled) textColor else styling.subTextColor.color, fontSize = fontSize)
    }
}

/** A plain text button. */
@Composable
fun FlatButton(text: String, modifier: Modifier = Modifier, fontSize: TextUnit = 15.sp, description: String? = null, enabled: Boolean = true, onClick: () -> Unit) {
    val styling = LocalStyling.current
    Text(
        text,
        modifier.clip(styling.innerShape).clickable(enabled = enabled, role = Role.Button, onClick = onClick).padding(horizontal = 14.dp, vertical = 12.dp)
            .let { m -> if (description != null) m.semantics { contentDescription = description } else m },
        color = styling.textColor.color,
        fontSize = fontSize,
    )
}

/** A filled button, as the editor's: [horizontal] by [vertical] padding. */
@Composable
fun FilledButton(
    text: String,
    background: Color,
    modifier: Modifier = Modifier,
    textColor: Color = LocalStyling.current.textColor.color,
    horizontal: Dp = 14.dp,
    vertical: Dp = 8.dp,
    onClick: () -> Unit,
) {
    val styling = LocalStyling.current
    Box(
        modifier.clip(styling.innerShape).background(background).clickable(role = Role.Button, onClick = onClick)
            .padding(horizontal = horizontal, vertical = vertical),
    ) {
        Text(text, color = textColor, fontSize = 15.sp)
    }
}
