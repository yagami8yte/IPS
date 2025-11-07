using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IPS.Core.Models;

namespace IPS.Core.Interfaces
{
    /// <summary>
    /// Interface for all unmanned kiosk systems (Coffee, Food, etc.)
    /// Provides unified interface for menu retrieval, order placement, and status monitoring
    /// </summary>
    public interface IUnmannedSystem
    {
        /// <summary>
        /// Unique name identifier for this system (e.g., "Coffee", "Food", "IceCream")
        /// </summary>
        string SystemName { get; }

        /// <summary>
        /// Retrieves the current menu items with availability status
        /// This should be called periodically (polling) to get real-time menu availability
        /// Recommended polling interval: 1 second
        /// </summary>
        /// <returns>List of menu items with current availability status</returns>
        List<MenuItem> GetMenuItems();

        /// <summary>
        /// Sends an order to the unmanned system
        /// </summary>
        /// <param name="order">Order information including items and selected options</param>
        /// <returns>
        /// True if the order was successfully accepted by the system
        /// False if the order was rejected (e.g., items unavailable, system error)
        /// </returns>
        bool SendOrder(OrderInfo order);

        /// <summary>
        /// Gets the current operational status of the unmanned system
        /// Includes availability, waiting orders, estimated wait time
        /// Should be called periodically to monitor system health
        /// </summary>
        /// <returns>Current system status</returns>
        SystemStatus GetStatus();
    }
}

