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
    public class PaymentViewModel : BaseViewModel, IDisposable
    {
        private readonly Action _onNavigateBack;
        private readonly Action<bool, string> _onPaymentComplete;  // success, orderLabel
        private readonly ConfigurationService _configService;
        private readonly ForteCheckoutService? _forteCheckoutService;
        private readonly FortePaymentService? _fortePaymentService;
        private readonly DynaflexService? _dynaflexService;
        private readonly MainViewModel? _mainViewModel;  // For storing payment details
        private bool _isDisposed = false;

        private bool _isProcessing = false;
        private string _paymentStatusMessage = string.Empty;
        private string _checkoutHtml = string.Empty;

        // Payment mode
        private readonly FortePaymentMode _paymentMode;
        private readonly bool _isSandboxMode;
        public bool IsCheckoutMode => _paymentMode == FortePaymentMode.Checkout;
        public bool IsRestApiMode => _paymentMode == FortePaymentMode.RestApi;
        public bool IsSandboxMode => _isSandboxMode;

        // Dynaflex card reader state
        private bool _isDynaflexConnected = false;
        private string _dynaflexStatus = "Searching for card reader...";

        public bool IsDynaflexConnected
        {
            get => _isDynaflexConnected;
            set { _isDynaflexConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(CardReaderStatusText)); }
        }

        public string DynaflexStatus
        {
            get => _dynaflexStatus;
            set { _dynaflexStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(CardReaderStatusText)); }
        }

        public string CardReaderStatusText
        {
            get
            {
                if (IsDynaflexConnected)
                {
                    return "✓ Card reader connected";
                }
                else if (_isSandboxMode)
                {
                    return "⚠ Test mode - use buttons below or insert real card";
                }
                else
                {
                    return DynaflexStatus;
                }
            }
        }

        // REST API states
        private bool _isConnectingToCardReader = false;
        private bool _isPreparingTerminal = false;
        private bool _isWaitingForCard = false;
        private bool _cardReaderConnectionFailed = false;

        /// <summary>
        /// True while attempting to connect to the card reader (production mode)
        /// </summary>
        public bool IsConnectingToCardReader
        {
            get => _isConnectingToCardReader;
            set { _isConnectingToCardReader = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True while preparing the terminal (after connection, before ready for card)
        /// </summary>
        public bool IsPreparingTerminal
        {
            get => _isPreparingTerminal;
            set { _isPreparingTerminal = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when card reader is connected and ready for card insertion
        /// </summary>
        public bool IsWaitingForCard
        {
            get => _isWaitingForCard;
            set { _isWaitingForCard = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// True when card reader connection failed after multiple attempts
        /// </summary>
        public bool CardReaderConnectionFailed
        {
            get => _cardReaderConnectionFailed;
            set { _cardReaderConnectionFailed = value; OnPropertyChanged(); }
        }

        // REST API Card Input Fields
        private string _cardNumber = string.Empty;
        private string _expiryMonth = string.Empty;
        private string _expiryYear = string.Empty;
        private string _cvv = string.Empty;
        private string _cardholderName = string.Empty;

        public string CardNumber
        {
            get => _cardNumber;
            set { _cardNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcessPayment)); }
        }

        public string ExpiryMonth
        {
            get => _expiryMonth;
            set { _expiryMonth = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcessPayment)); }
        }

        public string ExpiryYear
        {
            get => _expiryYear;
            set { _expiryYear = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcessPayment)); }
        }

        public string Cvv
        {
            get => _cvv;
            set { _cvv = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcessPayment)); }
        }

        public string CardholderName
        {
            get => _cardholderName;
            set { _cardholderName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcessPayment)); }
        }

        public bool CanProcessPayment =>
            !IsProcessing &&
            !string.IsNullOrWhiteSpace(CardNumber) && CardNumber.Length >= 13 &&
            !string.IsNullOrWhiteSpace(ExpiryMonth) &&
            !string.IsNullOrWhiteSpace(ExpiryYear) &&
            !string.IsNullOrWhiteSpace(Cvv) && Cvv.Length >= 3 &&
            !string.IsNullOrWhiteSpace(CardholderName);

        // REST API Logs
        public ObservableCollection<string> RestApiLogs { get; } = new();
        public bool HasRestApiLogs => RestApiLogs.Count > 0;

        public ObservableCollection<CartItem> CartItems { get; }

        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand PayCommand { get; }
        public ICommand ProcessRestApiPaymentCommand { get; }

        // Test card insertion commands (for sandbox mode)
        public ICommand InsertTestVisaCommand { get; }
        public ICommand InsertTestMastercardCommand { get; }
        public ICommand InsertTestAmexCommand { get; }

        // Card reader connection retry command
        public ICommand RetryCardReaderConnectionCommand { get; }

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
            Action<bool, string> onPaymentComplete,
            MainViewModel? mainViewModel = null)
        {
            _onNavigateBack = onNavigateBack ?? throw new ArgumentNullException(nameof(onNavigateBack));
            _onPaymentComplete = onPaymentComplete ?? throw new ArgumentNullException(nameof(onPaymentComplete));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _mainViewModel = mainViewModel;

            // Use the same cart items collection (shared reference)
            CartItems = cartItems;

            // Check if payment is enabled and get payment mode
            var config = _configService.GetConfiguration();
            IsPaymentEnabled = config.PaymentEnabled;
            _paymentMode = config.FortePaymentMode;
            _isSandboxMode = config.ForteSandboxMode;

            Console.WriteLine($"[PaymentViewModel] Payment Mode: {_paymentMode}, Sandbox: {_isSandboxMode}");

            // Initialize appropriate Forte service based on mode
            if (IsPaymentEnabled)
            {
                if (_paymentMode == FortePaymentMode.Checkout)
                {
                    _forteCheckoutService = new ForteCheckoutService(_configService);
                }
                else
                {
                    _fortePaymentService = new FortePaymentService(_configService);

                    // Initialize Dynaflex service for card reader detection (both sandbox and production)
                    // Show "Connecting to card reader" screen first
                    IsConnectingToCardReader = true;
                    IsWaitingForCard = false;

                    _dynaflexService = new DynaflexService();
                    _dynaflexService.ConnectionStateChanged += OnDynaflexConnectionChanged;
                    _dynaflexService.CardDataReceived += OnDynaflexCardDataReceived;
                    _dynaflexService.LogMessage += OnDynaflexLogMessage;

                    // Start connecting to Dynaflex in background
                    _ = ConnectToDynaflexAsync();
                }
            }

            IncreaseQuantityCommand = new RelayCommand<CartItem>(OnIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(OnDecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
            GoBackCommand = new RelayCommand(OnGoBack);
            PayCommand = new RelayCommand(async () => await OnPayAsync(), CanPay);
            ProcessRestApiPaymentCommand = new RelayCommand(async () => await ProcessRestApiPaymentAsync());

            // Test card insertion commands
            InsertTestVisaCommand = new RelayCommand(async () => await OnInsertTestCardAsync("visa"));
            InsertTestMastercardCommand = new RelayCommand(async () => await OnInsertTestCardAsync("mastercard"));
            InsertTestAmexCommand = new RelayCommand(async () => await OnInsertTestCardAsync("amex"));

            // Card reader connection retry command
            RetryCardReaderConnectionCommand = new RelayCommand(async () => await RetryCardReaderConnectionAsync());

            Console.WriteLine($"[PaymentViewModel] Initialized - Payment Enabled: {IsPaymentEnabled}, Mode: {_paymentMode}");

            // Go directly to payment screen (no cart review) - only for Checkout mode
            if (IsPaymentEnabled && _paymentMode == FortePaymentMode.Checkout)
            {
                Console.WriteLine($"[PaymentViewModel] Loading Forte Checkout directly...");
                IsProcessing = true;
                _ = LoadCheckoutDirectlyAsync();
            }
        }

        /// <summary>
        /// Loads Forte Checkout directly on initialization (skips cart review)
        /// </summary>
        private async Task LoadCheckoutDirectlyAsync()
        {
            try
            {
                var orderLabel = GenerateOrderLabel();
                Console.WriteLine($"[PaymentViewModel] Generating Forte Checkout HTML for ${CartTotalPrice}, Order: {orderLabel}");
                CheckoutHtml = await _forteCheckoutService!.GetCheckoutHtmlAsync(CartTotalPrice, orderLabel);
                Console.WriteLine("[PaymentViewModel] Forte Checkout loaded - waiting for payment...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Failed to load checkout: {ex.Message}");
                PaymentStatusMessage = $"Failed to load payment: {ex.Message}";
                IsProcessing = false;
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
        public void HandleCheckoutSuccess(string transactionId, string authorizationCode, string orderLabel, string cardLast4 = "")
        {
            Console.WriteLine($"[PaymentViewModel] Payment successful - Transaction ID: {transactionId}, Auth Code: {authorizationCode}, Card: ****{cardLast4}");

            // Store payment details in MainViewModel for receipt printing
            if (_mainViewModel != null)
            {
                _mainViewModel.LastPaymentTransactionId = transactionId;
                _mainViewModel.LastPaymentAuthorizationCode = authorizationCode;
                _mainViewModel.LastPaymentCardLast4Digits = cardLast4;
                Console.WriteLine("[PaymentViewModel] Payment details stored in MainViewModel for receipt printing");
            }

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

        /// <summary>
        /// Process payment via Forte REST API (for REST API mode)
        /// </summary>
        private async Task ProcessRestApiPaymentAsync()
        {
            if (_fortePaymentService == null)
            {
                PaymentStatusMessage = "REST API service not initialized";
                return;
            }

            try
            {
                IsProcessing = true;
                PaymentStatusMessage = "Processing payment...";
                RestApiLogs.Clear();
                OnPropertyChanged(nameof(HasRestApiLogs));

                string orderLabel = GenerateOrderLabel();

                // Add log callback
                void AddLog(string message)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        RestApiLogs.Add(message);
                        OnPropertyChanged(nameof(HasRestApiLogs));
                    });
                }

                AddLog($"Order Label: {orderLabel}");
                AddLog($"Amount: ${CartTotalPrice:F2}");

                // Parse expiry month/year
                if (!int.TryParse(ExpiryMonth, out int expMonth) || expMonth < 1 || expMonth > 12)
                {
                    AddLog("ERROR: Invalid expiry month");
                    PaymentStatusMessage = "Invalid expiry month (1-12)";
                    IsProcessing = false;
                    OnPropertyChanged(nameof(CanProcessPayment));
                    return;
                }

                if (!int.TryParse(ExpiryYear, out int expYear))
                {
                    AddLog("ERROR: Invalid expiry year");
                    PaymentStatusMessage = "Invalid expiry year";
                    IsProcessing = false;
                    OnPropertyChanged(nameof(CanProcessPayment));
                    return;
                }

                // Handle 2-digit year
                if (expYear < 100)
                {
                    expYear += 2000;
                }

                AddLog($"Calling Forte REST API...");

                // Process payment via REST API
                var result = await _fortePaymentService.ProcessPaymentAsync(
                    amount: CartTotalPrice,
                    cardNumber: CardNumber.Replace(" ", "").Replace("-", ""),
                    expiryMonth: expMonth,
                    expiryYear: expYear,
                    cvv: Cvv,
                    cardholderName: CardholderName,
                    billingZip: null,
                    logCallback: AddLog
                );

                if (result.Success)
                {
                    AddLog($"✓ Payment APPROVED");
                    PaymentStatusMessage = $"Payment approved! Transaction: {result.TransactionId}";

                    // Store payment details in MainViewModel for receipt printing
                    if (_mainViewModel != null)
                    {
                        _mainViewModel.LastPaymentTransactionId = result.TransactionId ?? "";
                        _mainViewModel.LastPaymentAuthorizationCode = result.AuthorizationCode ?? "";
                        _mainViewModel.LastPaymentCardLast4Digits = result.CardLast4 ?? "";
                        Console.WriteLine("[PaymentViewModel] REST API payment details stored in MainViewModel");
                    }

                    // Delay to show success message
                    await Task.Delay(1500);

                    IsProcessing = false;
                    _onPaymentComplete(true, orderLabel);
                }
                else
                {
                    AddLog($"✗ Payment FAILED: {result.ErrorMessage}");
                    PaymentStatusMessage = $"Payment failed: {result.ErrorMessage}";
                    IsProcessing = false;
                    OnPropertyChanged(nameof(CanProcessPayment));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] REST API Payment error: {ex.Message}");
                RestApiLogs.Add($"EXCEPTION: {ex.Message}");
                OnPropertyChanged(nameof(HasRestApiLogs));
                PaymentStatusMessage = $"Error: {ex.Message}";
                IsProcessing = false;
                OnPropertyChanged(nameof(CanProcessPayment));
            }
        }

        /// <summary>
        /// Simulate test card insertion (for sandbox mode)
        /// </summary>
        private async Task OnInsertTestCardAsync(string cardType)
        {
            if (_fortePaymentService == null)
            {
                PaymentStatusMessage = "REST API service not initialized";
                return;
            }

            Console.WriteLine($"[PaymentViewModel] Test card inserted: {cardType}");

            // Hide "Insert Card" screen and show processing
            IsWaitingForCard = false;
            IsProcessing = true;
            PaymentStatusMessage = "Processing card...";
            RestApiLogs.Clear();
            OnPropertyChanged(nameof(HasRestApiLogs));

            try
            {
                // Set test card details based on type
                switch (cardType.ToLower())
                {
                    case "visa":
                        CardNumber = "4111111111111111";
                        CardholderName = "Test Visa User";
                        Cvv = "123";
                        break;
                    case "mastercard":
                        CardNumber = "5454545454545454";
                        CardholderName = "Test MC User";
                        Cvv = "123";
                        break;
                    case "amex":
                        CardNumber = "370000000000002";
                        CardholderName = "Test Amex User";
                        Cvv = "1234";
                        break;
                }
                ExpiryMonth = "12";
                ExpiryYear = "2028";

                string orderLabel = GenerateOrderLabel();

                // Add log callback
                void AddLog(string message)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        RestApiLogs.Add(message);
                        OnPropertyChanged(nameof(HasRestApiLogs));
                    });
                }

                AddLog($"═══════════════════════════════════════════");
                AddLog($"TEST CARD INSERTED: {cardType.ToUpper()}");
                AddLog($"═══════════════════════════════════════════");
                AddLog($"Order Label: {orderLabel}");
                AddLog($"Amount: ${CartTotalPrice:F2}");
                AddLog($"Calling Forte REST API...");

                // Process payment via REST API
                var result = await _fortePaymentService.ProcessPaymentAsync(
                    amount: CartTotalPrice,
                    cardNumber: CardNumber.Replace(" ", "").Replace("-", ""),
                    expiryMonth: 12,
                    expiryYear: 2028,
                    cvv: Cvv,
                    cardholderName: CardholderName,
                    billingZip: null,
                    logCallback: AddLog
                );

                if (result.Success)
                {
                    AddLog($"✓ Payment APPROVED");
                    PaymentStatusMessage = $"Payment approved! Transaction: {result.TransactionId}";

                    // Store payment details in MainViewModel for receipt printing
                    if (_mainViewModel != null)
                    {
                        _mainViewModel.LastPaymentTransactionId = result.TransactionId ?? "";
                        _mainViewModel.LastPaymentAuthorizationCode = result.AuthorizationCode ?? "";
                        _mainViewModel.LastPaymentCardLast4Digits = result.CardLast4 ?? "";
                        Console.WriteLine("[PaymentViewModel] REST API test card payment details stored");
                    }

                    // Delay to show success message
                    await Task.Delay(1500);

                    IsProcessing = false;
                    _onPaymentComplete(true, orderLabel);
                }
                else
                {
                    AddLog($"✗ Payment FAILED: {result.ErrorMessage}");
                    PaymentStatusMessage = $"Payment failed: {result.ErrorMessage}";
                    IsProcessing = false;
                    // Show insert card screen again on failure
                    IsWaitingForCard = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Test card payment error: {ex.Message}");
                RestApiLogs.Add($"EXCEPTION: {ex.Message}");
                OnPropertyChanged(nameof(HasRestApiLogs));
                PaymentStatusMessage = $"Error: {ex.Message}";
                IsProcessing = false;
                IsWaitingForCard = true;
            }
        }

        #region Dynaflex Integration

        private int _connectionAttempts = 0;
        private const int MAX_CONNECTION_ATTEMPTS = 5;

        /// <summary>
        /// Connect to Dynaflex card reader
        /// </summary>
        private async Task ConnectToDynaflexAsync()
        {
            if (_dynaflexService == null) return;

            try
            {
                _connectionAttempts++;
                CardReaderConnectionFailed = false;

                DynaflexStatus = $"Searching for card reader... (Attempt {_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";
                Console.WriteLine($"[PaymentViewModel] Searching for Dynaflex card reader (Attempt {_connectionAttempts})...");

                // Scan for devices first
                var devices = _dynaflexService.ScanDevices();

                if (devices.Count == 0)
                {
                    Console.WriteLine("[PaymentViewModel] No Dynaflex device found");

                    if (_connectionAttempts >= MAX_CONNECTION_ATTEMPTS)
                    {
                        // Max attempts reached
                        if (_isSandboxMode)
                        {
                            // In sandbox mode, proceed to Insert Card screen with test buttons
                            DynaflexStatus = "No card reader (test mode)";
                            CardReaderConnectionFailed = false;
                            IsConnectingToCardReader = false;
                            IsPreparingTerminal = false;
                            IsWaitingForCard = true;
                            Console.WriteLine("[PaymentViewModel] Sandbox mode - proceeding without card reader");
                        }
                        else
                        {
                            // Production mode - show error
                            DynaflexStatus = "No card reader detected";
                            CardReaderConnectionFailed = true;
                            IsConnectingToCardReader = false;
                            IsPreparingTerminal = false;
                            Console.WriteLine("[PaymentViewModel] Max connection attempts reached - showing error");
                        }
                        return;
                    }

                    DynaflexStatus = $"No device found, retrying... ({_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";

                    // Retry in 2 seconds
                    await Task.Delay(2000);
                    if (!_isDisposed && !IsDynaflexConnected)
                    {
                        _ = ConnectToDynaflexAsync();
                    }
                    return;
                }

                DynaflexStatus = "Connecting to card reader...";

                bool connected = await _dynaflexService.ConnectAsync();

                if (connected)
                {
                    Console.WriteLine("[PaymentViewModel] Connected to Dynaflex successfully!");
                    IsDynaflexConnected = true;
                    DynaflexStatus = "Card reader connected";

                    // Transition to "Preparing Terminal" screen (not Insert Card yet!)
                    IsConnectingToCardReader = false;
                    IsPreparingTerminal = true;
                    IsWaitingForCard = false;

                    Console.WriteLine("[PaymentViewModel] Transitioned to 'Preparing Terminal' screen");

                    // Start EMV transaction to enable card reading (device will beep)
                    Console.WriteLine("[PaymentViewModel] Starting EMV transaction to enable card reading...");
                    await StartCardReadingAsync();
                }
                else
                {
                    Console.WriteLine("[PaymentViewModel] Failed to connect to Dynaflex");

                    if (_connectionAttempts >= MAX_CONNECTION_ATTEMPTS)
                    {
                        if (_isSandboxMode)
                        {
                            // In sandbox mode, proceed to Insert Card screen with test buttons
                            DynaflexStatus = "Connection failed (test mode)";
                            CardReaderConnectionFailed = false;
                            IsConnectingToCardReader = false;
                            IsPreparingTerminal = false;
                            IsWaitingForCard = true;
                            Console.WriteLine("[PaymentViewModel] Sandbox mode - proceeding without card reader");
                        }
                        else
                        {
                            DynaflexStatus = "Failed to connect to card reader";
                            CardReaderConnectionFailed = true;
                            IsConnectingToCardReader = false;
                            IsPreparingTerminal = false;
                        }
                        return;
                    }

                    DynaflexStatus = $"Connection failed, retrying... ({_connectionAttempts}/{MAX_CONNECTION_ATTEMPTS})";

                    // Retry in 2 seconds
                    await Task.Delay(2000);
                    if (!_isDisposed && !IsDynaflexConnected)
                    {
                        _ = ConnectToDynaflexAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Dynaflex connection error: {ex.Message}");
                DynaflexStatus = $"Error: {ex.Message}";

                if (_connectionAttempts >= MAX_CONNECTION_ATTEMPTS)
                {
                    if (_isSandboxMode)
                    {
                        // In sandbox mode, proceed to Insert Card screen with test buttons
                        CardReaderConnectionFailed = false;
                        IsConnectingToCardReader = false;
                        IsPreparingTerminal = false;
                        IsWaitingForCard = true;
                    }
                    else
                    {
                        CardReaderConnectionFailed = true;
                        IsConnectingToCardReader = false;
                        IsPreparingTerminal = false;
                    }
                }
            }
        }

        /// <summary>
        /// Retry card reader connection after failure
        /// </summary>
        private async Task RetryCardReaderConnectionAsync()
        {
            Console.WriteLine("[PaymentViewModel] Retrying card reader connection...");

            _connectionAttempts = 0;
            CardReaderConnectionFailed = false;
            IsConnectingToCardReader = true;
            IsWaitingForCard = false;

            await ConnectToDynaflexAsync();
        }

        /// <summary>
        /// Start card reading on the Dynaflex device
        /// This sends the StartTransaction command which makes the device beep and ready to accept cards
        /// </summary>
        private async Task StartCardReadingAsync()
        {
            if (_dynaflexService == null || !_dynaflexService.IsConnected)
            {
                Console.WriteLine("[PaymentViewModel] Cannot start card reading - not connected");
                // In sandbox mode, still allow proceeding
                if (_isSandboxMode)
                {
                    IsPreparingTerminal = false;
                    IsWaitingForCard = true;
                    DynaflexStatus = "Test mode - use buttons below";
                }
                return;
            }

            try
            {
                DynaflexStatus = "Preparing terminal...";

                // Start EMV transaction with the cart total amount
                bool started = await _dynaflexService.StartTransactionAsync(CartTotalPrice);

                if (started)
                {
                    Console.WriteLine("[PaymentViewModel] Card reading started - device should beep");
                    DynaflexStatus = "Terminal ready";

                    // NOW transition to "Insert Card" screen - terminal is ready!
                    IsPreparingTerminal = false;
                    IsWaitingForCard = true;
                    Console.WriteLine("[PaymentViewModel] Terminal ready - showing 'Insert Card' screen");
                }
                else
                {
                    // Try RequestCard as fallback (for non-EMV mode)
                    Console.WriteLine("[PaymentViewModel] StartTransaction not supported, trying RequestCard...");
                    bool requested = await _dynaflexService.RequestCardAsync();

                    if (requested)
                    {
                        Console.WriteLine("[PaymentViewModel] Card request sent - device should beep");
                        DynaflexStatus = "Terminal ready";

                        // Transition to "Insert Card" screen
                        IsPreparingTerminal = false;
                        IsWaitingForCard = true;
                        Console.WriteLine("[PaymentViewModel] Terminal ready - showing 'Insert Card' screen");
                    }
                    else
                    {
                        Console.WriteLine("[PaymentViewModel] Failed to initiate card reading");
                        DynaflexStatus = "Card reader connected (passive mode)";

                        // Still allow proceeding - device might work in passive mode
                        IsPreparingTerminal = false;
                        IsWaitingForCard = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentViewModel] Error starting card reading: {ex.Message}");
                DynaflexStatus = "Card reader connected";

                // Still allow proceeding
                IsPreparingTerminal = false;
                IsWaitingForCard = true;
            }
        }

        /// <summary>
        /// Handle Dynaflex connection state changes
        /// </summary>
        private void OnDynaflexConnectionChanged(object? sender, DynaflexConnectionEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsDynaflexConnected = e.IsConnected;

                if (e.IsConnected)
                {
                    DynaflexStatus = "Card reader connected";
                    Console.WriteLine($"[PaymentViewModel] Dynaflex connected: {e.DeviceSerialNumber}");

                    // If we were connecting, transition to preparing terminal screen
                    if (IsConnectingToCardReader)
                    {
                        IsConnectingToCardReader = false;
                        CardReaderConnectionFailed = false;
                        IsPreparingTerminal = true;
                        IsWaitingForCard = false;

                        // Start card reading (send StartTransaction command to make device beep)
                        Console.WriteLine("[PaymentViewModel] Starting card reading after connection...");
                        _ = StartCardReadingAsync();
                    }
                }
                else
                {
                    DynaflexStatus = "Card reader disconnected";
                    Console.WriteLine("[PaymentViewModel] Dynaflex disconnected");

                    // If we were waiting for card or preparing, go back to connecting screen
                    if ((IsWaitingForCard || IsPreparingTerminal) && !IsProcessing)
                    {
                        IsWaitingForCard = false;
                        IsPreparingTerminal = false;
                        IsConnectingToCardReader = true;
                        _connectionAttempts = 0;

                        // Try to reconnect
                        if (!_isDisposed)
                        {
                            _ = ConnectToDynaflexAsync();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Handle card data received from Dynaflex
        /// </summary>
        private async void OnDynaflexCardDataReceived(object? sender, DynaflexCardDataEventArgs e)
        {
            Console.WriteLine($"[PaymentViewModel] Card data received - Valid: {e.IsValid}, Type: {e.CardType}");

            if (!e.IsValid)
            {
                Console.WriteLine("[PaymentViewModel] Invalid card data received, ignoring");
                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Hide "Insert Card" screen and show processing
                    IsWaitingForCard = false;
                    IsProcessing = true;
                    DynaflexStatus = "Card detected - processing...";
                    PaymentStatusMessage = "Processing card...";
                    RestApiLogs.Clear();
                    OnPropertyChanged(nameof(HasRestApiLogs));

                    string orderLabel = GenerateOrderLabel();

                    // Add log callback
                    void AddLog(string message)
                    {
                        RestApiLogs.Add(message);
                        OnPropertyChanged(nameof(HasRestApiLogs));
                    }

                    AddLog($"═══════════════════════════════════════════");
                    AddLog($"CARD INSERTED/TAPPED: {e.CardType}");
                    AddLog($"═══════════════════════════════════════════");
                    AddLog($"Order Label: {orderLabel}");
                    AddLog($"Amount: ${CartTotalPrice:F2}");
                    AddLog($"Device Serial: {e.DeviceSerialNumber}");

                    if (!string.IsNullOrEmpty(e.KSN))
                    {
                        AddLog($"KSN: {e.KSN.Substring(0, Math.Min(16, e.KSN.Length))}...");
                    }

                    AddLog($"Calling Forte REST API with Dynaflex encrypted data...");

                    // Build the card_data string for Forte
                    var encryptedCardData = DynaflexService.BuildForteCardData(e);

                    if (string.IsNullOrEmpty(encryptedCardData))
                    {
                        AddLog("ERROR: No encrypted card data available");
                        PaymentStatusMessage = "Card read error - please try again";
                        IsProcessing = false;
                        IsWaitingForCard = true;
                        DynaflexStatus = "Ready - Insert or tap card";
                        return;
                    }

                    // Process payment via REST API with Dynaflex encrypted data
                    var result = await _fortePaymentService!.ProcessDynaflexPaymentAsync(
                        amount: CartTotalPrice,
                        encryptedCardData: encryptedCardData,
                        logCallback: AddLog
                    );

                    if (result.Success)
                    {
                        AddLog($"✓ Payment APPROVED");
                        PaymentStatusMessage = $"Payment approved! Transaction: {result.TransactionId}";
                        DynaflexStatus = "Payment successful!";

                        // Store payment details in MainViewModel for receipt printing
                        if (_mainViewModel != null)
                        {
                            _mainViewModel.LastPaymentTransactionId = result.TransactionId ?? "";
                            _mainViewModel.LastPaymentAuthorizationCode = result.AuthorizationCode ?? "";
                            _mainViewModel.LastPaymentCardLast4Digits = result.CardLast4 ?? e.CardLast4;
                            Console.WriteLine("[PaymentViewModel] Dynaflex payment details stored in MainViewModel");
                        }

                        // Delay to show success message
                        await Task.Delay(1500);

                        IsProcessing = false;
                        _onPaymentComplete(true, orderLabel);
                    }
                    else
                    {
                        AddLog($"✗ Payment FAILED: {result.ErrorMessage}");
                        PaymentStatusMessage = $"Payment failed: {result.ErrorMessage}";
                        DynaflexStatus = "Payment failed - try again";
                        IsProcessing = false;
                        // Show insert card screen again on failure
                        IsWaitingForCard = true;

                        // Reset status after delay
                        await Task.Delay(3000);
                        DynaflexStatus = "Ready - Insert or tap card";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PaymentViewModel] Dynaflex payment error: {ex.Message}");
                    RestApiLogs.Add($"EXCEPTION: {ex.Message}");
                    OnPropertyChanged(nameof(HasRestApiLogs));
                    PaymentStatusMessage = $"Error: {ex.Message}";
                    DynaflexStatus = "Error - try again";
                    IsProcessing = false;
                    IsWaitingForCard = true;
                }
            });
        }

        /// <summary>
        /// Handle log messages from Dynaflex service
        /// </summary>
        private void OnDynaflexLogMessage(object? sender, string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RestApiLogs.Add(message);
                OnPropertyChanged(nameof(HasRestApiLogs));
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_dynaflexService != null)
            {
                _dynaflexService.ConnectionStateChanged -= OnDynaflexConnectionChanged;
                _dynaflexService.CardDataReceived -= OnDynaflexCardDataReceived;
                _dynaflexService.LogMessage -= OnDynaflexLogMessage;
                _dynaflexService.Dispose();
            }

            Console.WriteLine("[PaymentViewModel] Disposed");
        }

        #endregion
    }
}
