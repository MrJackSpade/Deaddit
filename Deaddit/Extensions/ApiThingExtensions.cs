using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Extensions
{
    public static class ApiThingExtensions
    {
        public static string GetTitleOrEmpty(this ApiThing thing)
        {
            return (thing as ApiPost)?.Title ?? string.Empty;
        }

        public static string GetUrlOrEmpty(this ApiThing thing)
        {
            if (!Uri.TryCreate((thing as ApiPost)?.Url, UriKind.Absolute, out Uri? _))
            {
                return string.Empty;
            }

            return (thing as ApiPost)?.Url ?? string.Empty;
        }
    }
}