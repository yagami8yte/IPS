using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using IPS.MainApp.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace IPS.MainApp.Views
{
    public partial class PaymentView : UserControl
    {
        private bool _isWebViewInitialized = false;

        public PaymentView()
        {
            InitializeComponent();
            DataContextChanged += PaymentView_DataContextChanged;
        }

        private void PaymentView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PaymentViewModel viewModel)
            {
                // Subscribe to CheckoutHtml property changes
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentViewModel.CheckoutHtml))
            {
                if (DataContext is PaymentViewModel viewModel && !string.IsNullOrEmpty(viewModel.CheckoutHtml))
                {
                    LoadCheckoutHtml(viewModel.CheckoutHtml);
                }
            }
        }

        private async void ForteCheckoutWebView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[PaymentView] Initializing WebView2...");

                // Initialize WebView2
                await ForteCheckoutWebView.EnsureCoreWebView2Async(null);

                Console.WriteLine("[PaymentView] WebView2 initialized successfully");

                // Enable DevTools for debugging (개발 중에만)
                // WebView2에서 마우스 오른쪽 클릭 → Inspect를 사용할 수 있습니다
                ForteCheckoutWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                ForteCheckoutWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Set up virtual host mapping to allow localStorage access
                // This maps https://forte.local/ to navigate to string content
                ForteCheckoutWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "forte.local",
                    Environment.CurrentDirectory,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
                );

                // Subscribe to WebMessageReceived event
                ForteCheckoutWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Subscribe to navigation events for debugging
                ForteCheckoutWebView.CoreWebView2.NavigationCompleted += (sender, e) =>
                {
                    Console.WriteLine($"[WebView2] Navigation completed - Success: {e.IsSuccess}");
                    if (!e.IsSuccess)
                    {
                        Console.WriteLine($"[WebView2] Navigation error: {e.WebErrorStatus}");
                    }
                };

                _isWebViewInitialized = true;

                // If CheckoutHtml is already set, load it
                if (DataContext is PaymentViewModel viewModel && !string.IsNullOrEmpty(viewModel.CheckoutHtml))
                {
                    LoadCheckoutHtml(viewModel.CheckoutHtml);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentView] WebView2 initialization error: {ex.Message}");
                MessageBox.Show($"Failed to initialize payment system: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCheckoutHtml(string html)
        {
            if (!_isWebViewInitialized)
            {
                Console.WriteLine("[PaymentView] WebView2 not initialized yet, waiting...");
                return;
            }

            try
            {
                Console.WriteLine("[PaymentView] Loading Forte Checkout HTML into WebView2...");
                Console.WriteLine($"[PaymentView] HTML Length: {html.Length} characters");

                // Print first 1000 characters to see what's actually in the HTML
                var preview = html.Length > 1000 ? html.Substring(0, 1000) : html;
                Console.WriteLine($"[PaymentView] HTML Preview:\n{preview}");

                ForteCheckoutWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentView] Error loading checkout HTML: {ex.Message}");
                if (DataContext is PaymentViewModel viewModel)
                {
                    viewModel.HandleCheckoutError($"Failed to load checkout: {ex.Message}", "");
                }
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string? json = null;

                // Try to get message as string
                try
                {
                    json = e.TryGetWebMessageAsString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PaymentView] Failed to get message as string: {ex.Message}");
                    // Try alternative method - get as JSON
                    try
                    {
                        json = e.WebMessageAsJson;
                        Console.WriteLine($"[PaymentView] Successfully got message as JSON");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[PaymentView] Failed to get message as JSON: {ex2.Message}");
                        return;
                    }
                }

                Console.WriteLine($"[PaymentView] Received message from Forte Checkout: {json}");

                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("[PaymentView] Empty message received, ignoring");
                    return;
                }

                // Parse JSON message
                var message = JsonSerializer.Deserialize<ForteCheckoutMessage>(json);

                if (message == null)
                {
                    Console.WriteLine("[PaymentView] Failed to parse message");
                    return;
                }

                if (DataContext is not PaymentViewModel viewModel)
                {
                    Console.WriteLine("[PaymentView] ViewModel not found");
                    return;
                }

                // Handle different message types
                switch (message.type?.ToLower())
                {
                    case "success":
                        Console.WriteLine($"[PaymentView] Payment SUCCESS - Transaction ID: {message.transactionId}, Order: {message.orderLabel}, Card: ****{message.cardLast4}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            viewModel.HandleCheckoutSuccess(
                                message.transactionId ?? "",
                                message.authorizationCode ?? "",
                                message.orderLabel ?? "",
                                message.cardLast4 ?? ""
                            );
                        });
                        break;

                    case "failure":
                        Console.WriteLine($"[PaymentView] Payment FAILURE - Error: {message.error}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            viewModel.HandleCheckoutFailure(
                                message.error ?? "Unknown error",
                                message.orderLabel ?? ""
                            );
                        });
                        break;

                    case "cancel":
                        Console.WriteLine("[PaymentView] Payment CANCELLED");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            viewModel.HandleCheckoutCancel(message.orderLabel ?? "");
                        });
                        break;

                    case "error":
                        Console.WriteLine($"[PaymentView] Checkout ERROR - {message.error}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            viewModel.HandleCheckoutError(
                                message.error ?? "Unknown error",
                                message.orderLabel ?? ""
                            );
                        });
                        break;

                    case "ready":
                        Console.WriteLine("[PaymentView] Forte Checkout READY");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 로딩 완료 - IsProcessing을 false로 설정
                            viewModel.IsProcessing = false;
                        });
                        break;

                    default:
                        Console.WriteLine($"[PaymentView] Unknown message type: {message.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentView] Error handling web message: {ex.Message}");
                Console.WriteLine($"[PaymentView] Stack trace: {ex.StackTrace}");
            }
        }

        // JSON message structure from Forte Checkout
        private class ForteCheckoutMessage
        {
            public string? type { get; set; }
            public string? transactionId { get; set; }
            public string? authorizationCode { get; set; }
            public string? orderLabel { get; set; }
            public string? error { get; set; }
            public decimal? amount { get; set; }
            public string? cardLast4 { get; set; }
            public string? cardType { get; set; }
        }

        private void CopyLogsToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentViewModel viewModel)
            {
                var logsText = string.Join(Environment.NewLine, viewModel.RestApiLogs);
                if (!string.IsNullOrEmpty(logsText))
                {
                    Clipboard.SetText(logsText);
                    MessageBox.Show("Logs copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
