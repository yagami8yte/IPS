using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Aggregated sales statistics for a given time period
    /// </summary>
    public class SalesStatistics
    {
        /// <summary>
        /// Start date of the statistics period
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the statistics period
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Total revenue for the period
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Total number of orders
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Average order value
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Top selling items
        /// </summary>
        public List<TopSellingItem> TopItems { get; set; } = new();

        /// <summary>
        /// Revenue breakdown by system (Coffee, Food, etc.)
        /// </summary>
        public Dictionary<string, decimal> RevenueBySystem { get; set; } = new();

        /// <summary>
        /// Daily sales data for chart visualization
        /// </summary>
        public List<DailySales> DailySalesData { get; set; } = new();
    }

    /// <summary>
    /// Represents a top-selling item with sales metrics
    /// </summary>
    public class TopSellingItem
    {
        /// <summary>
        /// Item name
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// System name (Coffee, Food, etc.)
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Total quantity sold
        /// </summary>
        public int QuantitySold { get; set; }

        /// <summary>
        /// Total revenue from this item
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Percentage of total sales
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Daily sales aggregation for chart data
    /// </summary>
    public class DailySales
    {
        /// <summary>
        /// Date of the sales data
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Total revenue for the day
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Number of orders for the day
        /// </summary>
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Time interval options for sales filtering
    /// </summary>
    public enum TimeInterval
    {
        Today,
        Yesterday,
        Last7Days,
        Last30Days,
        ThisMonth,
        LastMonth,
        Last3Months,
        Last6Months,
        ThisYear,
        AllTime,
        CustomRange
    }
}
