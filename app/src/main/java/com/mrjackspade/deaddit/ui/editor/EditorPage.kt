package com.mrjackspade.deaddit.ui.editor

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.PlainTooltip
import androidx.compose.material3.Text
import androidx.compose.material3.TooltipBox
import androidx.compose.material3.TooltipDefaults
import androidx.compose.material3.rememberTooltipState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.DynamicColor
import com.mrjackspade.deaddit.ui.FilledButton
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.TextInput
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/**
 * A form for a settings object (the C# WebObjectEditorPage). The top-level page edits a copy and
 * hands it to [onSubmit] on Submit; Cancel or back discards it. Nested objects and list items open
 * as further pages that edit the same copy in place, with only a Back button.
 */
class EditorPage<T> private constructor(
    private val services: Services,
    private val schema: Schema<T>,
    private val ref: Ref<T>,
    private val onSubmit: ((T) -> Unit)?,
) : Screen() {

    companion object {
        /** Opens the editor for [original]; [onSubmit] gets the edited value. */
        fun <T> open(services: Services, schema: Schema<T>, original: T, onSubmit: (T) -> Unit) {
            val copy = mutableStateOf(original)
            services.navigator.push(EditorPage(services, schema, Ref({ copy.value }, { copy.value = it }), onSubmit))
        }
    }

    private val topLevel get() = onSubmit != null

    private fun leave() = services.navigator.remove(this)

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).statusBarsPadding().navigationBarsPadding().imePadding()) {
            Column(Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState()).padding(10.dp)) {
                for (field in schema.fields) FieldView(field)
            }

            Row(Modifier.fillMaxWidth().background(styling.secondaryColor.color)) {
                ButtonBarButton(if (topLevel) "Cancel" else "Back") { leave() }
                if (topLevel) {
                    ButtonBarButton("Submit") {
                        onSubmit!!(ref.get())
                        leave()
                    }
                }
            }
        }
    }

    @Composable
    private fun androidx.compose.foundation.layout.RowScope.ButtonBarButton(text: String, onClick: () -> Unit) {
        val styling = LocalStyling.current
        Box(
            Modifier.weight(1f).padding(5.dp).background(styling.primaryColor.color, RoundedCornerShape(4.dp))
                .clickable(role = Role.Button, onClick = onClick).padding(12.dp),
            contentAlignment = Alignment.Center,
        ) {
            Text(text, color = styling.textColor.color, fontSize = 14.sp)
        }
    }

    /** One field: its label (with its description as a long-press tooltip) and its input. */
    @Composable
    private fun FieldView(field: Field<T>) {
        Column(Modifier.fillMaxWidth().padding(5.dp).padding(5.dp)) {
            Label(field)
            Box(Modifier.padding(top = 5.dp)) {
                when (field) {
                    is BoolField -> BoolInput(field)
                    is TextField -> TextInputField(field)
                    is IntField -> NumberInput(field.label, field.get(ref.get()).toString(), "Invalid integer value") { text ->
                        // .NET int.TryParse: surrounding whitespace allowed.
                        text.trim().toIntOrNull()?.also { ref.set(field.set(ref.get(), it)) } != null
                    }
                    is DoubleField -> NumberInput(field.label, dotNetString(field.get(ref.get())), "Invalid decimal value") { text ->
                        text.trim().toDoubleOrNull()?.also { ref.set(field.set(ref.get(), it)) } != null
                    }
                    is ColorField -> ColorInput(field)
                    is EnumField<T, *> -> EnumInput(field)
                    is ObjectField<T, *> -> ObjectInput(field)
                    is ListField<T, *> -> ListInput(field)
                }
            }
        }
    }

    @OptIn(ExperimentalMaterial3Api::class)
    @Composable
    private fun Label(field: Field<T>) {
        val styling = LocalStyling.current
        val label = @Composable { Text(field.label, color = styling.textColor.color, fontSize = styling.subTextFontSize.px) }

        val description = field.description
        if (description.isNullOrBlank()) {
            label()
        } else {
            TooltipBox(
                positionProvider = TooltipDefaults.rememberPlainTooltipPositionProvider(),
                tooltip = { PlainTooltip { Text(description) } },
                state = rememberTooltipState(isPersistent = true),
            ) { label() }
        }
    }

    @Composable
    private fun BoolInput(field: BoolField<T>) {
        val styling = LocalStyling.current
        Checkbox(
            checked = field.get(ref.get()),
            onCheckedChange = { ref.set(field.set(ref.get(), it)) },
            modifier = Modifier.semantics { contentDescription = field.label },
            colors = CheckboxDefaults.colors(uncheckedColor = styling.textColor.color, checkmarkColor = styling.textColor.color),
        )
    }

    @Composable
    private fun TextInputField(field: TextField<T>) {
        val styling = LocalStyling.current
        val text = field.get(ref.get()) ?: ""
        // Kept as a TextFieldValue for the cursor; the model holds the text.
        var value by remember { mutableStateOf(TextFieldValue(text)) }
        if (value.text != text) value = TextFieldValue(text)

        TextInput(
            value,
            {
                value = it
                ref.set(field.set(ref.get(), it.text))
            },
            background = styling.tertiaryColor.color,
            border = styling.tertiaryColor.color,
            singleLine = !field.multiline,
            minLines = if (field.multiline) 4 else 1,
            modifier = Modifier.semantics { contentDescription = field.label },
        )
    }

    /** A number as typed; [apply] stores it and says whether it parsed (else the error shows). */
    @Composable
    private fun NumberInput(label: String, initial: String, error: String, apply: (String) -> Boolean) {
        val styling = LocalStyling.current
        var value by remember { mutableStateOf(TextFieldValue(initial)) }
        var valid by remember { mutableStateOf(true) }

        Column {
            TextInput(
                value,
                {
                    value = it
                    valid = apply(it.text)
                },
                background = styling.tertiaryColor.color,
                border = if (valid) styling.tertiaryColor.color else ERROR,
                modifier = Modifier.semantics { contentDescription = label },
            )
            if (!valid) Text(error, Modifier.padding(top = 4.dp), color = ERROR, fontSize = 12.sp)
        }
    }

    /**
     * A color as "#RRGGBB" (C#'s ToHex, without alpha) beside its swatch. Six or eight hex digits
     * replace it; anything else shows the error.
     */
    @Composable
    private fun ColorInput(field: ColorField<T>) {
        val styling = LocalStyling.current
        val current = field.get(ref.get())
        var value by remember { mutableStateOf(TextFieldValue(current.toHex())) }
        var valid by remember { mutableStateOf(true) }

        Column {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    Modifier.padding(end = 8.dp).size(36.dp)
                        .border(1.dp, styling.tertiaryColor.color, RoundedCornerShape(4.dp))
                        .background(Color(0xFF000000.toInt() or (current.toArgb() and 0xFFFFFF)), RoundedCornerShape(4.dp)),
                )
                TextInput(
                    value,
                    { changed ->
                        value = changed
                        // C#'s DynamicColor.TryParse checks only the length, then parses: other
                        // characters throw, as they did there.
                        if (changed.text.trim('#').length == 6 || changed.text.trim('#').length == 8) {
                            try {
                                ref.set(field.set(ref.get(), DynamicColor.parse(changed.text)))
                                valid = true
                            } catch (e: NumberFormatException) {
                                scope.launch { services.dialogs.error(e) }
                            }
                        } else {
                            valid = false
                        }
                    },
                    background = styling.tertiaryColor.color,
                    border = if (valid) styling.tertiaryColor.color else ERROR,
                    placeholder = "#RRGGBB",
                    modifier = Modifier.weight(1f).semantics { contentDescription = field.label },
                )
            }
            if (!valid) Text("Invalid color format (use #RRGGBB or #RRGGBBAA)", Modifier.padding(top = 4.dp), color = ERROR, fontSize = 12.sp)
        }
    }

    @Composable
    private fun EnumInput(field: EnumField<T, *>) {
        val styling = LocalStyling.current
        var open by remember { mutableStateOf(false) }
        val current = field.currentName(ref.get())

        Box {
            Row(
                Modifier.fillMaxWidth().border(1.dp, styling.tertiaryColor.color, RoundedCornerShape(4.dp))
                    .background(styling.tertiaryColor.color, RoundedCornerShape(4.dp))
                    .clickable(role = Role.DropdownList) { open = true }.padding(8.dp)
                    .semantics { contentDescription = "${field.label}: $current" },
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Text(current, color = styling.textColor.color, fontSize = 14.sp)
                Text("▾", color = styling.textColor.color, fontSize = 14.sp)
            }
            DropdownMenu(open, { open = false }, Modifier.background(styling.tertiaryColor.color)) {
                field.names.forEachIndexed { index, name ->
                    DropdownMenuItem(
                        text = { Text(name, color = styling.textColor.color) },
                        onClick = {
                            open = false
                            ref.set(field.select(ref.get(), index))
                        },
                    )
                }
            }
        }
    }

    @Composable
    private fun ObjectInput(field: ObjectField<T, *>) {
        val styling = LocalStyling.current
        FilledButton("Edit", styling.primaryColor.color, Modifier.semantics { contentDescription = "Edit ${field.label}" }) {
            field.edit(ref, nested)
        }
    }

    @Composable
    private fun ListInput(field: ListField<T, *>) {
        val styling = LocalStyling.current
        val labels = field.labels(ref.get())

        Column {
            Column(Modifier.padding(bottom = 8.dp)) {
                labels.forEachIndexed { index, label ->
                    Column {
                        Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                            Text(label, Modifier.weight(1f), color = styling.textColor.color, maxLines = 1)
                            FilledButton("Edit", styling.primaryColor.color, Modifier.padding(start = 8.dp).semantics { contentDescription = "Edit $label" }) {
                                field.edit(ref, index, nested)
                            }
                            FilledButton("Remove", REMOVE, Modifier.padding(start = 8.dp).semantics { contentDescription = "Remove $label" }, textColor = Color.White) {
                                field.remove(ref, index)
                            }
                        }
                        Box(Modifier.fillMaxWidth().size(1.dp).background(styling.tertiaryColor.color))
                    }
                }
            }
            FilledButton("Add", styling.primaryColor.color, Modifier.semantics { contentDescription = "Add to ${field.label}" }) {
                field.add(ref, nested)
            }
        }
    }

    private val nested = object : NestedEditor {
        override fun <C> open(schema: Schema<C>, ref: Ref<C>) {
            services.navigator.push(EditorPage(services, schema, ref, null))
        }
    }
}

private val ERROR = Color(0xFFFF4444)
private val REMOVE = Color(0xFFCC3333)

/** A double as .NET's ToString() writes it: whole numbers without ".0". */
private fun dotNetString(value: Double): String =
    if (value == Math.floor(value) && !value.isInfinite() && Math.abs(value) < 1e15) value.toLong().toString() else value.toString()
