using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents a complete order record for sales tracking and analysis
    /// </summary>
    public class OrderHistory
    {
        /// <summary>
        /// Unique order identifier
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Customer-facing order label (e.g., "A01", "B12")
        /// </summary>
        public string OrderLabel { get; set; } = string.Empty;

        /// <summary>
        /// Date and time when the order was placed
        /// </summary>
        public DateTime OrderDateTime { get; set; }

        /// <summary>
        /// Total amount paid for the order
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// List of items in the order
        /// </summary>
        public List<OrderHistoryItem> Items { get; set; } = new();

        /// <summary>
        /// Payment method used (QR, Card, etc.)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Order status (Completed, Cancelled, Refunded, etc.)
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Completed;
    }

    /// <summary>
    /// Represents a single item within an order history
    /// </summary>
    public class OrderHistoryItem
    {
        /// <summary>
        /// Name of the menu item
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Which system this item came from (Coffee, Food, etc.)
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity ordered
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price of the item
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total price for this line item (Quantity * UnitPrice)
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Selected options/customizations
        /// </summary>
        public List<string> Options { get; set; } = new();
    }

    /// <summary>
    /// Order status enumeration
    /// </summary>
    public enum OrderStatus
    {
        Completed,
        Cancelled,
        Refunded,
        InProgress
    }
}
