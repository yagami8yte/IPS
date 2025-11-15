using System;
using System.Globalization;
using System.Windows.Data;

namespace IPS.MainApp.Converters
{
    /// <summary>
    /// Converts IsPaymentEnabled boolean to button text
    /// true -> "Go to Payment", false -> "Process Order"
    /// </summary>
    public class BoolToPaymentTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPaymentEnabled)
            {
                return isPaymentEnabled ? "Go to Payment" : "Process Order";
            }
            return "Process Order";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
