using System;
using System.Collections.Generic;
using System.Linq;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Service for managing sales data, analytics, and reporting
    /// Currently uses mock data - will be connected to database in future
    /// </summary>
    public class SalesService
    {
        private readonly List<OrderHistory> _orderHistory = new();

        public SalesService()
        {
            // Initialize with mock data for demonstration
            GenerateMockData();
        }

        /// <summary>
        /// Get all order history within a time range
        /// </summary>
        public List<OrderHistory> GetOrderHistory(DateTime startDate, DateTime endDate)
        {
            return _orderHistory
                .Where(o => o.OrderDateTime >= startDate && o.OrderDateTime <= endDate && o.Status == OrderStatus.Completed)
                .OrderByDescending(o => o.OrderDateTime)
                .ToList();
        }

        /// <summary>
        /// Get sales statistics for a given time period
        /// </summary>
        public SalesStatistics GetSalesStatistics(DateTime startDate, DateTime endDate)
        {
            var orders = GetOrderHistory(startDate, endDate);

            var statistics = new SalesStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0
            };

            // Calculate daily sales data
            statistics.DailySalesData = CalculateDailySales(orders, startDate, endDate);

            // Calculate top selling items
            statistics.TopItems = CalculateTopSellingItems(orders);

            // Calculate revenue by system
            statistics.RevenueBySystem = CalculateRevenueBySystem(orders);

            return statistics;
        }

        /// <summary>
        /// Get time range based on selected interval
        /// </summary>
        public (DateTime StartDate, DateTime EndDate) GetTimeRange(TimeInterval interval, DateTime? customStart = null, DateTime? customEnd = null)
        {
            DateTime now = DateTime.Now;
            DateTime start, end;

            switch (interval)
            {
                case TimeInterval.Today:
                    start = now.Date;
                    end = now.Date.AddDays(1).AddSeconds(-1);
                    break;
                case TimeInterval.Yesterday:
                    start = now.Date.AddDays(-1);
                    end = now.Date.AddSeconds(-1);
                    break;
                case TimeInterval.Last7Days:
                    start = now.Date.AddDays(-7);
                    end = now;
                    break;
                case TimeInterval.Last30Days:
                    start = now.Date.AddDays(-30);
                    end = now;
                    break;
                case TimeInterval.ThisMonth:
                    start = new DateTime(now.Year, now.Month, 1);
                    end = now;
                    break;
                case TimeInterval.LastMonth:
                    start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    end = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
                    break;
                case TimeInterval.Last3Months:
                    start = now.Date.AddMonths(-3);
                    end = now;
                    break;
                case TimeInterval.Last6Months:
                    start = now.Date.AddMonths(-6);
                    end = now;
                    break;
                case TimeInterval.ThisYear:
                    start = new DateTime(now.Year, 1, 1);
                    end = now;
                    break;
                case TimeInterval.AllTime:
                    start = DateTime.MinValue;
                    end = DateTime.MaxValue;
                    break;
                case TimeInterval.CustomRange:
                    start = customStart ?? now.Date.AddDays(-30);
                    end = customEnd ?? now;
                    break;
                default:
                    start = now.Date.AddDays(-7);
                    end = now;
                    break;
            }

            return (start, end);
        }

        private List<DailySales> CalculateDailySales(List<OrderHistory> orders, DateTime startDate, DateTime endDate)
        {
            var dailySales = new List<DailySales>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayOrders = orders.Where(o => o.OrderDateTime.Date == date).ToList();

                dailySales.Add(new DailySales
                {
                    Date = date,
                    Revenue = dayOrders.Sum(o => o.TotalAmount),
                    OrderCount = dayOrders.Count
                });
            }

            return dailySales;
        }

        private List<TopSellingItem> CalculateTopSellingItems(List<OrderHistory> orders)
        {
            var totalRevenue = orders.Sum(o => o.TotalAmount);

            var itemSales = orders
                .SelectMany(o => o.Items)
                .GroupBy(item => new { item.ItemName, item.SystemName })
                .Select(g => new TopSellingItem
                {
                    ItemName = g.Key.ItemName,
                    SystemName = g.Key.SystemName,
                    QuantitySold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.TotalPrice),
                    Percentage = totalRevenue > 0 ? (double)(g.Sum(i => i.TotalPrice) / totalRevenue * 100) : 0
                })
                .OrderByDescending(i => i.TotalRevenue)
                .Take(10)
                .ToList();

            return itemSales;
        }

        private Dictionary<string, decimal> CalculateRevenueBySystem(List<OrderHistory> orders)
        {
            return orders
                .SelectMany(o => o.Items)
                .GroupBy(item => item.SystemName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(i => i.TotalPrice)
                );
        }

        /// <summary>
        /// Generate mock sales data for demonstration
        /// </summary>
        private void GenerateMockData()
        {
            var random = new Random();
            var menuItems = new[]
            {
                ("Americano", "Coffee", 3.5m),
                ("Latte", "Coffee", 4.5m),
                ("Cappuccino", "Coffee", 4.0m),
                ("Espresso", "Coffee", 3.0m),
                ("Mocha", "Coffee", 5.0m),
                ("Sandwich", "Food", 6.5m),
                ("Salad", "Food", 7.0m),
                ("Burger", "Food", 8.5m),
                ("Pasta", "Food", 9.0m),
                ("Wrap", "Food", 6.0m)
            };

            // Generate 100 random orders over the past 30 days
            for (int i = 0; i < 100; i++)
            {
                var orderDateTime = DateTime.Now.AddDays(-random.Next(0, 30)).AddHours(random.Next(8, 20));
                var itemCount = random.Next(1, 4);
                var orderItems = new List<OrderHistoryItem>();

                for (int j = 0; j < itemCount; j++)
                {
                    var item = menuItems[random.Next(menuItems.Length)];
                    var quantity = random.Next(1, 3);

                    orderItems.Add(new OrderHistoryItem
                    {
                        ItemName = item.Item1,
                        SystemName = item.Item2,
                        Quantity = quantity,
                        UnitPrice = item.Item3,
                        TotalPrice = item.Item3 * quantity,
                        Options = new List<string>()
                    });
                }

                _orderHistory.Add(new OrderHistory
                {
                    OrderId = Guid.NewGuid().ToString(),
                    OrderLabel = $"{(char)('A' + i % 26)}{(i % 100):D2}",
                    OrderDateTime = orderDateTime,
                    Items = orderItems,
                    TotalAmount = orderItems.Sum(item => item.TotalPrice),
                    PaymentMethod = random.Next(2) == 0 ? "QR" : "Card",
                    Status = OrderStatus.Completed
                });
            }
        }
    }
}
