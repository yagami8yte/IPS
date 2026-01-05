using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;

namespace IPS.Services
{
    /// <summary>
    /// Service for integrating MagTek Dynaflex II Go card reader
    /// Implements MagTek MMS (MagTek Messaging Schema) protocol for EMV transactions
    /// </summary>
    public class DynaflexService : IDisposable
    {
        // MagTek Vendor ID
        private const int MAGTEK_VENDOR_ID = 0x0801; // 2049 decimal

        // MagTek Product IDs for HID mode
        private static readonly int[] MAGTEK_PRODUCT_IDS = new[]
        {
            0x0011, // Wireless USB dongle in HID mode
            0x001A, // EMV-only devices (mDynamo, DynaWave) in HID mode
            0x0017, // Audio devices in HID mode
            0x0001, // KB mode (not typically used for direct communication)
            0x0002, // Alternative
        };

        // MagTek MMS Command IDs
        private static class MmsCommands
        {
            // Device Commands
            public const byte GetDeviceInfo = 0x00;
            public const byte GetDeviceState = 0x01;
            public const byte Reset = 0x02;
            public const byte SetLED = 0x09;
            public const byte Beep = 0x0A;

            // EMV Transaction Commands
            public const byte StartEMVTransaction = 0xA2;
            public const byte CancelEMVTransaction = 0xA3;
            public const byte RequestARQCData = 0xAB;

            // Card Reading Commands
            public const byte RequestCard = 0x1D;
            public const byte CancelCardRequest = 0x1E;

            // Configuration Commands
            public const byte GetDeviceSerialNumber = 0x05;
        }

        // MagTek MMS Notification IDs
        private static class MmsNotifications
        {
            public const byte TransactionStatus = 0x00;    // 0x0300 - Transaction progress
            public const byte DisplayMessage = 0x01;       // 0x0301 - Display message request
            public const byte CardholderSelection = 0x02;  // 0x0302 - Cardholder selection
            public const byte ARQCMessage = 0x03;          // 0x0303 - ARQC data ready
        }

        private HidDevice? _device;
        private HidStream? _stream;
        private CancellationTokenSource? _readCancellation;
        private Task? _readTask;
        private bool _isDisposed;
        private readonly object _lock = new object();

        // Device state
        private bool _isConnected;
        private bool _isTransactionActive;
        private string _deviceSerialNumber = string.Empty;
        private string _firmwareVersion = string.Empty;

        // Events
        public event EventHandler<DynaflexCardDataEventArgs>? CardDataReceived;
        public event EventHandler<DynaflexConnectionEventArgs>? ConnectionStateChanged;
        public event EventHandler<DynaflexTransactionEventArgs>? TransactionStatusChanged;
        public event EventHandler<string>? LogMessage;

