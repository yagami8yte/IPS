using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using IPS.Core.Models;
using IPS.Services;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for the menu view
    /// Manages menu display, cart, and modal interactions
    /// </summary>
    public class MenuViewModel : BaseViewModel
    {
        private readonly SystemManagerService _systemManager;
        private readonly SystemPollingService _pollingService;
        private readonly System.Windows.Threading.DispatcherTimer _inactivityTimer;

        private string _selectedSystemName = string.Empty;
        private string _selectedCategoryId = "ALL";
        private bool _isModalVisible;
        private MenuDetailModalViewModel? _modalViewModel;
        private List<MenuItem> _allMenuItemsForCurrentSystem = new();
        private bool _isTimeoutWarningVisible;
        private int _remainingSeconds;
        private int _inactivityTimeoutSeconds = 120; // 20 seconds for debugging, will be 120 for production
        private int _warningThresholdSeconds = 10;
        private bool _isLoading = true;

        /// <summary>
        /// Available system tabs for selection
        /// </summary>
        public ObservableCollection<SystemTabViewModel> SystemTabs { get; } = new();

        /// <summary>
        /// Available categories for the selected system
        /// </summary>
        public ObservableCollection<MenuCategoryViewModel> Categories { get; } = new();

        /// <summary>
        /// Category groups with their menu items (grouped display)
        /// </summary>
        public ObservableCollection<MenuCategoryGroup> CategoryGroups { get; } = new();

        /// <summary>
        /// Cart items
        /// </summary>
        public ObservableCollection<CartItem> CartItems { get; } = new();

        /// <summary>
        /// Currently selected system name
        /// </summary>
        public string SelectedSystemName
        {
            get => _selectedSystemName;
            set
            {
                if (_selectedSystemName != value)
                {
                    Console.WriteLine($"[MenuViewModel] SelectedSystemName changing from '{_selectedSystemName}' to '{value}'");
                    _selectedSystemName = value;
                    OnPropertyChanged();
                    Console.WriteLine($"[MenuViewModel] SelectedSystemName - Calling LoadMenuForSelectedSystem...");
                    LoadMenuForSelectedSystem();
                    Console.WriteLine($"[MenuViewModel] SelectedSystemName - LoadMenuForSelectedSystem completed");
                }
            }
        }

        /// <summary>
        /// Currently selected category ID
        /// </summary>
        public string SelectedCategoryId
        {
            get => _selectedCategoryId;
            set
            {
                if (_selectedCategoryId != value)
                {
                    _selectedCategoryId = value;
                    OnPropertyChanged();
                    UpdateCategorySelection();
                    FilterMenuItemsByCategory();
                }
            }
        }

        /// <summary>
        /// Whether the menu detail modal is visible
        /// </summary>
        public bool IsModalVisible
        {
            get => _isModalVisible;
            set
            {
                _isModalVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ViewModel for the menu detail modal
        /// </summary>
        public MenuDetailModalViewModel? ModalViewModel
        {
            get => _modalViewModel;
            set
            {
                _modalViewModel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the timeout warning modal is visible
        /// </summary>
        public bool IsTimeoutWarningVisible
        {
            get => _isTimeoutWarningVisible;
            set
            {
                _isTimeoutWarningVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Remaining seconds before automatic timeout
        /// </summary>
        public int RemainingSeconds
        {
            get => _remainingSeconds;
            set
            {
                _remainingSeconds = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Total number of items in cart
        /// </summary>
        public int CartItemCount => CartItems.Sum(item => item.Quantity);

        /// <summary>
        /// Total price of all items in cart
        /// </summary>
        public decimal CartTotalPrice => CartItems.Sum(item => item.TotalPrice);

        /// <summary>
        /// Whether the cart has any items
        /// </summary>
        public bool HasCartItems => CartItems.Count > 0;

        /// <summary>
        /// Whether the view is loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to select a menu item and show detail modal
        /// </summary>
        public IRelayCommand<MenuItem> SelectMenuItemCommand { get; }

        /// <summary>
        /// Command to select a system
        /// </summary>
        public IRelayCommand<string> SelectSystemCommand { get; }

        /// <summary>
        /// Command to select a category
        /// </summary>
        public IRelayCommand<string> SelectCategoryCommand { get; }

        /// <summary>
        /// Command to increase cart item quantity
        /// </summary>
        public IRelayCommand<CartItem> IncreaseQuantityCommand { get; }

        /// <summary>
        /// Command to decrease cart item quantity
        /// </summary>
        public IRelayCommand<CartItem> DecreaseQuantityCommand { get; }

        /// <summary>
        /// Command to remove item from cart
        /// </summary>
        public IRelayCommand<CartItem> RemoveFromCartCommand { get; }

        /// <summary>
        /// Command to proceed to checkout
        /// </summary>
        public IRelayCommand CheckoutCommand { get; }

        /// <summary>
        /// Command to start over and return to welcome screen
        /// </summary>
        public IRelayCommand StartOverCommand { get; }

        /// <summary>
        /// Command to continue ordering (dismiss timeout warning)
        /// </summary>
        public IRelayCommand ContinueOrderCommand { get; }

        /// <summary>
        /// Command to quit ordering (from timeout warning)
        /// </summary>
        public IRelayCommand QuitOrderCommand { get; }

        private readonly Action? _onNavigateToWelcome;
        private readonly Action? _onNavigateToPayment;

        public MenuViewModel(SystemManagerService systemManager, SystemPollingService pollingService, Action? onNavigateToWelcome = null, Action? onNavigateToPayment = null)
        {
            Console.WriteLine("[MenuViewModel] Constructor - START");

            Console.WriteLine("[MenuViewModel] Validating parameters...");
            _systemManager = systemManager ?? throw new ArgumentNullException(nameof(systemManager));
            _pollingService = pollingService ?? throw new ArgumentNullException(nameof(pollingService));
            _onNavigateToWelcome = onNavigateToWelcome;
            _onNavigateToPayment = onNavigateToPayment;
            Console.WriteLine("[MenuViewModel] Parameters validated");

            Console.WriteLine("[MenuViewModel] Creating commands...");
            SelectMenuItemCommand = new RelayCommand<MenuItem>(OnSelectMenuItem);
            SelectSystemCommand = new RelayCommand<string>(OnSelectSystem);
            SelectCategoryCommand = new RelayCommand<string>(OnSelectCategory);
            IncreaseQuantityCommand = new RelayCommand<CartItem>(OnIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(OnDecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItem>(OnRemoveFromCart);
            CheckoutCommand = new RelayCommand(OnCheckout);
            StartOverCommand = new RelayCommand(OnStartOver);
            ContinueOrderCommand = new RelayCommand(OnContinueOrder);
            QuitOrderCommand = new RelayCommand(OnQuitOrder);
            Console.WriteLine("[MenuViewModel] Commands created");

            Console.WriteLine("[MenuViewModel] Initializing inactivity timer...");
            _inactivityTimer = new System.Windows.Threading.DispatcherTimer();
            _inactivityTimer.Interval = TimeSpan.FromSeconds(1);
            _inactivityTimer.Tick += OnInactivityTimerTick;
            ResetInactivityTimer();
            Console.WriteLine("[MenuViewModel] Inactivity timer initialized");

            Console.WriteLine("[MenuViewModel] Subscribing to polling updates...");
            _pollingService.MenuItemsUpdated += OnMenuItemsUpdated;
            Console.WriteLine("[MenuViewModel] Subscribed to polling updates");

            Console.WriteLine("[MenuViewModel] Starting async initialization...");
            _ = InitializeAsync();

            Console.WriteLine("[MenuViewModel] Constructor - END");
        }

        private async Task InitializeAsync()
        {
            Console.WriteLine("[MenuViewModel] InitializeAsync - START");
            IsLoading = true;

            try
            {
                Console.WriteLine("[MenuViewModel] InitializeAsync - Getting system names in background...");
                // Get system names in background
                var systemNames = await Task.Run(() =>
                {
                    Console.WriteLine("[MenuViewModel] InitializeAsync - Background task started");
                    var names = _systemManager.SystemNames.ToList();
                    Console.WriteLine($"[MenuViewModel] InitializeAsync - Background task completed with {names.Count} systems");
                    return names;
                });

                Console.WriteLine("[MenuViewModel] InitializeAsync - Updating UI on UI thread...");
                // Update UI (we're already on UI thread after await)
                SystemTabs.Clear();
                Console.WriteLine("[MenuViewModel] InitializeAsync - SystemTabs cleared");

                foreach (var systemName in systemNames)
                {
                    Console.WriteLine($"[MenuViewModel] InitializeAsync - Adding system tab: {systemName}");
                    SystemTabs.Add(new SystemTabViewModel
                    {
                        SystemName = systemName,
                        IsSelected = false
                    });
                }

                // Select first system by default
                if (SystemTabs.Any())
                {
                    Console.WriteLine($"[MenuViewModel] InitializeAsync - Selecting first system: {SystemTabs[0].SystemName}");
                    SelectedSystemName = SystemTabs[0].SystemName;
                }
                else
                {
                    Console.WriteLine("[MenuViewModel] InitializeAsync - No systems to select");
                }

                // Update selection state
                Console.WriteLine("[MenuViewModel] InitializeAsync - Updating system tab selection...");
                UpdateSystemTabSelection();

                IsLoading = false;
                Console.WriteLine("[MenuViewModel] InitializeAsync - IsLoading set to false");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MenuViewModel] Initialization error: {ex.Message}");
                Console.WriteLine($"[MenuViewModel] Initialization stack trace: {ex.StackTrace}");
                IsLoading = false;
            }

            Console.WriteLine("[MenuViewModel] InitializeAsync - END");
        }

        private void OnSelectSystem(string? systemName)
        {
            if (!string.IsNullOrEmpty(systemName))
            {
                SelectedSystemName = systemName;
                UpdateSystemTabSelection();
                NotifyUserInteraction();
            }
        }

        private void OnSelectCategory(string? categoryId)
        {
            if (!string.IsNullOrEmpty(categoryId))
            {
                SelectedCategoryId = categoryId;
                NotifyUserInteraction();
            }
        }

        private void UpdateCategorySelection()
        {
            foreach (var category in Categories)
            {
                category.IsSelected = category.CategoryId == SelectedCategoryId;
            }
        }


        private void UpdateSystemTabSelection()
        {
            foreach (var tab in SystemTabs)
            {
                tab.IsSelected = tab.SystemName == SelectedSystemName;
            }
        }

        private void OnMenuItemsUpdated(object? sender, MenuItemsUpdatedEventArgs e)
        {
            // Update system names if changed
            var currentSystems = _systemManager.SystemNames.ToList();
            var existingSystemNames = SystemTabs.Select(t => t.SystemName).ToList();
            if (!existingSystemNames.SequenceEqual(currentSystems))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SystemTabs.Clear();
                    foreach (var systemName in currentSystems)
                    {
                        SystemTabs.Add(new SystemTabViewModel
                        {
                            SystemName = systemName,
                            IsSelected = systemName == SelectedSystemName
                        });
                    }
                    UpdateSystemTabSelection();
                });
            }

            // Update menu items for selected system
            if (!string.IsNullOrEmpty(SelectedSystemName))
            {
                LoadMenuForSelectedSystem(e.MenuItemsBySystem);
            }
        }

        private void LoadMenuForSelectedSystem(Dictionary<string, List<MenuItem>>? menuItemsBySystem = null)
        {
            Console.WriteLine($"[MenuViewModel] LoadMenuForSelectedSystem - START (SelectedSystemName='{SelectedSystemName}')");

            if (string.IsNullOrEmpty(SelectedSystemName))
            {
                Console.WriteLine("[MenuViewModel] LoadMenuForSelectedSystem - SelectedSystemName is empty, clearing data");
                _allMenuItemsForCurrentSystem.Clear();
                CategoryGroups.Clear();
                Categories.Clear();
                return;
            }

            List<MenuItem> items;
            if (menuItemsBySystem != null && menuItemsBySystem.TryGetValue(SelectedSystemName, out var cachedItems))
            {
                Console.WriteLine($"[MenuViewModel] LoadMenuForSelectedSystem - Using cached items ({cachedItems.Count} items)");
                // Use cached items from polling service - already on background thread
                items = cachedItems;
                UpdateMenuDisplay(items);
            }
            else
            {
                Console.WriteLine($"[MenuViewModel] LoadMenuForSelectedSystem - No cached items, calling LoadMenuItemsAsync...");
                // Need to fetch from system manager - do it asynchronously
                _ = LoadMenuItemsAsync(SelectedSystemName);
            }

            Console.WriteLine("[MenuViewModel] LoadMenuForSelectedSystem - END");
        }

        private async Task LoadMenuItemsAsync(string systemName)
        {
            Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - START (systemName='{systemName}')");

            try
            {
                Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - Fetching menu items in background...");
                // Fetch menu items in background
                var items = await Task.Run(() =>
                {
                    Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - Background task: Calling GetMenuItems...");
                    var result = _systemManager.GetMenuItems(systemName);
                    Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - Background task: GetMenuItems returned {result.Count} items");
                    return result;
                });

                Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - Background task completed, updating UI...");
                // Update UI (we're already on UI thread after await)
                // Only update if we're still on the same system
                if (SelectedSystemName == systemName)
                {
                    Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - System still selected, updating display...");
                    UpdateMenuDisplay(items);
                }
                else
                {
                    Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - System changed, skipping display update");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MenuViewModel] Error loading menu items: {ex.Message}");
                Console.WriteLine($"[MenuViewModel] Error stack trace: {ex.StackTrace}");
            }

            Console.WriteLine($"[MenuViewModel] LoadMenuItemsAsync - END");
        }

        private void UpdateMenuDisplay(List<MenuItem> items)
        {
            // Store all items for current system
            _allMenuItemsForCurrentSystem = items;

            // Load categories
            LoadCategories();

            // Filter and display items
            FilterMenuItemsByCategory();
        }

        private void LoadCategories()
        {
            Categories.Clear();

            // Add "All" category
            Categories.Add(new MenuCategoryViewModel
            {
                CategoryId = "ALL",
                CategoryName = "All Items",
                IsSelected = SelectedCategoryId == "ALL"
            });

            // Get unique categories from menu items
            var uniqueCategories = _allMenuItemsForCurrentSystem
                .GroupBy(item => new { item.CategoryId, item.CategoryName })
                .Select(g => new MenuCategoryViewModel
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    IsSelected = SelectedCategoryId == g.Key.CategoryId
                })
                .OrderBy(c => c.CategoryName);

            foreach (var category in uniqueCategories)
            {
                Categories.Add(category);
            }

            // Select "All" if no category selected or selected category doesn't exist
            if (!Categories.Any(c => c.CategoryId == SelectedCategoryId))
            {
                SelectedCategoryId = "ALL";
            }
        }

        private void FilterMenuItemsByCategory()
        {
            CategoryGroups.Clear();

            if (SelectedCategoryId == "ALL")
            {
                // Group all items by category and display all groups
                var grouped = _allMenuItemsForCurrentSystem
                    .GroupBy(item => new { item.CategoryId, item.CategoryName })
                    .OrderBy(g => g.Key.CategoryName);

                foreach (var group in grouped)
                {
                    CategoryGroups.Add(new MenuCategoryGroup
                    {
                        CategoryId = group.Key.CategoryId,
                        CategoryName = group.Key.CategoryName,
                        Items = new ObservableCollection<MenuItem>(group)
                    });
                }
            }
            else
            {
                // Show only the selected category as a single group
                var items = _allMenuItemsForCurrentSystem
                    .Where(item => item.CategoryId == SelectedCategoryId)
                    .ToList();

                if (items.Any())
                {
                    CategoryGroups.Add(new MenuCategoryGroup
                    {
                        CategoryId = SelectedCategoryId,
                        CategoryName = items.First().CategoryName,
                        Items = new ObservableCollection<MenuItem>(items)
                    });
                }
            }
        }

        private void OnSelectMenuItem(MenuItem? menuItem)
        {
            if (menuItem == null || !menuItem.IsAvailable)
                return;

            // Create and show modal
            ModalViewModel = new MenuDetailModalViewModel();
            ModalViewModel.Initialize(menuItem, SelectedSystemName, OnAddToCart, OnCloseModal);
            IsModalVisible = true;
            NotifyUserInteraction();
        }

        private void OnAddToCart(CartItem cartItem)
        {
            // Check if same item with same options already exists
            var existingItem = CartItems.FirstOrDefault(item =>
                item.SystemName == cartItem.SystemName &&
                item.MenuItem.MenuId == cartItem.MenuItem.MenuId &&
                AreOptionsEqual(item.SelectedOptions, cartItem.SelectedOptions));

            if (existingItem != null)
            {
                // Increment quantity of existing item
                existingItem.Quantity += cartItem.Quantity;
            }
            else
            {
                // Add new item to cart
                CartItems.Add(cartItem);
            }

            UpdateCartTotals();
        }

        private bool AreOptionsEqual(List<MenuOption> options1, List<MenuOption> options2)
        {
            if (options1.Count != options2.Count)
                return false;

            var optionIds1 = options1.Select(o => o.OptionId).OrderBy(id => id).ToList();
            var optionIds2 = options2.Select(o => o.OptionId).OrderBy(id => id).ToList();

            return optionIds1.SequenceEqual(optionIds2);
        }

        private void OnCloseModal()
        {
            IsModalVisible = false;
            ModalViewModel = null;
        }

        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(CartItemCount));
            OnPropertyChanged(nameof(CartTotalPrice));
            OnPropertyChanged(nameof(HasCartItems));
        }

        public void RemoveCartItem(CartItem item)
        {
            CartItems.Remove(item);
            UpdateCartTotals();
        }

        public void ClearCart()
        {
            CartItems.Clear();
            UpdateCartTotals();
        }

        private void OnIncreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null)
                return;

            cartItem.Quantity++;
            UpdateCartTotals();
            NotifyUserInteraction();
        }

        private void OnDecreaseQuantity(CartItem? cartItem)
        {
            if (cartItem == null)
                return;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCartTotals();
            }
            else
            {
                // If quantity would become 0, remove the item
                RemoveCartItem(cartItem);
            }
            NotifyUserInteraction();
        }

        private void OnRemoveFromCart(CartItem? cartItem)
        {
            if (cartItem == null)
                return;

            RemoveCartItem(cartItem);
            NotifyUserInteraction();
        }

        private void OnCheckout()
        {
            if (CartItems.Count == 0)
                return;

            // Stop inactivity timer during checkout
            _inactivityTimer.Stop();

            // Navigate to payment screen
            _onNavigateToPayment?.Invoke();
        }

        private void OnStartOver()
        {
            // Clear cart and navigate back to welcome screen
            ClearCart();
            _inactivityTimer.Stop();
            _onNavigateToWelcome?.Invoke();
        }

        private void OnContinueOrder()
        {
            IsTimeoutWarningVisible = false;
            ResetInactivityTimer();
        }

        private void OnQuitOrder()
        {
            IsTimeoutWarningVisible = false;
            OnStartOver();
        }

        private void ResetInactivityTimer()
        {
            RemainingSeconds = _inactivityTimeoutSeconds;
            IsTimeoutWarningVisible = false;
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }

        private void OnInactivityTimerTick(object? sender, EventArgs e)
        {
            RemainingSeconds--;

            if (RemainingSeconds <= 0)
            {
                // Time's up - automatically return to welcome
                _inactivityTimer.Stop();
                OnStartOver();
            }
            else if (RemainingSeconds <= _warningThresholdSeconds && !IsTimeoutWarningVisible)
            {
                // Show warning modal
                IsTimeoutWarningVisible = true;
            }
        }

        public void NotifyUserInteraction()
        {
            // Reset timer on any user interaction
            ResetInactivityTimer();
        }

        public void Cleanup()
        {
            // Stop timers and unsubscribe from polling events
            _inactivityTimer.Stop();
            _pollingService.MenuItemsUpdated -= OnMenuItemsUpdated;
            // Note: Polling service is managed at app level, not stopped here
        }
    }

    /// <summary>
    /// ViewModel for a system tab
    /// </summary>
    public class SystemTabViewModel : BaseViewModel
    {
        private bool _isSelected;

        public string SystemName { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ViewModel for a menu category tab
    /// </summary>
    public class MenuCategoryViewModel : BaseViewModel
    {
        private bool _isSelected;

        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ViewModel for a category group with its menu items
    /// </summary>
    public class MenuCategoryGroup : BaseViewModel
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public ObservableCollection<MenuItem> Items { get; set; } = new();
    }
}
