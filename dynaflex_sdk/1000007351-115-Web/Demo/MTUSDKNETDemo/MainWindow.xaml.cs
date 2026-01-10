using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MTAESDUKPT;
using MTUSDKNET;

namespace MTUSDKDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventSubscriber, IConfigurationCallback, IFallbackAdapter, ISystemStatusCallback, IMQTTDeviceStatusCallback
    {
        private int WINDOW_HEIGHT = 620;
        private int MAIN_PANEL_HEIGHT = 350;

        private delegate void UIDispatcher();
        private delegate void UIStringDispatcher(string text);
        private delegate void UIBoolDispatcher(bool enable);
        private delegate void UIDeviceBoolDispatcher(IDevice device, bool enable);
        private delegate void UIByteArrayDispatcher(byte[] data);
        private delegate void UISelectionsDisptacher(string title, int selectionType, List<string> selectionList, long timeout);
        private delegate void UIEnhancedSelectionsDisptacher(string title, int selectionType, List<DirectoryEntry> selectionList, long timeout);

        protected bool IsCheckedState = false;

        protected List<IDevice> mDeviceList = null;
        protected IDevice mDevice = null;

        protected ITransaction mTransaction = null;

        protected string mGetFileName = "";

        protected PANRequest mPANRequest = null;

        protected enum FileTransferMode { NONE, SEND_IMAGE, SEND_FILE, GET_FILE, UPDATE_FIRMWARE };

        protected FileTransferMode mFileTransferMode = FileTransferMode.NONE;

        public bool GetSignatureFromDevice { get; set; }
        public bool Fallback { get; set; }
        public bool EventDrivenTransaction { get; set; }
        public bool NFCReadOnlyMode { get; set; }

        private Point mSignaturePoint = new Point();

        private FallbackManager mFallbackManager = null;

        private bool mNonDisplayDevice = false;

        private string[] mUIStringList = DisplayStrings.StringList;

        private DeviceUIPageSettings mDeviceUIPageSettings = null;

        private MQTTSettings mMQTTSettings = new MQTTSettings();

        private enum NFCState
        {
            NONE, ENABLED, TAG_DETECTED, GET_VERSION, FAST_READ, READY, WRITE,
            CLASSIC_1K_DETECTED, CLASSIC_4K_DETECTED, CLASSIC_1K_READ, CLASSIC_4K_READ,
            CLASSIC_1K_READY, CLASSIC_4K_READY, CLASSIC_1K_WRITE, CLASSIC_4K_WRITE,
            DESFIRE_DETECTED, DESFIRE_GET_VERSION_P1, DESFIRE_GET_VERSION_P2, DESFIRE_GET_VERSION_P3,
            DESFIRE_SELECT, DESFIRE_GET_VALUE
        }

        private NFCState mNFCState = NFCState.NONE;

        private List<MTNdefRecord> mNDEFRecords = null;

        private byte[] mNDEFBytes = null;
        private int mNDEFBlock = 0;

        private int mReadSector;
        private int mWriteSector;

        private List<string> mClassicNFCData = null;

        private const string CLASSIC_KEY_A = "FFFFFFFFFFFF";
        private const string CLASSIC_KEY_B = "FFFFFFFFFFFF";

        private string[] ClassicKeyA =  {   CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 0,1,2,3
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 4,5,6,7
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 8,9,10,11
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 12,13,14,15
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 16,17,18,19
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 20,21,22,23
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 24,25,26,27
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 28,29,30,31
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A,     // Sector 32,33,34,35
                                            CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A, CLASSIC_KEY_A };   // Sector 36,37,38,39

        private string[] ClassicKeyB =  {   CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 0,1,2,3
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 4,5,6,7
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 8,9,10,11
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 12,13,14,15
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 16,17,18,19
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 20,21,22,23
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 24,25,26,27
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 28,29,30,31
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B,     // Sector 32,33,34,35
                                            CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B, CLASSIC_KEY_B };   // Sector 36,37,38,39

        private CustomizedSettings mCustomizedSettings = new CustomizedSettings();

        private bool mDeviceListOutDated = true;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            Version versionInfo = Assembly.GetExecutingAssembly().GetName().Version;

            this.Title = "MTUSDKNET Demo " + versionInfo.ToString();

            sendToOutput("API Version: " + CoreAPI.getAPIVersion());

            ShowImageIDCB.SelectedIndex = 0;
            SendImageIDCB.SelectedIndex = 0;
            SetDisplayImageIDCB.SelectedIndex = 1;

            NFCReadOnlyMode = true;

            mDeviceListOutDated = true;

            //MagTek.Logger.Default.LogReceived += Logger_LogReceived;

            initDeviceWatchers();

            scanDevices();
        }

        private void startMQTTDeviceStatusMonitoring()
        {
            CoreAPI.startMQTTDeviceStatusMonitoring(this);
        }

        private void stopMQTTDeviceStatusMonitoring()
        {
            CoreAPI.stopMQTTDeviceStatusMonitoring();
        }

        public void OnError(ErrorType error, string details)
        {
            sendToOutput("System Status Error: " + details);
        }

        public void OnConnected(string deviceAddress)
        {
            sendToOutput("MQTT Device Connected: " + deviceAddress);

            setDeviceListOutdated(true);
        }

        public void OnDisconnected(string deviceAddress)
        {
            sendToOutput("MQTT Device Disconnected: " + deviceAddress);

            setDeviceListOutdated(true);
        }

        private void setDeviceListOutdated(bool outdated)
        {
            mDeviceListOutDated = outdated;

            updateScanButtonHighlight();
        }

        private void updateScanButtonHighlight()
        {
            setScanButtonHighlight(mDeviceListOutDated);
        }

        private void setScanButtonHighlight(bool highlight)
        {
            try
            {
                if (ScanButton.Dispatcher.CheckAccess())
                {
                    ScanButton.Background = highlight ? Brushes.Yellow : SystemColors.ControlBrush;

                }
                else
                {
                    ScanButton.Dispatcher.BeginInvoke(new UIBoolDispatcher(setDeviceListOutdated),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { highlight });
                }
            }
            catch (Exception)
            {
            }
        }

        private void Logger_LogReceived(MagTek.LoggerFlags Flags, string Info)
        {
            if (Flags == MagTek.LoggerFlags.LF_DEVICE)
                sendToOutput("** Device: " + Info);
            else if (Flags == MagTek.LoggerFlags.LF_COMMUNICATION)
                sendToOutput("** Communication: " + Info);
        }

        private void initDeviceWatchers()
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
            insertQuery.GroupWithinInterval = new TimeSpan(0, 0, 1);
            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
            removeQuery.GroupWithinInterval = new TimeSpan(0, 0, 1);
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            sendToOutput("[Device Inserted]");

            //scanDevices();
            setDeviceListOutdated(true);
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            sendToOutput("[Device Removed]");

            //scanDevices();
            setDeviceListOutdated(true);
        }

        private void enableTransactionUI(bool enable)
        {
            try
            {
                if (CardTypePanel.Dispatcher.CheckAccess())
                {
                    CardTypePanel.Visibility = enable ? Visibility.Hidden : Visibility.Visible;
                    StartButton.IsEnabled = enable;
                    ReaderFeaturePanel.IsEnabled = enable;
                    CancelButton.IsEnabled = !enable;
                }
                else
                {
                    CardTypePanel.Dispatcher.BeginInvoke(new UIBoolDispatcher(enableTransactionUI),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { enable });
                }
            }
            catch (Exception)
            {
            }
        }

        private void enableNFCTagButton(bool enable)
        {
            try
            {
                if (NFCTagButton.Dispatcher.CheckAccess())
                {
                    NFCTagButton.IsEnabled = enable;
                }
                else
                {
                    NFCTagButton.Dispatcher.BeginInvoke(new UIBoolDispatcher(enableNFCTagButton),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { enable });
                }
            }
            catch (Exception)
            {
            }
        }

        private void enableClassicNFCTagButton(bool enable)
        {
            try
            {
                if (ClassicNFCTagButton.Dispatcher.CheckAccess())
                {
                    ClassicNFCTagButton.IsEnabled = enable;
                }
                else
                {
                    ClassicNFCTagButton.Dispatcher.BeginInvoke(new UIBoolDispatcher(enableClassicNFCTagButton),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { enable });
                }
            }
            catch (Exception)
            {
            }
        }

        private void updateDeviceStatus(IDevice device, bool connected)
        {
            var arg = new { dev = device, state = connected };
            Task task = Task.Factory.StartNew((dynamic obj) =>
            {
                Thread.Sleep(100);
                updateDeviceStatusUI(obj.dev, obj.state);
            }, arg);
        }

        private void updateDeviceStatusUI(IDevice device, bool connected)
        {
            try
            {
                if (StatusRectangle.Dispatcher.CheckAccess())
                {
                    StatusRectangle.Opacity = connected ? 1 : 0.5;

                    ConnectButton.IsEnabled = !connected;
                    DisconnectButton.IsEnabled = connected;
                    //ScanButton.IsEnabled = !connected;

                    if (device != null)
                    {
                        IDeviceCapabilities capabilities = device.getCapabilities();

                        MSRButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.MSR));
                        ContactButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.Contact));
                        ContactlessButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.Contactless));
                        NFCButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.NFC));
                        VASButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.AppleVAS));
                        GVASButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.GoogleVAS));
                        ManualEntryButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.ManualEntry));
                        BCRButton.IsEnabled = capabilities.PaymentMethods.Exists(x => x.Equals(PaymentMethod.Barcode));

                        mNonDisplayDevice = (capabilities.Display == false);
                    }
                }
                else
                {
                    StatusRectangle.Dispatcher.BeginInvoke(new UIDeviceBoolDispatcher(updateDeviceStatusUI),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { device, connected });
                }
            }
            catch (Exception)
            {
            }
        }

        private void delayClearDisplay(int delay)
        {
            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);
                clearDisplay();
            }, this);
        }

        private void clearSignatureCanvas()
        {
            try
            {
                if (SignaturePanel.Dispatcher.CheckAccess())
                {
                    SignatureCanvas.Children.Clear();
                }
                else
                {
                    SignatureCanvas.Dispatcher.BeginInvoke(new UIDispatcher(clearSignatureCanvas),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { });
                }
            }
            catch (Exception)
            {
            }
        }

        private void showSignaturePanel(bool show)
        {
            try
            {
                if (SignaturePanel.Dispatcher.CheckAccess())
                {
                    SignaturePanel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
                }
                else
                {
                    SignaturePanel.Dispatcher.BeginInvoke(new UIBoolDispatcher(showSignaturePanel),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { show });
                }
            }
            catch (Exception)
            {
            }
        }

        private void clearDisplay()
        {
            try
            {
                if (DisplayTextBox.Dispatcher.CheckAccess())
                {
                    DisplayTextBox.Clear();
                }
                else
                {
                    DisplayTextBox.Dispatcher.BeginInvoke(new UIDispatcher(clearDisplay),
                                                            System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception)
            {
            }
        }

        private void sendToDisplay(string data)
        {
            try
            {
                if (DisplayTextBox.Dispatcher.CheckAccess())
                {
                    DisplayTextBox.TextAlignment = TextAlignment.Center;
                    DisplayTextBox.AppendText(data);
                    DisplayTextBox.ScrollToEnd();
                }
                else
                {
                    DisplayTextBox.Dispatcher.BeginInvoke(new UIStringDispatcher(sendToDisplay),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { data });
                }
            }
            catch (Exception)
            {
            }
        }

        private void setDisplay(string data, int nLines = 0)
        {
            clearDisplay();

            if (nLines > 0)
            {
                sendToDisplay(new string('\n', nLines));
            }

            sendToDisplay(data + "\n");
        }

        private void sendToParserOutput(string data)
        {
            try
            {
                if (ParserOutputTextBox.Dispatcher.CheckAccess())
                {
                    ParserOutputTextBox.TextAlignment = TextAlignment.Left;
                    ParserOutputTextBox.AppendText(data + "\n");
                    ParserOutputTextBox.ScrollToEnd();
                }
                else
                {
                    ParserOutputTextBox.Dispatcher.BeginInvoke(new UIStringDispatcher(sendToParserOutput),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { data });
                }
            }
            catch (Exception)
            {
            }
        }

        private void updatePaymentMethodsUI(bool msr, bool contact, bool contactless, bool vas, bool gvas, bool nfc, bool bcr)
        {
            setTransactionStatus(true);

            MSRTypeImage.Visibility = msr ? Visibility.Visible : Visibility.Hidden;
            ContactTypeImage.Visibility = contact ? Visibility.Visible : Visibility.Hidden;
            ContactlessTypeImage.Visibility = contactless ? Visibility.Visible : Visibility.Hidden;
            VASTypeImage.Visibility = vas ? Visibility.Visible : Visibility.Hidden;
            GVASTypeImage.Visibility = gvas ? Visibility.Visible : Visibility.Hidden;
            NFCTypeImage.Visibility = nfc ? Visibility.Visible : Visibility.Hidden;
            BCRTypeImage.Visibility = bcr ? Visibility.Visible : Visibility.Hidden;
        }

        private void startTransaction()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(new UIDispatcher(startTransaction),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { });
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }

            setTransactionStatus(true);

            showSignaturePanel(false);

            bool msr = (bool)MSRButton.IsEnabled && (bool)MSRButton.IsChecked;
            bool contact = (bool)ContactButton.IsEnabled && (bool)ContactButton.IsChecked;
            bool contactless = (bool)ContactlessButton.IsEnabled && (bool)ContactlessButton.IsChecked;
            bool vas = (bool)VASButton.IsEnabled && (bool)VASButton.IsChecked;
            bool gvas = (bool)GVASButton.IsEnabled && (bool)GVASButton.IsChecked;
            bool nfc = (bool)NFCButton.IsEnabled && (bool)NFCButton.IsChecked;
            bool bcr = (bool)BCRButton.IsEnabled && (bool)BCRButton.IsChecked;

            updatePaymentMethodsUI(msr, contact, contactless, vas, gvas, nfc, bcr);

            mTransaction = new Transaction();
            mTransaction.Timeout = 255;
            mTransaction.TransactionType = 0;
            mTransaction.Amount = AmountTextBox.Text.Replace("$", "");
            mTransaction.QuickChip = (bool)QuickChipCheckBox.IsChecked;
            mTransaction.EMVOnly = (bool)EMVOnlyCheckBox.IsChecked;
            mTransaction.DisplayAmountForQuickChip = (bool)ShowAmountCheckBox.IsChecked;

            bool showTipOptions = (bool)ShowTipOptionsCheckBox.IsChecked;
            bool showTax = (bool)ShowTaxCheckBox.IsChecked;

            if (showTipOptions)
            {
                if (mCustomizedSettings != null)
                {
                    //mTransaction.TipMode = (byte) (mCustomizedSettings.TipMode + 1);

                    if (mCustomizedSettings.TipMode == 0)
                        mTransaction.TipMode = 1;
                    else if (mCustomizedSettings.TipMode == 1)
                        mTransaction.TipMode = 2;
                    else if (mCustomizedSettings.TipMode == 2)
                        mTransaction.TipMode = 0x11;
                    else if (mCustomizedSettings.TipMode == 3)
                        mTransaction.TipMode = 0x12;

                    mTransaction.Tip1DisplayMode = mCustomizedSettings.TipButton1Display;
                    mTransaction.Tip2DisplayMode = mCustomizedSettings.TipButton2Display;
                    mTransaction.Tip3DisplayMode = mCustomizedSettings.TipButton3Display;
                    mTransaction.Tip4DisplayMode = mCustomizedSettings.TipButton4Display;
                    mTransaction.Tip5DisplayMode = mCustomizedSettings.TipButton5Display;
                    mTransaction.Tip6DisplayMode = mCustomizedSettings.TipButton6Display;

                    mTransaction.Tip1Value = mCustomizedSettings.Tip1Value;
                    mTransaction.Tip2Value = mCustomizedSettings.Tip2Value;
                    mTransaction.Tip3Value = mCustomizedSettings.Tip3Value;
                    mTransaction.Tip4Value = mCustomizedSettings.Tip4Value;
                    mTransaction.Tip5Value = mCustomizedSettings.Tip5Value;
                    mTransaction.Tip6Value = mCustomizedSettings.Tip6Value;
                }
            }

            if (showTax)
            {
                if (mCustomizedSettings != null)
                {
                    try
                    {
                        if (mCustomizedSettings.TaxSurchargeRate.Length > 0)
                        {

                            double amount = 0;
                            double taxRate = 0;

                            double.TryParse(mTransaction.Amount, out amount);
                            double.TryParse(mCustomizedSettings.TaxSurchargeRate, out taxRate);

                            double taxAmount = amount * taxRate / 100;

                            mTransaction.TaxAmount = taxAmount.ToString("F");
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                //mTransaction.TaxAmount = "0.10";
            }

            mTransaction.CurrencyCode = new byte[] { 0x08, 0x40 }; // Tag 5F2A

            List<PaymentMethod> paymentMethods = TransactionBuilder.GetPaymentMethods(msr, contact, contactless, false);

            if (nfc)
            {
                paymentMethods.Add(PaymentMethod.NFC);
                mNFCState = NFCState.ENABLED;
            }

            if (vas)
            {
                paymentMethods.Add(PaymentMethod.AppleVAS);
            }

            if (gvas)
            {
                paymentMethods.Add(PaymentMethod.GoogleVAS);
            }

            if (bcr)
            {
                paymentMethods.Add(PaymentMethod.Barcode);
                //paymentMethods.Add(PaymentMethod.BarcodeEncrypted);
            }

            mTransaction.PaymentMethods = paymentMethods;

            mTransaction.CurrencyCode = new byte[] { 0x08, 0x40 }; // Tag 5F2A

            if (contactless && (vas || gvas))
            {
                //mTransaction.AppleVASMode = VASMode.Dual;
                mTransaction.AppleVASMode = VASMode.Single;

                mTransaction.AppleVASProtocol = VASProtocol.Full;
            }
            else if (vas || gvas)
            {
                mTransaction.AppleVASMode = VASMode.VASOnly;

                mTransaction.AppleVASProtocol = VASProtocol.Full;
            }

            //mTransaction.SuppressThankYouMessage = true;
            //mTransaction.OverrideFinalTransactionMessage = 0x15; // "PRESENT CARD"

            mTransaction.FunctionalButtonRightOption = mCustomizedSettings.PresentCardFunctionalButtonRightOption;

            IDevice device = getDevice();

            if (device != null)
            {
                device.unsubscribeAll(this);
                device.subscribeAll(this);

                mFallbackManager = null;

                if (Fallback == true)
                {
                    sendToOutput("[Fallback Enabled]");
                    mFallbackManager = new FallbackManager(this, mTransaction);

                    mTransaction.PreventMSRSignatureForCardWithICC = true;  // Prevent signature capture on device during MSR transaction if card has ICC 
                }

                if (device.startTransaction(mTransaction))
                {
                    sendToOutput("[Transaction Started]");
                    sendToOutput("Amount=" + mTransaction.Amount + ", Timeout=" + mTransaction.Timeout + ", Transaction Type=" + mTransaction.TransactionType);
                    sendToOutput("MSR=" + msr + ", Contact=" + contact + ", Contactless=" + contactless + ", NFC=" + nfc);
                    sendToOutput("AppleVAS=" + vas + ", GoogleVAS=" + gvas + ", NFC=" + nfc + ", BCR=" + bcr);
                }
                else
                {
                    setTransactionStatus(false);
                    clearDisplay();
                    sendToDisplay("\n\nPAYMENT METHOD NOT SUPPORTED");
                }
            }
        }

        private void startManualEntry(string amount)
        {
            setTransactionStatus(true);

            updatePaymentMethodsUI(false, false, false, false, false, false, false);

            mTransaction = new Transaction();
            mTransaction.PaymentMethods = TransactionBuilder.GetPaymentMethods(false, false, false, true);
            mTransaction.Amount = AmountTextBox.Text.Replace("$", "");
            mTransaction.ManualEntryType = 0x00;
            mTransaction.ManualEntryFormat = 0x00;
            mTransaction.ManualEntrySound = 0x01;

            IDevice device = getDevice();

            if (device != null)
            {
                device.unsubscribeAll(this);
                device.subscribeAll(this);

                if (device.startTransaction(mTransaction) == false)
                {
                    setTransactionStatus(false);
                    clearDisplay();
                    sendToDisplay("\n\nMANUAL ENTRY NOT SUPPORTED");
                }
            }
        }

        private void cancellTransaction()
        {
            setTransactionStatus(false);

            IDevice device = getDevice();

            if (device != null)
            {
                device.cancelTransaction();
            }
        }

        private void requestPIN()
        {
            IDevice device = getDevice();

            if (device != null)
            {
                PINRequest pinRequest = new PINRequest();

                pinRequest.PAN = "123456789012";

                mPANRequest = null;

                if (device.requestPIN(pinRequest) == false)
                {
                    setTransactionStatus(false);
                    clearDisplay();
                    sendToDisplay("\n\nREQUEST PIN NOT SUPPORTED");
                }

            }
        }

        private void requestPAN()
        {
            IDevice device = getDevice();

            if (device != null)
            {
                bool msr = (bool)MSRButton.IsEnabled && (bool)MSRButton.IsChecked;
                bool contact = (bool)ContactButton.IsEnabled && (bool)ContactButton.IsChecked;
                bool contactless = (bool)ContactlessButton.IsEnabled && (bool)ContactlessButton.IsChecked;

                mPANRequest = new PANRequest(60, TransactionBuilder.GetPaymentMethods(msr, contact, contactless, false));

                PINRequest pinRequest = new PINRequest();
                pinRequest.PINMode = 1;
                pinRequest.Format = 0;

                if (device.requestPAN(mPANRequest, pinRequest) == false)
                {
                    setTransactionStatus(false);
                    clearDisplay();
                    sendToDisplay("\n\nREQUEST PAN NOT SUPPORTED");
                }

            }
        }

        private void requestSignature()
        {
            IDevice device = getDevice();

            if (device != null)
            {
                IDeviceCapabilities capabilities = device.getCapabilities();

                if (capabilities.Signature)
                {
                    sendToOutput("Request Signature from Device");

                    if (mDevice.requestSignature(30) == false)
                    {
                        sendToOutput("Request Signature Failed");
                    }
                }
                else
                {
                    sendToOutput("Signature Capture Not Supported");
                }
            }
        }

        private void sendSelection(byte status, byte selection)
        {
            if (mDevice != null)
            {
                mDevice.sendSelection(new BaseData(new byte[] { status, selection }));
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            startTransaction();
        }

        private void ManualEntryButton_Click(object sender, RoutedEventArgs e)
        {
            string amount = AmountTextBox.Text;
            startManualEntry(amount);
        }

        private void PINButton_Click(object sender, RoutedEventArgs e)
        {
            requestPIN();
        }

        private void SignatureButton_Click(object sender, RoutedEventArgs e)
        {
            requestSignature();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            enableTransactionUI(true);
            cancellTransaction();
        }

        private void SignatureCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GetSignatureFromDevice == false)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    mSignaturePoint = e.GetPosition(SignatureCanvas);
            }
        }

        private void SignatureCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (GetSignatureFromDevice == false)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Line line = new Line();

                    line.Stroke = new SolidColorBrush(Colors.Black);
                    line.StrokeThickness = 1;
                    line.X1 = mSignaturePoint.X;
                    line.Y1 = mSignaturePoint.Y;
                    line.X2 = e.GetPosition(SignatureCanvas).X;
                    line.Y2 = e.GetPosition(SignatureCanvas).Y;

                    mSignaturePoint = e.GetPosition(SignatureCanvas);

                    SignatureCanvas.Children.Add(line);
                }
            }
        }

        private void DeviceAddressCB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            //IDevice device = getDevice();
            IDevice device = getSelectedDevice();

            if (device != null)
            {
                AddressTextBox.Text = "";

                if (device.getConnectionInfo().getConnectionType() == ConnectionType.WEBSOCKET)
                {
                    AddressTextBox.Text = device.getConnectionInfo().getAddress();
                }

                updateDeviceStatus(device, device.getConnectionState() == ConnectionState.Connected);

                mDevice = null;
            }

        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.Height > MAIN_PANEL_HEIGHT)
                Application.Current.MainWindow.Height = MAIN_PANEL_HEIGHT;
            else
                Application.Current.MainWindow.Height = WINDOW_HEIGHT;
        }

        private void UIStringFileButton_Click(object sender, RoutedEventArgs e)
        {
            selectUIStringFile();
        }

        private void CustomizeButton_Click(object sender, RoutedEventArgs e)
        {
            displayCustomizeWindow();
        }

        private void DeviceUIPageButton_Click(object sender, RoutedEventArgs e)
        {
            displayDeviceUIPage();
        }

        protected void selectUIStringFile()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    StreamReader streamReder = new StreamReader(ofd.FileName);
                    List<string> stringList = new List<String>();
                    int count = 0;
                    while (!streamReder.EndOfStream)
                    {
                        string line = streamReder.ReadLine();
                        if (count != 0)
                        {
                            if (!String.IsNullOrWhiteSpace(line))
                            {
                                stringList.Add(line);
                            }
                        }
                        count++;
                    }

                    mUIStringList = stringList.ToArray();
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }
        protected void displaySettingsWindow()
        {
            try
            {
                SettingsWindow dialog = new SettingsWindow(mMQTTSettings);

                dialog.Owner = this;

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    mMQTTSettings = dialog.getMQTTSEttings();
                }

                dialog.Owner = null;
                dialog = null;
            }
            catch (Exception)
            {
            }
        }

        protected void displayCustomizeWindow()
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    if (mDevice != null)
                    {
                        if (mDevice.getConnectionState() == ConnectionState.Connected)
                        {
                            byte[] responseBytes = mDevice.getDeviceConfiguration().getConfigInfo(1, TLVParser.getByteArrayFromHexString("E108E106E104E202C400"));

                            if ((responseBytes != null) && (responseBytes.Length > 0))
                            {
                                mCustomizedSettings.UseSurcharge = (responseBytes[0] == 1) ? true : false;
                            }
                        }
                    }

                    mCustomizedSettings.UIStringList = mUIStringList;

                    CustomizeWindow dialog = new CustomizeWindow(mCustomizedSettings);

                    dialog.Owner = this;

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        mCustomizedSettings = dialog.mSettings;
                    }

                    dialog.Owner = null;
                    dialog = null;
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        protected void displayDeviceUIPage()
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    if (mDeviceUIPageSettings == null)
                    {
                        mDeviceUIPageSettings = new DeviceUIPageSettings();
                    }

                    DeviceUIPageSettings settings = mDeviceUIPageSettings;

                    settings.UIStringList = mUIStringList;

                    DeviceUIPage dialog = new DeviceUIPage(settings);

                    dialog.Owner = this;

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        settings = dialog.mSettings;

                        if (settings != null)
                        {
                            IDeviceControl deviceControl = getDeviceControl();

                            if (deviceControl != null)
                            {
                                if (settings.PageOption == 0) // Text Lines
                                {
                                    deviceControl.showUIPageWithTextLines(settings.Timeout, settings.LineText1, settings.LineText2, settings.LineText3, settings.LineText4, settings.LineText5, settings.FButtonMiddleTextStringID);
                                }
                                else if (settings.PageOption == 1) // Text String Buttons
                                {
                                    deviceControl.showUIPageWithTextButtons(settings.Timeout, settings.TitleTextStringID, settings.ButtonTextStringID1, settings.ButtonTextStringID2, settings.ButtonTextStringID3,
                                                                            settings.ButtonTextStringID4, settings.ButtonTextStringID5, settings.ButtonTextStringID6,
                                                                            settings.FButtonLeftTextStringID, settings.FButtonMiddleTextStringID, settings.FButtonRightTextStringID,
                                                                            settings.FButtonLeftColor, settings.FButtonMiddleColor, settings.FButtonRightColor);
                                }
                                else if (settings.PageOption == 2) // Amount Buttons
                                {
                                    deviceControl.showUIPageWithAmountButtons(settings.Timeout, settings.TitleTextStringID, settings.ButtonAmountString1, settings.ButtonAmountString2, settings.ButtonAmountString3,
                                                                            settings.ButtonAmountString4, settings.ButtonAmountString5, settings.ButtonAmountString6,
                                                                            settings.FButtonLeftTextStringID, settings.FButtonMiddleTextStringID, settings.FButtonRightTextStringID,
                                                                            settings.FButtonLeftColor, settings.FButtonMiddleColor, settings.FButtonRightColor);
                                }
                                else if (settings.PageOption == 3) // Custom Image
                                {
                                    deviceControl.showUIPageWithImage(settings.Timeout, settings.TitleTextStringID, settings.FButtonRightTextStringID, settings.ImageXPosition, settings.ImageYPosition, settings.ImageData);
                                }
                            }
                        }
                    }

                    dialog.Owner = null;
                    dialog = null;
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void setTransactionStatus(bool started)
        {
            if (started)
            {
                enableTransactionUI(false);

                //clearDisplay();

                //sendToDisplay("\n\nPLEASE WAIT...");
            }
            else
            {
                enableTransactionUI(true);
                //clearDisplay();
            }
        }

        private void sendToOutput(string data)
        {
            try
            {
                if (OutputTextBox.Dispatcher.CheckAccess())
                {
                    OutputTextBox.AppendText(data + "\n");
                    OutputTextBox.ScrollToEnd();
                }
                else
                {
                    OutputTextBox.Dispatcher.BeginInvoke(new UIStringDispatcher(sendToOutput),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { data });
                }
            }
            catch (Exception)
            {
            }
        }

        private void sendToMSR(string data)
        {
            try
            {
                if (MSRTextBox.Dispatcher.CheckAccess())
                {
                    if (data != null)
                    {
                        MSRTextBox.AppendText(data);
                    }

                    MSRTextBox.ScrollToEnd();
                }
                else
                {
                    MSRTextBox.Dispatcher.BeginInvoke(new UIStringDispatcher(sendToMSR),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { data });
                }
            }
            catch (Exception)
            {
            }
        }

        private byte[] getTLVPayload(byte[] data)
        {
            byte[] payload = null;

            if (data != null)
            {
                int dataLen = data.Length;

                if (dataLen > 2)
                {
                    int tlvLen = (int)((data[0] & 0x000000FF) << 8) + (int)(data[1] & 0x000000FF);

                    payload = new byte[tlvLen];
                    Array.Copy(data, 2, payload, 0, tlvLen);
                }
            }

            return payload;
        }

        private void sendToAuthorization(byte[] dataBytes)
        {
            try
            {
                if (AuthorizationTextBox.Dispatcher.CheckAccess())
                {
                    if (dataBytes != null)
                    {
                        List<Dictionary<String, String>> parsedTLVList = MTParser.parseTLV(getTLVPayload(dataBytes));
                        AuthorizationTextBox.AppendText(getParsedTLVOutput(parsedTLVList));
                    }

                    AuthorizationTextBox.ScrollToEnd();
                }
                else
                {
                    AuthorizationTextBox.Dispatcher.BeginInvoke(new UIByteArrayDispatcher(sendToAuthorization),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { dataBytes });
                }
            }
            catch (Exception)
            {
            }
        }

        private void sendToResult(byte[] dataBytes)
        {
            try
            {
                if (ResultTextBox.Dispatcher.CheckAccess())
                {
                    if (dataBytes != null)
                    {
                        List<Dictionary<String, String>> parsedTLVList = MTParser.parseTLV(getTLVPayload(dataBytes));
                        ResultTextBox.AppendText(getParsedTLVOutput(parsedTLVList));
                    }

                    ResultTextBox.ScrollToEnd();
                }
                else
                {
                    ResultTextBox.Dispatcher.BeginInvoke(new UIByteArrayDispatcher(sendToResult),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { dataBytes });
                }
            }
            catch (Exception)
            {
            }
        }

        private void clearOutput()
        {
            try
            {
                if (OutputTextBox.Dispatcher.CheckAccess())
                {
                    OutputTextBox.Clear();
                    MSRTextBox.Clear();
                    AuthorizationTextBox.Clear();
                    ResultTextBox.Clear();
                    ParserInputTextBox.Clear();
                    ParserOutputTextBox.Clear();
                    showSignaturePanel(false);
                }
                else
                {
                    OutputTextBox.Dispatcher.BeginInvoke(new UIDispatcher(clearOutput),
                                                            System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception)
            {
            }
        }

        private List<IDevice> addDevices(List<IDevice> deviceList)
        {
            byte[] certificateData = null;

            try
            {
                //certificateData = System.IO.File.ReadAllBytes("client.p12");
            }
            catch (Exception ex)
            {

            }

            if (certificateData != null)
            {
                CertificateInfo certificateInfo = new CertificateInfo("PKCS12", certificateData, "password");

                deviceList.Add(CoreAPI.createDevice(DeviceType.MMS, ConnectionType.WEBSOCKET, "", "DynaFlex", "", "", certificateInfo));
                deviceList.Add(CoreAPI.createDevice(DeviceType.MMS, ConnectionType.WEBSOCKET_TRUST, "", "DynaFlex", "", "", certificateInfo));
                //deviceList.Add(CoreAPI.createDevice(DeviceType.MMS, ConnectionType.WEBSOCKET, "", "DynaFlex", "", ""));
            }
            else
            {
                deviceList.Add(CoreAPI.createDevice(DeviceType.MMS, ConnectionType.WEBSOCKET, "", "DynaFlex", "", ""));
            }

            return deviceList;
        }

        private void scanDevices()
        {
            if (mDevice != null)
            {
                if (mDevice.getConnectionState() == ConnectionState.Connected)
                {
                    sendToOutput("Refreshing Device List is not allowed while connected to a device.");
                    
                    return;
                }
            }
            
            stopMQTTDeviceStatusMonitoring();

            Task task = Task.Factory.StartNew(() =>
            {
                mDevice = null;

                // Set up MQTT Parameters

                CoreAPI.setMQTTBrokerInfo(mMQTTSettings.URI, mMQTTSettings.Username, mMQTTSettings.Password);
                CoreAPI.setMQTTSubscribeTopic(mMQTTSettings.SubscribeTopic);
                CoreAPI.setMQTTPublishTopic(mMQTTSettings.PublishTopic);
                CoreAPI.setMQTTDeviceDiscoveryTimeout(5000);

                CoreAPI.setSystemStatusCallback(this);

                try
                {
                    byte[] certificateData = null;

                    string filepath = mMQTTSettings.ClientCertificateFilePath;

                    if (!string.IsNullOrEmpty(filepath))
                    {
                        certificateData = System.IO.File.ReadAllBytes(filepath);
                    }


                    CoreAPI.setMQTTClientCertificateInfo(new CertificateInfo("PKCS12", certificateData, mMQTTSettings.ClientCertificatePassword));
                }
                catch (Exception ex)
                {

                }

                //mDeviceList = CoreAPI.getDeviceList(DeviceType.SCRA);
                mDeviceList = CoreAPI.getDeviceList(DeviceType.MMS);
                //mDeviceList = CoreAPI.getDeviceList();

                mDeviceList = addDevices(mDeviceList);

                updateDeviceList();
            });
        }

        private void updateDeviceList()
        {
            try
            {
                if (DeviceAddressCB.Dispatcher.CheckAccess())
                {
                    if (mDeviceList != null)
                    {
                        DeviceAddressCB.Items.Clear();

                        if (mDeviceList.Count > 0)
                        {
                            foreach (IDevice device in mDeviceList)
                            {
                                DeviceAddressCB.Items.Add(device);
                            }

                            DeviceAddressCB.SelectedIndex = 0;
                        }
                    }

                    setDeviceListOutdated(false);

                    startMQTTDeviceStatusMonitoring();
                }
                else
                {
                    DeviceAddressCB.Dispatcher.BeginInvoke(new UIDispatcher(updateDeviceList),
                                                            System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception)
            {
            }
        }

        private IDevice getDevice()
        {
            if (mDevice != null)
            {
                return mDevice;
            }

            return getSelectedDevice();
        }

        private IDevice getSelectedDevice()
        {
            int index = DeviceAddressCB.SelectedIndex;

            if (mDeviceList != null)
            {
                if ((index >= 0) && (index < mDeviceList.Count))
                {
                    mDevice = mDeviceList[index];

                    ConnectionInfo connectionInfo = mDevice.getConnectionInfo();

                    ConnectionType connectionType = connectionInfo.getConnectionType();

                    if ((connectionType == ConnectionType.WEBSOCKET) || (connectionType == ConnectionType.WEBSOCKET_TRUST))
                    {
                        string newAddress = AddressTextBox.Text;

                        string address = connectionInfo.getAddress();

                        if (address.CompareTo(newAddress) != 0)
                        {
                            byte[] certificateData = null;

                            try
                            {
                                certificateData = System.IO.File.ReadAllBytes("client.p12");
                            }
                            catch (Exception ex)
                            {

                            }

                            if (certificateData != null)
                            {
                                CertificateInfo certificateInfo = new CertificateInfo("PKCS12", certificateData, "password");

                                mDevice = CoreAPI.createDevice(DeviceType.MMS, connectionType, newAddress, "DynaFlex", "", "", certificateInfo);
                            }
                            else
                            {
                                mDevice = CoreAPI.createDevice(DeviceType.MMS, connectionType, newAddress, "DynaFlex", "", "");
                            }

                            mDeviceList[index] = mDevice;
                        }
                    }
                }
            }

            return mDevice;
        }

        private void showDeviceInfo(IDevice device)
        {
            if (device == null)
                return;

            ConnectionInfo connectionInfo = device.getConnectionInfo();
            DeviceInfo deviceInfo = device.getDeviceInfo();

            if (connectionInfo != null)
            {
                sendToOutput("Device Type: " + connectionInfo.getDeviceType());
                sendToOutput("Connection Type: " + connectionInfo.getConnectionType());
                sendToOutput("Device Address: " + connectionInfo.getAddress());
            }

            if (deviceInfo != null)
            {
                sendToOutput("Name: " + deviceInfo.getName());
                sendToOutput("Model: " + deviceInfo.getModel());

                if (device.getConnectionState() == ConnectionState.Connected)
                {
                    IDeviceConfiguration deviceConfiguration = device.getDeviceConfiguration();

                    if (deviceConfiguration != null)
                    {
                        Task task = Task.Factory.StartNew(async () =>
                        {
                            sendToOutput("Serial: " + deviceConfiguration.getDeviceInfo(InfoType.DeviceSerialNumber));
                            sendToOutput("Firmware: " + deviceConfiguration.getDeviceInfo(InfoType.FirmwareVersion));
                            sendToOutput("Capabilities: " + deviceConfiguration.getDeviceInfo(InfoType.DeviceCapabilities));
                            sendToOutput("BOOT-1: " + deviceConfiguration.getDeviceInfo(InfoType.Boot1Version));
                            sendToOutput("BOOT-0: " + deviceConfiguration.getDeviceInfo(InfoType.Boot0Version));
                            sendToOutput("Firmware Hash: " + deviceConfiguration.getDeviceInfo(InfoType.FirmwareHash));
                            sendToOutput("Tamper Status: " + deviceConfiguration.getDeviceInfo(InfoType.TamperStatus));
                            sendToOutput("Operation Status: " + deviceConfiguration.getDeviceInfo(InfoType.OperationStatus));
                            sendToOutput("Offline Detail: " + deviceConfiguration.getDeviceInfo(InfoType.OfflineDetail));
                        });
                    }
                }
            }

            sendToOutput("");
        }

        private void connectDevice(IDevice device)
        {
            if (device == null)
                return;

            if (device.getConnectionState() == ConnectionState.Connected)
            {
                sendToOutput("Device is already connected.");
                return;
            }

            //sendToOutput("Connecting to " + device.Name + "...");
            sendToOutput("Connecting...");

            IDeviceControl deviceControl = device.getDeviceControl();

            if (deviceControl != null)
            {
                device.unsubscribeAll(this);
                device.subscribeAll(this);

                deviceControl.open();
            }
        }

        private void disconnectDevice(IDevice device)
        {
            if (device == null)
                return;

            sendToOutput("Disconnecting...");

            IDeviceControl deviceControl = device.getDeviceControl();

            if (deviceControl != null)
            {
                deviceControl.close();
            }
        }

        private void sendToDevice(IDevice device, string command)
        {
            if (device == null)
                return;

            IDeviceControl deviceControl = device.getDeviceControl();

            if (deviceControl != null)
            {
                sendToOutput("Sending: " + command);
                deviceControl.send(new BaseData(command));
            }
        }

        private void sendToDeviceSync(IDevice device, string command)
        {
            if (device == null)
                return;

            sendToOutput("Sending (Sync): " + command);

            IDeviceControl deviceControl = device.getDeviceControl();

            if (deviceControl != null)
            {
                IResult result = deviceControl.sendSync(new BaseData(command));

                if (result.Status == StatusCode.SUCCESS)
                {
                    sendToOutput("Response (Sync): " + result.Data.StringValue);
                }
                else if (result.Status == StatusCode.TIMEOUT)
                {
                    sendToOutput("Response (Sync): TIMED OUT");
                }
            }
        }


        private void sendExtToDevice(IDevice device, string command)
        {
            if (device == null)
                return;

            IDeviceControl deviceControl = device.getDeviceControl();

            if (deviceControl != null)
            {
                sendToOutput("Sending Extended Cmd: " + command);
                deviceControl.sendExtendedCommand(new BaseData(command));
            }
        }

        private void showSignature(byte[] data)
        {

            try
            {
                if (SignatureCanvas.Dispatcher.CheckAccess())
                {
                    if (data.Length >= 4)
                    {
                        int x = (data[0] * 256) + data[1];
                        int y = (data[2] * 256) + data[3];

                        Point startPoint = new Point(x, y);

                        int i = 4;

                        SignatureCanvas.Children.Clear();

                        while ((i + 3) < data.Length)
                        {

                            x = (data[i] * 256) + data[i + 1];
                            y = (data[i + 2] * 256) + data[i + 3];

                            Point endPoint = new Point((int)x, (int)y);

                            if ((endPoint.Y != 0xFFFF) && (startPoint.Y != 0xFFFF))
                            {
                                Line line = new Line();
                                line.Stroke = new SolidColorBrush(Colors.DarkGray);
                                line.StrokeThickness = 2;

                                line.X1 = startPoint.X + 100;
                                line.Y1 = startPoint.Y / 2;
                                line.X2 = endPoint.X + 100;
                                line.Y2 = endPoint.Y / 2;

                                String info = "** (" + line.X1 + "," + line.Y1 + ") --> (" + line.X2 + "," + line.Y2 + ")";
                                Debug.WriteLine(info);

                                SignatureCanvas.Children.Add(line);
                            }

                            startPoint = endPoint;
                            i += 4;
                        }

                        showSignaturePanel(true);
                    }
                }
                else
                {
                    SignatureCanvas.Dispatcher.BeginInvoke(new UIByteArrayDispatcher(showSignature),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { data });
                }
            }
            catch (Exception)
            {
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            scanDevices();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            displaySettingsWindow();
        }

        private void DeviceInfoButton_Click(object sender, RoutedEventArgs e)
        {
            showDeviceInfo(getSelectedDevice());
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            connectDevice(getSelectedDevice());
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            disconnectDevice(getDevice());
        }

        private void SendCmdButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandTextBox.Text;
            //sendToDevice(getDevice(), command);
            sendToDeviceSync(getDevice(), command);
        }

        private void SendExtCmdButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandTextBox.Text;
            sendExtToDevice(getDevice(), command);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            clearOutput();
            clearDisplay();
        }

        private void NFCTagButton_Click(object sender, RoutedEventArgs e)
        {
            displayNFCTagWindow();
        }
        private void ClassicNFCTagButton_Click(object sender, RoutedEventArgs e)
        {
            displayClassicNFCTagWindow();
        }

        private void ParseTLVButton_Click(object sender, RoutedEventArgs e)
        {
            parseTLV();
        }
        private void ParseNDEFButton_Click(object sender, RoutedEventArgs e)
        {
            parseNDEF();
        }

        private void ShowImageButton_Click(object sender, RoutedEventArgs e)
        {
            showImage();
        }

        private void SendImageButton_Click(object sender, RoutedEventArgs e)
        {
            sendImage();
        }

        private void SetDisplayImageButton_Click(object sender, RoutedEventArgs e)
        {
            setDisplayImage();
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileID = SendFileIDTextBox.Text;

            sendFile(fileID, false);
        }

        private void GetFileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileID = GetFileIDTextBox.Text;

            getFile(fileID);
        }

        private void DisplayMessageButton_Click(object sender, RoutedEventArgs e)
        {
            string messageID = MessageIDTextBox.Text;

            displayMessage(messageID, 30);
        }

        private void GetChallengeButton_Click(object sender, RoutedEventArgs e)
        {
            string challengeID = ChallengeIDTextBox.Text;

            getChallenge(challengeID);
        }

        private void RequestPANButton_Click(object sender, RoutedEventArgs e)
        {
            requestPAN();
        }

        private void StartBCReaderButton_Click(object sender, RoutedEventArgs e)
        {
            startBarCodeReader(0);
        }

        private void StopBCReaderButton_Click(object sender, RoutedEventArgs e)
        {
            stopBarCodeReader();
        }

        private void ShowBitmapButton_Click(object sender, RoutedEventArgs e)
        {
            showBitmap();
        }

        private void ShowBarCodeButton_Click(object sender, RoutedEventArgs e)
        {
            showBarCode();
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            displayConfigWindow();
        }

        private void UpdateFWButton_Click(object sender, RoutedEventArgs e)
        {
            updateMainFirmware();
        }

        private void UpdateWLANFWButton_Click(object sender, RoutedEventArgs e)
        {
            updateWLANFirmware();
        }

        private void DeviceResetButton_Click(object sender, RoutedEventArgs e)
        {
            deviceReset();
        }

        protected IDeviceControl getDeviceControl()
        {
            IDeviceControl deviceControl = null;

            IDevice device = getDevice();

            if (device != null)
            {
                deviceControl = device.getDeviceControl();
            }

            return deviceControl;
        }

        protected IDeviceConfiguration getDeviceConfiguration()
        {
            IDeviceConfiguration deviceConfiguration = null;

            IDevice device = getDevice();

            if (device != null)
            {
                deviceConfiguration = device.getDeviceConfiguration();
            }

            return deviceConfiguration;
        }

        protected void showImage()
        {
            try
            {
                IDeviceControl deviceControl = getDeviceControl();

                if (deviceControl == null)
                {
                    return;
                }

                string valueString = ShowImageIDCB.Text;

                byte imageID = (byte)Convert.ToInt32(valueString);

                sendToOutput("Showing Image " + imageID);
                deviceControl.showImage(imageID);
            }
            catch (Exception ex)
            {
                sendToOutput("Show Image Failed: " + ex.Message);
            }
        }

        protected void sendImage()
        {
            try
            {
                IDeviceConfiguration deviceConfiguration = getDeviceConfiguration();

                if (deviceConfiguration == null)
                {
                    return;
                }

                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    var data = System.IO.File.ReadAllBytes(ofd.FileName);

                    string valueString = SendImageIDCB.Text;

                    byte imageID = (byte)Convert.ToInt32(valueString);

                    if (data != null)
                    {
                        mFileTransferMode = FileTransferMode.SEND_IMAGE;
                        sendToOutput("Sending Data for Image ... " + imageID);
                        deviceConfiguration.sendImage(imageID, data, this);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                sendToOutput("Send Image Failed: " + ex.Message);
            }
        }

        protected void setDisplayImage()
        {
            try
            {
                IDeviceConfiguration deviceConfiguration = getDeviceConfiguration();

                if (deviceConfiguration == null)
                {
                    return;
                }

                string valueString = SetDisplayImageIDCB.Text;

                byte imageID = (byte)Convert.ToInt32(valueString);

                sendToOutput("Set Display Image ID to " + imageID);
                deviceConfiguration.setDisplayImage(imageID);
            }
            catch (Exception ex)
            {
                sendToOutput("Set Display Image Failed: " + ex.Message);
            }
        }

        protected void sendFile(string fileID, bool secure)
        {
            try
            {
                IDeviceConfiguration deviceConfiguration = getDeviceConfiguration();

                if (deviceConfiguration == null)
                {
                    return;
                }

                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    var data = System.IO.File.ReadAllBytes(ofd.FileName);

                    if (data != null)
                    {
                        string fileName = ofd.SafeFileName;

                        sendToOutput("File ID: " + fileID);

                        byte[] fileIDBytes = MTParser.getByteArrayFromHexString(fileID);

                        mFileTransferMode = FileTransferMode.SEND_FILE;
                        deviceConfiguration.sendFile(fileIDBytes, data, this);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                sendToOutput("Send File failed: " + ex.Message);
            }
        }

        protected void getFile(string fileID)
        {
            try
            {
                IDeviceConfiguration deviceConfiguration = getDeviceConfiguration();

                if (deviceConfiguration == null)
                {
                    return;
                }

                Microsoft.Win32.SaveFileDialog ofd = new Microsoft.Win32.SaveFileDialog();
                ofd.ValidateNames = false;
                ofd.CheckFileExists = false;
                ofd.CheckPathExists = true;
                ofd.FileName = "FILE_" + fileID;

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    mGetFileName = ofd.FileName;

                    sendToOutput("File ID: " + fileID);
                    sendToOutput("Get File: " + mGetFileName);

                    byte[] fileIDBytes = MTParser.getByteArrayFromHexString(fileID);

                    mFileTransferMode = FileTransferMode.GET_FILE;
                    deviceConfiguration.getFile(fileIDBytes, this);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                sendToOutput("Get File failed: " + ex.Message);
            }
        }

        protected void displayMessage(string data, byte timeout)
        {
            IDeviceControl deviceControl = getDeviceControl();

            if (deviceControl != null)
            {
                byte[] messageID = MTParser.getByteArrayFromHexString(data);

                sendToOutput("Request Display Message ID=" + messageID[0]);

                deviceControl.displayMessage(messageID[0], timeout);
            }
        }

        protected void getChallenge(string data)
        {
            IDeviceConfiguration deviceConfiguration = getDeviceConfiguration();

            if (deviceConfiguration != null)
            {
                byte[] token = deviceConfiguration.getChallengeToken(MTParser.getByteArrayFromHexString(data));

                if (token != null)
                {
                    sendToOutput("Challenge Token=" + MTParser.getHexString(token));
                }
            }
        }

        protected void showBitmap()
        {
            try
            {
                IDeviceControl deviceControl = getDeviceControl();

                if (deviceControl != null)
                {
                    Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                    bool bShow = ofd.ShowDialog() ?? false;

                    if (bShow)
                    {
                        var data = System.IO.File.ReadAllBytes(ofd.FileName);

                        if (data != null)
                        {

                            sendToOutput("Request show bitmap");

                            ImageData imageData = new ImageData(ImageType.BITMAP, data);

                            deviceControl.showImage(imageData, 30);
                        }
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                sendToOutput("Send File failed: " + ex.Message);
            }
        }

        protected void startBarCodeReader(byte encryptionMode)
        {
            IDeviceControl deviceControl = getDeviceControl();

            if (deviceControl != null)
            {
                sendToOutput("Start bar code reader");

                deviceControl.startBarCodeReader(0, encryptionMode);
            }
        }

        protected void stopBarCodeReader()
        {
            IDeviceControl deviceControl = getDeviceControl();

            if (deviceControl != null)
            {
                sendToOutput("Stop scan bar reader");

                deviceControl.stopBarCodeReader();
            }
        }

        protected void showBarCode()
        {
            IDeviceControl deviceControl = getDeviceControl();

            if (deviceControl != null)
            {
                string dataString = "http://www.magtek.com/";
                byte[] data = TLVParser.getByteArrayFromASCIIString(dataString);

                sendToOutput("Request show bar code");

                BarCodeRequest request = new BarCodeRequest(BarCodeType.QRCODE, BarCodeFormat.BLOB, data);

                deviceControl.showBarCode(request, 30, new BaseData("Show Me Yours"));
            }
        }

        protected void displayNFCTagWindow()
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    NFCTagWindow dialog = new NFCTagWindow();

                    dialog.Owner = this;

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        string textString = dialog.getTextString();
                        int uriPrefix = dialog.getURIPrefix();
                        string uriString = dialog.getURIString();
                        bool appendMode = dialog.getAppendMode();

                        IDevice device = getDevice();

                        if (device != null)
                        {
                            List<MTNdefRecord> records = new List<MTNdefRecord>();

                            if (appendMode)
                            {
                                if (mNDEFRecords != null)
                                {
                                    foreach (MTNdefRecord rec in mNDEFRecords)
                                    {
                                        records.Add(rec);
                                    }
                                }
                            }

                            sendToOutput("[NFC Write TAG] Text Record: " + textString);

                            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(textString);

                            if (textBytes != null)
                            {
                                byte[] textPayload = new byte[textBytes.Length + 3];
                                textPayload[0] = 2;
                                textPayload[1] = (byte)'e';
                                textPayload[2] = (byte)'n';
                                System.Array.Copy(textBytes, 0, textPayload, 3, textBytes.Length);
                                MTNdefRecord textRecord = MTNdefRecord.createTextRecord(textPayload);
                                records.Add(textRecord);
                            }

                            sendToOutput("[NFC Write TAG] URI Record: " + MTNdefRecord.URI_MAP[uriPrefix] + uriString);

                            byte[] uriBytes = System.Text.Encoding.UTF8.GetBytes(uriString);

                            if (uriBytes != null)
                            {
                                byte[] uriPayload = new byte[uriBytes.Length + 1];
                                //uriPayload[0] = 2; // https://wwww.
                                uriPayload[0] = (byte) uriPrefix;
                                System.Array.Copy(uriBytes, 0, uriPayload, 1, uriBytes.Length);
                                MTNdefRecord uriRecord = MTNdefRecord.createUriRecord(uriPayload);
                                records.Add(uriRecord);
                            }

                            byte[] ndefMessageBytes = MTNdef.BuildNDEFMessage(records);

                            if (ndefMessageBytes != null)
                            {
                                string nfcData = MTParser.getHexString(ndefMessageBytes) + "FE";
                                sendToOutput("nfcData=" + nfcData);
                                writeNDEFMessage(MTParser.getByteArrayFromHexString(nfcData));
                            }
                        }
                    }

                    dialog.Owner = null;
                    dialog = null;
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        protected void displayClassicNFCTagWindow()
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    ClassicNFCTagWindow dialog = new ClassicNFCTagWindow(mClassicNFCData);

                    dialog.Owner = this;

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        mWriteSector = dialog.getWriteSector();
                        string dataString = dialog.getWriteData();

                        IDevice device = getDevice();

                        if (device != null)
                        {
                            sendToOutput("[Classic NFC Write Sector (" + mWriteSector + ") :");
                            sendToOutput(dataString + "]");

                            dataString = dataString.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");

                            byte[] data = MTParser.getByteArrayFromHexString(dataString);

                            if (!writeClassicNFCData(mWriteSector, data))
                            {
                                sendToOutput("[Classic NFC Write Sector (" + mWriteSector + ") : Bad Data]");
                            }
                        }
                    }
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        protected void displayConfigWindow()
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    ConfigWindow dialog = new ConfigWindow();

                    dialog.Owner = this;

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        int configAction = dialog.getConfigAction();
                        string configTypeString = dialog.getConfigType();
                        string configDataString = dialog.getConfigData();

                        byte[] configType = MTParser.getByteArrayFromHexString(configTypeString);
                        byte[] configData = MTParser.getByteArrayFromHexString(configDataString);

                        IDevice device = getDevice();

                        if (device != null)
                        {
                            if ((configType != null) && (configType.Length > 0))
                            {
                                executeConfigurationAction(device, configAction, configType[0], configData);
                            }
                        }
                    }

                    dialog.Owner = null;
                    dialog = null;
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void executeConfigurationAction(IDevice device, int configAction, byte configType, byte[] configData)
        {
            if (device == null)
                return;

            IDeviceConfiguration deviceConfiguration = device.getDeviceConfiguration();

            if (deviceConfiguration != null)
            {
                if (configAction == 0)
                {
                    byte[] response = deviceConfiguration.getConfigInfo(configType, configData);

                    if (response != null)
                    {
                        sendToOutput("Get Configuration Response: " + MTParser.getHexString(response));
                    }
                }
                else if (configAction == 1)
                {
                    int response = deviceConfiguration.setConfigInfo(configType, configData, this);

                    sendToOutput("Set Configuration Response: " + response);
                }
                else if (configAction == 2)
                {
                    byte[] response = deviceConfiguration.getKeyInfo(configType, configData);

                    if (response != null)
                    {
                        sendToOutput("Get Key Info Response: " + MTParser.getHexString(response));
                    }
                }
                else if (configAction == 3)
                {
                    int response = deviceConfiguration.updateKeyInfo(configType, configData, this);

                    sendToOutput("Update Key Info Response: " + response);
                }
            }
        }

        protected void updateMainFirmware()
        {
            updateFirmware(1);
        }

        protected void updateWLANFirmware()
        {
            updateFirmware(2);
        }

        protected void updateFirmware(byte firmwareType)
        {
            try
            {
                IDevice device = getDevice();

                if (device == null)
                {
                    return;
                }

                IDeviceConfiguration deviceConfiguration = device.getDeviceConfiguration();

                if (deviceConfiguration == null)
                {
                    return;
                }

                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    var data = System.IO.File.ReadAllBytes(ofd.FileName);

                    if (data != null)
                    {
                        mFileTransferMode = FileTransferMode.UPDATE_FIRMWARE;
                        deviceConfiguration.updateFirmware(firmwareType, data, this);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                sendToOutput("Update Firmware failed: " + ex.Message);
            }
        }

        protected void deviceReset()
        {
            try
            {
                IDeviceControl deviceControl = getDeviceControl();

                if (deviceControl == null)
                {
                    return;
                }

                deviceControl.deviceReset();
            }
            catch (Exception ex)
            {
                sendToOutput("Device Reset Failed: " + ex.Message);
            }
        }

        private void parseTLV()
        {
            string tlvInput = ParserInputTextBox.Text;

            byte[] inputBytes = MTParser.getByteArrayFromHexString(tlvInput);

            if (inputBytes != null)
            {
                List<Dictionary<String, String>> parsedTLVList = MTParser.parseTLV(inputBytes);

                displayParsedTLV(parsedTLVList);
            }
        }

        private void parseNDEF()
        {
            string ndefMsgString = ParserInputTextBox.Text;

            parseNDEFMessage(ndefMsgString);
        }

        private void displayParsedTLV(List<Dictionary<string, string>> parsedTLVList)
        {
            sendToParserOutput(getParsedTLVOutput(parsedTLVList));
        }

        private string getParsedTLVOutput(List<Dictionary<string, string>> parsedTLVList)
        {
            string output = "";

            if (parsedTLVList != null)
            {
                foreach (Dictionary<String, String> map in parsedTLVList)
                {
                    string tagString;
                    string valueString;

                    if (map.TryGetValue("tag", out tagString))
                    {
                        if (map.TryGetValue("value", out valueString))
                        {
                            output += "  " + tagString + "=" + valueString + "\n";
                        }
                    }
                }
            }

            return output;
        }
        private void processSCDE(string arqc)
        {
            string bdk = "FEDCBA9876543210F1F1F1F1F1F1F1F1";

            MTEncryptedData encryptedData = new MTEncryptedData(bdk);

            SelectableCardData selectableCardData = encryptedData.getSelectableCardData(arqc);

            if (selectableCardData != null)
            {
                sendToOutput("[Decrypted SCDE]");
                string cardHolderName = System.Text.Encoding.UTF8.GetString(MTParser.getByteArrayFromHexString(selectableCardData.CardHolderName));
                sendToOutput("CardHolderName: " + cardHolderName);
                sendToOutput("ExpirationDate: " + selectableCardData.ExpirationDate);
                sendToOutput("ServiceCode: " + selectableCardData.ServiceCode);
                sendToOutput("Track1DiscretionaryData: " + selectableCardData.Track1DiscretionaryData);
                sendToOutput("Track2DiscretionaryData: " + selectableCardData.Track2DiscretionaryData);
            }
        }

        protected void processAuthorizationRequest(byte[] data)
        {
            if (!mTransaction.QuickChip)
            {
                List<Dictionary<String, String>> parsedTLVList = MTParser.parseTLV(getTLVPayload(data));

                if (parsedTLVList != null)
                {
                    byte[] deviceSN = MTParser.getTagByteArrayValue(parsedTLVList, "DFDF25");

                    byte[] macKSN = MTParser.getTagByteArrayValue(parsedTLVList, "DFDF54");

                    byte[] macEncryptionType = MTParser.getTagByteArrayValue(parsedTLVList, "DFDF55");

                    byte[] ApprovedARC = new byte[] { (byte)0x8A, 0x02, 0x30, 0x30 };

                    //                    byte[] response = buildAcquirerResponse(macKSN, macEncryptionType, deviceSN, ApprovedARC, true);
                    byte[] response = buildNoMacAcquirerResponse(deviceSN, ApprovedARC);

                    sendToOutput("[Sending Authorization]\n" + MTParser.getHexString(response));

                    mDevice.sendAuthorization(new BaseData(response));
                }
            }
        }

        protected byte[] buildNoMacAcquirerResponse(byte[] deviceSN, byte[] arc)
        {
            byte[] response = null;

            TLVObject responseObject = new TLVObject("FF74");
            responseObject.addTLVObject(new TLVObject("DFDF25", TLVParser.getHexString(deviceSN)));

            TLVObject faObject = new TLVObject("FA", "");
            TLVObject arpcObject = new TLVObject("70", TLVParser.getHexString(arc));

            faObject.addTLVObject(arpcObject);
            responseObject.addTLVObject(faObject);

            response = responseObject.getTLVByteArray();

            return response;
        }


        protected byte[] buildAcquirerResponse(byte[] macKSN, byte[] macEncryptionType, byte[] deviceSN, byte[] arc, bool useLengthHeader)
        {
            byte[] response = null;

            int lenMACKSN = 0;
            int lenMACEncryptionType = 0;
            int lenSN = 0;

            if (macKSN != null)
            {
                lenMACKSN = macKSN.Length;
            }

            if (macEncryptionType != null)
            {
                lenMACEncryptionType = macEncryptionType.Length;
            }

            if (deviceSN != null)
            {
                lenSN = deviceSN.Length;
            }

            byte[] macKSNTag = new byte[] { (byte)0xDF, (byte)0xDF, 0x54, (byte)lenMACKSN };
            byte[] macEncryptionTypeTag = new byte[] { (byte)0xDF, (byte)0xDF, 0x55, (byte)lenMACEncryptionType };
            byte[] snTag = new byte[] { (byte)0xDF, (byte)0xDF, 0x25, (byte)lenSN };
            byte[] container = new byte[] { (byte)0xFA, 0x06, 0x70, 0x04 };

            int lenTLV = 2 + macKSNTag.Length + lenMACKSN + macEncryptionTypeTag.Length + lenMACEncryptionType + snTag.Length + lenSN + container.Length + arc.Length;

            if (useLengthHeader)
                lenTLV += 2;

            int lenPadding = 0;

            if ((lenTLV % 8) > 0)
            {
                lenPadding = (8 - lenTLV % 8);
            }

            int lenData = lenTLV + lenPadding + 4;

            response = new byte[lenData];

            int i = 0;

            if (useLengthHeader)
            {
                response[i++] = (byte)(((lenData - 2) >> 8) & 0xFF);
                response[i++] = (byte)((lenData - 2) & 0xFF);
            }

            response[i++] = (byte)0xF9;
            response[i++] = (byte)(lenTLV - 2);

            Array.Copy(macKSNTag, 0, response, i, macKSNTag.Length);
            i += macKSNTag.Length;

            if (macKSN != null)
            {
                Array.Copy(macKSN, 0, response, i, macKSN.Length);
                i += macKSN.Length;
            }

            Array.Copy(macEncryptionTypeTag, 0, response, i, macEncryptionTypeTag.Length);
            i += macEncryptionTypeTag.Length;

            if (macEncryptionType != null)
            {
                Array.Copy(macEncryptionType, 0, response, i, macEncryptionType.Length);
                i += macEncryptionType.Length;
            }

            Array.Copy(snTag, 0, response, i, snTag.Length);
            i += snTag.Length;

            if (deviceSN != null)
            {
                Array.Copy(deviceSN, 0, response, i, deviceSN.Length);
                i += deviceSN.Length;
            }

            Array.Copy(container, 0, response, i, container.Length);
            i += container.Length;

            if (arc != null)
            {
                Array.Copy(arc, 0, response, i, arc.Length);
            }

            return response;

        }

        protected void displayUserSelections(string title, int selectionType, List<string> selectionList, long timeout)
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    SelectionsWindow dialog = new SelectionsWindow();

                    long timeoutMS = (timeout * 1000);

                    dialog.Owner = this;

                    if (selectionType == InputRequest.INPUT_TYPE_LANGUAGE)
                    {
                        int nSelections = selectionList.Count;

                        for (int i = 0; i < nSelections; i++)
                        {
                            byte[] code = System.Text.Encoding.UTF8.GetBytes(selectionList[i]);
                            EMVLanguage language = EMVLanguage.GetLanguage(code);

                            if (language != null)
                            {
                                selectionList[i] = language.Name;
                            }
                        }

                    }

                    dialog.init(title, selectionList, timeoutMS);

                    bool? result = dialog.ShowDialog();

                    int selectionIndex = (byte)dialog.getSelectedIndex();

                    dialog.Owner = null;
                    dialog = null;

                    if (result == false)
                    {
                        if (selectionIndex < 0)
                        {
                            sendSelection(InputRequest.INPUT_STATUS_TIMED_OUT, (byte)0);
                        }
                        else
                        {
                            sendSelection(InputRequest.INPUT_STATUS_CANCELLED, (byte)0);
                        }
                    }
                    else
                    {
                        sendSelection(InputRequest.INPUT_STATUS_COMPLETED, (byte)(selectionIndex));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new UISelectionsDisptacher(displayUserSelections),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { title, selectionType, selectionList, timeout });
                }
            }
            catch (Exception)
            {
            }

        }

        protected void displayEnhancedUserSelections(string title, int selectionType, List<DirectoryEntry> enhancedSelectionList, long timeout)
        {
            try
            {
                if (Dispatcher.CheckAccess())
                {
                    EnhancedSelectionsWindow dialog = new EnhancedSelectionsWindow();

                    long timeoutMS = (timeout * 1000);

                    dialog.Owner = this;

                    dialog.init(title, enhancedSelectionList, timeoutMS);

                    bool? result = dialog.ShowDialog();

                    int selectionIndex = (byte)dialog.getSelectedIndex();

                    dialog.Owner = null;
                    dialog = null;

                    if (result == false)
                    {
                        if (selectionIndex < 0)
                        {
                            sendSelection(InputRequest.INPUT_STATUS_TIMED_OUT, (byte)0);
                        }
                        else
                        {
                            sendSelection(InputRequest.INPUT_STATUS_CANCELLED, (byte)0);
                        }
                    }
                    else
                    {
                        sendSelection(InputRequest.INPUT_STATUS_COMPLETED, (byte)(selectionIndex));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new UIEnhancedSelectionsDisptacher(displayEnhancedUserSelections),
                                                            System.Windows.Threading.DispatcherPriority.Normal,
                                                            new object[] { title, selectionType, enhancedSelectionList, timeout });
                }
            }
            catch (Exception)
            {
            }

        }

        protected void processInputRequest(byte[] data)
        {
            InputRequest inputRequest = new InputRequest(data);

            if (inputRequest != null)
            {
                displayUserSelections(inputRequest.Title, inputRequest.Type, inputRequest.SelectionList, inputRequest.Timeout);
            }

        }

        protected void processEnhancedInputRequest(byte[] data)
        {
            EnhancedInputRequest enhancedInputRequest = new EnhancedInputRequest(data);

            if (enhancedInputRequest != null)
            {
                displayEnhancedUserSelections(enhancedInputRequest.Title, enhancedInputRequest.Type, enhancedInputRequest.EnhancedSelectionList, enhancedInputRequest.Timeout);
            }
        }

        protected void processUserEvent(UserEvent userEvent, String dataString)
        {
            if (userEvent == UserEvent.ContactlessCardPresented)
            {
                sendToOutput("[USER EVENT / CONTACTLESS CARD PRESENTED]");

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.ContactlessCardRemoved)
            {
                sendToOutput("[USER EVENT / CONTACTLESS CARD REMOVED]");
            }
            else if (userEvent == UserEvent.CardSeated)
            {
                sendToOutput("[USER EVENT / CARD SEATED]");

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.CardUnseated)
            {
                sendToOutput("[USER EVENT / CARD UNSEATED]");
            }
            else if (userEvent == UserEvent.CardSwiped)
            {
                sendToOutput("[USER EVENT / CARD SWIPED]");

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.TouchPresented)
            {
                sendToOutput("[USER EVENT / TOUCH PRESENTED]");

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.TouchRemoved)
            {
                sendToOutput("[USER EVENT / TOUCH REMOVED]");
            }
            else if (userEvent == UserEvent.BarcodeRead)
            {
                sendToOutput("[USER EVENT / BARCODE READ]");
            }
            else if (userEvent == UserEvent.NFCMifareUltralightPresented)
            {
                sendToOutput("[USER EVENT / NFC_MIFARE_ULTRALIGHT PRESENTED]");
                sendToOutput("UID=" + UserEventBuilder.GetDetail(dataString));

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.MifareClassic1KPresented)
            {
                sendToOutput("[USER EVENT / MIFARE_CLASSIC_1K PRESENTED]");
                sendToOutput("UID=" + UserEventBuilder.GetDetail(dataString));

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.MifareClassic4KPresented)
            {
                sendToOutput("[USER EVENT / MIFARE_CLASSIC_4K PRESENTED]");
                sendToOutput("UID=" + UserEventBuilder.GetDetail(dataString));

                if (EventDrivenTransaction)
                    startTransaction();
            }
            else if (userEvent == UserEvent.NFCMifareUltralightRemoved)
            {
                sendToOutput("[USER EVENT / NFC_MIFARE_ULTRALIGHT REMOVED]");
            }
            else if (userEvent == UserEvent.MifareClassic1KRemoved)
            {
                sendToOutput("[USER EVENT / MIFARE_CLASSIC_1K REMOVED]");
            }
            else if (userEvent == UserEvent.MifareClassic4KRemoved)
            {
                sendToOutput("[USER EVENT / MIFARE_CLASSIC_4K REMOVED]");
            }
        }

        async private void launchWebPage(string uri)
        {
            System.Diagnostics.Process.Start(uri);
        }

        private void processPINData(PINData pinData)
        {
            bool validPIN = true; // PIN Validation Result from Payment Gateway

            IDevice device = getDevice();

            if (device != null)
            {
                if (device.getConnectionInfo().getDeviceType() == DeviceType.MMS)
                {
                    PINRequest pinRequest = new PINRequest();

                    pinRequest.PINMode = validPIN ? (byte)0xFF : (byte)0xFE; // (PIN Entry Successful) : (PIN Entry SuccessfulFailed)
                    pinRequest.Format = 0;

                    if (device.requestPIN(pinRequest) == false)
                    {
                        setTransactionStatus(false);
                        clearDisplay();
                        sendToDisplay("\n\nREQUEST PIN NOT SUPPORTED");
                    }
                }
            }
        }

        private void processPINDataForPANRequest(PINData pinData)
        {
            bool validPIN = true; // PIN Validation Result from Payment Gateway

            IDevice device = getDevice();

            if (device != null)
            {
                if (device.getConnectionInfo().getDeviceType() == DeviceType.MMS)
                {
                    PANRequest panRequest = new PANRequest(60, TransactionBuilder.GetPaymentMethods(false, false, false, false));

                    PINRequest pinRequest = new PINRequest();

                    pinRequest.PINMode = validPIN ? (byte)0xFF : (byte)0xFE; // (PIN Entry Successful) : (PIN Entry SuccessfulFailed)
                    pinRequest.Format = 0;

                    if (device.requestPAN(panRequest, pinRequest) == false)
                    {
                        setTransactionStatus(false);
                        clearDisplay();
                        sendToDisplay("\n\nREQUEST PAN NOT SUPPORTED");
                    }
                }
            }
        }

        public void OnEvent(EventType eventType, IData data)
        {
            switch (eventType)
            {
                case EventType.ConnectionState:
                    ConnectionState value = ConnectionStateBuilder.GetValue(data.StringValue);
                    if (value == ConnectionState.Connected)
                    {
                        sendToOutput("[CONNECTED]");
                        updateDeviceStatus(mDevice, true);
                    }
                    else if (value == ConnectionState.Disconnected)
                    {
                        sendToOutput("[DISCONNECTED]");
                        updateDeviceStatus(mDevice, false);
                    }
                    else if (value == ConnectionState.Disconnecting)
                    {
                        sendToOutput("[DISCONNECTING]");
                    }
                    else if (value == ConnectionState.Connecting)
                    {
                        sendToOutput("[CONNECTING]");
                    }
                    break;
                case EventType.DeviceResponse:
                    sendToOutput("[Response]\n" + data.StringValue);
                    break;
                case EventType.DeviceExtendedResponse:
                    sendToOutput("[Extended Response]\n" + data.StringValue);
                    break;
                case EventType.DeviceNotification:
                    sendToOutput("[Notification]\n" + data.StringValue);
                    break;
                case EventType.CardData:
                    sendToOutput("[MSR]\n" + data.StringValue);
                    sendToMSR(data.StringValue);
                    setTransactionStatus(false);
                    break;
                case EventType.TransactionStatus:
                    TransactionStatus status = TransactionStatusBuilder.GetStatusCode(data.StringValue);
                    if (status == TransactionStatus.CardSwiped)
                    {
                        sendToOutput("[CARD SWIPED]");
                    }
                    else if (status == TransactionStatus.CardInserted)
                    {
                        sendToOutput("[CARD INSERTED]");
                    }
                    else if (status == TransactionStatus.CardRemoved)
                    {
                        sendToOutput("[CARD REMOVED]");
                    }
                    else if (status == TransactionStatus.CardDetected)
                    {
                        sendToOutput("[CARD DETECTED]");
                    }
                    else if (status == TransactionStatus.CardCollision)
                    {
                        sendToOutput("[CARD COLLISION]");
                    }
                    else if (status == TransactionStatus.TimedOut)
                    {
                        sendToOutput("[TRANSACTION TIMED OUT]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.HostCancelled)
                    {
                        sendToOutput("[HOST CANCELLED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionCancelled)
                    {
                        sendToOutput("[TRANSACTION CANCELLED]");
                        string statusDetail = TransactionStatusBuilder.GetStatusDetail(data.StringValue);
                        string deviceDetail = TransactionStatusBuilder.GetDeviceDetail(data.StringValue);
                        sendToOutput("(Status Detail=" + statusDetail + ")");
                        sendToOutput("(Device Detail=" + deviceDetail + ")");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionInProgress)
                    {
                        sendToOutput("[TRANSACTION IN PROGRESS]");
                    }
                    else if (status == TransactionStatus.TransactionError)
                    {
                        sendToOutput("[TRANSACTION ERROR]");
                        string statusDetail = TransactionStatusBuilder.GetStatusDetail(data.StringValue);
                        string deviceDetail = TransactionStatusBuilder.GetDeviceDetail(data.StringValue);
                        sendToOutput("(Status Detail=" + statusDetail + ")");
                        sendToOutput("(Device Detail=" + deviceDetail + ")");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionCompleted)
                    {
                        sendToOutput("[TRANSACTION COMPLETED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionApproved)
                    {
                        sendToOutput("[TRANSACTION APPROVED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionDeclined)
                    {
                        sendToOutput("[TRANSACTION DECLINED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionFailed)
                    {
                        sendToOutput("[TRANSACTION FAILED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionNotAccepted)
                    {
                        sendToOutput("[TRANSACTION NOT ACCEPTED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.SignatureCaptureRequested)
                    {
                        sendToOutput("[SIGNATURE CAPTURE REQUESTED]");

                        if (mFallbackManager == null)
                        {
                            if (GetSignatureFromDevice)
                            {
                                requestSignature();
                            }
                            else
                            {
                                clearSignatureCanvas();
                                showSignaturePanel(true);
                            }
                        }
                    }
                    else if (status == TransactionStatus.TechnicalFallback)
                    {
                        sendToOutput("[TECHNICAL FALLBACK]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.QuickChipDeferred)
                    {
                        sendToOutput("[TRANSACTION STATUS / QUICK CHIP DEFERRED]");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.DataEntered)
                    {
                        sendToOutput("[DATA ENTERED]");
                    }
                    else if (status == TransactionStatus.TryAnotherInterface)
                    {
                        sendToOutput("[TRY ANOTHER INTERFACE]");
                    }
                    else if (status == TransactionStatus.BarcodeRead)
                    {
                        sendToOutput("[BARCODE READ]");
                    }
                    else if (status == TransactionStatus.VASError)
                    {
                        sendToOutput("[VAS Error]");
                        string statusDetail = TransactionStatusBuilder.GetStatusDetail(data.StringValue);
                        string deviceDetail = TransactionStatusBuilder.GetDeviceDetail(data.StringValue);
                        sendToOutput("(Status Detail=" + statusDetail + ")");
                        sendToOutput("(Device Detail=" + deviceDetail + ")");
                        setTransactionStatus(false);
                    }
                    else if (status == TransactionStatus.TransactionStartedFromDevice)
                    {
                        sendToOutput("[TRANSACTION STARTED FROM DEVICE]");
                        mTransaction = new Transaction();
                        mTransaction.QuickChip = false;
                    }
                    else if (status == TransactionStatus.TransactionStartedFromDeviceQuickChip)
                    {
                        sendToOutput("[TRANSACTION STARTED FROM DEVICE (QUICK_CHIP)]");
                        mTransaction = new Transaction();
                        mTransaction.QuickChip = true;
                    }
                    else if (status == TransactionStatus.TransactionCancelledFromDevice)
                    {
                        sendToOutput("[TRANSACTION CANCELLED FROM DEVICE]");
                    }
                    break;
                case EventType.DisplayMessage:
                    string displayMessage = data.StringValue;
                    if (displayMessage.Length > 1)
                    {
                        clearDisplay();
                        displayMessage = displayMessage.Replace((char)0, '\n').Replace((char)0x0A, '\n');
                        sendToDisplay(displayMessage);
                        sendToOutput("[DisplayMessage] : " + displayMessage);
                    }
                    break;
                case EventType.ClearDisplay:
                    clearDisplay();
                    break;
                case EventType.InputRequest:
                    sendToOutput("[InputRequest]\n");
                    processInputRequest(data.ByteArray);
                    break;
                case EventType.EnhancedInputRequest:
                    sendToOutput("[EnhancedInputRequest]\n");
                    processEnhancedInputRequest(data.ByteArray);
                    break;
                case EventType.AuthorizationRequest:
                    sendToOutput("[Authorization Request]\n" + MTParser.getHexString(data.ByteArray));
                    sendToAuthorization(data.ByteArray);
                    if ((mTransaction != null) && (mTransaction.QuickChip == false))
                    {
                        processAuthorizationRequest(data.ByteArray);
                        processSCDE(data.StringValue);
                    }
                    setTransactionStatus(false);
                    break;
                case EventType.TransactionResult:
                    sendToOutput("[Transaction Result]\n" + MTParser.getHexString(data.ByteArray));
                    sendToResult(data.ByteArray);
                    setTransactionStatus(false);
                    delayClearDisplay(3000);
                    break;
                case EventType.PANData:
                    sendToOutput("[PAN Data]\n" + MTParser.getHexString(data.ByteArray));
                    PANData panData = PANDataBuilder.GetPANData(mDevice.getConnectionInfo().getDeviceType(), data.ByteArray);
                    if (panData != null)
                    {
                        sendToOutput("Encrypted Data=" + MTParser.getHexString(panData.Data));
                        sendToOutput("PAN KSN=" + MTParser.getHexString(panData.KSN));
                        sendToOutput("PAN Encryption Type=" + panData.EncryptionType);
                    }
                    setTransactionStatus(false);
                    break;
                case EventType.PINData:
                    sendToOutput("[PIN Data]\n" + MTParser.getHexString(data.ByteArray));
                    PINData pinData = PINDataBuilder.GetPINData(mDevice.getConnectionInfo().getDeviceType(), data.ByteArray);
                    if (pinData != null)
                    {
                        sendToOutput("PIN Block Format=" + pinData.Format);
                        sendToOutput("PIN Block=" + MTParser.getHexString(pinData.PINBlock));
                        sendToOutput("PIN KSN=" + MTParser.getHexString(pinData.KSN));
                        sendToOutput("PIN Encryption Type=" + pinData.EncryptionType);

                        if (mPANRequest != null)
                        {
                            processPINDataForPANRequest(pinData);
                            mPANRequest = null;
                        }
                        else
                        {
                            processPINData(pinData);
                        }
                    }
                    setTransactionStatus(false);
                    break;
                case EventType.BarCodeData:
                    sendToOutput("[Bar Code Data]\n" + MTParser.getHexString(data.ByteArray));
                    DeviceType deviceType2 = mDevice.getConnectionInfo().getDeviceType();
                    BarCodeData bcData = BarCodeDataBuilder.GetBarCodeData(deviceType2, data.ByteArray);
                    if (bcData != null)
                    {
                        sendToOutput("Bar Code Data=" + MTParser.getHexString(bcData.Data));
                        bool encrypted = bcData.Encrypted;
                        if (encrypted)
                        {
                            sendToOutput("Encryption Type=" + bcData.EncryptionType);
                            sendToOutput("KSN=" + MTParser.getHexString(bcData.KSN));
                        }
                        else
                        {
                            string asciiText = System.Text.Encoding.UTF8.GetString(bcData.Data);
                            sendToOutput("ASCII Text=" + asciiText);
                        }
                    }
                    setTransactionStatus(false);
                    break;
                case EventType.Signature:
                    sendToOutput("[Signature]\n" + MTParser.getHexString(data.ByteArray));
                    setTransactionStatus(false);
                    showSignature(data.ByteArray);
                    break;
                case EventType.OperationStatus:
                    OperationStatus opStatus = OperationStatusBuilder.GetStatusCode(data.StringValue);
                    string opDetail = OperationStatusBuilder.GetOperationDetail(data.StringValue);
                    if (opStatus == OperationStatus.Started)
                    {
                        sendToOutput("[OPERATION STARTED: " + opDetail + "]");
                    }
                    else if (opStatus == OperationStatus.Warning)
                    {
                        sendToOutput("[OPERATION WARNING: " + opDetail + "]");
                        string statusDetail = OperationStatusBuilder.GetStatusDetail(data.StringValue);
                        string deviceDetail = OperationStatusBuilder.GetDeviceDetail(data.StringValue);
                        sendToOutput("(Status Detail=" + statusDetail + ")");
                        sendToOutput("(Device Detail=" + deviceDetail + ")");
                    }
                    else if (opStatus == OperationStatus.Failed)
                    {
                        sendToOutput("[OPERATION FAILED: " + opDetail + "]");
                        string statusDetail = OperationStatusBuilder.GetStatusDetail(data.StringValue);
                        string deviceDetail = OperationStatusBuilder.GetDeviceDetail(data.StringValue);
                        sendToOutput("(Status Detail=" + statusDetail + ")");
                        sendToOutput("(Device Detail=" + deviceDetail + ")");
                    }
                    else if (opStatus == OperationStatus.Done)
                    {
                        sendToOutput("[OPERATION DONE: " + opDetail + "]");
                    }
                    break;
                case EventType.DeviceEvent:
                    DeviceEvent deviceEvent = DeviceEventBuilder.GetEventValue(data.StringValue);
                    string eventDetail = DeviceEventBuilder.GetDetail(data.StringValue);
                    if (deviceEvent == DeviceEvent.DeviceResetOccurred)
                    {
                        sendToOutput("[DEVICE RESET OCCURRED: " + eventDetail + "]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceResetWillOccur)
                    {
                        sendToOutput("[DEVICE RESET WILL OCCUR: " + eventDetail + "]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceBondingFailure)
                    {
                        sendToOutput("[DEVICE BONDING FAULURE]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceTemperatureLow)
                    {
                        sendToOutput("[DEVICE TEMPERATURE LOW: " + eventDetail + "]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceTemperatureHigh)
                    {
                        sendToOutput("[DEVICE TEMPERATURE HIGH: " + eventDetail + "]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceBatteryLow)
                    {
                        sendToOutput("[DEVICE BATTERY LOW: " + eventDetail + "]");
                    }
                    else if (deviceEvent == DeviceEvent.DeviceBatteryLowPowerDown)
                    {
                        sendToOutput("[DEVICE BATTERY LOW POWER DOWN: " + eventDetail + "]");
                    }

                    break;
                case EventType.UserEvent:
                    UserEvent userEvent = UserEventBuilder.GetValue(data.StringValue);
                    processUserEvent(userEvent, data.StringValue);
                    break;
                case EventType.FeatureStatus:
                    DeviceFeature feature = FeatureStatusBuilder.GetDeviceFeature(data.StringValue);
                    FeatureStatus featureStatus = FeatureStatusBuilder.GetFeatureStatus(data.StringValue);
                    string featureName = getFeatureName(feature);
                    sendToOutput("[Feature: " + featureName + "]");
                    if (featureStatus == FeatureStatus.Success)
                    {
                        sendToOutput("Status: Sucess");
                    }
                    else if (featureStatus == FeatureStatus.Failed)
                    {
                        sendToOutput("Status: Failed");
                    }
                    else if (featureStatus == FeatureStatus.TimedOut)
                    {
                        sendToOutput("Status: Timed out");
                    }
                    else if (featureStatus == FeatureStatus.Cancelled)
                    {
                        sendToOutput("Status: Cancelled");
                    }
                    else if (featureStatus == FeatureStatus.Error)
                    {
                        sendToOutput("Status: Error");
                    }
                    else if (featureStatus == FeatureStatus.HardwareNA)
                    {
                        sendToOutput("Status: Hardware NA");
                    }
                    break;
                case EventType.NFCEvent:
                    NFCEvent nfcEvent = NFCEventBuilder.GetEventValue(data.StringValue);
                    if (nfcEvent == NFCEvent.TagRemoved)
                    {
                        mNFCState = NFCState.NONE;
                        sendToOutput("[NFCEvent: Tag Removed]");
                        setTransactionStatus(false);
                        enableNFCTagButton(false);
                        enableClassicNFCTagButton(false);
                    }
                    else if (nfcEvent == NFCEvent.NFCMifareUltralight)
                    {
                        enableNFCTagButton(true);
                        mNFCState = NFCState.TAG_DETECTED;
                        sendToOutput("[NFCEvent: NFC/Mifare Ultralight]");
                    }
                    else if (nfcEvent == NFCEvent.MifareClassic1K)
                    {
                        enableClassicNFCTagButton(true);
                        mNFCState = NFCState.CLASSIC_1K_DETECTED;
                        sendToOutput("[NFCEvent: NFCMifare Classic 1K]");
                    }
                    else if (nfcEvent == NFCEvent.MifareClassic4K)
                    {
                        enableClassicNFCTagButton(true);
                        mNFCState = NFCState.CLASSIC_4K_DETECTED;
                        sendToOutput("[NFCEvent: NFCMifare Classic 4K]");
                    }
                    else if (nfcEvent == NFCEvent.MifareDESFire)
                    {
                        mNFCState = NFCState.DESFIRE_DETECTED;
                        sendToOutput("[NFCEvent: NFC/Mifare DESFire]");
                    }
                    else if (nfcEvent == NFCEvent.Failed)
                    {
                        mNFCState = NFCState.READY;
                        sendToOutput("[NFCEvent: Failed");
                    }
                    else if (nfcEvent == NFCEvent.IOFailed)
                    {
                        mNFCState = NFCState.READY;
                        sendToOutput("[NFCEvent: I/O Failed");
                    }
                    else if (nfcEvent == NFCEvent.AuthenticationFailed)
                    {
                        string detail = NFCEventBuilder.GetDetail(data.StringValue);

                        mNFCState = NFCState.READY;
                        sendToOutput("[NFCEvent: Authentication Failed, Block(0x" + detail + ") ]");
                    }
                    break;
                case EventType.NFCData:
                    processNFCData(data);
                    break;
                case EventType.NFCResponse:
                    processNFCResponse(data);
                    break;
                case EventType.NFCAPDUResponse:
                    processNFCAPDUResponse(data);
                    break;
                case EventType.TouchscreenSignatureCapture:
                    sendToOutput("TouchscreenSignatureCapture");
                    break;
                case EventType.TouchscreenFunctionalButtonSelected:
                    sendToOutput("Touchscreen Functional Button Selected: " + data.StringValue);
                    break;
                case EventType.TouchscreenTextStringButtonSelected:
                    sendToOutput("Touchscreen Text String Button Selected: " + data.StringValue);
                    break;
                case EventType.TouchscreenAmountButtonSelected:
                    sendToOutput("Touchscreen Amount Button Selected: " + data.StringValue);
                    break;
                case EventType.TouchscreenPresentCardFunctionalButtonSelected:
                    sendToOutput("Touchscreen Present Card Functional Button Selected");
                    break;
            }

            if (mFallbackManager != null)
            {
                try
                {
                    mFallbackManager.OnEvent(eventType, data);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private bool writeClassicNFCData(int sector, byte[] data)
        {
            if (data == null)
                return false;

            int len = data.Length;

            if (sector < 0)
                return false;

            if (mNFCState == NFCState.CLASSIC_1K_READY)
            {
                if (sector >= 16)
                    return false;

                if (len != 64) // 16 x 4 blocks
                    return false;
            }
            else if (mNFCState == NFCState.CLASSIC_4K_READY)
            {
                if (sector >= 40)
                {
                    return false;
                }
                else if (sector >= 32)
                {
                    if (len != 256) // 16 x 16 blocks
                        return false;
                }
                else
                {
                    if (len != 64) // 16 x 4 block
                        return false;
                }
            }
            else
            {
                // Not in ready state to write
                return false;
            }

            string dataString = MTParser.getHexString(data);

            sendToOutput("[NFC writeClassicNFCData started...]");

            sendToOutput("[NFC writeClassicNFCData=" + dataString + "]");

            if (mNFCState == NFCState.CLASSIC_1K_READY)
            {
                mNFCState = NFCState.CLASSIC_1K_WRITE;
            }
            else if (mNFCState == NFCState.CLASSIC_4K_READY)
            {
                mNFCState = NFCState.CLASSIC_4K_WRITE;
            }

            if (sector == 0)
            {
                string dataString1 = dataString.Substring(32, dataString.Length - 32); // block 0, sector 0 is protected, write only block 1-3

                String writeCommand = "A0" + MTParser.getHexString(new byte[] { (byte)sector }) + "010300" + CLASSIC_KEY_A + dataString1; // block 0

                sendToOutput("[Classic Write Sector (" + sector + ") Command=" + writeCommand + "]");

                mDevice.sendClassicNFCCommand(new BaseData(writeCommand), true);
            }
            else if (sector <= 31)
            {
                String writeCommand = "A0" + MTParser.getHexString(new byte[] { (byte)sector }) + "000300" + CLASSIC_KEY_A + dataString; // block 0-3

                sendToOutput("[Classic Write Sector (" + sector + ") Command=" + writeCommand + "]");

                mDevice.sendClassicNFCCommand(new BaseData(writeCommand), true);
            }
            else if (sector <= 39)
            {
                String writeCommand = "A0" + MTParser.getHexString(new byte[] { (byte)sector }) + "000F00" + CLASSIC_KEY_A + dataString; // blocks 0-15

                sendToOutput("[Classic Write Sector (" + sector + ") Command=" + writeCommand + "]");

                mDevice.sendClassicNFCCommand(new BaseData(writeCommand), true);
            }

            return true;
        }

        private bool writeNDEFMessage(byte[] data)
        {
            if (data == null)
                return false;

            if (data.Length < 1)
                return false;

            if (mNFCState != NFCState.READY)
                return false;

            sendToOutput("[NFC writeNDEFMessage started...]");

            sendToOutput("[NFC writeNDEFMessage=" + MTParser.getHexString(data) + "]");

            int len = data.Length;
            mNDEFBytes = new byte[len];
            Array.Copy(data, 0, mNDEFBytes, 0, len);

            mNDEFBlock = 0;

            writeNDEFBlock(mNDEFBlock++);

            return true;
        }

        private bool writeNDEFBlock(int block)
        {
            if (mNDEFBytes != null)
            {
                int len = mNDEFBytes.Length;

                int i = block * 4;

                sendToOutput("[NFC writeNDEFBlock i=" + block + " Len=" + len + "]");

                if ((i) < len)
                {
                    byte[] data = new byte[6];

                    data[0] = 0xA2;

                    int page = block + 4;
                    data[1] = (byte)(page);

                    if (i < len)
                        data[2] = mNDEFBytes[i++];
                    if (i < len)
                        data[3] = mNDEFBytes[i++];
                    if (i < len)
                        data[4] = mNDEFBytes[i++];
                    if (i < len)
                        data[5] = mNDEFBytes[i++];

                    string dataString = MTParser.getHexString(data);

                    sendToOutput("[NFC Write Page=" + page + " Data=" + dataString + "]");

                    mNFCState = NFCState.WRITE;

                    bool lastCommand = false;

                    if (i >= len)
                        lastCommand = true;

                    mDevice.sendNFCCommand(new BaseData(dataString), lastCommand);

                    return true;
                }
            }

            return false;
        }

        private void parseNDEFMessage(string msgString)
        {
            byte[] msgBytes = MTParser.getByteArrayFromHexString(msgString);

            if (msgBytes != null)
            {
                try
                {
                    List<MTNdefRecord> ndefRecords = MTNdef.Parse(msgBytes);

                    foreach (MTNdefRecord record in ndefRecords)
                    {
                        if (record.isWellKnownType())
                        {
                            if (record.isUri())
                            {
                                sendToParserOutput("Well Known Type RTD URI: " + record.getUriString());
                            }
                            else if (record.isText())
                            {
                                sendToParserOutput("Well Known Type RTD Text: " + record.getTextString());
                            }
                        }
                        else if (record.isExternalType())
                        {
                            if (record.Type != null)
                            {
                                sendToParserOutput("External Type Name: " + System.Text.Encoding.UTF8.GetString(record.Type));

                                if (record.Payload != null)
                                {
                                    string payloadString = MTParser.getHexString(record.Payload);

                                    sendToParserOutput("External Type Payload: " + payloadString);

                                    parseNDEFMessage(payloadString);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    sendToParserOutput("Exception: " + ex.Message);
                }
            }
        }

        private string getFeatureName(DeviceFeature feature)
        {
            string featureName = "Unknown";

            switch (feature)
            {
                case DeviceFeature.SignatureCapture:
                    featureName = "Signature Capture";
                    break;
                case DeviceFeature.PINEntry:
                    featureName = "PIN Entry";
                    break;
                case DeviceFeature.PANEntry:
                    featureName = "PAN Entry";
                    break;
                case DeviceFeature.ShowBarCode:
                    featureName = "Show Bar Code";
                    break;
                case DeviceFeature.ScanBarCode:
                    featureName = "Scan Bar Code";
                    break;
                case DeviceFeature.DisplayMessage:
                    featureName = "Display Message";
                    break;
            }

            return featureName;
        }

        public void OnProgress(int progress)
        {
            switch (mFileTransferMode)
            {
                case FileTransferMode.SEND_IMAGE:
                    sendToOutput("Send Image Progress: " + progress);
                    break;
                case FileTransferMode.SEND_FILE:
                    sendToOutput("Send File Progress: " + progress);
                    break;
                case FileTransferMode.GET_FILE:
                    sendToOutput("Get File Progress: " + progress);
                    break;
                case FileTransferMode.UPDATE_FIRMWARE:
                    sendToOutput("Update Firmware Progress: " + progress);
                    break;
            }
        }

        protected bool writeFile(string fileName, byte[] data)
        {
            bool result = false;

            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                writer.Write(data);

                result = true;
            }

            return result;
        }

        public void OnResult(StatusCode status, byte[] data)
        {
            switch (mFileTransferMode)
            {
                case FileTransferMode.SEND_IMAGE:
                    sendToOutput("Send Image Result: " + status);
                    break;
                case FileTransferMode.SEND_FILE:
                    sendToOutput("Send File Result: " + status);
                    break;
                case FileTransferMode.GET_FILE:
                    sendToOutput("Get File Result: " + status);
                    if (status == StatusCode.SUCCESS)
                    {
                        if (writeFile(mGetFileName, data))
                        {
                            sendToOutput("Saved to File: " + mGetFileName);
                        }
                    }
                    break;
                case FileTransferMode.UPDATE_FIRMWARE:
                    sendToOutput("Update Firmware Result: " + status);
                    break;
            }

            mFileTransferMode = FileTransferMode.NONE;
        }

        public IResult OnCalculateMAC(byte macType, byte[] data)
        {
            IResult result = new Result(StatusCode.UNAVAILABLE);

            return result;
        }

        public void OnUseChipReader()
        {
            sendToOutput("Display Message: USE CHIP READER");

            if (mNonDisplayDevice)
                sendToDisplay("USE CHIP READER");
            else
                displayMessage("11", 3);

            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    bool contactless = mTransaction.PaymentMethods.Contains(PaymentMethod.Contactless);
                    mTransaction.PaymentMethods = TransactionBuilder.GetPaymentMethods(false, true, contactless, false);

                    IDevice device = getDevice();

                    if (device != null)
                    {
                        if (device.startTransaction(mTransaction))
                        {
                            sendToOutput("[Chip Transaction Started]");
                            sendToOutput("Amount=" + mTransaction.Amount + ", Timeout=" + mTransaction.Timeout + ", Transaction Type=" + mTransaction.TransactionType);
                        }
                    }

                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        public void OnUseMSR()
        {
            sendToOutput("Display Message: USE MAGSTRIPE");

            if (mNonDisplayDevice)
                sendToDisplay("USE MAGSTRIPE");
            else
                displayMessage("12", 3);

            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    mTransaction.PaymentMethods = TransactionBuilder.GetPaymentMethods(true, false, false, false);
                    mTransaction.PreventMSRSignatureForCardWithICC = false;

                    IDevice device = getDevice();

                    if (device != null)
                    {
                        if (device.startTransaction(mTransaction))
                        {
                            sendToOutput("[MSR Transaction Started]");
                            sendToOutput("Amount=" + mTransaction.Amount + ", Timeout=" + mTransaction.Timeout + ", Transaction Type=" + mTransaction.TransactionType);
                        }
                    }

                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        public void OnTryAgain()
        {
            sendToOutput("Display Message: TRY AGAIN");

            if (mNonDisplayDevice)
                sendToDisplay("TRY AGAIN");
            else
                displayMessage("13", 3);

            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    bool msr = mTransaction.PaymentMethods.Contains(PaymentMethod.MSR);
                    bool contact = mTransaction.PaymentMethods.Contains(PaymentMethod.Contact);
                    bool contactless = mTransaction.PaymentMethods.Contains(PaymentMethod.Contactless);
                    mTransaction.PaymentMethods = TransactionBuilder.GetPaymentMethods(msr, contact, contactless, false);

                    IDevice device = getDevice();

                    if (device != null)
                    {
                        if (device.startTransaction(mTransaction))
                        {
                            sendToOutput("[Transaction Started]");
                            sendToOutput("Amount=" + mTransaction.Amount + ", Timeout=" + mTransaction.Timeout + ", Transaction Type=" + mTransaction.TransactionType);
                        }
                    }

                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        public void OnSignatureCaptureRequested()
        {
            sendToOutput("Request Signature");

            if (GetSignatureFromDevice)
            {
                requestSignature();
            }
            else
            {
                clearSignatureCanvas();
                showSignaturePanel(true);
            }
        }

        private void processNFCData(IData data)
        {
            sendToOutput("[NFCData: UID=" + data.StringValue + "]");

            if (mNFCState == NFCState.TAG_DETECTED)
            {
                String getVersion = "60";
                mNFCState = NFCState.GET_VERSION;
                mDevice.sendNFCCommand(new BaseData(getVersion));
            }
            else if (mNFCState == NFCState.CLASSIC_1K_DETECTED)
            {
                mClassicNFCData = new List<string>();
                mReadSector = 0;

                String read1K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000300" + ClassicKeyA[mReadSector];

                sendToOutput("[NFCResponse: Classic 1K Read Sector (0) Command=" + read1K + "]");

                mNFCState = NFCState.CLASSIC_1K_READ;
                mDevice.sendClassicNFCCommand(new BaseData(read1K));
            }
            else if (mNFCState == NFCState.CLASSIC_4K_DETECTED)
            {
                mClassicNFCData = new List<string>();
                mReadSector = 0;

                String read4K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000300" + ClassicKeyA[mReadSector];

                sendToOutput("[NFCResponse: Classic 4K Read Sector (0) Command=" + read4K + "]");

                mNFCState = NFCState.CLASSIC_4K_READ;
                mDevice.sendClassicNFCCommand(new BaseData(read4K));
            }
            else if (mNFCState == NFCState.DESFIRE_DETECTED)
            {
                sendToOutput("[Mifare DESFire - Get Version P1]");
                mNFCState = NFCState.DESFIRE_GET_VERSION_P1;
                mDevice.sendDESFireNFCCommand(new BaseData("9060000000"));
            }
        }

        private void processNFCResponse(IData data)
        {
            if (mNFCState == NFCState.GET_VERSION)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCData nfcData = NFCDataBuilder.GetNFCData(deviceType, data.ByteArray);

                if (nfcData != null)
                {
                    if (!nfcData.Encrypted)
                    {
                        string responseString = MTParser.getHexString(nfcData.Data);
                        sendToOutput("[NFCResponse: GetVersion=" + responseString + "]");

                        if (responseString != null)
                        {
                            if (responseString.CompareTo("0004040201000F03") == 0)
                            {
                                sendToOutput("[NTAG213]");
                                string fastReadNTAG213 = "3A0427"; // NTAG213 : 180 bytes / 45 pages (Read user memory page 4-39)
                                mNFCState = NFCState.FAST_READ;
                                mDevice.sendNFCCommand(new BaseData(fastReadNTAG213), NFCReadOnlyMode);
                            }
                            else if (responseString.CompareTo("0004040201001103") == 0)
                            {
                                sendToOutput("[NTAG215]");
                                string fastReadNTAG215 = "3A0481"; // NTAG215 : 540 bytes / 135 pages (Read user memory page 4-129)
                                mNFCState = NFCState.FAST_READ;
                                mDevice.sendNFCCommand(new BaseData(fastReadNTAG215), NFCReadOnlyMode);
                            }
                            else if (responseString.CompareTo("0004040201001303") == 0)
                            {
                                sendToOutput("[NTAG216]");
                                string fastReadNTAG216 = "3A04E1"; // NTAG216 : 924 bytes / 231 pages (Read user memory page 4-225)
                                mNFCState = NFCState.FAST_READ;
                                mDevice.sendNFCCommand(new BaseData(fastReadNTAG216), NFCReadOnlyMode);
                            }
                        }
                    }
                    else
                    {
                        sendToOutput("NFC Data=" + MTParser.getHexString(nfcData.Data));
                        sendToOutput("Encryption Type=" + nfcData.EncryptionType);
                        sendToOutput("KSN=" + MTParser.getHexString(nfcData.KSN));
                        mNFCState = NFCState.READY;
                    }
                }
            }
            else if (mNFCState == NFCState.FAST_READ)
            {
                mNFCState = NFCState.READY;
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCData nfcData = NFCDataBuilder.GetNFCData(deviceType, data.ByteArray);

                if ((nfcData != null) && (!nfcData.Encrypted))
                {
                    string responseString = MTParser.getHexString(nfcData.Data);
                    sendToOutput("[NFCResponse: FastRead=" + responseString + "]");

                    List<string> ndefMessages = MTNdef.getNDEFMessages(responseString);

                    foreach (string msgString in ndefMessages)
                    {
                        byte[] msgBytes = MTParser.getByteArrayFromHexString(msgString);

                        if (msgBytes != null)
                        {
                            try
                            {
                                mNDEFRecords = MTNdef.Parse(msgBytes);

                                foreach (MTNdefRecord record in mNDEFRecords)
                                {
                                    if (record.isWellKnownType())
                                    {
                                        if (record.isUri())
                                        {
                                            sendToOutput("Well Known Type RTD URI: " + record.getUriString());
                                            //launchWebPage(uriString);
                                        }
                                        else if (record.isText())
                                        {
                                            sendToOutput("Well Known Type RTD Text: " + record.getTextString());
                                        }
                                    }
                                    else if (record.isExternalType())
                                    {
                                        if (record.Type != null)
                                        {
                                            sendToOutput("External Type Name: " + System.Text.Encoding.UTF8.GetString(record.Type));

                                            if (record.Payload != null)
                                            {
                                                sendToOutput("External Type Payload: " + MTParser.getHexString(record.Payload));
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                sendToOutput("Exception: " + ex.Message);
                            }

                        }
                    }
                }
            }
            else if (mNFCState == NFCState.WRITE)
            {
                if (writeNDEFBlock(mNDEFBlock++) == false)
                {
                    sendToOutput("[NFC writeNDEFMessage done]");
                    mNFCState = NFCState.READY;
                }
            }
            else if (mNFCState == NFCState.CLASSIC_1K_READ)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCData nfcData = NFCDataBuilder.GetNFCData(deviceType, data.ByteArray);

                if ((nfcData != null) && (!nfcData.Encrypted))
                {
                    string responseString = MTParser.getHexString(nfcData.Data);
                    sendToOutput("[NFCResponse: Classic 1K Read Sector " + mReadSector + "=" + responseString + "]");

                    string dataString = responseString;

                    if (responseString.Length == 128)
                    {
                        string keyA = responseString.Substring(96, 12);
                        if (keyA.Equals("000000000000"))
                        {
                            dataString = responseString.Substring(0, 96) + ClassicKeyA[mReadSector] + responseString.Substring(108, 20);
                            sendToOutput("[Actual Sector (" + mReadSector + ") Data=" + dataString + "]");
                        }
                    }

                    mClassicNFCData.Add(dataString);
                }

                mReadSector++;

                if (mReadSector <= 15)
                {
                    String read1K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000300" + ClassicKeyA[mReadSector];

                    if (mReadSector == 5)
                    {
                        read1K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000301" + ClassicKeyB[mReadSector];
                    }

                    sendToOutput("[NFCResponse: Classic 1K Read Sector (" + mReadSector + ") Command=" + read1K + "]");

                    if (mReadSector == 15)
                        mDevice.sendClassicNFCCommand(new BaseData(read1K), NFCReadOnlyMode);
                    else
                        mDevice.sendClassicNFCCommand(new BaseData(read1K));
                }
                else
                {
                    mNFCState = NFCState.CLASSIC_1K_READY;
                }
            }
            else if (mNFCState == NFCState.CLASSIC_4K_READ)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCData nfcData = NFCDataBuilder.GetNFCData(deviceType, data.ByteArray);

                if ((nfcData != null) && (!nfcData.Encrypted))
                {
                    string responseString = MTParser.getHexString(nfcData.Data);
                    sendToOutput("[NFCResponse: Classic 4K Read Sector " + mReadSector + "=" + responseString + "]");

                    string dataString = responseString;

                    if (responseString.Length == 128)
                    {
                        string keyA = responseString.Substring(96, 12);
                        if (keyA.Equals("000000000000"))
                        {
                            dataString = responseString.Substring(0, 96) + ClassicKeyA[mReadSector] + responseString.Substring(108, 20);
                            sendToOutput("[Actual Sector (" + mReadSector + ") Data=" + dataString + "]");
                        }
                    }
                    else if (responseString.Length == 512)
                    {
                        string keyA = responseString.Substring(480, 12);
                        if (keyA.Equals("000000000000"))
                        {
                            dataString = responseString.Substring(0, 480) + ClassicKeyA[mReadSector] + responseString.Substring(492, 20);
                            sendToOutput("[Actual Sector (" + mReadSector + ") Data=" + dataString + "]");
                        }
                    }

                    mClassicNFCData.Add(dataString);
                }

                mReadSector++;

                if (mReadSector <= 31)
                {
                    String read4K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000300" + CLASSIC_KEY_A; // block 0-3

                    sendToOutput("[NFCResponse: Classic 4K Read Sector (" + mReadSector + ") Command=" + read4K + "]");

                    mDevice.sendClassicNFCCommand(new BaseData(read4K));
                }
                else if (mReadSector <= 39)
                {
                    String read4K = "30" + MTParser.getHexString(new byte[] { (byte)mReadSector }) + "000F00" + CLASSIC_KEY_A; // blocks 0-15

                    sendToOutput("[NFCResponse: Classic 4K Read Sector (" + mReadSector + ") Command=" + read4K);

                    if (mReadSector == 39)
                        mDevice.sendClassicNFCCommand(new BaseData(read4K), NFCReadOnlyMode);
                    else
                        mDevice.sendClassicNFCCommand(new BaseData(read4K));
                }
                else
                {
                    mNFCState = NFCState.CLASSIC_4K_READY;
                }
            }
            else if (mNFCState == NFCState.CLASSIC_1K_WRITE)
            {
                sendToOutput("[NFC CLASSIC_1K_WRITE Sector (" + mWriteSector + ") done]");
                mNFCState = NFCState.READY;
            }
            else if (mNFCState == NFCState.CLASSIC_4K_WRITE)
            {
                sendToOutput("[NFC CLASSIC_4K_WRITE Sector (" + mWriteSector + ") done]");
                mNFCState = NFCState.READY;
            }
        }

        private void processNFCAPDUResponse(IData data)
        {
            if (mNFCState == NFCState.DESFIRE_GET_VERSION_P1)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCRAPDUData nfcRAPDUData = NFCDataBuilder.GetNFCRAPDUData(deviceType, data.ByteArray);

                if (nfcRAPDUData != null)
                {
                    if (nfcRAPDUData.Response != null)
                    {
                        string responseString = MTParser.getHexString(nfcRAPDUData.Response);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 1 Response=" + responseString + "]");
                    }

                    if (!nfcRAPDUData.Encrypted)
                    {
                        string dataString = MTParser.getHexString(nfcRAPDUData.Data);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 1 Data=" + dataString + "]");
                    }
                }

                sendToOutput("[Mifare DESFire - Get Version P2]");
                mNFCState = NFCState.DESFIRE_GET_VERSION_P2;
                mDevice.sendDESFireNFCCommand(new BaseData("90AF000000"));
            }
            else if (mNFCState == NFCState.DESFIRE_GET_VERSION_P2)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCRAPDUData nfcRAPDUData = NFCDataBuilder.GetNFCRAPDUData(deviceType, data.ByteArray);

                if (nfcRAPDUData != null)
                {
                    if (nfcRAPDUData.Response != null)
                    {
                        string responseString = MTParser.getHexString(nfcRAPDUData.Response);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 2 Response=" + responseString + "]");
                    }

                    if (!nfcRAPDUData.Encrypted)
                    {
                        string dataString = MTParser.getHexString(nfcRAPDUData.Data);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 2 Data=" + dataString + "]");
                    }
                }

                sendToOutput("[Mifare DESFire - Get Version P3]");
                mNFCState = NFCState.DESFIRE_GET_VERSION_P3;
                mDevice.sendDESFireNFCCommand(new BaseData("90AF000000"));
            }
            else if (mNFCState == NFCState.DESFIRE_GET_VERSION_P3)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCRAPDUData nfcRAPDUData = NFCDataBuilder.GetNFCRAPDUData(deviceType, data.ByteArray);

                if (nfcRAPDUData != null)
                {
                    if (nfcRAPDUData.Response != null)
                    {
                        string responseString = MTParser.getHexString(nfcRAPDUData.Response);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 3 Response=" + responseString + "]");
                    }

                    if (!nfcRAPDUData.Encrypted)
                    {
                        string dataString = MTParser.getHexString(nfcRAPDUData.Data);
                        sendToOutput("[NFCAPDUResponse: Get Version Part 3 Data=" + dataString + "]");
                    }
                }

                sendToOutput("[Mifare DESFire - Select]");
                mNFCState = NFCState.DESFIRE_SELECT;
                mDevice.sendDESFireNFCCommand(new BaseData("00A4000002DF0100"), false); // DF FileID, return FCI
            }
            else if (mNFCState == NFCState.DESFIRE_SELECT)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCRAPDUData nfcRAPDUData = NFCDataBuilder.GetNFCRAPDUData(deviceType, data.ByteArray);

                if (nfcRAPDUData != null)
                {
                    if (nfcRAPDUData.Response != null)
                    {
                        string responseString = MTParser.getHexString(nfcRAPDUData.Response);
                        sendToOutput("[NFCAPDUResponse: Select File Response=" + responseString + "]");
                    }

                    if (!nfcRAPDUData.Encrypted)
                    {
                        string dataString = MTParser.getHexString(nfcRAPDUData.Data);
                        sendToOutput("[NFCAPDUResponse: Select File Data=" + dataString + "]");
                    }
                }

                sendToOutput("[Mifare DESFire - Get Value File 03]");
                mNFCState = NFCState.DESFIRE_GET_VALUE;
                mDevice.sendDESFireNFCCommand(new BaseData("906C0000010300"), NFCReadOnlyMode);
            }
            else if (mNFCState == NFCState.DESFIRE_GET_VALUE)
            {
                DeviceType deviceType = mDevice.getConnectionInfo().getDeviceType();
                NFCRAPDUData nfcRAPDUData = NFCDataBuilder.GetNFCRAPDUData(deviceType, data.ByteArray);

                if (nfcRAPDUData != null)
                {
                    if (nfcRAPDUData.Response != null)
                    {
                        string responseString = MTParser.getHexString(nfcRAPDUData.Response);
                        sendToOutput("[NFCAPDUResponse: Get Value File 03 Response=" + responseString + "]");
                    }

                    if (!nfcRAPDUData.Encrypted)
                    {
                        string dataString = MTParser.getHexString(nfcRAPDUData.Data);
                        sendToOutput("[NFCAPDUResponse: Get Value File 03 Data=" + dataString + "]");
                    }
                }

                mNFCState = NFCState.READY;
            }
        }

    }

}
