﻿using Deaddit.Interfaces;

namespace Deaddit.PageModels
{
    internal class LandingPageViewModel : BaseViewModel
    {
        private readonly IAppTheme _appTheme;

        public LandingPageViewModel(IAppTheme appTheme)
        {
            _appTheme = appTheme;
            SecondaryColor = appTheme.SecondaryColor;
            TextColor = appTheme.TextColor;
            PrimaryColor = appTheme.PrimaryColor;
            TertiaryColor = appTheme.TertiaryColor;
        }

        public Color PrimaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color SecondaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color TertiaryColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }

        public Color TextColor
        {
            get => this.GetValue<Color>();
            set => this.SetValue(value);
        }
    }
}