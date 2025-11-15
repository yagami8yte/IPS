using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Service for printing receipts to thermal/POS printers
    /// </summary>
    public class PrintingService : IPrintingService
    {
        private readonly ConfigurationService _configService;

        public PrintingService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>
        /// Print a receipt for a completed order
        /// Non-blocking - returns false if fails, but doesn't throw
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

                // Validate printer
                PrinterSettings settings = new PrinterSettings
                {
                    PrinterName = config.SelectedReceiptPrinter
                };

                if (!settings.IsValid)
                {
                    Console.WriteLine($"[PrintingService] Printer '{config.SelectedReceiptPrinter}' is not valid, skipping receipt");
                    return false;
                }

                // Create print document
                PrintDocument doc = new PrintDocument();
                doc.PrinterSettings = settings;

                bool printSuccess = false;

                doc.PrintPage += (sender, e) =>
                {
                    try
                    {
                        // Use monospace font for receipt (typical for POS printers)
                        var font = new Font("Courier New", 9, FontStyle.Regular);
                        var fontBold = new Font("Courier New", 9, FontStyle.Bold);
                        var fontLarge = new Font("Courier New", 12, FontStyle.Bold);
                        var brush = Brushes.Black;
                        float y = 10;
                        float lineHeight = 15;

                        // Helper to draw text
                        void DrawLine(string text, Font f = null)
                        {
                            e.Graphics.DrawString(text, f ?? font, brush, 10, y);
                            y += lineHeight;
                        }

                        void DrawSeparator()
                        {
                            DrawLine("========================================");
                        }

                        // Header - Business Information
                        if (!string.IsNullOrWhiteSpace(config.BusinessName))
                        {
                            DrawLine(CenterText(config.BusinessName, 40), fontLarge);
                        }

                        if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine1))
                        {
                            DrawLine(CenterText(config.BusinessAddressLine1, 40));
                        }

                        if (!string.IsNullOrWhiteSpace(config.BusinessAddressLine2))
                        {
                            DrawLine(CenterText(config.BusinessAddressLine2, 40));
                        }

                        if (!string.IsNullOrWhiteSpace(config.BusinessPhone))
                        {
                            DrawLine(CenterText(config.BusinessPhone, 40));
                        }

                        if (!string.IsNullOrWhiteSpace(config.BusinessTaxId))
                        {
                            DrawLine(CenterText($"Tax ID: {config.BusinessTaxId}", 40));
                        }

                        DrawSeparator();

                        // Order Information
                        DrawLine($"Order #: {order.OrderLabel ?? order.OrderId}", fontBold);
                        DrawLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        DrawSeparator();

                        // Items
                        DrawLine("ITEMS:", fontBold);
                        DrawSeparator();

                        decimal subtotal = 0;

                        foreach (var cartItem in cartItems)
                        {
                            // Item name and total price (price × quantity)
                            string itemLine = cartItem.Quantity > 1 ?
                                $"{cartItem.MenuItem.Name} x{cartItem.Quantity}" :
                                cartItem.MenuItem.Name;
                            string priceStr = $"${cartItem.TotalPrice:F2}";
                            DrawLine(PadRight(itemLine, priceStr, 40));

                            subtotal += cartItem.TotalPrice;

                            // Selected options (if any)
                            if (cartItem.SelectedOptions != null && cartItem.SelectedOptions.Count > 0)
                            {
                                foreach (var option in cartItem.SelectedOptions)
                                {
                                    DrawLine($"  + {option.Name}");
                                }
                            }
                        }

                        DrawSeparator();

                        // Totals
                        DrawLine(PadRight("Subtotal:", $"${subtotal:F2}", 40));

                        // Tax (if enabled)
                        if (config.TaxEnabled && config.TaxRate > 0)
                        {
                            decimal taxAmount = subtotal * config.TaxRate;
                            string taxLabel = config.TaxLabel ?? "Tax";
                            DrawLine(PadRight($"{taxLabel} ({config.TaxRate * 100:F2}%):", $"${taxAmount:F2}", 40));
                        }

                        DrawSeparator();
                        DrawLine(PadRight("TOTAL:", $"${order.TotalAmount:F2}", 40), fontBold);
                        DrawSeparator();

                        // Payment Information
                        DrawLine("PAYMENT:", fontBold);

                        if (!string.IsNullOrWhiteSpace(cardLast4Digits))
                        {
                            // PCI DSS Compliant - only last 4 digits
                            DrawLine($"Card: •••• {cardLast4Digits}");
                            DrawLine("Status: APPROVED");
                        }
                        else
                        {
                            DrawLine("Payment: $0.00 (No charge)");
                        }

                        if (!string.IsNullOrWhiteSpace(authorizationCode))
                        {
                            DrawLine($"Auth Code: {authorizationCode}");
                        }

                        if (!string.IsNullOrWhiteSpace(transactionId))
                        {
                            DrawLine($"Transaction ID: {transactionId}");
                        }

                        DrawSeparator();

                        // Footer
                        y += 10; // Extra space
                        if (!string.IsNullOrWhiteSpace(config.ReceiptFooterMessage))
                        {
                            DrawLine(CenterText(config.ReceiptFooterMessage, 40), fontBold);
                        }

                        DrawLine(CenterText("Thank you!", 40));
                        DrawSeparator();

                        // QR Code info (if applicable)
                        if (!string.IsNullOrWhiteSpace(order.QrData))
                        {
                            y += 10;
                            DrawLine(CenterText("Order Ready Notification", 40));
                            DrawLine(CenterText($"QR: {order.QrData}", 40));
                        }

                        printSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PrintingService] Error during PrintPage: {ex.Message}");
                        printSuccess = false;
                    }
                };

                doc.Print();

                if (printSuccess)
                {
                    Console.WriteLine($"[PrintingService] ✓ Receipt printed successfully to '{config.SelectedReceiptPrinter}'");
                    Console.WriteLine($"[PrintingService] Order: {order.OrderLabel}, Total: ${order.TotalAmount:F2}, Items: {order.Items.Count}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintingService] ✗ Failed to print receipt: {ex.Message}");
                Console.WriteLine($"[PrintingService] Stack trace: {ex.StackTrace}");
            }

            return false;
        }

        /// <summary>
        /// Center text within a specified width
        /// </summary>
        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return new string(' ', padding) + text;
        }

        /// <summary>
        /// Pad text to align left and right within a specified width
        /// </summary>
        private string PadRight(string left, string right, int width)
        {
            int spaces = width - left.Length - right.Length;
            if (spaces < 1) spaces = 1;
            return left + new string(' ', spaces) + right;
        }
    }
}
