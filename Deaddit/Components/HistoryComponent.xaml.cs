using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.Interfaces;

namespace Deaddit.Components
{
    public partial class HistoryComponent : ContentView
    {
        private readonly IAppNavigator _appNavigator;

        public HistoryComponent(IAppNavigator appNavigator, ApplicationStyling applicationStyling)
        {
            _appNavigator = appNavigator;

            BindingContext = new HistoryComponentViewModel(applicationStyling);
            this.InitializeComponent();
        }

        private async void OnTapped(object? sender, TappedEventArgs e)
        {
            await _appNavigator.OpenHistory();
        }
    }

    public class HistoryComponentViewModel
    {
        public Color SecondaryColor { get; }

        public Color TextColor { get; }

        public HistoryComponentViewModel(ApplicationStyling applicationStyling)
        {
            SecondaryColor = applicationStyling.SecondaryColor.ToMauiColor();
            TextColor = applicationStyling.TextColor.ToMauiColor();
        }
    }
}
