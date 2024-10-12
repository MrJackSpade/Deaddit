using Deaddit.Core.Configurations.Models;
using Maui.WebComponents;
using Maui.WebComponents.Classes;

namespace Deaddit.Components
{
    internal class RedditWebElement : WebElement
    {
        public override async Task OnDocumentLoaded()
        {
            await this.LoadResource("Deaddit.Resources.Embedded.site.css", typeof(RedditWebElement).Assembly);
            await this.LoadResource("Deaddit.Resources.Embedded.site.js", typeof(RedditWebElement).Assembly);
        }

        public void SetColors(ApplicationStyling styling)
        {
            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "body",
                Styles = { ["background-color"] = styling.SecondaryColor.ToHex() }
            }
            );

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "blockquote",
                Styles =
                {
                    ["border-left"] = $"1px solid {styling.TextColor.ToHex()}"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = ".md-spoiler-text",
                Styles =
                {
                    ["color"] = styling.TextColor.ToHex(),
                    ["background-color"] = styling.TextColor.ToHex()
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "a",
                Styles =
                {
                    ["color"] = styling.HyperlinkColor.ToHex()
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "th, strong, td, p",
                Styles =
                {
                    ["color"] = styling.TextColor.ToHex()
                }
            });
        }
    }
}