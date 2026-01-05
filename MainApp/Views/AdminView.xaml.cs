using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IPS.MainApp.ViewModels;
using Microsoft.Web.WebView2.Core;

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

            // Initialize WebView2 for payment testing
            InitializePaymentTestWebView();

            // Watch for PaymentTestHtml changes
            DataContextChanged += AdminView_DataContextChanged;
        }

        private void AdminView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AdminViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is AdminViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdminViewModel.PaymentTestHtml))
            {
                LoadPaymentTestHtml();
            }
        }

        private void LoadPaymentTestHtml()
        {
            if (DataContext is AdminViewModel viewModel && PaymentTestWebView.CoreWebView2 != null)
            {
                if (!string.IsNullOrWhiteSpace(viewModel.PaymentTestHtml))
                {
                    Console.WriteLine($"[AdminView] Loading payment test HTML ({viewModel.PaymentTestHtml.Length} chars)");
                    PaymentTestWebView.NavigateToString(viewModel.PaymentTestHtml);
                }
            }
        }

        private async void InitializePaymentTestWebView()
        {
            try
            {
                await PaymentTestWebView.EnsureCoreWebView2Async(null);

                // Enable DevTools for debugging (same as PaymentView)
                PaymentTestWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                PaymentTestWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Set up virtual host mapping to allow localStorage access (same as PaymentView)
                PaymentTestWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "forte.local",
                    Environment.CurrentDirectory,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                // Handle messages from JavaScript
                PaymentTestWebView.CoreWebView2.WebMessageReceived += PaymentTestWebView_WebMessageReceived;

                Console.WriteLine("[AdminView] Payment test WebView2 initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminView] Failed to initialize payment test WebView2: {ex.Message}");
            }
        }

        private void PaymentTestWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Get message as JSON (same as PaymentView)
                string? json = null;
                try
                {
                    json = e.TryGetWebMessageAsString();
                }
                catch
                {
                    json = e.WebMessageAsJson;
                }

                Console.WriteLine($"[AdminView] WebView2 message received: {json}");

                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("[AdminView] Empty message, ignoring");
                    return;
                }

                if (DataContext is AdminViewModel viewModel)
                {
                    viewModel.HandlePaymentTestWebViewMessage(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminView] Error handling WebView2 message: {ex.Message}");
            }
        }

        // JSON message structure from Forte Checkout (same as PaymentView)
        private class ForteCheckoutMessage
        {
            public string? type { get; set; }
            public string? transactionId { get; set; }
            public string? authorizationCode { get; set; }
            public string? orderLabel { get; set; }
            public string? error { get; set; }
            public decimal? amount { get; set; }
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

        private void ForteSecureKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel viewModel && sender is PasswordBox passwordBox)
            {
                // Only update if user typed something (not empty)
                if (!string.IsNullOrEmpty(passwordBox.Password))
                {
                    viewModel.ForteApiSecureKey = passwordBox.Password;
                }
            }
        }

        private void OnPaymentModeCheckoutClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is AdminViewModel viewModel)
            {
                viewModel.SetPaymentMode(IPS.Core.Models.FortePaymentMode.Checkout);
            }
        }

        private void OnPaymentModeRestApiClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is AdminViewModel viewModel)
            {
                viewModel.SetPaymentMode(IPS.Core.Models.FortePaymentMode.RestApi);
            }
        }
    }
}
