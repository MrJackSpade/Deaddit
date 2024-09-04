using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;
using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.MAUI.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IRedditClient _redditClient;

        private readonly ApiThing _replyTo;

        private readonly ApplicationTheme _applicationTheme;

        public ReplyPage(ApiThing replyTo, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _redditClient = redditClient;
            _replyTo = replyTo;
            _applicationTheme = applicationTheme;

            BindingContext = new ReplyPageViewModel(applicationTheme);
            this.InitializeComponent();

            ApiThing? toRender = replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is ApiComment rc)
                {
                    RedditCommentComponent redditCommentComponent = RedditCommentComponent.Preview(rc, null, redditClient, applicationTheme, visitTracker, unused, blockConfiguration, configurationService);

                    commentStack.Children.Insert(0, redditCommentComponent);
                }
                else if (toRender is ApiPost post)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, applicationTheme, visitTracker, unused, blockConfiguration, configurationService);

                    if (!string.IsNullOrWhiteSpace(post.Body))
                    {
                        Border border = new()
                        {
                            Stroke = applicationTheme.TertiaryColor,
                            BackgroundColor = applicationTheme.PrimaryColor,
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(10),
                            StrokeThickness = 2,  
                            Padding = new Thickness(10),
                            StrokeShape = new RoundRectangle
                            {
                                CornerRadius = new CornerRadius(5, 5, 5, 5)
                            },
                        };

                        // Content Text as Markdown
                        MarkdownView markdownView = new()
                        {
                            MarkdownText = MarkDownHelper.Clean(post.Body),
                            HyperlinkColor = applicationTheme.HyperlinkColor,
                            TextColor = applicationTheme.TextColor,
                            TextFontSize = applicationTheme.FontSize,
                            BlockQuoteBorderColor = applicationTheme.TextColor,
                            BlockQuoteBackgroundColor = applicationTheme.SecondaryColor,
                            BlockQuoteTextColor = applicationTheme.TextColor,
                            Margin = new Thickness(5)
                        };

                        border.Content = markdownView;

                        markdownView.OnHyperLinkClicked += this.OnHyperLinkClicked;

                        commentStack.Children.Insert(0, border);

                    }

                    // Add to the layout
                    commentStack.Children.Insert(0, redditPostComponent);
                }

                toRender = toRender.Parent;
            } while (toRender != null);
        }

        private async void OnHyperLinkClicked(object sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);
            PostTarget resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, null, null, null);
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