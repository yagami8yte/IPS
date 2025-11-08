using System;
using System.Windows;
using System.Windows.Controls;

namespace IPS.MainApp.Views.Controls
{
    /// <summary>
    /// Numeric keypad control for touchscreen PIN entry
    /// </summary>
    public partial class NumericKeypad : UserControl
    {
        /// <summary>
        /// Event raised when a number button is clicked
        /// </summary>
        public event EventHandler<string>? NumberPressed;

        /// <summary>
        /// Event raised when backspace is clicked
        /// </summary>
        public event EventHandler? BackspacePressed;

        /// <summary>
        /// Event raised when clear is clicked
        /// </summary>
        public event EventHandler? ClearPressed;

        public NumericKeypad()
        {
            InitializeComponent();
        }

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string number)
            {
                NumberPressed?.Invoke(this, number);
            }
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            BackspacePressed?.Invoke(this, EventArgs.Empty);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}
