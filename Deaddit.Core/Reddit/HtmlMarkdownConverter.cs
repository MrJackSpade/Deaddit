using HtmlAgilityPack;
using System.Text;
using System.Web;

namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// Converts Reddit body_html to markdown text suitable for editing.
    /// </summary>
    public static class HtmlMarkdownConverter
    {
        public static string Convert(string? html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Reddit wraps body_html in <div class="md">...</div>
            HtmlNode root = doc.DocumentNode.SelectSingleNode("//div[@class='md']") ?? doc.DocumentNode;

            StringBuilder sb = new();
            ProcessChildren(root, sb);

            // Trim trailing newlines
            while (sb.Length > 0 && sb[^1] == '\n')
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        private static void ProcessChildren(HtmlNode parent, StringBuilder sb)
        {
            foreach (HtmlNode child in parent.ChildNodes)
            {
                ProcessNode(child, sb);
            }
        }

        private static void ProcessNode(HtmlNode node, StringBuilder sb)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    string text = HttpUtility.HtmlDecode(node.InnerText);

                    // Skip whitespace-only text nodes between block elements
                    // (insignificant HTML formatting whitespace from Reddit's body_html)
                    if (string.IsNullOrWhiteSpace(text) && IsBlockContainer(node.ParentNode))
                    {
                        break;
                    }

                    sb.Append(text);
                    break;

                case HtmlNodeType.Element:
                    ProcessElement(node, sb);
                    break;
            }
        }

        private static void ProcessElement(HtmlNode node, StringBuilder sb)
        {
            switch (node.Name.ToLowerInvariant())
            {
                case "p":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    ProcessChildren(node, sb);
                    sb.Append("\n\n");
                    break;

                case "br":
                    sb.Append('\n');
                    break;

                case "strong":
                case "b":
                    sb.Append("**");
                    ProcessChildren(node, sb);
                    sb.Append("**");
                    break;

                case "em":
                case "i":
                    sb.Append('*');
                    ProcessChildren(node, sb);
                    sb.Append('*');
                    break;

                case "del":
                case "s":
                    sb.Append("~~");
                    ProcessChildren(node, sb);
                    sb.Append("~~");
                    break;

                case "sup":
                    sb.Append("^(");
                    ProcessChildren(node, sb);
                    sb.Append(')');
                    break;

                case "a":
                    string? href = node.GetAttributeValue("href", null);
                    string linkText = GetInnerText(node);

                    if (href != null)
                    {
                        // Bare URL (link text matches href)
                        if (linkText == href || linkText == HttpUtility.HtmlDecode(href))
                        {
                            sb.Append(linkText);
                        }
                        else
                        {
                            sb.Append('[');
                            sb.Append(linkText);
                            sb.Append("](");
                            sb.Append(href);
                            sb.Append(')');
                        }
                    }
                    else
                    {
                        sb.Append(linkText);
                    }

                    break;

                case "code":
                    // Check if inside a <pre> (block code) — handled by pre case
                    if (node.ParentNode?.Name == "pre")
                    {
                        sb.Append(HttpUtility.HtmlDecode(node.InnerText));
                    }
                    else
                    {
                        sb.Append('`');
                        sb.Append(HttpUtility.HtmlDecode(node.InnerText));
                        sb.Append('`');
                    }

                    break;

                case "pre":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    sb.Append("```\n");
                    ProcessChildren(node, sb);
                    sb.Append("\n```\n\n");
                    break;

                case "blockquote":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    StringBuilder inner = new();
                    ProcessChildren(node, inner);

                    // Trim trailing newlines from inner content
                    while (inner.Length > 0 && inner[^1] == '\n')
                    {
                        inner.Length--;
                    }

                    foreach (string line in inner.ToString().Split('\n'))
                    {
                        sb.Append("> ");
                        sb.Append(line);
                        sb.Append('\n');
                    }

                    sb.Append('\n');
                    break;

                case "table":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    ProcessTable(node, sb);
                    sb.Append('\n');
                    break;

                case "thead":
                case "tbody":
                case "tr":
                case "th":
                case "td":
                    // Handled by ProcessTable
                    break;

                case "span":
                    // Spoiler spans: <span class="md-spoiler-text">
                    string? cls = node.GetAttributeValue("class", null);

                    if (cls != null && cls.Contains("spoiler"))
                    {
                        sb.Append(">!");
                        ProcessChildren(node, sb);
                        sb.Append("!<");
                    }
                    else
                    {
                        ProcessChildren(node, sb);
                    }

                    break;

                case "div":
                    ProcessChildren(node, sb);
                    break;

                case "hr":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    sb.Append("---\n\n");
                    break;

                case "ul":
                case "ol":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    ProcessList(node, sb);
                    sb.Append('\n');
                    break;

                case "li":
                    // Handled by ProcessList
                    break;

                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    if (sb.Length > 0 && sb[^1] != '\n')
                    {
                        sb.Append("\n\n");
                    }

                    int level = node.Name[1] - '0';
                    sb.Append(new string('#', level));
                    sb.Append(' ');
                    ProcessChildren(node, sb);
                    sb.Append("\n\n");
                    break;

                default:
                    ProcessChildren(node, sb);
                    break;
            }
        }

        private static void ProcessTable(HtmlNode tableNode, StringBuilder sb)
        {
            List<List<string>> rows = [];
            bool headerDone = false;

            foreach (HtmlNode section in tableNode.ChildNodes)
            {
                if (section.NodeType != HtmlNodeType.Element)
                {
                    continue;
                }

                IEnumerable<HtmlNode> trNodes = section.Name == "table"
                    ? section.ChildNodes.Where(n => n.Name == "tr")
                    : section.ChildNodes.Where(n => n.Name == "tr");

                foreach (HtmlNode tr in trNodes)
                {
                    List<string> cells = [];

                    foreach (HtmlNode cell in tr.ChildNodes.Where(n => n.Name == "th" || n.Name == "td"))
                    {
                        cells.Add(GetInnerText(cell).Trim());
                    }

                    rows.Add(cells);

                    if (section.Name == "thead" && !headerDone)
                    {
                        headerDone = true;
                    }
                }

                // Add separator after thead
                if (section.Name == "thead" && rows.Count > 0)
                {
                    int colCount = rows[^1].Count;
                    List<string> separator = [];

                    for (int i = 0; i < colCount; i++)
                    {
                        separator.Add("---");
                    }

                    rows.Add(separator);
                }
            }

            foreach (List<string> row in rows)
            {
                sb.Append("| ");
                sb.Append(string.Join(" | ", row));
                sb.Append(" |\n");
            }
        }

        private static void ProcessList(HtmlNode listNode, StringBuilder sb)
        {
            bool ordered = listNode.Name == "ol";
            int index = 1;

            foreach (HtmlNode li in listNode.ChildNodes.Where(n => n.Name == "li"))
            {
                if (ordered)
                {
                    sb.Append($"{index}. ");
                    index++;
                }
                else
                {
                    sb.Append("- ");
                }

                StringBuilder liContent = new();
                ProcessChildren(li, liContent);

                // Trim trailing newlines from li content
                while (liContent.Length > 0 && liContent[^1] == '\n')
                {
                    liContent.Length--;
                }

                sb.Append(liContent);
                sb.Append('\n');
            }
        }

        private static string GetInnerText(HtmlNode node)
        {
            StringBuilder sb = new();
            ProcessChildren(node, sb);
            return sb.ToString();
        }

        private static bool IsBlockContainer(HtmlNode? node)
        {
            if (node == null)
            {
                return false;
            }

            return node.Name.ToLowerInvariant() switch
            {
                "div" or "blockquote" or "ul" or "ol" or "li" or "table" or "thead" or "tbody" or "tr" => true,
                _ => false
            };
        }
    }
}
