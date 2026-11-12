package com.mrjackspade.deaddit.handlers

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.ui.Pages

/** Reddit galleries: every image, in the gallery's order. */
class RedditGalleryHandler(private val services: Services) : PostHandler {
    override fun canDownload(post: Post, caller: PostHandlers) = post.galleryData?.items?.any() ?: false
    override fun canLaunch(post: Post, caller: PostHandlers) = post.galleryData?.items?.any() ?: false
    override fun canShare(post: Post, caller: PostHandlers) = post.isGallery

    override suspend fun download(post: Post, caller: PostHandlers) =
        services.media.save(getSortedGalleryItems(post), services.media.gifConverter())

    override suspend fun launch(post: Post, caller: PostHandlers) = Pages.openImages(services, getSortedGalleryItems(post))

    override suspend fun share(post: Post, caller: PostHandlers) =
        services.media.shareDownloads(getSortedGalleryItems(post), services.media.gifConverter())

    private fun getSortedGalleryItems(post: Post): List<FileDownload> {
        val toReturn = mutableListOf<FileDownload>()

        // As in the C#: a gallery without gallery_data or media_metadata is an error here.
        val galleryItems = post.galleryData!!.items.map { it.mediaId }
        val mediaMeta = post.mediaMetadata!!

        // Metadata not in the gallery's list sorts first (IndexOf gives -1), as OrderBy did; the sort is stable.
        val sortedMedia = mediaMeta.values.sortedBy { galleryItems.indexOf(it.id) }

        for (imageUrl in sortedMedia.map { it.source?.url ?: it.source?.gif }) {
            if (!imageUrl.isNullOrBlank()) {
                toReturn.add(FileDownload.of("${post.id}_${toReturn.size}${UrlHelper.getExtension(imageUrl)}", imageUrl))
            }
        }

        return toReturn
    }
}

/** Reddit-hosted videos: the DASH playlist, saved and shared muxed into an MP4. */
class RedditVideoHandler(private val services: Services) : PostHandler {
    override fun canDownload(post: Post, caller: PostHandlers) = post.media?.redditVideo != null
    override fun canLaunch(post: Post, caller: PostHandlers) = post.media?.redditVideo != null
    override fun canShare(post: Post, caller: PostHandlers) = post.media?.redditVideo != null

    // No GIF converter here: the C# saved and shared Reddit videos without one.
    override suspend fun download(post: Post, caller: PostHandlers) = services.media.save(listOf(getDownload(post)))

    override suspend fun launch(post: Post, caller: PostHandlers) = Pages.openVideo(services, getDownload(post))

    override suspend fun share(post: Post, caller: PostHandlers) = services.media.shareDownloads(listOf(getDownload(post)))

    private fun getDownload(post: Post): FileDownload {
        val dashUrl = requireNotNull(post.media?.redditVideo?.dashUrl) { "The post's Reddit video has no DASH URL." }
        return FileDownload("${post.id}.mp4", dashUrl, dashUrl)
    }
}
