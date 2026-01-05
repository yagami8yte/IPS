using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IPS.Services;

namespace IPS.MainApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly SystemManagerService _systemManager;
        private readonly SystemPollingService _pollingService;
        private readonly ConfigurationService _configService;
        private readonly PrintingService _printingService;
        private BaseViewModel? _currentViewModel;
        private MenuViewModel? _currentMenuViewModel;

        // Payment details for receipt printing
        public string? LastPaymentTransactionId { get; set; }
        public string? LastPaymentAuthorizationCode { get; set; }
        public string? LastPaymentCardLast4Digits { get; set; }

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateToWelcomeCommand { get; }
        public ICommand NavigateToMenuCommand { get; }
        public ICommand NavigateToAdminCommand { get; }

        public MainViewModel(
            SystemManagerService systemManager,
            SystemPollingService pollingService,
            ConfigurationService configService,
            Func<IProgress<(string message, int progress)>, Task<bool>> initializeSystemsAsync)
        {
            _systemManager = systemManager;
            _pollingService = pollingService;
            _configService = configService;
            _printingService = new PrintingService(configService);

            NavigateToWelcomeCommand = new RelayCommand(ExecuteNavigateToWelcome);
            NavigateToMenuCommand = new RelayCommand(ExecuteNavigateToMenu);
            NavigateToAdminCommand = new RelayCommand(ExecuteNavigateToAdmin);

            // Start with loading screen and initialize systems in background
            StartInitialization(initializeSystemsAsync);
        }

        private async void StartInitialization(Func<IProgress<(string message, int progress)>, Task<bool>> initializeSystemsAsync)
        {
            // Show loading screen
            var loadingViewModel = new LoadingViewModel();
            CurrentViewModel = loadingViewModel;

            // Create progress reporter
            var progress = new Progress<(string message, int progress)>(update =>
            {
                // Update UI on UI thread (Progress<T> already marshals to UI thread, so direct update is fine)
                loadingViewModel.UpdateStatus(update.message, update.progress);
            });

            try
            {
                // Run initialization on background thread so UI thread can process progress updates
                bool success = await Task.Run(async () => await initializeSystemsAsync(progress));

                if (success)
                {
                    // Navigate to welcome screen (we're back on UI thread after await)
                    ExecuteNavigateToWelcome();
                }
                else
                {
                    // Show error
                    loadingViewModel.UpdateStatus("Initialization failed. Please check configuration.", 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainViewModel] Initialization error: {ex.Message}");
                loadingViewModel.UpdateStatus("Initialization failed. Please check logs.", 0);
            }
        }

        private void ExecuteNavigateToWelcome()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateToWelcome - Checking break time status");

            // Check if we're in break time (instant or scheduled)
            var config = _configService.GetConfiguration();
            bool isInstantBreak;
            if (IsInBreakTime(config, out isInstantBreak))
            {
                Console.WriteLine($"[MainViewModel] Break time active (Instant: {isInstantBreak}) - showing BreakTimeView");
                CurrentViewModel = new BreakTimeViewModel(
                    config.BreakEndHour,
                    config.BreakEndMinute,
                    config.BreakMessage,
                    isInstantBreak,
                    ExecuteNavigateToAdmin);
                Console.WriteLine("[MainViewModel] BreakTimeViewModel set as CurrentViewModel");
            }
            else
            {
                Console.WriteLine("[MainViewModel] Not in break time - creating WelcomeViewModel");
                CurrentViewModel = new WelcomeViewModel(ExecuteNavigateToMenu, ExecuteNavigateToAdmin);
                Console.WriteLine("[MainViewModel] WelcomeViewModel set as CurrentViewModel");
            }
        }

        private bool IsInBreakTime(IPS.Core.Models.AppConfiguration config, out bool isInstantBreak)
        {
            isInstantBreak = false;

            // Check instant break first (takes priority)
            if (config.IsInstantBreakActive)
            {
                isInstantBreak = true;
                return true;
            }

            // Check scheduled break time
            if (!config.IsBreakTimeEnabled)
            {
                return false;
            }

            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var breakStart = new TimeSpan(config.BreakStartHour, config.BreakStartMinute, 0);
            var breakEnd = new TimeSpan(config.BreakEndHour, config.BreakEndMinute, 0);

            // Handle break time that crosses midnight
            if (breakStart > breakEnd)
            {
                // Example: 23:00 to 01:00 (11 PM to 1 AM)
                return currentTime >= breakStart || currentTime < breakEnd;
            }
            else
            {
                // Normal case: 14:00 to 15:00 (2 PM to 3 PM)
                return currentTime >= breakStart && currentTime < breakEnd;
            }
        }

        private void ExecuteNavigateToMenu()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateToMenu - START");
            Console.WriteLine("[MainViewModel] ExecuteNavigateToMenu - Creating MenuViewModel...");

            try
            {
                var menuViewModel = new MenuViewModel(_systemManager, _pollingService, ExecuteNavigateToWelcome, ExecuteNavigateToPayment, ExecuteSubmitOrderDirectly);
                Console.WriteLine("[MainViewModel] ExecuteNavigateToMenu - MenuViewModel created successfully");

                _currentMenuViewModel = menuViewModel;
                CurrentViewModel = menuViewModel;
                Console.WriteLine("[MainViewModel] ExecuteNavigateToMenu - MenuViewModel set as CurrentViewModel");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainViewModel] ExecuteNavigateToMenu - ERROR: {ex.Message}");
                Console.WriteLine($"[MainViewModel] ExecuteNavigateToMenu - Stack trace: {ex.StackTrace}");
                throw;
            }

            Console.WriteLine("[MainViewModel] ExecuteNavigateToMenu - END");
        }

        private void ExecuteNavigateToAdmin()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateToAdmin - Showing password entry");

            var passwordService = new PasswordService();
            CurrentViewModel = new PasswordEntryViewModel(
                _configService,
                passwordService,
                ExecuteNavigateToAdminAfterAuth,
                ExecuteNavigateToWelcome);

            Console.WriteLine("[MainViewModel] ExecuteNavigateToAdmin - PasswordEntryViewModel set as CurrentViewModel");
        }

        private void ExecuteNavigateToAdminAfterAuth()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateToAdminAfterAuth - Password verified, creating AdminViewModel");
            CurrentViewModel = new AdminViewModel(_configService, ExecuteNavigateToWelcome);
            Console.WriteLine("[MainViewModel] ExecuteNavigateToAdminAfterAuth - AdminViewModel set as CurrentViewModel");
        }

        private void ExecuteNavigateToPayment()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateToPayment - Creating PaymentViewModel");

            if (_currentMenuViewModel == null)
            {
                Console.WriteLine("[MainViewModel] ERROR: No current menu view model");
                return;
            }

            // Check if payment is disabled - skip payment screen entirely
            var config = _configService.GetConfiguration();
            if (!config.PaymentEnabled)
            {
                Console.WriteLine("[MainViewModel] Payment is DISABLED - skipping payment screen, submitting order directly");
                string orderLabel = $"A-{DateTime.Now:mmss}";

                // Set payment details as N/A for non-payment orders
                LastPaymentTransactionId = "N/A (Payment Disabled)";
                LastPaymentAuthorizationCode = "N/A";
                LastPaymentCardLast4Digits = "";

                ExecuteProcessPaymentAndOrder(true, orderLabel);
                return;
            }

            var paymentViewModel = new PaymentViewModel(
                _currentMenuViewModel.CartItems,
                _configService,
                ExecuteNavigateBackToMenu,
                ExecuteProcessPaymentAndOrder,
                this);

            CurrentViewModel = paymentViewModel;
            Console.WriteLine("[MainViewModel] ExecuteNavigateToPayment - PaymentViewModel set as CurrentViewModel");
        }

        /// <summary>
        /// Directly submits order without payment (for $0 orders)
        /// Skips PaymentView entirely and goes straight to order processing
        /// </summary>
        private void ExecuteSubmitOrderDirectly(string orderLabel)
        {
            Console.WriteLine($"[MainViewModel] ExecuteSubmitOrderDirectly - Submitting $0 order directly: {orderLabel}");

            // Set payment details as N/A for $0 orders
            LastPaymentTransactionId = "N/A";
            LastPaymentAuthorizationCode = "N/A";
            LastPaymentCardLast4Digits = "";

            // Call the same order processing as after successful payment
            ExecuteProcessPaymentAndOrder(true, orderLabel);
        }

        private void ExecuteNavigateBackToMenu()
        {
            Console.WriteLine("[MainViewModel] ExecuteNavigateBackToMenu - Returning to MenuViewModel");

            if (_currentMenuViewModel != null)
            {
                CurrentViewModel = _currentMenuViewModel;
                Console.WriteLine("[MainViewModel] ExecuteNavigateBackToMenu - MenuViewModel restored");
            }
            else
            {
                Console.WriteLine("[MainViewModel] ExecuteNavigateBackToMenu - No menu view model, creating new one");
                ExecuteNavigateToMenu();
            }
        }

        private async void ExecuteProcessPaymentAndOrder(bool paymentSuccess, string orderLabel)
        {
            Console.WriteLine($"[MainViewModel] ExecuteProcessPaymentAndOrder - Payment: {paymentSuccess}, Label: {orderLabel}");

            if (!paymentSuccess)
            {
                ExecuteNavigateToOrderResult(false, orderLabel, "Payment failed. Please try again.");
                return;
            }

            // Payment succeeded, now send order to DLL
            if (_currentMenuViewModel == null || _currentMenuViewModel.CartItems.Count == 0)
            {
                ExecuteNavigateToOrderResult(false, orderLabel, "No items in cart.");
                return;
            }

            try
            {
                // Create order from cart
                var order = CreateOrderFromCart(orderLabel);

                // Send order to all systems (multi-system order handling)
                Console.WriteLine($"[MainViewModel] Sending multi-system order (Label: {orderLabel})...");
                Console.WriteLine($"[MainViewModel] Order contains {order.Items.Count} items across systems:");

                var systemNames = order.Items.Select(i => i.SystemName).Distinct().ToList();
                foreach (var sysName in systemNames)
                {
                    var itemCount = order.Items.Count(i => i.SystemName == sysName);
                    Console.WriteLine($"[MainViewModel]   - {sysName}: {itemCount} items");
                }

                var results = await Task.Run(() => _systemManager.SendMultiSystemOrder(order));

                Console.WriteLine($"[MainViewModel] Multi-system order results:");
                bool allSuccess = true;
                foreach (var (systemName, success) in results)
                {
                    Console.WriteLine($"[MainViewModel]   {systemName}: {(success ? "✓ Success" : "✗ Failed")}");
                    if (!success) allSuccess = false;
                }

                if (allSuccess)
                {
                    Console.WriteLine($"[MainViewModel] ✓ All orders sent successfully!");

                    // Print receipt (non-blocking - don't fail order if printing fails)
                    try
                    {
                        Console.WriteLine("[MainViewModel] Attempting to print receipt...");
                        bool printed = _printingService.PrintReceipt(
                            order,
                            _currentMenuViewModel.CartItems,
                            LastPaymentCardLast4Digits,
                            LastPaymentAuthorizationCode,
                            LastPaymentTransactionId);

                        if (printed)
                        {
                            Console.WriteLine("[MainViewModel] ✓ Receipt printed successfully");
                        }
                        else
                        {
                            Console.WriteLine("[MainViewModel] Receipt printing skipped or failed (non-blocking)");
                        }
                    }
                    catch (Exception printEx)
                    {
                        Console.WriteLine($"[MainViewModel] Receipt printing error (non-blocking): {printEx.Message}");
                    }

                    _currentMenuViewModel.ClearCart();
                    ExecuteNavigateToOrderResult(true, orderLabel, "Your order has been placed successfully! Please collect it at the pickup counter.");
                }
                else
                {
                    Console.WriteLine($"[MainViewModel] ✗ Some orders failed to send");
                    ExecuteNavigateToOrderResult(false, orderLabel, "Failed to send order to one or more systems. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainViewModel] ✗✗✗ Error processing order ✗✗✗");
                Console.WriteLine($"[MainViewModel] Exception: {ex.Message}");
                Console.WriteLine($"[MainViewModel] Stack trace: {ex.StackTrace}");
                ExecuteNavigateToOrderResult(false, orderLabel, "An error occurred while processing your order.");
            }
        }

        private IPS.Core.Models.OrderInfo CreateOrderFromCart(string orderLabel)
        {
            if (_currentMenuViewModel == null)
                throw new InvalidOperationException("No current menu view model");

            var orderItems = new System.Collections.Generic.List<IPS.Core.Models.OrderItem>();

            foreach (var cartItem in _currentMenuViewModel.CartItems)
            {
                var orderItem = new IPS.Core.Models.OrderItem
                {
                    MenuId = cartItem.MenuItem.MenuId,
                    Quantity = cartItem.Quantity,
                    SelectedOptionIds = cartItem.SelectedOptions.Select(o => o.OptionId).ToList(),
                    SystemName = cartItem.SystemName
                };
                orderItems.Add(orderItem);
            }

            var order = new IPS.Core.Models.OrderInfo
            {
                OrderId = Guid.NewGuid().ToString(),
                OrderLabel = orderLabel,
                Items = orderItems,
                TotalAmount = _currentMenuViewModel.CartTotalPrice,
                QrData = null  // Will be generated by DLL if needed
            };

            return order;
        }

        private void ExecuteNavigateToOrderResult(bool success, string orderLabel, string message)
        {
            Console.WriteLine($"[MainViewModel] ExecuteNavigateToOrderResult - Success: {success}, Label: {orderLabel}");

            var orderResultViewModel = new OrderResultViewModel(success, orderLabel, message, ExecuteNavigateToWelcome);
            CurrentViewModel = orderResultViewModel;

            Console.WriteLine("[MainViewModel] ExecuteNavigateToOrderResult - OrderResultViewModel set as CurrentViewModel");
        }
    }
}
