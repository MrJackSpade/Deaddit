using Deaddit.Extensions;
using Deaddit.Utils;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Image = Microsoft.Maui.Controls.Image;

namespace Deaddit.MAUI.Components
{
    public class LinkEventArgs : EventArgs
    {
        public string? Url { get; set; }
    }

    /// <summary>
    /// https://github.com/0xc3u/Indiko.Maui.Controls.Markdown/blob/main/src/Indiko.Maui.Controls.Markdown/MarkdownView.cs
    /// </summary>
    public class MarkdownView : ContentView
    {
        public static readonly BindableProperty BlockQuoteBackgroundColorProperty =
         BindableProperty.Create(nameof(BlockQuoteBackgroundColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty BlockQuoteBorderColorProperty =
          BindableProperty.Create(nameof(BlockQuoteBorderColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty BlockQuoteFontFaceProperty =
         BindableProperty.Create(nameof(BlockQuoteFontFace), typeof(string), typeof(MarkdownView), defaultValue: "Consolas", propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty BlockQuoteTextColorProperty =
          BindableProperty.Create(nameof(BlockQuoteTextColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty CodeBlockBackgroundColorProperty =
           BindableProperty.Create(nameof(CodeBlockBackgroundColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty CodeBlockBorderColorProperty =
           BindableProperty.Create(nameof(CodeBlockBorderColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty CodeBlockFontFaceProperty =
          BindableProperty.Create(nameof(CodeBlockFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty CodeBlockFontSizeProperty =
           BindableProperty.Create(nameof(CodeBlockFontSize), typeof(double), typeof(MarkdownView), defaultValue: 12d, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty CodeBlockTextColorProperty =
           BindableProperty.Create(nameof(CodeBlockTextColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty H1ColorProperty =
            BindableProperty.Create(nameof(H1Color), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty H1FontSizeProperty =
          BindableProperty.Create(nameof(H1FontSize), typeof(double), typeof(MarkdownView), defaultValue: 24d, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty H2ColorProperty =
            BindableProperty.Create(nameof(H2Color), typeof(Color), typeof(MarkdownView), Colors.DarkGray, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty H2FontSizeProperty =
         BindableProperty.Create(nameof(H2FontSize), typeof(double), typeof(MarkdownView), defaultValue: 20d, propertyChanged: OnMarkdownTextChanged);

        // H3Color property
        public static readonly BindableProperty H3ColorProperty =
            BindableProperty.Create(nameof(H3Color), typeof(Color), typeof(MarkdownView), Colors.Gray, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty H3FontSizeProperty =
         BindableProperty.Create(nameof(H3FontSize), typeof(double), typeof(MarkdownView), defaultValue: 18d, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty HyperlinkColorProperty =
         BindableProperty.Create(nameof(HyperlinkColor), typeof(Color), typeof(MarkdownView), Colors.BlueViolet, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty ImageAspectProperty =
           BindableProperty.Create(nameof(ImageAspect), typeof(Aspect), typeof(MarkdownView), defaultValue: Aspect.AspectFit, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty LineBreakModeHeaderProperty =
           BindableProperty.Create(nameof(LineBreakModeHeader), typeof(LineBreakMode), typeof(MarkdownView), LineBreakMode.TailTruncation, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty LineBreakModeTextProperty =
           BindableProperty.Create(nameof(LineBreakModeText), typeof(LineBreakMode), typeof(MarkdownView), LineBreakMode.WordWrap, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty LineColorProperty =
        BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(MarkdownView), Colors.LightGray, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty LinkCommandParameterProperty =
            BindableProperty.Create(nameof(LinkCommandParameter), typeof(object), typeof(MarkdownView));

        public static readonly BindableProperty LinkCommandProperty =
        BindableProperty.Create(nameof(LinkCommand), typeof(ICommand), typeof(MarkdownView));

        public static readonly BindableProperty MarkdownTextProperty =
            BindableProperty.Create(nameof(MarkdownText), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty TextColorProperty =
           BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(MarkdownView), Colors.Black, propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty TextFontFaceProperty =
          BindableProperty.Create(nameof(TextFontFace), typeof(string), typeof(MarkdownView), propertyChanged: OnMarkdownTextChanged);

        public static readonly BindableProperty TextFontSizeProperty =
           BindableProperty.Create(nameof(TextFontSize), typeof(double), typeof(MarkdownView), defaultValue: 12d, propertyChanged: OnMarkdownTextChanged);

        private Dictionary<string, ImageSource> _imageCache = [];

        ~MarkdownView()
        {
            _imageCache.Clear();
            _imageCache = null;
        }

        public delegate void HyperLinkClicked(object sender, LinkEventArgs e);

        public event HyperLinkClicked? OnHyperLinkClicked;

        public Color BlockQuoteBackgroundColor
        {
            get => (Color)this.GetValue(BlockQuoteBackgroundColorProperty);
            set => this.SetValue(BlockQuoteBackgroundColorProperty, value);
        }

        public Color BlockQuoteBorderColor
        {
            get => (Color)this.GetValue(BlockQuoteBorderColorProperty);
            set => this.SetValue(BlockQuoteBorderColorProperty, value);
        }

        public string BlockQuoteFontFace
        {
            get => (string)this.GetValue(BlockQuoteFontFaceProperty);
            set => this.SetValue(BlockQuoteFontFaceProperty, value);
        }

        public Color BlockQuoteTextColor
        {
            get => (Color)this.GetValue(BlockQuoteTextColorProperty);
            set => this.SetValue(BlockQuoteTextColorProperty, value);
        }

        public Color CodeBlockBackgroundColor
        {
            get => (Color)this.GetValue(CodeBlockBackgroundColorProperty);
            set => this.SetValue(CodeBlockBackgroundColorProperty, value);
        }

        public Color CodeBlockBorderColor
        {
            get => (Color)this.GetValue(CodeBlockBorderColorProperty);
            set => this.SetValue(CodeBlockBorderColorProperty, value);
        }

        public string CodeBlockFontFace
        {
            get => (string)this.GetValue(CodeBlockFontFaceProperty);
            set => this.SetValue(CodeBlockFontFaceProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double CodeBlockFontSize
        {
            get => (double)this.GetValue(CodeBlockFontSizeProperty);
            set => this.SetValue(CodeBlockFontSizeProperty, value);
        }

        public Color CodeBlockTextColor
        {
            get => (Color)this.GetValue(CodeBlockTextColorProperty);
            set => this.SetValue(CodeBlockTextColorProperty, value);
        }

        public Color H1Color
        {
            get => (Color)this.GetValue(H1ColorProperty);
            set => this.SetValue(H1ColorProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double H1FontSize
        {
            get => (double)this.GetValue(H1FontSizeProperty);
            set => this.SetValue(H1FontSizeProperty, value);
        }

        public Color H2Color
        {
            get => (Color)this.GetValue(H2ColorProperty);
            set => this.SetValue(H2ColorProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double H2FontSize
        {
            get => (double)this.GetValue(H2FontSizeProperty);
            set => this.SetValue(H2FontSizeProperty, value);
        }

        public Color H3Color
        {
            get => (Color)this.GetValue(H3ColorProperty);
            set => this.SetValue(H3ColorProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double H3FontSize
        {
            get => (double)this.GetValue(H3FontSizeProperty);
            set => this.SetValue(H3FontSizeProperty, value);
        }

        public Color HyperlinkColor
        {
            get => (Color)this.GetValue(HyperlinkColorProperty);
            set => this.SetValue(HyperlinkColorProperty, value);
        }

        public Aspect ImageAspect
        {
            get => (Aspect)this.GetValue(ImageAspectProperty);
            set => this.SetValue(ImageAspectProperty, value);
        }

        public LineBreakMode LineBreakModeHeader
        {
            get => (LineBreakMode)this.GetValue(LineBreakModeHeaderProperty);
            set => this.SetValue(LineBreakModeHeaderProperty, value);
        }

        public LineBreakMode LineBreakModeText
        {
            get => (LineBreakMode)this.GetValue(LineBreakModeTextProperty);
            set => this.SetValue(LineBreakModeTextProperty, value);
        }

        public Color LineColor
        {
            get => (Color)this.GetValue(LineColorProperty);
            set => this.SetValue(LineColorProperty, value);
        }

        public ICommand LinkCommand
        {
            get => (ICommand)this.GetValue(LinkCommandProperty);
            set => this.SetValue(LinkCommandProperty, value);
        }

        public object LinkCommandParameter
        {
            get => this.GetValue(LinkCommandParameterProperty);
            set => this.SetValue(LinkCommandParameterProperty, value);
        }

        public string MarkdownText
        {
            get => (string)this.GetValue(MarkdownTextProperty);
            set => this.SetValue(MarkdownTextProperty, value);
        }

        /* ****** Text Styling ******** */

        public Color TextColor
        {
            get => (Color)this.GetValue(TextColorProperty);
            set => this.SetValue(TextColorProperty, value);
        }

        public string TextFontFace
        {
            get => (string)this.GetValue(TextFontFaceProperty);
            set => this.SetValue(TextFontFaceProperty, value);
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double TextFontSize
        {
            get => (double)this.GetValue(TextFontSizeProperty);
            set => this.SetValue(TextFontSizeProperty, value);
        }

        internal void TriggerHyperLinkClicked(string url)
        {
            OnHyperLinkClicked?.Invoke(this, new LinkEventArgs { Url = url });

            if (LinkCommand?.CanExecute(url) == true)
            {
                LinkCommand.Execute(url);
            }
        }

        private static void HandleActiveCodeBlock(string line, ref Label activeCodeBlockLabel, ref int gridRow)
        {
            if (MarkDownHelper.IsCodeBlock(line, out bool _))
            {
                activeCodeBlockLabel = null;
                gridRow++;
            }
            else
            {
                activeCodeBlockLabel.Text += (string.IsNullOrWhiteSpace(activeCodeBlockLabel.Text) ? "" : "\n") + line;
            }
        }

        private static void OnMarkdownTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MarkdownView control = (MarkdownView)bindable;
            control.RenderMarkdown();
        }

        private void AddBulletPointToGrid(Grid grid, int gridRow)
        {
            string bulletPointSign = "•";

#if ANDROID
            bulletPointSign = "\u2022";
#endif
#if iOS
            bulletPointSign = "\u2029";
#endif
            Label bulletPoint = new()
            {
                Text = bulletPointSign,
                FontSize = TextFontSize,
                FontFamily = TextFontFace,
                TextColor = TextColor,
                FontAutoScalingEnabled = true,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 0),
                Padding = new Thickness(0, 0)
            };

            grid.Children.Add(bulletPoint);
            Grid.SetRow(bulletPoint, gridRow);
            Grid.SetColumn(bulletPoint, 0);
        }

        private static void AddEmptyRow(Grid grid, ref int gridRow)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            gridRow++;
        }

        private void AddListItemTextToGrid(string listItemText, Grid grid, int gridRow)
        {
            FormattedString formattedString = this.CreateFormattedString(listItemText, TextColor);

            Label listItemLabel = new()
            {
                FormattedText = formattedString,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(15, 0, 0, 0) // Indent the list item text
            };

            grid.Children.Add(listItemLabel);
            Grid.SetRow(listItemLabel, gridRow);
            Grid.SetColumn(listItemLabel, 1);
        }

        private void AddOrderedListItemToGrid(int listItemIndex, Grid grid, int gridRow)
        {
            Label orderedListItem = new()
            {
                Text = $"{listItemIndex}.",
                FontSize = TextFontSize,
                FontFamily = TextFontFace,
                TextColor = TextColor,
                FontAutoScalingEnabled = true,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0),
                Padding = new Thickness(0)
            };

            grid.Children.Add(orderedListItem);
            Grid.SetRow(orderedListItem, gridRow);
            Grid.SetColumn(orderedListItem, 0);
        }

        private Frame CreateCodeBlock(string codeText, out Label contentLabel)
        {
            Label content = new()
            {
                Text = codeText.Trim('`', ' '),
                FontSize = CodeBlockFontSize,
                FontAutoScalingEnabled = true,
                FontFamily = CodeBlockFontFace,
                TextColor = CodeBlockTextColor,
                BackgroundColor = Colors.Transparent
            };
            contentLabel = content;
            return new Frame
            {
                Padding = new Thickness(10),
                CornerRadius = 4,
                BackgroundColor = CodeBlockBackgroundColor,
                BorderColor = CodeBlockBorderColor,
                Content = content
            };
        }

        private FormattedString CreateFormattedString(string line, Color textColor)
        {
            FormattedString formattedString = new();

            string[] parts = Regex.Split(line, MarkDownHelper.MARKDOWN_PATTERN);

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                Span span = new();

                if(part.TryTrim(">!", "!<", out string? trimmed))
                {
                    span.Text = trimmed;
                    span.BackgroundColor = textColor;
                    span.FontFamily = CodeBlockFontFace;
                    span.TextColor = textColor;

                    TapGestureRecognizer linkTapGestureRecognizer = new();
                    linkTapGestureRecognizer.Tapped += (_, _) => span.BackgroundColor = new Color(0,0,0,0);
                    span.GestureRecognizers.Add(linkTapGestureRecognizer);
                } else if (part.TryTrim('`', out trimmed))
                {
                    span.Text = trimmed;
                    span.BackgroundColor = CodeBlockBackgroundColor;
                    span.FontFamily = CodeBlockFontFace;
                    span.TextColor = CodeBlockTextColor;
                }
                else if (part.TryTrim("__", out trimmed))
                {
                    span.Text = trimmed;
                    span.FontAttributes = FontAttributes.Bold;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.TryTrim('_', out trimmed))
                {
                    span.Text = trimmed;
                    span.FontAttributes = FontAttributes.Italic;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.TryTrim("~~", out trimmed))
                {
                    span.Text = trimmed;
                    span.TextDecorations = TextDecorations.Strikethrough;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.TryTrim("***", out trimmed))
                {
                    span.Text = trimmed;
                    span.FontAttributes = FontAttributes.Bold | FontAttributes.Italic;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.TryTrim("**", out trimmed))
                {
                    span.Text = trimmed;
                    span.FontAttributes = FontAttributes.Bold;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.TryTrim("*", out trimmed))
                {
                    span.Text = trimmed;
                    span.FontAttributes = FontAttributes.Italic;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }
                else if (part.StartsWith('[') && part.Contains("](")) // Link detection
                {
                    string linkText = part.Split("](")[0] + "]";
                    string linkUrl = "(" + part.Split("](")[1];

                    span.Text = linkText[1..^1];
                    span.TextColor = HyperlinkColor;
                    span.TextDecorations = TextDecorations.Underline;
                    span.FontFamily = TextFontFace;

                    TapGestureRecognizer linkTapGestureRecognizer = new();
                    linkTapGestureRecognizer.Tapped += (_, _) => this.TriggerHyperLinkClicked(linkUrl[1..^1]);
                    span.GestureRecognizers.Add(linkTapGestureRecognizer);
                }
                else
                {
                    span.Text = part;
                    span.TextColor = textColor;
                    span.FontFamily = TextFontFace;
                }

                span.FontSize = TextFontSize;

                formattedString.Spans.Add(span);
            }

            return formattedString;
        }

        private Image CreateImageBlock(string line)
        {
            int startIndex = line.IndexOf('(') + 1;
            int endIndex = line.IndexOf(')', startIndex);
            string imageUrl = line[startIndex..endIndex];

            Image image = new()
            {
                Aspect = ImageAspect,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0),
            };

            this.LoadImageAsync(imageUrl).ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    ImageSource imageSource = task.Result;
                    MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
                }
            });

            return image;
        }

        private Grid CreateTable(string[] lines, int startIndex, int endIndex)
        {
            Grid tableGrid = new()
            {
                ColumnSpacing = 2,
                RowSpacing = 2,
                BackgroundColor = Colors.Transparent
            };

            string[] headerCells = lines[startIndex].Split('|').Select(cell => cell.Trim()).ToArray();
            for (int i = 0; i < headerCells.Length; i++)
            {
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int colIndex = 0; colIndex < headerCells.Length; colIndex++)
            {
                Label headerLabel = new()
                {
                    Text = headerCells[colIndex],
                    FontAttributes = FontAttributes.Bold,
                    FontSize = TextFontSize,
                    BackgroundColor = CodeBlockBackgroundColor,
                    TextColor = CodeBlockTextColor,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(5)
                };
                tableGrid.Children.Add(headerLabel);
                Grid.SetColumn(headerLabel, colIndex);
                Grid.SetRow(headerLabel, 0);
            }

            int rowIndex = 1;
            for (int i = startIndex + 2; i <= endIndex; i++)
            {
                string[] rowCells = lines[i].Split('|').Select(cell => cell.Trim()).ToArray();
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int colIndex = 0; colIndex < rowCells.Length; colIndex++)
                {
                    Label cellLabel = new()
                    {
                        Text = rowCells[colIndex],
                        FontSize = TextFontSize,
                        TextColor = TextColor,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = new Thickness(5)
                    };
                    tableGrid.Children.Add(cellLabel);
                    Grid.SetColumn(cellLabel, colIndex);
                    Grid.SetRow(cellLabel, rowIndex);
                }

                rowIndex++;
            }

            return tableGrid;
        }

        private void HandleBlockQuote(string line, bool lineBeforeWasBlockQuote, Grid grid, out bool currentLineIsBlockQuote, ref int gridRow)
        {
            Frame box = new()
            {
                Margin = new Thickness(0),
                BackgroundColor = BlockQuoteBorderColor,
                BorderColor = BlockQuoteBorderColor,
                CornerRadius = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            Label blockQuoteLabel = new()
            {
                FormattedText = this.CreateFormattedString(line[1..].Trim(), BlockQuoteTextColor),
                LineBreakMode = LineBreakModeText,
                FontFamily = BlockQuoteFontFace,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(5)
            };

            Grid blockQuoteGrid = new()
            {
                RowSpacing = 0,
                ColumnSpacing = 0,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 5 },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            blockQuoteGrid.Children.Add(box);
            Grid.SetRow(box, 0);
            Grid.SetColumn(box, 0);

            blockQuoteGrid.Children.Add(blockQuoteLabel);
            Grid.SetRow(blockQuoteLabel, 0);
            Grid.SetColumn(blockQuoteLabel, 1);

            Frame blockquote = new()
            {
                Padding = new Thickness(0),
                CornerRadius = 0,
                BackgroundColor = BlockQuoteBackgroundColor,
                BorderColor = BlockQuoteBackgroundColor,
                Content = blockQuoteGrid
            };

            if (lineBeforeWasBlockQuote)
            {
                blockquote.Margin = new Thickness(0, -grid.RowSpacing, 0, 0);
            }

            currentLineIsBlockQuote = true;

            grid.Children.Add(blockquote);
            Grid.SetColumnSpan(blockquote, 2);
            Grid.SetRow(blockquote, gridRow++);
        }

        private void HandleSingleLineOrStartOfCodeBlock(string line, Grid grid, ref int gridRow, bool isSingleLineCodeBlock, ref Label activeCodeBlockLabel)
        {
            Frame codeBlock = this.CreateCodeBlock(line, out Label contentLabel);
            grid.Children.Add(codeBlock);
            Grid.SetRow(codeBlock, gridRow);
            Grid.SetColumnSpan(codeBlock, 2);
            if (isSingleLineCodeBlock)
            {
                gridRow++;
            }
            else
            {
                activeCodeBlockLabel = contentLabel;
            }
        }

        private async Task<ImageSource> LoadImageAsync(string imageUrl)
        {
            ImageSource imageSource;

            try
            {
                if (System.Buffers.Text.Base64.IsValid(imageUrl))
                {
                    byte[] imageBytes = Convert.FromBase64String(imageUrl);
                    imageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uriResult))
                {
                    if (imageUrl != null && _imageCache.TryGetValue(imageUrl, out ImageSource? value))
                    {
                        return value;
                    }
                    else
                    {
                        try
                        {
                            using HttpClient httpClient = new();
                            byte[] imageBytes = await httpClient.GetByteArrayAsync(uriResult);
                            imageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                            if (imageUrl != null)
                            {
                                _imageCache[imageUrl] = imageSource;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error downloading image: {ex.Message}");
                            throw;
                        }
                    }
                }
                else
                {
                    imageSource = ImageSource.FromFile(imageUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image: {ex.Message}");
                throw;
            }

            return imageSource ?? ImageSource.FromFile("icon.png");
        }

        private void RenderMarkdown()
        {
            if (string.IsNullOrWhiteSpace(MarkdownText))
            {
                return;
            }

            // Clear existing content
            Content = null;

            Grid grid = new()
            {
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0),
                RowSpacing = 2,
                ColumnSpacing = 0,
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = 15 }, // For bullet points or ordered list numbers
                new ColumnDefinition { Width = GridLength.Star } // For text
            }
            };

            string[] lines = Regex.Split(MarkdownText, @"\r\n?|\n", RegexOptions.Compiled);
            lines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();

            int gridRow = 0;
            bool isUnorderedListActive = false;
            bool isOrderedListActive = false;
            bool currentLineIsBlockQuote = true;
            Label? activeCodeBlockLabel = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                bool lineBeforeWasBlockQuote = currentLineIsBlockQuote;
                currentLineIsBlockQuote = false;
                if (activeCodeBlockLabel == null)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                if (activeCodeBlockLabel != null)
                {
                    HandleActiveCodeBlock(line, ref activeCodeBlockLabel, ref gridRow);
                }
                else if (MarkDownHelper.IsHeadline(line, out int headlineLevel))
                {
                    string headlineText = line[(headlineLevel + 1)..].Trim();
                    Color textColor = headlineLevel == 1 ? H1Color :
                                      headlineLevel == 2 ? H2Color :
                                      headlineLevel == 3 ? H3Color : TextColor; // Default for h4-h6
                    double fontSize = headlineLevel == 1 ? H1FontSize :
                                      headlineLevel == 2 ? H2FontSize :
                                      headlineLevel == 3 ? H3FontSize : TextFontSize; // Default for h4-h6

                    Label label = new()
                    {
                        Text = headlineText,
                        TextColor = textColor,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = fontSize,
                        FontFamily = TextFontFace,
                        LineBreakMode = LineBreakModeHeader,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Center
                    };

                    grid.Children.Add(label);
                    Grid.SetColumnSpan(label, 2);
                    Grid.SetRow(label, gridRow++);
                }
                else if (MarkDownHelper.IsImage(line))
                {
                    Image image = this.CreateImageBlock(line);

                    if (image == null)
                    {
                        continue;
                    }

                    grid.Children.Add(image);
                    Grid.SetColumnSpan(image, 2);
                    Grid.SetRow(image, gridRow++);
                }
                else if (MarkDownHelper.IsBlockQuote(line))
                {
                    this.HandleBlockQuote(line, lineBeforeWasBlockQuote, grid, out currentLineIsBlockQuote, ref gridRow);
                }
                else if (MarkDownHelper.IsUnorderedList(line))
                {
                    if (!isUnorderedListActive)
                    {
                        isUnorderedListActive = true;
                    }

                    this.AddBulletPointToGrid(grid, gridRow);
                    this.AddListItemTextToGrid(line[2..], grid, gridRow);

                    gridRow++;
                }
                else if (MarkDownHelper.IsOrderedList(line, out int listItemIndex))
                {
                    if (!isOrderedListActive)
                    {
                        isOrderedListActive = true;
                    }

                    this.AddOrderedListItemToGrid(listItemIndex, grid, gridRow);
                    this.AddListItemTextToGrid(line[(listItemIndex.ToString().Length + 2)..], grid, gridRow);

                    gridRow++;
                }
                else if (MarkDownHelper.IsCodeBlock(line, out bool isSingleLineCodeBlock))
                {
                    this.HandleSingleLineOrStartOfCodeBlock(line, grid, ref gridRow, isSingleLineCodeBlock, ref activeCodeBlockLabel);
                }
                else if (MarkDownHelper.IsHorizontalRule(line))
                {
                    BoxView horizontalLine = new()
                    {
                        HeightRequest = 2,
                        Color = LineColor,
                        BackgroundColor = LineColor,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Center
                    };

                    grid.Children.Add(horizontalLine);
                    Grid.SetRow(horizontalLine, gridRow);
                    Grid.SetColumnSpan(horizontalLine, 2);
                    gridRow++;
                }
                else if (MarkDownHelper.IsTable(lines, i, out int tableEndIndex)) // Detect table
                {
                    Grid table = this.CreateTable(lines, i, tableEndIndex);
                    grid.Children.Add(table);
                    Grid.SetColumnSpan(table, 2);
                    Grid.SetRow(table, gridRow++);
                    i = tableEndIndex; // Skip processed lines
                }
                else // Regular text
                {
                    if (isUnorderedListActive || isOrderedListActive)
                    {
                        isUnorderedListActive = false;
                        isOrderedListActive = false;
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        grid.Children.Add(new BoxView { Color = Colors.Transparent });
                        gridRow++;
                    }

                    FormattedString formattedString = this.CreateFormattedString(line, TextColor);
                    Label label = new()
                    {
                        FormattedText = formattedString,
                        LineBreakMode = LineBreakModeText,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Start
                    };

                    grid.Children.Add(label);
                    Grid.SetRow(label, gridRow);
                    Grid.SetColumn(label, 0);
                    Grid.SetColumnSpan(label, 2);

                    gridRow++;

                    // never finish with empty superfluous empty line
                    if (i != lines.Length - 1)
                    {
                        AddEmptyRow(grid, ref gridRow);
                    }
                }
            }

            Content = grid;
        }
    }
}