package com.mrjackspade.deaddit.core.session

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.double
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.time.Instant
import java.util.Base64

/**
 * A logged-in browser session, in the shape Reddit.Api's BearerToken takes over the bridge.
 * [expiration] is ISO-8601 UTC; [sessionCookies] is the whole reddit.com Cookie header.
 */
@Serializable
data class Session(
    val accessToken: String,
    val authenticatedUser: String,
    val expiration: String? = null,
    val sessionCookies: String? = null,
) {
    val isExpired: Boolean get() = expiration != null && !Instant.now().isBefore(Instant.parse(expiration))
}

/** The claims Deaddit reads from Reddit's token_v2 browser cookie, a JWT. */
data class TokenV2(
    /** False for the anonymous token Reddit issues before login (subject "loid"). */
    val isLoggedIn: Boolean,
    val expiration: Instant,
) {
    val isExpired: Boolean get() = !Instant.now().isBefore(expiration)

    companion object {
        /** Throws IllegalArgumentException if the value isn't a JWT carrying both claims. */
        fun parse(jwt: String): TokenV2 {
            val parts = jwt.split('.')
            require(parts.size == 3) { "token_v2 is not a JWT: expected 3 segments, found ${parts.size}." }

            val claims = Json.parseToJsonElement(String(Base64.getUrlDecoder().decode(parts[1].trimEnd('=')), Charsets.UTF_8)).jsonObject
            val sub = claims["sub"]?.jsonPrimitive?.takeIf { it.isString }?.content
                ?: throw IllegalArgumentException("token_v2 has no string 'sub' claim.")
            // Reddit writes exp with a fractional part.
            val exp = claims["exp"]?.jsonPrimitive?.double
                ?: throw IllegalArgumentException("token_v2 has no numeric 'exp' claim.")

            return TokenV2(sub != "loid", Instant.ofEpochMilli((exp * 1000).toLong()))
        }
    }
}
