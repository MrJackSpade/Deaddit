using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using Maui.WebComponents.Exceptions;
using Maui.WebComponents.Extensions;
using System.Reflection;
using System.Text;
using System.Web;

namespace Maui.WebComponents
{
    public class WebElement : WebView
    {
        private readonly List<WebComponent> _children = [];

        private readonly Dictionary<string, WebComponent> _componentMap = [];

        private readonly TaskCompletionSource _loadedTask = new();

        public StyleCollection BodyStyle { get; } = [];

        public event EventHandler<string> ClickUrl;

        public event EventHandler? OnScrollBottom;

        public WebElement()
        {
            BodyStyle.OnStyleChanged += this.UpdateStyle;
            Source = new HtmlWebViewSource { Html = RenderInitialHtml() };
            Navigating += this.OnNavigating;
        }

        public async Task Clear()
        {
            await _loadedTask.Task;
            _children.Clear();
            _componentMap.Clear();
            await this.EvaluateJavaScriptAsync($"document.body.replaceChildren();");
        }

        public async Task InsertChild(int index, WebComponent child)
        {
            await _loadedTask.Task;

            if (index == -1 || index >= _children.Count)
            {
                _children.Add(child);
                index = _children.Count - 1;
            }
            else
            {
                _children.Insert(index, child);
            }

            await this.InjectChildElement(child, index);
        }

        public async Task InsertChild(WebComponent parent, int index, WebComponent child)
        {
            await _loadedTask.Task;

            if (index == -1 || index >= parent.Children.Count)
            {
                index = parent.Children.Count - 1;
            }

            await this.InjectChildElement(child, index, parent);
        }

        public bool IsRendered(WebComponent component)
        {
            return _componentMap.ContainsKey(component.Id);
        }

        public async Task RemoveChild(WebComponent child)
        {
            await this.RemoveChild(child, true);
        }

        private static string RenderComponent(WebComponent component)
        {
            Type type = component.GetType();
            HtmlEntityAttribute? entityAttr = type.GetCustomAttribute<HtmlEntityAttribute>()
                                              ?? throw new InvalidOperationException($"Component {type.Name} is missing HtmlEntity attribute.");

            StringBuilder sb = new();
            sb.Append($"<{entityAttr.Tag}");

            // Render attributes
            foreach (PropertyInfo prop in type.GetProperties())
            {
                HtmlAttributeAttribute? attrAttr = prop.GetCustomAttribute<HtmlAttributeAttribute>();
                if (attrAttr != null)
                {
                    object? value = prop.GetValue(component);
                    if (value != null)
                    {
                        sb.Append($" {prop.Name.ToLowerInvariant()}=\"{value}\"");
                    }
                }
            }

            if (component.Style.Count > 0)
            {
                sb.Append(" style=\"");
                foreach (KeyValuePair<string, string> style in component.Style)
                {
                    sb.Append($" {style.Key}: {style.Value};");
                }

                sb.Append('"');
            }

            // Render events
            foreach (MethodInfo method in type.GetMethods())
            {
                HtmlEventAttribute? eventAttr = method.GetCustomAttribute<HtmlEventAttribute>();

                if (eventAttr != null)
                {
                    string eventName = eventAttr.Name ?? method.Name.ToLowerInvariant();

                    sb.Append($" {eventName}=\"invokeMethod('{component.Id}', '{method.Name}', event)\"");
                }
            }

            sb.Append('>');

            if (component.InnerText != null)
            {
                sb.Append(component.InnerText);
            }

            // Render children
            foreach (WebComponent child in component.Children)
            {
                sb.Append(RenderComponent(child));
            }

            sb.Append($"</{entityAttr.Tag}>");

            return sb.ToString();
        }

        private static string RenderInitialHtml()
        {
            StringBuilder sb = new();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Maui.WebComponents.WebElement.html";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))

            using (StreamReader reader = new(stream))
            {
                string result = reader.ReadToEnd();
                sb.Append(result);
            }

            return sb.ToString();
        }

        private void HandleInvokeMethod(string componentId, string methodName, string argsJson)
        {
            if (_componentMap.TryGetValue(componentId, out WebComponent? component))
            {
                MethodInfo? method = component.GetType().GetMethod(methodName);

                // For simplicity, we're just invoking the method without args
                // You might want to deserialize argsJson and pass it to the method
                method?.Invoke(component, [null, EventArgs.Empty]);
            }
        }

        private async Task InjectChildElement(WebComponent component, int index, WebComponent? parent = null)
        {
            this.RegisterChildren(component);

            string elementHtml = RenderComponent(component);

            // Base64 encode the HTML string
            string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(elementHtml));

            // Build the JavaScript code to execute
            string script = parent is null ?
                            $"addElement('{base64Html}', {index});" :
                            $"addElement('{base64Html}', {index}, '{parent.Id}');";

