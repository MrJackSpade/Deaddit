package com.mrjackspade.deaddit.api

import android.util.Base64
import android.util.Log
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.put
import okhttp3.Headers
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * Reddit.Api, the C# library, compiled to native code (libredditapi.so) and reached through the
 * JNI shim in app/src/main/cpp. All of Reddit.Api's logic runs in .NET; this side only moves
 * HTTP bytes for it (native .NET on Android has no TLS of its own) and asks for a fresh login.
 */
object NativeRedditApi {
    private const val TAG = "RedditApi"

    // OkHttp limits connect/read/write to 10s by default; 0 removes those limits.
    // Redirects are followed as .NET's HttpClient follows them (Reddit 301s /r/All to /r/all),
    // except from https to http, which .NET refuses too.
    private val http = OkHttpClient.Builder()
        .followRedirects(true)
        .followSslRedirects(false)
        .connectTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .readTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .writeTimeout(0, java.util.concurrent.TimeUnit.MILLISECONDS)
        .build()

    /** Supplies a fresh session when Reddit rejects the current one; null if the user declines. */
    @Volatile
    var sessionRefresher: (() -> String?)? = null

    /** [userAgent] null keeps Reddit.Api's default. */
    fun init(userAgent: String?) {
        System.loadLibrary("deadditbridge")
        nativeInit(userAgent?.toByteArray(Charsets.UTF_8))?.let {
            throw RedditApiException(Json.parseToJsonElement(String(it, Charsets.UTF_8)).jsonObject["error"]!!.jsonObject)
        }
    }

    /** Runs one Reddit.Api command. Blocks: call it off the main thread. */
    fun call(method: String, args: JsonObject = JsonObject(emptyMap())): JsonElement {
        val request = buildJsonObject {
            put("method", method)
            put("args", args)
        }.toString()

        val response = Json.parseToJsonElement(String(nativeCall(request.toByteArray(Charsets.UTF_8)), Charsets.UTF_8)).jsonObject

        response["error"]?.let { error ->
            val e = RedditApiException(error.jsonObject)
            Log.e(TAG, "$method failed: ${e.message}")
            throw e
        }

        return response["result"] ?: JsonNull
    }

    @JvmStatic
    private external fun nativeInit(userAgent: ByteArray?): ByteArray?

    @JvmStatic
    private external fun nativeCall(requestJson: ByteArray): ByteArray

    /** Called by .NET for every HTTP request Reddit.Api makes. Never throws: failures go back as JSON. */
    @JvmStatic
    fun send(requestJson: ByteArray): ByteArray {
        val result = try {
            val request = Json.parseToJsonElement(String(requestJson, Charsets.UTF_8)).jsonObject
            val method = request["method"]!!.jsonPrimitive.content
            val url = request["url"]!!.jsonPrimitive.content
            val headers = Headers.Builder()
            var contentType: String? = null

            for (header in request["headers"]!!.jsonArray) {
                val pair = header.jsonArray
                val name = pair[0].jsonPrimitive.content
                val value = pair[1].jsonPrimitive.content
                if (name.equals("Content-Type", ignoreCase = true)) contentType = value
                if (!name.equals("Content-Length", ignoreCase = true)) headers.add(name, value)
            }

            val body = request["bodyBase64"]?.jsonPrimitive?.content
                ?.let { Base64.decode(it, Base64.NO_WRAP).toRequestBody(contentType?.toMediaTypeOrNull()) }

            http.newCall(Request.Builder().url(url).headers(headers.build()).method(method, body).build()).execute().use { response ->
                buildJsonObject {
                    put("status", response.code)
                    put("headers", JsonArray(response.headers.map { (name, value) -> JsonArray(listOf(JsonPrimitive(name), JsonPrimitive(value))) }))
                    put("bodyBase64", Base64.encodeToString(response.body!!.bytes(), Base64.NO_WRAP))
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "transport failed", e)
            buildJsonObject { put("error", e.toString()) }
        }

        return result.toString().toByteArray(Charsets.UTF_8)
    }

    /** Called by .NET when Reddit rejects the session. Never throws. */
    @JvmStatic
    fun refreshSession(): ByteArray? {
        return try {
            sessionRefresher?.invoke()?.toByteArray(Charsets.UTF_8)
        } catch (e: Exception) {
            Log.e(TAG, "session refresh failed", e)
            null
        }
    }
}

/** A Reddit.Api call that failed in .NET, with what .NET reported. */
class RedditApiException(val error: JsonObject) : Exception(describe(error)) {
    val type: String? = error["type"]?.jsonPrimitive?.content
    val status: Int? = error["status"]?.let { if (it is JsonNull) null else it.jsonPrimitive.content.toInt() }

    private companion object {
        /** The .NET message, plus its full exception text (type, inner exceptions, stack) when given. */
        fun describe(error: JsonObject): String {
            val message = error["message"]?.jsonPrimitive?.content ?: "Reddit.Api failed without a message."
            val detail = error["detail"]?.jsonPrimitive?.content ?: return message
            return "$message\n$detail"
        }
    }
}
