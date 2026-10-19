using Deaddit.Core.Utils;

namespace Deaddit.Tests
{
    [TestClass]
    public class UrlHelperTests
    {
        [TestMethod]
        public void GetDisplayHost_ExternalLink_ReturnsHost()
        {
            Assert.AreEqual("example.com", UrlHelper.GetDisplayHost("https://example.com/article"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("/r/pics/comments/abc123/title/")]
        [DataRow("file:///r/pics/comments/abc123/title/")]
        [DataRow("https://i.redd.it/abc.jpg")]
        [DataRow("https://v.redd.it/abc")]
        [DataRow("https://www.reddit.com/gallery/abc123")]
        public void GetDisplayHost_NothingToShow_ReturnsNull(string? url)
        {
            Assert.IsNull(UrlHelper.GetDisplayHost(url));
        }
    }
}
