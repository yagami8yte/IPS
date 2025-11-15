using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPS.MainApp.Converters
{
    /// <summary>
    /// Converter that compares two string values and returns Visibility.Visible if they match, Collapsed otherwise
    /// Used for highlighting selected items in lists
    /// </summary>
    public class StringEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return Visibility.Collapsed;

            string value1 = values[0] as string;
            string value2 = values[1] as string;

            if (string.IsNullOrEmpty(value1) || string.IsNullOrEmpty(value2))
                return Visibility.Collapsed;

            bool isEqual = string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
            return isEqual ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
