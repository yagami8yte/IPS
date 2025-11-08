using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;

namespace IPS.Services
{
    /// <summary>
    /// Service for detecting available receipt printers on the system
    /// </summary>
    public class PrinterScannerService
    {
        /// <summary>
        /// Discovered printer information
        /// </summary>
        public class DiscoveredPrinter
        {
            public string PrinterName { get; set; } = "";
            public string PortName { get; set; } = "";
            public bool IsDefault { get; set; }
            public bool IsOnline { get; set; }
            public string DriverName { get; set; } = "";
            public string PrinterType { get; set; } = "";
        }

        /// <summary>
        /// Scan for all available printers on the system
        /// </summary>
        public List<DiscoveredPrinter> ScanPrinters()
        {
            var printers = new List<DiscoveredPrinter>();

            Console.WriteLine("[PrinterScanner] Scanning for installed printers...");

            try
            {
                // Get default printer name
                PrinterSettings defaultSettings = new PrinterSettings();
                string defaultPrinterName = defaultSettings.PrinterName;

                // Enumerate all installed printers
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var printer = new DiscoveredPrinter
                    {
                        PrinterName = printerName,
                        IsDefault = printerName.Equals(defaultPrinterName, StringComparison.OrdinalIgnoreCase)
                    };

                    // Try to get printer details
                    try
                    {
                        PrinterSettings settings = new PrinterSettings
                        {
                            PrinterName = printerName
                        };

                        printer.IsOnline = settings.IsValid;
                        printer.PortName = GetPrinterPort(printerName);
                        printer.DriverName = GetPrinterDriver(printerName);
                        printer.PrinterType = IdentifyPrinterType(printerName, printer.PortName);

                        if (printer.IsOnline)
                        {
                            Console.WriteLine($"[PrinterScanner] ✓ Found: {printerName} ({printer.PrinterType})");
                            if (printer.IsDefault)
                            {
                                Console.WriteLine($"[PrinterScanner]   - Default printer");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PrinterScanner] ✗ Error reading printer '{printerName}': {ex.Message}");
                        printer.IsOnline = false;
                    }

                    printers.Add(printer);
                }

                Console.WriteLine($"[PrinterScanner] Found {printers.Count} printer(s), {printers.Count(p => p.IsOnline)} online");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrinterScanner] Error scanning printers: {ex.Message}");
            }

            return printers;
        }

        /// <summary>
        /// Scan for printers asynchronously with progress reporting
        /// </summary>
        public async Task<List<DiscoveredPrinter>> ScanPrintersAsync(IProgress<int>? progress = null)
        {
            return await Task.Run(() =>
            {
                var printers = new List<DiscoveredPrinter>();

                Console.WriteLine("[PrinterScanner] Scanning for installed printers...");

                try
                {
                    // Get default printer name
                    PrinterSettings defaultSettings = new PrinterSettings();
                    string defaultPrinterName = defaultSettings.PrinterName;

                    // Get list of all printers
                    var installedPrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
                    int totalPrinters = installedPrinters.Count;

                    // Enumerate all installed printers with progress
                    for (int i = 0; i < installedPrinters.Count; i++)
                    {
                        string printerName = installedPrinters[i];

                        var printer = new DiscoveredPrinter
                        {
                            PrinterName = printerName,
                            IsDefault = printerName.Equals(defaultPrinterName, StringComparison.OrdinalIgnoreCase)
                        };

                        // Try to get printer details
                        try
                        {
                            PrinterSettings settings = new PrinterSettings
                            {
                                PrinterName = printerName
                            };

                            printer.IsOnline = settings.IsValid;
                            printer.PortName = GetPrinterPort(printerName);
                            printer.DriverName = GetPrinterDriver(printerName);
                            printer.PrinterType = IdentifyPrinterType(printerName, printer.PortName);

                            if (printer.IsOnline)
                            {
                                Console.WriteLine($"[PrinterScanner] ✓ Found: {printerName} ({printer.PrinterType})");
                                if (printer.IsDefault)
                                {
                                    Console.WriteLine($"[PrinterScanner]   - Default printer");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[PrinterScanner] ✗ Error reading printer '{printerName}': {ex.Message}");
                            printer.IsOnline = false;
                        }

                        printers.Add(printer);

                        // Report progress
                        int percentComplete = (int)(((i + 1) / (double)totalPrinters) * 100);
                        progress?.Report(percentComplete);
                    }

                    Console.WriteLine($"[PrinterScanner] Found {printers.Count} printer(s), {printers.Count(p => p.IsOnline)} online");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PrinterScanner] Error scanning printers: {ex.Message}");
                }

                return printers;
            });
        }

        /// <summary>
        /// Get receipt printers only (filter out regular document printers)
        /// </summary>
        public List<DiscoveredPrinter> GetReceiptPrinters()
        {
            var allPrinters = ScanPrinters();

            // Filter for receipt printers based on common naming patterns and types
            var receiptPrinters = allPrinters.Where(p =>
                p.PrinterName.ToLower().Contains("receipt") ||
                p.PrinterName.ToLower().Contains("pos") ||
                p.PrinterName.ToLower().Contains("thermal") ||
                p.PrinterName.ToLower().Contains("epson") && p.PrinterName.ToLower().Contains("tm") ||
                p.PrinterName.ToLower().Contains("star") ||
                p.PrinterName.ToLower().Contains("tsp") ||
                p.PrinterName.ToLower().Contains("bixolon") ||
                p.PortName.ToLower().Contains("usb") ||
                p.PortName.ToLower().Contains("com")
            ).ToList();

            if (receiptPrinters.Count > 0)
            {
                Console.WriteLine($"[PrinterScanner] Identified {receiptPrinters.Count} receipt printer(s)");
            }
            else
            {
                Console.WriteLine("[PrinterScanner] No receipt printers identified by name. Showing all printers.");
                return allPrinters;
            }

            return receiptPrinters;
        }

        /// <summary>
        /// Get receipt printers asynchronously with progress reporting
        /// </summary>
        public async Task<List<DiscoveredPrinter>> GetReceiptPrintersAsync(IProgress<int>? progress = null)
        {
            var allPrinters = await ScanPrintersAsync(progress);

            // Filter for receipt printers based on common naming patterns and types
            var receiptPrinters = allPrinters.Where(p =>
                p.PrinterName.ToLower().Contains("receipt") ||
                p.PrinterName.ToLower().Contains("pos") ||
                p.PrinterName.ToLower().Contains("thermal") ||
                p.PrinterName.ToLower().Contains("epson") && p.PrinterName.ToLower().Contains("tm") ||
                p.PrinterName.ToLower().Contains("star") ||
                p.PrinterName.ToLower().Contains("tsp") ||
                p.PrinterName.ToLower().Contains("bixolon") ||
                p.PortName.ToLower().Contains("usb") ||
                p.PortName.ToLower().Contains("com")
            ).ToList();

            if (receiptPrinters.Count > 0)
            {
                Console.WriteLine($"[PrinterScanner] Identified {receiptPrinters.Count} receipt printer(s)");
            }
            else
            {
                Console.WriteLine("[PrinterScanner] No receipt printers identified by name. Showing all printers.");
                return allPrinters;
            }

            return receiptPrinters;
        }

        /// <summary>
        /// Get printer port from Windows registry or system
        /// </summary>
        private string GetPrinterPort(string printerName)
        {
            try
            {
                // Try to get port information from WMI
                var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Printer WHERE Name = '{printerName.Replace("\\", "\\\\")}'");

                foreach (System.Management.ManagementObject printer in searcher.Get())
                {
                    var portName = printer["PortName"]?.ToString();
                    if (!string.IsNullOrEmpty(portName))
                    {
                        return portName;
                    }
                }
            }
            catch
            {
                // WMI not available or access denied
            }

            return "Unknown";
        }

        /// <summary>
        /// Get printer driver name
        /// </summary>
        private string GetPrinterDriver(string printerName)
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Printer WHERE Name = '{printerName.Replace("\\", "\\\\")}'");

                foreach (System.Management.ManagementObject printer in searcher.Get())
                {
                    var driverName = printer["DriverName"]?.ToString();
                    if (!string.IsNullOrEmpty(driverName))
                    {
                        return driverName;
                    }
                }
            }
            catch
            {
                // WMI not available
            }

