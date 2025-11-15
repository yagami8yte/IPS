using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPS.Core.Models;

namespace IPS.Core.Interfaces
{
    /// <summary>
    /// Service interface for receipt printing operations
    /// </summary>
    public interface IPrintingService
    {
        /// <summary>
        /// Print a receipt for a completed order
        /// </summary>
        /// <param name="order">Order information</param>
        /// <param name="cartItems">Cart items with full details (names, prices, options)</param>
        /// <param name="cardLast4Digits">Last 4 digits of card (for PCI compliance)</param>
        /// <param name="authorizationCode">Payment authorization code</param>
        /// <param name="transactionId">Payment transaction ID</param>
        /// <returns>True if printed successfully, false if failed (non-blocking)</returns>
        bool PrintReceipt(OrderInfo order, System.Collections.ObjectModel.ObservableCollection<CartItem> cartItems, string? cardLast4Digits, string? authorizationCode, string? transactionId);
    }
}
