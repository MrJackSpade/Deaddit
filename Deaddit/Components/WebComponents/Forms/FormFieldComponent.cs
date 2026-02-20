using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class FormFieldComponent : DivComponent
    {
        private readonly LabelComponent _label;
        private readonly ButtonComponent? _infoButton;
        private readonly DivComponent _inputContainer;

        public string? LabelText
        {
            get => _label.InnerText;
            set => _label.InnerText = value;
        }

        public string? Description { get; }

        public event EventHandler? OnInfoClicked;

        public FormFieldComponent(string labelText, string? description, ApplicationStyling styling)
        {
            Description = description;

            Display = "flex";
            FlexDirection = "column";
            Margin = "5px";
            Padding = "5px";

            DivComponent labelContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                AlignItems = "center",
                MarginBottom = "5px"
            };

            _label = new LabelComponent
            {
                InnerText = labelText,
                Color = styling.TextColor.ToHex(),
                FontSize = $"{styling.SubTextFontSize}px"
            };
            labelContainer.Children.Add(_label);

            if (!string.IsNullOrWhiteSpace(description))
            {
                _infoButton = new ButtonComponent
                {
                    InnerText = "i",
                    Width = "20px",
                    Height = "20px",
                    Border = "none",
                    BorderRadius = "50%",
                    BackgroundColor = styling.TertiaryColor.ToHex(),
                    Color = styling.TextColor.ToHex(),
                    FontSize = "12px",
                    Cursor = "pointer",
                    MarginLeft = "8px"
                };
                _infoButton.OnClick += (s, e) => OnInfoClicked?.Invoke(this, EventArgs.Empty);
                labelContainer.Children.Add(_infoButton);
            }

            Children.Add(labelContainer);

            _inputContainer = new DivComponent
            {
                Display = "flex",
                FlexDirection = "column"
            };
            Children.Add(_inputContainer);
        }

        protected void AddInput(WebComponent input)
        {
            _inputContainer.Children.Add(input);
        }

        protected void ClearInputs()
        {
            _inputContainer.Children.Clear();
        }
    }
}
