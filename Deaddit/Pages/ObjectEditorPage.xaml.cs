using Deaddit.Core.Attributes;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Utils.DeepCopy;
using Deaddit.Core.Utils.Extensions;
using Deaddit.Core.Utils.Models;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Pages.Models;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Switch = Microsoft.Maui.Controls.Switch;

namespace Deaddit.Pages
{
    public partial class ObjectEditorPage : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly DeepCopyHelper _deepCopyHelper;

        private readonly object _original;

        private readonly object _toEdit;

        private readonly bool _topLevel;

        public event EventHandler<ObjectEditorSaveEventArgs>? OnSave;

        public ObjectEditorPage(object original, ApplicationStyling applicationTheme) : this(original, true, applicationTheme)
        {
        }

        private ObjectEditorPage(object original, bool topLevel, ApplicationStyling applicationTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _deepCopyHelper = new();

            if (topLevel)
            {
                _toEdit = _deepCopyHelper.Copy(original);
            }
            else
            {
                _toEdit = original;
            }

            _original = original;
            _applicationStyling = applicationTheme;
            _topLevel = topLevel;

            BindingContext = new ObjectEditorPageViewModel(applicationTheme);

            this.InitializeComponent();
        }

        public static int GetPropertyOrder(PropertyInfo pi)
        {
            if (pi.GetCustomAttribute<EditorDisplayAttribute>() is EditorDisplayAttribute da)
            {
                return da.Order;
            }

            return 0;
        }

        public async void OnCancelClicked(object? sender, EventArgs e)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void OnSubmitClicked(object? sender, EventArgs e)
        {
            if (_topLevel)
            {
                _deepCopyHelper.Fill(_toEdit, _original);
                OnSave(this, new ObjectEditorSaveEventArgs(_original));
            }

            Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            mainStack.Children.Clear();

            if (!_topLevel)
            {
                submitButton.IsVisible = false;
                cancelButton.Text = "Back";
            }

            this.GenerateEditor(_toEdit, mainStack);
        }

        private View? CreateEditorForType(PropertyInfo prop, object obj)
        {
            Type propertyType = prop.PropertyType;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                IList propertyValue = (IList)prop.GetValue(obj)!;

                Type itemType = propertyType.GetGenericArguments()[0];

                if (!itemType.IsClass)
                {
                    throw new NotImplementedException("Only lists of class types are supported.");
                }

                // Create a vertical stack layout to hold the list items and the "Add" button
                StackLayout listLayout = [];

                foreach (object? item in propertyValue)
                {
                    Grid itemGrid = [];

                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Expanding column for the label
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Auto-sized column for the "Edit" button
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Auto-sized column for the "Remove" button

                    // Display item.ToString()
                    Label itemLabel = new()
                    {
                        Text = item.ToString(),
                        TextColor = _applicationStyling.TextColor.ToMauiColor(),
                        VerticalOptions = LayoutOptions.Center
                    };

                    Grid.SetColumn(itemLabel, 0);

                    // Add "Edit" button
                    Button editButton = new()
                    {
                        Text = "Edit",
                        BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor(),
                        TextColor = _applicationStyling.TextColor.ToMauiColor(),
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Center
                    };

                    Grid.SetColumn(editButton, 1);

                    editButton.Clicked += (s, e) =>
                    {
                        ObjectEditorPage nestedEditor = new(item, false, _applicationStyling);
                        Navigation.PushAsync(nestedEditor);
                    };

                    // Add "Remove" button
                    Button removeButton = new()
                    {
                        Text = "Remove",
                        BackgroundColor = Colors.Red,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Center
                    };

                    Grid.SetColumn(removeButton, 2);

                    removeButton.Clicked += (s, e) =>
                    {
                        propertyValue.Remove(item);
                        listLayout.Remove(itemGrid); // Remove item from UI
                    };

                    // Add elements to the grid
                    itemGrid.Children.Add(itemLabel, editButton, removeButton);

                    listLayout.Children.Add(itemGrid);
                }

                // Add "Add" button
                Button addButton = new() { Text = "Add", BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor() };

                addButton.Clicked += (s, e) =>
                {
                    object newItem = Activator.CreateInstance(itemType)!;
                    propertyValue.Add(newItem);
                    ObjectEditorPage nestedEditor = new(newItem, false, _applicationStyling);
                    Navigation.PushAsync(nestedEditor);
                };

                listLayout.Children.Add(addButton);

                return listLayout;
            }

