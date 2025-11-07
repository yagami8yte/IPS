using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents a single item in an order with selected options
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Name of the unmanned system this item belongs to (e.g., "Coffee", "Food")
        /// Used to route items to the correct system when processing multi-system orders
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Menu ID of the ordered item (must match MenuItem.MenuId)
        /// </summary>
        public string MenuId { get; set; } = string.Empty;

        /// <summary>
        /// List of selected option IDs for this item
        /// Each option ID must match a valid MenuOption.OptionId from the menu item
        /// Can be null or empty if no options are selected
        /// </summary>
        public List<string>? SelectedOptionIds { get; set; }

        /// <summary>
        /// Quantity of this item ordered
        /// </summary>
        public int Quantity { get; set; } = 1;
    }
}
