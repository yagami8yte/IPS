using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IPS.MainApp.Converters
{
    /// <summary>
    /// Converter that compares two string values and returns a Brush based on equality
    /// Used for highlighting selected items with background colors
    /// </summary>
    public class StringEqualityToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return Brushes.Transparent;

            string value1 = values[0] as string;
            string value2 = values[1] as string;

            if (string.IsNullOrEmpty(value1) || string.IsNullOrEmpty(value2))
                return Brushes.Transparent;

            bool isEqual = string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);

            if (isEqual)
            {
                // Return AccentGold brush when strings match
                // Color: #D4AF37
                return new SolidColorBrush(Color.FromRgb(0xD4, 0xAF, 0x37));
            }
            else
            {
                // Return transparent when strings don't match
                return Brushes.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