            if (propertyType.IsEnum)
            {
                Picker picker = new()
                {
                    ItemsSource = Enum.GetValues(propertyType).Cast<Enum>().ToList(),
                    SelectedItem = prop.GetValue(obj)
                };

                picker.SelectedIndexChanged += (s, e) =>
                {
                    if (picker.SelectedItem != null)
                    {
                        prop.SetValue(obj, picker.SelectedItem);
                    }
                };

                return picker;
            }

            if (propertyType == typeof(string))
            {
                Entry entry = new()
                {
                    Text = prop.GetValue(obj)?.ToString()
                };

                entry.TextChanged += (s, e) => prop.SetValue(obj, e.NewTextValue);

                if (prop.GetCustomAttribute<EditorDisplayAttribute>() is EditorDisplayAttribute input)
                {
                    if (input.Masked)
                    {
                        entry.IsPassword = true;
                    }
                }

                return entry;
            }

            if (propertyType == typeof(int) || propertyType == typeof(double))
            {
                Entry entry = new()
                {
                    Keyboard = Keyboard.Numeric,
                    Text = prop.GetValue(obj)?.ToString()
                };

                entry.TextChanged += (s, e) =>
                {
                    if (propertyType == typeof(int) && int.TryParse(e.NewTextValue, out int intValue))
                    {
                        prop.SetValue(obj, intValue);
                    }
                    else if (propertyType == typeof(double) && double.TryParse(e.NewTextValue, out double doubleValue))
                    {
                        prop.SetValue(obj, doubleValue);
                    }
                };

                return entry;
            }

            if (propertyType == typeof(bool))
            {
                Switch toggle = new() { IsToggled = (bool)prop.GetValue(obj)! };
                toggle.Toggled += (s, e) => prop.SetValue(obj, e.Value);
                return toggle;
            }

            if (propertyType == typeof(DynamicColor))
            {
                DynamicColor? stored = prop.GetValue(obj) as DynamicColor;

                string? hex = stored?.ToHex();

                Entry entry = new() { Text = hex };

                entry.TextChanged += (s, e) =>
                {
                    if (DynamicColor.TryParse(e.NewTextValue, out DynamicColor colorValue))
                    {
                        prop.SetValue(obj, colorValue);
                    }
                };

                // Optionally, you can create a color picker control instead of an entry.
                return entry;
            }

            // Add more custom mappings as needed

            return null; // Fallback for unsupported types
        }

        private void GenerateEditor(object obj, Layout parentLayout)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties.OrderBy(GetPropertyOrder))
            {
                if(prop.HasCustomAttribute<EditorIgnoreAttribute>())
                {
                    continue;
                }

                Type propertyType = obj?.GetType() ?? prop.PropertyType;

                string labelText = prop.Name;

                if (prop.GetCustomAttribute<EditorDisplayAttribute>() is EditorDisplayAttribute da && !string.IsNullOrWhiteSpace(da.Name))
                {
                    labelText = da.Name;
                }

                // Create a label for the property name
                Label label = new()
                {
                    Text = labelText,
                    TextColor = _applicationStyling.TextColor.ToMauiColor(),
                    Margin = new Thickness(5)
                };

                parentLayout.Children.Add(label);

                View? editor = this.CreateEditorForType(prop, obj);

                if (editor != null)
                {
                    if (editor is InputView iv)
                    {
                        iv.TextColor = _applicationStyling.TextColor.ToMauiColor();
                        iv.BackgroundColor = _applicationStyling.TertiaryColor.ToMauiColor();
                    }

                    if (editor is Picker p)
                    {
                        p.TextColor = _applicationStyling.TextColor.ToMauiColor();
                        p.BackgroundColor = _applicationStyling.TertiaryColor.ToMauiColor();
                    }

                    editor.Margin = new Thickness(5);

                    parentLayout.Children.Add(editor);
                }
                else if (propertyType.IsClass)
                {
                    // For class types, create a button to open a nested editor
                    Button editButton = new()
                    {
                        Text = "Edit",
                        BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor(),
                        TextColor = _applicationStyling.TextColor.ToMauiColor()
                    };

                    editButton.Clicked += (s, e) =>
                    {
                        object? nestedObj = prop.GetValue(obj);
                        if (nestedObj != null)
                        {
                            ObjectEditorPage nestedEditor = new(nestedObj, false, _applicationStyling);
                            Navigation.PushAsync(nestedEditor);
                        }
                    };
                    parentLayout.Children.Add(editButton);
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // List<T> has already been handled in CreateEditorForType
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}