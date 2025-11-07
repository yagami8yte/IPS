using System;
using System.Collections.Generic;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Adapters.Coffee
{
    /// <summary>
    /// Dummy Coffee System Adapter for testing and development
    /// Provides sample menu data without requiring actual DLL connection
    /// </summary>
    public class DummyCoffeeSystemAdapter : IUnmannedSystem
    {
        public string SystemName => "Coffee";

        private readonly List<MenuItem> _menuItems;
        private readonly Random _random = new Random();

        public DummyCoffeeSystemAdapter()
        {
            _menuItems = GenerateSampleMenuItems();
        }

        public List<MenuItem> GetMenuItems()
        {
            // Return static menu items with consistent availability
            return new List<MenuItem>(_menuItems);
        }

        public bool SendOrder(OrderInfo order)
        {
            // Simulate order processing
            Console.WriteLine($"[Coffee System] Received order {order.OrderId} with {order.Items.Count} items");

            // Simulate 95% success rate
            bool success = _random.Next(100) < 95;

            if (success)
            {
                Console.WriteLine($"[Coffee System] Order {order.OrderId} accepted successfully");
            }
            else
            {
                Console.WriteLine($"[Coffee System] Order {order.OrderId} rejected - items unavailable");
            }

            return success;
        }

        public SystemStatus GetStatus()
        {
            return new SystemStatus
            {
                IsOnline = true,
                IsAvailable = true,
                WaitingOrdersCount = _random.Next(0, 5),
                EstimatedWaitingTimeSeconds = _random.Next(60, 300),
                LastUpdated = DateTime.Now,
                ErrorMessage = null,
                AdditionalInfo = "Dummy Coffee System - Development Mode"
            };
        }

        private List<MenuItem> GenerateSampleMenuItems()
        {
            var items = new List<MenuItem>();

            // Hot Coffee Category
            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Hot Americano",
                Description = "Classic espresso with hot water",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-hot-coffee",
                CategoryName = "Coffee (Hot)",
                Options = new List<MenuOption>
                {
                    new MenuOption
                    {
                        OptionId = "opt-strength-light",
                        OptionCategoryId = "cat-strength",
                        Name = "Light",
                        CategoryName = "Strength",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 0
                    },
                    new MenuOption
                    {
                        OptionId = "opt-strength-regular",
                        OptionCategoryId = "cat-strength",
                        Name = "Regular",
                        CategoryName = "Strength",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 1
                    },
                    new MenuOption
                    {
                        OptionId = "opt-strength-strong",
                        OptionCategoryId = "cat-strength",
                        Name = "Strong",
                        CategoryName = "Strength",
                        Price = 0.50m,
                        IsEnabled = true,
                        ViewIndex = 2
                    }
                }
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Hot Cafe Latte",
                Description = "Espresso with steamed milk",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-hot-coffee",
                CategoryName = "Coffee (Hot)",
                Options = new List<MenuOption>
                {
                    new MenuOption
                    {
                        OptionId = "opt-size-regular",
                        OptionCategoryId = "cat-size",
                        Name = "Regular",
                        CategoryName = "Size",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 0
                    },
                    new MenuOption
                    {
                        OptionId = "opt-size-large",
                        OptionCategoryId = "cat-size",
                        Name = "Large",
                        CategoryName = "Size",
                        Price = 1.00m,
                        IsEnabled = true,
                        ViewIndex = 1
                    }
                }
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Hot Cappuccino",
                Description = "Espresso with steamed milk foam",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_cappuccino.jpg",
                IsAvailable = true,
                CategoryId = "cat-hot-coffee",
                CategoryName = "Coffee (Hot)",
                Options = null // No options for cappuccino
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Hot Caramel Macchiato",
                Description = "Espresso with vanilla and caramel",
                Price = 5.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_caramel_macchiato.jpg",
                IsAvailable = true,
                CategoryId = "cat-hot-coffee",
                CategoryName = "Coffee (Hot)",
                Options = new List<MenuOption>
                {
                    new MenuOption
                    {
                        OptionId = "opt-extra-caramel",
                        OptionCategoryId = "cat-extra",
                        Name = "Extra Caramel",
                        CategoryName = "Add-ons",
                        Price = 0.50m,
                        IsEnabled = true,
                        ViewIndex = 0
                    },
                    new MenuOption
                    {
                        OptionId = "opt-extra-shot",
                        OptionCategoryId = "cat-extra",
                        Name = "Extra Shot",
                        CategoryName = "Add-ons",
                        Price = 1.00m,
                        IsEnabled = true,
                        ViewIndex = 1
                    }
                }
            });

            // Iced Coffee Category
            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Iced Americano",
                Description = "Classic espresso with cold water and ice",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-iced-coffee",
                CategoryName = "Coffee (Iced)",
                Options = new List<MenuOption>
                {
                    new MenuOption
                    {
                        OptionId = "opt-ice-regular",
                        OptionCategoryId = "cat-ice",
                        Name = "Regular Ice",
                        CategoryName = "Ice Amount",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 0
                    },
                    new MenuOption
                    {
                        OptionId = "opt-ice-less",
                        OptionCategoryId = "cat-ice",
                        Name = "Less Ice",
                        CategoryName = "Ice Amount",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 1
                    }
                }
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Iced Cafe Latte",
                Description = "Espresso with cold milk and ice",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-iced-coffee",
                CategoryName = "Coffee (Iced)",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Iced Vanilla Latte",
                Description = "Espresso with vanilla and cold milk",
                Price = 5.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_vanilla_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-iced-coffee",
                CategoryName = "Coffee (Iced)",
                Options = new List<MenuOption>
                {
                    new MenuOption
                    {
                        OptionId = "opt-vanilla-regular",
                        OptionCategoryId = "cat-vanilla",
                        Name = "Regular Vanilla",
                        CategoryName = "Vanilla Amount",
                        Price = 0.00m,
                        IsEnabled = true,
                        ViewIndex = 0
                    },
                    new MenuOption
                    {
                        OptionId = "opt-vanilla-extra",
                        OptionCategoryId = "cat-vanilla",
                        Name = "Extra Vanilla",
                        CategoryName = "Vanilla Amount",
                        Price = 0.50m,
                        IsEnabled = true,
                        ViewIndex = 1
                    }
                }
            });

            // Non-Coffee Category
            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Hot Chocolate",
                Description = "Rich chocolate milk with whipped cream",
                Price = 4.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_chocolate.jpg",
                IsAvailable = true,
                CategoryId = "cat-non-coffee",
                CategoryName = "Non-Coffee",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = Guid.NewGuid().ToString(),
                Name = "Green Tea Latte",
                Description = "Matcha green tea with steamed milk",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/green_tea_latte.jpg",
                IsAvailable = false, // Currently unavailable
                CategoryId = "cat-non-coffee",
                CategoryName = "Non-Coffee",
                Options = null
            });

            return items;
        }
    }
}
