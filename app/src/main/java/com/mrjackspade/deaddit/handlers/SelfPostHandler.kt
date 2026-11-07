package com.mrjackspade.deaddit.handlers

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.link
import com.mrjackspade.deaddit.ui.Pages

/** Text posts open their thread, and share as their link. */
class SelfPostHandler(private val services: Services) : PostHandler {
    override fun canDownload(post: Post, caller: PostHandlers) = false
    override fun canLaunch(post: Post, caller: PostHandlers) = post.isSelf
    override fun canShare(post: Post, caller: PostHandlers) = post.isSelf

    override suspend fun download(post: Post, caller: PostHandlers) = throw UnsupportedOperationException("Text posts have nothing to download.")

    override suspend fun launch(post: Post, caller: PostHandlers) = Pages.openPost(services, post)

    override suspend fun share(post: Post, caller: PostHandlers) = services.platform.shareText(post.link, post.title)
}
