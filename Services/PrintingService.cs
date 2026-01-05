using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Service for printing receipts to thermal/POS printers using ESC/POS commands
    /// Supports Hwasung HMK-072 and other ESC/POS compatible thermal printers
    /// </summary>
    public class PrintingService : IPrintingService
    {
        private readonly ConfigurationService _configService;

        // ESC/POS Command Constants
        private static class EscPos
        {
            // Basic Commands
            public static readonly byte[] Initialize = { 0x1B, 0x40 };           // ESC @ - Initialize printer
            public static readonly byte[] LineFeed = { 0x0A };                    // LF - Line feed
            public static readonly byte[] CarriageReturn = { 0x0D };              // CR - Carriage return
            public static readonly byte[] FormFeed = { 0x0C };                    // FF - Form feed (page mode)

            // Text Formatting
            public static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };          // ESC E 1 - Bold on
            public static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };         // ESC E 0 - Bold off
            public static readonly byte[] UnderlineOn = { 0x1B, 0x2D, 0x01 };     // ESC - 1 - Underline on
            public static readonly byte[] UnderlineOff = { 0x1B, 0x2D, 0x00 };    // ESC - 0 - Underline off
            public static readonly byte[] DoubleHeightOn = { 0x1B, 0x21, 0x10 };  // ESC ! 16 - Double height
            public static readonly byte[] DoubleWidthOn = { 0x1B, 0x21, 0x20 };   // ESC ! 32 - Double width
            public static readonly byte[] DoubleOn = { 0x1B, 0x21, 0x30 };        // ESC ! 48 - Double height+width
            public static readonly byte[] NormalSize = { 0x1B, 0x21, 0x00 };      // ESC ! 0 - Normal size

            // GS ! n - Character size (n = 0x00 to 0x77)
            public static byte[] CharacterSize(int width, int height)
            {
                // width and height: 0-7 (0=normal, 1=2x, 2=3x, etc.)
                byte n = (byte)(((width & 0x07) << 4) | (height & 0x07));
                return new byte[] { 0x1D, 0x21, n };
            }

            // Alignment
            public static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };       // ESC a 0 - Align left
            public static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };     // ESC a 1 - Align center
            public static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };      // ESC a 2 - Align right

            // Paper Cutting
            public static readonly byte[] CutPaperFull = { 0x1D, 0x56, 0x00 };    // GS V 0 - Full cut
            public static readonly byte[] CutPaperPartial = { 0x1D, 0x56, 0x01 }; // GS V 1 - Partial cut
            public static readonly byte[] CutPaperFeedAndCut = { 0x1D, 0x56, 0x42, 0x00 }; // GS V 66 0 - Feed and full cut

            // Alternative cut commands (some printers use these)
            public static readonly byte[] CutAlt1 = { 0x1B, 0x69 };               // ESC i - Partial cut
            public static readonly byte[] CutAlt2 = { 0x1B, 0x6D };               // ESC m - Full cut

            // Line Spacing
            public static byte[] SetLineSpacing(byte n) => new byte[] { 0x1B, 0x33, n }; // ESC 3 n
            public static readonly byte[] DefaultLineSpacing = { 0x1B, 0x32 };    // ESC 2 - Default spacing

            // Character Set
            public static readonly byte[] CharsetPC437 = { 0x1B, 0x74, 0x00 };    // ESC t 0 - PC437 (USA)
            public static readonly byte[] CharsetPC850 = { 0x1B, 0x74, 0x02 };    // ESC t 2 - PC850 (Multilingual)

            // Feed lines
            public static byte[] FeedLines(byte n) => new byte[] { 0x1B, 0x64, n }; // ESC d n - Feed n lines

            // Cash drawer (if connected)
            public static readonly byte[] OpenCashDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC p 0 25 250
        }

        public PrintingService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>
        /// Print a receipt for a completed order using ESC/POS commands
        /// </summary>
        public bool PrintReceipt(OrderInfo order, System.Collections.ObjectModel.ObservableCollection<CartItem> cartItems, string? cardLast4Digits, string? authorizationCode, string? transactionId)
        {
            try
            {
                var config = _configService.GetConfiguration();

                // Check if auto-print is enabled
                if (!config.AutoPrintReceipt)
                {
                    Console.WriteLine("[PrintingService] Auto-print is disabled, skipping receipt");
                    return false;
                }

                // Check if printer is configured
                if (string.IsNullOrWhiteSpace(config.SelectedReceiptPrinter))
                {
                    Console.WriteLine("[PrintingService] No receipt printer configured, skipping receipt");
                    return false;
                }

                Console.WriteLine($"[PrintingService] Printing receipt to: {config.SelectedReceiptPrinter}");

                // Build ESC/POS receipt data
                using var ms = new MemoryStream();

                // Initialize printer
                WriteBytes(ms, EscPos.Initialize);
                WriteBytes(ms, EscPos.CharsetPC850); // Multilingual charset

                // Header - Business Information (centered, large)
                WriteBytes(ms, EscPos.AlignCenter);

                if (!string.IsNullOrWhiteSpace(config.BusinessName))
                {
                    WriteBytes(ms, EscPos.DoubleOn);
                    WriteBytes(ms, EscPos.BoldOn);
                    WriteLine(ms, config.BusinessName);
                    WriteBytes(ms, EscPos.BoldOff);
                    WriteBytes(ms, EscPos.NormalSize);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine1))
                {
                    WriteLine(ms, config.BusinessAddressLine1);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine2))
                {
                    WriteLine(ms, config.BusinessAddressLine2);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessPhone))
                {
                    WriteLine(ms, config.BusinessPhone);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessTaxId))
                {
                    WriteLine(ms, $"Tax ID: {config.BusinessTaxId}");
                }

                WriteSeparator(ms);

                // Order Information (left aligned)
                WriteBytes(ms, EscPos.AlignLeft);
                WriteBytes(ms, EscPos.BoldOn);
                WriteLine(ms, $"Order #: {order.OrderLabel ?? order.OrderId}");
                WriteBytes(ms, EscPos.BoldOff);
                WriteLine(ms, $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                WriteSeparator(ms);

                // Items Header
                WriteBytes(ms, EscPos.BoldOn);
                WriteLine(ms, "ITEMS:");
                WriteBytes(ms, EscPos.BoldOff);
                WriteSeparator(ms);

                decimal subtotal = 0;

                foreach (var cartItem in cartItems)
                {
                    // Item name and total price
                    string itemName = cartItem.Quantity > 1 ?
                        $"{cartItem.MenuItem.Name} x{cartItem.Quantity}" :
                        cartItem.MenuItem.Name;
                    string priceStr = $"${cartItem.TotalPrice:F2}";

                    WriteLine(ms, FormatLineItem(itemName, priceStr, 42));
                    subtotal += cartItem.TotalPrice;

                    // Selected options (if any)
                    if (cartItem.SelectedOptions != null && cartItem.SelectedOptions.Count > 0)
                    {
                        foreach (var option in cartItem.SelectedOptions)
                        {
                            WriteLine(ms, $"  + {option.Name}");
                        }
                    }
                }

                WriteSeparator(ms);

                // Totals
                WriteLine(ms, FormatLineItem("Subtotal:", $"${subtotal:F2}", 42));

                // Tax (if enabled)
                if (config.TaxEnabled && config.TaxRate > 0)
                {
                    decimal taxAmount = subtotal * config.TaxRate;
                    string taxLabel = config.TaxLabel ?? "Tax";
                    WriteLine(ms, FormatLineItem($"{taxLabel} ({config.TaxRate * 100:F2}%):", $"${taxAmount:F2}", 42));
                }

                WriteSeparator(ms);

                // Total (bold, larger)
                WriteBytes(ms, EscPos.BoldOn);
                WriteBytes(ms, EscPos.DoubleHeightOn);
                WriteLine(ms, FormatLineItem("TOTAL:", $"${order.TotalAmount:F2}", 42));
                WriteBytes(ms, EscPos.NormalSize);
                WriteBytes(ms, EscPos.BoldOff);

                WriteSeparator(ms);

                // Payment Information
                WriteBytes(ms, EscPos.BoldOn);
                WriteLine(ms, "PAYMENT:");
                WriteBytes(ms, EscPos.BoldOff);

                if (!string.IsNullOrWhiteSpace(cardLast4Digits))
                {
                    WriteLine(ms, $"Card: **** **** **** {cardLast4Digits}");
                    WriteLine(ms, "Status: APPROVED");
                }
                else
                {
                    WriteLine(ms, "Payment: $0.00 (No charge)");
                }

                if (!string.IsNullOrWhiteSpace(authorizationCode))
                {
                    WriteLine(ms, $"Auth Code: {authorizationCode}");
                }

                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    WriteLine(ms, $"Trans ID: {transactionId}");
                }

                WriteSeparator(ms);

                // Footer (centered)
                WriteBytes(ms, EscPos.AlignCenter);
                WriteBytes(ms, EscPos.LineFeed);

                if (!string.IsNullOrWhiteSpace(config.ReceiptFooterMessage))
                {
                    WriteBytes(ms, EscPos.BoldOn);
                    WriteLine(ms, config.ReceiptFooterMessage);
                    WriteBytes(ms, EscPos.BoldOff);
                }

                WriteLine(ms, "Thank you!");
                WriteSeparator(ms);

                // QR Code info (if applicable)
                if (!string.IsNullOrWhiteSpace(order.QrData))
                {
                    WriteBytes(ms, EscPos.LineFeed);
                    WriteLine(ms, "Order Ready Notification");
                    WriteLine(ms, $"QR: {order.QrData}");
                }

                // Feed and cut
                WriteBytes(ms, EscPos.FeedLines(4));
                WriteBytes(ms, EscPos.CutPaperPartial);

                // Send to printer
                byte[] receiptData = ms.ToArray();
                bool success = RawPrinterHelper.SendBytesToPrinter(config.SelectedReceiptPrinter, receiptData);

                if (success)
                {
                    Console.WriteLine($"[PrintingService] Receipt printed successfully ({receiptData.Length} bytes)");
                    Console.WriteLine($"[PrintingService] Order: {order.OrderLabel}, Total: ${order.TotalAmount:F2}");
                    return true;
                }
                else
                {
                    Console.WriteLine("[PrintingService] Failed to send data to printer");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintingService] Error printing receipt: {ex.Message}");
                Console.WriteLine($"[PrintingService] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Print a test receipt with sample data
        /// </summary>
        public bool PrintTestReceipt(string printerName)
        {
            try
            {
                var config = _configService.GetConfiguration();

                Console.WriteLine($"[PrintingService] Printing test receipt to: {printerName}");

                using var ms = new MemoryStream();

                // Initialize
                WriteBytes(ms, EscPos.Initialize);
                WriteBytes(ms, EscPos.CharsetPC850);

                // Header
                WriteBytes(ms, EscPos.AlignCenter);
                WriteBytes(ms, EscPos.DoubleOn);
                WriteBytes(ms, EscPos.BoldOn);

                if (!string.IsNullOrWhiteSpace(config.BusinessName))
                {
                    WriteLine(ms, config.BusinessName);
                }
                else
                {
                    WriteLine(ms, "TEST BUSINESS");
                }

                WriteBytes(ms, EscPos.BoldOff);
                WriteBytes(ms, EscPos.NormalSize);

                if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine1))
                {
                    WriteLine(ms, config.BusinessAddressLine1);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine2))
                {
                    WriteLine(ms, config.BusinessAddressLine2);
                }

                if (!string.IsNullOrWhiteSpace(config.BusinessPhone))
                {
                    WriteLine(ms, "Tel: " + config.BusinessPhone);
                }

                WriteSeparator(ms);

                WriteBytes(ms, EscPos.BoldOn);
                WriteLine(ms, "*** TEST RECEIPT ***");
                WriteBytes(ms, EscPos.BoldOff);

                WriteSeparator(ms);

                // Test order info
                WriteBytes(ms, EscPos.AlignLeft);
                WriteLine(ms, $"Order #: TEST");
                WriteLine(ms, $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                WriteSeparator(ms);

                // Test items
                WriteLine(ms, FormatLineItem("Test Coffee Latte", "$4.50", 42));
                WriteLine(ms, FormatLineItem("  2 x $4.50", "$9.00", 42));
                WriteLine(ms, FormatLineItem("Test Americano", "$3.00", 42));
                WriteLine(ms, FormatLineItem("  1 x $3.00", "$3.00", 42));

                WriteSeparator(ms);

                // Total
                WriteBytes(ms, EscPos.BoldOn);
                WriteLine(ms, FormatLineItem("TOTAL:", "$12.00", 42));
                WriteBytes(ms, EscPos.BoldOff);

                WriteSeparator(ms);

                // Payment
                WriteLine(ms, "Payment: TEST CARD ****1234");
                WriteLine(ms, "Auth Code: TEST123");

                WriteSeparator(ms);

                // Footer
                WriteBytes(ms, EscPos.AlignCenter);

                if (!string.IsNullOrWhiteSpace(config.ReceiptFooterMessage))
                {
                    WriteLine(ms, config.ReceiptFooterMessage);
                }
                else
                {
                    WriteLine(ms, "Thank you for your order!");
                }

                WriteBytes(ms, EscPos.LineFeed);
                WriteLine(ms, "=== ESC/POS TEST SUCCESSFUL ===");
                WriteLine(ms, $"Printer: {printerName}");
                WriteLine(ms, $"Bytes sent: will vary");

                // Feed and cut
                WriteBytes(ms, EscPos.FeedLines(4));
                WriteBytes(ms, EscPos.CutPaperPartial);

                byte[] testData = ms.ToArray();
                bool success = RawPrinterHelper.SendBytesToPrinter(printerName, testData);

                if (success)
                {
                    Console.WriteLine($"[PrintingService] Test receipt sent ({testData.Length} bytes)");
                    return true;
                }
                else
                {
                    Console.WriteLine("[PrintingService] Failed to send test receipt");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintingService] Error printing test receipt: {ex.Message}");
                return false;
            }
        }

        #region Helper Methods

        private void WriteBytes(MemoryStream ms, byte[] data)
        {
            ms.Write(data, 0, data.Length);
        }

        private void WriteLine(MemoryStream ms, string text)
        {
            // Convert to bytes (using code page 850 for multilingual support)
            byte[] textBytes = Encoding.GetEncoding(850).GetBytes(text);
            ms.Write(textBytes, 0, textBytes.Length);
            ms.Write(EscPos.LineFeed, 0, EscPos.LineFeed.Length);
        }

        private void WriteSeparator(MemoryStream ms)
        {
            WriteLine(ms, "==========================================");
        }

        /// <summary>
        /// Format a line with left and right aligned text
        /// </summary>
        private string FormatLineItem(string left, string right, int totalWidth)
        {
            int spaces = totalWidth - left.Length - right.Length;
            if (spaces < 1) spaces = 1;

            // Truncate left text if too long
            if (left.Length + right.Length + 1 > totalWidth)
            {
                int maxLeftLen = totalWidth - right.Length - 4; // Leave room for "..."
                if (maxLeftLen > 3)
                {
                    left = left.Substring(0, maxLeftLen) + "...";
                    spaces = 1;
                }
            }

            return left + new string(' ', spaces) + right;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for sending raw data to printers using Windows API
    /// </summary>
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string? pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string? pDataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Send raw bytes to a printer
        /// </summary>
        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            DOCINFOA di = new DOCINFOA();
            bool success = false;

            di.pDocName = "ESC/POS Receipt";
            di.pDataType = "RAW";

            try
            {
                Console.WriteLine($"[RawPrinter] Opening printer: {printerName}");

                if (!OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[RawPrinter] Failed to open printer. Error code: {error}");
                    return false;
                }

                Console.WriteLine($"[RawPrinter] Printer opened. Handle: {hPrinter}");

                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[RawPrinter] Failed to start document. Error code: {error}");
                    ClosePrinter(hPrinter);
                    return false;
                }

                Console.WriteLine("[RawPrinter] Document started");

                if (!StartPagePrinter(hPrinter))
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[RawPrinter] Failed to start page. Error code: {error}");
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                Console.WriteLine("[RawPrinter] Page started");

                // Allocate unmanaged memory and copy bytes
                IntPtr pBytes = Marshal.AllocCoTaskMem(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, pBytes, bytes.Length);

                    int written = 0;
                    success = WritePrinter(hPrinter, pBytes, bytes.Length, out written);

                    if (success)
                    {
                        Console.WriteLine($"[RawPrinter] Wrote {written} bytes to printer");
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        Console.WriteLine($"[RawPrinter] WritePrinter failed. Error code: {error}");
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pBytes);
                }

                EndPagePrinter(hPrinter);
                Console.WriteLine("[RawPrinter] Page ended");

                EndDocPrinter(hPrinter);
                Console.WriteLine("[RawPrinter] Document ended");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawPrinter] Exception: {ex.Message}");
                success = false;
            }
            finally
            {
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                    Console.WriteLine("[RawPrinter] Printer closed");
                }
            }

            return success;
        }

        /// <summary>
        /// Send a file to a printer
        /// </summary>
        public static bool SendFileToPrinter(string printerName, string fileName)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(fileName);
                return SendBytesToPrinter(printerName, bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawPrinter] Error reading file: {ex.Message}");
                return false;
            }
        }
    }
}
