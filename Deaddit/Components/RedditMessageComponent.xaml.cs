using Deaddit.Components;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.Partials;
using Deaddit.Pages;
using Deaddit.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.MAUI.Components
{
    public partial class RedditMessageComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiMessage _message;

        private readonly IRedditClient _redditClient;

        private readonly View messageBody;

        public void OnParentTapped(object? sender, TappedEventArgs e)
        {
            SelectionGroup.Toggle(this);
        }

        public RedditMessageComponent(ApiMessage message, bool selectEnabled, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration)
        {
            SelectEnabled = selectEnabled;
            _applicationStyling = applicationTheme;
            BlockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            AppNavigator = appNavigator;
            _message = message;
            SelectionGroup = selectionTracker;

            this.InitializeComponent();

            if (message.Distinguished == DistinguishedKind.Moderator)
            {
                authorLabel.TextColor = applicationTheme.DistinguishedAuthorTextColor.ToMauiColor();
                authorLabel.BackgroundColor = applicationTheme.DistinguishedAuthorBackgroundColor.ToMauiColor();
            }
            else
            {
                authorLabel.TextColor = applicationTheme.TertiaryColor.ToMauiColor();
            }

            authorLabel.Text = message.Author;

            messageContainer.Background = applicationTheme.SecondaryColor.ToMauiColor();

            if (MarkDownHelper.IsMarkDown(_message.Body))
            {
                int markdownIndex = messageContainer.Children.IndexOf(contentLabel);
                messageContainer.Children.RemoveAt(markdownIndex);

                // Content Text as Markdown
                MarkdownView markdownView = new()
                {
                    MarkdownText = MarkDownHelper.Clean(_message.Body),
                    HyperlinkColor = _applicationStyling.HyperlinkColor.ToMauiColor(),
                    TextColor = _applicationStyling.TextColor.ToMauiColor(),
                    H1Color = _applicationStyling.TextColor.ToMauiColor(),
                    H2Color = _applicationStyling.TextColor.ToMauiColor(),
                    H3Color = _applicationStyling.TextColor.ToMauiColor(),
                    TextFontSize = _applicationStyling.FontSize,
                    BlockQuoteBorderColor = _applicationStyling.TextColor.ToMauiColor(),
                    BlockQuoteBackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor(),
                    BlockQuoteTextColor = _applicationStyling.TextColor.ToMauiColor(),
                    Padding = new Thickness(10, 4, 0, 10)
                };

                markdownView.OnHyperLinkClicked += this.OnHyperLinkClicked;

                // Add to the layout
                messageContainer.Children.Insert(markdownIndex, markdownView);
                messageBody = markdownView;
            }
            else
            {
                contentLabel.Text = MarkDownHelper.Clean(_message.Body);
                contentLabel.TextColor = _applicationStyling.TextColor.ToMauiColor();
                contentLabel.FontSize = _applicationStyling.FontSize;
                contentLabel.Padding = new Thickness(10, 4, 0, 10);
                messageBody = contentLabel;
            }
        }

        public event EventHandler<OnDeleteClickedEventArgs>? OnDelete;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        public ApiPost Post { get; }

        public bool SelectEnabled { get; private set; }

        public SelectionGroup SelectionGroup { get; }

        public async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostItems resource = UrlHelper.Resolve(e.Url);

            await Navigation.OpenResource(resource, AppNavigator);
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await AppNavigator.OpenReplyPage(_message);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
        }

        void ISelectionGroupItem.Select()
        {

        }

        void ISelectionGroupItem.Unselect()
        {

        }

        private readonly Dictionary<string, Stream> _cachedImageStreams = [];

        private async Task<Stream> GetImageStream(CancellationToken c, string url)
        {
            if (!_cachedImageStreams.TryGetValue(url, out Stream cachedImageStream))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    cachedImageStream = await ImageHelper.ResizeLargeImageWithContainFitAsync(url, (int)Application.Current.Windows[0].Height);
                    _cachedImageStreams.Add(url, cachedImageStream);
                }
            }

            if (cachedImageStream is null)
            {
                return null;
            }

            cachedImageStream.Seek(0, SeekOrigin.Begin);

            return cachedImageStream;
        }

        internal void LoadImages(bool recursive = false)
        {
            if (messageBody is MarkdownView mv)
            {
                foreach (LinkSpan linkSpan in mv.LinkSpans)
                {
                    Grid? grid = linkSpan.Element.Closest<Grid>();

                    Label? label = linkSpan.Element.Closest<Label>();

                    if (grid is null || label is null)
                    {
                        Debug.WriteLine("Could not find image grid or label");
                        continue;
                    }

                    PostItems item = UrlHelper.Resolve(linkSpan.Url);

                    if (item.Kind == PostTargetKind.Image)
                    {
                        grid.Children.InsertAfter(
                            label,
                            new Image()
                            {
                                Source = ImageSource.FromStream(async (c) => await this.GetImageStream(c, linkSpan.Url)),
                                MaximumWidthRequest = messageBody.Width,
                                Aspect = Aspect.AspectFit,
                                MaximumHeightRequest = Application.Current.Windows[0].Height
                            });

                        grid.Children.Remove(label);
                    }
                }
            }

            if (recursive)
            {
                foreach (RedditMessageComponent element in messageContainer.OfType<RedditMessageComponent>())
                {
                    element.LoadImages(true);
                }
            }
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {

        }
    }
}