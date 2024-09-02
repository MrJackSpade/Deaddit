using Deaddit.Interfaces;
using Deaddit.PageModels;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Components.ComponentModels
{
    internal class SubRedditComponentViewModel : BaseViewModel
    {
        private readonly IAppTheme _appTheme;

        public SubRedditComponentViewModel(string subReddit, IAppTheme appTheme)
        {
            _appTheme = appTheme;
            SubReddit = subReddit;
            PrimaryColor = appTheme.PrimaryColor;
            SecondaryColor = appTheme.SecondaryColor;
            TertiaryColor = appTheme.TertiaryColor;
            TextColor = appTheme.TextColor;
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

        public string SubReddit
        {
            get => this.GetValue<string>();
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