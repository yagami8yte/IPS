using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents an item in the shopping cart with selected options
    /// </summary>
    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity = 1;

        /// <summary>
        /// Unique identifier for this cart item
        /// </summary>
        public string CartItemId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// System this item belongs to
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Reference to the menu item
        /// </summary>
        public MenuItem MenuItem { get; set; } = null!;

        /// <summary>
        /// Selected options for this item
        /// </summary>
        public List<MenuOption> SelectedOptions { get; set; } = new();

        /// <summary>
        /// Quantity of this item
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                    OnPropertyChanged(nameof(UnitPrice));
                }
            }
        }

        /// <summary>
        /// Unit price for one item (base price + options)
        /// </summary>
        public decimal UnitPrice
        {
            get
            {
                decimal basePrice = MenuItem?.Price ?? 0;
                decimal optionsPrice = SelectedOptions?.Sum(o => o.Price) ?? 0;
                return basePrice + optionsPrice;
            }
        }

        /// <summary>
        /// Total price for this cart item (base price + options) × quantity
        /// </summary>
        public decimal TotalPrice
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Display string for selected options
        /// </summary>
        public string OptionsDisplayText
        {
            get
            {
                if (SelectedOptions == null || SelectedOptions.Count == 0)
                    return "No options";

                return string.Join(", ", SelectedOptions.Select(o => o.Name));
            }
        }
    }
}
