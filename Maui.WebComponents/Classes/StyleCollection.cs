using Maui.WebComponents.Events;
using System.Collections;

namespace Maui.WebComponents.Classes
{
    public class StyleCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> _styles = [];

        public int Count => _styles.Count;

        internal event EventHandler<OnStyleChangedEventArgs>? OnStyleChanged;

        public string this[string key]
        {
            get => _styles[key];
            set => this.SetValue(key, value);
        }

        public void Add(string key, string value)
        {
            _styles.Add(key, value);
            OnStyleChanged?.Invoke(this, new OnStyleChangedEventArgs { Key = key, Value = value });
        }

        public bool ContainsKey(string key) => _styles.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)_styles).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_styles).GetEnumerator();
        }

        public void Remove(string key)
        {
            if (_styles.Remove(key))
            {
                OnStyleChanged?.Invoke(this, new OnStyleChangedEventArgs { Key = key, Value = null });
            }
        }

        private void SetValue(string key, string value)
        {
            _styles.TryGetValue(key, out string existingValue);

            if (value != existingValue)
            {
                _styles[key] = value;
                OnStyleChanged?.Invoke(this, new OnStyleChangedEventArgs { Key = key, Value = value });
            }
        }
    }
}