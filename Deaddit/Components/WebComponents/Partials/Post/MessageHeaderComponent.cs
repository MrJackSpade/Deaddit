using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Api;
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

            AuthorNameComponent authorSpan = new(message.Author, applicationStyling, message.Distinguished, false);

            Children.Add(authorSpan);

            SpanComponent messageSubject = new()
            {
                InnerText = "- " + message.Subject,
                Color = applicationStyling.TextColor.ToHex(),
                FontWeight = "bold"
            };

            Children.Add(messageSubject);
        }
    }
}