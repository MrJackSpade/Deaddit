package com.mrjackspade.deaddit.ai

import android.util.Log
import com.mrjackspade.deaddit.core.config.AIConfiguration
import com.mrjackspade.deaddit.core.config.ClaudeModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.IOException
import kotlin.math.pow
import kotlin.random.Random

/** A failed request to Claude's API (the C# HttpRequestException). */
class ClaudeApiException(message: String) : IOException(message)

@Serializable
data class ClaudeMessage(val role: String, val content: String)

@Serializable
data class ClaudeMessageRequest(
    @SerialName("max_tokens") val maxTokens: Int = 1024,
    val messages: List<ClaudeMessage> = emptyList(),
    val model: String = ClaudeModel.Claude_Sonnet_4_5.id,
    val system: String? = null,
    val temperature: Double? = null,
)

@Serializable
data class ClaudeContentBlock(val text: String? = null, val type: String? = null)

@Serializable
data class ClaudeUsage(@SerialName("input_tokens") val inputTokens: Int = 0, @SerialName("output_tokens") val outputTokens: Int = 0)

@Serializable
data class ClaudeMessageResponse(
    val content: List<ClaudeContentBlock> = emptyList(),
    val id: String? = null,
    val model: String? = null,
    val role: String? = null,
    @SerialName("stop_reason") val stopReason: String? = null,
    val type: String? = null,
    val usage: ClaudeUsage? = null,
)

/**
 * Claude's Messages API, for AI Assist (the C# ClaudeService). The key is the one configured when
 * this was made, as in C#. Overloaded and gateway errors (502, 503, 504, 529) and failed requests
 * are retried with backoff, as the C# did.
 */
class ClaudeService(http: OkHttpClient, configuration: AIConfiguration) {
    private val apiKey = configuration.apiKey

    val isConfigured: Boolean = !apiKey.isNullOrBlank()

    // The C# HttpClient's requests ran until Claude answered; OkHttp's default per-read limit
    // would cut off a long generation, so this client has none.
    private val http = http.newBuilder()
        .connectTimeout(java.time.Duration.ZERO)
        .readTimeout(java.time.Duration.ZERO)
        .writeTimeout(java.time.Duration.ZERO)
        .build()

    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        encodeDefaults = true
    }

    /** Sends [input] as the user's message under the system [prompt]; the reply's first text. */
    suspend fun sendMessage(prompt: String, input: String, model: String? = null, temperature: Double = 0.0): String {
        val response = sendMessage(listOf(ClaudeMessage("user", input)), systemPrompt = prompt, model = model, temperature = temperature)
        return response.content.firstOrNull()?.text ?: ""
    }

    suspend fun sendMessage(
        messages: List<ClaudeMessage>,
        systemPrompt: String? = null,
        model: String? = null,
        maxTokens: Int = 10000,
        temperature: Double? = 0.0,
        maxRetries: Int = 3,
    ): ClaudeMessageResponse = withContext(Dispatchers.IO) {
        val request = ClaudeMessageRequest(
            maxTokens = maxTokens,
            messages = messages,
            model = model ?: ClaudeModel.Claude_Sonnet_4_5.id,
            system = systemPrompt,
            temperature = temperature,
        )
        val body = json.encodeToString(ClaudeMessageRequest.serializer(), request)

        var retryCount = 0
        val baseDelayMs = 1000L

        while (retryCount <= maxRetries) {
            try {
                val (code, responseBody) = post(body)

                if (code in RETRYABLE) {
                    if (retryCount < maxRetries) {
                        retryCount++
                        val delayMs = baseDelayMs * 2.0.pow(retryCount - 1).toLong() + Random.nextInt(0, 1000)
                        Log.w(TAG, "Retryable error $code. Retry $retryCount/$maxRetries after ${delayMs}ms delay.")
                        delay(delayMs)
                        continue
                    } else {
                        throw ClaudeApiException("Claude API request failed after $maxRetries retries: $code - $responseBody")
                    }
                }

                if (code !in 200..299) {
                    throw ClaudeApiException("Claude API request failed: $code - $responseBody")
                }

                return@withContext json.decodeFromString(ClaudeMessageResponse.serializer(), responseBody)
            } catch (e: IOException) {
                // As the C# "catch (HttpRequestException) when (retryCount < maxRetries)": this
                // also catches the failure thrown just above, so any failed request is retried.
                if (retryCount >= maxRetries) throw e
                retryCount++
                val delayMs = baseDelayMs * 2.0.pow(retryCount - 1).toLong() + Random.nextInt(0, 1000)
                Log.w(TAG, "Network error. Retry $retryCount/$maxRetries after ${delayMs}ms delay.", e)
                delay(delayMs)
            }
        }

        throw ClaudeApiException("Claude API request failed after $maxRetries retries")
    }

    private fun post(body: String): Pair<Int, String> {
        val builder = Request.Builder().url(MESSAGE_URL).post(body.toRequestBody("application/json; charset=utf-8".toMediaType()))
        if (isConfigured) {
            builder.header("x-api-key", apiKey!!)
            builder.header("anthropic-version", "2023-06-01")
        }
        http.newCall(builder.build()).execute().use { response ->
            return response.code to response.body!!.string()
        }
    }

    private companion object {
        const val TAG = "Deaddit"
        const val MESSAGE_URL = "https://api.anthropic.com/v1/messages"
        val RETRYABLE = setOf(502, 503, 504, 529)
    }
}
