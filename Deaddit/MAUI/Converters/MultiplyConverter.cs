using System.Globalization;

namespace Deaddit.MAUI.Converters
{

    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string factorString && double.TryParse(factorString, out double factor))
            {
                return doubleValue * factor;
            }

            return value;  // Return the original value if there's any issue
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string factorString && double.TryParse(factorString, out double factor))
            {
                return doubleValue / factor;
            }

            return value;
        }
    }
}