            try
            {
                // Execute the JavaScript code
                string result = await this.EvaluateJavaScriptAsync(script);

                component.IsRendered = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string _lastDifferentiator;

        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("webcomponent://"))
            {
                e.Cancel = true;

                Uri uri = new(e.Url);

                string componentId = uri.Host;

                // Extract the path segments: [methodName, differentiator]
                string[] pathSegments = uri.AbsolutePath.Trim('/').Split('/');
                string methodName = pathSegments.Length > 0 ? pathSegments[0] : string.Empty;
                string differentiator = pathSegments.Length > 1 ? pathSegments[1] : string.Empty;

                // If the differentiator is the same as the last one, cancel processing
                if (!string.IsNullOrEmpty(differentiator))
                {
                    if (differentiator == _lastDifferentiator)
                    {
                        return;
                    }
                    else
                    {
                        _lastDifferentiator = differentiator;
                    }
                }

                // Handle special cases
                if (componentId.Equals("system", StringComparison.OrdinalIgnoreCase) && methodName.Equals("Loaded", StringComparison.OrdinalIgnoreCase))
                {
                    _loadedTask.SetResult();
                    return;
                }

                if (componentId.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    System.Collections.Specialized.NameValueCollection queryDictionary = HttpUtility.ParseQueryString(uri.Query);
                    string message = queryDictionary["message"];
                    throw new JavascriptException(message);
                }

                if (componentId.Equals("scroll", StringComparison.OrdinalIgnoreCase) && methodName.Equals("bottom", StringComparison.OrdinalIgnoreCase))
                {
                    OnScrollBottom?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (componentId.Equals("navigation", StringComparison.OrdinalIgnoreCase) && methodName.Equals("href", StringComparison.OrdinalIgnoreCase))
                {
                    System.Collections.Specialized.NameValueCollection queryDictionary = HttpUtility.ParseQueryString(uri.Query);
                    string targetUrl = queryDictionary["href"];
                    targetUrl = HttpUtility.UrlDecode(targetUrl);
                    ClickUrl?.Invoke(this, targetUrl);
                    return;
                }

                // Parse arguments from the query string
                System.Collections.Specialized.NameValueCollection queryDictionaryArgs = HttpUtility.ParseQueryString(uri.Query);
                Dictionary<string, string?> args = queryDictionaryArgs.AllKeys
                    .Where(key => key != null)
                    .ToDictionary(key => key!, key => queryDictionaryArgs[key])!;

                // Convert args dictionary to JSON string
                string argsJson = System.Text.Json.JsonSerializer.Serialize(args);

                // Handle the method invocation
                this.HandleInvokeMethod(componentId, methodName, argsJson);
            }
        }

        private void RegisterChildren(WebComponent component)
        {
            _componentMap[component.Id] = component;

            component.Style.OnStyleChanged += async (s, e) => await this.EvaluateJavaScriptAsync($"updateElementStyle('{component.Id}', '{e.Key}', '{e.Value}')");
            component.OnInnerTextChanged += async (s, e) => await this.EvaluateJavaScriptAsync($"updateTextNode('{component.Id}', '{e.InnerText}')");
            component.Children.OnWebComponentAdded += async (s, e) => await this.InsertChild(component, -1, e.Added);
            component.Children.OnWebComponentInsert += async (s, e) => await this.InsertChild(component, e.Index, e.Added);
            component.Children.OnWebComponentRemoved += async (s, e) => await this.RemoveChild(e.Removed);

            foreach (WebComponent child in component.Children)
            {
                this.RegisterChildren(child);
            }
        }

        private async Task RemoveChild(WebComponent component, bool top)
        {
            if (_componentMap.Remove(component.Id))
            {
                _children.Remove(component);

                component.Style.RemoveEventHandlers(nameof(StyleCollection.OnStyleChanged));
                component.RemoveEventHandlers(nameof(WebComponent.OnInnerTextChanged));
                component.Children.RemoveEventHandlers(nameof(WebComponentCollection.OnWebComponentAdded));
                component.Children.RemoveEventHandlers(nameof(WebComponentCollection.OnWebComponentRemoved));
                component.Children.RemoveEventHandlers(nameof(WebComponentCollection.OnWebComponentInsert));

                if (top)
                {
                    await this.RemoveChildElement(component);
                }

                foreach (WebComponent child in component.Children)
                {
                    await this.RemoveChild(child, false);
                }
            }
        }

        private async Task RemoveChildElement(WebComponent component)
        {
            string script = $"removeElement('{component.Id}');";
            component.IsRendered = false;
            await this.EvaluateJavaScriptAsync(script);
        }

        private async void UpdateStyle(object? sender, OnStyleChangedEventArgs? args)
        {
            await _loadedTask.Task;

            await this.EvaluateJavaScriptAsync($"updateElementStyle('body', '{args.Key}', '{args.Value}')");
        }
    }
}