using Maui.WebComponents.Attributes;
using Maui.WebComponents.Interfaces;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Maui.WebComponents
{
    public class WebElement : WebView
    {
        private readonly List<IWebComponent> _children = [];

        private readonly Dictionary<string, IWebComponent> _componentMap = [];

        private readonly TaskCompletionSource _loadedTask = new();

        public WebElement()
        {
            Source = new HtmlWebViewSource { Html = RenderInitialHtml() };
            Navigating += this.OnNavigating;
        }

        public async Task AddChild(IWebComponent child)
        {
            await this.AddChild(child, -1);
        }

        public async Task InsertChild(int index, IWebComponent child)
        {
            await this.AddChild(child, index);
        }

        public async Task RemoveChild(string componentId)
        {
            await _loadedTask.Task;

            if (_componentMap.TryGetValue(componentId, out IWebComponent? component))
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

        private static string RenderComponent(IWebComponent component)
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
            foreach (IWebComponent child in component.Children)
            {
                sb.Append(RenderComponent(child));
            }

            sb.Append($"</{entityAttr.Tag}>");

            return sb.ToString();
        }

        private static string RenderInitialHtml()
        {
            StringBuilder sb = new();
            sb.Append(@"
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        html, body {
                            margin: 0;
                            padding: 0;
                        }

                        body {
                            font-family: Arial, sans-serif;
                        }   
                    </style>
                    <script>
                        function invokeMethod(componentId, methodName, args) {
                            window.location.href = `webcomponent://${componentId}/${methodName}?args=${JSON.stringify(args)}`;
                        }

                        function base64ToUtf8(base64Str) {
                            // Decode base64 to bytes
                            var binaryStr = atob(base64Str);

                            // Convert binary string to an array of bytes
                            var bytes = new Uint8Array(binaryStr.length);
                            for (var i = 0; i < binaryStr.length; i++) {
                                bytes[i] = binaryStr.charCodeAt(i);
                            }

                            // Decode bytes to a UTF-8 string
                            var decodedStr = new TextDecoder('utf-8').decode(bytes);
                            return decodedStr;
                        }

                        function addElement(base64Html, index) {
                            var container = document.getElementById('container');

                            // Save the current scroll position
                            var scrollTop = window.pageYOffset || document.documentElement.scrollTop;

                            // Decode the base64-encoded HTML string using UTF-8
                            var decodedHtml = base64ToUtf8(base64Html);

                            var div = document.createElement('div');
                            div.innerHTML = decodedHtml;
                            var newElement = div.firstChild;

                            if (index >= container.children.length) {
                                container.appendChild(newElement);
                            } else {
                                container.insertBefore(newElement, container.children[index]);
                            }

                            // Restore the previous scroll position
                            window.scrollTo(0, scrollTop);
                        }


                        function removeElement(componentId) {
                            var element = document.getElementById(componentId);
                            if (element) {
                                element.parentNode.removeChild(element);
                            }
                        }

                        window.onload = function() {
                            window.location.href = `webcomponent://Loaded`;
                        };
                    </script>
                </head>
                <body>
                    <div id='container'></div>
                </body>
                </html>");
            return sb.ToString();
        }

        private async Task AddChild(IWebComponent child, int index)
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

            _componentMap[child.Id] = child;
            await this.InjectChildElement(child, index);
        }

        private void HandleInvokeMethod(string componentId, string methodName, string argsJson)
        {
            if (_componentMap.TryGetValue(componentId, out IWebComponent? component))
            {
                MethodInfo? method = component.GetType().GetMethod(methodName);

                // For simplicity, we're just invoking the method without args
                // You might want to deserialize argsJson and pass it to the method
                method?.Invoke(component, [null, EventArgs.Empty]);
            }
        }

        private async Task InjectChildElement(IWebComponent component, int index)
        {
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

        private void RefreshContent()
        {
            Source = new HtmlWebViewSource { Html = this.RenderHtml() };
        }

        private async Task RemoveChildElement(string componentId)
        {
            string script = $"removeElement('{componentId}');";
            await this.EvaluateJavaScriptAsync(script);
        }

        private string RenderHtml()
        {
            StringBuilder sb = new();
            sb.Append("<html><head><script>");
            sb.Append(@"
                    function invokeMethod(componentId, methodName, args) {
                        window.location.href = `webcomponent://${componentId}/${methodName}?args=${JSON.stringify(args)}`;
                    }
            ");
            sb.Append("</script></head><body>");
            foreach (IWebComponent child in _children)
            {
                sb.Append(RenderComponent(child));
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}