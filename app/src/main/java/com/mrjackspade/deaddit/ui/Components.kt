package com.mrjackspade.deaddit.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Shape
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.core.config.ApplicationStyling

/*
 * The Lantern design's building blocks, shared by every page: calm cards with quiet edges to
 * anchor on, one accent kept for what's active or selected, and icon-first controls.
 */

/** A card's corners; [innerShape] for what sits in a card, [chipShape] for chips. */
val ApplicationStyling.cardShape: Shape get() = RoundedCornerShape(cornerRadius.dp)
val ApplicationStyling.innerShape: Shape get() = RoundedCornerShape((cornerRadius * 0.8f).dp)
val ApplicationStyling.chipShape: Shape get() = RoundedCornerShape((cornerRadius * 0.4f).dp)

/** The accent as a soft fill behind what's active, and as the edge of what's selected. */
val ApplicationStyling.accentFill: Color get() = accentColor.color.copy(alpha = 0.2f)
val ApplicationStyling.accentEdge: Color get() = accentColor.color.copy(alpha = 0.6f)

/** A soft fill of [color], behind an active vote. */
fun Color.fill(): Color = copy(alpha = 0.2f)

/** A card: the card color (the highlight, edged in the accent, when [selected]) with the border color around it. */
@Composable
fun Card(
    modifier: Modifier = Modifier,
    selected: Boolean = false,
    background: Color? = null,
    content: @Composable ColumnScope.() -> Unit,
) {
    val styling = LocalStyling.current
    val shape = styling.cardShape
    Column(
        modifier.fillMaxWidth().clip(shape)
            .background(background ?: if (selected) styling.highlightColor.color else styling.cardColor.color)
            .border(1.dp, if (selected) styling.accentEdge else styling.borderColor.color, shape),
        content = content,
    )
}

/**
 * An icon button. [active] marks one that's on: a soft fill of [activeColor] (the accent unless
 * given) behind it. [description] is what screen readers announce.
 */
@Composable
fun IconAction(
    icon: ImageVector,
    description: String,
    modifier: Modifier = Modifier,
    tint: Color = LocalStyling.current.subTextColor.color,
    active: Boolean = false,
    activeColor: Color = LocalStyling.current.accentColor.color,
    size: Dp = 40.dp,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val styling = LocalStyling.current
    Box(
        modifier.size(size).clip(styling.innerShape)
            .background(if (active) activeColor.fill() else Color.Transparent)
            .clickable(enabled = enabled, role = Role.Button, onClickLabel = description, onClick = onClick)
            .semantics { contentDescription = description },
        contentAlignment = Alignment.Center,
    ) {
        Icon(icon, contentDescription = null, tint = if (enabled) tint else tint.copy(alpha = 0.4f), modifier = Modifier.size(ICON_SIZE))
    }
}

/** A short label in a quiet chip: flair, tags, counts. */
@Composable
fun Chip(
    text: String,
    modifier: Modifier = Modifier,
    color: Color = LocalStyling.current.subTextColor.color,
    background: Color = LocalStyling.current.tertiaryColor.color,
    fontSize: TextUnit = (LocalStyling.current.subTextFontSize * 0.9).px,
) {
    Text(
        text,
        modifier.clip(LocalStyling.current.chipShape).background(background).padding(horizontal = 7.dp, vertical = 1.dp),
        color = color,
        fontSize = fontSize,
        maxLines = 1,
        overflow = TextOverflow.Ellipsis,
    )
}

