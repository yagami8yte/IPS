using CommunityToolkit.Mvvm.Input;
using IPS.Core.Models;
using IPS.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPS.MainApp.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {
        private readonly Action _onNavigateBack;
        private readonly Action<bool, string> _onPaymentComplete;  // success, orderLabel
        private readonly ConfigurationService _configService;
        private readonly ForteCheckoutService? _forteCheckoutService;

        private bool _isProcessing = false;
        private string _paymentStatusMessage = string.Empty;
        private string _checkoutHtml = string.Empty;

        public ObservableCollection<CartItem> CartItems { get; }

        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand PayCommand { get; }

        public decimal CartTotalPrice => CartItems.Sum(item => item.TotalPrice);
        public int CartItemCount => CartItems.Sum(item => item.Quantity);

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                ((RelayCommand)PayCommand).NotifyCanExecuteChanged();
            }
        }

        public string PaymentStatusMessage
        {
            get => _paymentStatusMessage;
            set
            {
                _paymentStatusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsPaymentEnabled { get; }

        public string CheckoutHtml
        {
            get => _checkoutHtml;
            set
            {
                _checkoutHtml = value;
                OnPropertyChanged();
            }
        }

        public PaymentViewModel(
            ObservableCollection<CartItem> cartItems,
            ConfigurationService configService,
            Action onNavigateBack,
            Action<bool, string> onPaymentComplete)
        {
            _onNavigateBack = onNavigateBack ?? throw new ArgumentNullException(nameof(onNavigateBack));
            _onPaymentComplete = onPaymentComplete ?? throw new ArgumentNullException(nameof(onPaymentComplete));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            // Use the same cart items collection (shared reference)
            CartItems = cartItems;

            // Check if payment is enabled
            var config = _configService.GetConfiguration();
            IsPaymentEnabled = config.PaymentEnabled;

            // Initialize Forte Checkout service if payment is enabled
            if (IsPaymentEnabled)
            {
                _forteCheckoutService = new ForteCheckoutService(_configService);
            }

            IncreaseQuantityCommand = new RelayCommand<CartItem>(OnIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(OnDecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
            GoBackCommand = new RelayCommand(OnGoBack);
            PayCommand = new RelayCommand(async () => await OnPayAsync(), CanPay);

            Console.WriteLine($"[PaymentViewModel] Initialized - Payment Enabled: {IsPaymentEnabled}");
            Console.WriteLine($"[PaymentViewModel] Cart Review mode - CheckoutHtml is empty, will show cart items first");

            // CheckoutHtml starts empty, so Cart Review screen shows first
            // User clicks "Go to Payment" button to trigger OnPayAsync() which loads Forte Checkout

            // Pre-load checkout HTML in background to reduce wait time when user clicks "Go to Payment"
            if (IsPaymentEnabled)
            {
                _ = PreloadCheckoutHtmlAsync();
            }
        }

        private string? _preloadedCheckoutHtml = null;
        private string? _preloadedOrderLabel = null;

        /// <summary>
        /// Pre-loads checkout HTML in background to improve perceived performance
        /// </summary>
        private async Task PreloadCheckoutHtmlAsync()
        {
            try
            {
                Console.WriteLine("[PaymentViewModel] Pre-loading Forte Checkout HTML in background...");
                _preloadedOrderLabel = GenerateOrderLabel();
                _preloadedCheckoutHtml = await _forteCheckoutService!.GetCheckoutHtmlAsync(CartTotalPrice, _preloadedOrderLabel);
                Console.WriteLine($"[PaymentViewModel] Checkout HTML pre-loaded successfully (Order: {_preloadedOrderLabel})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Failed to pre-load checkout HTML: {ex.Message}");
                // Not critical - will regenerate when user clicks "Go to Payment"
            }
        }

        private void OnIncreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null) return;

            cartItem.Quantity++;
            UpdateCartTotals();
        }

        private void OnDecreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null) return;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCartTotals();
            }
            else
            {
                OnRemoveFromCart(cartItem);
            }
        }

        private void OnRemoveFromCart(CartItem? cartItem)
        {
            if (cartItem == null) return;

            CartItems.Remove(cartItem);
            UpdateCartTotals();
            ((RelayCommand)PayCommand).NotifyCanExecuteChanged();
        }

        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(CartTotalPrice));
            OnPropertyChanged(nameof(CartItemCount));
        }

        private void OnGoBack()
        {
            _onNavigateBack();
        }

        private bool CanPay()
        {
            return CartItems.Count > 0 && !IsProcessing;
        }

        private async Task OnPayAsync()
        {
            try
            {
                IsProcessing = true;
                PaymentStatusMessage = "";

                // If payment is disabled, skip payment and continue
                if (!IsPaymentEnabled)
                {
                    string orderLabel = GenerateOrderLabel();
                    Console.WriteLine("[PaymentViewModel] Payment disabled - skipping payment processing");
                    PaymentStatusMessage = "Payment disabled - Order processed without payment";
                    await Task.Delay(1000); // Brief delay to show message
                    _onPaymentComplete(true, orderLabel);
                    return;
                }

                // If cart total is $0.00, skip payment (Forte does not allow $0 transactions)
                if (CartTotalPrice <= 0)
                {
                    string orderLabel = GenerateOrderLabel();
                    Console.WriteLine("[PaymentViewModel] Cart total is $0.00 - skipping payment, processing order");
                    PaymentStatusMessage = "Order total is $0.00 - No payment required";
                    await Task.Delay(1000); // Brief delay to show message
                    _onPaymentComplete(true, orderLabel);
                    return;
                }

                // Payment enabled - use pre-loaded HTML if available, otherwise generate new
                if (_preloadedCheckoutHtml != null && _preloadedOrderLabel != null)
                {
                    Console.WriteLine($"[PaymentViewModel] Using pre-loaded Forte Checkout HTML (Order: {_preloadedOrderLabel})");
                    CheckoutHtml = _preloadedCheckoutHtml;

                    // Clear pre-loaded data
                    _preloadedCheckoutHtml = null;
                    _preloadedOrderLabel = null;
                }
                else
                {
                    // Pre-load failed or not ready - generate new HTML
                    string orderLabel = GenerateOrderLabel();
                    Console.WriteLine($"[PaymentViewModel] Generating Forte Checkout HTML for ${CartTotalPrice}, Order: {orderLabel}");
                    CheckoutHtml = await _forteCheckoutService!.GetCheckoutHtmlAsync(CartTotalPrice, orderLabel);
                }

                Console.WriteLine("[PaymentViewModel] Forte Checkout loaded - waiting for payment...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Payment error: {ex.Message}");
                PaymentStatusMessage = $"Error: {ex.Message}";
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Called from PaymentView when Forte Checkout reports success
        /// </summary>
        public void HandleCheckoutSuccess(string transactionId, string authorizationCode, string orderLabel)
        {
            Console.WriteLine($"[PaymentViewModel] Payment successful - Transaction ID: {transactionId}, Auth Code: {authorizationCode}");
            PaymentStatusMessage = $"Payment approved! Transaction ID: {transactionId}";
            IsProcessing = false;
            _onPaymentComplete(true, orderLabel);
        }

        /// <summary>
        /// Called from PaymentView when Forte Checkout reports failure
        /// </summary>
        public async void HandleCheckoutFailure(string error, string orderLabel)
        {
            Console.WriteLine($"[PaymentViewModel] Payment failed: {error}");
            PaymentStatusMessage = $"Payment failed: {error}";
            IsProcessing = false;

            // Auto-return to cart review after 3 seconds
            Console.WriteLine("[PaymentViewModel] Auto-returning to cart review in 3 seconds...");
            await Task.Delay(3000);
            ResetToCartReview();
        }

        /// <summary>
        /// Called from PaymentView when user cancels Forte Checkout
        /// </summary>
        public async void HandleCheckoutCancel(string orderLabel)
        {
            Console.WriteLine("[PaymentViewModel] Payment cancelled by user");
            PaymentStatusMessage = "Payment cancelled. Returning to cart...";
            IsProcessing = false;

            // Auto-return to cart review after 2 seconds
            Console.WriteLine("[PaymentViewModel] Auto-returning to cart review in 2 seconds...");
            await Task.Delay(2000);
            ResetToCartReview();
        }

        /// <summary>
        /// Called from PaymentView when Forte Checkout has an error (including timeout/expired)
        /// </summary>
        public async void HandleCheckoutError(string error, string orderLabel)
        {
            Console.WriteLine($"[PaymentViewModel] Checkout error: {error}");
            PaymentStatusMessage = $"Error: {error}";
            IsProcessing = false;

            // Auto-return to cart review after 3 seconds
            Console.WriteLine("[PaymentViewModel] Auto-returning to cart review in 3 seconds...");
            await Task.Delay(3000);
            ResetToCartReview();
        }

        /// <summary>
        /// Resets view back to cart review mode
        /// </summary>
        private void ResetToCartReview()
        {
            Console.WriteLine("[PaymentViewModel] Resetting to cart review mode");
            CheckoutHtml = string.Empty;  // Clear checkout HTML to show cart review
            PaymentStatusMessage = string.Empty;
            IsProcessing = false;

            // Pre-load checkout HTML again for next attempt
            if (IsPaymentEnabled && CartItems.Count > 0)
            {
                _ = PreloadCheckoutHtmlAsync();
            }
        }

        private string GenerateOrderLabel()
        {
            // Generate a simple order label like A-001
            // In production, this should be sequential and stored
            Random random = new Random();
            int orderNumber = random.Next(1, 999);
            return $"A-{orderNumber:D3}";
        }
    }
}
