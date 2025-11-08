using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        /// Indicates if network/printer scan is in progress
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
        /// Scan progress percentage (0-100)
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
        /// Status message for current scan operation
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
        /// Command to save configuration
        /// </summary>
        public IRelayCommand SaveCommand { get; }

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

        // Section visibility properties
        private bool _isSystemsSectionVisible = true;
        private bool _isSalesSectionVisible = false;
        private bool _isPrintersSectionVisible = false;
        private bool _isServerSectionVisible = false;
        private bool _isSecuritySectionVisible = false;

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

        public bool IsSecuritySectionVisible
        {
            get => _isSecuritySectionVisible;
            set
            {
                _isSecuritySectionVisible = value;
                OnPropertyChanged();
            }
        }

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

            // Initialize SalesViewModel
            SalesViewModel = new SalesViewModel();

            AddSystemCommand = new RelayCommand(OnAddSystem);
            RemoveSystemCommand = new RelayCommand<SystemConfigurationViewModel>(OnRemoveSystem);
            ScanNetworkCommand = new RelayCommand(async () => await OnScanNetworkAsync());
            ScanPrintersCommand = new RelayCommand(async () => await OnScanPrintersAsync());
            AddDiscoveredDeviceCommand = new RelayCommand<NetworkScannerService.DiscoveredDevice>(OnAddDiscoveredDevice);
            TestPrinterCommand = new RelayCommand<PrinterScannerService.DiscoveredPrinter>(OnTestPrinter);
            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(OnCancel);
            ExitCommand = new RelayCommand(OnExit);
            ChangePasswordCommand = new RelayCommand(OnChangePassword);
            SelectSectionCommand = new RelayCommand<string>(OnSelectSection);

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
            IsSecuritySectionVisible = SelectedSectionId == "Security";

            Console.WriteLine($"[AdminViewModel] Section visibility updated: {SelectedSectionId}");
        }

        private void LoadConfiguration()
        {
            var config = _configService.GetConfiguration();

            DllServerPort = config.DllServerPort;

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

        private void OnSave()
        {
            // Build configuration from ViewModels
            var config = new AppConfiguration
            {
                DllServerPort = DllServerPort,
                Systems = Systems.Select(vm => new SystemConfiguration
                {
                    SystemName = vm.SystemName,
                    IpAddress = vm.IpAddress,
                    Port = vm.Port,
                    IsEnabled = vm.IsEnabled
                }).ToList()
            };

            // Save configuration
            bool success = _configService.SaveConfiguration(config);

            if (success)
            {
                // TODO: Show success message to user
                Console.WriteLine("[AdminViewModel] Configuration saved successfully");

                // Reload systems with new configuration
                Console.WriteLine("[AdminViewModel] Reloading systems...");
                IPS.App.ReloadSystems();

                // Navigate back
                _onNavigateBack?.Invoke();
            }
            else
            {
                // TODO: Show error message to user
                Console.WriteLine("[AdminViewModel] Failed to save configuration");
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
            if (IsScanning) return;

            IsScanning = true;
            ScanProgress = 0;
            ScanStatus = "Scanning for printers...";
            AvailablePrinters.Clear();

            try
            {
                Console.WriteLine("[AdminViewModel] Starting printer scan...");

                var progress = new Progress<int>(percent =>
                {
                    ScanProgress = percent;
                    ScanStatus = $"Scanning for printers... {percent}%";
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

                ScanStatus = $"Scan complete. Found {printers.Count} printer(s)";
                Console.WriteLine($"[AdminViewModel] Printer scan complete. Found {printers.Count} printer(s)");
            }
            catch (Exception ex)
            {
                ScanStatus = $"Printer scan failed: {ex.Message}";
                Console.WriteLine($"[AdminViewModel] Printer scan failed: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
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
        /// Test printer by sending a test print
        /// </summary>
        private void OnTestPrinter(PrinterScannerService.DiscoveredPrinter? printer)
        {
            if (printer == null) return;

            ScanStatus = $"Testing printer '{printer.PrinterName}'...";

            Task.Run(() =>
            {
                bool success = _printerScanner.TestPrint(printer.PrinterName);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        ScanStatus = $"Test print sent to '{printer.PrinterName}'";
                        SelectedPrinter = printer.PrinterName;
                    }
                    else
                    {
                        ScanStatus = $"Test print failed for '{printer.PrinterName}'";
                    }
                });
            });
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
