using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Collections;

namespace Maui.WebComponents.Classes
{
    public class WebComponentCollection : IEnumerable<WebComponent>
    {
        private readonly List<WebComponent> _components = [];

        internal event EventHandler<OnWebComponentAddedEventArgs>? OnWebComponentAdded;

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

        public IEnumerator<WebComponent> GetEnumerator()
        {
            return ((IEnumerable<WebComponent>)_components).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_components).GetEnumerator();
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