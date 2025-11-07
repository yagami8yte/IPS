# Hardware Scanning and Configuration Guide

## Overview

IPS includes built-in hardware scanning tools to automatically discover:
- **Unmanned System Devices** (Folletto booths, coffee machines, etc.) on your network
- **Receipt Printers** (USB, Network, Serial) connected to the POS PC

## Quick Start

### 1. Access Admin Panel

1. Run `IPS.exe`
2. Click the **⚙ gear icon** in the top-right corner of the Welcome screen
3. You'll see the Admin Configuration panel

### 2. Scan for Network Devices

The Network Scanner will search your local network for unmanned systems:

**Auto-Scan Network:**
```
1. Click "🔍 Scan Network" button
2. Wait for scan to complete (shows progress bar)
3. Review discovered devices in the list
4. Click "+ Add to Config" for each device you want to use
5. Edit system name if needed
6. Click "Save Configuration"
```

**What Gets Scanned:**
- IP Range: Your subnet (e.g., 192.168.1.1 - 192.168.1.254)
- Ports: 5000, 5001, 8080, 8081, 9000 (common unmanned system ports)
- Device Types: Folletto Booth, Secondary Booth, HTTP Device, etc.

**Example Output:**
```
Folletto Booth - 192.168.1.100:5000    [+ Add to Config]
HTTP Device - 192.168.1.150:8080       [+ Add to Config]
```

### 3. Scan for Printers

The Printer Scanner will detect all installed printers:

**Auto-Scan Printers:**
```
1. Click "🖨 Scan Printers" button
2. View list of discovered printers
3. Click "📄 Test Print" to verify a printer works
4. Selected printer will be highlighted
5. Note the printer name for receipt printing configuration
```

**What Gets Detected:**
- USB Printers
- Network Printers
- Serial (COM) Printers
- Receipt printer brands: EPSON TM-series, Star TSP, Bixolon, Citizen

**Example Output:**
```
EPSON TM-T88V Receipt Printer
Type: EPSON TM Series | Port: USB001          [📄 Test Print]

Star TSP143III LAN
Type: Star TSP Series | Port: 192.168.1.50    [📄 Test Print]
```

### 4. Manual Configuration (Alternative)

If automatic scanning doesn't work, configure manually:

**For Unmanned Systems:**
```
1. Click "+ Add System"
2. Enter:
   - System Name: "Folletto" (or your device name)
   - IP Address: 192.168.1.100 (your device IP)
   - Port: 5000 (your device port)
   - Enabled: ✓ (check box)
3. Click "Save Configuration"
```

**For Printers:**
- Note the printer name from Windows Settings → Devices → Printers
- Configure in future PrintingService implementation

## Network Scanning Technical Details

### Scan Process

1. **Local IP Detection**
   - Automatically detects your POS PC's IP address
   - Determines subnet (e.g., 192.168.1.x)

2. **Network Sweep**
   - Pings each IP in range (1-254)
   - Tests common ports for open connections
   - Identifies device type based on port

3. **Device Identification**
   - Port 5000 → "Folletto Booth"
   - Port 5001 → "Secondary Booth"
   - Port 8080/8081 → "HTTP Device"
   - Port 9000 → "Generic Device"

### Connection Testing

Test individual devices:
```csharp
// In code, you can test specific IP:Port
var scanner = new NetworkScannerService();
bool isOnline = await scanner.TestConnectionAsync("192.168.1.100", 5000, timeoutMs: 2000);
```

### Scan Localhost Only

Quick scan for services on the POS PC itself:
```
Scans ports: 5000, 5001, 6000, 8080, 8081, 9000 on 127.0.0.1
Useful for development and local testing
```

## Printer Scanning Technical Details

### Printer Detection

Uses Windows Print Spooler API:
- Enumerates `PrinterSettings.InstalledPrinters`
- Queries WMI (Windows Management Instrumentation) for details
- Retrieves port, driver, and status information

### Printer Type Identification

**By Brand Name:**
- "EPSON TM" → EPSON TM Series
- "Star" / "TSP" → Star TSP Series
- "Bixolon" → Bixolon Thermal
- "Citizen" → Citizen Thermal

**By Connection:**
- "USB" → USB Printer
- "COM" → Serial Printer
- "LPT" → Parallel Printer
- "192.168..." → Network Printer

**By Keywords:**
- "receipt" / "thermal" → Receipt Printer
- "pos" → POS Printer

### Test Print

Sends a simple receipt to verify printer connectivity:
```
=============================
  IPS TEST PRINT
=============================
Printer: EPSON TM-T88V
Time: 2025-11-06 21:30:45
=============================
```

## Troubleshooting

### Network Scan Issues

**Problem: "Scan found 0 devices"**

