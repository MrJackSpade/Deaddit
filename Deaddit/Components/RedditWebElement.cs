using Deaddit.Core.Utils;
using Maui.WebComponents;
using Maui.WebComponents.Classes;

namespace Deaddit.Components
{
    internal class RedditWebElement : WebElement
    {
        public async override Task OnDocumentLoaded()
        {
            await this.LoadResource("Deaddit.Resources.Embedded.site.css", typeof(RedditWebElement).Assembly);
        }

        public void SetBackgroundColor(DynamicColor hex)
        {
            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "body",
                Styles = { ["background-color"] = hex.ToHex() }
            }
        );
        }

        public void SetBlockQuoteColor(DynamicColor hex)
        {
            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "blockquote",
                Styles =
                {
                    ["border-left"] = $"1px solid {hex.ToHex()}",
                    ["padding-left"] = "5px",
                    ["margin"] = "0 0px 10px 20px"
                }
            });
        }

        public void SetSpoilerColor(DynamicColor hex)
        {
            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = ".md-spoiler-text",
                Styles =
                {
                    ["color"] = hex.ToHex(),
                    ["background-color"] = hex.ToHex()
                }
            });
        }
    }
}