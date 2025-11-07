using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents order information to be sent to an unmanned kiosk system
    /// </summary>
    public class OrderInfo
    {
        /// <summary>
        /// Unique identifier for this order (UUID4 format)
        /// </summary>
        public string OrderId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Display label for the order shown to customer (e.g., "001", "A-12")
        /// Used for order tracking and pickup identification
        /// </summary>
        public string OrderLabel { get; set; } = string.Empty;

        /// <summary>
        /// List of items in this order
        /// </summary>
        public List<OrderItem> Items { get; set; } = new();

        /// <summary>
        /// Total amount for this order including all items and options
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency unit (e.g., "USD", "KRW")
        /// </summary>
        public string PriceUnit { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the order was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// QR code data for order pickup (if QR pickup is enabled)
        /// Customer scans this code to pick up their order
        /// </summary>
        public string? QrData { get; set; }

        /// <summary>
        /// Additional payment/invoice information (optional)
        /// Can store invoice number, reference number, auth codes, etc.
        /// Used for integration with payment systems and audit trails
        /// </summary>
        public Dictionary<string, string>? AdditionalInfo { get; set; }
    }
}
