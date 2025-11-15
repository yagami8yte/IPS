using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Configuration for an unmanned system (IP address and port)
    /// </summary>
    public class SystemConfiguration
    {
        /// <summary>
        /// System identifier (e.g., "Coffee", "Food")
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the unmanned system
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Whether this system is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Application-wide configuration settings
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// List of configured unmanned systems
        /// </summary>
        public List<SystemConfiguration> Systems { get; set; } = new();

        /// <summary>
        /// Port for the DLL's internal HTTP server
        /// </summary>
        public int DllServerPort { get; set; } = 5000;

        /// <summary>
        /// BCrypt hashed admin PIN
        /// Default PIN is "0000" (numeric only, entered via touchscreen keypad)
        /// </summary>
        public string AdminPasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Whether scheduled break time mode is currently enabled
        /// </summary>
        public bool IsBreakTimeEnabled { get; set; } = false;

        /// <summary>
        /// Whether instant break mode is active (manual override)
        /// </summary>
        public bool IsInstantBreakActive { get; set; } = false;

        /// <summary>
        /// Break time start (hours 0-23)
        /// </summary>
        public int BreakStartHour { get; set; } = 14;

        /// <summary>
        /// Break time start (minutes 0-59)
        /// </summary>
        public int BreakStartMinute { get; set; } = 0;

        /// <summary>
        /// Break time end (hours 0-23)
        /// </summary>
        public int BreakEndHour { get; set; } = 15;

        /// <summary>
        /// Break time end (minutes 0-59)
        /// </summary>
        public int BreakEndMinute { get; set; } = 0;

        /// <summary>
        /// Custom break message to display to customers (optional)
        /// </summary>
        public string BreakMessage { get; set; } = string.Empty;

        /// <summary>
        /// Forte Payment Gateway - API Access ID (username for authentication)
        /// </summary>
        public string ForteApiAccessId { get; set; } = string.Empty;

        /// <summary>
        /// Forte Payment Gateway - API Secure Key (password for authentication)
        /// </summary>
        public string ForteApiSecureKey { get; set; } = string.Empty;

        /// <summary>
        /// Forte Organization ID (e.g., org_300005)
        /// </summary>
        public string ForteOrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Forte Location ID (e.g., loc_192834)
        /// </summary>
        public string ForteLocationId { get; set; } = string.Empty;

        /// <summary>
        /// Use Forte Sandbox environment for testing
        /// </summary>
        public bool ForteSandboxMode { get; set; } = true;

        /// <summary>
        /// Enable payment processing (disable for testing without payment)
        /// </summary>
        public bool PaymentEnabled { get; set; } = false;

        /// <summary>
        /// Use physical card terminal for card-present transactions (via Device Handler DLL)
        /// When false, use manual card entry via REST API
        /// </summary>
        public bool UseCardTerminal { get; set; } = false;

        /// <summary>
        /// Terminal type (e.g., "Verifone VX520", "Verifone V400C Plus")
        /// </summary>
        public string TerminalType { get; set; } = "Verifone V400C Plus";

        /// <summary>
        /// Terminal connection type (e.g., "USB", "Serial")
        /// </summary>
        public string TerminalConnection { get; set; } = "USB";

        /// <summary>
        /// COM Port for serial connection (e.g., "COM1", "COM3")
        /// Only used if TerminalConnection is "Serial"
        /// </summary>
        public string TerminalComPort { get; set; } = "COM1";

        /// <summary>
        /// Forte AGI Merchant ID (for Device Handler / AGI API authentication)
        /// Different from REST API Organization ID
        /// </summary>
        public string ForteMerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Forte AGI Processing Password (for Device Handler / AGI API authentication)
        /// Different from REST API Secure Key
        /// </summary>
        public string ForteProcessingPassword { get; set; } = string.Empty;

        // ========================================
        // Receipt Printing Configuration
        // ========================================

        /// <summary>
        /// Business/Store name to print on receipts
        /// </summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Business address line 1 (e.g., "123 Main Street, Suite 100")
        /// </summary>
        public string BusinessAddressLine1 { get; set; } = string.Empty;

        /// <summary>
        /// Business address line 2 (e.g., "New York, NY 10001")
        /// </summary>
        public string BusinessAddressLine2 { get; set; } = string.Empty;

        /// <summary>
        /// Business phone number (e.g., "(555) 123-4567")
        /// </summary>
        public string BusinessPhone { get; set; } = string.Empty;

        /// <summary>
        /// Tax ID / EIN for receipt (optional, for compliance)
        /// </summary>
        public string BusinessTaxId { get; set; } = string.Empty;

        /// <summary>
        /// Custom footer message on receipt (e.g., "Thank you for your order!")
        /// </summary>
        public string ReceiptFooterMessage { get; set; } = "Thank you for your order!";

        /// <summary>
        /// Selected receipt printer name (from Windows installed printers)
        /// </summary>
        public string SelectedReceiptPrinter { get; set; } = string.Empty;

        /// <summary>
        /// Enable automatic receipt printing after successful payment
        /// </summary>
        public bool AutoPrintReceipt { get; set; } = true;

        /// <summary>
        /// Enable tax calculation on orders
        /// </summary>
        public bool TaxEnabled { get; set; } = false;

        /// <summary>
        /// Tax rate (e.g., 0.08 for 8%)
        /// </summary>
        public decimal TaxRate { get; set; } = 0.0m;

        /// <summary>
        /// Tax label to display on receipt (e.g., "Sales Tax", "VAT")
        /// </summary>
        public string TaxLabel { get; set; } = "Tax";
    }
}
