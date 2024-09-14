using Deaddit.Components;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;
using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly IRedditClient _redditClient;

        private readonly ApiThing _replyTo;

        private readonly ApiThing _toEdit;

        public ReplyPage(ApiThing? replyTo, ApiThing? toEdit, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme)
        {
            _redditClient = redditClient;
            _replyTo = replyTo ?? toEdit.Parent;
            _toEdit = toEdit;
            _appNavigator = appNavigator;

            BindingContext = new ReplyPageViewModel(applicationTheme);
            this.InitializeComponent();

            ApiThing? toRender = _replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is ApiComment rc)
                {
                    RedditCommentComponent redditCommentComponent = _appNavigator.CreateCommentComponent(rc, null, unused);

                    commentStack.Children.Insert(0, redditCommentComponent);
                }
                else if (toRender is ApiPost post)
                {
                    RedditPostComponent redditPostComponent = _appNavigator.CreatePostComponent(post, null);

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

            if (toEdit != null)
            {
                textEditor.Text = toEdit.Body;
            }
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

            OnSubmitted?.Invoke(this, new ReplySubmittedEventArgs(_replyTo, meta));

            await Navigation.PopAsync();
        }

        private async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);
            PostItems resource = RedditPostExtensions.Resolve(e.Url);

            await Navigation.OpenResource(resource, _appNavigator);
        }
    }
}