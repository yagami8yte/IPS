using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace IPS.MainApp.Views.Controls
{
    /// <summary>
    /// Display control that shows dots representing entered PIN digits
    /// </summary>
    public partial class PinDisplay : UserControl
    {
        private readonly ObservableCollection<object> _dots = new();

        /// <summary>
        /// Dependency property for PIN length
        /// </summary>
        public static readonly DependencyProperty PinLengthProperty =
            DependencyProperty.Register(
                nameof(PinLength),
                typeof(int),
                typeof(PinDisplay),
                new PropertyMetadata(0, OnPinLengthChanged));

        /// <summary>
        /// Maximum number of digits to display
        /// </summary>
        public static readonly DependencyProperty MaxDigitsProperty =
            DependencyProperty.Register(
                nameof(MaxDigits),
                typeof(int),
                typeof(PinDisplay),
                new PropertyMetadata(8));

        /// <summary>
        /// Current PIN length (number of digits entered)
        /// </summary>
        public int PinLength
        {
            get => (int)GetValue(PinLengthProperty);
            set => SetValue(PinLengthProperty, value);
        }

        /// <summary>
        /// Maximum number of digits allowed
        /// </summary>
        public int MaxDigits
        {
            get => (int)GetValue(MaxDigitsProperty);
            set => SetValue(MaxDigitsProperty, value);
        }

        public PinDisplay()
        {
            InitializeComponent();
            PinDotsContainer.ItemsSource = _dots;
        }

        private static void OnPinLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PinDisplay display)
            {
                display.UpdateDots();
            }
        }

        private void UpdateDots()
        {
            _dots.Clear();
            for (int i = 0; i < PinLength; i++)
            {
                _dots.Add(new object()); // Each object represents one dot
            }
        }
    }
}
