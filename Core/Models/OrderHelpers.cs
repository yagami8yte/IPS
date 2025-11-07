using System;
using System.Collections.Generic;
using System.Linq;

namespace IPS.Core.Models
{
    /// <summary>
    /// Helper methods for order processing
    /// </summary>
    public static class OrderHelpers
    {
        /// <summary>
        /// Splits a multi-system order into separate orders for each unmanned system
        /// This is used when customers order items from multiple systems (e.g., Coffee + Food)
        /// Each system will receive an OrderInfo with the same OrderId but only their items
        /// </summary>
        /// <param name="order">The original order containing items from multiple systems</param>
        /// <returns>Dictionary mapping SystemName to OrderInfo for that system</returns>
        /// <example>
        /// Original order:
        ///   - OrderId: "abc-123"
        ///   - Items: [Coffee Latte (Coffee), Sandwich (Food), Americano (Coffee)]
        ///
        /// Result:
        ///   "Coffee" -> OrderInfo with [Coffee Latte, Americano]
        ///   "Food"   -> OrderInfo with [Sandwich]
        ///
        /// Both split orders share the same OrderId "abc-123" for tracking
        /// </example>
        public static Dictionary<string, OrderInfo> SplitOrderBySystem(OrderInfo order)
        {
            var systemOrders = new Dictionary<string, OrderInfo>();

            // Group items by system
            var itemsBySystem = order.Items
                .Where(item => !string.IsNullOrEmpty(item.SystemName))
                .GroupBy(item => item.SystemName);

            foreach (var systemGroup in itemsBySystem)
            {
                string systemName = systemGroup.Key;
                var systemItems = systemGroup.ToList();

                // Calculate total for this system's items
                // Note: Actual price calculation should be done by the service layer
                // using menu item prices + option prices
                decimal systemTotal = CalculateSystemTotal(systemItems, order);

                // Create a new OrderInfo for this system with the same OrderId
                var systemOrder = new OrderInfo
                {
                    OrderId = order.OrderId,  // Same ID for all systems
                    OrderLabel = order.OrderLabel,
                    Items = systemItems,
                    TotalAmount = systemTotal,
                    PriceUnit = order.PriceUnit,
                    Timestamp = order.Timestamp,
                    QrData = order.QrData,
                    AdditionalInfo = order.AdditionalInfo != null
                        ? new Dictionary<string, string>(order.AdditionalInfo)
                        : null
                };

                systemOrders[systemName] = systemOrder;
            }

            return systemOrders;
        }

        /// <summary>
        /// Gets list of unique system names from order items
        /// </summary>
        /// <param name="order">Order to analyze</param>
        /// <returns>List of system names that have items in this order</returns>
        public static List<string> GetSystemNames(OrderInfo order)
        {
            return order.Items
                .Where(item => !string.IsNullOrEmpty(item.SystemName))
                .Select(item => item.SystemName)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Validates that an order can be split (all items have SystemName)
        /// </summary>
        /// <param name="order">Order to validate</param>
        /// <returns>True if all items have a SystemName assigned</returns>
        public static bool ValidateOrderItems(OrderInfo order)
        {
            return order.Items.All(item => !string.IsNullOrEmpty(item.SystemName));
        }

        /// <summary>
        /// Private helper to calculate total for a system's items
        /// Note: This is a simplified calculation.
        /// Actual implementation should fetch menu item prices from the service layer
        /// </summary>
        private static decimal CalculateSystemTotal(List<OrderItem> items, OrderInfo originalOrder)
        {
            // This is a proportional split based on item count
            // In a real implementation, you would:
            // 1. Look up actual menu item prices
            // 2. Add option prices
            // 3. Multiply by quantity
            // For now, we'll do a simple proportional split of the total

            if (originalOrder.Items.Count == 0)
                return 0;

            int systemItemCount = items.Sum(i => i.Quantity);
            int totalItemCount = originalOrder.Items.Sum(i => i.Quantity);

            return originalOrder.TotalAmount * systemItemCount / totalItemCount;
        }
    }
}
