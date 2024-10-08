using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using Maui.WebComponents.Exceptions;
using Maui.WebComponents.Extensions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Maui.WebComponents
{
    public class WebElement : WebView
    {
        public StyleRuleCollection DocumentStyles = [];

        private readonly List<WebComponent> _children = [];

        private readonly Dictionary<string, WebComponent> _componentMap = [];

        public event EventHandler<Exception>? OnJavascriptError;

        private readonly TaskCompletionSource _loadedTask = new();

        private string _lastDifferentiator;

        public event EventHandler<string> ClickUrl;

        public event EventHandler? OnScrollBottom;

        public WebElement()
        {
            DocumentStyles.OnStyleRuleAdded += this.UpdateStyle;
            Source = new HtmlWebViewSource { Html = this.RenderInitialHtml() };
            Navigating += this.OnNavigating;
        }

        public async Task Clear()
        {
            await _loadedTask.Task;
            _children.Clear();
            _componentMap.Clear();
            await this.EvaluateJavaScriptWithResultAsync($"document.body.replaceChildren();");
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

        public async Task LoadResource(string resourceName, Assembly assembly)
        {
            StringBuilder sb = new();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))

            using (StreamReader reader = new(stream))
            {
                string result = reader.ReadToEnd();
                sb.Append(result);
            }

            string html = sb.ToString();

            if (resourceName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                html = $"<style>{html}</style>";
            }

            if (resourceName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                html = $"<script>{html}</script>";
            }

            // Build the JavaScript code to execute
            await this.AddElement(html, 0, "head");
        }

        public async Task RemoveChild(WebComponent child)
        {
            await this.RemoveChild(child, true);
        }

        private static string EncodeHtml(string html)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
        }

        private static void RaiseEvent(WebComponent component, EventInfo eventInfo, string argsJson)
        {
            // Deserialize argsJson if necessary
            EventArgs eventArgs = EventArgs.Empty; // Replace with actual deserialization if needed

            // Get the backing field of the event (this relies on the field name matching the event name)
            FieldInfo? field = null;

            Type? searchType = component.GetType();

            while (field == null && searchType != null)
            {
                field = searchType.GetField(eventInfo.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                searchType = searchType.BaseType;
            }

            if (field != null)
            {
                if (field.GetValue(component) is MulticastDelegate eventDelegate)
                {
                    // Prepare arguments for the event handler
                    object[] invokeArgs = [component, eventArgs];

                    // Invoke each subscribed event handler
                    foreach (Delegate handler in eventDelegate.GetInvocationList())
                    {
                        handler.DynamicInvoke(invokeArgs);
                    }
                }
            }
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

            // Existing code to process methods with HtmlEventAttribute
            foreach (MethodInfo method in type.GetMethods())
            {
                HtmlEventAttribute? eventAttr = method.GetCustomAttribute<HtmlEventAttribute>();

                if (eventAttr != null)
                {
                    string eventName = eventAttr.Name ?? method.Name.ToLowerInvariant();

                    sb.Append($" {eventName}=\"invokeMethod('{component.Id}', '{method.Name}', event)\"");
                }
            }

            // New code to process events with HtmlEventAttribute
            foreach (EventInfo eventInfo in type.GetEvents())
            {
                HtmlEventAttribute? eventAttr = eventInfo.GetCustomAttribute<HtmlEventAttribute>();

                if (eventAttr != null)
                {
                    string eventName = eventAttr.Name ?? eventInfo.Name.ToLowerInvariant();

                    sb.Append($" {eventName}=\"invokeMethod('{component.Id}', '{eventInfo.Name}', event)\"");
                }
            }

            sb.Append('>');

            if (component.InnerText != null)
            {
                sb.Append(component.InnerText);
            }

            if (component.InnerHTML != null)
            {
                sb.Append(component.InnerHTML);
            }

            // Render children
            foreach (WebComponent child in component.Children)
            {
                sb.Append(RenderComponent(child));
            }

            sb.Append($"</{entityAttr.Tag}>");

            return sb.ToString();
        }

        private async Task AddElement(string html, int index, string? parentSelector = null)
        {
            // Base64 encode the HTML string
            string base64Html = EncodeHtml(html);

            // Build the JavaScript code to execute
            string script = string.IsNullOrWhiteSpace(parentSelector) ?
                            $"addElement('{base64Html}', {index});" :
                            $"addElement('{base64Html}', {index}, '{parentSelector}');";

            // Execute the JavaScript code
            await this.EvaluateJavaScriptWithResultAsync(script);
        }

        protected async Task EvaluateJavaScriptWithResultAsync(string script)
        {
            string result = await this.EvaluateJavaScriptAsync(script) ?? "{ }";

            try
            {
                OperationResult resultObj = JsonSerializer.Deserialize<OperationResult>(result);

                if (resultObj.Success)
                {
                    // success
                    return;
                }
                else
                {
                    Exception exception = new (resultObj.Message);

                    if(OnJavascriptError != null)
                    {
                        OnJavascriptError(this, exception);
                    }
                    else
                    {
                        throw exception;
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse JavaScript result: " + ex.Message);
            }
        }

        private void HandleInvokeMethod(string componentId, string methodName, string argsJson)
        {
            if (_componentMap.TryGetValue(componentId, out WebComponent? component))
            {
                MethodInfo? method = component.GetType().GetMethod(methodName);

                if (method != null)
                {
                    // Invoke the method (existing functionality)
                    method.Invoke(component, [null, EventArgs.Empty]);
                }
                else
                {
                    // Attempt to find an event with the specified name
                    EventInfo? eventInfo = component.GetType().GetEvent(methodName);

                    if (eventInfo != null)
                    {
                        // Raise the event via reflection
                        RaiseEvent(component, eventInfo, argsJson);
                    }
                    else
                    {
                        // No method or event found
                        throw new InvalidOperationException($"Method or event '{methodName}' not found on component '{component.GetType().Name}'.");
                    }
                }
            }
        }

        private async Task InjectChildElement(WebComponent component, int index, WebComponent? parent = null)
        {
            this.RegisterChildren(component);

            string elementHtml = RenderComponent(component);

            try
            {
                // Build the JavaScript code to execute

                if (parent?.Id is not null)
                {
                    await this.AddElement(elementHtml, index, $"#{parent?.Id}");
                }
                else
                {
                    await this.AddElement(elementHtml, index);
                }

                component.IsRendered = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
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
                    await this.OnDocumentLoaded();
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
                string argsJson = JsonSerializer.Serialize(args);

                // Handle the method invocation
                this.HandleInvokeMethod(componentId, methodName, argsJson);
            }
        }

        public virtual Task OnDocumentLoaded()
        {
            return Task.CompletedTask;
        }

        private void RegisterChildren(WebComponent component)
        {
            _componentMap[component.Id] = component;

            component.Style.OnStyleChanged += async (s, e) => await this.EvaluateJavaScriptWithResultAsync($"updateElementStyle('{component.Id}', '{e.Key}', '{e.Value}')");
            component.OnInnerTextChanged += async (s, e) => await this.EvaluateJavaScriptWithResultAsync($"updateTextNode('{component.Id}', '{EncodeHtml(e.Text)}')");
            component.OnInnerHTMLChanged += async (s, e) => await this.EvaluateJavaScriptWithResultAsync($"updateInnerHTML('{component.Id}', '{EncodeHtml(e.Text)}')");
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
                component.RemoveEventHandlers(nameof(WebComponent.OnInnerHTMLChanged));
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
            await this.EvaluateJavaScriptWithResultAsync(script);
        }

        private string RenderInitialHtml()
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

            string html = sb.ToString();

            if (!html.Contains("<style></style>"))
            {
                throw new InvalidOperationException("The initial HTML must contain an empty <style> tag to render initial styles");
            }

            StringBuilder cssBuilder = new();

            foreach (StyleRule styleRule in DocumentStyles)
            {
                cssBuilder.Append(styleRule.ToString());
            }

            html = html.Replace("<style></style>", $"<style>{cssBuilder}</style>");

            return html;
        }

        private async void UpdateStyle(object? sender, OnStyleRuleAddedEventArgs? args)
        {
            await _loadedTask.Task;

            // Serialize the styles object to JSON
            string jsonString = JsonSerializer.Serialize(args.StyleRule.Styles);

            // Encode the JSON string to Base64
            string base64Rules = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

            // Escape the CSS selector and Base64 string for JavaScript
            string escapedSelector = args.StyleRule.CssSelector.Replace("'", "\\'");
            string escapedBase64Rules = base64Rules.Replace("'", "\\'");

            // Construct the JavaScript call
            string script = $"addStyle('{escapedSelector}', '{escapedBase64Rules}');";

            // Execute the JavaScript
            await this.EvaluateJavaScriptWithResultAsync(script);
        }
    }
}