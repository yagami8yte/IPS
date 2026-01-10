using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MTUSDKNET;

namespace IPS.Services
{
    /// <summary>
    /// Service for integrating MagTek Dynaflex II Go card reader using the official MTUSDKNET SDK.
    /// Requires the MagneFlex Powder V2 API service to be installed and running.
    ///
    /// This implementation follows the EXACT same flow as the official SDK demo (MainWindow.xaml.cs).
    /// </summary>
    public class DynaflexSdkService : IDisposable, IEventSubscriber
    {
        // Device references - exactly like demo
        private List<IDevice>? mDeviceList = null;
        private IDevice? mDevice = null;
        private ITransaction? mTransaction = null;

        private bool _isDisposed;

        // Transaction state for async waiting
        private TaskCompletionSource<DynaflexArqcData>? _arqcCompletionSource;
        private CancellationTokenSource? _transactionCancellation;

        // Events
        public event EventHandler<DynaflexArqcData>? ArqcDataReceived;
        public event EventHandler<DynaflexSdkConnectionEventArgs>? ConnectionStateChanged;
        public event EventHandler<DynaflexSdkTransactionEventArgs>? TransactionStatusChanged;
        public event EventHandler<string>? DisplayMessageReceived;
        public event EventHandler<string>? LogMessage;

