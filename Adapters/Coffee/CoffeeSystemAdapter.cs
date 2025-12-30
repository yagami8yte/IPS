using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Adapters.Coffee
{
    /// <summary>
    /// Adapter for Folletto coffee kiosk system
    /// Wraps FollettoKioskApi.dll and implements IUnmannedSystem interface
    /// </summary>
    public class CoffeeSystemAdapter : IUnmannedSystem
    {
        private readonly string _boothId;
        private readonly string _systemName;
        private bool _isInitialized;
        private bool _isConnected;

        public string SystemName => _systemName;

        /// <summary>
        /// Create coffee system adapter with configuration
        /// </summary>
        /// <param name="systemName">Display name for this system (e.g., "Coffee")</param>
        /// <param name="boothId">Unique booth identifier for the DLL</param>
        /// <param name="serverIp">DLL server IP address</param>
        /// <param name="serverPort">DLL server port</param>
        /// <param name="boothIp">Booth (kiosk) IP address</param>
        /// <param name="boothPort">Booth (kiosk) port</param>
        public CoffeeSystemAdapter(
            string systemName,
            string boothId,
            string serverIp,
            int serverPort,
            string boothIp,
            int boothPort)
        {
            _systemName = systemName;
            _boothId = boothId;

            InitializeConnection(serverIp, serverPort, boothIp, boothPort);
        }

        private void InitializeConnection(string serverIp, int serverPort, string boothIp, int boothPort)
        {
            try
            {
                Console.WriteLine($"[CoffeeSystemAdapter] Attempting to initialize DLL internal HTTP server on port {serverPort}");

                // Step 1: Initialize DLL's internal HTTP server
                // Returns: 0=Success, 1=InvalidPort, 2=PortUnavailable, 3=InternalError
                int initResult = FollettoKioskInterop.initializeServer(serverPort);
                Console.WriteLine($"[CoffeeSystemAdapter] DLL server initialization result: {initResult} (0=Success)");

                if (initResult != 0)
                {
                    string errorMsg = initResult switch
                    {
                        1 => "Invalid port number",
                        2 => "Port unavailable or already in use",
                        3 => "Internal error",
                        _ => $"Unknown error code: {initResult}"
                    };
                    throw new Exception($"Failed to initialize FollettoKioskApi server: {errorMsg}");
                }

                _isInitialized = true;
                Console.WriteLine($"[CoffeeSystemAdapter] Attempting to connect to booth at {boothIp}:{boothPort}");

                // Step 2: Connect to booth
                var boothAddresses = new[]
                {
                    new FollettoKioskInterop.IPAddressPort
                    {
                        ipAddress = boothIp,
                        port = (ushort)boothPort
                    }
                };

                // Returns: 0=Success, see documentation for other error codes
                int connectResult = FollettoKioskInterop.connectToBooths(boothAddresses, 1);
                Console.WriteLine($"[CoffeeSystemAdapter] Booth connection result: {connectResult} (0=Success)");

                if (connectResult != 0)
                {
                    string errorMsg = connectResult switch
                    {
                        1 => "Invalid IP address",
                        2 => "Invalid port",
                        3 => "IP address not available",
                        4 => "Port unavailable",
                        5 => "Connection timeout",
                        6 => "Network unreachable",
                        7 => "Connection refused",
                        8 => "Resource limitation",
                        9 => "Network protocol error",
                        10 => "Internal error",
                        11 => "Security restrictions",
                        12 => "Host name resolution failure",
                        13 => "Socket initialization failure",
                        _ => $"Unknown error code: {connectResult}"
                    };
                    throw new Exception($"Failed to connect to booth: {errorMsg}");
                }

                _isConnected = true;
                Console.WriteLine($"[CoffeeSystemAdapter] ✓ Successfully connected to system '{_systemName}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] ✗ Connection error: {ex.Message}");
                Console.WriteLine($"[CoffeeSystemAdapter] Stack trace: {ex.StackTrace}");
                _isInitialized = false;
                _isConnected = false;
            }
        }

        public List<MenuItem> GetMenuItems()
        {
            if (!_isConnected)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] GetMenuItems called but not connected (isInitialized={_isInitialized}, isConnected={_isConnected})");
                return new List<MenuItem>();
            }

            try
            {
                // Get JSON string from DLL
                IntPtr jsonPtr = FollettoKioskInterop.getProductStatusWrapped();
                string? json = FollettoKioskInterop.MarshalString(jsonPtr);

                Console.WriteLine($"[CoffeeSystemAdapter] Received JSON from DLL: {(string.IsNullOrEmpty(json) ? "NULL/EMPTY" : $"{json.Length} characters")}");

                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"[CoffeeSystemAdapter] No JSON data received from DLL");
                    return new List<MenuItem>();
                }

                // Parse JSON into menu items
                var items = ParseMenuItems(json);
                Console.WriteLine($"[CoffeeSystemAdapter] Parsed {items.Count} menu items");
                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] GetMenuItems error: {ex.Message}");
                Console.WriteLine($"[CoffeeSystemAdapter] Stack trace: {ex.StackTrace}");
                return new List<MenuItem>();
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

                int itemIndex = 0;
                foreach (JsonElement productElement in products.EnumerateArray())
                {
                    try
                    {
                        // Log first item's raw JSON for debugging
                        if (itemIndex == 0)
                        {
                            Console.WriteLine($"[CoffeeSystemAdapter] First product raw JSON: {productElement.GetRawText()}");
                        }

                        string menuId = GetStringProperty(productElement, "menuId");
                        string name = GetStringProperty(productElement, "alias");
                        decimal price = GetDecimalProperty(productElement, "price");
                        bool availability = GetBoolProperty(productElement, "availability");

                        Console.WriteLine($"[CoffeeSystemAdapter] Parsing item #{itemIndex}: {name}, Price: {price}, Available: {availability}");
                        itemIndex++;

                        var menuItem = new MenuItem
                        {
                            MenuId = menuId,
                            Name = name,  // DLL uses "alias" not "name"
                            Description = GetStringProperty(productElement, "description"),
                            Price = price,
                            PriceUnit = GetStringProperty(productElement, "priceUnit"),
                            ImagePath = GetStringProperty(productElement, "menuImage"),  // DLL uses "menuImage" not "imagePath"
                            IsAvailable = availability,  // DLL uses "availability" not "isAvailable"
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
                        Console.WriteLine($"[CoffeeSystemAdapter] Error parsing menu item: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] JSON parsing error: {ex.Message}");
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
                    // Only include enabled options
                    bool isEnabled = GetBoolProperty(optionElement, "isEnabled");
                    if (!isEnabled)
                        continue;

                    var option = new MenuOption
                    {
                        OptionId = GetStringProperty(optionElement, "optionId"),
                        Name = GetStringProperty(optionElement, "alias"),  // DLL uses "alias" not "name"
                        Price = GetDecimalProperty(optionElement, "price"),
                        OptionCategoryId = GetStringProperty(optionElement, "optionCategoryId"),
                        ViewIndex = GetIntProperty(optionElement, "viewIndex")
                    };

                    options.Add(option);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CoffeeSystemAdapter] Error parsing option: {ex.Message}");
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
                        // Already an object (shouldn't happen based on actual data, but handle it)
                        sourceElement = kioskElement;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // Direct format: extraInformation contains baseMenu, menuTemperature directly
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
                        Console.WriteLine($"[CoffeeSystemAdapter] Parsed variant: {menuItem.Name} -> BaseMenuId={menuItem.BaseMenuId}, Alias={menuItem.BaseMenuAlias}, Temp={menuItem.MenuTemperature}, Variant={menuItem.MenuVariant}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] Error parsing extraInformation: {ex.Message}");
            }
        }

        public bool SendOrder(OrderInfo order)
        {
            Console.WriteLine($"[CoffeeSystemAdapter] SendOrder called for system '{_systemName}'");
            Console.WriteLine($"[CoffeeSystemAdapter]   IsConnected: {_isConnected}");

            if (!_isConnected)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] SendOrder FAILED - Not connected");
                return false;
            }

            try
            {
                // Create invoice with all required payment fields
                // Based on working KioskFolletto codebase
                var invoice = new FollettoKioskInterop.Invoice
                {
                    orderId = order.OrderId ?? Guid.NewGuid().ToString(),
                    orderLabel = order.OrderLabel ?? "A-001",
                    qrData = order.QrData ?? "",
                    invoiceNo = "",
                    refNo = "",
                    purchase = order.TotalAmount.ToString("F2"),  // STRING, not double!
                    authCode = "",
                    acqRefData = "",
                    processData = "",
                    recordNo = "",
                    tranDeviceId = "",
                    DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };

                Console.WriteLine($"[CoffeeSystemAdapter] Invoice:");
                Console.WriteLine($"[CoffeeSystemAdapter]   OrderId: '{invoice.orderId}'");
                Console.WriteLine($"[CoffeeSystemAdapter]   OrderLabel: '{invoice.orderLabel}'");
                Console.WriteLine($"[CoffeeSystemAdapter]   Purchase: '{invoice.purchase}'");
                Console.WriteLine($"[CoffeeSystemAdapter]   QrData: '{(string.IsNullOrEmpty(invoice.qrData) ? "(empty)" : invoice.qrData)}'");
                Console.WriteLine($"[CoffeeSystemAdapter]   DateTime: '{invoice.DateTime}'");

                // Create products array
                // Product struct has no quantity field - add each item multiple times for quantity
                var products = new List<FollettoKioskInterop.Product>();

                Console.WriteLine($"[CoffeeSystemAdapter] Processing {order.Items.Count} unique order items:");
                int totalProductCount = 0;

                foreach (var item in order.Items)
                {
                    Console.WriteLine($"[CoffeeSystemAdapter]   Item: MenuId='{item.MenuId}', Quantity={item.Quantity}, Options={item.SelectedOptionIds?.Count ?? 0}");

                    // Convert option IDs to array
                    string[] optionIds = item.SelectedOptionIds?.ToArray() ?? Array.Empty<string>();

                    // Add product to array once for each quantity (Product struct has no quantity field)
                    for (int q = 0; q < item.Quantity; q++)
                    {
                        products.Add(new FollettoKioskInterop.Product
                        {
                            menuId = item.MenuId ?? "",
                            menuAliasCulture = "en",  // Default to English
                            options = FollettoKioskInterop.ConvertStringArrayToUnmanaged(optionIds),
                            numberOfOptions = optionIds.Length.ToString()
                        });
                        totalProductCount++;
                    }
                }

                Console.WriteLine($"[CoffeeSystemAdapter] Total products in array (with quantity expansion): {totalProductCount}");

                // Place order
                Console.WriteLine($"[CoffeeSystemAdapter] Platform info:");
                Console.WriteLine($"[CoffeeSystemAdapter]   IntPtr size: {IntPtr.Size} bytes ({(IntPtr.Size == 8 ? "64-bit" : "32-bit")})");
                Console.WriteLine($"[CoffeeSystemAdapter]   Process: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

                int invoiceSize = System.Runtime.InteropServices.Marshal.SizeOf<FollettoKioskInterop.Invoice>();
                int productSize = System.Runtime.InteropServices.Marshal.SizeOf<FollettoKioskInterop.Product>();
                int orderResultSize = System.Runtime.InteropServices.Marshal.SizeOf<FollettoKioskInterop.OrderResult>();

                Console.WriteLine($"[CoffeeSystemAdapter] Struct sizes:");
                Console.WriteLine($"[CoffeeSystemAdapter]   Invoice size: {invoiceSize} bytes");
                Console.WriteLine($"[CoffeeSystemAdapter]   Product size: {productSize} bytes");
                Console.WriteLine($"[CoffeeSystemAdapter]   OrderResult size: {orderResultSize} bytes");

                Console.WriteLine($"[CoffeeSystemAdapter] Calling DLL placeOrderWithInvoice with {products.Count} products...");
                Console.WriteLine($"[CoffeeSystemAdapter] About to call native DLL function...");

                // Pattern from working KioskFolletto codebase
                FollettoKioskInterop.Product[] productsArray = products.ToArray();
                string numberOfProducts = productsArray.Length.ToString();

                FollettoKioskInterop.OrderResult result = FollettoKioskInterop.placeOrderWithInvoice(ref invoice, productsArray, numberOfProducts);

                Console.WriteLine($"[CoffeeSystemAdapter] DLL call completed successfully");

                // Free unmanaged memory for options
                foreach (var product in productsArray)
                {
                    if (product.options != IntPtr.Zero && int.TryParse(product.numberOfOptions, out int count))
                    {
                        FollettoKioskInterop.FreeUnmanagedStringArray(product.options, count);
                    }
                }

                Console.WriteLine($"[CoffeeSystemAdapter] Result from DLL:");
                Console.WriteLine($"[CoffeeSystemAdapter]   ErrorCode: {result.errorCode}");
                Console.WriteLine($"[CoffeeSystemAdapter]   BoothName: {result.boothName}");

                if (result.errorCode != FollettoKioskInterop.OrderRegistrationErrorCode.Success)
                {
                    Console.WriteLine($"[CoffeeSystemAdapter] ✗ Order FAILED with error code {result.errorCode}");
                    Console.WriteLine($"[CoffeeSystemAdapter]   Booth: {result.boothName}");
                    return false;
                }

                Console.WriteLine($"[CoffeeSystemAdapter] ✓ Order placed successfully to booth: {result.boothName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoffeeSystemAdapter] ✗✗✗ SendOrder EXCEPTION ✗✗✗");
                Console.WriteLine($"[CoffeeSystemAdapter] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[CoffeeSystemAdapter] Exception Message: {ex.Message}");
                Console.WriteLine($"[CoffeeSystemAdapter] Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[CoffeeSystemAdapter] Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public SystemStatus GetStatus()
        {
            if (!_isConnected)
            {
                return new SystemStatus
                {
                    IsOnline = false,
                    IsAvailable = false,
                    WaitingOrdersCount = 0,
                    EstimatedWaitingTimeSeconds = 0
                };
            }

            // Return basic status without calling DLL functions that may timeout
            return new SystemStatus
            {
                IsOnline = true,
                IsAvailable = true,
                WaitingOrdersCount = 0,  // Not fetching to avoid timeout
                EstimatedWaitingTimeSeconds = 0  // Not fetching to avoid timeout
            };
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

        public void Dispose()
        {
            // Note: No shutdown function exists in this DLL version
            // DLL manages its own resources
            _isInitialized = false;
            _isConnected = false;
        }
    }
}
