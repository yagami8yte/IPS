using CommunityToolkit.Mvvm.Input;
using IPS.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace IPS.MainApp.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {
        private readonly Action _onNavigateBack;
        private readonly Action<bool, string> _onPaymentComplete;  // success, orderLabel

        public ObservableCollection<CartItem> CartItems { get; }

        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand PayCommand { get; }

        public decimal CartTotalPrice => CartItems.Sum(item => item.TotalPrice);
        public int CartItemCount => CartItems.Sum(item => item.Quantity);

        public PaymentViewModel(ObservableCollection<CartItem> cartItems, Action onNavigateBack, Action<bool, string> onPaymentComplete)
        {
            _onNavigateBack = onNavigateBack ?? throw new ArgumentNullException(nameof(onNavigateBack));
            _onPaymentComplete = onPaymentComplete ?? throw new ArgumentNullException(nameof(onPaymentComplete));

            // Use the same cart items collection (shared reference)
            CartItems = cartItems;

            IncreaseQuantityCommand = new RelayCommand<CartItem>(OnIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(OnDecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
            GoBackCommand = new RelayCommand(OnGoBack);
            PayCommand = new RelayCommand(OnPay, CanPay);
        }

        private void OnIncreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null) return;

            cartItem.Quantity++;
            UpdateCartTotals();
        }

        private void OnDecreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null) return;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCartTotals();
            }
            else
            {
                OnRemoveFromCart(cartItem);
            }
        }

        private void OnRemoveFromCart(CartItem? cartItem)
        {
            if (cartItem == null) return;

            CartItems.Remove(cartItem);
            UpdateCartTotals();
            ((RelayCommand)PayCommand).NotifyCanExecuteChanged();
        }

        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(CartTotalPrice));
            OnPropertyChanged(nameof(CartItemCount));
        }

        private void OnGoBack()
        {
            _onNavigateBack();
        }

        private bool CanPay()
        {
            return CartItems.Count > 0;
        }

        private void OnPay()
        {
            // For now, assume payment succeeds
            // In the future, integrate actual payment service here

            // Generate order label (format: A-001, A-002, etc.)
            string orderLabel = GenerateOrderLabel();

            // Call the completion callback with success
            _onPaymentComplete(true, orderLabel);
        }

        private string GenerateOrderLabel()
        {
            // Generate a simple order label like A-001
            // In production, this should be sequential and stored
            Random random = new Random();
            int orderNumber = random.Next(1, 999);
            return $"A-{orderNumber:D3}";
        }
    }
}
