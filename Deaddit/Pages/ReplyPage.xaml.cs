using Deaddit.Components;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;
using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly IRedditClient _redditClient;

        private readonly ApiThing _replyTo;

        public ReplyPage(ApiThing replyTo, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _redditClient = redditClient;
            _replyTo = replyTo;
            _applicationTheme = applicationTheme;
            _applicationHacks = applicationHacks;

            BindingContext = new ReplyPageViewModel(applicationTheme);
            this.InitializeComponent();

            ApiThing? toRender = replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is ApiComment rc)
                {
                    RedditCommentComponent redditCommentComponent = RedditCommentComponent.Preview(rc, null, redditClient, applicationTheme, applicationHacks, visitTracker, unused, blockConfiguration, configurationService);

                    commentStack.Children.Insert(0, redditCommentComponent);
                }
                else if (toRender is ApiPost post)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, applicationTheme, applicationHacks, visitTracker, unused, blockConfiguration, configurationService);

                    if (!string.IsNullOrWhiteSpace(post.Body))
                    {
                        Border border = new()
                        {
                            Stroke = applicationTheme.TertiaryColor.ToMauiColor(),
                            BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor(),
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
                            HyperlinkColor = applicationTheme.HyperlinkColor.ToMauiColor(),
                            TextColor = applicationTheme.TextColor.ToMauiColor(),
                            TextFontSize = applicationTheme.FontSize,
                            BlockQuoteBorderColor = applicationTheme.TextColor.ToMauiColor(),
                            BlockQuoteBackgroundColor = applicationTheme.SecondaryColor.ToMauiColor(),
                            BlockQuoteTextColor = applicationTheme.TextColor.ToMauiColor(),
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

        public event EventHandler<ReplySubmittedEventArgs>? OnSubmitted;

        public async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void OnPreviewClicked(object? sender, EventArgs e)
        {
        }

        public async void OnSubmitClicked(object? sender, EventArgs e)
        {
            string commentBody = textEditor.Text;

            ApiCommentMeta meta = await _redditClient.Comment(_replyTo, commentBody);

            OnSubmitted?.Invoke(this, new ReplySubmittedEventArgs(_replyTo, meta));

            await Navigation.PopAsync();
        }

        private async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);
            PostTarget resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, _applicationHacks, null, null, null);
        }
    }
}