            return "Unknown";
        }

        /// <summary>
        /// Identify printer type based on name and port
        /// </summary>
        private string IdentifyPrinterType(string printerName, string portName)
        {
            string nameLower = printerName.ToLower();
            string portLower = portName.ToLower();

            // Check for common receipt printer brands
            if (nameLower.Contains("epson") && nameLower.Contains("tm"))
                return "EPSON TM Series";
            if (nameLower.Contains("star") || nameLower.Contains("tsp"))
                return "Star TSP Series";
            if (nameLower.Contains("bixolon"))
                return "Bixolon Thermal";
            if (nameLower.Contains("citizen"))
                return "Citizen Thermal";

            // Check by connection type
            if (portLower.Contains("usb"))
                return "USB Printer";
            if (portLower.Contains("com"))
                return "Serial (COM) Printer";
            if (portLower.Contains("lpt"))
                return "Parallel (LPT) Printer";
            if (portLower.Contains("ip") || portLower.Contains("192.168") || portLower.Contains("10."))
                return "Network Printer";

            // Generic identification
            if (nameLower.Contains("receipt") || nameLower.Contains("thermal"))
                return "Receipt Printer";
            if (nameLower.Contains("pos"))
                return "POS Printer";

            return "Generic Printer";
        }

        /// <summary>
        /// Test print to verify printer is working
        /// </summary>
        public bool TestPrint(string printerName)
        {
            try
            {
                PrinterSettings settings = new PrinterSettings
                {
                    PrinterName = printerName
                };

                if (!settings.IsValid)
                {
                    Console.WriteLine($"[PrinterScanner] Printer '{printerName}' is not valid");
                    return false;
                }

                // Create a simple test print document
                PrintDocument doc = new PrintDocument();
                doc.PrinterSettings = settings;

                bool printSuccess = false;

                doc.PrintPage += (sender, e) =>
                {
                    // Simple test receipt
                    var font = new System.Drawing.Font("Courier New", 10);
                    float y = 10;

                    e.Graphics.DrawString("=============================", font, System.Drawing.Brushes.Black, 10, y);
                    y += 20;
                    e.Graphics.DrawString("  IPS TEST PRINT", font, System.Drawing.Brushes.Black, 10, y);
                    y += 20;
                    e.Graphics.DrawString("=============================", font, System.Drawing.Brushes.Black, 10, y);
                    y += 20;
                    e.Graphics.DrawString($"Printer: {printerName}", font, System.Drawing.Brushes.Black, 10, y);
                    y += 20;
                    e.Graphics.DrawString($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", font, System.Drawing.Brushes.Black, 10, y);
                    y += 20;
                    e.Graphics.DrawString("=============================", font, System.Drawing.Brushes.Black, 10, y);

                    printSuccess = true;
                };

                doc.Print();

                if (printSuccess)
                {
                    Console.WriteLine($"[PrinterScanner] ✓ Test print sent to '{printerName}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrinterScanner] ✗ Test print failed for '{printerName}': {ex.Message}");
            }

            return false;
        }
    }
}
