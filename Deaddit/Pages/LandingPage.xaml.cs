using Deaddit.Components;
using Deaddit.Configurations;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.PageModels;
using Deaddit.Services;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly AppConfiguration _appConfiguration;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IMarkDownService _markDownService;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

        private readonly IBlockConfiguration _blockConfiguration;

        private readonly LandingPageViewModel _viewModel;

        public void OnMenuClicked(object sender, EventArgs e)
        {
            ObjectEditorPage editorPage = new(_appConfiguration, true, _appTheme);

            editorPage.OnSave += this.EditorPage_OnSave;

            Navigation.PushAsync(editorPage);
        }

        private void EditorPage_OnSave(object? sender, EventArguments.ObjectEditorSaveEventArgs e)
        {
            _configurationService.Write(_appConfiguration);
        }

        public LandingPage(IAppTheme appTheme, AppConfiguration appConfiguration, IRedditClient redditClient, IConfigurationService configurationService, IMarkDownService markDownService, IBlockConfiguration blockConfiguration)
        {
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _redditClient = redditClient;
            _appConfiguration = appConfiguration;
            _configuration = configurationService.Read<LandingPageConfiguration>();
            _configurationService = configurationService;
            _blockConfiguration = blockConfiguration;
            _appTheme = appTheme;
            _markDownService = markDownService;
            _selectionTracker = new SelectionTracker();

            BindingContext = _viewModel = new LandingPageViewModel(appTheme);
            this.InitializeComponent();

            this.mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("All", "r/all", "Hot"), redditClient, appTheme, _selectionTracker, _markDownService, _blockConfiguration));
            this.mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("Home", "", "Hot"), redditClient, appTheme, _selectionTracker, _markDownService, _blockConfiguration));

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                SubRedditComponent subRedditComponent = SubRedditComponent.Removable(subscription, redditClient, appTheme, _selectionTracker, _markDownService, _blockConfiguration);
                subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
                this.mainStack.Add(subRedditComponent);
            }
        }

        private void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            this.mainStack.Remove(e.Component);
            _configuration.Subscriptions.Remove(e.Subscription);
            _configurationService.Write(_configuration);
        }

        public async void OnAddClicked(object sender, EventArgs e)
        {
            string result = await this.DisplayPromptAsync("", "Enter a SubReddit");

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if (!result.Contains('/'))
            {
                result = $"r/{result}";
            }

            SubRedditSubscription newSubscription = new()
            {
                SubReddit = result,
                DisplayString = result,
                Sort = "Hot"
            };

            _configuration.Subscriptions.Add(newSubscription);

            _configurationService.Write(_configuration);

            SubRedditComponent subRedditComponent = SubRedditComponent.Removable(newSubscription, _redditClient, _appTheme, _selectionTracker, _markDownService, _blockConfiguration);
            subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
            this.mainStack.Add(subRedditComponent);
        }
    }
}