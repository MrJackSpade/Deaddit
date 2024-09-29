using Deaddit.Core.Utils;
using Maui.WebComponents;
using Maui.WebComponents.Classes;

namespace Deaddit.Components
{
    internal class RedditWebElement : WebElement
    {
        public RedditWebElement() : base()
        {
            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "html, body",
                Styles =
                {
                    ["margin"] = "0",
                    ["padding"] = "0",
                    ["box-sizing"] = "border-box"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "body",
                Styles =
                {
                    ["font-family"] = "Arial, sans-serif"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "*",
                Styles =
                {
                    ["box-sizing"] = "border-box"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "pre",
                Styles =
                {
                    ["overflow-x"] = "scroll"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "p",
                Styles =
                {
                    ["margin"] = "0",
                    ["padding"] = "0"
                }
            });

            DocumentStyles.Add(new StyleRule()
            {
                CssSelector = "img",
                Styles =
                {
                    ["max-width"] = "100%",
                    ["max-height"] = "100vh"
                }
            });
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
                    ["padding-left"] = "5px"
                }
            });
        }
    }
}