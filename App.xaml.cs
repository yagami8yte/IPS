using IPS.MainApp.ViewModels;
using IPS.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IPS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ConfigurationService? _configService;
        private SystemManagerService? _systemManager;
        private SystemPollingService? _pollingService;

        /// <summary>
        /// Reload all systems from current configuration
        /// Called when admin changes system configuration
        /// </summary>
        public static void ReloadSystems()
        {
            var app = (App)Current;
            if (app._systemManager != null && app._configService != null && app._pollingService != null)
            {
                Console.WriteLine("[App] Reloading systems from configuration...");

                // Clear existing systems
                app._systemManager.ClearSystems();

                // Re-register systems from configuration
                var config = app._configService.GetConfiguration();
                foreach (var systemConfig in config.Systems)
                {
                    if (!systemConfig.IsEnabled)
                        continue;

                    try
                    {
                        Console.WriteLine($"[App] Re-registering {systemConfig.SystemName} system");
                        var adapter = new Adapters.Coffee.CoffeeSystemAdapter(
                            systemName: systemConfig.SystemName,
                            boothId: $"booth_{systemConfig.SystemName}".ToLower(),
                            serverIp: "127.0.0.1",
                            serverPort: config.DllServerPort,
                            boothIp: systemConfig.IpAddress,
                            boothPort: systemConfig.Port
                        );

                        app._systemManager.RegisterSystem(adapter);
                        Console.WriteLine($"[App] ✓ Re-registered: {systemConfig.SystemName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[App] ✗ Failed to re-register {systemConfig.SystemName}: {ex.Message}");
                    }
                }

                // Force an immediate poll to update UI
                app._pollingService.PollNow();

                Console.WriteLine("[App] System reload complete");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Display startup banner
            Console.WriteLine("========================================");
            Console.WriteLine("  IPS - Integrated POS Solution");
            Console.WriteLine("  Multi-Kiosk Linkage System");
            Console.WriteLine("========================================");
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Initialize services
            Console.WriteLine("[App] Initializing services...");
            _configService = new ConfigurationService();
            _systemManager = new SystemManagerService();
            _pollingService = new SystemPollingService(_systemManager);

            Console.WriteLine("[App] Creating main window...");

            // Create and show main window with initialization callback
            var mainViewModel = new MainViewModel(
                _systemManager,
                _pollingService,
                _configService,
                InitializeSystemsAsync);

            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();

            Console.WriteLine("[App] Application window created.");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        private async Task<bool> InitializeSystemsAsync(IProgress<(string message, int progress)> progress)
        {
            await Task.Delay(100);

            Console.WriteLine("[App] Starting system initialization...");
            progress.Report(("Initializing systems...", 10));

            var config = _configService!.GetConfiguration();
            int totalSystems = config.Systems.Count(s => s.IsEnabled);
            int currentSystem = 0;
            int registeredCount = 0;

            Console.WriteLine($"[App] Found {totalSystems} enabled system(s) to initialize");

            // Register each enabled system from configuration
            foreach (var systemConfig in config.Systems)
            {
                if (!systemConfig.IsEnabled)
                {
                    Console.WriteLine($"[App] Skipping disabled system: {systemConfig.SystemName}");
                    continue;
                }

                currentSystem++;
                int progressPercent = 20 + (currentSystem * 60 / Math.Max(totalSystems, 1));

                progress.Report(($"Connecting to {systemConfig.SystemName}...", progressPercent));

                try
                {
                    // All systems currently use the CoffeeSystemAdapter (FollettoKioskApi.dll)
                    // SystemName is just a display name and can be anything the admin configures
                    // In the future, add a "SystemType" field to distinguish adapter types
                    Console.WriteLine($"[App] Initializing {systemConfig.SystemName} system:");
                    Console.WriteLine($"[App]   DLL Server: 127.0.0.1:{config.DllServerPort}");
                    Console.WriteLine($"[App]   Booth: {systemConfig.IpAddress}:{systemConfig.Port}");

                    var adapter = new Adapters.Coffee.CoffeeSystemAdapter(
                        systemName: systemConfig.SystemName,
                        boothId: $"booth_{systemConfig.SystemName}".ToLower(),
                        serverIp: "127.0.0.1",  // DLL internal server
                        serverPort: config.DllServerPort,
                        boothIp: systemConfig.IpAddress,  // Booth (coffee machine)
                        boothPort: systemConfig.Port
                    );

                    _systemManager!.RegisterSystem(adapter);
                    Console.WriteLine($"[App] ✓ Successfully registered: {systemConfig.SystemName}");
                    registeredCount++;

                    // Small delay to show progress
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[App] ✗ Failed to register {systemConfig.SystemName}: {ex.Message}");
                }
            }

            if (registeredCount == 0)
            {
                progress.Report(("No systems registered", 0));
                Console.WriteLine($"[App] ✗ No systems registered - initialization failed");
                return false;
            }

            Console.WriteLine($"[App] Registration complete: {registeredCount} system(s) registered");

            // Start polling service (must be called on UI thread for DispatcherTimer)
            progress.Report(("Starting polling service...", 85));
            Console.WriteLine("[App] Starting polling service on UI thread...");

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _pollingService!.StartPolling();
                Console.WriteLine("[App] Polling service started");
            });

            progress.Report(("Ready!", 100));
            Console.WriteLine("[App] ✓ Initialization complete - polling service active");

            await Task.Delay(300);
            return true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop polling service
            _pollingService?.StopPolling();

            base.OnExit(e);
        }
    }

}
