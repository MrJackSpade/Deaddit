using Maui.WebComponents.Events;
using System.Collections;

namespace Maui.WebComponents.Classes
{
    public class StyleRuleCollection : IEnumerable<StyleRule>
    {
        private readonly List<StyleRule> _styles = [];

        public int Count => _styles.Count;

        internal event EventHandler<OnStyleRuleAddedEventArgs>? OnStyleRuleAdded;

        public void Add(StyleRule styleRule)
        {
            _styles.Add(styleRule);
            OnStyleRuleAdded?.Invoke(this, new OnStyleRuleAddedEventArgs(styleRule));
        }

        public IEnumerator<StyleRule> GetEnumerator()
        {
            return ((IEnumerable<StyleRule>)_styles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_styles).GetEnumerator();
        }
    }
}