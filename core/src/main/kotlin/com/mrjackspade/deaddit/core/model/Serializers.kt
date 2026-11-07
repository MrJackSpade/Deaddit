package com.mrjackspade.deaddit.core.model

import kotlinx.serialization.KSerializer
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.descriptors.SerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonDecoder
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonEncoder
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.doubleOrNull
import java.time.Instant

/** Reads Reddit's JSON, and Reddit.Api's re-serialization of it, which uses the same names. */
val RedditJson = Json {
    ignoreUnknownKeys = true
    coerceInputValues = true
    explicitNulls = false
    isLenient = true
}

/**
 * Reddit's timestamps: Unix seconds, but "edited" is false when a thing was never edited, and
 * Reddit.Api writes 0 for the same. Both read as null.
 */
object RedditInstantSerializer : KSerializer<Instant?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("RedditInstant", PrimitiveKind.DOUBLE)

    override fun deserialize(decoder: Decoder): Instant? {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive || element is JsonNull) return null
        val seconds = element.doubleOrNull ?: return null
        if (seconds == 0.0) return null
        return Instant.ofEpochMilli((seconds * 1000).toLong())
    }

    override fun serialize(encoder: Encoder, value: Instant?) {
        (encoder as JsonEncoder).encodeJsonElement(if (value == null) JsonNull else JsonPrimitive(value.toEpochMilli() / 1000.0))
    }
}

/** Reddit's "likes": true is an upvote, false a downvote, null no vote. */
object VoteStateSerializer : KSerializer<VoteState> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("VoteState", PrimitiveKind.BOOLEAN)

    override fun deserialize(decoder: Decoder): VoteState {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        return when ((element as? JsonPrimitive)?.booleanOrNull) {
            true -> VoteState.Upvote
            false -> VoteState.Downvote
            null -> VoteState.None
        }
    }

    override fun serialize(encoder: Encoder, value: VoteState) {
        (encoder as JsonEncoder).encodeJsonElement(
            when (value) {
                VoteState.Upvote -> JsonPrimitive(true)
                VoteState.Downvote -> JsonPrimitive(false)
                VoteState.None -> JsonNull
            }
        )
    }
}

/** Strings Reddit sometimes sends as "" for none; read as null. */
object BlankAsNullSerializer : KSerializer<String?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("BlankAsNull", PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): String? {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive || element is JsonNull) return null
        return element.content.ifBlank { null }
    }

    override fun serialize(encoder: Encoder, value: String?) {
        (encoder as JsonEncoder).encodeJsonElement(if (value == null) JsonNull else JsonPrimitive(value))
    }
}

/** Keeps a field's raw JSON, for shapes that vary (comment replies are "" or a listing). */
object RawJsonSerializer : KSerializer<JsonElement?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("RawJson", PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): JsonElement? = (decoder as JsonDecoder).decodeJsonElement()

    override fun serialize(encoder: Encoder, value: JsonElement?) {
        (encoder as JsonEncoder).encodeJsonElement(value ?: JsonNull)
    }
}

/**
 * URLs Reddit's JSON sends HTML-escaped ("&amp;" for "&"), and Reddit.Api passes on as they are.
 * The C# app put them into HTML, where the browser decoded them; loaded directly they 403.
 */
object HtmlDecodedUrlSerializer : KSerializer<String?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("HtmlDecodedUrl", PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): String? {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive || element is JsonNull) return null
        return org.jsoup.parser.Parser.unescapeEntities(element.content, true)
    }

    override fun serialize(encoder: Encoder, value: String?) {
        (encoder as JsonEncoder).encodeJsonElement(if (value == null) JsonNull else JsonPrimitive(value))
    }
}