Solutions:
1. Verify devices are powered on
2. Check network cables/WiFi connections
3. Ensure devices are on same subnet as POS PC
4. Check firewall settings (allow outgoing connections on ports 5000-9000)
5. Manually ping device IP from command prompt: `ping 192.168.1.100`

**Problem: "Scan is very slow"**

Solutions:
1. Reduce scan range (e.g., scan 1-50 instead of 1-254)
2. Scan specific IP addresses only
3. Check network speed/congestion

### Printer Scan Issues

**Problem: "No printers found"**

Solutions:
1. Verify printer is connected (USB cable or network)
2. Check printer is powered on
3. Install printer drivers from manufacturer
4. Verify printer appears in Windows Settings → Devices → Printers

**Problem: "Test print fails"**

Solutions:
1. Check printer has paper loaded
2. Verify printer is online (not "Offline" in Windows)
3. Clear print queue in Windows
4. Restart printer
5. Check USB cable or network connection

### Configuration Not Saving

**Problem: "Configuration resets after restart"**

Solutions:
1. Check `appsettings.json` file permissions
2. Run IPS.exe as Administrator
3. Verify file path: Same folder as `IPS.exe`

## Best Practices

### Network Setup

1. **Use Static IPs** for unmanned systems
   - Prevents IP changes from DHCP
   - Easier to configure and troubleshoot

2. **Document Your Network**
   ```
   Device          | IP Address      | Port | Notes
   ----------------|-----------------|------|------------------
   Folletto Booth  | 192.168.1.100  | 5000 | Coffee machine
   POS PC          | 192.168.1.10   | -    | Running IPS
   Receipt Printer | 192.168.1.50   | 9100 | Star TSP143III
   ```

3. **Test Connections**
   - Use "Test Connection" after adding each device
   - Verify green checkmark before saving

### Printer Setup

1. **Install Official Drivers**
   - Download from manufacturer website
   - Don't rely on Windows generic drivers

2. **Set Default Printer**
   - IPS will auto-select default printer
   - Set in Windows Settings → Devices → Printers

3. **Configure Print Settings**
   - Paper size: Typically 80mm thermal
   - Print quality: Fast/Draft for receipts
   - Auto-cut: Enable if supported

## Advanced Configuration

### Custom Scan Ranges

Edit scan parameters in code:

```csharp
// Scan specific subnet
var devices = await scanner.ScanNetworkAsync(
    startRange: 100,        // Start at .100
    endRange: 150,          // End at .150
    portsToScan: new[] { 5000, 5001 },  // Only these ports
    progress: progressHandler
);
```

### Custom Port Detection

Add your custom ports to scan:

```csharp
// In NetworkScannerService
portsToScan = new[] { 5000, 5001, 8080, 8081, 9000, 1234 };  // Added port 1234
```

### Printer Filters

Filter specific printer types:

```csharp
// Get only network printers
var networkPrinters = availablePrinters.Where(p =>
    p.PortName.Contains("IP") ||
    p.PortName.Contains("192.168")
).ToList();
```

## API Reference

### NetworkScannerService

```csharp
// Get local IP
string localIp = scanner.GetLocalIPAddress();

// Get subnet
string subnet = scanner.GetSubnet();  // Returns "192.168.1"

// Scan network
List<DiscoveredDevice> devices = await scanner.ScanNetworkAsync(
    startRange: 1,
    endRange: 254,
    portsToScan: new[] { 5000 },
    progress: new Progress<int>(percent => Console.WriteLine($"{percent}%"))
);

// Test specific device
bool isOnline = await scanner.TestConnectionAsync("192.168.1.100", 5000);

// Quick localhost scan
List<DiscoveredDevice> localDevices = await scanner.ScanLocalhostAsync();
```

### PrinterScannerService

```csharp
// Scan all printers
List<DiscoveredPrinter> allPrinters = scanner.ScanPrinters();

// Get receipt printers only
List<DiscoveredPrinter> receiptPrinters = scanner.GetReceiptPrinters();

// Test printer
bool success = scanner.TestPrint("EPSON TM-T88V");
```

### DiscoveredDevice Properties

```csharp
device.IpAddress       // "192.168.1.100"
device.Port            // 5000
device.DeviceType      // "Folletto Booth"
device.IsResponding    // true/false
device.DeviceName      // "Folletto Booth (192.168.1.100:5000)"
```

### DiscoveredPrinter Properties

```csharp
printer.PrinterName    // "EPSON TM-T88V"
printer.PortName       // "USB001"
printer.IsDefault      // true/false
printer.IsOnline       // true/false
printer.DriverName     // "EPSON TM-T88V ReceiptE4"
printer.PrinterType    // "EPSON TM Series"
```

## Support

For additional help:
1. Check console output for detailed error messages
2. Review `appsettings.json` for configuration
3. Verify Windows Event Viewer for system-level errors
4. Contact hardware vendor for device-specific issues
