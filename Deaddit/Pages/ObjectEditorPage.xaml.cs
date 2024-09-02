using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.PageModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Deaddit.Pages
{
    public partial class ObjectEditorPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly object _toEdit;

        private readonly bool _topLevel;

        public ObjectEditorPage(object toEdit, bool topLevel, IAppTheme appTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _toEdit = toEdit;
            _appTheme = appTheme;
            _topLevel = topLevel;

            BindingContext = new ObjectEditorPageViewModel(appTheme);

            InitializeComponent();

            if (!_topLevel)
            {
                submitButton.IsVisible = false;
                cancelButton.Text = "Back";
            }

            this.GenerateEditor(_toEdit, (Layout)mainStack);
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
            await Navigation.PopAsync();
        }

        public void OnSubmitClicked(object sender, EventArgs e)
        {
            if (_topLevel)
            {
                OnSave(this, new ObjectEditorSaveEventArgs(sender));
            }

            Navigation.PopAsync();
        }

        private View CreateEditorForType(PropertyInfo prop, object obj)
        {
            var propertyType = prop.PropertyType;

            if (propertyType == typeof(string))
            {
                var entry = new Entry
                {
                    Text = prop.GetValue(obj)?.ToString()
                };

                entry.TextChanged += (s, e) => prop.SetValue(obj, e.NewTextValue);
                return entry;
            }

            if (propertyType == typeof(int) || propertyType == typeof(double))
            {
                var entry = new Entry
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
                var toggle = new Switch { IsToggled = (bool)prop.GetValue(obj) };
                toggle.Toggled += (s, e) => prop.SetValue(obj, e.Value);
                return toggle;
            }

            if (propertyType == typeof(Color))
            {
                Color stored = prop.GetValue(obj) as Color;

                string hex = stored?.ToHex();

                var entry = new Entry { Text = hex};

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

                if(prop.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da && !string.IsNullOrWhiteSpace(da.Name))
                {
                    labelText = da.Name;
                }

                // Create a label for the property name
                Label label = new() { 
                    Text = labelText, 
                    TextColor = _appTheme.TextColor, 
                    Margin = new Thickness(5) 
                };

                parentLayout.Children.Add(label);

                View editor = this.CreateEditorForType(prop, obj);
                if (editor != null)
                {
                    if (editor is InputView iv)
                    {
                        iv.TextColor = _appTheme.TextColor;
                        iv.BackgroundColor = _appTheme.TertiaryColor;
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
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}