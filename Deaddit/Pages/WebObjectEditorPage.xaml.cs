using Deaddit.Components.WebComponents.Forms;
using Deaddit.Core.Attributes;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Utils.DeepCopy;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using Reddit.Api.Models;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Deaddit.Pages
{
    public partial class WebObjectEditorPage : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;
        private readonly DeepCopyHelper _deepCopyHelper;
        private readonly DivComponent _formContainer;
        private readonly INavigation _navigation;
        private readonly object _original;
        private readonly object _toEdit;
        private readonly bool _topLevel;

        public event EventHandler<ObjectEditorSaveEventArgs>? OnSave;

        public WebObjectEditorPage(object original, ApplicationStyling applicationStyling, INavigation navigation)
            : this(original, true, applicationStyling, navigation)
        {
        }

        private WebObjectEditorPage(object original, bool topLevel, ApplicationStyling applicationStyling, INavigation navigation)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _deepCopyHelper = new DeepCopyHelper();
            _navigation = navigation;

            if (topLevel)
            {
                _toEdit = _deepCopyHelper.Copy(original);
            }
            else
            {
                _toEdit = original;
            }

            _original = original;
            _applicationStyling = applicationStyling;
            _topLevel = topLevel;

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);

            _formContainer = new DivComponent
            {
                Display = "flex",
                FlexDirection = "column",
                Padding = "10px"
            };

            // Style the buttons
            cancelButton.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            cancelButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            submitButton.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            submitButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            buttonBar.BackgroundColor = applicationStyling.SecondaryColor.ToMauiColor();

            if (!topLevel)
            {
                submitButton.IsVisible = false;
                cancelButton.Text = "Back";
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await webElement.Clear();

            this.GenerateEditor(_toEdit);

            await webElement.AddChild(_formContainer);
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
                await _navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async void OnSubmitClicked(object? sender, EventArgs e)
        {
            if (_topLevel)
            {
                _deepCopyHelper.Fill(_toEdit, _original);
                OnSave?.Invoke(this, new ObjectEditorSaveEventArgs(_original));
            }

            await _navigation.PopAsync();
        }

        private void GenerateEditor(object obj)
        {
            _formContainer.Children.Clear();

            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties.OrderBy(GetPropertyOrder))
            {
                if (prop.HasCustomAttribute<EditorIgnoreAttribute>())
                {
                    continue;
                }

                Type propertyType = prop.PropertyType;

                string labelText = prop.Name;
                string? descriptionText = null;
                bool masked = false;
                bool multiline = false;

                if (prop.GetCustomAttribute<EditorDisplayAttribute>() is EditorDisplayAttribute da)
                {
                    if (!string.IsNullOrWhiteSpace(da.Name))
                    {
                        labelText = da.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(da.Description))
                    {
                        descriptionText = da.Description;
                    }

                    masked = da.Masked;
                    multiline = da.Multiline;
                }

                FormFieldComponent? field = this.CreateFieldForType(prop, obj, labelText, descriptionText, masked, multiline);

                if (field != null)
                {
                    if (!string.IsNullOrWhiteSpace(descriptionText))
                    {
                        field.OnInfoClicked += async (s, e) =>
                        {
                            await this.DisplayAlert(labelText, descriptionText, "OK");
                        };
                    }

                    _formContainer.Children.Add(field);
                }
            }
        }

        private FormFieldComponent? CreateFieldForType(PropertyInfo prop, object obj, string labelText, string? description, bool masked, bool multiline)
        {
            Type propertyType = prop.PropertyType;

            // Handle List<T> types
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = propertyType.GetGenericArguments()[0];

                if (!itemType.IsClass || itemType == typeof(string))
                {
                    // For now, only support lists of class types (not primitives or strings)
                    return null;
                }

                ListFieldComponent listField = new(labelText, description, prop, obj, _applicationStyling);
                listField.OnEditItem += async (s, item) =>
                {
                    WebObjectEditorPage nestedEditor = new(item, false, _applicationStyling, _navigation);
                    await _navigation.PushAsync(nestedEditor);
                };
                return listField;
            }

            // Handle enum types
            if (propertyType.IsEnum)
            {
                return new EnumFieldComponent(labelText, description, prop, obj, _applicationStyling);
            }

            // Handle string types
            if (propertyType == typeof(string))
            {
                if (multiline)
                {
                    return new MultilineTextFieldComponent(labelText, description, prop, obj, _applicationStyling);
                }
                else
                {
                    return new TextFieldComponent(labelText, description, masked, prop, obj, _applicationStyling);
                }
            }

            // Handle numeric types
            if (propertyType == typeof(int) || propertyType == typeof(double))
            {
                return new NumericFieldComponent(labelText, description, prop, obj, _applicationStyling);
            }

            // Handle boolean types
            if (propertyType == typeof(bool))
            {
                return new BooleanFieldComponent(labelText, description, prop, obj, _applicationStyling);
            }

            // Handle DynamicColor types
            if (propertyType == typeof(DynamicColor))
            {
                return new ColorFieldComponent(labelText, description, prop, obj, _applicationStyling);
            }

            // Handle nested class types
            if (propertyType.IsClass && propertyType != typeof(string))
            {
                NestedObjectFieldComponent nestedField = new(labelText, description, prop, obj, _applicationStyling);
                nestedField.OnEditClicked += async (s, nestedObj) =>
                {
                    WebObjectEditorPage nestedEditor = new(nestedObj, false, _applicationStyling, _navigation);
                    await _navigation.PushAsync(nestedEditor);
                };
                return nestedField;
            }

            return null;
        }
    }
}
