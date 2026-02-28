using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Deaddit.Pages.Models
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, PropertyWrapper> _wrappers = [];

        public BaseViewModel()
        {
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                PropertyWrapper wrapper = new();
                _wrappers.Add(pi.Name, wrapper);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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

        private class PropertyWrapper
        {
            public object? Value { get; set; }
        }
    }
}