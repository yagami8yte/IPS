# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

IPS is a WPF desktop application built with .NET 8.0, targeting Windows. IPS stands for Integrated POS Solution for Multi-Kiosk Linkage - a Point of Sale system designed for managing multiple interconnected kiosks.

## Technology Stack

- **Framework**: .NET 8.0 (Windows-specific WPF application)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **MVVM Library**: CommunityToolkit.Mvvm (version 8.4.0)
- **Architecture**: MVVM (Model-View-ViewModel)

## Build and Run Commands

```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Architecture Overview

The application follows **Clean Architecture** principles with clear separation of concerns and dependency inversion. The architecture is designed to support multiple kiosk systems (Coffee, Food, etc.) through a unified interface.

```
UI Layer (MainApp)
    ↓ uses
Business Logic (Services)
    ↓ manages/calls
Data & Hardware Abstraction (Adapters)
    ↓ wraps
External Systems (DLLs, Payment Gateway, Printers)
```

All layers depend on abstractions defined in the **Core** layer (interfaces and models).

## Project Structure

### MainApp/ - UI Layer
WPF application layer with MVVM pattern:
- **ViewModels/**: Presentation logic
  - `BaseViewModel.cs`: Base class with `INotifyPropertyChanged` implementation
  - `MainViewModel.cs`: Root ViewModel managing view navigation via `CurrentViewModel` property
  - `WelcomeViewModel.cs`: Welcome screen with video ad functionality
  - `MenuViewModel.cs`: Main menu interface
- **Views/**: XAML UserControls bound to ViewModels
  - `WelcomeView.xaml`: Entry screen with "Start" button
  - `MenuView.xaml`: Main menu screen
  - `AdminView.xaml`: Admin interface (has direct access to Config/Log services)
- **MainWindow.xaml**: Shell window using `ContentControl` for view composition
- **Models/**: UI-specific models (if needed)

### Services/ - Business Logic Layer
Service layer implementing business logic with Dependency Injection:
- `SystemManagerService`: Manages all unmanned system adapters (IUnmannedSystem implementations)
- `SystemPollingService`: Background service that continuously polls SystemManagerService to check system status
- `PaymentService`: Implements IPaymentService, interfaces with Payment Gateway
- `PrintingService`: Implements IPrintingService, interfaces with Receipt Printer
- Config/Log Services: (To be implemented) Configuration management and logging

**Note**: ViewModels consume these services. Admin ViewModels have direct access to Config/Log services.

### Adapters/ - Hardware Abstraction Layer
Adapter pattern implementations that wrap external system DLLs:
- Each system has its own folder: `Adapters/{SystemName}/`
- Adapter classes: `{SystemName}SystemAdapter.cs` (e.g., `CoffeeSystemAdapter.cs`)
- All adapters implement `IUnmannedSystem` interface from Core
- External DLLs are placed in: `Adapters/{SystemName}/actual_dll.dll`

**Example structure:**
```
Adapters/
  Coffee/
    CoffeeSystemAdapter.cs
    coffee_machine.dll
  Food/
    FoodSystemAdapter.cs
    food_dispenser.dll