        /// <summary>
        /// Whether the Dynaflex device is currently connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (mDevice == null) return false;
                return mDevice.getConnectionState() == ConnectionState.Connected;
            }
        }

        private void Log(string message)
        {
            var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] [DynaflexSDK] {message}";
            Console.WriteLine(timestamped);
            LogMessage?.Invoke(this, timestamped);
        }

        /// <summary>
        /// Scan for available Dynaflex devices using the SDK (async to not block UI)
        /// EXACTLY matches demo's scanDevices() method
        /// </summary>
        public async Task<List<DynaflexSdkDeviceInfo>> ScanDevicesAsync()
        {
            return await Task.Run(() => ScanDevices());
        }

        /// <summary>
        /// Scan for available Dynaflex devices using the SDK
        /// EXACTLY matches demo's scanDevices() method
        /// </summary>
        public List<DynaflexSdkDeviceInfo> ScanDevices()
        {
            var devices = new List<DynaflexSdkDeviceInfo>();

            // Exactly like demo - don't refresh while connected
            if (mDevice != null)
            {
                if (mDevice.getConnectionState() == ConnectionState.Connected)
                {
                    Log("Refreshing Device List is not allowed while connected to a device.");
                    // Return current device info
                    var deviceInfo = mDevice.getDeviceInfo();
                    var connectionInfo = mDevice.getConnectionInfo();
                    devices.Add(new DynaflexSdkDeviceInfo
                    {
                        Name = deviceInfo?.getName() ?? "Unknown",
                        Model = deviceInfo?.getModel() ?? "Unknown",
                        Address = connectionInfo?.getAddress() ?? "",
                        ConnectionType = connectionInfo?.getConnectionType().ToString() ?? "Unknown",
                        Device = mDevice
                    });
                    return devices;
                }
            }

            try
            {
                Log("Scanning for MagTek devices via SDK...");
                Log($"SDK API Version: {CoreAPI.getAPIVersion()}");

                mDevice = null;

                // EXACTLY like demo - use DeviceType.MMS for DynaFlex
                mDeviceList = CoreAPI.getDeviceList(DeviceType.MMS);

                if (mDeviceList == null || mDeviceList.Count == 0)
                {
                    Log("No MagTek MMS devices found. Is MagneFlex Powder V2 API service running?");
                    return devices;
                }

                Log($"Found {mDeviceList.Count} device(s)");

                foreach (var device in mDeviceList)
                {
                    var connectionInfo = device.getConnectionInfo();
                    var deviceInfo = device.getDeviceInfo();

                    string name = deviceInfo?.getName() ?? "Unknown";
                    string model = deviceInfo?.getModel() ?? "Unknown";
                    string address = connectionInfo?.getAddress() ?? "";
                    var connectionType = connectionInfo?.getConnectionType().ToString() ?? "Unknown";

                    Log($"  Device: {name} ({model}), Connection: {connectionType}, Address: {address}");

                    devices.Add(new DynaflexSdkDeviceInfo
                    {
                        Name = name,
                        Model = model,
                        Address = address,
                        ConnectionType = connectionType,
                        Device = device
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error scanning devices: {ex.Message}");
                Log("Make sure MagneFlex Powder V2 API service is installed and running.");
            }

            return devices;
        }

        /// <summary>
        /// Connect to the first available Dynaflex device
        /// EXACTLY matches demo's connectDevice() method
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() => ConnectSync());
        }

        /// <summary>
        /// Connect to the first available Dynaflex device (sync version)
        /// EXACTLY matches demo's connectDevice() method
        /// </summary>
        public bool ConnectSync()
        {
            try
            {
                // Scan for devices if we haven't already
                if (mDeviceList == null || mDeviceList.Count == 0)
                {
                    mDeviceList = CoreAPI.getDeviceList(DeviceType.MMS);
                }

                if (mDeviceList == null || mDeviceList.Count == 0)
                {
                    Log("No MagTek devices found");
                    return false;
                }

                // Use the first device - exactly like demo's getSelectedDevice()
                mDevice = mDeviceList[0];

                if (mDevice == null)
                {
                    Log("Device is null");
                    return false;
                }

                // EXACTLY like demo's connectDevice() method
                if (mDevice.getConnectionState() == ConnectionState.Connected)
                {
                    Log("Device is already connected.");
                    return true;
                }

                Log("Connecting...");

                IDeviceControl deviceControl = mDevice.getDeviceControl();

                if (deviceControl != null)
                {
                    // EXACTLY like demo - unsubscribe then subscribe, then open
                    mDevice.unsubscribeAll(this);
                    mDevice.subscribeAll(this);

                    deviceControl.open();

                    // Wait for connection to establish
                    Thread.Sleep(1500);

                    if (mDevice.getConnectionState() == ConnectionState.Connected)
                    {
                        Log("[CONNECTED]");
                        var deviceInfo = mDevice.getDeviceInfo();
                        Log($"Device: {deviceInfo?.getName() ?? "Unknown"}");
                        ConnectionStateChanged?.Invoke(this, new DynaflexSdkConnectionEventArgs(true, deviceInfo?.getName() ?? ""));
                        return true;
                    }
                    else
                    {
                        Log($"Connection state: {mDevice.getConnectionState()}");
                    }
                }

                Log("Failed to connect");
                return false;
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the Dynaflex device
        /// EXACTLY matches demo's disconnectDevice() method
        /// </summary>
        public void Disconnect()
        {
            if (mDevice == null)
                return;

            Log("Disconnecting...");

            CancelTransaction();

            try
            {
                IDeviceControl deviceControl = mDevice.getDeviceControl();

                if (deviceControl != null)
                {
                    deviceControl.close();
                }
            }
            catch (Exception ex)
            {
                Log($"Error during disconnect: {ex.Message}");
            }

            ConnectionStateChanged?.Invoke(this, new DynaflexSdkConnectionEventArgs(false, ""));
            Log("[DISCONNECTED]");
        }

        /// <summary>
        /// Start an EMV transaction and wait for card data (ARQC)
        /// EXACTLY matches demo's startTransaction() method
        /// </summary>
        public async Task<DynaflexArqcData?> StartTransactionAsync(decimal amount, int timeoutSeconds = 60)
        {
            if (mDevice == null)
            {
                Log("Cannot start transaction - device is null");
                return null;
            }

            if (mDevice.getConnectionState() != ConnectionState.Connected)
            {
                Log("Cannot start transaction - not connected");
                return null;
            }

            try
            {
                Log($"Starting EMV transaction for amount: ${amount:F2}");

                // Cancel any existing transaction
                CancelTransaction();

                // Create new completion source for async waiting
                _arqcCompletionSource = new TaskCompletionSource<DynaflexArqcData>();
                _transactionCancellation = new CancellationTokenSource();

                // ============================================================
                // EXACTLY like demo's startTransaction() method
                // ============================================================

                // Payment methods - using checkboxes like demo
                bool msr = true;      // MSR enabled
                bool contact = true;  // Contact (chip) enabled
                bool contactless = true; // Contactless (tap) enabled

                // Create transaction - EXACTLY like demo
                mTransaction = new Transaction();
                mTransaction.Timeout = 255;  // Demo uses 255
                mTransaction.TransactionType = 0;  // Sale
                mTransaction.Amount = amount.ToString("F2");
                mTransaction.QuickChip = true;  // QuickChip for faster transactions

                // Build payment methods - EXACTLY like demo
                List<PaymentMethod> paymentMethods = TransactionBuilder.GetPaymentMethods(msr, contact, contactless, false);
                mTransaction.PaymentMethods = paymentMethods;

                // Set currency code - EXACTLY like demo (0x08, 0x40 = USD)
                mTransaction.CurrencyCode = new byte[] { 0x08, 0x40 }; // Tag 5F2A

                // ============================================================
                // CRITICAL: Re-subscribe before each transaction - EXACTLY like demo
                // ============================================================
                mDevice.unsubscribeAll(this);
                mDevice.subscribeAll(this);

                // Start the transaction - EXACTLY like demo
                if (mDevice.startTransaction(mTransaction))
                {
                    Log("[Transaction Started]");
                    Log($"Amount={mTransaction.Amount}, Timeout={mTransaction.Timeout}, Transaction Type={mTransaction.TransactionType}");
                    Log($"MSR={msr}, Contact={contact}, Contactless={contactless}");

                    TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("waiting_for_card", "Please insert, tap, or swipe card"));

                    // Wait for ARQC data or timeout
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(_transactionCancellation.Token);
                    cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                    try
                    {
                        var result = await _arqcCompletionSource.Task.WaitAsync(cts.Token);
                        Log("ARQC data received successfully");
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        Log("Transaction cancelled or timed out");
                        TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("timeout", "Transaction timed out"));
                        return null;
                    }
                }
                else
                {
                    // EXACTLY like demo - show error message
                    Log("PAYMENT METHOD NOT SUPPORTED");
                    TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("error", "Payment method not supported"));
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting transaction: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cancel the current transaction
        /// EXACTLY matches demo's cancellTransaction() method
        /// </summary>
        public void CancelTransaction()
        {
            try
            {
                _transactionCancellation?.Cancel();

                if (mDevice != null)
                {
                    mDevice.cancelTransaction();
                }

                mTransaction = null;
                _arqcCompletionSource?.TrySetCanceled();
                _arqcCompletionSource = null;
            }
            catch (Exception ex)
            {
                Log($"Error cancelling transaction: {ex.Message}");
            }
        }

        #region IEventSubscriber Implementation - EXACTLY like demo

        /// <summary>
        /// Event handler - EXACTLY matches demo's OnEvent() method
        /// </summary>
        public void OnEvent(EventType eventType, IData data)
        {
            try
            {
                switch (eventType)
                {
                    case EventType.ConnectionState:
                        {
                            // EXACTLY like demo
                            ConnectionState value = ConnectionStateBuilder.GetValue(data.StringValue);
                            if (value == ConnectionState.Connected)
                            {
                                Log("[CONNECTED]");
                                ConnectionStateChanged?.Invoke(this, new DynaflexSdkConnectionEventArgs(true, ""));
                            }
                            else if (value == ConnectionState.Disconnected)
                            {
                                Log("[DISCONNECTED]");
                                ConnectionStateChanged?.Invoke(this, new DynaflexSdkConnectionEventArgs(false, ""));
                            }
                            else if (value == ConnectionState.Disconnecting)
                            {
                                Log("[DISCONNECTING]");
                            }
                            else if (value == ConnectionState.Connecting)
                            {
                                Log("[CONNECTING]");
                            }
                        }
                        break;

                    case EventType.DeviceResponse:
                        Log("[Response]\n" + data.StringValue);
                        break;

                    case EventType.DeviceExtendedResponse:
                        Log("[Extended Response]\n" + data.StringValue);
                        break;

                    case EventType.DeviceNotification:
                        Log("[Notification]\n" + data.StringValue);
                        break;

                    case EventType.CardData:
                        // EXACTLY like demo - MSR swipe
                        Log("[MSR]\n" + data.StringValue);
                        HandleCardData(data);
                        break;

                    case EventType.TransactionStatus:
                        HandleTransactionStatus(data);
                        break;

                    case EventType.DisplayMessage:
                        {
                            // EXACTLY like demo
                            string displayMessage = data.StringValue;
                            if (displayMessage.Length > 1)
                            {
                                displayMessage = displayMessage.Replace((char)0, '\n').Replace((char)0x0A, '\n');
                                Log("[DisplayMessage] : " + displayMessage);
                                DisplayMessageReceived?.Invoke(this, displayMessage);
                            }
                        }
                        break;

                    case EventType.ClearDisplay:
                        Log("[ClearDisplay]");
                        break;

                    case EventType.AuthorizationRequest:
                        // EXACTLY like demo - this contains the ARQC data
                        Log("[Authorization Request]\n" + GetHexString(data.ByteArray));
                        HandleAuthorizationRequest(data);
                        break;

                    case EventType.TransactionResult:
                        // EXACTLY like demo
                        Log("[Transaction Result]\n" + GetHexString(data.ByteArray));
                        TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("complete", "Transaction complete"));
                        break;

                    case EventType.OperationStatus:
                        {
                            // EXACTLY like demo
                            OperationStatus opStatus = OperationStatusBuilder.GetStatusCode(data.StringValue);
                            string opDetail = OperationStatusBuilder.GetOperationDetail(data.StringValue);
                            if (opStatus == OperationStatus.Started)
                            {
                                Log("[OPERATION STARTED: " + opDetail + "]");
                            }
                            else if (opStatus == OperationStatus.Warning)
                            {
                                Log("[OPERATION WARNING: " + opDetail + "]");
                            }
                            else if (opStatus == OperationStatus.Failed)
                            {
                                Log("[OPERATION FAILED: " + opDetail + "]");
                            }
                            else if (opStatus == OperationStatus.Done)
                            {
                                Log("[OPERATION DONE: " + opDetail + "]");
                            }
                        }
                        break;

                    case EventType.DeviceEvent:
                        {
                            // EXACTLY like demo
                            DeviceEvent deviceEvent = DeviceEventBuilder.GetEventValue(data.StringValue);
                            string eventDetail = DeviceEventBuilder.GetDetail(data.StringValue);
                            Log($"[Device Event: {deviceEvent}] {eventDetail}");
                        }
                        break;

                    default:
                        Log($"[Event: {eventType}] {data.StringValue}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling event {eventType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle TransactionStatus events - EXACTLY like demo
        /// </summary>
        private void HandleTransactionStatus(IData data)
        {
            // EXACTLY like demo
            TransactionStatus status = TransactionStatusBuilder.GetStatusCode(data.StringValue);

            if (status == TransactionStatus.CardSwiped)
            {
                Log("[CARD SWIPED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("card_swiped", "Card swiped"));
            }
            else if (status == TransactionStatus.CardInserted)
            {
                Log("[CARD INSERTED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("card_inserted", "Card inserted"));
            }
            else if (status == TransactionStatus.CardRemoved)
            {
                Log("[CARD REMOVED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("card_removed", "Card removed"));
            }
            else if (status == TransactionStatus.CardDetected)
            {
                Log("[CARD DETECTED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("card_detected", "Card detected"));
            }
            else if (status == TransactionStatus.CardCollision)
            {
                Log("[CARD COLLISION]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("card_collision", "Card collision"));
            }
            else if (status == TransactionStatus.TimedOut)
            {
                Log("[TRANSACTION TIMED OUT]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("timeout", "Transaction timed out"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.HostCancelled)
            {
                Log("[HOST CANCELLED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("cancelled", "Host cancelled"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.TransactionCancelled)
            {
                Log("[TRANSACTION CANCELLED]");
                string statusDetail = TransactionStatusBuilder.GetStatusDetail(data.StringValue);
                string deviceDetail = TransactionStatusBuilder.GetDeviceDetail(data.StringValue);
                Log("(Status Detail=" + statusDetail + ")");
                Log("(Device Detail=" + deviceDetail + ")");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("cancelled", "Transaction cancelled"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.TransactionInProgress)
            {
                Log("[TRANSACTION IN PROGRESS]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("in_progress", "Transaction in progress"));
            }
            else if (status == TransactionStatus.TransactionError)
            {
                Log("[TRANSACTION ERROR]");
                string statusDetail = TransactionStatusBuilder.GetStatusDetail(data.StringValue);
                string deviceDetail = TransactionStatusBuilder.GetDeviceDetail(data.StringValue);
                Log("(Status Detail=" + statusDetail + ")");
                Log("(Device Detail=" + deviceDetail + ")");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("error", $"Transaction error: {statusDetail}"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.TransactionCompleted)
            {
                Log("[TRANSACTION COMPLETED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("completed", "Transaction completed"));
            }
            else if (status == TransactionStatus.TransactionApproved)
            {
                Log("[TRANSACTION APPROVED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("approved", "Transaction approved"));
            }
            else if (status == TransactionStatus.TransactionDeclined)
            {
                Log("[TRANSACTION DECLINED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("declined", "Transaction declined"));
            }
            else if (status == TransactionStatus.TransactionFailed)
            {
                Log("[TRANSACTION FAILED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("failed", "Transaction failed"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.TransactionNotAccepted)
            {
                Log("[TRANSACTION NOT ACCEPTED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("not_accepted", "Transaction not accepted"));
                _arqcCompletionSource?.TrySetCanceled();
            }
            else if (status == TransactionStatus.QuickChipDeferred)
            {
                Log("[TRANSACTION STATUS / QUICK CHIP DEFERRED]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("quickchip_deferred", "Quick chip deferred"));
            }
            else if (status == TransactionStatus.TryAnotherInterface)
            {
                Log("[TRY ANOTHER INTERFACE]");
                TransactionStatusChanged?.Invoke(this, new DynaflexSdkTransactionEventArgs("try_another", "Try another interface"));
            }
            else
            {
                Log($"[Transaction Status: {status}]");
            }
        }

        /// <summary>
        /// Handle AuthorizationRequest - contains ARQC data for Forte
        /// EXACTLY like demo's handling of EventType.AuthorizationRequest
        /// </summary>
        private void HandleAuthorizationRequest(IData data)
        {
            try
            {
                var arqcData = ParseArqcData(data.ByteArray);

                if (arqcData != null && arqcData.IsValid)
                {
                    Log("ARQC Data Parsed Successfully:");
                    Log($"  Card Type: {arqcData.CardTypeName} ({arqcData.CardType})");
                    Log($"  Device S/N: {arqcData.DeviceSerialNumber}");
                    Log($"  KSN: {arqcData.KSN}");
                    Log($"  EMVSREDData: {arqcData.EMVSREDData?.Length ?? 0} chars");

                    ArqcDataReceived?.Invoke(this, arqcData);
                    _arqcCompletionSource?.TrySetResult(arqcData);
                }
                else
                {
                    Log("Failed to parse ARQC data");
                    Log($"  KSN present: {!string.IsNullOrEmpty(arqcData?.KSN)}");
                    Log($"  EMVSREDData present: {!string.IsNullOrEmpty(arqcData?.EMVSREDData)}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing ARQC: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle CardData (MSR swipe) - EXACTLY like demo
        /// </summary>
        private void HandleCardData(IData data)
        {
            try
            {
                // MSR data is also TLV encoded - parse same as ARQC
                var bytes = HexStringToBytes(data.StringValue);
                var arqcData = ParseArqcData(bytes);

                if (arqcData != null && arqcData.IsValid)
                {
                    Log("MSR Data Parsed Successfully:");
                    Log($"  Card Type: {arqcData.CardTypeName}");
                    Log($"  KSN: {arqcData.KSN}");

                    ArqcDataReceived?.Invoke(this, arqcData);
                    _arqcCompletionSource?.TrySetResult(arqcData);
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing MSR data: {ex.Message}");
            }
        }

        #endregion

        #region TLV Parsing - Standard BER-TLV parsing

        private DynaflexArqcData? ParseArqcData(byte[]? data)
        {
            if (data == null || data.Length == 0)
                return null;

            var arqcData = new DynaflexArqcData();

            try
            {
                // Parse TLV data
                var tlvPayload = GetTlvPayload(data);
                var parsedTlv = ParseTlv(tlvPayload);

                // Extract required fields using MagTek's TLV tags:
                // DFDF56 = KSN
                arqcData.KSN = GetTagHexValue(parsedTlv, "DFDF56");

                // DFDF25 = Device Serial Number
                var snBytes = GetTagByteValue(parsedTlv, "DFDF25");
                if (snBytes != null)
                {
                    arqcData.DeviceSerialNumber = Encoding.UTF8.GetString(snBytes).Trim('\0');
                }

                // DFDF52 = Card Type
                arqcData.CardType = GetTagHexValue(parsedTlv, "DFDF52");
                arqcData.CardTypeName = ParseCardType(arqcData.CardType);

                // DFDF59 = EMVSREDData (encrypted card data for Forte)
                arqcData.EMVSREDData = GetTagHexValue(parsedTlv, "DFDF59");

                // Store raw data for debugging
                arqcData.RawArqc = BitConverter.ToString(data).Replace("-", "");

                arqcData.IsValid = !string.IsNullOrEmpty(arqcData.KSN) && !string.IsNullOrEmpty(arqcData.EMVSREDData);
            }
            catch (Exception ex)
            {
                Log($"Error parsing TLV: {ex.Message}");
            }

            return arqcData;
        }

        private byte[] GetTlvPayload(byte[] data)
        {
            // SDK demo format: 2-byte big-endian length prefix followed by TLV data
            // Example: 01 A0 [416 bytes of TLV data starting with F9...]

            if (data == null || data.Length <= 2)
                return data ?? Array.Empty<byte>();

            int tlvLen = ((data[0] & 0xFF) << 8) + (data[1] & 0xFF);

            if (tlvLen > 0 && tlvLen <= data.Length - 2)
            {
                var payload = new byte[tlvLen];
                Array.Copy(data, 2, payload, 0, tlvLen);
                return payload;
            }

            return data;
        }

        private Dictionary<string, byte[]> ParseTlv(byte[] data)
        {
            // Parse TLV matching SDK demo's MTParser.parseTLV behavior:
            // - For constructed tags (bit 6 set): DON'T skip value bytes, continue parsing into them
            // - For primitive tags: skip value bytes after storing

            var result = new Dictionary<string, byte[]>();
            int i = 0;

            while (i < data.Length)
            {
                // Parse tag (BER-TLV format)
                string tag = "";
                byte firstByte = data[i];

                if ((data[i] & 0x1F) == 0x1F)
                {
                    // Multi-byte tag
                    tag = data[i].ToString("X2");
                    i++;
                    while (i < data.Length && (data[i] & 0x80) != 0)
                    {
                        tag += data[i].ToString("X2");
                        i++;
                    }
                    if (i < data.Length)
                    {
                        tag += data[i].ToString("X2");
                        i++;
                    }
                }
                else
                {
                    tag = data[i].ToString("X2");
                    i++;
                }

                if (i >= data.Length) break;

                // Parse length (BER-TLV format)
                int length = 0;
                if ((data[i] & 0x80) != 0)
                {
                    int numLenBytes = data[i] & 0x7F;
                    i++;
                    for (int j = 0; j < numLenBytes && i < data.Length; j++)
                    {
                        length = (length << 8) | data[i];
                        i++;
                    }
                }
                else
                {
                    length = data[i];
                    i++;
                }

                // Check if constructed (bit 6 set in first tag byte)
                bool isConstructed = (firstByte & 0x20) != 0;

                if (isConstructed)
                {
                    // Constructed tag: DON'T skip value bytes, parser will continue into nested content
                    // Just store a marker (SDK stores "[Container]")
                    result[tag] = Array.Empty<byte>();
                }
                else
                {
                    // Primitive tag: store value and skip past it
                    if (i + length <= data.Length)
                    {
                        var value = new byte[length];
                        Array.Copy(data, i, value, 0, length);
                        result[tag] = value;
                        i += length;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private string? GetTagHexValue(Dictionary<string, byte[]> tlv, string tag)
        {
            if (tlv.TryGetValue(tag, out var value))
            {
                return BitConverter.ToString(value).Replace("-", "");
            }
            return null;
        }

        private byte[]? GetTagByteValue(Dictionary<string, byte[]> tlv, string tag)
        {
            return tlv.TryGetValue(tag, out var value) ? value : null;
        }

        private string ParseCardType(string? cardTypeHex)
        {
            return cardTypeHex switch
            {
                "01" => "Visa",
                "02" => "Mastercard",
                "05" => "Amex",
                "06" => "Discover",
                "07" => "JCB",
                "08" => "UnionPay",
                _ => cardTypeHex ?? "Unknown"
            };
        }

        private string GetHexString(byte[]? data)
        {
            if (data == null || data.Length == 0)
                return "";
            return BitConverter.ToString(data).Replace("-", "");
        }

        private byte[] HexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Array.Empty<byte>();

            hex = hex.Replace("-", "").Replace(" ", "");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Disconnect();
            _transactionCancellation?.Dispose();
        }
    }

    /// <summary>
    /// ARQC data received from Dynaflex containing encrypted card data for Forte
    /// </summary>
    public class DynaflexArqcData
    {
        public bool IsValid { get; set; }
        public string KSN { get; set; } = string.Empty;
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string EMVSREDData { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardTypeName { get; set; } = string.Empty;
        public string RawArqc { get; set; } = string.Empty;

        /// <summary>
        /// Build the TransactionOutput JSON string for Forte REST API
        /// </summary>
        public string ToForteTransactionOutput()
        {
            return $"{{\"TransactionOutput\":{{\"KSN\":\"{KSN}\",\"DeviceSerialNumber\":\"{DeviceSerialNumber}\",\"EMVSREDData\":\"{EMVSREDData}\",\"CardType\":\"{CardType}\"}}}}";
        }
    }

    /// <summary>
    /// Device info from SDK scan
    /// </summary>
    public class DynaflexSdkDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public IDevice? Device { get; set; }
    }

    /// <summary>
    /// Connection state event args
    /// </summary>
    public class DynaflexSdkConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string DeviceSerialNumber { get; }

        public DynaflexSdkConnectionEventArgs(bool isConnected, string serialNumber)
        {
            IsConnected = isConnected;
            DeviceSerialNumber = serialNumber;
        }
    }

    /// <summary>
    /// Transaction status event args
    /// </summary>
    public class DynaflexSdkTransactionEventArgs : EventArgs
    {
        public string Status { get; }
        public string Message { get; }

        public DynaflexSdkTransactionEventArgs(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
