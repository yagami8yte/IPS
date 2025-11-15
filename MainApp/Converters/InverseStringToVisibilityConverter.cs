using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPS.MainApp.Converters
{
    /// <summary>
    /// Inverse of StringToVisibilityConverter
    /// Empty/Null string -> Visible, Non-empty -> Collapsed
    /// </summary>
    public class InverseStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
