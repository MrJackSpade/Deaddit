package com.mrjackspade.deaddit.core.model

import kotlinx.serialization.KSerializer
import kotlinx.serialization.SerializationException
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

/**
 * Reads Reddit.Api's JSON: its models re-serialized, under Reddit's names. A value a model doesn't
 * know (an enum value Reddit added, say) fails the read, as it failed the page in the C# app.
 */
val RedditJson = Json {
    ignoreUnknownKeys = true
    explicitNulls = false
    isLenient = true
}

/**
 * Reddit's timestamps (Unix seconds), read as Reddit.Api's JsonDateTimeConverter reads them: null,
 * true or false, a number <= 0, or a string that isn't a number > 0 is no date. Reddit says false
 * for "never edited", and Reddit.Api writes that as 0.
 */
object RedditInstantSerializer : KSerializer<Instant?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("RedditInstant", PrimitiveKind.DOUBLE)

    override fun deserialize(decoder: Decoder): Instant? {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive) throw SerializationException("Unexpected $element when parsing a Unix timestamp.")
        if (element is JsonNull) return null

        val seconds = if (element.isString) {
            element.content.trim().toDoubleOrNull()
        } else {
            if (element.booleanOrNull != null) return null
            element.doubleOrNull ?: throw SerializationException("Unexpected ${element.content} when parsing a Unix timestamp.")
        }

        if (seconds == null || seconds <= 0) return null
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

/**
 * Reddit.Api's JsonBool, which it writes as true, false, null or "". Only true is true: the C# app
 * read it through JsonBool's conversion to bool, which is false for the rest.
 */
object JsonBoolSerializer : KSerializer<Boolean> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("JsonBool", PrimitiveKind.BOOLEAN)

    override fun deserialize(decoder: Decoder): Boolean {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive) throw SerializationException("Unexpected $element when parsing a JsonBool.")
        if (element is JsonNull) return false
        if (element.isString) {
            if (element.content.isEmpty()) return false
            throw SerializationException("Unexpected \"${element.content}\" when parsing a JsonBool.")
        }
        return element.booleanOrNull ?: throw SerializationException("Unexpected ${element.content} when parsing a JsonBool.")
    }

    override fun serialize(encoder: Encoder, value: Boolean) = encoder.encodeBoolean(value)
}

/**
 * Reddit.Api's EmptyStringEnumConverter, for its enums with Null and Empty members. Their Null is
 * Kotlin null, so these enums are read into nullable properties, where JSON null never reaches the
 * serializer. "" is [empty], the other [names] match case-insensitively, and anything else throws,
 * failing the read as it did in the C# app.
 */
abstract class EmptyStringEnumSerializer<T : Enum<T>>(
    serialName: String,
    private val empty: T,
    private val names: Map<T, String>,
) : KSerializer<T> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor(serialName, PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): T {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive || element is JsonNull || !element.isString) {
            throw SerializationException("Unexpected $element when parsing enum ${descriptor.serialName}.")
        }

        val value = element.content
        if (value.isEmpty()) return empty
        return names.entries.firstOrNull { it.value.equals(value, ignoreCase = true) }?.key
            ?: throw SerializationException("Unable to convert \"$value\" to enum ${descriptor.serialName}.")
    }

    override fun serialize(encoder: Encoder, value: T) {
        encoder.encodeString(if (value == empty) "" else names.getValue(value))
    }
}

/**
 * Reddit.Api's JsonColor as the C# app read it (JsonColor.ToHex()): null or "" is no color, and
 * anything else is the CSS value as it is, even if blank.
 */
object JsonColorSerializer : KSerializer<String?> {
    override val descriptor: SerialDescriptor = PrimitiveSerialDescriptor("JsonColor", PrimitiveKind.STRING)

    override fun deserialize(decoder: Decoder): String? {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element is JsonNull) return null
        if (element !is JsonPrimitive || !element.isString) throw SerializationException("Unexpected $element when parsing a color.")
        return element.content.ifEmpty { null }
    }

    override fun serialize(encoder: Encoder, value: String?) {
        (encoder as JsonEncoder).encodeJsonElement(if (value == null) JsonNull else JsonPrimitive(value))
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
 * URLs that Reddit's JSON sends HTML-escaped ("&amp;" for "&") and Reddit.Api's models pass on as
 * they are: a post's link and thumbnail, and video, embed thumbnail and gallery link URLs. The C#
 * app put them into HTML, where the browser decoded them; loaded directly they 403. URLs the models
 * decode themselves (preview and media_metadata images, flair emoji) must not use this, or they're
 * decoded twice.
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
