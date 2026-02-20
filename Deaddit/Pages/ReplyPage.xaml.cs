using Deaddit.Components.WebComponents;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Configurations;
using Deaddit.Configurations.Ai;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Reddit.Api.Interfaces;
using Reddit.Api.Models.Api;
using System.Text;

namespace Deaddit.Pages
{
    public partial class ReplyPage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly IClaudeService _claudeService;

        private readonly IDisplayMessages _displayExceptions;

        private readonly IRedditClient _redditClient;

        private readonly ApiThing _replyTo;

        private readonly ApiThing _toEdit;

        public event EventHandler<ReplySubmittedEventArgs>? OnSubmitted;

        private readonly AIConfiguration _aiConfiguration;

        public ReplyPage(ApiThing? replyTo, ApiThing? toEdit, AIConfiguration aiConfiguration, IClaudeService claudeService, IDisplayMessages displayExceptions, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling)
        {
            _redditClient = redditClient;
            _replyTo = replyTo ?? toEdit.Parent;
            _displayExceptions = displayExceptions;
            _toEdit = toEdit;
            _appNavigator = appNavigator;
            _claudeService = claudeService;
            _aiConfiguration = aiConfiguration;

            BindingContext = new ReplyPageViewModel(applicationStyling);

            this.InitializeComponent();

            if (_aiConfiguration.Prompts.Count > 0)
            {
                Button aiButton = new()
                {
                    Text = "AI Assist",
                    TextColor = applicationStyling.TextColor.ToMauiColor(),
                    BackgroundColor = Color.FromArgb("#00000000"),
                    FontSize = applicationStyling.TitleFontSize,
                    Padding = new Thickness(10),
                };

                aiButton.Clicked += async (s, e) =>
                {
                    string prompt = await this.DisplayActionSheet("Select AI Prompt", "Cancel", null, [.. _aiConfiguration.Prompts.Select(p => p.DisplayName)]);

                    string additionalContext = string.Empty;

                    AiPrompt? selectedPrompt = _aiConfiguration.Prompts.FirstOrDefault(p => p.DisplayName == prompt);

                    if (selectedPrompt != null)
                    {
                        string systemPrompt = selectedPrompt.TextContent;

                        if (selectedPrompt.TextContent.Contains("{0}"))
                        {
                            additionalContext = await this.DisplayPromptAsync("Additional Context", "Enter any additional instructions for the AI");

                            systemPrompt = string.Format(systemPrompt, additionalContext);
                        }

                        string history = this.GenerateAiHistory(_replyTo);

                        // Show activity indicator
                        aiButton.IsEnabled = false;
                        string originalText = aiButton.Text;
                        aiButton.Text = "Loading...";

                        ActivityIndicator activityIndicator = new()
                        {
                            IsRunning = true,
                            IsVisible = true,
                            Color = applicationStyling.TextColor.ToMauiColor()
                        };
                        actionButtonsStack.Children.Add(activityIndicator);

                        try
                        {
                            textEditor.Text = await _claudeService.SendMessageAsync(systemPrompt, history, selectedPrompt.Model.ToModelId());
                        }
                        finally
                        {
                            // Hide activity indicator
                            activityIndicator.IsRunning = false;
                            activityIndicator.IsVisible = false;
                            actionButtonsStack.Children.Remove(activityIndicator);
                            aiButton.Text = originalText;
                            aiButton.IsEnabled = true;
                        }
                    }
                };

                actionButtonsStack.Children.Add(aiButton);
            }

            webElement.SetColors(applicationStyling);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            ApiThing? toRender = _replyTo;
            SelectionGroup unused = new();
            do
            {
                if (toRender is ApiComment rc)
                {
                    RedditCommentWebComponent redditCommentComponent = _appNavigator.CreateCommentWebComponent(rc, null, unused);

                    webElement.InsertChild(0, redditCommentComponent);
                }
                else if (toRender is ApiPost post)
                {
                    RedditPostWebComponent redditPostComponent = _appNavigator.CreatePostWebComponent(post, PostState.None, null);

                    if (!string.IsNullOrWhiteSpace(post.Body))
                    {
                        webElement.InsertChild(0, new PostBodyComponent(post, applicationStyling));
                    }

                    webElement.InsertChild(0, redditPostComponent);
                }

                toRender = toRender.Parent;
            } while (toRender != null);

            if (toEdit != null)
            {
                textEditor.Text = toEdit.Body;
            }
        }

        private string GenerateAiHistory(ApiThing replyTo)
        {
            string history = string.Empty;

            if (replyTo is ApiPost post)
            {
                history = $"**{post.Author}**: {post.Title}\n\n";
                
                if(!string.IsNullOrWhiteSpace(post.Body))
                {
                    history +=  post.Body + "\n\n";
                }
            }
            else
            {
                history = $"**{replyTo.Author}**: {replyTo.Body}\n\n";
            }

            if(replyTo.Parent != null)
            {
                history = this.GenerateAiHistory(replyTo.Parent) + history;
            }

            return history;
        }

        public async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void OnPreviewClicked(object? sender, EventArgs e)
        {
        }

        public async void OnSubmitClicked(object? sender, EventArgs e)
        {

            ApiComment meta;

            if (_toEdit is ApiComment comment)
            {
                comment.Body = textEditor.Text;
                meta = await _redditClient.Update(comment);
            }
            else
            {
                string commentBody = textEditor.Text;
                meta = await _redditClient.Comment(_replyTo, commentBody);
            }

            if (meta != null)
            {
                OnSubmitted?.Invoke(this, new ReplySubmittedEventArgs(_replyTo, meta));

                await Navigation.PopAsync();
            }
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayExceptions.DisplayException(e);
        }
    }
}