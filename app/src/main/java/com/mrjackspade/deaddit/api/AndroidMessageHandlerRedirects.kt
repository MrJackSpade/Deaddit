package com.mrjackspade.deaddit.api

import com.mrjackspade.deaddit.core.util.DotNetUri
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import java.io.IOException
import java.net.ProtocolException

/**
 * Redirects followed as the C# app's HttpClient followed them. On Android its handler was AndroidMessageHandler
 * (HttpURLConnection underneath, with redirects handled by the handler itself, HandleRedirect):
 *
 * - 300, 307 and 308 repeat the request; 301, 302 and 303 turn any other method (HEAD and POST included) into a
 *   GET without a body; 304 and a redirect without a Location are handed back as they are; 305, 306 and 309..399
 *   are errors.
 * - Every redirect drops the Authorization header, whatever the host.
 * - The Location is resolved against the URL just requested with .NET's System.Uri, then sent in its escaped form.
 * - The 50th redirect is an error (MaxAutomaticRedirections = 50, checked as `count >= 50`).
 * - http to https and https to http are both followed: AndroidMessageHandler has no scheme check (only .NET's
 *   own SocketsHttpHandler refuses the downgrade, and the C# app on Android never used it).
 *
 * Use it on a client built with [AndroidMessageHandlerRedirects.install], which also turns OkHttp's own redirect
 * following off. Its errors are IOExceptions, as an OkHttp interceptor's must be: anything else escaping an
 * asynchronous call kills the thread it ran on.
 */
class AndroidMessageHandlerRedirects private constructor() : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        var request = chain.request()
        var redirectCounter = 0

        while (true) {
            val response = chain.proceed(request)
            val code = response.code
            var methodChanged = false

            when (code) {
                301, 302, 303 -> methodChanged = request.method != "GET"
                300, 307, 308 -> {}
                304 -> return response
                in 305..399 -> {
                    response.close()
                    throw ProtocolException("HTTP Redirection status code $code not supported")
                }
                else -> return response
            }

            // The first non-empty Location, trimmed.
            val location = response.headers("Location").map { it.trim() }.firstOrNull { it.isNotEmpty() } ?: return response

            redirectCounter++
            if (redirectCounter >= MAX_AUTOMATIC_REDIRECTIONS) {
                response.close()
                throw IOException("Maximum automatic redirections exceeded (allowed $MAX_AUTOMATIC_REDIRECTIONS, redirected $redirectCounter times)")
            }

            val target = resolve(response.request.url.toString(), location)

            val targetUrl = target.absoluteUri.toHttpUrlOrNull()
                ?: run {
                    response.close()
                    throw ProtocolException("Unsupported URL scheme ${target.scheme}")
                }

            response.close()
            request = redirected(request, targetUrl, methodChanged)
        }
    }

    /** The next request: the same headers but Authorization; a GET without body or content headers when the method changes. */
    private fun redirected(request: Request, url: okhttp3.HttpUrl, methodChanged: Boolean): Request {
        val builder = request.newBuilder().url(url).removeHeader("Authorization")
        if (methodChanged) {
            builder.method("GET", null)
            for (name in CONTENT_HEADERS) {
                builder.removeHeader(name)
            }
        }
        return builder.build()
    }

    companion object {
        private const val MAX_AUTOMATIC_REDIRECTIONS = 50

        /** AndroidMessageHandler's known_content_headers: these go with the content, which a GET redirect drops. */
        private val CONTENT_HEADERS = listOf(
            "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location",
            "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified",
        )

        /** [builder] with OkHttp's redirect following replaced by AndroidMessageHandler's. */
        fun install(builder: OkHttpClient.Builder): OkHttpClient.Builder =
            builder.followRedirects(false).followSslRedirects(false).addInterceptor(AndroidMessageHandlerRedirects())

        /**
         * HandleRedirect's target: a Location starting with '/' against the requested URL, else the Location itself
         * when it's absolute, else resolved against the requested URL. A Location .NET can't parse is an error.
         */
        fun resolve(requestUrl: String, location: String): DotNetUri = try {
            val baseUri = DotNetUri.create(requestUrl)
            if (location[0] == '/') {
                DotNetUri.create(baseUri, location)
            } else {
                val uri = DotNetUri.createRelativeOrAbsolute(location)
                if (uri.isAbsoluteUri) uri else DotNetUri.create(baseUri, location)
            }
        } catch (e: Exception) {
            throw IOException("Invalid redirect URI received: $location", e)
        }
    }
}
