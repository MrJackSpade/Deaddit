using Deaddit.Components.WebComponents.Partials.User;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Maui.WebComponents.Extensions;

namespace Deaddit.Pages
{
    public partial class MessagePage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly IDisplayMessages _displayMessages;

        private readonly IRedditClient _redditClient;

        private readonly ApiUser _user;

        private readonly ApiMessage? _replyTo;

        public event EventHandler<MessageSubmittedEventArgs>? OnSubmitted;

        public MessagePage(ApiUser user, ApiMessage? replyTo, IDisplayMessages displayExceptions, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling)
        {
            _redditClient = redditClient;
            _user = user;
            _replyTo = replyTo;
            _displayMessages = displayExceptions;
            _appNavigator = appNavigator;

            BindingContext = new ReplyPageViewModel(applicationStyling);

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            SelectionGroup unused = new();

            webElement.AddChild(new UserHeader(user, appNavigator, applicationStyling, false));

            if (replyTo is not null)
            {
                webElement.AddChild(_appNavigator.CreateMessageWebComponent(replyTo, null));
            }
        }

        public async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void OnPreviewClicked(object? sender, EventArgs e)
        {
        }

        public async void OnSubmitClicked(object? sender, EventArgs e)
        {
            try
            {
                string subject = subjectEditor.Text;
                string body = bodyEditor.Text;

                await _redditClient.Message(_user, subject, body);

                OnSubmitted?.Invoke(this, new MessageSubmittedEventArgs(_replyTo, new ApiMessage()
                {
                    Subject = subject,
                    Body = body
                }));

                await _displayMessages.DisplayMessage("Message sent");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                if (!(await _displayMessages.DisplayException(ex)))
                {
                    throw;
                }
            }
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayMessages.DisplayException(e);
        }
    }
}