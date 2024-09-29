using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    public class MessageHeaderComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiMessage _message;

        public MessageHeaderComponent(ApplicationStyling applicationStyling, ApiMessage message)
        {
            _applicationStyling = applicationStyling;
            _message = message;

            SpanComponent authorSpan = new()
            {
                InnerText = _message.Author,
                Color = _applicationStyling.SubTextColor.ToHex(),
                MarginRight = "5px"
            };

            Children.Add(authorSpan);
        }
    }
}