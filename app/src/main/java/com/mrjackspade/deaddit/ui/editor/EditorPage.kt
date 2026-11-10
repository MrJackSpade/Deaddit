package com.mrjackspade.deaddit.ui.editor

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.clipToBounds
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.DynamicColor
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.automirrored.outlined.KeyboardArrowRight
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.Close
import androidx.compose.material.icons.outlined.DeleteOutline
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Info
import androidx.compose.material3.Icon
import androidx.compose.foundation.layout.heightIn
import com.mrjackspade.deaddit.ui.AccentAction
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.FormInput
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.ListGroup
import com.mrjackspade.deaddit.ui.ListRow
import com.mrjackspade.deaddit.ui.PillButton
import com.mrjackspade.deaddit.ui.ToggleSwitch
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.TopBarTitle
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
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
    private val title: String,
    private val onSubmit: ((T) -> Unit)?,
) : Screen() {

    companion object {
        /** Opens the editor for [original], headed [title]; [onSubmit] gets the edited value. */
        fun <T> open(services: Services, schema: Schema<T>, original: T, title: String = "Edit", onSubmit: (T) -> Unit) {
            val copy = mutableStateOf(original)
            services.navigator.push(EditorPage(services, schema, Ref({ copy.value }, { copy.value = it }), title, onSubmit))
        }
    }

    private val topLevel get() = onSubmit != null

    private fun leave() = services.navigator.remove(this)

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color).navigationBarsPadding().imePadding()) {
            // The top-level page discards (close) or saves (the accent check); nested pages edit
            // the same copy, so they only go back.
            TopBar {
                TopBarButton(
                    if (topLevel) Icons.Outlined.Close else Icons.AutoMirrored.Outlined.ArrowBack,
                    if (topLevel) "Cancel" else "Back",
                    color = styling.textColor.color,
                ) { leave() }
                TopBarTitle(title)
                if (topLevel) {
                    AccentAction(Icons.Outlined.Check, "Submit") {
                        onSubmit!!(ref.get())
                        leave()
                    }
                }
            }

            Column(
                Modifier.weight(1f).fillMaxWidth().clipToBounds().verticalScroll(rememberScrollState()).padding(horizontal = 8.dp, vertical = 6.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                for (field in schema.fields) FieldView(field)
            }
        }
    }

    /**
     * One field. A switch or a nested object is one row, label and control side by side; the
     * rest have their label (with an "i" button for its description) over the input.
     */
    @Composable
    private fun FieldView(field: Field<T>) {
        when (field) {
            is BoolField -> RowField(field) { BoolInput(field) }
            is ObjectField<T, *> -> ObjectInput(field)
            else -> Column(Modifier.fillMaxWidth().padding(horizontal = 4.dp, vertical = 6.dp)) {
                Label(field)
                Box(Modifier.padding(top = 6.dp)) {
                    when (field) {
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
                        is ListField<T, *> -> ListInput(field)
                        is BoolField, is ObjectField<T, *> -> Unit
                    }
                }
            }
        }
    }

    /** A field as a card row: label (and its "i") at the start, [control] at the end. */
    @Composable
    private fun RowField(field: Field<T>, control: @Composable () -> Unit) {
        Card {
            Row(Modifier.fillMaxWidth().heightIn(min = 52.dp).padding(start = 12.dp, end = 12.dp), verticalAlignment = Alignment.CenterVertically) {
                Box(Modifier.weight(1f).padding(vertical = 8.dp)) { Label(field) }
                control()
            }
        }
    }

    /**
     * The label, and beside it for a described field a round "i" button that shows the
     * description in an alert titled with the label (the C# FormFieldComponent).
     */
    @Composable
    private fun Label(field: Field<T>) {
        val styling = LocalStyling.current
        val description = field.description

        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(field.label, color = styling.textColor.color, fontSize = (styling.subTextFontSize * 1.05).px)
            if (!description.isNullOrBlank()) {
                Box(
                    Modifier.padding(start = 6.dp).size(28.dp).clip(CircleShape)
                        .clickable(role = Role.Button, onClickLabel = "About ${field.label}") {
                            scope.launch { services.dialogs.alert(field.label, description, "OK") }
                        },
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.Outlined.Info, contentDescription = "About ${field.label}", tint = styling.subTextColor.color, modifier = Modifier.size(18.dp))
                }
            }
        }
    }

    @Composable
    private fun BoolInput(field: BoolField<T>) {
        ToggleSwitch(field.get(ref.get()), { ref.set(field.set(ref.get(), it)) }, field.label)
    }

    @Composable
    private fun TextInputField(field: TextField<T>) {
        val styling = LocalStyling.current
        val text = field.get(ref.get()) ?: ""
        // Kept as a TextFieldValue for the cursor; the model holds the text.
        var value by remember { mutableStateOf(TextFieldValue(text)) }
        if (value.text != text) value = TextFieldValue(text)

        FormInput(
            value,
            {
                value = it
                ref.set(field.set(ref.get(), it.text))
            },
            background = styling.cardColor.color,
            border = styling.borderColor.color,
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
            FormInput(
                value,
                {
                    value = it
                    valid = apply(it.text)
                },
                background = styling.cardColor.color,
                border = if (valid) styling.borderColor.color else ERROR,
                modifier = Modifier.semantics { contentDescription = label },
                // An HTML type="number" input, which brings up the number keyboard.
                keyboard = KeyboardType.Decimal,
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
                        .border(1.dp, styling.borderColor.color, styling.innerShape)
                        .background(Color(0xFF000000.toInt() or (current.toArgb() and 0xFFFFFF)), styling.innerShape),
                )
                FormInput(
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
                    background = styling.cardColor.color,
                    border = if (valid) styling.borderColor.color else ERROR,
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
                Modifier.fillMaxWidth().clip(styling.innerShape).border(1.dp, styling.borderColor.color, styling.innerShape)
                    .background(styling.cardColor.color)
                    .clickable(role = Role.DropdownList) { open = true }.padding(horizontal = 12.dp, vertical = 10.dp)
                    .semantics { contentDescription = "${field.label}: $current" },
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Text(current, color = styling.textColor.color, fontSize = 15.sp)
                Icon(Icons.Outlined.ExpandMore, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(20.dp))
            }
            DropdownMenu(open, { open = false }, Modifier.background(styling.cardColor.color)) {
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

    /** A nested object: a card row with its label that opens its own page. */
    @Composable
    private fun ObjectInput(field: ObjectField<T, *>) {
        val styling = LocalStyling.current
        Card(Modifier.clickable(role = Role.Button, onClickLabel = "Edit ${field.label}") { field.edit(ref, nested(field.label)) }) {
            Row(Modifier.fillMaxWidth().heightIn(min = 52.dp).padding(start = 12.dp, end = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                Text(field.label, Modifier.weight(1f), color = styling.textColor.color, fontSize = (styling.subTextFontSize * 1.05).px)
                Icon(Icons.AutoMirrored.Outlined.KeyboardArrowRight, contentDescription = null, tint = styling.subTextColor.color)
            }
        }
    }

    /** A list: its items as rows of one card, each with edit and remove, then Add. */
    @Composable
    private fun ListInput(field: ListField<T, *>) {
        val styling = LocalStyling.current
        val labels = field.labels(ref.get())

        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            if (labels.isNotEmpty()) {
                ListGroup {
                    labels.forEachIndexed { index, label ->
                        ListRow(label, divider = index != labels.lastIndex) {
                            IconAction(Icons.Outlined.Edit, "Edit $label") { field.edit(ref, index, nested(label)) }
                            IconAction(Icons.Outlined.DeleteOutline, "Remove $label") { field.remove(ref, index) }
                        }
                    }
                }
            }
            PillButton("Add", icon = Icons.Outlined.Add, modifier = Modifier.semantics { contentDescription = "Add to ${field.label}" }) {
                field.add(ref, nested(field.label))
            }
        }
    }

    /** Opens nested objects and list items, each headed [title]. */
    private fun nested(title: String) = object : NestedEditor {
        override fun <C> open(schema: Schema<C>, ref: Ref<C>) {
            services.navigator.push(EditorPage(services, schema, ref, title, null))
        }
    }
}

private val ERROR = Color(0xFFF28B82)

/** A double as .NET's ToString() writes it: whole numbers without ".0". */
private fun dotNetString(value: Double): String =
    if (value == Math.floor(value) && !value.isInfinite() && Math.abs(value) < 1e15) value.toLong().toString() else value.toString()
