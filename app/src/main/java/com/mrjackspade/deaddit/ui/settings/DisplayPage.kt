package com.mrjackspade.deaddit.ui.settings

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
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.outlined.ArrowDownward
import androidx.compose.material.icons.outlined.ArrowUpward
import androidx.compose.material.icons.outlined.ChatBubbleOutline
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.Contrast
import androidx.compose.material.icons.outlined.CropSquare
import androidx.compose.material.icons.outlined.FormatSize
import androidx.compose.material.icons.outlined.Opacity
import androidx.compose.material.icons.outlined.Palette
import androidx.compose.material3.Icon
import androidx.compose.material3.Slider
import androidx.compose.material3.SliderDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.ApplicationStyling
import com.mrjackspade.deaddit.core.config.Lantern
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Segmented
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.TopBarTitle
import com.mrjackspade.deaddit.ui.accentFill
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.px

/**
 * The Lantern presets over the styling: contrast, edge strength, text size, accent and how far
 * read posts fade. Each choice saves at once, so the whole app (this page included) shows it as
 * it's made. A styling changed by hand in Configuration may match no preset: nothing is picked.
 */
class DisplayPage(private val services: Services) : Screen() {
    private fun change(edit: (ApplicationStyling) -> ApplicationStyling) = services.settings.styling.update(edit)

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                TopBarButton(Icons.AutoMirrored.Outlined.ArrowBack, "Back", color = styling.textColor.color) { services.navigator.remove(this@DisplayPage) }
                TopBarTitle("Display")
            }

            Column(
                Modifier.weight(1f).fillMaxWidth().verticalScroll(rememberScrollState()).navigationBarsPadding().padding(8.dp),
                verticalArrangement = Arrangement.spacedBy(18.dp),
            ) {
                Preview(styling)

                Section(Icons.Outlined.Contrast, "Contrast") {
                    val levels = Lantern.Contrast.entries
                    Segmented(levels.map { it.name }, levels.indexOf(Lantern.contrastOf(styling)).takeIf { it >= 0 }, { i ->
                        change { Lantern.withContrast(it, levels[i]) }
                    })
                }

                Section(Icons.Outlined.CropSquare, "Edges") {
                    val edges = Lantern.Edges.entries
                    Segmented(edges.map { it.name }, edges.indexOf(Lantern.edgesOf(styling)).takeIf { it >= 0 }, { i ->
                        change { Lantern.withEdges(it, edges[i]) }
                    })
                }

                Section(Icons.Outlined.FormatSize, "Text") {
                    val sizes = Lantern.TextSize.entries
                    Segmented(
                        sizes.map { it.name },
                        sizes.indexOf(Lantern.textSizeOf(styling)).takeIf { it >= 0 },
                        { i -> change { Lantern.withTextSize(it, sizes[i]) } },
                        label = { i, picked ->
                            Text(
                                "A",
                                color = if (picked) styling.textColor.color else styling.subTextColor.color,
                                fontSize = listOf(15, 19, 23)[i].sp,
                                fontWeight = FontWeight.SemiBold,
                            )
                        },
                    )
                }

                Section(Icons.Outlined.Palette, "Accent") {
                    Row(Modifier.padding(horizontal = 4.dp), horizontalArrangement = Arrangement.spacedBy(14.dp)) {
                        val current = Lantern.accentOf(styling)
                        for (accent in Lantern.accents) {
                            val picked = accent === current
                            Box(
                                Modifier.size(48.dp).clip(CircleShape)
                                    .border(2.dp, if (picked) accent.color.color else Color.Transparent, CircleShape)
                                    .padding(5.dp).clip(CircleShape).background(accent.color.color)
                                    .clickable(role = Role.RadioButton) { change { Lantern.withAccent(it, accent) } }
                                    .semantics {
                                        contentDescription = accent.name
                                        selected = picked
                                    },
                                contentAlignment = Alignment.Center,
                            ) {
                                if (picked) Icon(Icons.Outlined.Check, contentDescription = null, tint = styling.secondaryColor.color, modifier = Modifier.size(20.dp))
                            }
                        }
                    }
                }

                Section(Icons.Outlined.Opacity, "Read fade") {
                    // Moved freely, saved when let go: the preview's read post shows it as it moves.
                    Slider(
                        value = previewFade ?: styling.visitedOpacity.toFloat(),
                        onValueChange = { previewFade = it },
                        onValueChangeFinished = {
                            previewFade?.let { value -> change { it.copy(visitedOpacity = Math.round(value * 100) / 100.0) } }
                            previewFade = null
                        },
                        valueRange = 0.05f..1f,
                        modifier = Modifier.padding(horizontal = 4.dp).semantics { contentDescription = "Read fade" },
                        colors = SliderDefaults.colors(
                            thumbColor = styling.accentColor.color,
                            activeTrackColor = styling.accentColor.color,
                            inactiveTrackColor = styling.tertiaryColor.color,
                        ),
                    )
                }
            }
        }
    }

    /** The read fade while its slider moves, for the preview. */
    private var previewFade by mutableStateOf<Float?>(null)

    @Composable
    private fun Section(icon: ImageVector, title: String, content: @Composable () -> Unit) {
        val styling = LocalStyling.current
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(Modifier.padding(start = 6.dp), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Icon(icon, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(20.dp))
                Text(title, color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
            }
            content()
        }
    }

    /** A post as the feed shows it, upvoted and selected, over one already read. */
    @Composable
    private fun Preview(styling: ApplicationStyling) {
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            SamplePost(styling, "r/AskHistorians · 4h", "How did medieval towns get around at night before street lighting?", "3.1k", "287", selected = true, read = false)
            SamplePost(styling, "r/Android · 9h", "Which app do you use every day that nobody ever talks about?", "912", "642", selected = false, read = true)
        }
    }

    @Composable
    private fun SamplePost(styling: ApplicationStyling, meta: String, title: String, score: String, comments: String, selected: Boolean, read: Boolean) {
        val fade = previewFade ?: styling.visitedOpacity.toFloat()
        Card(Modifier.alpha(if (read) fade else 1f), selected = selected) {
            Column(Modifier.padding(start = 12.dp, end = 12.dp, top = 10.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(meta, color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
                    if (read) Icon(Icons.Outlined.Check, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(16.dp))
                }
                Text(
                    title,
                    color = styling.textColor.color,
                    fontSize = styling.titleFontSize.px,
                    fontWeight = if (read) FontWeight.Normal else FontWeight.SemiBold,
                    lineHeight = styling.titleFontSize.px * 1.3f,
                )
            }
            Row(Modifier.fillMaxWidth().padding(end = 6.dp, bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                val up = !read
                Spacer(Modifier.weight(1f))
                Box(
                    Modifier.size(40.dp).clip(styling.innerShape).background(if (up) styling.accentFill else Color.Transparent),
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.Outlined.ArrowUpward, contentDescription = null, tint = if (up) styling.upvoteColor.color else styling.subTextColor.color, modifier = Modifier.size(22.dp))
                }
                Text(
                    score,
                    Modifier.width(36.dp),
                    color = if (up) styling.upvoteColor.color else styling.subTextColor.color,
                    fontSize = styling.subTextFontSize.px,
                    fontWeight = if (up) FontWeight.SemiBold else FontWeight.Normal,
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                )
                Box(Modifier.size(40.dp), contentAlignment = Alignment.Center) {
                    Icon(Icons.Outlined.ArrowDownward, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(22.dp))
                }
                Spacer(Modifier.width(8.dp))
                Row(Modifier.heightIn(min = 40.dp).padding(horizontal = 8.dp), verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    Icon(Icons.Outlined.ChatBubbleOutline, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(20.dp))
                    Text(comments, color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
                }
            }
        }
    }
}
