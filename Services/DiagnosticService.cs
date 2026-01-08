using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace IPS.Services
{
    /// <summary>
    /// Service for capturing diagnostic logs and exporting them for debugging
    /// </summary>
    public static class DiagnosticService
    {
        private static readonly List<string> _logs = new List<string>();
        private static readonly object _lock = new object();
        private static TextWriter? _originalConsoleOut;
        private static LogCapturingTextWriter? _capturingWriter;
        private static bool _isCapturing = false;

        /// <summary>
        /// Start capturing console output
        /// </summary>
        public static void StartCapturing()
        {
            if (_isCapturing) return;

            lock (_lock)
            {
                _logs.Clear();
                _logs.Add($"=== IPS Diagnostic Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _logs.Add($"Machine: {Environment.MachineName}");
                _logs.Add($"OS: {Environment.OSVersion}");
                _logs.Add($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                _logs.Add($"64-bit Process: {Environment.Is64BitProcess}");
                _logs.Add($".NET Version: {Environment.Version}");
                _logs.Add($"Working Directory: {Environment.CurrentDirectory}");
                _logs.Add("===================================================");
                _logs.Add("");

                _originalConsoleOut = Console.Out;
                _capturingWriter = new LogCapturingTextWriter(_originalConsoleOut, AddLog);
                Console.SetOut(_capturingWriter);
                _isCapturing = true;
            }
        }

        /// <summary>
        /// Stop capturing console output
        /// </summary>
        public static void StopCapturing()
        {
            if (!_isCapturing) return;

            lock (_lock)
            {
                if (_originalConsoleOut != null)
                {
                    Console.SetOut(_originalConsoleOut);
                }
                _capturingWriter = null;
                _isCapturing = false;
            }
        }

        /// <summary>
        /// Add a log entry directly
        /// </summary>
        public static void AddLog(string message)
        {
            lock (_lock)
            {
                string timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                _logs.Add(timestamped);

                // Keep max 10000 lines to prevent memory issues
                while (_logs.Count > 10000)
                {
                    _logs.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Get all captured logs
        /// </summary>
        public static List<string> GetLogs()
        {
            lock (_lock)
            {
                return new List<string>(_logs);
            }
        }

        /// <summary>
        /// Export logs to a file on the Desktop
        /// </summary>
        public static string ExportToFile(string? additionalInfo = null)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"IPS_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(desktopPath, fileName);

                var sb = new StringBuilder();

                // Header
                sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║           IPS DIAGNOSTIC LOG EXPORT                          ║");
                sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
                sb.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Machine Name: {Environment.MachineName}");
                sb.AppendLine($"OS Version: {Environment.OSVersion}");
                sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
                sb.AppendLine($".NET Version: {Environment.Version}");
                sb.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
                sb.AppendLine();

                // Printer Info
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("INSTALLED PRINTERS:");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                try
                {
                    foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                    {
                        sb.AppendLine($"  - {printer}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  Error listing printers: {ex.Message}");
                }
                sb.AppendLine();

                // USB HID Devices
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("USB HID DEVICES (MagTek):");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                try
                {
                    var hidDevices = HidSharp.DeviceList.Local.GetHidDevices();
                    foreach (var device in hidDevices)
                    {
                        if (device.VendorID == 0x0801) // MagTek
                        {
                            string name = "Unknown";
                            try { name = device.GetProductName() ?? "Unknown"; } catch { }
                            sb.AppendLine($"  - {name}");
                            sb.AppendLine($"    VID: 0x{device.VendorID:X4}, PID: 0x{device.ProductID:X4}");
                            sb.AppendLine($"    Path: {device.DevicePath}");
                            try
                            {
                                sb.AppendLine($"    Max Input: {device.GetMaxInputReportLength()}");
                                sb.AppendLine($"    Max Output: {device.GetMaxOutputReportLength()}");
                                sb.AppendLine($"    Max Feature: {device.GetMaxFeatureReportLength()}");
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  Error listing HID devices: {ex.Message}");
                }
                sb.AppendLine();

                // Additional Info
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine("ADDITIONAL INFO:");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine(additionalInfo);
                    sb.AppendLine();
                }

                // Console Logs
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("CONSOLE LOGS:");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                lock (_lock)
                {
                    foreach (var log in _logs)
                    {
                        sb.AppendLine(log);
                    }
                }

                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("END OF DIAGNOSTIC LOG");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                return filePath;
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Custom TextWriter that captures output while still writing to original console
        /// </summary>
        private class LogCapturingTextWriter : TextWriter
        {
            private readonly TextWriter _originalWriter;
            private readonly Action<string> _logAction;
            private readonly StringBuilder _lineBuffer = new StringBuilder();

            public LogCapturingTextWriter(TextWriter originalWriter, Action<string> logAction)
            {
                _originalWriter = originalWriter;
                _logAction = logAction;
            }

            public override Encoding Encoding => _originalWriter.Encoding;

            public override void Write(char value)
            {
                _originalWriter.Write(value);

                if (value == '\n')
                {
                    string line = _lineBuffer.ToString().TrimEnd('\r');
                    if (!string.IsNullOrEmpty(line))
                    {
                        _logAction(line);
                    }
                    _lineBuffer.Clear();
                }
                else
                {
                    _lineBuffer.Append(value);
                }
            }

            public override void Write(string? value)
            {
                if (value == null) return;
                _originalWriter.Write(value);

                foreach (char c in value)
                {
                    if (c == '\n')
                    {
                        string line = _lineBuffer.ToString().TrimEnd('\r');
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logAction(line);
                        }
                        _lineBuffer.Clear();
                    }
                    else
                    {
                        _lineBuffer.Append(c);
                    }
                }
            }

            public override void WriteLine(string? value)
            {
                _originalWriter.WriteLine(value);
                _logAction(value ?? "");
            }

            public override void WriteLine()
            {
                _originalWriter.WriteLine();
            }
        }
    }
}
