using Deaddit.Core.Reddit.Mapping;
using Deaddit.Core.Reddit.Models.Api;
using Reddit.Api.Models.Json.Listings;

namespace Deaddit.Tests
{
    [TestClass]
    public class RedditModelMapperTests
    {
        [TestMethod]
        public void Map_Link_WithoutMedia_ShouldHaveNullMedia()
        {
            // Arrange
            Link link = new()
            {
                Id = "test789",
                Name = "t3_test789",
                Title = "Text Post",
                Author = "testuser",
                Subreddit = "test",
                Permalink = "/r/test/comments/test789/text_post/",
                Media = null,
                SecureMedia = null
            };

            // Act
            ApiPost result = RedditModelMapper.Map(link);

            // Assert
            Assert.IsNull(result.Media, "Media should be null for posts without media");
        }

        [TestMethod]
        public void Map_Link_WithRedditVideo_ShouldMapMedia()
        {
            // Arrange
            Link link = new()
            {
                Id = "test123",
                Name = "t3_test123",
                Title = "Test Video Post",
                Author = "testuser",
                Subreddit = "test",
                Permalink = "/r/test/comments/test123/test_video_post/",
                Media = new Media
                {
                    RedditVideo = new RedditVideo
                    {
                        DashUrl = "https://v.redd.it/test123/DASHPlaylist.mpd",
                        HlsUrl = "https://v.redd.it/test123/HLSPlaylist.m3u8",
                        FallbackUrl = "https://v.redd.it/test123/DASH_720.mp4",
                        Duration = 30,
                        Height = 720,
                        Width = 1280,
                        IsGif = false
                    }
                }
            };

            // Act
            ApiPost result = RedditModelMapper.Map(link);

            // Assert
            Assert.IsNotNull(result, "Mapped ApiPost should not be null");
            Assert.IsNotNull(result.Media, "Media should be mapped");
            Assert.IsNotNull(result.Media.RedditVideo, "RedditVideo should be mapped");
            Assert.AreEqual("https://v.redd.it/test123/DASHPlaylist.mpd", result.Media.RedditVideo.DashUrl);
            Assert.AreEqual("https://v.redd.it/test123/HLSPlaylist.m3u8", result.Media.RedditVideo.HlsUrl);
            Assert.AreEqual("https://v.redd.it/test123/DASH_720.mp4", result.Media.RedditVideo.FallbackUrl);
        }

        [TestMethod]
        public void Map_Link_WithSecureMedia_ShouldMapSecureMedia()
        {
            // Arrange - Some posts only have SecureMedia
            Link link = new()
            {
                Id = "test456",
                Name = "t3_test456",
                Title = "Secure Video Post",
                Author = "testuser",
                Subreddit = "test",
                Permalink = "/r/test/comments/test456/secure_video_post/",
                Media = null,
                SecureMedia = new Media
                {
                    RedditVideo = new RedditVideo
                    {
                        DashUrl = "https://v.redd.it/test456/DASHPlaylist.mpd",
                        HlsUrl = "https://v.redd.it/test456/HLSPlaylist.m3u8",
                        FallbackUrl = "https://v.redd.it/test456/DASH_720.mp4"
                    }
                }
            };

            // Act
            ApiPost result = RedditModelMapper.Map(link);

            // Assert
            Assert.IsNull(result.Media, "Media should be null when source Media is null");
            Assert.IsNotNull(result.SecureMedia, "SecureMedia should be mapped");
            Assert.IsNotNull(result.SecureMedia.RedditVideo, "RedditVideo from SecureMedia should be mapped");
            Assert.AreEqual("https://v.redd.it/test456/DASHPlaylist.mpd", result.SecureMedia.RedditVideo.DashUrl);
        }
    }
}