```

### Core/ - Domain Layer
Core abstractions and domain models (no dependencies on other layers):
- **Interfaces/**:
  - `IUnmannedSystem.cs`: Base interface for all kiosk system adapters
    - `string SystemName { get; }`: System identifier
    - `List<MenuItem> GetMenuItems()`: Poll menu items and availability (recommended interval: 1 second)
    - `bool SendOrder(OrderInfo order)`: Submit order to system
    - `SystemStatus GetStatus()`: Get current system status
  - `IPaymentService.cs`: Payment processing operations
  - `IPrintingService.cs`: Receipt printing operations
- **Models/**:
  - `MenuItem`: Menu item with availability, price, options
    - Contains optional `List<MenuOption>` for customizations
    - Each option has category (mutually exclusive within category)
  - `MenuOption`: Customization option (e.g., "Extra Shot", "Light", "Add Ice")
    - `ViewIndex` for display ordering within category
    - `OptionCategoryId` groups mutually exclusive options
  - `OrderInfo`: Order details for submission
    - Contains `List<OrderItem>` with selected menu items
    - Includes QR data for pickup if enabled
  - `OrderItem`: Individual item in order with selected option IDs
    - **`SystemName` field** identifies which unmanned system this item belongs to
  - `SystemStatus`: Real-time system state
    - Online/Available status, waiting orders count, estimated wait time
  - `OrderHelpers`: Static helper class for multi-system order processing
    - `SplitOrderBySystem()`: Splits multi-system cart into system-specific orders
    - `GetSystemNames()`: Lists all systems in an order
    - `ValidateOrderItems()`: Ensures all items have SystemName assigned
  - Shared across all layers

## Architecture Patterns

### Dependency Flow
The architecture follows strict dependency rules to maintain separation of concerns:

1. **UI → Services**: ViewModels depend on Services layer
   - ViewModels call business logic methods in Services
   - Admin ViewModels have direct access to Config/Log services

2. **Services → Adapters**: Services manage and call Adapters
   - `SystemManagerService` manages all `IUnmannedSystem` adapter instances
   - `SystemPollingService` polls `SystemManagerService` in background
   - `PaymentService` calls Payment Gateway hardware
   - `PrintingService` calls Receipt Printer hardware

3. **Adapters → External Systems**: Adapters wrap external DLLs
   - Each adapter (e.g., `CoffeeSystemAdapter`, `FoodSystemAdapter`) wraps its respective DLL
   - Provides unified interface via `IUnmannedSystem`

4. **All Layers → Core**: All layers depend on Core abstractions
   - ViewModels, Services, and Adapters all use Core Models
   - Services and Adapters implement Core Interfaces

### MVVM Implementation
- Uses CommunityToolkit.Mvvm for `RelayCommand` implementations
- All ViewModels inherit from `BaseViewModel` which provides `INotifyPropertyChanged` functionality
- Navigation is handled via the `MainViewModel.CurrentViewModel` property which switches between different ViewModels
- Commands use `ICommand` interface with `RelayCommand` from CommunityToolkit.Mvvm

### Adapter Pattern
Each unmanned system (Coffee, Food, etc.) is abstracted through the Adapter pattern:
- **Interface**: `IUnmannedSystem` defines the contract for all kiosk systems
- **Concrete Adapters**: Each system has a dedicated adapter (e.g., `CoffeeSystemAdapter`)
- **DLL Wrapping**: Adapters wrap vendor-specific DLLs and expose a consistent interface
- **Managed by**: `SystemManagerService` manages all adapter instances centrally

### Background Service Pattern
`SystemPollingService` implements background polling:
- Runs continuously in the background
- Polls `SystemManagerService` to check health/status of all connected systems
- Enables real-time monitoring of kiosk hardware without blocking UI

### Navigation Pattern
Navigation between screens is orchestrated by `MainViewModel`:
1. `MainViewModel` holds reference to current ViewModel in `CurrentViewModel` property
2. Navigation commands (`NavigateToWelcomeCommand`, `NavigateToMenuCommand`) create new ViewModel instances
3. MainWindow's `ContentControl` binds to the entire ViewModel, relying on WPF's implicit DataTemplate resolution
4. Callback actions (e.g., `onStartOrder` in WelcomeViewModel) enable child ViewModels to trigger parent navigation

### View Composition
- The application uses a shell-based composition pattern
- `MainWindow` acts as the shell with a `ContentControl` that displays the current view
- The MainWindow is initialized in `App.xaml.cs.OnStartup()` with `MainViewModel` as DataContext
- Views are UserControls bound to their ViewModels through implicit DataTemplate resolution

## Adding New Kiosk Systems

To add a new unmanned kiosk system (e.g., Ice Cream, Snacks):

1. **Create Adapter Folder Structure**:
   ```
   Adapters/{SystemName}/
   ├── {SystemName}SystemAdapter.cs
   └── vendor_dll.dll
   ```

2. **Implement Adapter**:
   - Create `{SystemName}SystemAdapter.cs`
   - Implement `IUnmannedSystem` interface
   - Wrap the vendor DLL and expose operations through the interface

3. **Register in SystemManagerService**:
   - Add the new adapter instance to `SystemManagerService`
   - Configure any system-specific settings

4. **Create UI Components** (if needed):
   - Add ViewModel in `MainApp/ViewModels/`
   - Add corresponding View in `MainApp/Views/`
   - Update navigation in `MainViewModel` if required

5. **Add Domain Models** (if needed):
   - Define system-specific models in `Core/Models/`

## Design Considerations

### Window Dimensions
The MainWindow is configured with unusual dimensions (Height="1920" Width="1080"), suggesting this is a kiosk application intended for portrait-mode displays or vertical screens.

### Asset Management
The application references video assets (e.g., `Assets/Ads/WelcomeVideo.mp4`). Ensure video files exist in the expected paths when developing ad-related features.

### Multi-Kiosk Linkage
The system is designed to manage multiple interconnected kiosks:
- Each kiosk system (Coffee, Food, etc.) operates independently through its adapter
- `SystemManagerService` provides centralized management
- `SystemPollingService` monitors all systems continuously
- This allows a single POS interface to control multiple unmanned kiosks simultaneously

### Multi-System Cart & Order Splitting
Customers can add items from different unmanned systems into a single cart:

**Cart Management**:
- Each `OrderItem` includes a `SystemName` field to identify its source system
- Customers can order coffee from Coffee system + sandwich from Food system in one transaction
- Single payment is processed for all items regardless of source system

**Order Processing Flow**:
1. Customer adds items from multiple systems to cart
2. Single payment is processed for total amount
3. Use `OrderHelpers.SplitOrderBySystem(OrderInfo)` to split the order
4. Each system receives an `OrderInfo` with:
   - **Same `OrderId`** (for tracking and linking)
   - **Same `OrderLabel`** (customer sees one order number)
   - **Only items for that system** (filtered by `SystemName`)
   - **Proportional `TotalAmount`** (for system accounting)
5. Send split orders to respective systems via their `IUnmannedSystem.SendOrder()` methods

**Example**:
```csharp
// Original cart: Coffee Latte + Sandwich + Americano
var order = new OrderInfo
{
    OrderId = "abc-123",
    OrderLabel = "A01",
    Items = [
        new OrderItem { SystemName = "Coffee", MenuId = "latte-id" },
        new OrderItem { SystemName = "Food", MenuId = "sandwich-id" },
        new OrderItem { SystemName = "Coffee", MenuId = "americano-id" }
    ],
    TotalAmount = 15.00m
};

// Split by system
var systemOrders = OrderHelpers.SplitOrderBySystem(order);

// Result:
// systemOrders["Coffee"] -> OrderInfo with [Latte, Americano]
// systemOrders["Food"]   -> OrderInfo with [Sandwich]
// Both have OrderId = "abc-123" for tracking
```

**Important**: All split orders maintain the same `OrderId` so they can be tracked as a single customer transaction across multiple systems.

## Development Notes

- The project uses nullable reference types (`<Nullable>enable</Nullable>`)
- Implicit usings are enabled for cleaner code
- The architecture is designed for extensibility - adding new kiosk systems should not require modifying existing code
- Services should be registered with Dependency Injection container (to be implemented)
- All hardware interactions must go through the Adapter layer - never directly from ViewModels or Services
