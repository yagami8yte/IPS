using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents an option that can be selected for a menu item
    /// </summary>
    public class MenuOption
    {
        /// <summary>
        /// Unique identifier for this option
        /// </summary>
        public string OptionId { get; set; } = string.Empty;

        /// <summary>
        /// Category ID - options with the same category ID are mutually exclusive
        /// (user can only select one option per category)
        /// </summary>
        public string OptionCategoryId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the option (e.g., "Extra Shot", "Light", "Add Ice")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Name of the option category (e.g., "Strength", "Ice", "Size")
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Additional price for selecting this option
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Whether this option is currently available
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Display priority among options in the same category (lower values display first)
        /// </summary>
        public int ViewIndex { get; set; }

    }
}
