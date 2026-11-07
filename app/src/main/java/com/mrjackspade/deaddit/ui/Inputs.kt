package com.mrjackspade.deaddit.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.defaultMinSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
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

/** MAUI's Gray500 (Resources/Styles/Colors.xaml): the dark theme's placeholder color in Styles.xaml. */
private val GRAY_500 = Color(0xFF6E6E6E)

/** MAUI's Gray200 and Gray600: a disabled Button's text and background in the dark theme (Styles.xaml). */
private val GRAY_200 = Color(0xFFC8C8C8)
private val GRAY_600 = Color(0xFF404040)

/**
 * A MAUI Entry/Editor (the reply and message pages): square, on [background], font 14, with
 * [placeholder] in Gray500 while empty. Android's EditText keeps its own background's padding
 * (AppCompat's abc_edit_text_material: 4dp at the sides, 10dp above and 11dp below the text)
 * when MAUI swaps that background for a color, and MAUI's style sets a minimum height of 44.
 * Single-line unless [singleLine] says otherwise.
 */
@Composable
fun TextInput(
    value: TextFieldValue,
    onValueChange: (TextFieldValue) -> Unit,
    background: Color,
    modifier: Modifier = Modifier,
    placeholder: String? = null,
    singleLine: Boolean = true,
) {
    val styling = LocalStyling.current
    BasicTextField(
        value,
        onValueChange,
        modifier.fillMaxWidth().defaultMinSize(minHeight = 44.dp).background(background)
            .padding(start = 4.dp, top = 10.dp, end = 4.dp, bottom = 11.dp)
            .let { m -> if (placeholder != null) m.semantics { contentDescription = placeholder } else m },
        textStyle = TextStyle(color = styling.textColor.color, fontSize = 14.sp),
        cursorBrush = SolidColor(styling.textColor.color),
        singleLine = singleLine,
        decorationBox = { inner ->
            Box {
                if (placeholder != null && value.text.isEmpty()) {
                    Text(placeholder, color = GRAY_500, fontSize = 14.sp)
                }
                inner()
            }
        },
    )
}

/**
 * An HTML form input, as the C# settings editor's (8px padding, a 1px [border], 4px corners),
 * on [background]. [keyboard] is the keyboard it asks for ("type=number" fields want numbers).
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
    BasicTextField(
        value,
        onValueChange,
        modifier.fillMaxWidth()
            .border(1.dp, border, RoundedCornerShape(4.dp))
            .background(background, RoundedCornerShape(4.dp)).padding(8.dp)
            .let { m -> if (placeholder != null) m.semantics { contentDescription = placeholder } else m },
        textStyle = TextStyle(color = styling.textColor.color, fontSize = 14.sp),
        cursorBrush = SolidColor(styling.textColor.color),
        singleLine = singleLine,
        minLines = minLines,
        keyboardOptions = KeyboardOptions(keyboardType = keyboard),
        decorationBox = { inner ->
            Box {
                if (placeholder != null && value.text.isEmpty()) {
                    Text(placeholder, color = styling.subTextColor.color, fontSize = 14.sp)
                }
                inner()
            }
        },
    )
}

/**
 * A MAUI Button as Styles.xaml styles it: 8 corners, 14,10 padding, at least 44 by 44, font 14;
 * disabled, it greys out (Gray200 text on Gray600). [padding] and [fontSize] for the pages that
 * set their own.
 */
@Composable
fun MauiButton(
    text: String,
    background: Color,
    modifier: Modifier = Modifier,
    textColor: Color = LocalStyling.current.textColor.color,
    fontSize: TextUnit = 14.sp,
    padding: PaddingValues = PaddingValues(horizontal = 14.dp, vertical = 10.dp),
    description: String? = null,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val shape = RoundedCornerShape(8.dp)
    Box(
        modifier.defaultMinSize(minWidth = 44.dp, minHeight = 44.dp)
            .clip(shape)
            .background(if (enabled) background else GRAY_600, shape)
            .clickable(enabled = enabled, role = Role.Button, onClickLabel = description, onClick = onClick)
            .padding(padding),
        contentAlignment = Alignment.Center,
    ) {
        Text(text, color = if (enabled) textColor else GRAY_200, fontSize = fontSize)
    }
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

/**
 * A filled button, as the editor's HTML buttons: 4px corners, [horizontal] by [vertical] padding
 * (8px 16px unless the C# gave the button its own).
 */
@Composable
fun FilledButton(
    text: String,
    background: Color,
    modifier: Modifier = Modifier,
    textColor: Color = LocalStyling.current.textColor.color,
    horizontal: Dp = 16.dp,
    vertical: Dp = 8.dp,
    onClick: () -> Unit,
) {
    Box(
        modifier.background(background, RoundedCornerShape(4.dp)).clickable(role = Role.Button, onClick = onClick)
            .padding(horizontal = horizontal, vertical = vertical),
    ) {
        Text(text, color = textColor, fontSize = 14.sp)
    }
}
