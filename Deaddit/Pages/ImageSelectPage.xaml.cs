using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Models;
using Deaddit.Extensions;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using Maui.WebComponents.Extensions;

namespace Deaddit.Pages
{
    public partial class ImageSelectPage : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly Dictionary<InputComponent, FileDownload> _checkboxes = [];

        private readonly List<FileDownload> _items;

        private readonly HashSet<FileDownload> _selected = [];

        private readonly TaskCompletionSource<List<FileDownload>?> _selection = new();

        private bool _resolved;

        public ImageSelectPage(List<FileDownload> items, ApplicationStyling applicationStyling)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _items = items;
            _applicationStyling = applicationStyling;

            this.InitializeComponent();

            navigationBar.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            titleLabel.TextColor = applicationStyling.TextColor.ToMauiColor();
            backButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            confirmButton.TextColor = applicationStyling.TextColor.ToMauiColor();

            webElement.SetColors(applicationStyling);

            this.BuildRows();
        }

        public Task<List<FileDownload>?> SelectionTask => _selection.Task;

        protected override bool OnBackButtonPressed()
        {
            this.Resolve(null);
            return base.OnBackButtonPressed();
        }

        private void BuildRows()
        {
            string textColor = _applicationStyling.TextColor.ToHex();
            string borderColor = _applicationStyling.TertiaryColor.ToHex();

            foreach (FileDownload item in _items)
            {
                InputComponent checkbox = new()
                {
                    Type = "checkbox",
                    Checked = "checked",
                    Width = "24px",
                    Height = "24px",
                    Margin = "0 12px 0 0",
                    Cursor = "pointer"
                };

                checkbox.OnChange += this.OnCheckboxChanged;

                ImgComponent thumbnail = new()
                {
                    Src = item.LaunchUrl,
                    MaxWidth = "80px",
                    MaxHeight = "80px",
                    Margin = "0 12px 0 0"
                };

                SpanComponent label = new()
                {
                    InnerText = item.FileName,
                    Color = textColor,
                    FlexGrow = "1",
                    OverflowWrap = "anywhere"
                };

                DivComponent row = new()
                {
                    Display = "flex",
                    AlignItems = "center",
                    Padding = "12px",
                    BorderBottom = $"1px solid {borderColor}"
                };

                row.Children.Add(checkbox);
                row.Children.Add(thumbnail);
                row.Children.Add(label);

                _checkboxes[checkbox] = item;
                _selected.Add(item);
                webElement.AddChild(row);
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
            this.Resolve(null);
        }

        private void OnCheckboxChanged(object? sender, InputEventArgs e)
        {
            if (sender is not InputComponent checkbox || !_checkboxes.TryGetValue(checkbox, out FileDownload? item))
            {
                return;
            }

            if (e.Checked ?? false)
            {
                _selected.Add(item);
            }
            else
            {
                _selected.Remove(item);
            }
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            List<FileDownload> selected = _items.Where(_selected.Contains).ToList();

            await Navigation.PopAsync();
            this.Resolve(selected);
        }

        private void Resolve(List<FileDownload>? result)
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            _selection.TrySetResult(result);
        }
    }
}
