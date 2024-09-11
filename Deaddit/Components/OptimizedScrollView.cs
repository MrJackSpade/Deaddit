using Deaddit.Core.Utils.Extensions;
using Deaddit.Extensions;
using Microsoft.Maui.Animations;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Diagnostics;

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

        public void Add(VisualElement view)
        {
            innerStack.Add(view);
            content.Add(new RenderedElement(view)
            {
                IsRendered = true
            });
        }

        public void Clear()
        {
            innerStack.Clear();
            content.Clear();
        }

        public async void OnScrolled(object? sender, ScrolledEventArgs e)
        {
            if (_scrollSemaphore.Wait(0))
            {
                if (e.ScrollY < _lastScroll)
                {
                    this.ScrollUp(e);
                    if (ScrolledUp is not null)
                    {
                        await ScrolledUp.Invoke(e);
                    }
                }
                else
                {
                    this.ScrollDown(e);
                    if (ScrolledDown is not null)
                    {
                        await ScrolledDown.Invoke(e);
                    }
                }

                _lastScroll = e.ScrollY;

                _scrollSemaphore.Release();
            }
        }

        public void Remove(VisualElement toRemove)
        {
            innerStack.Remove(toRemove);
            RenderedElement? found = content.FirstOrDefault(x => x.Element == toRemove);

            if (found is not null)
            {
                content.Remove(found);
            }
        }

        private void ScrollDown(ScrolledEventArgs e)
        {
            if (Math.Abs(_lastRefresh - e.ScrollY) < RefreshPeriod)
            {
                return;
            }

            try
            {
                _lastRefresh = e.ScrollY;

                for (int i = 0; i < content.Count; i++)
                {
                    RenderedElement child = content[i];

                    ViewPosition position = child.IsRendered ? this.Position(child.Element, LoadBuffer) : this.Position(child.RenderedBounds, LoadBuffer);

                    switch (position)
                    {
                        case ViewPosition.Above:
                            child.FindBounds();
                            if (child.IsRendered)
                            {
                                this.innerStack.Remove(child.Element);
                                this.innerStack.Padding = new Thickness(0, this.innerStack.Padding.Top + child.RenderedBounds.Height, 0, 0);
                                child.IsRendered = false;
                            }

                            break;

                        case ViewPosition.Below:
                            return;

                        case ViewPosition.Within:
                            if (!child.IsRendered)
                            {
                                this.innerStack.Add(child.Element);
                                child.IsRendered = true;
                            }

                            break;
                    }
                }

                return;
            }
            finally
            {

            }
        }

        private void ScrollUp(ScrolledEventArgs e)
        {
            if (Math.Abs(_lastRefresh - e.ScrollY) < RefreshPeriod)
            {
                return;
            }

            List<string> log = [];

            try
            {
                _lastRefresh = e.ScrollY;

                for (int i = content.Count - 1; i >= 0; i--)
                {
                    RenderedElement child = content[i];

                    ViewPosition position = child.IsRendered ? this.Position(child.Element, LoadBuffer) : this.Position(child.RenderedBounds, LoadBuffer);

                    log.Insert(0, $"{i}: {position}");

                    switch (position)
                    {
                        case ViewPosition.Above:
                            return;

                        case ViewPosition.Below:
                            //if (child.IsRendered)
                            //{
                            //    child.FindBounds();
                            //    this.innerStack.Remove(child.Element);
                            //    this.endPadding.HeightRequest += child.RenderedBounds.Height;
                            //    child.IsRendered = false;
                            //}

                            break;

                        case ViewPosition.Within:
                            if (!child.IsRendered)
                            {
                                this.innerStack.Insert(0, child.Element);
                                this.innerStack.Padding = new Thickness(0, this.innerStack.Padding.Top - child.RenderedBounds.Height, 0, 0);
                                child.IsRendered = true;
                            }

                            break;
                    }
                }
            }
            finally
            {

            }

            return;
        }

        private class RenderedElement(VisualElement element)
        {
            public VisualElement Element { get; set; } = element ?? throw new ArgumentNullException(nameof(element));

            public Rect RenderedBounds { get; set; }

            public bool IsRendered { get; set; }

            internal void FindBounds()
            {
                RenderedBounds = Element.Bounds;
            }
        }
    }
}