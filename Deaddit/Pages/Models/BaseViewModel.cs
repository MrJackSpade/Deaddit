using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Deaddit.Pages.Models
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private class PropertyWrapper
        {
            public object? Value { get; set; }
        }

        private readonly Dictionary<string, PropertyWrapper> _wrappers = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        public BaseViewModel()
        {
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                PropertyWrapper wrapper = new();
                _wrappers.Add(pi.Name, wrapper);
            }
        }

        public T GetValue<T>([CallerMemberName] string callerName = "")
        {
            PropertyWrapper notifying = _wrappers[callerName];
            return (T)notifying.Value;
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetValue(object? value, [CallerMemberName] string callerName = "")
        {
            PropertyWrapper notifying = _wrappers[callerName];
            notifying.Value = value;
            this.OnPropertyChanged(callerName);
        }
    }
}