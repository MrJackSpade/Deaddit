using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components
{
    public partial class MoreCommentsComponent : ContentView
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly MoreCommentsComponentViewModel _commentViewModel;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly RedditThing _thing;

        private readonly IVisitTracker _visitTracker;

        public MoreCommentsComponent(RedditComment comment, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _thing = comment;
            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _selectionGroup = selectionTracker;
            BindingContext = _commentViewModel = new MoreCommentsComponentViewModel($"More {comment.Count}", applicationTheme);
            this.InitializeComponent();
        }

        public bool SelectEnabled { get; private set; }

        public async void OnMoreClicked(object sender, EventArgs e)
        {

        }

        public void OnParentTapped(object sender, EventArgs e)
        {
        }
    }
}