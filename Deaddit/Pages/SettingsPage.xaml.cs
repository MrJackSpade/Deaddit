using CommunityToolkit.Maui.Storage;
using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly IConfigurationService _configurationService;

        private readonly IDisplayMessages _displayMessages;

        private readonly IHistoryTracker _historyTracker;

        private readonly JsonSerializerOptions _jsonOptions;

        private readonly SavePathConfiguration _savePaths;

        private readonly UserTagCollection _userTags;

        public SettingsPage(IAppNavigator appNavigator, IConfigurationService configurationService, IHistoryTracker historyTracker, UserTagCollection userTags, IDisplayMessages displayMessages, SavePathConfiguration savePaths, ApplicationStyling applicationStyling)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _configurationService = configurationService;
            _historyTracker = historyTracker;
            _userTags = userTags;
            _displayMessages = displayMessages;
            _savePaths = savePaths;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());

            BindingContext = new LandingPageViewModel(applicationStyling);
            this.InitializeComponent();

            navigationBar.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            titleLabel.TextColor = applicationStyling.TextColor.ToMauiColor();
            backButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            versionLabel.TextColor = applicationStyling.SubTextColor.ToMauiColor();
            versionLabel.Text = $"Version {VersionInfo.Version}";

            Color textColor = applicationStyling.TextColor.ToMauiColor();
            Color subTextColor = applicationStyling.SubTextColor.ToMauiColor();
            Color bgColor = applicationStyling.SecondaryColor.ToMauiColor();

            foreach (Button button in new[] { editConfigButton, exportButton, importButton, pickImageFolderButton, clearImageFolderButton, pickVideoFolderButton, clearVideoFolderButton })
            {
                button.TextColor = textColor;
                button.BackgroundColor = bgColor;
            }

            foreach (Label heading in new[] { imageFolderHeading, videoFolderHeading })
            {
                heading.TextColor = textColor;
            }

            foreach (Label pathLabel in new[] { imageFolderLabel, videoFolderLabel })
            {
                pathLabel.TextColor = subTextColor;
            }

            this.RefreshFolderLabels();
        }

        private void RefreshFolderLabels()
        {
            imageFolderLabel.Text = string.IsNullOrWhiteSpace(_savePaths.DefaultImageDirectory) ? "(use system save dialog)" : _savePaths.DefaultImageDirectory;
            videoFolderLabel.Text = string.IsNullOrWhiteSpace(_savePaths.DefaultVideoDirectory) ? "(use system save dialog)" : _savePaths.DefaultVideoDirectory;
        }

        private async void OnPickImageFolderClicked(object? sender, EventArgs e)
        {
            await this.PickFolderInto(value => _savePaths.DefaultImageDirectory = value);
        }

        private async void OnPickVideoFolderClicked(object? sender, EventArgs e)
        {
            await this.PickFolderInto(value => _savePaths.DefaultVideoDirectory = value);
        }

        private void OnClearImageFolderClicked(object? sender, EventArgs e)
        {
            _savePaths.DefaultImageDirectory = null;
            _configurationService.Write(_savePaths);
            this.RefreshFolderLabels();
        }

        private void OnClearVideoFolderClicked(object? sender, EventArgs e)
        {
            _savePaths.DefaultVideoDirectory = null;
            _configurationService.Write(_savePaths);
            this.RefreshFolderLabels();
        }

        private async Task PickFolderInto(Action<string?> assign)
        {
            try
            {
                FolderPickerResult result = await FolderPicker.Default.PickAsync(CancellationToken.None);

                if (!result.IsSuccessful || result.Folder is null)
                {
                    return;
                }

                assign(result.Folder.Path);
                _configurationService.Write(_savePaths);
                this.RefreshFolderLabels();
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnEditConfigurationClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenObjectEditor();
        }

        private async void OnExportClicked(object? sender, EventArgs e)
        {
            try
            {
                SettingsExportData exportData = new()
                {
                    Styling = _configurationService.Read<ApplicationStyling>(),
                    AIConfiguration = _configurationService.Read<AIConfiguration>(),
                    BlockConfiguration = _configurationService.Read<BlockConfiguration>(),
                    ApplicationHacks = _configurationService.Read<ApplicationHacks>(),
                    SavePaths = _configurationService.Read<SavePathConfiguration>(),
                    LandingPageConfiguration = _configurationService.Read<LandingPageConfiguration>(),
                    PostHistory = [.. _historyTracker.GetHistory()],
                    BearerTokenJson = Preferences.Get("reddit_bearer_token", null),
                    UserTags = new Dictionary<string, string>(_userTags.GetAllTags())
                };

                string json = JsonSerializer.Serialize(exportData, _jsonOptions);
                using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
                await FileSaver.Default.SaveAsync("deaddit_settings.json", stream, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _displayMessages.DisplayException(ex);
            }
        }

        private async void OnImportClicked(object? sender, EventArgs e)
        {
            try
            {
                bool confirmed = await this.DisplayAlert(
                    "Import Settings",
                    "This will overwrite all current settings. The app will need to restart afterward.",
                    "Continue",
                    "Cancel");

                if (!confirmed)
                {
                    return;
                }

                FileResult? file = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select settings file"
                });

                if (file == null)
                {
                    return;
                }

                using Stream stream = await file.OpenReadAsync();
                using StreamReader reader = new(stream);
                string json = await reader.ReadToEndAsync();

                SettingsExportData? importData = JsonSerializer.Deserialize<SettingsExportData>(json, _jsonOptions);

                if (importData == null)
                {
                    await this.DisplayAlert("Error", "Failed to read settings file.", "OK");
                    return;
                }

                _configurationService.Write(importData.Styling);
                _configurationService.Write(importData.AIConfiguration);
                _configurationService.Write(importData.BlockConfiguration);
                _configurationService.Write(importData.ApplicationHacks);
                _configurationService.Write(importData.SavePaths);
                _configurationService.Write(importData.LandingPageConfiguration);

                if (importData.PostHistory != null)
                {
                    ((PreferencesHistoryTracker)_historyTracker).ImportHistory(importData.PostHistory);
                }

                if (importData.BearerTokenJson != null)
                {
                    Preferences.Set("reddit_bearer_token", importData.BearerTokenJson);
                }

                if (importData.UserTags != null)
                {
                    _userTags.ImportTags(importData.UserTags);
                }

                await this.DisplayAlert("Success", "Settings imported. Please restart the app for all changes to take effect.", "OK");
            }
            catch (Exception ex)
            {
                _displayMessages.DisplayException(ex);
            }
        }
    }
}
