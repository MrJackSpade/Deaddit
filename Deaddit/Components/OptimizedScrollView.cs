namespace Deaddit.Components
{
    internal class OptimizedScrollView : ScrollView
    {
        public Func<ScrolledEventArgs, Task> ScrolledDown;

        public Func<ScrolledEventArgs, Task> ScrolledUp;

        private readonly SemaphoreSlim _scrollSemaphore = new(1);

        private readonly List<RenderedElement> content = [];

        private readonly VerticalStackLayout innerStack;

        private double _lastRefresh;

        private double _lastScroll;

        public int HeaderCount { get; set; }

        public OptimizedScrollView()
        {
            innerStack = [];
            Content = innerStack;
            Scrolled += this.OnScrolled;
        }

        public Layout InnerStack => innerStack;

        public double LoadBuffer { get; set; } = 0;

        public double RefreshPeriod { get; set; } = 0;

        public double Spacing
        {
            get => innerStack.Spacing;
            set => innerStack.Spacing = value;
        }

        public void Add(VisualElement view, bool isContent = true)
        {
            innerStack.Add(view);

            if (isContent)
            {
                content.Add(new RenderedElement(view)
                {
                    IsRendered = true
                });
            }
        }

        public void Clear()
        {
            innerStack.Clear();
            content.Clear();
        }

        public async void OnScrolled(object? sender, ScrolledEventArgs e)
        {

            if (e.ScrollY < _lastScroll)
            {
                if (_scrollSemaphore.Wait(0))
                {
                    this.ScrollUp(e);
                    _scrollSemaphore.Release();
                }

                if (ScrolledUp is not null)
                {
                    await ScrolledUp.Invoke(e);
                }
            }
            else
            {
                if (_scrollSemaphore.Wait(0))
                {
                    this.ScrollDown(e);
                    _scrollSemaphore.Release();
                }

                if (ScrolledDown is not null)
                {
                    await ScrolledDown.Invoke(e);
                }
            }

            _lastScroll = e.ScrollY;
        }

        public void Remove(VisualElement toRemove)
        {
            innerStack.Remove(toRemove);

            RenderedElement? found = content.FirstOrDefault(x => x.Element == toRemove);

            if (found != null)
            {
                content.Remove(found);
            }
        }

        private void ScrollDown(ScrolledEventArgs e)
        {
            _lastRefresh = e.ScrollY;

            this.RefreshView();
        }

        private void RefreshView()
        {
            if (ScrollY < innerStack.Padding.Top)
            {
                foreach (VisualElement element in innerStack.OfType<VisualElement>().Where(v => !v.IsVisible).Reverse())
                {
                    element.IsVisible = true;
                    innerStack.Padding = new Thickness(0, innerStack.Padding.Top - element.Height, 0, innerStack.Padding.Bottom);

                    if (ScrollY >= innerStack.Padding.Top)
                    {
                        break;
                    }
                }
            }

            if (ScrollY > innerStack.Padding.Top)
            {
                foreach (VisualElement element in innerStack.OfType<VisualElement>().Where(v => v.IsVisible))
                {
                    if(element.Height > ScrollY - innerStack.Padding.Top)
                    {
                        return;
                    }

                    element.IsVisible = false;
                    innerStack.Padding = new Thickness(0, innerStack.Padding.Top + element.Height, 0, innerStack.Padding.Bottom);
                }
            }
        }

        private void ScrollUp(ScrolledEventArgs e)
        {
            _lastRefresh = e.ScrollY;

            this.RefreshView();
        }

        private class RenderedElement(VisualElement element)
        {
            public VisualElement Element { get; set; } = element ?? throw new ArgumentNullException(nameof(element));

            public Rect RenderedBounds { get; set; }

            public bool IsRendered
            {
                get => Element.IsVisible;
                set => Element.IsVisible = value;
            }

            internal void FindBounds()
            {
                RenderedBounds = Element.Bounds;
            }
        }
    }
}