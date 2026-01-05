using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using IPS.Core.Models;
using IPS.Services;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for admin configuration page
    /// Allows configuration of unmanned system IP addresses and ports
    /// </summary>
    public class AdminViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configService;
        private readonly PasswordService _passwordService;
        private readonly NetworkScannerService _networkScanner;
        private readonly PrinterScannerService _printerScanner;
        private readonly ForteCheckoutService? _forteCheckoutService;
        private readonly Action? _onNavigateBack;
        private int _dllServerPort;
        private bool _isScanning;
        private int _scanProgress;
        private string _scanStatus = string.Empty;
        private string _selectedPrinter = string.Empty;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _passwordChangeMessage = string.Empty;
        private bool _hasPasswordChangeMessage = false;
        private bool _isPasswordChangeSuccess = false;
        private string _selectedSectionId = "Systems";
        private bool _isBreakTimeEnabled = false;
        private bool _isInstantBreakActive = false;
        private int _breakStartHour = 14;
        private int _breakStartMinute = 0;
        private int _breakEndHour = 15;
        private int _breakEndMinute = 0;
        private string _breakMessage = string.Empty;
        private string _forteApiAccessId = string.Empty;
        private string _forteApiSecureKey = string.Empty;
        private string _forteOrganizationId = string.Empty;
        private string _forteLocationId = string.Empty;
        private bool _forteSandboxMode = true;
        private Core.Models.FortePaymentMode _fortePaymentMode = Core.Models.FortePaymentMode.Checkout;
        private bool _paymentEnabled = false;
        private bool _useCardTerminal = false;
        private string _terminalType = "Verifone V400C Plus";
        private string _terminalConnection = "USB";
        private string _terminalComPort = "COM1";
        private string _forteMerchantId = string.Empty;
        private string _forteProcessingPassword = string.Empty;
        private bool _isTestingPayment = false;
        private string _paymentTestStatus = string.Empty;
        private bool _hasPaymentTestStatus = false;
        private bool _isPaymentTestSuccess = false;
        private bool _isSaving = false;

        /// <summary>
        /// Payment test logs
        /// </summary>
        public ObservableCollection<string> PaymentTestLogs { get; } = new();

        private string _paymentTestHtml = string.Empty;
        private System.Threading.CancellationTokenSource? _paymentTestCancellation;
        private TaskCompletionSource<(bool success, string message)>? _paymentTestCompletion;

        /// <summary>
        /// Collection of configuration section tabs
        /// </summary>
        public ObservableCollection<ConfigSection> ConfigSections { get; } = new();

        /// <summary>
        /// Currently selected section ID
        /// </summary>
        public string SelectedSectionId
        {
            get => _selectedSectionId;
            set
            {
                _selectedSectionId = value;
                OnPropertyChanged();
                UpdateSectionVisibility();
            }
        }

        /// <summary>
        /// Collection of system configurations
        /// </summary>
        public ObservableCollection<SystemConfigurationViewModel> Systems { get; } = new();

        /// <summary>
        /// Port for the DLL's internal HTTP server
        /// </summary>
        public int DllServerPort
        {
            get => _dllServerPort;
            set
            {
                _dllServerPort = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicates if network scan is in progress
        /// </summary>
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicates if save operation is in progress
        /// </summary>
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Scan progress percentage for network scan (0-100)
        /// </summary>
        public int ScanProgress
        {
            get => _scanProgress;
            set
            {
                _scanProgress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Status message for network scan operation
        /// </summary>
        public string ScanStatus
        {
            get => _scanStatus;
            set
            {
                _scanStatus = value;
                OnPropertyChanged();
            }
        }

        private bool _isPrinterScanning = false;
        private int _printerScanProgress = 0;
        private string _printerScanStatus = string.Empty;

        /// <summary>
        /// Indicates if printer scan is in progress
        /// </summary>
        public bool IsPrinterScanning
        {
            get => _isPrinterScanning;
            set
            {
                _isPrinterScanning = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Scan progress percentage for printer scan (0-100)
        /// </summary>
        public int PrinterScanProgress
        {
            get => _printerScanProgress;
            set
            {
                _printerScanProgress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Status message for printer scan operation
        /// </summary>
        public string PrinterScanStatus
        {
            get => _printerScanStatus;
            set
            {
                _printerScanStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Selected printer name
        /// </summary>
        public string SelectedPrinter
        {
            get => _selectedPrinter;
            set
            {
                _selectedPrinter = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Discovered devices from network scan
        /// </summary>
        public ObservableCollection<NetworkScannerService.DiscoveredDevice> DiscoveredDevices { get; } = new();

        /// <summary>
        /// Available printers from printer scan
        /// </summary>
        public ObservableCollection<PrinterScannerService.DiscoveredPrinter> AvailablePrinters { get; } = new();

        /// <summary>
        /// Current password for password change
        /// </summary>
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// New password for password change
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Confirm new password for password change
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Password change status/error message
        /// </summary>
        public string PasswordChangeMessage
        {
            get => _passwordChangeMessage;
            set
            {
                _passwordChangeMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether a password change message is displayed
        /// </summary>
        public bool HasPasswordChangeMessage
        {
            get => _hasPasswordChangeMessage;
            set
            {
                _hasPasswordChangeMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the password change was successful (for styling)
        /// </summary>
        public bool IsPasswordChangeSuccess
        {
            get => _isPasswordChangeSuccess;
            set
            {
                _isPasswordChangeSuccess = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether scheduled break time mode is enabled
        /// </summary>
        public bool IsBreakTimeEnabled
        {
            get => _isBreakTimeEnabled;
            set
            {
                _isBreakTimeEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether instant break mode is active (manual override)
        /// </summary>
        public bool IsInstantBreakActive
        {
            get => _isInstantBreakActive;
            set
            {
                _isInstantBreakActive = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Break start hour (0-23)
        /// </summary>
        public int BreakStartHour
        {
            get => _breakStartHour;
            set
            {
                _breakStartHour = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Break start minute (0-59)
        /// </summary>
        public int BreakStartMinute
        {
            get => _breakStartMinute;
            set
            {
                _breakStartMinute = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Break end hour (0-23)
        /// </summary>
        public int BreakEndHour
        {
            get => _breakEndHour;
            set
            {
                _breakEndHour = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Break end minute (0-59)
        /// </summary>
        public int BreakEndMinute
        {
            get => _breakEndMinute;
            set
            {
                _breakEndMinute = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Custom break message
        /// </summary>
        public string BreakMessage
        {
            get => _breakMessage;
            set
            {
                _breakMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte API Access ID
        /// </summary>
        public string ForteApiAccessId
        {
            get => _forteApiAccessId;
            set
            {
                _forteApiAccessId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte API Secure Key
        /// </summary>
        public string ForteApiSecureKey
        {
            get => _forteApiSecureKey;
            set
            {
                _forteApiSecureKey = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte Organization ID
        /// </summary>
        public string ForteOrganizationId
        {
            get => _forteOrganizationId;
            set
            {
                _forteOrganizationId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte Location ID
        /// </summary>
        public string ForteLocationId
        {
            get => _forteLocationId;
            set
            {
                _forteLocationId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte Sandbox Mode
        /// </summary>
        public bool ForteSandboxMode
        {
            get => _forteSandboxMode;
            set
            {
                _forteSandboxMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte Payment Mode (Checkout v2 or REST API)
        /// </summary>
        public Core.Models.FortePaymentMode FortePaymentMode
        {
            get => _fortePaymentMode;
            set
            {
                _fortePaymentMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCheckoutMode));
                OnPropertyChanged(nameof(IsRestApiMode));
            }
        }

        /// <summary>
        /// Helper property for UI binding - true when Checkout mode is selected
        /// </summary>
        public bool IsCheckoutMode => _fortePaymentMode == Core.Models.FortePaymentMode.Checkout;

        /// <summary>
        /// Helper property for UI binding - true when REST API mode is selected
        /// </summary>
        public bool IsRestApiMode => _fortePaymentMode == Core.Models.FortePaymentMode.RestApi;

        /// <summary>
        /// Set the payment mode (called from code-behind)
        /// </summary>
        public void SetPaymentMode(Core.Models.FortePaymentMode mode)
        {
            FortePaymentMode = mode;
            Console.WriteLine($"[AdminViewModel] Payment mode changed to: {mode}");
        }

        /// <summary>
        /// Enable payment processing
        /// </summary>
        public bool PaymentEnabled
        {
            get => _paymentEnabled;
            set
            {
                _paymentEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Use physical card terminal for card-present transactions
        /// </summary>
        public bool UseCardTerminal
        {
            get => _useCardTerminal;
            set
            {
                _useCardTerminal = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Terminal type (e.g., Verifone VX520, V400C Plus)
        /// </summary>
        public string TerminalType
        {
            get => _terminalType;
            set
            {
                _terminalType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Terminal connection type (USB, Serial)
        /// </summary>
        public string TerminalConnection
        {
            get => _terminalConnection;
            set
            {
                _terminalConnection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSerialConnection));
            }
        }

        /// <summary>
        /// COM Port for serial connection
        /// </summary>
        public string TerminalComPort
        {
            get => _terminalComPort;
            set
            {
                _terminalComPort = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte AGI Merchant ID
        /// </summary>
        public string ForteMerchantId
        {
            get => _forteMerchantId;
            set
            {
                _forteMerchantId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Forte AGI Processing Password
        /// </summary>
        public string ForteProcessingPassword
        {
            get => _forteProcessingPassword;
            set
            {
                _forteProcessingPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Helper property to show/hide COM Port based on connection type
        /// </summary>
        public bool IsSerialConnection => TerminalConnection == "Serial";

        /// <summary>
        /// Indicates if payment test is in progress
        /// </summary>
        public bool IsTestingPayment
        {
            get => _isTestingPayment;
            set
            {
                _isTestingPayment = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Payment test status message
        /// </summary>
        public string PaymentTestStatus
        {
            get => _paymentTestStatus;
            set
            {
                _paymentTestStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether a payment test status message is displayed
        /// </summary>
        public bool HasPaymentTestStatus
        {
            get => _hasPaymentTestStatus;
            set
            {
                _hasPaymentTestStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the payment test was successful (for styling)
        /// </summary>
        public bool IsPaymentTestSuccess
        {
            get => _isPaymentTestSuccess;
            set
            {
                _isPaymentTestSuccess = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Payment test HTML for WebView2
        /// </summary>
        public string PaymentTestHtml
        {
            get => _paymentTestHtml;
            set
            {
                _paymentTestHtml = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to add new system configuration
        /// </summary>
        public IRelayCommand AddSystemCommand { get; }

        /// <summary>
        /// Command to remove system configuration
        /// </summary>
        public IRelayCommand<SystemConfigurationViewModel> RemoveSystemCommand { get; }

        /// <summary>
        /// Command to scan network for devices
        /// </summary>
        public IRelayCommand ScanNetworkCommand { get; }

        /// <summary>
        /// Command to scan for printers
        /// </summary>
        public IRelayCommand ScanPrintersCommand { get; }

        /// <summary>
        /// Command to add discovered device to configuration
        /// </summary>
        public IRelayCommand<NetworkScannerService.DiscoveredDevice> AddDiscoveredDeviceCommand { get; }

        /// <summary>
        /// Command to test printer
        /// </summary>
        public IRelayCommand<PrinterScannerService.DiscoveredPrinter> TestPrinterCommand { get; }

        /// <summary>
        /// Command to save configuration (stays on page)
        /// </summary>
        public IRelayCommand SaveCommand { get; }

        /// <summary>
        /// Command to save configuration and navigate back
        /// </summary>
        public IRelayCommand SaveAndExitCommand { get; }

        /// <summary>
        /// Command to cancel and navigate back
        /// </summary>
        public IRelayCommand CancelCommand { get; }

        /// <summary>
        /// Command to exit the application
        /// </summary>
        public IRelayCommand ExitCommand { get; }

        /// <summary>
        /// Command to change admin password
        /// </summary>
        public IRelayCommand ChangePasswordCommand { get; }

        /// <summary>
        /// Command to select a configuration section
        /// </summary>
        public IRelayCommand<string> SelectSectionCommand { get; }

        /// <summary>
        /// Command to test payment integration (Checkout mode)
        /// </summary>
        public IRelayCommand TestPaymentCommand { get; }

        /// <summary>
        /// Command to test REST API connection
        /// </summary>
        public IRelayCommand TestRestApiCommand { get; }

        /// <summary>
        /// Command to fetch store information from Forte
        /// </summary>
        public IRelayCommand FetchFromForteCommand { get; }

        /// <summary>
        /// Command to select a receipt printer
        /// </summary>
        public IRelayCommand<string> SelectReceiptPrinterCommand { get; }

        /// <summary>
        /// Command to test card reader connection
        /// </summary>
        public IRelayCommand TestCardReaderCommand { get; }

        // Section visibility properties
        private bool _isSystemsSectionVisible = true;
        private bool _isSalesSectionVisible = false;
        private bool _isPrintersSectionVisible = false;
        private bool _isServerSectionVisible = false;
        private bool _isBreakTimeSectionVisible = false;
        private bool _isPaymentSectionVisible = false;
        private bool _isSecuritySectionVisible = false;
        private bool _isReceiptSectionVisible = false;

        public bool IsSystemsSectionVisible
        {
            get => _isSystemsSectionVisible;
            set
            {
                _isSystemsSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsSalesSectionVisible
        {
            get => _isSalesSectionVisible;
            set
            {
                _isSalesSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrintersSectionVisible
        {
            get => _isPrintersSectionVisible;
            set
            {
                _isPrintersSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsServerSectionVisible
        {
            get => _isServerSectionVisible;
            set
            {
                _isServerSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsBreakTimeSectionVisible
        {
            get => _isBreakTimeSectionVisible;
            set
            {
                _isBreakTimeSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsPaymentSectionVisible
        {
            get => _isPaymentSectionVisible;
            set
            {
                _isPaymentSectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsSecuritySectionVisible
        {
            get => _isSecuritySectionVisible;
            set
            {
                _isSecuritySectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsReceiptSectionVisible
        {
            get => _isReceiptSectionVisible;
            set
            {
                _isReceiptSectionVisible = value;
                OnPropertyChanged();
            }
        }

        // ========================================
        // Receipt Settings Properties
        // ========================================

        private string _businessName = string.Empty;
        public string BusinessName
        {
            get => _businessName;
            set
            {
                _businessName = value;
                OnPropertyChanged();
            }
        }

        private string _businessAddressLine1 = string.Empty;
        public string BusinessAddressLine1
        {
            get => _businessAddressLine1;
            set
            {
                _businessAddressLine1 = value;
                OnPropertyChanged();
            }
        }

        private string _businessAddressLine2 = string.Empty;
        public string BusinessAddressLine2
        {
            get => _businessAddressLine2;
            set
            {
                _businessAddressLine2 = value;
                OnPropertyChanged();
            }
        }

        private string _businessPhone = string.Empty;
        public string BusinessPhone
        {
            get => _businessPhone;
            set
            {
                _businessPhone = value;
                OnPropertyChanged();
            }
        }

        private string _businessTaxId = string.Empty;
        public string BusinessTaxId
        {
            get => _businessTaxId;
            set
            {
                _businessTaxId = value;
                OnPropertyChanged();
            }
        }

        private string _receiptFooterMessage = string.Empty;
        public string ReceiptFooterMessage
        {
            get => _receiptFooterMessage;
            set
            {
                _receiptFooterMessage = value;
                OnPropertyChanged();
            }
        }

        private string _selectedReceiptPrinter = string.Empty;
        public string SelectedReceiptPrinter
        {
            get => _selectedReceiptPrinter;
            set
            {
                _selectedReceiptPrinter = value;
                OnPropertyChanged();
            }
        }

        private bool _autoPrintReceipt = true;
        public bool AutoPrintReceipt
        {
            get => _autoPrintReceipt;
            set
            {
                _autoPrintReceipt = value;
                OnPropertyChanged();
            }
        }

        private bool _taxEnabled = false;
        public bool TaxEnabled
        {
            get => _taxEnabled;
            set
            {
                _taxEnabled = value;
                OnPropertyChanged();
            }
        }

        private decimal _taxRate = 0.0m;
        public string TaxRateDisplay
        {
            get => (_taxRate * 100).ToString("F2");
            set
            {
                if (decimal.TryParse(value, out decimal rate))
                {
                    _taxRate = rate / 100;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TaxRate));
                }
            }
        }

        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TaxRateDisplay));
            }
        }

        private string _taxLabel = "Tax";
        public string TaxLabel
        {
            get => _taxLabel;
            set
            {
                _taxLabel = value;
                OnPropertyChanged();
            }
        }

        private bool _isFetchingFromForte = false;
        public bool IsFetchingFromForte
        {
            get => _isFetchingFromForte;
            set
            {
                _isFetchingFromForte = value;
                OnPropertyChanged();
            }
        }

        private string _forteStoreInfoStatus = string.Empty;
        public string ForteStoreInfoStatus
        {
            get => _forteStoreInfoStatus;
            set
            {
                _forteStoreInfoStatus = value;
                OnPropertyChanged();
            }
        }

        // Card Reader Test Properties
        private bool _isTestingCardReader = false;
        public bool IsTestingCardReader
        {
            get => _isTestingCardReader;
            set
            {
                _isTestingCardReader = value;
                OnPropertyChanged();
            }
        }

        private string _cardReaderTestStatus = string.Empty;
        public string CardReaderTestStatus
        {
            get => _cardReaderTestStatus;
            set
            {
                _cardReaderTestStatus = value;
                OnPropertyChanged();
            }
        }

        private bool _hasCardReaderTestStatus = false;
        public bool HasCardReaderTestStatus
        {
            get => _hasCardReaderTestStatus;
            set
            {
                _hasCardReaderTestStatus = value;
                OnPropertyChanged();
            }
        }

        private bool _isCardReaderTestSuccess = false;
        public bool IsCardReaderTestSuccess
        {
            get => _isCardReaderTestSuccess;
            set
            {
                _isCardReaderTestSuccess = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Card reader test logs
        /// </summary>
        public ObservableCollection<string> CardReaderTestLogs { get; } = new();

        /// <summary>
        /// Sales ViewModel for sales dashboard
        /// </summary>
        public SalesViewModel SalesViewModel { get; }

        public AdminViewModel(ConfigurationService configService, Action? onNavigateBack = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _passwordService = new PasswordService();
            _networkScanner = new NetworkScannerService();
            _printerScanner = new PrinterScannerService();
            _onNavigateBack = onNavigateBack;

            // Initialize ForteCheckoutService
            try
            {
                _forteCheckoutService = new ForteCheckoutService(_configService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminViewModel] Failed to initialize ForteCheckoutService: {ex.Message}");
                _forteCheckoutService = null;
            }

            // Initialize SalesViewModel
            SalesViewModel = new SalesViewModel();

            AddSystemCommand = new RelayCommand(OnAddSystem);
            RemoveSystemCommand = new RelayCommand<SystemConfigurationViewModel>(OnRemoveSystem);
            ScanNetworkCommand = new RelayCommand(async () => await OnScanNetworkAsync());
            ScanPrintersCommand = new RelayCommand(async () => await OnScanPrintersAsync());
            AddDiscoveredDeviceCommand = new RelayCommand<NetworkScannerService.DiscoveredDevice>(OnAddDiscoveredDevice);
            TestPrinterCommand = new RelayCommand<PrinterScannerService.DiscoveredPrinter>(OnTestPrinter);
            SaveCommand = new RelayCommand(OnSave);
            SaveAndExitCommand = new RelayCommand(OnSaveAndExit);
            CancelCommand = new RelayCommand(OnCancel);
            ExitCommand = new RelayCommand(OnExit);
            ChangePasswordCommand = new RelayCommand(OnChangePassword);
            SelectSectionCommand = new RelayCommand<string>(OnSelectSection);
            TestPaymentCommand = new RelayCommand(async () => await OnTestPaymentAsync());
            TestRestApiCommand = new RelayCommand(async () => await OnTestRestApiAsync());
            TestCardReaderCommand = new RelayCommand(async () => await OnTestCardReaderAsync());
            FetchFromForteCommand = new RelayCommand(async () => await OnFetchFromForteAsync());
            SelectReceiptPrinterCommand = new RelayCommand<string>(OnSelectReceiptPrinter);

            InitializeConfigSections();
            LoadConfiguration();
        }

        private void InitializeConfigSections()
        {
            ConfigSections.Add(new ConfigSection
            {
                SectionId = "Systems",
                DisplayName = "Systems",
                Icon = "🖥️",
                IsSelected = true
            });

            ConfigSections.Add(new ConfigSection
            {
                SectionId = "Sales",
                DisplayName = "Sales",
                Icon = "📊",
                IsSelected = false
            });

            ConfigSections.Add(new ConfigSection
            {
                SectionId = "BreakTime",
                DisplayName = "Pause",
                Icon = "☕",
                IsSelected = false
            });

            ConfigSections.Add(new ConfigSection
            {
                SectionId = "Payment",
                DisplayName = "Payment",
                Icon = "💳",
                IsSelected = false
            });

            ConfigSections.Add(new ConfigSection
            {
                SectionId = "Security",
                DisplayName = "Security",
                Icon = "🔒",
                IsSelected = false
            });
        }

        private void OnSelectSection(string? sectionId)
        {
            if (string.IsNullOrEmpty(sectionId)) return;

            Console.WriteLine($"[AdminViewModel] Selecting section: {sectionId}");

            // Update selected state
            foreach (var section in ConfigSections)
            {
                section.IsSelected = section.SectionId == sectionId;
            }

            SelectedSectionId = sectionId;
        }

        private void UpdateSectionVisibility()
        {
            IsSystemsSectionVisible = SelectedSectionId == "Systems";
            IsSalesSectionVisible = SelectedSectionId == "Sales";
            IsBreakTimeSectionVisible = SelectedSectionId == "BreakTime";
            IsPaymentSectionVisible = SelectedSectionId == "Payment";
            IsSecuritySectionVisible = SelectedSectionId == "Security";
            IsReceiptSectionVisible = SelectedSectionId == "Receipt";

            Console.WriteLine($"[AdminViewModel] Section visibility updated: {SelectedSectionId}");
        }

        private void LoadConfiguration()
        {
            var config = _configService.GetConfiguration();

            DllServerPort = config.DllServerPort;

            // Load break time settings
            IsBreakTimeEnabled = config.IsBreakTimeEnabled;
            IsInstantBreakActive = config.IsInstantBreakActive;
            BreakStartHour = config.BreakStartHour;
            BreakStartMinute = config.BreakStartMinute;
            BreakEndHour = config.BreakEndHour;
            BreakEndMinute = config.BreakEndMinute;
            BreakMessage = config.BreakMessage;

            // Load Forte payment settings
            ForteApiAccessId = config.ForteApiAccessId;
            ForteApiSecureKey = config.ForteApiSecureKey;
            ForteOrganizationId = config.ForteOrganizationId;
            ForteLocationId = config.ForteLocationId;
            ForteSandboxMode = config.ForteSandboxMode;
            FortePaymentMode = config.FortePaymentMode;
            PaymentEnabled = config.PaymentEnabled;

            // Load terminal settings
            UseCardTerminal = config.UseCardTerminal;
            TerminalType = config.TerminalType;
            TerminalConnection = config.TerminalConnection;
            TerminalComPort = config.TerminalComPort;
            ForteMerchantId = config.ForteMerchantId;
            ForteProcessingPassword = config.ForteProcessingPassword;

            // Load receipt settings
            BusinessName = config.BusinessName;
            BusinessAddressLine1 = config.BusinessAddressLine1;
            BusinessAddressLine2 = config.BusinessAddressLine2;
            BusinessPhone = config.BusinessPhone;
            BusinessTaxId = config.BusinessTaxId;
            ReceiptFooterMessage = config.ReceiptFooterMessage;
            SelectedReceiptPrinter = config.SelectedReceiptPrinter;
            AutoPrintReceipt = config.AutoPrintReceipt;
            TaxEnabled = config.TaxEnabled;
            TaxRate = config.TaxRate;
            TaxLabel = config.TaxLabel;

            Systems.Clear();
            foreach (var system in config.Systems)
            {
                Systems.Add(new SystemConfigurationViewModel
                {
                    SystemName = system.SystemName,
                    IpAddress = system.IpAddress,
                    Port = system.Port,
                    IsEnabled = system.IsEnabled
                });
            }
        }

        private void OnAddSystem()
        {
            Systems.Add(new SystemConfigurationViewModel
            {
                SystemName = $"System{Systems.Count + 1}",
                IpAddress = "0.0.0.0",
                Port = 5000,
                IsEnabled = true
            });
        }

        private void OnRemoveSystem(SystemConfigurationViewModel? system)
        {
            if (system != null)
            {
                Systems.Remove(system);
            }
        }

        private async void OnSave()
        {
            IsSaving = true;
            try
            {
                await Task.Run(() =>
                {
                    // Build configuration from ViewModels
                    var config = new AppConfiguration
                    {
                        DllServerPort = DllServerPort,
                        IsBreakTimeEnabled = IsBreakTimeEnabled,
                        IsInstantBreakActive = IsInstantBreakActive,
                        BreakStartHour = BreakStartHour,
                        BreakStartMinute = BreakStartMinute,
                        BreakEndHour = BreakEndHour,
                        BreakEndMinute = BreakEndMinute,
                        BreakMessage = BreakMessage,
                        ForteApiAccessId = ForteApiAccessId,
                        ForteApiSecureKey = ForteApiSecureKey,
                        ForteOrganizationId = ForteOrganizationId,
                        ForteLocationId = ForteLocationId,
                        ForteSandboxMode = ForteSandboxMode,
                        FortePaymentMode = FortePaymentMode,
                        PaymentEnabled = PaymentEnabled,
                        UseCardTerminal = UseCardTerminal,
                        TerminalType = TerminalType,
                        TerminalConnection = TerminalConnection,
                        TerminalComPort = TerminalComPort,
                        ForteMerchantId = ForteMerchantId,
                        ForteProcessingPassword = ForteProcessingPassword,
                        BusinessName = BusinessName,
                        BusinessAddressLine1 = BusinessAddressLine1,
                        BusinessAddressLine2 = BusinessAddressLine2,
                        BusinessPhone = BusinessPhone,
                        BusinessTaxId = BusinessTaxId,
                        ReceiptFooterMessage = ReceiptFooterMessage,
                        SelectedReceiptPrinter = SelectedReceiptPrinter,
                        AutoPrintReceipt = AutoPrintReceipt,
                        TaxEnabled = TaxEnabled,
                        TaxRate = TaxRate,
                        TaxLabel = TaxLabel,
                        Systems = Systems.Select(vm => new SystemConfiguration
                        {
                            SystemName = vm.SystemName,
                            IpAddress = vm.IpAddress,
                            Port = vm.Port,
                            IsEnabled = vm.IsEnabled
                        }).ToList()
                    };

                    // Preserve password hash
                    var currentConfig = _configService.GetConfiguration();
                    config.AdminPasswordHash = currentConfig.AdminPasswordHash;

                    // Save configuration
                    bool success = _configService.SaveConfiguration(config);

                    if (success)
                    {
                        // TODO: Show success message to user
                        Console.WriteLine("[AdminViewModel] Configuration saved successfully");

                        // Reload systems with new configuration
                        Console.WriteLine("[AdminViewModel] Reloading systems...");
                        IPS.App.ReloadSystems();

                        // Stay on the page (do NOT navigate back)
                    }
                    else
                    {
                        // TODO: Show error message to user
                        Console.WriteLine("[AdminViewModel] Failed to save configuration");
                    }
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async void OnSaveAndExit()
        {
            IsSaving = true;
            try
            {
                await Task.Run(() =>
                {
                    // Build configuration from ViewModels
                    var config = new AppConfiguration
                    {
                        DllServerPort = DllServerPort,
                        IsBreakTimeEnabled = IsBreakTimeEnabled,
                        IsInstantBreakActive = IsInstantBreakActive,
                        BreakStartHour = BreakStartHour,
                        BreakStartMinute = BreakStartMinute,
                        BreakEndHour = BreakEndHour,
                        BreakEndMinute = BreakEndMinute,
                        BreakMessage = BreakMessage,
                        ForteApiAccessId = ForteApiAccessId,
                        ForteApiSecureKey = ForteApiSecureKey,
                        ForteOrganizationId = ForteOrganizationId,
                        ForteLocationId = ForteLocationId,
                        ForteSandboxMode = ForteSandboxMode,
                        FortePaymentMode = FortePaymentMode,
                        PaymentEnabled = PaymentEnabled,
                        UseCardTerminal = UseCardTerminal,
                        TerminalType = TerminalType,
                        TerminalConnection = TerminalConnection,
                        TerminalComPort = TerminalComPort,
                        ForteMerchantId = ForteMerchantId,
                        ForteProcessingPassword = ForteProcessingPassword,
                        BusinessName = BusinessName,
                        BusinessAddressLine1 = BusinessAddressLine1,
                        BusinessAddressLine2 = BusinessAddressLine2,
                        BusinessPhone = BusinessPhone,
                        BusinessTaxId = BusinessTaxId,
                        ReceiptFooterMessage = ReceiptFooterMessage,
                        SelectedReceiptPrinter = SelectedReceiptPrinter,
                        AutoPrintReceipt = AutoPrintReceipt,
                        TaxEnabled = TaxEnabled,
                        TaxRate = TaxRate,
                        TaxLabel = TaxLabel,
                        Systems = Systems.Select(vm => new SystemConfiguration
                        {
                            SystemName = vm.SystemName,
                            IpAddress = vm.IpAddress,
                            Port = vm.Port,
                            IsEnabled = vm.IsEnabled
                        }).ToList()
                    };

                    // Preserve password hash
                    var currentConfig = _configService.GetConfiguration();
                    config.AdminPasswordHash = currentConfig.AdminPasswordHash;

                    // Save configuration
                    bool success = _configService.SaveConfiguration(config);

                    if (success)
                    {
                        // TODO: Show success message to user
                        Console.WriteLine("[AdminViewModel] Configuration saved successfully");

                        // Reload systems with new configuration
                        Console.WriteLine("[AdminViewModel] Reloading systems...");
                        IPS.App.ReloadSystems();
                    }
                    else
                    {
                        // TODO: Show error message to user
                        Console.WriteLine("[AdminViewModel] Failed to save configuration");
                    }
                });

                // Navigate back after saving (on UI thread)
                _onNavigateBack?.Invoke();
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void OnCancel()
        {
            _onNavigateBack?.Invoke();
        }

        private void OnExit()
        {
            Console.WriteLine("[AdminViewModel] Exit requested - shutting down application");
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Scan local network for unmanned system devices
        /// </summary>
        private async Task OnScanNetworkAsync()
        {
            if (IsScanning) return;

            IsScanning = true;
            ScanProgress = 0;
            ScanStatus = "Scanning network...";
            DiscoveredDevices.Clear();

            try
            {
                Console.WriteLine("[AdminViewModel] Starting network scan...");

                var progress = new Progress<int>(percent =>
                {
                    ScanProgress = percent;
                    ScanStatus = $"Scanning network... {percent}%";
                });

                // Scan network (range 1-254, common unmanned system ports)
                var devices = await _networkScanner.ScanNetworkAsync(
                    startRange: 1,
                    endRange: 254,
                    portsToScan: new[] { 5000, 5001, 8080, 8081, 9000 },
                    progress: progress
                );

                // Add discovered devices to collection
                foreach (var device in devices)
                {
                    DiscoveredDevices.Add(device);
                }

                ScanStatus = $"Scan complete. Found {devices.Count} device(s)";
                Console.WriteLine($"[AdminViewModel] Network scan complete. Found {devices.Count} device(s)");
            }
            catch (Exception ex)
            {
                ScanStatus = $"Scan failed: {ex.Message}";
                Console.WriteLine($"[AdminViewModel] Network scan failed: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Scan for available receipt printers with progress reporting
        /// </summary>
        private async Task OnScanPrintersAsync()
        {
            if (IsPrinterScanning) return;

            IsPrinterScanning = true;
            PrinterScanProgress = 0;
            PrinterScanStatus = "Scanning for printers...";
            AvailablePrinters.Clear();

            try
            {
                Console.WriteLine("[AdminViewModel] Starting printer scan...");

                var progress = new Progress<int>(percent =>
                {
                    PrinterScanProgress = percent;
                    PrinterScanStatus = $"Scanning for printers... {percent}%";
                });

                var printers = await _printerScanner.GetReceiptPrintersAsync(progress);

                foreach (var printer in printers)
                {
                    AvailablePrinters.Add(printer);
                }

                // Set default printer if available
                var defaultPrinter = printers.FirstOrDefault(p => p.IsDefault);
                if (defaultPrinter != null)
                {
                    SelectedPrinter = defaultPrinter.PrinterName;
                }

                PrinterScanStatus = $"Scan complete. Found {printers.Count} printer(s)";
                Console.WriteLine($"[AdminViewModel] Printer scan complete. Found {printers.Count} printer(s)");
            }
            catch (Exception ex)
            {
                PrinterScanStatus = $"Printer scan failed: {ex.Message}";
                Console.WriteLine($"[AdminViewModel] Printer scan failed: {ex.Message}");
            }
            finally
            {
                IsPrinterScanning = false;
            }
        }

        /// <summary>
        /// Add discovered device to configuration
        /// </summary>
        private void OnAddDiscoveredDevice(NetworkScannerService.DiscoveredDevice? device)
        {
            if (device == null) return;

            // Check if device already exists
            var existing = Systems.FirstOrDefault(s =>
                s.IpAddress == device.IpAddress && s.Port == device.Port);

            if (existing != null)
            {
                Console.WriteLine($"[AdminViewModel] Device {device.IpAddress}:{device.Port} already in configuration");
                return;
            }

            // Add new system
            Systems.Add(new SystemConfigurationViewModel
            {
                SystemName = device.DeviceType,
                IpAddress = device.IpAddress,
                Port = device.Port,
                IsEnabled = true
            });

            Console.WriteLine($"[AdminViewModel] Added device: {device.IpAddress}:{device.Port} ({device.DeviceType})");
        }

        /// <summary>
        /// Test printer by sending a test print with actual receipt format
        /// </summary>
        private void OnTestPrinter(PrinterScannerService.DiscoveredPrinter? printer)
        {
            if (printer == null) return;

            PrinterScanStatus = $"Testing printer '{printer.PrinterName}'...";

            Task.Run(() =>
            {
                // Use PrintingService to print with actual receipt format
                var printingService = new PrintingService(_configService);
                bool success = printingService.PrintTestReceipt(printer.PrinterName);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        PrinterScanStatus = $"Test receipt sent to '{printer.PrinterName}'";
                        SelectedPrinter = printer.PrinterName;
                    }
                    else
                    {
                        PrinterScanStatus = $"Test print failed for '{printer.PrinterName}'";
                    }
                });
            });
        }

        /// <summary>
        /// Test payment integration with Forte Checkout by actually loading it in WebView2
        /// This captures real Forte server validation errors
        /// </summary>
        private async Task OnTestPaymentAsync()
        {
            if (IsTestingPayment) return;

            Console.WriteLine("[AdminViewModel] Payment test requested");

            // Clear previous messages and logs
            HasPaymentTestStatus = false;
            PaymentTestStatus = string.Empty;
            IsPaymentTestSuccess = false;
            PaymentTestLogs.Clear();
            PaymentTestHtml = string.Empty;
            IsTestingPayment = true;

            // Cancel any previous test
            _paymentTestCancellation?.Cancel();
            _paymentTestCancellation = new System.Threading.CancellationTokenSource();

            try
            {
                if (_forteCheckoutService == null)
                {
                    HasPaymentTestStatus = true;
                    IsPaymentTestSuccess = false;
                    PaymentTestStatus = "Payment service not initialized. Please check Forte credentials.";
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: ForteCheckoutService is null");
                    Console.WriteLine("[AdminViewModel] ForteCheckoutService is null");
                    return;
                }

                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Starting payment test...");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] NOTE: This test loads actual Forte Checkout in WebView2");
                PaymentTestLogs.Add("");

                // Run basic validation first
                var (basicSuccess, logs) = await _forteCheckoutService.TestCredentialsAsync();

                // Add all logs to the UI
                foreach (var log in logs)
                {
                    PaymentTestLogs.Add(log);
                }

                if (!basicSuccess)
                {
                    // Basic validation failed
                    HasPaymentTestStatus = true;
                    IsPaymentTestSuccess = false;
                    PaymentTestStatus = "❌ Payment test failed. Check the detailed logs below for error information.";
                    Console.WriteLine($"[AdminViewModel] Payment test failed at basic validation");
                    return;
                }

                // Step 5: Load Forte Checkout in WebView2 to catch server-side validation errors
                PaymentTestLogs.Add("");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Step 5: Testing Forte server-side signature validation...");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Loading Forte Checkout in hidden WebView2...");

                decimal testAmount = 1.00m;
                string testOrderLabel = $"TEST-{DateTime.Now:HHmmss}";

                string checkoutHtml = await _forteCheckoutService.GetCheckoutHtmlAsync(testAmount, testOrderLabel);

                // Set up completion source
                _paymentTestCompletion = new TaskCompletionSource<(bool success, string message)>();

                // Load HTML in WebView2
                PaymentTestHtml = checkoutHtml;

                // Wait for Forte Checkout to initialize (max 30 seconds)
                // Forte Checkout can take 10-20 seconds to load, especially on first run
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Waiting for Forte Checkout initialization (max 30 seconds)...");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] NOTE: Forte Checkout may take 10-20 seconds to load");

                var startTime = DateTime.Now;
                var progressTimer = new System.Timers.Timer(5000); // Update every 5 seconds
                progressTimer.Elapsed += (s, e) =>
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Still waiting... ({elapsed:F0}s elapsed)");
                    });
                };
                progressTimer.Start();

                var timeoutTask = Task.Delay(30000, _paymentTestCancellation.Token);
                var completedTask = await Task.WhenAny(_paymentTestCompletion.Task, timeoutTask);

                progressTimer.Stop();
                progressTimer.Dispose();

                if (completedTask == timeoutTask)
                {
                    // Timeout
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ TIMEOUT: No response from Forte Checkout after 30 seconds");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] This may indicate:");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - Network connectivity issues");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - Forte server is slow/unavailable");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - JavaScript errors in checkout page");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Try running the test again - first load is often slower");

                    HasPaymentTestStatus = true;
                    IsPaymentTestSuccess = false;
                    PaymentTestStatus = "⚠️ Test timed out after 30 seconds. Try again.";
                }
                else
                {
                    // Got response from WebView2
                    var (success, message) = _paymentTestCompletion.Task.Result;
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;

                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Forte Checkout response received after {elapsed:F1}s");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Response: {message}");

                    if (success)
                    {
                        PaymentTestLogs.Add("");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ ALL TESTS PASSED!");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Forte server validated your credentials successfully.");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Payment integration is fully operational.");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Total test time: {elapsed:F1} seconds");

                        HasPaymentTestStatus = true;
                        IsPaymentTestSuccess = true;
                        PaymentTestStatus = $"✅ Payment integration is working! (tested in {elapsed:F1}s)";
                    }
                    else
                    {
                        PaymentTestLogs.Add("");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Forte server rejected the credentials");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] This is the actual error users would see in PaymentView");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Test completed in {elapsed:F1} seconds");

                        HasPaymentTestStatus = true;
                        IsPaymentTestSuccess = false;
                        PaymentTestStatus = "❌ Forte server rejected credentials. Check logs for details.";
                    }
                }

                // Clear WebView2
                PaymentTestHtml = string.Empty;
            }
            catch (Exception ex)
            {
                HasPaymentTestStatus = true;
                IsPaymentTestSuccess = false;
                PaymentTestStatus = $"❌ Unexpected error during test: {ex.Message}";
                PaymentTestLogs.Add("");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ UNEXPECTED ERROR: {ex.GetType().Name}");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Message: {ex.Message}");
                Console.WriteLine($"[AdminViewModel] Payment test error: {ex.Message}");
            }
            finally
            {
                IsTestingPayment = false;
                _paymentTestCancellation?.Dispose();
                _paymentTestCancellation = null;
            }
        }

        /// <summary>
        /// Test REST API connection by calling Forte API directly
        /// </summary>
        private async Task OnTestRestApiAsync()
        {
            if (IsTestingPayment) return;

            Console.WriteLine("[AdminViewModel] REST API test requested");

            // Clear previous messages and logs
            HasPaymentTestStatus = false;
            PaymentTestStatus = string.Empty;
            IsPaymentTestSuccess = false;
            PaymentTestLogs.Clear();
            IsTestingPayment = true;

            try
            {
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] FORTE REST API CONNECTION TEST");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                PaymentTestLogs.Add("");

                // Create a temporary FortePaymentService instance
                var paymentService = new Services.FortePaymentService(_configService);

                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Testing connection to Forte REST API...");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Mode: {(ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Organization ID: {ForteOrganizationId}");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Location ID: {ForteLocationId}");
                PaymentTestLogs.Add("");

                // Test connection
                var startTime = DateTime.Now;
                var connectionSuccess = await paymentService.TestConnectionAsync();
                var elapsed = (DateTime.Now - startTime).TotalSeconds;

                if (connectionSuccess)
                {
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Connection successful ({elapsed:F2}s)");
                    PaymentTestLogs.Add("");

                    // Fetch organization info
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Fetching organization info...");
                    var orgInfo = await paymentService.GetOrganizationInfoAsync();
                    if (orgInfo != null)
                    {
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Organization: {orgInfo.OrganizationName}");
                    }
                    else
                    {
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ Could not fetch organization info");
                    }

                    // Fetch location info
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Fetching location info...");
                    var locInfo = await paymentService.GetLocationInfoAsync();
                    if (locInfo != null)
                    {
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Location: {locInfo.DbaName}");
                    }
                    else
                    {
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ Could not fetch location info");
                    }

                    PaymentTestLogs.Add("");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ REST API CONNECTION SUCCESSFUL");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");

                    HasPaymentTestStatus = true;
                    IsPaymentTestSuccess = true;
                    PaymentTestStatus = $"✅ REST API connection successful! ({elapsed:F1}s)";
                }
                else
                {
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ Connection failed ({elapsed:F2}s)");
                    PaymentTestLogs.Add("");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Please check:");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - API Access ID and Secure Key are correct");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - Organization ID is correct");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   - Network connectivity");

                    HasPaymentTestStatus = true;
                    IsPaymentTestSuccess = false;
                    PaymentTestStatus = "❌ REST API connection failed. Check credentials.";
                }
            }
            catch (Exception ex)
            {
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: {ex.Message}");
                HasPaymentTestStatus = true;
                IsPaymentTestSuccess = false;
                PaymentTestStatus = $"❌ Error: {ex.Message}";
                Console.WriteLine($"[AdminViewModel] REST API test error: {ex.Message}");
            }
            finally
            {
                IsTestingPayment = false;
            }
        }

        /// <summary>
        /// Test card reader (Dynaflex) - full test including beep and card reading
        /// </summary>
        private async Task OnTestCardReaderAsync()
        {
            if (IsTestingCardReader) return;

            Console.WriteLine("[AdminViewModel] Card reader full test requested");

            // Clear previous messages and logs
            HasCardReaderTestStatus = false;
            CardReaderTestStatus = string.Empty;
            IsCardReaderTestSuccess = false;
            CardReaderTestLogs.Clear();
            IsTestingCardReader = true;

            try
            {
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] DYNAFLEX CARD READER FULL TEST");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                CardReaderTestLogs.Add("");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] This test will:");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   1. Scan for USB HID devices");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   2. Connect to Dynaflex card reader");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   3. Send beep command");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   4. Start card reading transaction");
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   5. Wait for card insertion (30 seconds)");
                CardReaderTestLogs.Add("");

                // Create DynaflexService
                using var dynaflexService = new DynaflexService();

                // Subscribe to log messages
                dynaflexService.LogMessage += (sender, message) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CardReaderTestLogs.Add(message);
                    });
                };

                // Use the full TestCardReaderAsync method
                var result = await dynaflexService.TestCardReaderAsync(timeoutSeconds: 30);

                // Add all logs from the result
                foreach (var log in result.Logs)
                {
                    CardReaderTestLogs.Add(log);
                }

                if (result.Success)
                {
                    CardReaderTestLogs.Add("");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ FULL CARD READER TEST PASSED");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Device connected, beep confirmed, card read successfully!");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] The card reader is fully operational.");

                    if (result.CardData != null)
                    {
                        CardReaderTestLogs.Add("");
                        CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Card data captured:");
                        CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   • Type: {result.CardData.CardType}");
                        if (!string.IsNullOrEmpty(result.CardData.CardLast4))
                        {
                            CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   • Last 4: ****{result.CardData.CardLast4}");
                        }
                        if (!string.IsNullOrEmpty(result.CardData.KSN))
                        {
                            CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}]   • KSN: {result.CardData.KSN.Substring(0, Math.Min(16, result.CardData.KSN.Length))}...");
                        }
                    }

                    HasCardReaderTestStatus = true;
                    IsCardReaderTestSuccess = true;
                    CardReaderTestStatus = "✅ Full card reader test PASSED!";
                }
                else
                {
                    CardReaderTestLogs.Add("");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ CARD READER TEST: {result.ErrorMessage}");
                    CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");

                    if (result.ErrorMessage?.Contains("Timeout") == true)
                    {
                        CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] The device is connected and working,");
                        CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] but no card was inserted within 30 seconds.");
                        CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Run the test again and insert a card when prompted.");

                        HasCardReaderTestStatus = true;
                        IsCardReaderTestSuccess = false;
                        CardReaderTestStatus = "⚠️ No card inserted (timeout). Device is working.";
                    }
                    else
                    {
                        HasCardReaderTestStatus = true;
                        IsCardReaderTestSuccess = false;
                        CardReaderTestStatus = $"❌ {result.ErrorMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                CardReaderTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: {ex.Message}");
                HasCardReaderTestStatus = true;
                IsCardReaderTestSuccess = false;
                CardReaderTestStatus = $"❌ Error: {ex.Message}";
                Console.WriteLine($"[AdminViewModel] Card reader test error: {ex.Message}");
            }
            finally
            {
                IsTestingCardReader = false;
            }
        }

        /// <summary>
        /// Handle messages from payment test WebView2
        /// This is called from AdminView code-behind
        /// Same logic as PaymentView.CoreWebView2_WebMessageReceived
        /// </summary>
        public void HandlePaymentTestWebViewMessage(string json)
        {
            try
            {
                Console.WriteLine($"[AdminViewModel] Processing WebView2 message: {json}");

                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("[AdminViewModel] Empty message, ignoring");
                    return;
                }

                // Parse JSON message (same as PaymentView)
                var message = JsonSerializer.Deserialize<ForteCheckoutMessage>(json);

                if (message == null)
                {
                    Console.WriteLine("[AdminViewModel] Failed to parse message");
                    PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ Failed to parse message: {json}");
                    return;
                }

                // Handle different message types (same as PaymentView)
                switch (message.type?.ToLower())
                {
                    case "error":
                        // Forte Checkout reported an error (signature validation failed, etc.)
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: Forte Checkout reported error");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Error message: {message.error ?? "Unknown error"}");
                        _paymentTestCompletion?.TrySetResult((false, message.error ?? "Unknown error"));
                        break;

                    case "ready":
                        // Checkout page loaded and ready (signature validated successfully)
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ SUCCESS: Forte Checkout ready");
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] Forte server validated signature successfully");
                        _paymentTestCompletion?.TrySetResult((true, "Forte Checkout ready"));
                        break;

                    case "success":
                        // Payment completed (shouldn't happen in test, but handle it)
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ℹ️ Payment completed (Transaction: {message.transactionId})");
                        _paymentTestCompletion?.TrySetResult((true, "Payment completed"));
                        break;

                    case "failure":
                        // Payment failed
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ℹ️ Payment failed: {message.error}");
                        break;

                    case "cancel":
                        // Payment cancelled
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ℹ️ Payment cancelled");
                        break;

                    default:
                        // Other messages
                        PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ℹ️ WebView2 ({message.type}): {json}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminViewModel] Error processing WebView2 message: {ex.Message}");
                PaymentTestLogs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ Error processing message: {ex.Message}");
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

        /// <summary>
        /// Select a receipt printer from the discovered printers list
        /// </summary>
        private void OnSelectReceiptPrinter(string? printerName)
        {
            if (!string.IsNullOrEmpty(printerName))
            {
                SelectedReceiptPrinter = printerName;
                Console.WriteLine($"[AdminViewModel] Selected receipt printer: {printerName}");
            }
        }

        /// <summary>
        /// Fetch store information from Forte and auto-populate receipt settings
        /// </summary>
        private async Task OnFetchFromForteAsync()
        {
            try
            {
                IsFetchingFromForte = true;
                ForteStoreInfoStatus = "Fetching store information from Forte...";
                Console.WriteLine("[AdminViewModel] Fetching store info from Forte");

                // Create FortePaymentService
                var forteService = new FortePaymentService(_configService);

                // Fetch location info first (has more detailed business information)
                var locationInfo = await forteService.GetLocationInfoAsync();

                if (locationInfo != null)
                {
                    Console.WriteLine($"[AdminViewModel] Location info retrieved: {locationInfo.DbaName}");

                    // Populate business information from location
                    if (!string.IsNullOrWhiteSpace(locationInfo.DbaName))
                    {
                        BusinessName = locationInfo.DbaName;
                    }

                    if (!string.IsNullOrWhiteSpace(locationInfo.CustomerServicePhone))
                    {
                        BusinessPhone = locationInfo.CustomerServicePhone;
                    }

                    if (!string.IsNullOrWhiteSpace(locationInfo.TaxId))
                    {
                        BusinessTaxId = locationInfo.TaxId;
                    }

                    // Populate address
                    if (locationInfo.Address != null)
                    {
                        var addr = locationInfo.Address;

                        // Line 1: Street address
                        if (!string.IsNullOrWhiteSpace(addr.StreetLine1))
                        {
                            if (!string.IsNullOrWhiteSpace(addr.StreetLine2))
                            {
                                BusinessAddressLine1 = $"{addr.StreetLine1}, {addr.StreetLine2}";
                            }
                            else
                            {
                                BusinessAddressLine1 = addr.StreetLine1;
                            }
                        }

                        // Line 2: City, State ZIP
                        if (!string.IsNullOrWhiteSpace(addr.Locality) && !string.IsNullOrWhiteSpace(addr.Region))
                        {
                            BusinessAddressLine2 = $"{addr.Locality}, {addr.Region} {addr.PostalCode}";
                        }
                        else if (!string.IsNullOrWhiteSpace(addr.Locality))
                        {
                            BusinessAddressLine2 = $"{addr.Locality} {addr.PostalCode}";
                        }
                    }

                    ForteStoreInfoStatus = $"✅ Successfully loaded: {locationInfo.DbaName}";
                    Console.WriteLine("[AdminViewModel] Store info populated successfully");
                }
                else
                {
                    // Try organization info as fallback
                    Console.WriteLine("[AdminViewModel] Location info not available, trying organization info");
                    var orgInfo = await forteService.GetOrganizationInfoAsync();

                    if (orgInfo != null)
                    {
                        Console.WriteLine($"[AdminViewModel] Organization info retrieved: {orgInfo.OrganizationName}");

                        // Populate from organization
                        if (!string.IsNullOrWhiteSpace(orgInfo.DbaName))
                        {
                            BusinessName = orgInfo.DbaName;
                        }
                        else if (!string.IsNullOrWhiteSpace(orgInfo.CompanyName))
                        {
                            BusinessName = orgInfo.CompanyName;
                        }

                        if (!string.IsNullOrWhiteSpace(orgInfo.Phone))
                        {
                            BusinessPhone = orgInfo.Phone;
                        }

                        // Populate address
                        if (orgInfo.Address != null)
                        {
                            var addr = orgInfo.Address;

                            if (!string.IsNullOrWhiteSpace(addr.StreetLine1))
                            {
                                if (!string.IsNullOrWhiteSpace(addr.StreetLine2))
                                {
                                    BusinessAddressLine1 = $"{addr.StreetLine1}, {addr.StreetLine2}";
                                }
                                else
                                {
                                    BusinessAddressLine1 = addr.StreetLine1;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(addr.Locality) && !string.IsNullOrWhiteSpace(addr.Region))
                            {
                                BusinessAddressLine2 = $"{addr.Locality}, {addr.Region} {addr.PostalCode}";
                            }
                        }

                        ForteStoreInfoStatus = $"✅ Successfully loaded: {orgInfo.OrganizationName}";
                        Console.WriteLine("[AdminViewModel] Store info populated from organization");
                    }
                    else
                    {
                        ForteStoreInfoStatus = "❌ Failed to retrieve store information from Forte. Check your credentials.";
                        Console.WriteLine("[AdminViewModel] Failed to retrieve both location and organization info");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminViewModel] Error fetching from Forte: {ex.Message}");
                ForteStoreInfoStatus = $"❌ Error: {ex.Message}";
            }
            finally
            {
                IsFetchingFromForte = false;
            }
        }

        /// <summary>
        /// Handle PIN change request
        /// </summary>
        private void OnChangePassword()
        {
            Console.WriteLine("[AdminViewModel] PIN change requested");

            // Clear previous messages
            HasPasswordChangeMessage = false;
            PasswordChangeMessage = string.Empty;
            IsPasswordChangeSuccess = false;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                HasPasswordChangeMessage = true;
                PasswordChangeMessage = "Please enter your current PIN";
                Console.WriteLine("[AdminViewModel] Current PIN is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                HasPasswordChangeMessage = true;
                PasswordChangeMessage = "Please enter a new PIN";
                Console.WriteLine("[AdminViewModel] New PIN is empty");
                return;
            }

            if (NewPassword.Length < 4)
            {
                HasPasswordChangeMessage = true;
                PasswordChangeMessage = "New PIN must be at least 4 digits long";
                Console.WriteLine("[AdminViewModel] New PIN too short");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                HasPasswordChangeMessage = true;
                PasswordChangeMessage = "New PIN and confirmation do not match";
                Console.WriteLine("[AdminViewModel] PINs do not match");
                return;
            }

            // Verify current PIN
            var config = _configService.GetConfiguration();
            bool currentPasswordValid = _passwordService.VerifyPassword(CurrentPassword, config.AdminPasswordHash);

            if (!currentPasswordValid)
            {
                HasPasswordChangeMessage = true;
                PasswordChangeMessage = "Current PIN is incorrect";
                Console.WriteLine("[AdminViewModel] Current PIN is incorrect");
                return;
            }

            // Hash new PIN and save
            string newPasswordHash = _passwordService.HashPassword(NewPassword);
            config.AdminPasswordHash = newPasswordHash;

            bool saved = _configService.SaveConfiguration(config);

            if (saved)
            {
                HasPasswordChangeMessage = true;
                IsPasswordChangeSuccess = true;
                PasswordChangeMessage = "PIN changed successfully!";
                Console.WriteLine("[AdminViewModel] PIN changed successfully");

                // Clear PIN fields
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                HasPasswordChangeMessage = true;
                IsPasswordChangeSuccess = false;
                PasswordChangeMessage = "Failed to save new PIN. Please try again.";
                Console.WriteLine("[AdminViewModel] Failed to save new PIN");
            }
        }
    }

    /// <summary>
    /// ViewModel for a single system configuration
    /// </summary>
    public class SystemConfigurationViewModel : BaseViewModel
    {
        private string _systemName = string.Empty;
        private string _ipAddress = "127.0.0.1";
        private int _port = 5000;
        private bool _isEnabled = true;

        public string SystemName
        {
            get => _systemName;
            set
            {
                _systemName = value;
                OnPropertyChanged();
            }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged();
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }
}
