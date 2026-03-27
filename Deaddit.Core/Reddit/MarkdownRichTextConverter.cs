using Reddit.Api.Models.Json.Media;

namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// Converts markdown text to Reddit's richtext JSON document format.
    /// </summary>
    public static class MarkdownRichTextConverter
    {
        public static RichTextDocument Convert(string markdown)
        {
            RichTextDocument doc = new();

            if (string.IsNullOrEmpty(markdown))
            {
                doc.Document.Add(new RichTextElement
                {
                    ElementType = RichTextElementType.Paragraph,
                    Children = [new RichTextElement { ElementType = RichTextElementType.Text, Text = "" }]
                });

                return doc;
            }

            // First extract fenced code blocks, which span across paragraph boundaries
            List<(string content, bool isCode)> blocks = ExtractCodeBlocks(markdown);

            foreach ((string content, bool isCode) in blocks)
            {
                if (isCode)
                {
                    doc.Document.Add(new RichTextElement
                    {
                        ElementType = RichTextElementType.Code,
                        Children = [new RichTextElement { ElementType = RichTextElementType.Raw, Text = content }]
                    });
                }
                else
                {
                    // Split non-code content into paragraphs on double newline
                    string[] paragraphs = content.Split("\n\n");

                    foreach (string paragraph in paragraphs)
                    {
                        AddParagraphElements(doc.Document, paragraph);
                    }
                }
            }

            return doc;
        }

        /// <summary>
        /// Splits markdown into alternating (text, false) and (code, true) segments
        /// by extracting fenced code blocks (```...```).
        /// </summary>
        private static List<(string content, bool isCode)> ExtractCodeBlocks(string markdown)
        {
            List<(string content, bool isCode)> blocks = [];
            int i = 0;

            while (i < markdown.Length)
            {
                // Look for opening ```
                int fenceStart = markdown.IndexOf("```", i);

                if (fenceStart == -1)
                {
                    // No more code blocks, rest is text
                    string remaining = markdown[i..];

                    if (remaining.Length > 0)
                    {
                        blocks.Add((remaining, false));
                    }

                    break;
                }

                // Add text before the fence
                if (fenceStart > i)
                {
                    string before = markdown[i..fenceStart];

                    // Trim trailing \n\n that precedes the fence
                    if (before.EndsWith("\n\n"))
                    {
                        before = before[..^2];
                    }

                    if (before.Length > 0)
                    {
                        blocks.Add((before, false));
                    }
                }

                // Skip past opening ``` and optional language identifier
                int lineEnd = markdown.IndexOf('\n', fenceStart);

                if (lineEnd == -1)
                {
                    // Unclosed fence at end of string, treat as text
                    blocks.Add((markdown[fenceStart..], false));
                    break;
                }

                int codeStart = lineEnd + 1;

                // Find closing ```
                int fenceEnd = markdown.IndexOf("\n```", codeStart);

                if (fenceEnd == -1)
                {
                    // Unclosed fence, treat rest as code
                    string code = markdown[codeStart..];
                    blocks.Add((code, true));
                    i = markdown.Length;
                    break;
                }

                string codeContent = markdown[codeStart..fenceEnd];
                blocks.Add((codeContent, true));

                // Move past closing ``` and optional trailing newlines
                i = fenceEnd + 4; // "\n```" is 4 chars

                // Skip trailing \n\n after the closing fence
                if (i < markdown.Length && markdown[i] == '\n')
                {
                    i++;
                }

                if (i < markdown.Length && markdown[i] == '\n')
                {
                    i++;
                }
            }

            return blocks;
        }

        /// <summary>
        /// Checks if the paragraph is a blockquote (lines starting with >) and handles accordingly.
        /// </summary>
        private static void AddParagraphElements(List<RichTextElement> document, string rawParagraph)
        {
            string trimmed = rawParagraph.TrimStart();

            if (trimmed.StartsWith('>') && !trimmed.StartsWith(">!"))
            {
                // Strip > prefix from each line to get inner content
                string[] lines = rawParagraph.Split('\n');
                string inner = string.Join("\n", lines.Select(l =>
                {
                    string lt = l.TrimStart();
                    if (lt.StartsWith("> "))
                    {
                        return lt[2..];
                    }

                    if (lt.StartsWith('>'))
                    {
                        return lt[1..];
                    }

                    return l;
                }));

                List<RichTextElement> blockquoteChildren = [];
                AddParagraphElements(blockquoteChildren, inner);

                document.Add(new RichTextElement
                {
                    ElementType = RichTextElementType.Blockquote,
                    Children = blockquoteChildren
                });
            }
            else
            {
                AddParagraphContent(document, rawParagraph);
            }
        }

        /// <summary>
        /// Splits a paragraph around ![image](key) placeholders, adding
        /// text paragraphs and image elements directly to the document.
        /// </summary>
        private static void AddParagraphContent(List<RichTextElement> document, string paragraph)
        {
            int lastIndex = 0;
            int i = 0;

            while (i < paragraph.Length)
            {
                // Look for ![image](
                if (paragraph[i] == '!' && i + 9 < paragraph.Length && paragraph.AsSpan(i, 9).SequenceEqual("![image]("))
                {
                    int closeP = paragraph.IndexOf(')', i + 9);

                    if (closeP != -1)
                    {
                        // Flush text before the image
                        if (i > lastIndex)
                        {
                            string before = paragraph[lastIndex..i];
                            document.Add(new RichTextElement
                            {
                                ElementType = RichTextElementType.Paragraph,
                                Children = ParseInline(before)
                            });
                        }

                        string mediaKey = paragraph[(i + 9)..closeP];
                        document.Add(new RichTextElement
                        {
                            ElementType = RichTextElementType.Image,
                            Id = mediaKey
                        });

                        i = closeP + 1;
                        lastIndex = i;
                        continue;
                    }
                }

                i++;
            }

            // Flush remaining text
            if (lastIndex < paragraph.Length)
            {
                string remaining = paragraph[lastIndex..];
                document.Add(new RichTextElement
                {
                    ElementType = RichTextElementType.Paragraph,
                    Children = ParseInline(remaining)
                });
            }
            else if (lastIndex == 0 && paragraph.Length == 0)
            {
                document.Add(new RichTextElement
                {
                    ElementType = RichTextElementType.Paragraph,
                    Children = [new RichTextElement { ElementType = RichTextElementType.Text, Text = "" }]
                });
            }
        }

        private static List<RichTextElement> ParseInline(string text)
        {
            List<RichTextElement> elements = [];

            if (string.IsNullOrEmpty(text))
            {
                elements.Add(new RichTextElement { ElementType = RichTextElementType.Text, Text = "" });
                return elements;
            }

            AddTextRuns(elements, text);

            if (elements.Count == 0)
            {
                elements.Add(new RichTextElement { ElementType = RichTextElementType.Text, Text = "" });
            }

            return elements;
        }

        /// <summary>
        /// Parses markdown inline formatting (bold, italic, strikethrough, code, links)
        /// into richtext text elements with format arrays.
        /// </summary>
        private static void AddTextRuns(List<RichTextElement> elements, string text)
        {
            // Reddit richtext format codes:
            // 1 = bold
            // 2 = italic
            // 4 = strikethrough
            // 8 = superscript
            // 16 = spoiler (not standard markdown)
            // 32 = inline code
            // 64 = link (not used in format, links are separate elements)

            List<(int format, int start, int length)> formats = [];
            string plainText = ExtractFormats(text, formats, elements);

            if (string.IsNullOrEmpty(plainText) && elements.Count > 0)
            {
                return;
            }

            RichTextElement element = new()
            {
                ElementType = RichTextElementType.Text,
                Text = plainText
            };

            if (formats.Count > 0)
            {
                element.Format = formats.Select(f => new List<int> { f.format, f.start, f.length }).ToList();
            }

            elements.Add(element);
        }

        /// <summary>
        /// Extracts formatting markers from markdown text, returning the plain text
        /// and populating the formats list with (formatCode, startIndex, length) tuples.
        /// Links are added directly to elements as separate richtext elements.
        /// </summary>
        private static string ExtractFormats(
            string markdown,
            List<(int format, int start, int length)> formats,
            List<RichTextElement> elements)
        {
            char[] result = new char[markdown.Length];
            int resultLen = 0;
            int i = 0;

            while (i < markdown.Length)
            {
                // Inline code: `code`
                if (markdown[i] == '`')
                {
                    int end = markdown.IndexOf('`', i + 1);

                    if (end != -1)
                    {
                        string code = markdown[(i + 1)..end];
                        int start = resultLen;
                        code.CopyTo(0, result, resultLen, code.Length);
                        resultLen += code.Length;
                        formats.Add((32, start, code.Length));
                        i = end + 1;
                        continue;
                    }
                }

                // Bold+Italic: ***text*** or ___text___
                if (i + 2 < markdown.Length &&
                    ((markdown[i] == '*' && markdown[i + 1] == '*' && markdown[i + 2] == '*') ||
                     (markdown[i] == '_' && markdown[i + 1] == '_' && markdown[i + 2] == '_')))
                {
                    char marker = markdown[i];
                    string closing = new(marker, 3);
                    int end = markdown.IndexOf(closing, i + 3);

                    if (end != -1)
                    {
                        string inner = markdown[(i + 3)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((1, start, inner.Length)); // bold
                        formats.Add((2, start, inner.Length)); // italic
                        i = end + 3;
                        continue;
                    }
                }

                // Bold: **text** or __text__
                if (i + 1 < markdown.Length &&
                    ((markdown[i] == '*' && markdown[i + 1] == '*') ||
                     (markdown[i] == '_' && markdown[i + 1] == '_')))
                {
                    char marker = markdown[i];
                    string closing = new(marker, 2);
                    int end = markdown.IndexOf(closing, i + 2);

                    if (end != -1)
                    {
                        string inner = markdown[(i + 2)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((1, start, inner.Length));
                        i = end + 2;
                        continue;
                    }
                }

                // Italic: *text* or _text_
                if (markdown[i] == '*' || markdown[i] == '_')
                {
                    char marker = markdown[i];
                    int end = markdown.IndexOf(marker, i + 1);

                    if (end != -1 && end > i + 1)
                    {
                        string inner = markdown[(i + 1)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((2, start, inner.Length));
                        i = end + 1;
                        continue;
                    }
                }

                // Strikethrough: ~~text~~
                if (i + 1 < markdown.Length && markdown[i] == '~' && markdown[i + 1] == '~')
                {
                    int end = markdown.IndexOf("~~", i + 2);

                    if (end != -1)
                    {
                        string inner = markdown[(i + 2)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((4, start, inner.Length));
                        i = end + 2;
                        continue;
                    }
                }

                // Superscript: ^(text) or ^word
                if (markdown[i] == '^')
                {
                    if (i + 1 < markdown.Length && markdown[i + 1] == '(')
                    {
                        int end = markdown.IndexOf(')', i + 2);

                        if (end != -1)
                        {
                            string inner = markdown[(i + 2)..end];
                            int start = resultLen;
                            inner.CopyTo(0, result, resultLen, inner.Length);
                            resultLen += inner.Length;
                            formats.Add((8, start, inner.Length));
                            i = end + 1;
                            continue;
                        }
                    }
                    else if (i + 1 < markdown.Length && !char.IsWhiteSpace(markdown[i + 1]))
                    {
                        int end = i + 1;

                        while (end < markdown.Length && !char.IsWhiteSpace(markdown[end]))
                        {
                            end++;
                        }

                        string inner = markdown[(i + 1)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((8, start, inner.Length));
                        i = end;
                        continue;
                    }
                }

                // Spoiler: >!text!<
                if (i + 1 < markdown.Length && markdown[i] == '>' && markdown[i + 1] == '!')
                {
                    int end = markdown.IndexOf("!<", i + 2);

                    if (end != -1)
                    {
                        string inner = markdown[(i + 2)..end];
                        int start = resultLen;
                        inner.CopyTo(0, result, resultLen, inner.Length);
                        resultLen += inner.Length;
                        formats.Add((16, start, inner.Length));
                        i = end + 2;
                        continue;
                    }
                }

                // Link: [text](url)
                if (markdown[i] == '[')
                {
                    int closeBracket = markdown.IndexOf("](", i + 1);

                    if (closeBracket != -1)
                    {
                        int closeParen = markdown.IndexOf(')', closeBracket + 2);

                        if (closeParen != -1)
                        {
                            // Flush any accumulated plain text first
                            if (resultLen > 0)
                            {
                                string preceding = new(result, 0, resultLen);
                                RichTextElement textEl = new()
                                {
                                    ElementType = RichTextElementType.Text,
                                    Text = preceding
                                };

                                if (formats.Count > 0)
                                {
                                    textEl.Format = formats.Select(f => new List<int> { f.format, f.start, f.length }).ToList();
                                    formats.Clear();
                                }

                                elements.Add(textEl);
                                resultLen = 0;
                            }

                            string linkText = markdown[(i + 1)..closeBracket];
                            string url = markdown[(closeBracket + 2)..closeParen];

                            elements.Add(new RichTextElement
                            {
                                ElementType = RichTextElementType.Link,
                                Text = linkText,
                                Url = url
                            });

                            i = closeParen + 1;
                            continue;
                        }
                    }
                }

                // Plain character
                result[resultLen++] = markdown[i];
                i++;
            }

            return new string(result, 0, resultLen);
        }
    }
}
