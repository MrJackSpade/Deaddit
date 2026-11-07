package com.mrjackspade.deaddit.ui

import com.mrjackspade.deaddit.Services

/** Opening a link tapped in a post or comment body (the C# UrlClickLauncher). */
object Links {
    suspend fun open(services: Services, url: String) {
        try {
            if (!services.posts.urls.canLaunch(url, services.posts)) {
                services.dialogs.alert("Alert", "Can not handle url $url")
                return
            }
            services.posts.urls.launch(url, services.posts)
        } catch (e: Exception) {
            services.dialogs.error(e)
        }
    }
}
