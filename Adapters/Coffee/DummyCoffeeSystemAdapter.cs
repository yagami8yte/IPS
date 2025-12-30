using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Adapters.Coffee
{
    /// <summary>
    /// Dummy Coffee System Adapter for testing and development
    /// Loads menu data from menu_debug.json if available, otherwise generates sample data
    /// </summary>
    public class DummyCoffeeSystemAdapter : IUnmannedSystem
    {
        public string SystemName => "Coffee";

        private readonly List<MenuItem> _menuItems;
        private readonly Random _random = new Random();

        public DummyCoffeeSystemAdapter()
        {
            // Try to load from menu_debug.json first
            _menuItems = LoadFromDebugJson() ?? GenerateSampleMenuItems();
        }

        public List<MenuItem> GetMenuItems()
        {
            // Return static menu items with consistent availability
            return new List<MenuItem>(_menuItems);
        }

        private List<MenuItem>? LoadFromDebugJson()
        {
            try
            {
                // Look for menu_debug.json in various locations
                string[] possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "menu_debug.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "menu_debug.json"),
                    @"C:\Users\jinho\DEV\IPS\menu_debug.json"
                };

                string? jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        jsonPath = path;
                        break;
                    }
                }

                if (jsonPath == null)
                {
                    Console.WriteLine("[DummyCoffeeSystemAdapter] menu_debug.json not found, using generated sample data");
                    return null;
                }

                Console.WriteLine($"[DummyCoffeeSystemAdapter] Loading menu data from: {jsonPath}");
                string json = File.ReadAllText(jsonPath);

                return ParseMenuItems(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyCoffeeSystemAdapter] Error loading menu_debug.json: {ex.Message}");
                return null;
            }
        }

        private List<MenuItem> ParseMenuItems(string json)
        {
            var menuItems = new List<MenuItem>();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("products", out JsonElement products))
                    return menuItems;

                foreach (JsonElement productElement in products.EnumerateArray())
                {
                    try
                    {
                        string menuId = GetStringProperty(productElement, "menuId");
                        string name = GetStringProperty(productElement, "alias");
                        decimal price = GetDecimalProperty(productElement, "price");
                        bool availability = GetBoolProperty(productElement, "availability");

                        var menuItem = new MenuItem
                        {
                            MenuId = menuId,
                            Name = name,
                            Description = GetStringProperty(productElement, "description"),
                            Price = price,
                            PriceUnit = GetStringProperty(productElement, "priceUnit"),
                            ImagePath = GetStringProperty(productElement, "menuImage"),
                            IsAvailable = availability,
                            CategoryId = GetStringProperty(productElement, "categoryId"),
                            CategoryName = GetStringProperty(productElement, "categoryName"),
                            Options = ParseOptions(productElement)
                        };

                        // Parse extraInformation for variant properties
                        ParseExtraInformation(productElement, menuItem);

                        menuItems.Add(menuItem);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DummyCoffeeSystemAdapter] Error parsing menu item: {ex.Message}");
                    }
                }

                Console.WriteLine($"[DummyCoffeeSystemAdapter] Loaded {menuItems.Count} menu items from JSON");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyCoffeeSystemAdapter] JSON parsing error: {ex.Message}");
            }

            return menuItems;
        }

        private List<MenuOption>? ParseOptions(JsonElement productElement)
        {
            if (!productElement.TryGetProperty("options", out JsonElement optionsElement))
                return null;

            var options = new List<MenuOption>();

            foreach (JsonElement optionElement in optionsElement.EnumerateArray())
            {
                try
                {
                    bool isEnabled = GetBoolProperty(optionElement, "isEnabled");
                    if (!isEnabled)
                        continue;

                    var option = new MenuOption
                    {
                        OptionId = GetStringProperty(optionElement, "optionId"),
                        Name = GetStringProperty(optionElement, "alias"),
                        Price = GetDecimalProperty(optionElement, "price"),
                        OptionCategoryId = GetStringProperty(optionElement, "optionCategoryId"),
                        ViewIndex = GetIntProperty(optionElement, "viewIndex")
                    };

                    options.Add(option);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DummyCoffeeSystemAdapter] Error parsing option: {ex.Message}");
                }
            }

            return options.Count > 0 ? options : null;
        }

        private void ParseExtraInformation(JsonElement productElement, MenuItem menuItem)
        {
            try
            {
                if (!productElement.TryGetProperty("extraInformation", out JsonElement extraInfo))
                    return;

                // The Kiosk property contains a JSON STRING that needs to be parsed
                JsonElement sourceElement;

                if (extraInfo.TryGetProperty("Kiosk", out JsonElement kioskElement))
                {
                    // Kiosk is a JSON string - need to parse it
                    if (kioskElement.ValueKind == JsonValueKind.String)
                    {
                        string? kioskJsonString = kioskElement.GetString();
                        if (string.IsNullOrEmpty(kioskJsonString))
                            return;

                        // Parse the nested JSON string
                        using JsonDocument kioskDoc = JsonDocument.Parse(kioskJsonString);
                        sourceElement = kioskDoc.RootElement.Clone();
                    }
                    else if (kioskElement.ValueKind == JsonValueKind.Object)
                    {
                        sourceElement = kioskElement;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    sourceElement = extraInfo;
                }

                // Parse menuTemperature (hot/ice)
                menuItem.MenuTemperature = GetStringProperty(sourceElement, "menuTemperature");

                // Parse menuVariant (light/regular/extra)
                menuItem.MenuVariant = GetStringProperty(sourceElement, "menuVariant");

                // Parse baseMenu for grouping
                if (sourceElement.TryGetProperty("baseMenu", out JsonElement baseMenu))
                {
                    menuItem.BaseMenuId = GetStringProperty(baseMenu, "menuId");
                    menuItem.BaseMenuAlias = GetStringProperty(baseMenu, "menuAlias");

                    if (!string.IsNullOrEmpty(menuItem.BaseMenuId))
                    {
                        Console.WriteLine($"[DummyCoffeeSystemAdapter] Parsed variant: {menuItem.Name} -> BaseMenuId={menuItem.BaseMenuId}, Alias={menuItem.BaseMenuAlias}, Temp={menuItem.MenuTemperature}, Variant={menuItem.MenuVariant}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyCoffeeSystemAdapter] Error parsing extraInformation: {ex.Message}");
            }
        }

        #region JSON Helper Methods

        private static string GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? string.Empty
                : string.Empty;
        }

        private static decimal GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDecimal();
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out decimal value))
                    return value;
            }
            return 0m;
        }

        private static int GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int value))
                    return value;
            }
            return 0;
        }

        private static bool GetBoolProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement prop) &&
                   (prop.ValueKind == JsonValueKind.True ||
                    (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out bool value) && value));
        }

        #endregion

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

            // ===== Americano Variant Group (demonstrates temperature + variant grouping) =====
            // Base ID for grouping: "base-americano"

            items.Add(new MenuItem
            {
                MenuId = "americano-hot-regular",
                Name = "Hot Americano (Regular)",
                Description = "Classic espresso with hot water",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "hot",
                MenuVariant = "regular",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = "americano-hot-light",
                Name = "Hot Americano (Light)",
                Description = "Light espresso with hot water",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "hot",
                MenuVariant = "light",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = "americano-hot-extra",
                Name = "Hot Americano (Extra)",
                Description = "Strong espresso with hot water",
                Price = 4.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "hot",
                MenuVariant = "extra",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = "americano-ice-regular",
                Name = "Iced Americano (Regular)",
                Description = "Classic espresso with cold water and ice",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "ice",
                MenuVariant = "regular",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = "americano-ice-light",
                Name = "Iced Americano (Light)",
                Description = "Light espresso with cold water and ice",
                Price = 3.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "ice",
                MenuVariant = "light",
                Options = null
            });

            items.Add(new MenuItem
            {
                MenuId = "americano-ice-extra",
                Name = "Iced Americano (Extra)",
                Description = "Strong espresso with cold water and ice",
                Price = 4.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_americano.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-americano",
                BaseMenuAlias = "Americano",
                MenuTemperature = "ice",
                MenuVariant = "extra",
                Options = null
            });

            // ===== Cafe Latte Variant Group (temperature only) =====
            // Base ID for grouping: "base-latte"

            items.Add(new MenuItem
            {
                MenuId = "latte-hot",
                Name = "Hot Cafe Latte",
                Description = "Espresso with steamed milk",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-latte",
                BaseMenuAlias = "Cafe Latte",
                MenuTemperature = "hot",
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
                MenuId = "latte-ice",
                Name = "Iced Cafe Latte",
                Description = "Espresso with cold milk and ice",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                BaseMenuId = "base-latte",
                BaseMenuAlias = "Cafe Latte",
                MenuTemperature = "ice",
                Options = null
            });

            // ===== Standalone Menu Items (no variants) =====

            items.Add(new MenuItem
            {
                MenuId = "cappuccino-hot",
                Name = "Hot Cappuccino",
                Description = "Espresso with steamed milk foam",
                Price = 4.50m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_cappuccino.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
                Options = null // No options for cappuccino
            });

            items.Add(new MenuItem
            {
                MenuId = "caramel-macchiato-hot",
                Name = "Hot Caramel Macchiato",
                Description = "Espresso with vanilla and caramel",
                Price = 5.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/hot_caramel_macchiato.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
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

            items.Add(new MenuItem
            {
                MenuId = "vanilla-latte-ice",
                Name = "Iced Vanilla Latte",
                Description = "Espresso with vanilla and cold milk",
                Price = 5.00m,
                PriceUnit = "USD",
                ImagePath = "pack://application:,,,/Assets/Menu/iced_vanilla_latte.jpg",
                IsAvailable = true,
                CategoryId = "cat-coffee",
                CategoryName = "Coffee",
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
                MenuId = "hot-chocolate",
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
                MenuId = "green-tea-latte",
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
