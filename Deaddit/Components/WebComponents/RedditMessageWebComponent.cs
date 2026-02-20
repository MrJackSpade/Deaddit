using Deaddit.Components.WebComponents.Partials.Comment;
using Deaddit.Components.WebComponents.Partials.Message;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Blocking;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.Core.Utils.Validation;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Deaddit.Utils;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Reddit.Api.Interfaces;
using Reddit.Api.Models.Api;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-message")]
    public class RedditMessageWebComponent : DivComponent, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly MessageBarComponent _bottomBar;

        private readonly HtmlBodyComponent _messageBody;

        private readonly DivComponent _commentContainer;

        private readonly ApiMessage _message;

        private readonly MessageHeaderComponent _messageHeader;

        private readonly MultiSelector _multiselector;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly RepliesContainerComponent _replies;

        private readonly TopBarComponent _topBar;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        public bool SelectEnabled { get; private set; }

        public SelectionGroup SelectionGroup { get; private set; }

        public event EventHandler<OnDeleteClickedEventArgs> OnDelete;

        public RedditMessageWebComponent(ApiMessage message, ISelectBoxDisplay selectBoxDisplay, INavigation navigation, AppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup selectionGroup, BlockConfiguration blockConfiguration)
        {
            _multiselector = new MultiSelector(selectBoxDisplay);
            _message = Ensure.NotNull(message);
            SelectEnabled = selectionGroup != null;
            AppNavigator = appNavigator;
            _redditClient = redditClient;
            _applicationStyling = applicationStyling;
            SelectionGroup = selectionGroup;
            BlockConfiguration = blockConfiguration;
            _navigation = navigation;

            _commentContainer = new DivComponent()
            {
                Display = "flex",
                FlexDirection = "column",
            };

            _messageBody = new HtmlBodyComponent(message.BodyHtml, applicationStyling);

            _replies = new RepliesContainerComponent(_applicationStyling);

            _messageHeader = new MessageHeaderComponent(applicationStyling, message);

            _topBar = new TopBarComponent();

            _bottomBar = new MessageBarComponent(applicationStyling);

            _bottomBar.OnMoreClicked += this.OnMoreClicked;
            _bottomBar.OnReplyClicked += this.OnReplyClicked;

            _commentContainer.Children.Add(_topBar);
            _commentContainer.Children.Add(_messageHeader);
            _commentContainer.Children.Add(_messageBody);
            _commentContainer.Children.Add(_bottomBar);

            Children.Add(_commentContainer);
            Children.Add(_replies);

            BoxSizing = "border-box";
            PaddingLeft = "5px";
            PaddingTop = "5px";
            PaddingBottom = "5px";

            Display = "flex";
            FlexDirection = "column";

            _commentContainer.OnClick += this.SelectClick;
        }

        public async void OnMoreClicked(object? sender, EventArgs e)
        {
            if (!string.Equals(_redditClient.LoggedInUser, _message.Author, StringComparison.OrdinalIgnoreCase))
            {
                await _multiselector.Select(
                    "Select:",
                    new($"Block /u/{_message.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_message))),
                    new($"View /u/{_message.Author}", async () => await AppNavigator.OpenUser(_message.Author))
                );
            }
            else
            {
                // No action needed for the else block based on the provided code
            }
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ApiUser author = await _redditClient.GetUserData(_message.Author);
            await AppNavigator.OpenMessagePage(author, _message);
        }

        public async Task Select()
        {
            _commentContainer.BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _topBar.Display = "flex";
            _bottomBar.Display = "flex";

            await _redditClient.MarkRead(_message, true);
        }

        public Task Unselect()
        {
            _commentContainer.BackgroundColor = null;
            _topBar.Display = "none";
            _bottomBar.Display = "none";
            return Task.CompletedTask;
        }

        internal void LoadImages(bool recursive = false)
        {
            throw new NotImplementedException();
        }

        private void EditPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            _message.Body = e.NewComment.Body;

            _messageBody.InnerText = e.NewComment.Body;
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            WebObjectEditorPage objectEditorPage = await AppNavigator.OpenObjectEditor(blockRule);
        }

        private async void SelectClick(object? sender, EventArgs e)
        {
            if (SelectionGroup != null)
            {
                await SelectionGroup.Select(this);
            }
        }
    }
}