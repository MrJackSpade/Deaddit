using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.MAUI.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IRedditClient _redditClient;

        private readonly RedditThing _replyTo;

        public ReplyPage(RedditThing replyTo, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _redditClient = redditClient;
            _replyTo = replyTo;

            BindingContext = new ReplyPageViewModel(applicationTheme);
            this.InitializeComponent();

            RedditThing? toRender = replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is RedditComment)
                {
                    RedditCommentComponent redditCommentComponent = RedditCommentComponent.Preview(toRender, redditClient, applicationTheme, visitTracker, unused, blockConfiguration, configurationService);

                    commentStack.Children.Insert(0, redditCommentComponent);
                }
                else if (toRender is RedditPost post)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, applicationTheme, visitTracker, unused, blockConfiguration, configurationService);
                    commentStack.Children.Insert(0, redditPostComponent);
                }

                toRender = toRender.Parent;
            } while (toRender != null);
        }

        public event EventHandler<ReplySubmittedEventArgs>? OnSubmitted;

        public async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void OnPreviewClicked(object sender, EventArgs e)
        {
        }

        public async void OnSubmitClicked(object sender, EventArgs e)
        {
            string commentBody = textEditor.Text;

            RedditCommentMeta meta = await _redditClient.Comment(_replyTo, commentBody);

            OnSubmitted?.Invoke(this, new ReplySubmittedEventArgs(_replyTo, meta));

            await Navigation.PopAsync();
        }
    }
}