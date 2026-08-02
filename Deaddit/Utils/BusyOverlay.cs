using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.Utils
{
    /// <summary>
    /// Visible feedback for a tap-triggered async action (submit, delete, upload, save).
    /// Renders a centered translucent rounded block with a spinner.
    ///
    /// Two ways to use:
    /// 1. Drop a &lt;BusyOverlay x:Name="..." /&gt; into a page's layout and call Show/Hide.
    /// 2. Call the static <see cref="RunAsync(Page, Func{Task})"/> from anywhere with
    ///    access to a Page; it finds an existing instance in the page's visual tree or
    ///    wraps the page's Content in a Grid and injects one.
    ///
    /// The empty area around the spinner block is input-transparent, matching the
    /// long-standing share/save overlay UX — taps on uncovered page area still work.
    /// </summary>
    public class BusyOverlay : ContentView
    {
        private readonly ActivityIndicator _indicator;

        public BusyOverlay()
        {
            IsVisible = false;
            InputTransparent = true;
            CascadeInputTransparent = false;

            _indicator = new ActivityIndicator
            {
                IsRunning = false,
                Color = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };

            Content = new Border
            {
                BackgroundColor = Color.FromArgb("#AA000000"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(30),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = false,
                Content = _indicator
            };
        }

        public void Show()
        {
            _indicator.IsRunning = true;
            IsVisible = true;
        }

        public void Hide()
        {
            _indicator.IsRunning = false;
            IsVisible = false;
        }

        public static async Task RunAsync(Page page, Func<Task> work)
        {
            BusyOverlay overlay = GetOrInject(page);
            overlay.Show();

            try
            {
                await work();
            }
            finally
            {
                overlay.Hide();
            }
        }

        public static async Task<T> RunAsync<T>(Page page, Func<Task<T>> work)
        {
            BusyOverlay overlay = GetOrInject(page);
            overlay.Show();

            try
            {
                return await work();
            }
            finally
            {
                overlay.Hide();
            }
        }

        private static BusyOverlay GetOrInject(Page page)
        {
            if (page is not ContentPage contentPage)
            {
                throw new InvalidOperationException(
                    $"{nameof(BusyOverlay)} can only host on a {nameof(ContentPage)}; got {page.GetType().Name}.");
            }

            BusyOverlay? existing = Find(contentPage);

            if (existing is not null)
            {
                return existing;
            }

            View? original = contentPage.Content;
            contentPage.Content = null;

            Grid wrapper = new();

            if (original is not null)
            {
                wrapper.Children.Add(original);
            }

            BusyOverlay injected = new();
            wrapper.Children.Add(injected);

            contentPage.Content = wrapper;
            return injected;
        }

        private static BusyOverlay? Find(Element element)
        {
            if (element is BusyOverlay overlay)
            {
                return overlay;
            }

            if (element is IVisualTreeElement vte)
            {
                foreach (IVisualTreeElement child in vte.GetVisualChildren())
                {
                    if (child is Element childElement)
                    {
                        BusyOverlay? found = Find(childElement);

                        if (found is not null)
                        {
                            return found;
                        }
                    }
                }
            }

            return null;
        }
    }
}
