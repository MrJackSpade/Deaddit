using Deaddit.Components;
using Deaddit.Configurations;
using Deaddit.Interfaces;
using Deaddit.PageModels;
using Deaddit.Services;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly AppConfiguration _appConfiguration;

        private readonly LandingPageSubscriptions _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IMarkDownService _markDownService;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

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

        public LandingPage(IAppTheme appTheme, AppConfiguration appConfiguration, IRedditClient redditClient, IConfigurationService configurationService, IMarkDownService markDownService)
        {
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _redditClient = redditClient;
            _appConfiguration = appConfiguration;
            _configuration = configurationService.Read<LandingPageSubscriptions>();
            _configurationService = configurationService;
            _appTheme = appTheme;
            _markDownService = markDownService;
            _selectionTracker = new SelectionTracker();

            BindingContext = _viewModel = new LandingPageViewModel(appTheme);
            InitializeComponent();

            this.mainStack.Add(new SubRedditComponent("All", "r/all", "Hot", redditClient, appTheme, _selectionTracker, _markDownService));
            this.mainStack.Add(new SubRedditComponent("Home", "", "Hot", redditClient, appTheme, _selectionTracker, _markDownService));

            foreach (LandingPageSubscription subscription in _configuration.Subscriptions)
            {
                this.mainStack.Add(new SubRedditComponent(subscription.SubReddit, subscription.Sort, redditClient, appTheme, _selectionTracker, _markDownService));
            }
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

            _configuration.Subscriptions.Add(new LandingPageSubscription()
            {
                SubReddit = result,
                Sort = "Hot"
            });

            _configurationService.Write(_configuration);

            this.mainStack.Add(new SubRedditComponent(result, "Hot", _redditClient, _appTheme, _selectionTracker, _markDownService));
        }
    }
}