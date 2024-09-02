using Deaddit.Attributes;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.PageModels;
using Deaddit.Services;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using Switch = Microsoft.Maui.Controls.Switch;

namespace Deaddit.Pages
{
    public partial class ObjectEditorPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly object _toEdit;

        private readonly object _original;

        private readonly bool _topLevel;
        private readonly DeepCopyHelper _deepCopyHelper;
        public ObjectEditorPage(object original, bool topLevel, IAppTheme appTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _deepCopyHelper = new();
            _deepCopyHelper.ReferenceCopyTypes.Add(typeof(Color));

            if (topLevel)
            {
                _toEdit = _deepCopyHelper.DeepCopy(original);
            } else
            {
                _toEdit = original;
            }

            _original = original;
            _appTheme = appTheme;
            _topLevel = topLevel;

            BindingContext = new ObjectEditorPageViewModel(appTheme);

            this.InitializeComponent();
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

        public event EventHandler<ObjectEditorSaveEventArgs> OnSave;

        public int GetPropertyOrder(PropertyInfo pi)
        {
            if (pi.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da)
            {
                return da.Order;
            }

            return 0;
        }

        public async void OnCancelClicked(object sender, EventArgs e)
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

        public void OnSubmitClicked(object sender, EventArgs e)
        {
            if (_topLevel)
            {
                _deepCopyHelper.DeepCopy(_toEdit, _original);
                OnSave(this, new ObjectEditorSaveEventArgs(sender));
            }

            Navigation.PopAsync();
        }

        private View CreateEditorForType(PropertyInfo prop, object obj, Layout parentLayout)
        {
            Type propertyType = prop.PropertyType;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = propertyType.GetGenericArguments()[0];

                if (!itemType.IsClass)
                {
                    throw new NotImplementedException("Only lists of class types are supported.");
                }

                // Create a vertical stack layout to hold the list items and the "Add" button
                StackLayout listLayout = new();

                foreach (var item in (IEnumerable)prop.GetValue(obj))
                {
                    Grid itemGrid = [];

                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Expanding column for the label
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Auto-sized column for the "Edit" button
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Auto-sized column for the "Remove" button

                    // Display item.ToString()
                    Label itemLabel = new()
                    {
                        Text = item.ToString(),
                        TextColor = _appTheme.TextColor,
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(itemLabel, 0);

                    // Add "Edit" button
                    Button editButton = new()
                    {
                        Text = "Edit",
                        BackgroundColor = _appTheme.PrimaryColor,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(editButton, 1);
                    editButton.Clicked += (s, e) =>
                    {
                        ObjectEditorPage nestedEditor = new(item, false, _appTheme);
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
                        ((IList)prop.GetValue(obj)).Remove(item);
                        listLayout.Remove(itemGrid); // Remove item from UI
                    };

                    // Add elements to the grid
                    itemGrid.Children.Add(itemLabel, editButton, removeButton);

                    listLayout.Children.Add(itemGrid);

                }

                // Add "Add" button
                Button addButton = new() { Text = "Add", BackgroundColor = _appTheme.PrimaryColor };

                addButton.Clicked += (s, e) =>
                {
                    object newItem = Activator.CreateInstance(itemType);
                    ((IList)prop.GetValue(obj)).Add(newItem);
                    ObjectEditorPage nestedEditor = new(newItem, false, _appTheme);
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

                if (prop.GetCustomAttribute<InputAttribute>() is InputAttribute input)
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
                Switch toggle = new() { IsToggled = (bool)prop.GetValue(obj) };
                toggle.Toggled += (s, e) => prop.SetValue(obj, e.Value);
                return toggle;
            }

            if (propertyType == typeof(Color))
            {
                Color stored = prop.GetValue(obj) as Color;

                string hex = stored?.ToHex();

                Entry entry = new() { Text = hex };

                entry.TextChanged += (s, e) =>
                {
                    if (Color.TryParse(e.NewTextValue, out Color colorValue))
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

            foreach (PropertyInfo prop in properties.OrderBy(this.GetPropertyOrder))
            {
                Type propertyType = prop.PropertyType;

                string labelText = prop.Name;

                if (prop.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da && !string.IsNullOrWhiteSpace(da.Name))
                {
                    labelText = da.Name;
                }

                // Create a label for the property name
                Label label = new()
                {
                    Text = labelText,
                    TextColor = _appTheme.TextColor,
                    Margin = new Thickness(5)
                };

                parentLayout.Children.Add(label);

                View editor = this.CreateEditorForType(prop, obj, parentLayout);
                if (editor != null)
                {
                    if (editor is InputView iv)
                    {
                        iv.TextColor = _appTheme.TextColor;
                        iv.BackgroundColor = _appTheme.TertiaryColor;
                    }

                    if(editor is Picker p)
                    {
                        p.TextColor = _appTheme.TextColor;
                        p.BackgroundColor = _appTheme.TertiaryColor;
                    }

                    editor.Margin = new Thickness(5);

                    parentLayout.Children.Add(editor);
                }
                else if (propertyType.IsClass)
                {
                    // For class types, create a button to open a nested editor
                    Button editButton = new() { Text = "Edit", BackgroundColor = _appTheme.PrimaryColor };
                    editButton.Clicked += (s, e) =>
                    {
                        object? nestedObj = prop.GetValue(obj);
                        if (nestedObj != null)
                        {
                            ObjectEditorPage nestedEditor = new(nestedObj, false, _appTheme);
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