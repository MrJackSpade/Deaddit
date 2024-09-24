using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System.Reflection;
using System.Text;

namespace Maui.WebComponents
{
    public class WebElement : WebView
    {
        private readonly List<WebComponent> _children = [];

        private readonly Dictionary<string, WebComponent> _componentMap = [];

        private readonly TaskCompletionSource _loadedTask = new();

        public WebElement()
        {
            Source = new HtmlWebViewSource { Html = RenderInitialHtml() };
            Navigating += this.OnNavigating;
        }

        public async Task AddChild(WebComponent child)
        {
            await this.AddChild(child, -1);
        }

        public async Task InsertChild(int index, WebComponent child)
        {
            await this.AddChild(child, index);
        }

        public async Task RemoveChild(string componentId)
        {
            await _loadedTask.Task;

            if (_componentMap.TryGetValue(componentId, out WebComponent? component))
            {
                int index = _children.IndexOf(component);
                if (index != -1)
                {
                    _children.RemoveAt(index);
                    _componentMap.Remove(componentId);
                    await this.RemoveChildElement(componentId);
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
                    sb.Append($"{style.Key}: {style.Value};");
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

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Maui.WebComponents.WebElement.html";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))

            using (StreamReader reader = new(stream))
            {
                string result = reader.ReadToEnd();
                sb.Append(result);
            }

            return sb.ToString();
        }

        private async Task AddChild(WebComponent child, int index)
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

        private async Task InjectChildElement(WebComponent component, int index)
        {
            this.RegisterChildren(component);

            string elementHtml = RenderComponent(component);

            // Base64 encode the HTML string
            string base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(elementHtml));

            // Build the JavaScript code to execute
            string script = $"addElement('{base64Html}', {index});";

            try
            {
                // Execute the JavaScript code
                string result = await this.EvaluateJavaScriptAsync(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("webcomponent://"))
            {
                e.Cancel = true;

                if (string.Equals(e.Url, "webcomponent://loaded/", StringComparison.OrdinalIgnoreCase))
                {
                    _loadedTask.SetResult();
                    return;
                }

                Uri uri = new(e.Url);

                string componentId = uri.Host;
                string methodName = uri.AbsolutePath.TrimStart('/');
                string? args = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("args");
                this.HandleInvokeMethod(componentId, methodName, args);
            }
        }

        private void RegisterChildren(WebComponent component)
        {
            _componentMap[component.Id] = component;

            component.Style.OnStyleChanged += async (s, e) => await this.EvaluateJavaScriptAsync($"updateElementStyle('{component.Id}', '{e.Key}', '{e.Value}')");
            component.OnInnerTextChanged += async (s, e) => await this.EvaluateJavaScriptAsync($"updateTextNode('{component.Id}', '{e.InnerText}')");

            foreach (WebComponent child in component.Children)
            {
                this.RegisterChildren(child);
            }
        }

        private async Task RemoveChildElement(string componentId)
        {
            string script = $"removeElement('{componentId}');";
            await this.EvaluateJavaScriptAsync(script);
        }
    }
}