/** The small heading over a group of rows. */
@Composable
fun SectionLabel(text: String, modifier: Modifier = Modifier) {
    val styling = LocalStyling.current
    Text(text, modifier.padding(start = 6.dp, bottom = 6.dp), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
}

/** A 1dp line in the border color, between rows. */
@Composable
fun Divider(modifier: Modifier = Modifier) {
    Box(modifier.fillMaxWidth().height(1.dp).background(LocalStyling.current.borderColor.color))
}

/**
 * A row of choices, one picked: the picked one on the highlight with an accent edge. Each option
 * draws [label] (text by default); [selected] may be null when none matches.
 */
@Composable
fun Segmented(
    options: List<String>,
    selected: Int?,
    onSelect: (Int) -> Unit,
    modifier: Modifier = Modifier,
    label: @Composable (index: Int, picked: Boolean) -> Unit = { index, picked ->
        val styling = LocalStyling.current
        Text(
            options[index],
            color = if (picked) styling.textColor.color else styling.subTextColor.color,
            fontSize = styling.subTextFontSize.px,
            textAlign = TextAlign.Center,
            maxLines = 1,
        )
    },
) {
    val styling = LocalStyling.current
    val outer = styling.innerShape
    val inner = RoundedCornerShape((styling.cornerRadius * 0.6f).dp)
    Row(
        modifier.fillMaxWidth().clip(outer).background(styling.cardColor.color).border(1.dp, styling.borderColor.color, outer).padding(3.dp),
        horizontalArrangement = Arrangement.spacedBy(3.dp),
    ) {
        options.forEachIndexed { index, option ->
            val picked = index == selected
            Box(
                Modifier.weight(1f).heightIn(min = 40.dp).clip(inner)
                    .background(if (picked) styling.highlightColor.color else Color.Transparent)
                    .border(1.dp, if (picked) styling.accentEdge else Color.Transparent, inner)
                    .clickable(role = Role.Tab) { onSelect(index) }
                    .semantics {
                        this.selected = picked
                        contentDescription = option
                    },
                contentAlignment = Alignment.Center,
            ) {
                label(index, picked)
            }
        }
    }
}

/** An on/off switch: the accent's soft fill with an accent knob when on, a quiet chip when off. */
@Composable
fun ToggleSwitch(checked: Boolean, onCheckedChange: (Boolean) -> Unit, description: String, modifier: Modifier = Modifier) {
    val styling = LocalStyling.current
    val shape = RoundedCornerShape((styling.cornerRadius * 0.8f).dp)
    Row(
        modifier.width(48.dp).height(28.dp).clip(shape)
            .background(if (checked) styling.accentFill else styling.tertiaryColor.color)
            .clickable(role = Role.Switch) { onCheckedChange(!checked) }
            .semantics {
                contentDescription = description
                stateDescription = if (checked) "On" else "Off"
            }
            .padding(3.dp),
        horizontalArrangement = if (checked) Arrangement.End else Arrangement.Start,
    ) {
        Box(
            Modifier.size(22.dp).clip(RoundedCornerShape((styling.cornerRadius * 0.6f).dp))
                .background(if (checked) styling.accentColor.color else styling.subTextColor.color),
        )
    }
}

/** Rows in one card, divided by lines ([ListRow] draws its own). */
@Composable
fun ListGroup(modifier: Modifier = Modifier, content: @Composable ColumnScope.() -> Unit) {
    Card(modifier, content = content)
}

/**
 * A row of a [ListGroup]: an icon, a label and what's at its end. [divider] draws the line under
 * it (left off the group's last row). Tapping it runs [onClick], when there is one.
 */
@Composable
fun ListRow(
    label: String,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    labelColor: Color = LocalStyling.current.textColor.color,
    divider: Boolean = true,
    onClick: (() -> Unit)? = null,
    trailing: @Composable RowScope.() -> Unit = {},
) {
    val styling = LocalStyling.current
    Column {
        Row(
            modifier.fillMaxWidth().heightIn(min = 50.dp)
                .let { m -> if (onClick != null) m.clickable(onClick = onClick) else m }
                .padding(start = 12.dp, end = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            if (icon != null) Icon(icon, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(ICON_SIZE))
            Text(
                label,
                Modifier.weight(1f).padding(vertical = 10.dp),
                color = labelColor,
                fontSize = (styling.titleFontSize * 0.95).px,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
            )
            trailing()
        }
        if (divider) Divider()
    }
}

/**
 * A bordered button with an optional icon before its label. [accent] makes it the page's one
 * accented action (a soft accent fill, an accent edge and text).
 */
@Composable
fun PillButton(
    text: String,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null,
    accent: Boolean = false,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val styling = LocalStyling.current
    val shape = styling.cardShape
    val content = when {
        !enabled -> styling.subTextColor.color.copy(alpha = 0.5f)
        accent -> styling.accentColor.color
        else -> styling.textColor.color
    }
    Row(
        modifier.heightIn(min = 44.dp).clip(shape)
            .background(if (accent) styling.accentFill else styling.cardColor.color)
            .border(1.dp, if (accent) styling.accentEdge else styling.borderColor.color, shape)
            .clickable(enabled = enabled, role = Role.Button, onClick = onClick)
            .padding(horizontal = 14.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterHorizontally),
    ) {
        if (icon != null) Icon(icon, contentDescription = null, tint = if (accent) content else styling.subTextColor.color, modifier = Modifier.size(20.dp))
        Text(text, color = content, fontSize = styling.subTextFontSize.px, fontWeight = FontWeight.Medium, maxLines = 1)
    }
}

/** A top bar's main action (send, save): the accent's soft fill with the icon in the accent. */
@Composable
fun AccentAction(icon: ImageVector, description: String, enabled: Boolean = true, onClick: () -> Unit) {
    val styling = LocalStyling.current
    Box(
        Modifier.padding(horizontal = 4.dp).width(52.dp).height(40.dp).clip(styling.cardShape)
            .background(if (enabled) styling.accentFill else styling.tertiaryColor.color)
            .clickable(enabled = enabled, role = Role.Button, onClickLabel = description, onClick = onClick)
            .semantics { contentDescription = description },
        contentAlignment = Alignment.Center,
    ) {
        Icon(icon, contentDescription = null, tint = if (enabled) styling.accentColor.color else styling.subTextColor.color, modifier = Modifier.size(ICON_SIZE))
    }
}
