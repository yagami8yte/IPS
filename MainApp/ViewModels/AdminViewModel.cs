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
        private readonly NetworkScannerService _networkScanner;
        private readonly PrinterScannerService _printerScanner;
        private readonly Action? _onNavigateBack;
        private int _dllServerPort;
        private bool _isScanning;
        private int _scanProgress;
        private string _scanStatus = string.Empty;
        private string _selectedPrinter = string.Empty;

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

        public AdminViewModel(ConfigurationService configService, Action? onNavigateBack = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _networkScanner = new NetworkScannerService();
            _printerScanner = new PrinterScannerService();
            _onNavigateBack = onNavigateBack;

            AddSystemCommand = new RelayCommand(OnAddSystem);
            RemoveSystemCommand = new RelayCommand<SystemConfigurationViewModel>(OnRemoveSystem);
            ScanNetworkCommand = new RelayCommand(async () => await OnScanNetworkAsync());
            ScanPrintersCommand = new RelayCommand(OnScanPrinters);
            AddDiscoveredDeviceCommand = new RelayCommand<NetworkScannerService.DiscoveredDevice>(OnAddDiscoveredDevice);
            TestPrinterCommand = new RelayCommand<PrinterScannerService.DiscoveredPrinter>(OnTestPrinter);
            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(OnCancel);
            ExitCommand = new RelayCommand(OnExit);

            LoadConfiguration();
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
        /// Scan for available receipt printers
        /// </summary>
        private void OnScanPrinters()
        {
            if (IsScanning) return;

            IsScanning = true;
            ScanStatus = "Scanning for printers...";
            AvailablePrinters.Clear();

            try
            {
                Console.WriteLine("[AdminViewModel] Starting printer scan...");

                var printers = _printerScanner.GetReceiptPrinters();

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

                ScanStatus = $"Found {printers.Count} printer(s)";
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
