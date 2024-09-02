using Deaddit.Components;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;
using Deaddit.PageModels;
using Deaddit.Services;

namespace Deaddit.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly IMarkDownService _markDownService;

        private readonly IRedditClient _redditClient;

        private readonly RedditThing _replyTo;

        public ReplyPage(RedditThing replyTo, IRedditClient redditClient, IAppTheme appTheme, IMarkDownService markDownService)
        {
            _redditClient = redditClient;
            _appTheme = appTheme;
            _markDownService = markDownService;
            _replyTo = replyTo;

            BindingContext = new ReplyPageViewModel(appTheme, replyTo);
            this.InitializeComponent();

            RedditThing toRender = replyTo;
            SelectionTracker unused = new();
            do
            {
                if (toRender is RedditComment)
                {
                    RedditCommentComponent redditCommentComponent = RedditCommentComponent.Preview(toRender, redditClient, appTheme, unused, markDownService);

                    commentStack.Children.Insert(0, redditCommentComponent);
                }
                else if (toRender is RedditPost post)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, appTheme, unused, markDownService);
                    commentStack.Children.Insert(0, redditPostComponent);
                }

                toRender = toRender.Parent;
            } while (toRender != null);
        }

        public event EventHandler<ReplySubmittedEventArgs> OnSubmitted;

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