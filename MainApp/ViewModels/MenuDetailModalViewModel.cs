using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using IPS.Core.Models;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for the menu detail modal
    /// </summary>
    public class MenuDetailModalViewModel : BaseViewModel
    {
        private MenuItem _menuItem = null!;
        private string _systemName = string.Empty;
        private int _quantity = 1;
        private Action<CartItem>? _onAddToCart;
        private Action? _onClose;

        public MenuItem MenuItem
        {
            get => _menuItem;
            set
            {
                _menuItem = value;
                OnPropertyChanged();
                LoadOptions();
                CalculateTotalPrice();
            }
        }

        public string SystemName
        {
            get => _systemName;
            set
            {
                _systemName = value;
                OnPropertyChanged();
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value > 0 && value <= 99)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    CalculateTotalPrice();
                }
            }
        }

        public decimal TotalPrice { get; private set; }

        public ObservableCollection<OptionCategoryViewModel> OptionCategories { get; } = new();

        public IRelayCommand IncreaseQuantityCommand { get; }
        public IRelayCommand DecreaseQuantityCommand { get; }
        public IRelayCommand AddToCartCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public MenuDetailModalViewModel()
        {
            IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity);
            AddToCartCommand = new RelayCommand(AddToCart);
            CloseCommand = new RelayCommand(Close);
        }

        public void Initialize(MenuItem menuItem, string systemName, Action<CartItem> onAddToCart, Action onClose)
        {
            MenuItem = menuItem;
            SystemName = systemName;
            _onAddToCart = onAddToCart;
            _onClose = onClose;
            Quantity = 1;
        }

        private void LoadOptions()
        {
            OptionCategories.Clear();

            if (MenuItem?.Options == null || !MenuItem.Options.Any())
                return;

            // Group options by category
            var groupedOptions = MenuItem.Options
                .GroupBy(o => new { o.OptionCategoryId, o.CategoryName })
                .OrderBy(g => g.Min(o => o.ViewIndex));

            foreach (var group in groupedOptions)
            {
                var categoryVm = new OptionCategoryViewModel
                {
                    CategoryId = group.Key.OptionCategoryId,
                    CategoryName = group.Key.CategoryName,
                    Options = new ObservableCollection<MenuOptionViewModel>(
                        group.OrderBy(o => o.ViewIndex)
                             .Select(o => new MenuOptionViewModel(o, this))
                    )
                };

                // Select first option by default
                if (categoryVm.Options.Any())
                {
                    categoryVm.Options[0].IsSelected = true;
                }

                OptionCategories.Add(categoryVm);
            }
        }

        private void IncreaseQuantity()
        {
            Quantity++;
        }

        private void DecreaseQuantity()
        {
            if (Quantity > 1)
                Quantity--;
        }

        internal void CalculateTotalPrice()
        {
            if (MenuItem == null)
            {
                TotalPrice = 0;
                OnPropertyChanged(nameof(TotalPrice));
                return;
            }

            decimal basePrice = MenuItem.Price;
            decimal optionsPrice = OptionCategories
                .SelectMany(c => c.Options)
                .Where(o => o.IsSelected)
                .Sum(o => o.Price);

            TotalPrice = (basePrice + optionsPrice) * Quantity;
            OnPropertyChanged(nameof(TotalPrice));
        }

        private void AddToCart()
        {
            if (MenuItem == null) return;

            var selectedOptions = OptionCategories
                .SelectMany(c => c.Options)
                .Where(o => o.IsSelected)
                .Select(o => o.Option)
                .ToList();

            var cartItem = new CartItem
            {
                SystemName = SystemName,
                MenuItem = MenuItem,
                SelectedOptions = selectedOptions,
                Quantity = Quantity
            };

            _onAddToCart?.Invoke(cartItem);
            Close();
        }

        private void Close()
        {
            _onClose?.Invoke();
        }
    }

    /// <summary>
    /// ViewModel for an option category
    /// </summary>
    public class OptionCategoryViewModel
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public ObservableCollection<MenuOptionViewModel> Options { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for a menu option
    /// </summary>
    public class MenuOptionViewModel : BaseViewModel
    {
        private readonly MenuDetailModalViewModel _parent;
        private bool _isSelected;

        public MenuOption Option { get; }

        public string OptionId => Option.OptionId;
        public string CategoryId => Option.OptionCategoryId;
        public string Name => Option.Name;
        public decimal Price => Option.Price;
        public bool IsEnabled => Option.IsEnabled;

        public Visibility PriceVisibility => Price > 0 ? Visibility.Visible : Visibility.Collapsed;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();

                    // If this option is selected, deselect other options in the same category
                    if (value)
                    {
                        DeselectOtherOptionsInCategory();
                    }

                    // Always recalculate price when selection changes
                    _parent.CalculateTotalPrice();
                }
            }
        }

        private void DeselectOtherOptionsInCategory()
        {
            var category = _parent.OptionCategories.FirstOrDefault(c => c.CategoryId == CategoryId);
            if (category != null)
            {
                foreach (var option in category.Options)
                {
                    if (option != this && option.IsSelected)
                    {
                        option._isSelected = false; // Set directly to avoid recursive calls
                        option.OnPropertyChanged(nameof(IsSelected));
                    }
                }
            }
        }

        public MenuOptionViewModel(MenuOption option, MenuDetailModalViewModel parent)
        {
            Option = option;
            _parent = parent;
        }
    }
}
