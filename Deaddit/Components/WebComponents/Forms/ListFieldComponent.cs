using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System.Collections;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class ListFieldComponent : FormFieldComponent
    {
        private readonly DivComponent _itemsContainer;
        private readonly ButtonComponent _addButton;
        private readonly PropertyInfo _property;
        private readonly object _target;
        private readonly Type _itemType;
        private readonly ApplicationStyling _styling;
        private readonly IList _list;

        public event EventHandler<object>? OnEditItem;

        public ListFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;
            _styling = styling;
            _itemType = property.PropertyType.GetGenericArguments()[0];
            _list = (IList)property.GetValue(target)!;

            _itemsContainer = new DivComponent
            {
                Display = "flex",
                FlexDirection = "column",
                MarginBottom = "8px"
            };

            this.AddInput(_itemsContainer);

            // Render existing items
            this.RenderItems();

            _addButton = new ButtonComponent
            {
                InnerText = "Add",
                Padding = "8px 16px",
                Border = "none",
                BorderRadius = "4px",
                BackgroundColor = styling.PrimaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Cursor = "pointer"
            };

            _addButton.OnClick += OnAddClicked;
            this.AddInput(_addButton);
        }

        private void RenderItems()
        {
            _itemsContainer.Children.Clear();

            for (int i = 0; i < _list.Count; i++)
            {
                object? item = _list[i];
                if (item == null)
                {
                    continue;
                }

                int index = i;
                DivComponent itemRow = CreateItemRow(item, index);
                _itemsContainer.Children.Add(itemRow);
            }
        }

        private DivComponent CreateItemRow(object item, int index)
        {
            DivComponent row = new()
            {
                Display = "flex",
                AlignItems = "center",
                Padding = "4px 0",
                BorderBottom = $"1px solid {_styling.TertiaryColor.ToHex()}"
            };

            SpanComponent label = new()
            {
                InnerText = item.ToString() ?? $"Item {index + 1}",
                Color = _styling.TextColor.ToHex(),
                FlexGrow = "1",
                Overflow = "hidden"
            };

            ButtonComponent editButton = new()
            {
                InnerText = "Edit",
                Padding = "4px 8px",
                Border = "none",
                BorderRadius = "4px",
                BackgroundColor = _styling.PrimaryColor.ToHex(),
                Color = _styling.TextColor.ToHex(),
                Cursor = "pointer",
                MarginLeft = "8px"
            };

            ButtonComponent removeButton = new()
            {
                InnerText = "Remove",
                Padding = "4px 8px",
                Border = "none",
                BorderRadius = "4px",
                BackgroundColor = "#cc3333",
                Color = "#ffffff",
                Cursor = "pointer",
                MarginLeft = "8px"
            };

            editButton.OnClick += (s, e) => OnEditItem?.Invoke(this, item);

            removeButton.OnClick += (s, e) =>
            {
                _list.Remove(item);
                _itemsContainer.Children.Remove(row);
            };

            row.Children.Add(label);
            row.Children.Add(editButton);
            row.Children.Add(removeButton);

            return row;
        }

        private void OnAddClicked(object? sender, EventArgs e)
        {
            object? newItem = Activator.CreateInstance(_itemType);
            if (newItem != null)
            {
                _list.Add(newItem);

                DivComponent itemRow = CreateItemRow(newItem, _list.Count - 1);
                _itemsContainer.Children.Add(itemRow);

                OnEditItem?.Invoke(this, newItem);
            }
        }
    }
}
