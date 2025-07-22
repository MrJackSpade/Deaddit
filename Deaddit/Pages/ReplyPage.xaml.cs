using Deaddit.Components.WebComponents;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Reddit.Api.Interfaces;
using Reddit.Api.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;

namespace Deaddit.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly IDisplayMessages _displayExceptions;

        private readonly IRedditClient _redditClient;

        private readonly ApiThing _replyTo;

        private readonly ApiThing _toEdit;

        public event EventHandler<ReplySubmittedEventArgs>? OnSubmitted;

        public ReplyPage(ApiThing? replyTo, ApiThing? toEdit, IDisplayMessages displayExceptions, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling)
        {
            _redditClient = redditClient;
            _replyTo = replyTo ?? toEdit.Parent;
            _displayExceptions = displayExceptions;
            _toEdit = toEdit;
            _appNavigator = appNavigator;

            BindingContext = new ReplyPageViewModel(applicationStyling);

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            ApiThing? toRender = _replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is ApiComment rc)
                {
                    RedditCommentWebComponent redditCommentComponent = _appNavigator.CreateCommentWebComponent(rc, null, unused);

                    webElement.InsertChild(0, redditCommentComponent);
                }
                else if (toRender is ApiPost post)
                {
                    RedditPostWebComponent redditPostComponent = _appNavigator.CreatePostWebComponent(post, PostState.None, null);

                    if (!string.IsNullOrWhiteSpace(post.Body))
                    {
                        webElement.InsertChild(0, new PostBodyComponent(post, applicationStyling));
                    }

                    webElement.InsertChild(0, redditPostComponent);
                }

                toRender = toRender.Parent;
            } while (toRender != null);

            if (toEdit != null)
            {
                textEditor.Text = toEdit.Body;
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

            ApiComment meta;

            if (_toEdit is ApiComment comment)
            {
                comment.Body = textEditor.Text;
                meta = await _redditClient.Update(comment);
            }
            else
            {
                string commentBody = textEditor.Text;
                meta = await _redditClient.Comment(_replyTo, commentBody);
            }

            if (meta != null)
            {
                OnSubmitted?.Invoke(this, new ReplySubmittedEventArgs(_replyTo, meta));

                await Navigation.PopAsync();
            }
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayExceptions.DisplayException(e);
        }
    }
}