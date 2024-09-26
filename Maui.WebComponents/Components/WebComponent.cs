﻿using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    public partial class WebComponent
    {
        private string _innerText = string.Empty;

        public WebComponentCollection Children { get; } = [];

        [HtmlAttribute]
        public string Id { get; } = Guid.NewGuid().ToString();

        public string InnerText
        {
            get => _innerText;
            set
            {
                if (_innerText != value)
                {
                    _innerText = value;
                    OnInnerTextChanged?.Invoke(this, new OnInnerTextChangedEventArgs { InnerText = value });
                }
            }
        }

        public bool IsRendered { get; internal set; }

        public StyleCollection Style { get; } = [];

        [HtmlEvent("onclick", true)]
        public event EventHandler? OnClick;

        internal event EventHandler<OnInnerTextChangedEventArgs>? OnInnerTextChanged;
    }
}