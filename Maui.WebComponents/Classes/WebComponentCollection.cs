using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Collections;

namespace Maui.WebComponents.Classes
{
    public class WebComponentCollection : IEnumerable<WebComponent>
    {
        private readonly List<WebComponent> _components = [];

        public int Count => _components.Count;

        internal event EventHandler<OnWebComponentAddedEventArgs>? OnWebComponentAdded;

        internal event EventHandler<OnWebComponentInsertEventArgs>? OnWebComponentInsert;

        internal event EventHandler<OnWebComponentRemovedEventArgs>? OnWebComponentRemoved;

        public void Add(WebComponent component)
        {
            if (_components.Contains(component))
            {
                throw new ArgumentException("Component already exists in collection", nameof(component));
            }

            _components.Add(component);

            OnWebComponentAdded?.Invoke(this, new OnWebComponentAddedEventArgs(component));
        }

        public void Clear()
        {
            List<WebComponent> list = [.. _components];
            _components.Clear();

            foreach (WebComponent component in list)
            {
                OnWebComponentRemoved?.Invoke(this, new OnWebComponentRemovedEventArgs(component));
            }
        }

        public IEnumerator<WebComponent> GetEnumerator()
        {
            return ((IEnumerable<WebComponent>)_components).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_components).GetEnumerator();
        }

        public void Insert(int index, WebComponent component)
        {
            _components.Insert(index, component);

            OnWebComponentInsert?.Invoke(this, new OnWebComponentInsertEventArgs(component, index));
        }

        public void InsertAfter(WebComponent component, WebComponent after)
        {
            if (!_components.Contains(after))
            {
                throw new ArgumentException("Component does not exist in collection", nameof(after));
            }

            if (_components.Contains(component))
            {
                throw new ArgumentException("Component already exists in collection", nameof(component));
            }

            int index = _components.IndexOf(after);

            _components.Insert(index + 1, component);

            OnWebComponentInsert?.Invoke(this, new OnWebComponentInsertEventArgs(component, index + 1));
        }

        public bool Remove(WebComponent component)
        {
            if (_components.Remove(component))
            {
                OnWebComponentRemoved?.Invoke(this, new OnWebComponentRemovedEventArgs(component));
                return true;
            }

            return false;
        }
    }
}