        /// <summary>
        /// Whether the Dynaflex device is currently connected
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStateChanged?.Invoke(this, new DynaflexConnectionEventArgs(value, _deviceSerialNumber));
                }
            }
        }

        /// <summary>
        /// Whether a transaction is currently active (waiting for card)
        /// </summary>
        public bool IsTransactionActive => _isTransactionActive;

        /// <summary>
        /// Device serial number (if connected)
        /// </summary>
        public string DeviceSerialNumber => _deviceSerialNumber;

        private void Log(string message)
        {
            var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] [Dynaflex] {message}";
            Console.WriteLine(timestamped);
            LogMessage?.Invoke(this, timestamped);
        }

        /// <summary>
        /// Scan for available Dynaflex devices
        /// </summary>
        public List<DynaflexDeviceInfo> ScanDevices()
        {
            var devices = new List<DynaflexDeviceInfo>();

            try
            {
                var hidDevices = DeviceList.Local.GetHidDevices();
                Log($"Scanning USB HID devices... Found {hidDevices.Count()} total devices");

                foreach (var device in hidDevices)
                {
                    // Check for MagTek vendor ID
                    if (device.VendorID == MAGTEK_VENDOR_ID)
                    {
                        string productName = "MagTek Device";
                        try { productName = device.GetProductName() ?? productName; } catch { }

                        Log($"Found MagTek device: VID={device.VendorID:X4}, PID={device.ProductID:X4}, Product={productName}");

                        devices.Add(new DynaflexDeviceInfo
                        {
                            VendorId = device.VendorID,
                            ProductId = device.ProductID,
                            ProductName = productName,
                            SerialNumber = TryGetSerialNumber(device),
                            DevicePath = device.DevicePath
                        });
                    }
                }

                if (devices.Count == 0)
                {
                    Log("No MagTek/Dynaflex devices found");
                }
                else
                {
                    Log($"Found {devices.Count} MagTek device(s)");
                }
            }
            catch (Exception ex)
            {
                Log($"Error scanning devices: {ex.Message}");
            }

            return devices;
        }

        private string TryGetSerialNumber(HidDevice device)
        {
            try
            {
                return device.GetSerialNumber() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Connect to the first available Dynaflex device
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (IsConnected)
                        {
                            Log("Already connected to Dynaflex");
                            return true;
                        }

                        Log("Attempting to connect to Dynaflex...");

                        var hidDevices = DeviceList.Local.GetHidDevices()
                            .Where(d => d.VendorID == MAGTEK_VENDOR_ID)
                            .ToList();

                        if (hidDevices.Count == 0)
                        {
                            Log("No MagTek devices found");
                            return false;
                        }

                        // Try each MagTek device
                        foreach (var device in hidDevices)
                        {
                            try
                            {
                                string productName = "Unknown";
                                try { productName = device.GetProductName() ?? productName; } catch { }

                                Log($"Trying to open device: {productName} (PID={device.ProductID:X4})");

                                if (device.TryOpen(out var stream))
                                {
                                    _device = device;
                                    _stream = stream;
                                    _deviceSerialNumber = TryGetSerialNumber(device);

                                    Log($"Connected to: {productName}");
                                    Log($"Serial Number: {_deviceSerialNumber}");
                                    Log($"Max Input Report: {device.GetMaxInputReportLength()}");
                                    Log($"Max Output Report: {device.GetMaxOutputReportLength()}");

                                    // Start reading data
                                    StartReading();

                                    IsConnected = true;

                                    // Try to get device info and set initial state
                                    _ = Task.Run(async () =>
                                    {
                                        await Task.Delay(500);
                                        await SendBeepAsync(1, 100); // Short beep to confirm connection
                                    });

                                    return true;
                                }
                                else
                                {
                                    Log($"Failed to open device: {productName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Error opening device: {ex.Message}");
                            }
                        }

                        Log("Could not connect to any MagTek device");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Connection error: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Disconnect from the Dynaflex device
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                Log("Disconnecting from Dynaflex...");

                _isTransactionActive = false;
                StopReading();

                _stream?.Dispose();
                _stream = null;
                _device = null;
                _deviceSerialNumber = string.Empty;

                IsConnected = false;
                Log("Disconnected");
            }
        }

        /// <summary>
        /// Start an EMV transaction - enables card reading
        /// The device will beep and wait for card insertion/tap/swipe
        /// </summary>
        public async Task<bool> StartTransactionAsync(decimal amount)
        {
            if (!IsConnected || _stream == null)
            {
                Log("Cannot start transaction - not connected");
                return false;
            }

            if (_isTransactionActive)
            {
                Log("Transaction already active");
                return true;
            }

            try
            {
                Log($"Starting EMV transaction for amount: ${amount:F2}");

                // Build StartTransaction command
                // Format: [ReportID][CommandID][DataLength][Data...]
                // Amount is in cents, 4 bytes little-endian
                int amountCents = (int)(amount * 100);

                var command = BuildStartTransactionCommand(amountCents);

                bool sent = await SendCommandAsync(command);

                if (sent)
                {
                    _isTransactionActive = true;
                    Log("Transaction started - waiting for card");
                    TransactionStatusChanged?.Invoke(this, new DynaflexTransactionEventArgs("waiting_for_card", "Please insert, tap, or swipe card"));

                    // Send beep to indicate ready
                    await SendBeepAsync(2, 150);

                    return true;
                }
                else
                {
                    Log("Failed to send StartTransaction command");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting transaction: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Request a card swipe/tap (non-EMV mode)
        /// </summary>
        public async Task<bool> RequestCardAsync()
        {
            if (!IsConnected || _stream == null)
            {
                Log("Cannot request card - not connected");
                return false;
            }

            try
            {
                Log("Requesting card swipe/tap...");

                // Build RequestCard command
                var command = BuildRequestCardCommand();

                bool sent = await SendCommandAsync(command);

                if (sent)
                {
                    _isTransactionActive = true;
                    Log("Card request sent - waiting for card");
                    TransactionStatusChanged?.Invoke(this, new DynaflexTransactionEventArgs("waiting_for_card", "Please swipe or tap card"));

                    // Send beep to indicate ready
                    await SendBeepAsync(1, 200);

                    return true;
                }
                else
                {
                    Log("Failed to send RequestCard command");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"Error requesting card: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cancel the current transaction
        /// </summary>
        public async Task<bool> CancelTransactionAsync()
        {
            if (!IsConnected || _stream == null)
            {
                return false;
            }

            try
            {
                Log("Cancelling transaction...");

                var command = new byte[] { 0x00, MmsCommands.CancelEMVTransaction, 0x00 };

                bool sent = await SendCommandAsync(command);

                if (sent)
                {
                    _isTransactionActive = false;
                    Log("Transaction cancelled");
                    TransactionStatusChanged?.Invoke(this, new DynaflexTransactionEventArgs("cancelled", "Transaction cancelled"));
                }

                return sent;
            }
            catch (Exception ex)
            {
                Log($"Error cancelling transaction: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a beep command to the device
        /// </summary>
        public async Task<bool> SendBeepAsync(int count = 1, int durationMs = 200)
        {
            if (!IsConnected || _stream == null)
            {
                return false;
            }

            try
            {
                Log($"Sending beep: count={count}, duration={durationMs}ms");

                // Beep command format varies by device
                // Generic format: [ReportID][CommandID][Count][Duration(10ms units)]
                byte durationUnits = (byte)Math.Min(255, durationMs / 10);

                var command = new byte[]
                {
                    0x00,                       // Report ID
                    MmsCommands.Beep,           // Command ID (0x0A)
                    0x02,                       // Data length
                    (byte)Math.Min(255, count), // Beep count
                    durationUnits               // Duration in 10ms units
                };

                return await SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                Log($"Error sending beep: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set LED state on the device
        /// </summary>
        public async Task<bool> SetLEDAsync(byte ledMask, bool on)
        {
            if (!IsConnected || _stream == null)
            {
                return false;
            }

            try
            {
                Log($"Setting LED: mask={ledMask:X2}, on={on}");

                var command = new byte[]
                {
                    0x00,                   // Report ID
                    MmsCommands.SetLED,     // Command ID (0x09)
                    0x02,                   // Data length
                    ledMask,                // LED mask
                    (byte)(on ? 0x01 : 0x00) // On/Off
                };

                return await SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                Log($"Error setting LED: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test the card reader by starting a transaction and waiting for response
        /// </summary>
        public async Task<DynaflexTestResult> TestCardReaderAsync(int timeoutSeconds = 30)
        {
            var result = new DynaflexTestResult();

            try
            {
                Log("=== CARD READER TEST STARTED ===");

                // Step 1: Check connection
                result.Logs.Add("Step 1: Checking device connection...");
                if (!IsConnected)
                {
                    result.Logs.Add("Device not connected, attempting to connect...");
                    bool connected = await ConnectAsync();
                    if (!connected)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to connect to card reader";
                        result.Logs.Add("FAILED: Could not connect to device");
                        return result;
                    }
                }
                result.Logs.Add($"Connected to: {_device?.GetProductName() ?? "Unknown"}");
                result.Logs.Add($"Serial: {_deviceSerialNumber}");

                // Step 2: Send beep
                result.Logs.Add("Step 2: Testing beep function...");
                bool beeped = await SendBeepAsync(2, 150);
                result.Logs.Add(beeped ? "Beep command sent successfully" : "Beep command may have failed");

                // Step 3: Start transaction
                result.Logs.Add("Step 3: Starting test transaction...");
                result.Logs.Add("Please INSERT, TAP, or SWIPE a card within the next 30 seconds");

                // Set up card data received handler
                var cardDataTcs = new TaskCompletionSource<DynaflexCardDataEventArgs>();
                EventHandler<DynaflexCardDataEventArgs>? cardHandler = null;
                cardHandler = (s, e) =>
                {
                    cardDataTcs.TrySetResult(e);
                };

                CardDataReceived += cardHandler;

                try
                {
                    // Start transaction
                    bool started = await StartTransactionAsync(0.01m); // Test amount
                    if (!started)
                    {
                        // Try RequestCard as fallback
                        result.Logs.Add("StartTransaction not supported, trying RequestCard...");
                        started = await RequestCardAsync();
                    }

                    if (!started)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to start card read transaction";
                        result.Logs.Add("FAILED: Could not initiate card reading");
                        return result;
                    }

                    result.Logs.Add("Waiting for card...");

                    // Wait for card data with timeout
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                    try
                    {
                        var completedTask = await Task.WhenAny(
                            cardDataTcs.Task,
                            Task.Delay(Timeout.Infinite, cts.Token)
                        );

                        if (completedTask == cardDataTcs.Task)
                        {
                            var cardData = await cardDataTcs.Task;
                            result.Logs.Add("Card data received!");
                            result.Logs.Add($"Card Type: {cardData.CardType}");
                            result.Logs.Add($"Valid: {cardData.IsValid}");
                            if (!string.IsNullOrEmpty(cardData.CardLast4))
                            {
                                result.Logs.Add($"Card Last 4: ****{cardData.CardLast4}");
                            }
                            result.Success = true;
                            result.CardData = cardData;
                            result.Logs.Add("=== CARD READER TEST PASSED ===");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        result.Logs.Add("Timeout - no card detected within time limit");
                        result.Success = false;
                        result.ErrorMessage = "Timeout waiting for card";
                        await CancelTransactionAsync();
                    }
                }
                finally
                {
                    CardDataReceived -= cardHandler;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Logs.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        #region Private Methods

        private byte[] BuildStartTransactionCommand(int amountCents)
        {
            // MagTek StartTransaction command format
            // This varies by device firmware, using common format
            var command = new List<byte>
            {
                0x00,                           // Report ID
                MmsCommands.StartEMVTransaction, // Command ID (0xA2)
                0x04,                           // Data length (4 bytes for amount)
                (byte)(amountCents & 0xFF),
                (byte)((amountCents >> 8) & 0xFF),
                (byte)((amountCents >> 16) & 0xFF),
                (byte)((amountCents >> 24) & 0xFF)
            };

            return command.ToArray();
        }

        private byte[] BuildRequestCardCommand()
        {
            // RequestCard command for non-EMV mode
            return new byte[]
            {
                0x00,                       // Report ID
                MmsCommands.RequestCard,    // Command ID (0x1D)
                0x01,                       // Data length
                0x07                        // Card types: 0x01=Swipe, 0x02=Chip, 0x04=Contactless -> 0x07=All
            };
        }

        private async Task<bool> SendCommandAsync(byte[] command)
        {
            if (_stream == null) return false;

            try
            {
                // Pad to max output report length
                int maxLen = _device?.GetMaxOutputReportLength() ?? 64;
                var paddedCommand = new byte[maxLen];
                Array.Copy(command, paddedCommand, Math.Min(command.Length, maxLen));

                Log($"Sending command: {BitConverter.ToString(command.Take(Math.Min(10, command.Length)).ToArray())}...");

                await _stream.WriteAsync(paddedCommand, 0, paddedCommand.Length);
                await _stream.FlushAsync();

                return true;
            }
            catch (Exception ex)
            {
                Log($"Error sending command: {ex.Message}");
                return false;
            }
        }

        private void StartReading()
        {
            _readCancellation = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoop(_readCancellation.Token));
            Log("Started reading from device");
        }

        private void StopReading()
        {
            try
            {
                _readCancellation?.Cancel();
                _readTask?.Wait(1000);
            }
            catch { }
            finally
            {
                _readCancellation?.Dispose();
                _readCancellation = null;
                _readTask = null;
            }
        }

        private void ReadLoop(CancellationToken cancellationToken)
        {
            Log("Read loop started");

            var buffer = new byte[4096];
            var dataBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                try
                {
                    _stream.ReadTimeout = 100;
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                    catch (System.IO.IOException)
                    {
                        continue;
                    }

                    if (bytesRead > 0)
                    {
                        ProcessReceivedData(buffer, bytesRead);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Read error: {ex.Message}");

                    if (!IsDeviceConnected())
                    {
                        Log("Device disconnected");
                        IsConnected = false;
                        break;
                    }
                }
            }

            Log("Read loop ended");
        }

        private bool IsDeviceConnected()
        {
            try
            {
                if (_device == null) return false;
                return DeviceList.Local.GetHidDevices()
                    .Any(d => d.DevicePath == _device.DevicePath);
            }
            catch
            {
                return false;
            }
        }

        private readonly StringBuilder _dataBuffer = new StringBuilder();

        private void ProcessReceivedData(byte[] buffer, int length)
        {
            try
            {
                // Log raw data for debugging
                var hexData = BitConverter.ToString(buffer, 0, Math.Min(length, 32));
                Log($"Received {length} bytes: {hexData}...");

                // Check for MMS notification or response
                if (length > 2)
                {
                    byte reportId = buffer[0];
                    byte messageType = buffer[1];

                    // Check if this is an ARQC/card data notification
                    if (messageType == MmsNotifications.ARQCMessage ||
                        ContainsTransactionOutput(buffer, length))
                    {
                        ParseARQCData(buffer, length);
                        return;
                    }

                    // Check for transaction status
                    if (messageType == MmsNotifications.TransactionStatus)
                    {
                        ParseTransactionStatus(buffer, length);
                        return;
                    }
                }

                // Try to parse as text/JSON (some devices send ASCII)
                var textData = Encoding.UTF8.GetString(buffer, 0, length).Trim('\0');
                if (!string.IsNullOrWhiteSpace(textData) && textData.Contains("{"))
                {
                    _dataBuffer.Append(textData);
                    TryParseJsonCardData(_dataBuffer.ToString());
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing data: {ex.Message}");
            }
        }

        private bool ContainsTransactionOutput(byte[] buffer, int length)
        {
            var text = Encoding.UTF8.GetString(buffer, 0, length);
            return text.Contains("TransactionOutput") || text.Contains("ARQC") || text.Contains("EMVSREDData");
        }

        private void ParseARQCData(byte[] buffer, int length)
        {
            try
            {
                Log("Parsing ARQC data...");

                var cardData = new DynaflexCardDataEventArgs
                {
                    DeviceSerialNumber = _deviceSerialNumber
                };

                // Try to extract TLV data
                // Look for key tags: DFDF56 (KSN), DFDF59 (EMVSREDData), DFDF52 (CardType)

                var hexString = BitConverter.ToString(buffer, 0, length).Replace("-", "");

                // Find KSN (tag DFDF56)
                int ksnIdx = hexString.IndexOf("DFDF56");
                if (ksnIdx >= 0 && ksnIdx + 8 < hexString.Length)
                {
                    int ksnLen = Convert.ToInt32(hexString.Substring(ksnIdx + 6, 2), 16) * 2;
                    if (ksnIdx + 8 + ksnLen <= hexString.Length)
                    {
                        cardData.KSN = hexString.Substring(ksnIdx + 8, ksnLen);
                        Log($"Found KSN: {cardData.KSN}");
                    }
                }

                // Find EMVSREDData (tag DFDF59)
                int emvIdx = hexString.IndexOf("DFDF59");
                if (emvIdx >= 0 && emvIdx + 10 < hexString.Length)
                {
                    // Length is 2 bytes for DFDF59
                    int emvLen = Convert.ToInt32(hexString.Substring(emvIdx + 6, 4), 16) * 2;
                    if (emvIdx + 10 + emvLen <= hexString.Length)
                    {
                        cardData.EncryptedCardData = hexString.Substring(emvIdx + 10, Math.Min(emvLen, hexString.Length - emvIdx - 10));
                        Log($"Found EMVSREDData: {cardData.EncryptedCardData.Substring(0, Math.Min(32, cardData.EncryptedCardData.Length))}...");
                    }
                }

                // Find CardType (tag DFDF52)
                int typeIdx = hexString.IndexOf("DFDF52");
                if (typeIdx >= 0 && typeIdx + 10 < hexString.Length)
                {
                    string typeCode = hexString.Substring(typeIdx + 8, 2);
                    cardData.CardType = ParseCardTypeCode(typeCode);
                    Log($"Found CardType: {cardData.CardType}");
                }

                cardData.IsValid = !string.IsNullOrEmpty(cardData.KSN) || !string.IsNullOrEmpty(cardData.EncryptedCardData);

                if (cardData.IsValid)
                {
                    _isTransactionActive = false;
                    CardDataReceived?.Invoke(this, cardData);
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing ARQC: {ex.Message}");
            }
        }

        private void ParseTransactionStatus(byte[] buffer, int length)
        {
            if (length < 4) return;

            byte statusCode = buffer[2];
            string status = statusCode switch
            {
                0x00 => "idle",
                0x01 => "waiting_for_card",
                0x02 => "card_inserted",
                0x03 => "reading_card",
                0x04 => "processing",
                0x05 => "complete",
                0x06 => "error",
                0x07 => "timeout",
                0x08 => "cancelled",
                _ => $"unknown_{statusCode:X2}"
            };

            Log($"Transaction status: {status}");
            TransactionStatusChanged?.Invoke(this, new DynaflexTransactionEventArgs(status, ""));
        }

        private void TryParseJsonCardData(string data)
        {
            try
            {
                if (!data.Contains("TransactionOutput") && !data.Contains("CardData"))
                    return;

                int start = data.IndexOf('{');
                int end = data.LastIndexOf('}');

                if (start >= 0 && end > start)
                {
                    var jsonStr = data.Substring(start, end - start + 1);

                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;

                    var cardData = new DynaflexCardDataEventArgs
                    {
                        RawJson = jsonStr,
                        DeviceSerialNumber = _deviceSerialNumber
                    };

                    if (root.TryGetProperty("TransactionOutput", out var txOutput))
                    {
                        if (txOutput.TryGetProperty("KSN", out var ksn))
                            cardData.KSN = ksn.GetString() ?? "";
                        if (txOutput.TryGetProperty("EMVSREDData", out var emv))
                            cardData.EncryptedCardData = emv.GetString() ?? "";
                        if (txOutput.TryGetProperty("DeviceSerialNumber", out var serial))
                            cardData.DeviceSerialNumber = serial.GetString() ?? _deviceSerialNumber;
                        if (txOutput.TryGetProperty("CardType", out var type))
                            cardData.CardType = ParseCardTypeCode(type.GetString());
                    }

                    cardData.IsValid = !string.IsNullOrEmpty(cardData.KSN) || !string.IsNullOrEmpty(cardData.EncryptedCardData);

                    if (cardData.IsValid)
                    {
                        _dataBuffer.Clear();
                        _isTransactionActive = false;
                        Log("Card data parsed from JSON");
                        CardDataReceived?.Invoke(this, cardData);
                    }
                }
            }
            catch (JsonException)
            {
                // Not complete JSON yet
            }
            catch (Exception ex)
            {
                Log($"JSON parse error: {ex.Message}");
            }
        }

        private string ParseCardTypeCode(string? code)
        {
            return code switch
            {
                "01" => "Visa",
                "02" => "Mastercard",
                "05" => "Amex",
                "06" => "Discover",
                "07" => "JCB",
                "08" => "UnionPay",
                _ => code ?? "Unknown"
            };
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Build the card data string for Forte REST API from Dynaflex card data
        /// Format: JSON with TransactionOutput containing KSN, EMVSREDData, DeviceSerialNumber, CardType
        /// </summary>
        public static string BuildForteCardData(DynaflexCardDataEventArgs cardData)
        {
            if (cardData == null || !cardData.IsValid)
            {
                return string.Empty;
            }

            try
            {
                // Build TransactionOutput JSON for Forte
                var transactionOutput = new
                {
                    KSN = cardData.KSN,
                    DeviceSerialNumber = cardData.DeviceSerialNumber,
                    EMVSREDData = cardData.EncryptedCardData,
                    CardType = cardData.CardType
                };

                return JsonSerializer.Serialize(transactionOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynaflexService] Error building Forte card data: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Disconnect();
        }
    }

    /// <summary>
    /// Result of card reader test
    /// </summary>
    public class DynaflexTestResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DynaflexCardDataEventArgs? CardData { get; set; }
        public List<string> Logs { get; } = new();
    }

    /// <summary>
    /// Transaction status event args
    /// </summary>
    public class DynaflexTransactionEventArgs : EventArgs
    {
        public string Status { get; }
        public string Message { get; }

        public DynaflexTransactionEventArgs(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    /// <summary>
    /// Information about a detected Dynaflex device
    /// </summary>
    public class DynaflexDeviceInfo
    {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string DevicePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event args for card data received from Dynaflex
    /// </summary>
    public class DynaflexCardDataEventArgs : EventArgs
    {
        public bool IsValid { get; set; }
        public string KSN { get; set; } = string.Empty;
        public string EncryptedCardData { get; set; } = string.Empty;
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string? Track1 { get; set; }
        public string? Track2 { get; set; }
        public string? RawJson { get; set; }
        public string CardLast4 { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event args for connection state changes
    /// </summary>
    public class DynaflexConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string DeviceSerialNumber { get; }

        public DynaflexConnectionEventArgs(bool isConnected, string serialNumber)
        {
            IsConnected = isConnected;
            DeviceSerialNumber = serialNumber;
        }
    }
}
