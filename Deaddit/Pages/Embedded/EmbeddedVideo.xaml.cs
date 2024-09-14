﻿using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Extensions;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly PostItems _postItems;

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                mediaView.Stop();
                mediaView.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public EmbeddedVideo(PostItems items, ApplicationStyling applicationTheme)
        {
            _applicationStyling = applicationTheme;
            _postItems = items;

            this.InitializeComponent();
            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();
            mediaView.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            PostItem item = items.Items.Single();

            string url = item.LaunchUrl;

            mediaView.Source = new Uri(url);
        }

        private void OnBackClicked(object? sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            await FileStorage.Save(_postItems);
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.ShareFiles("", _postItems);
        }
    }
}