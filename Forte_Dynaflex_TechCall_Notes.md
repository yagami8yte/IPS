# Forte Dynaflex Technical Call Notes

## ACTUAL LOG DATA (from tester's machine)

```
Machine: LGKIOSK1
OS: Windows NT 10.0.17763.0 (Windows Server 2019 / Windows 10 LTSC)
Working Directory: C:\Users\Administrator\Desktop\POS\publish
```

---

## Device Detection (WORKING)

```
Vendor ID:    0x0801 (MagTek)
Product ID:   0x2024 (8228 decimal)  ← ACTUAL PID FROM LOG
HID Usage:    Page=0xFF00, ID=0x0001 (Vendor-Specific mode - CORRECT)

Max Input Report:   65 bytes
Max Output Report:  65 bytes
Max Feature Report: 0 bytes   <- Device doesn't support Feature Reports
```

This is good - the device is in HID mode (not Keyboard emulation mode), so it *should* accept commands.

---

## How We're Sending Commands

Since Feature Reports are not supported (max=0), we send via **Output Reports**:

```csharp
// Pad command to max output report length (65 bytes)
var paddedCommand = new byte[65];
Array.Copy(command, paddedCommand, command.Length);

// Write to HID stream
await _stream.WriteAsync(paddedCommand, 0, paddedCommand.Length);
await _stream.FlushAsync();
```

**Result**: `Write completed` - no errors thrown, but device doesn't respond.

---

## Commands We're Sending (Beep Example)

We try **4 different formats** for the beep command:

| Format | Bytes Sent | Description |
|--------|------------|-------------|
| 1 | `00 0A 02 01 14` | [ReportID=0x00] [Cmd=0x0A] [Len=0x02] [Count=1] [Duration=20] |
| 2 | `00 0A 01 14` | [ReportID=0x00] [Cmd=0x0A] [Count=1] [Duration=20] |
| 3 | `01 0A 02 01 14` | [ReportID=0x01] [Cmd=0x0A] [Len=0x02] [Count=1] [Duration=20] |
| 4 | `0A 02 01 14` | [Cmd=0x0A] [Len=0x02] [Count=1] [Duration=20] (no report ID) |

**MMS Command IDs we're using:**
- `0x0A` = Beep
- `0x09` = Set LED
- `0xA2` = Start EMV Transaction
- `0x1D` = Request Card

---

## What We Expect vs What Happens

| Step | Expected | Actual |
|------|----------|--------|
| Open HID device | Success | Success |
| Send beep command | Device beeps | No beep, no error |
| Send StartTransaction | Device beeps, waits for card | No response |
| Read card data | Get encrypted ARQC/EMVSREDData | Never happens |

---

## The Specific Problem

```
[Dynaflex] Sending command: 00-0A-02-01-14
[Dynaflex] Output Report: MaxLen=65, CommandLen=5
[Dynaflex] Output Report: Writing 65 bytes...
[Dynaflex] Output Report: Write completed
[Dynaflex] Command sent via Output Report - SUCCESS
```

**The write succeeds with no errors, but the device does nothing.**

---

## Questions for Technician

1. **"Is the MMS command format correct?"**
   - We're sending `[ReportID][CommandID][Length][Data...]`
   - Should Report ID be 0x00, 0x01, or omitted?

2. **"Does Dynaflex II Go require initialization before accepting commands?"**
   - Do we need to send a specific "wake up" or "open session" command first?
   - Is there a handshake protocol?

3. **"Is the device in the correct mode for direct HID commands?"**
   - It shows Usage Page 0xFF00 (Vendor-Specific), not Keyboard mode
   - But does it need to be configured via MagTek tools first?

4. **"Should we be using a different protocol entirely?"**
   - MagTek MMS? MTSCRA? Something else?
   - Does Forte provide an SDK or DLL for Dynaflex communication?

5. **"Is the Dynaflex supposed to work standalone, or only through Forte's systems?"**
   - Can we send commands directly via USB HID?
   - Or does Forte's backend need to "unlock" the device first?

---

## Integration Method: Forte Checkout v2 (Embedded Mode)

- Using the **button-based** checkout with JavaScript library
- Sandbox URL: `https://sandbox.forte.net/checkout/v2/js`
- Payment form is embedded in a WebView2 control inside WPF app

### Authentication Flow:
1. Fetch UTC time from Forte server (`/checkout/getUTC`)
2. Generate HMAC-SHA256 signature with format:
   ```
   api_access_id|method|version_number|total_amount|utc_time|order_number||
   ```
3. HTML button has all required attributes (api_access_id, location_id, signature, etc.)
4. Button attribute: `swipe='Dynaflex'` tells Forte to use Dynaflex reader

---

## ACTUAL Raw Log Output (from tester 2026-01-05)

```
[11:25:16.275] [Dynaflex] Attempting to connect to Dynaflex...
[11:25:16.383] [Dynaflex] Trying to open device: DynaFlex II Go (PID=2024)
[11:25:16.409] [Dynaflex] Connected to: DynaFlex II Go
[11:25:16.410] [Dynaflex] Serial Number: B574E87
[11:25:16.416] [Dynaflex] Max Input Report: 65
[11:25:16.417] [Dynaflex] Max Output Report: 65
[11:25:16.420] [Dynaflex] Max Feature Report: 0
[11:25:16.455] [Dynaflex]   HID Usage: Page=0xFF00, ID=0x0001
[11:25:16.458] [Dynaflex] Read loop started

[11:25:16.465] [Dynaflex] Sending beep: count=2, duration=150ms
[11:25:16.466] [Dynaflex] Trying beep format: 00-0A-02-02-0F
[11:25:16.468] [Dynaflex] Sending command: 00-0A-02-02-0F
[11:25:16.471] [Dynaflex] Output Report: MaxLen=65, CommandLen=5
[11:25:16.472] [Dynaflex] Output Report: Writing 65 bytes...
[11:25:16.495] [Dynaflex] Output Report: Write completed
[11:25:16.497] [Dynaflex] Command sent via Output Report - SUCCESS
[11:25:16.498] [Dynaflex] Beep command sent successfully
← (Device does NOT beep)

[11:25:16.802] [Dynaflex] Starting EMV transaction for amount: $0.01
[11:25:16.803] [Dynaflex] Sending command: 00-A2-04-01-00-00-00
[11:25:16.803] [Dynaflex] Output Report: MaxLen=65, CommandLen=7
[11:25:16.803] [Dynaflex] Output Report: Writing 65 bytes...
[11:25:16.810] [Dynaflex] Output Report: Write completed
[11:25:16.811] [Dynaflex] Command sent via Output Report - SUCCESS
[11:25:16.812] [Dynaflex] Transaction started - waiting for card
← (Device does NOT respond, no card reading)

[11:25:47.129] [Dynaflex] Disconnecting from Dynaflex...
[11:25:47.211] [Dynaflex] Read loop ended
[11:25:48.132] [Dynaflex] Disconnected
```

**Key observation**: Test ran for 30 seconds, all commands "succeeded" but device never responded physically.
