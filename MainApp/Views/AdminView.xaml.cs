using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IPS.MainApp.ViewModels;

namespace IPS.MainApp.Views
{
    /// <summary>
    /// Interaction logic for AdminView.xaml
    /// </summary>
    public partial class AdminView : UserControl
    {
        public AdminView()
        {
            InitializeComponent();

            // Wire up keypad events
            PinKeypad.NumberPressed += OnPinKeypadNumberPressed;
            PinKeypad.BackspacePressed += OnPinKeypadBackspacePressed;
            PinKeypad.ClearPressed += OnPinKeypadClearPressed;
        }

        private void CurrentPinField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CurrentPinRadio.IsChecked = true;
        }

        private void NewPinField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NewPinRadio.IsChecked = true;
        }

        private void ConfirmPinField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ConfirmPinRadio.IsChecked = true;
        }

        private void OnPinKeypadNumberPressed(object? sender, string number)
        {
            if (DataContext is not AdminViewModel viewModel)
                return;

            // Determine which field is selected and update it
            if (CurrentPinRadio.IsChecked == true)
            {
                if (viewModel.CurrentPassword.Length < 8)
                    viewModel.CurrentPassword += number;
            }
            else if (NewPinRadio.IsChecked == true)
            {
                if (viewModel.NewPassword.Length < 8)
                    viewModel.NewPassword += number;
            }
            else if (ConfirmPinRadio.IsChecked == true)
            {
                if (viewModel.ConfirmPassword.Length < 8)
                    viewModel.ConfirmPassword += number;
            }
        }

        private void OnPinKeypadBackspacePressed(object? sender, System.EventArgs e)
        {
            if (DataContext is not AdminViewModel viewModel)
                return;

            // Determine which field is selected and remove last digit
            if (CurrentPinRadio.IsChecked == true && viewModel.CurrentPassword.Length > 0)
            {
                viewModel.CurrentPassword = viewModel.CurrentPassword.Substring(0, viewModel.CurrentPassword.Length - 1);
            }
            else if (NewPinRadio.IsChecked == true && viewModel.NewPassword.Length > 0)
            {
                viewModel.NewPassword = viewModel.NewPassword.Substring(0, viewModel.NewPassword.Length - 1);
            }
            else if (ConfirmPinRadio.IsChecked == true && viewModel.ConfirmPassword.Length > 0)
            {
                viewModel.ConfirmPassword = viewModel.ConfirmPassword.Substring(0, viewModel.ConfirmPassword.Length - 1);
            }
        }

        private void OnPinKeypadClearPressed(object? sender, System.EventArgs e)
        {
            if (DataContext is not AdminViewModel viewModel)
                return;

            // Clear the selected field
            if (CurrentPinRadio.IsChecked == true)
            {
                viewModel.CurrentPassword = string.Empty;
            }
            else if (NewPinRadio.IsChecked == true)
            {
                viewModel.NewPassword = string.Empty;
            }
            else if (ConfirmPinRadio.IsChecked == true)
            {
                viewModel.ConfirmPassword = string.Empty;
            }
        }
    }
}
