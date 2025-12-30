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
        private MenuItem _selectedVariant = null!;
        private List<MenuItem> _allVariants = new();
        private string _systemName = string.Empty;
        private int _quantity = 1;
        private Action<CartItem>? _onAddToCart;
        private Action? _onClose;
        private string? _selectedTemperature;
        private string? _selectedVariantType;

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

        /// <summary>
        /// Currently selected variant (considering temperature and variant type selections)
        /// </summary>
        public MenuItem SelectedVariant
        {
            get => _selectedVariant;
            set
            {
                if (_selectedVariant != value)
                {
                    _selectedVariant = value;
                    OnPropertyChanged();
                    MenuItem = value; // Update MenuItem to refresh UI
                }
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

        // Variant selection properties
        public bool HasTemperatureVariants { get; private set; }
        public bool HasVariantTypes { get; private set; }
        public ObservableCollection<string> AvailableTemperatures { get; } = new();
        public ObservableCollection<string> AvailableVariantTypes { get; } = new();

        public string? SelectedTemperature
        {
            get => _selectedTemperature;
            set
            {
                if (_selectedTemperature != value)
                {
                    _selectedTemperature = value;
                    OnPropertyChanged();
                    UpdateSelectedVariant();
                }
            }
        }

        public string? SelectedVariantType
        {
            get => _selectedVariantType;
            set
            {
                if (_selectedVariantType != value)
                {
                    _selectedVariantType = value;
                    OnPropertyChanged();
                    UpdateSelectedVariant();
                }
            }
        }

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

        public void Initialize(MenuItem menuItem, List<MenuItem> allVariants, string systemName, Action<CartItem> onAddToCart, Action onClose)
        {
            _allVariants = allVariants ?? new List<MenuItem> { menuItem };

            // Ensure the list always contains at least the clicked menu item
            if (_allVariants.Count == 0)
            {
                _allVariants.Add(menuItem);
            }

            SystemName = systemName;
            _onAddToCart = onAddToCart;
            _onClose = onClose;
            Quantity = 1;

            // Load available variant options
            LoadVariantOptions();

            // Set initial selection (prefer available items)
            ChooseDefaultVariant();
        }

        private void LoadVariantOptions()
        {
            AvailableTemperatures.Clear();
            AvailableVariantTypes.Clear();

            // Get unique temperatures
            var temperatures = _allVariants
                .Where(v => !string.IsNullOrEmpty(v.MenuTemperature))
                .Select(v => v.MenuTemperature!.ToLowerInvariant())
                .Distinct()
                .OrderBy(t => t == "hot" ? 0 : 1) // Hot first, then ice
                .ToList();

            foreach (var temp in temperatures)
            {
                AvailableTemperatures.Add(temp);
            }

            // Get unique variant types
            var variantTypes = _allVariants
                .Where(v => !string.IsNullOrEmpty(v.MenuVariant))
                .Select(v => v.MenuVariant!.ToLowerInvariant())
                .Distinct()
                .OrderBy(v => v == "regular" ? 0 : v == "light" ? 1 : 2) // Regular, Light, Extra
                .ToList();

            foreach (var variant in variantTypes)
            {
                AvailableVariantTypes.Add(variant);
            }

            HasTemperatureVariants = AvailableTemperatures.Count > 0;
            HasVariantTypes = AvailableVariantTypes.Count > 0;

            OnPropertyChanged(nameof(HasTemperatureVariants));
            OnPropertyChanged(nameof(HasVariantTypes));
        }

        private void ChooseDefaultVariant()
        {
            if (_allVariants.Count == 0)
            {
                Console.WriteLine("[MenuDetailModalViewModel] ChooseDefaultVariant - No variants available!");
                return;
            }

            // Find first available variant
            var defaultVariant = _allVariants.FirstOrDefault(v => v.IsAvailable) ?? _allVariants.First();

            Console.WriteLine($"[MenuDetailModalViewModel] ChooseDefaultVariant - Selected: {defaultVariant.Name}, Temp: {defaultVariant.MenuTemperature}, Variant: {defaultVariant.MenuVariant}");

            // Set default selections
            SelectedTemperature = defaultVariant.MenuTemperature?.ToLowerInvariant();
            SelectedVariantType = defaultVariant.MenuVariant?.ToLowerInvariant();

            // For non-variant items (no temperature/variant), directly set the MenuItem
            if (string.IsNullOrEmpty(SelectedTemperature) && string.IsNullOrEmpty(SelectedVariantType))
            {
                Console.WriteLine("[MenuDetailModalViewModel] ChooseDefaultVariant - Non-variant item, setting MenuItem directly");
                MenuItem = defaultVariant;
                _selectedVariant = defaultVariant;
            }
            // For variant items, UpdateSelectedVariant will be triggered by the property setters
        }

        private void UpdateSelectedVariant()
        {
            // Find variant matching current selections
            var matchingVariant = _allVariants.FirstOrDefault(v =>
                (string.IsNullOrEmpty(SelectedTemperature) ||
                 v.MenuTemperature?.ToLowerInvariant() == SelectedTemperature) &&
                (string.IsNullOrEmpty(SelectedVariantType) ||
                 v.MenuVariant?.ToLowerInvariant() == SelectedVariantType)
            );

            if (matchingVariant != null)
            {
                Console.WriteLine($"[MenuDetailModalViewModel] UpdateSelectedVariant - Found match: {matchingVariant.Name}");
                SelectedVariant = matchingVariant;
            }
            else
            {
                Console.WriteLine($"[MenuDetailModalViewModel] UpdateSelectedVariant - No match found for Temp={SelectedTemperature}, Variant={SelectedVariantType}");
                // If no exact match found, use the first item (fallback for non-variant items)
                if (_allVariants.Count > 0)
                {
                    SelectedVariant = _allVariants.First();
                }
            }
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
            // Use SelectedVariant if available, otherwise MenuItem
            var menuToAdd = _selectedVariant ?? MenuItem;
            if (menuToAdd == null) return;

            var selectedOptions = OptionCategories
                .SelectMany(c => c.Options)
                .Where(o => o.IsSelected)
                .Select(o => o.Option)
                .ToList();

            var cartItem = new CartItem
            {
                SystemName = SystemName,
                MenuItem = menuToAdd, // Use the selected variant
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
