using System;
using System.Runtime.InteropServices;

namespace IPS.Adapters.Coffee
{
    /// <summary>
    /// P/Invoke wrapper for FollettoKioskApi.dll
    /// Provides interop between C# and native C++ DLL
    /// </summary>
    internal static class FollettoKioskInterop
    {
        private const string DllName = "FollettoKioskApi.dll";

        #region Structs

        /// <summary>
        /// IP address and port configuration for connecting to booth
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct IPAddressPort
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ipAddress;
            public ushort port;  // unsigned short, not int
        }

        // Structs from working KioskFolletto codebase

        public enum OrderRegistrationErrorCode
        {
            Success = 0,
            OutOfStock,
            ExceedsMaximumAcceptableProductCount,
            Failed
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct OrderResult
        {
            public OrderRegistrationErrorCode errorCode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string boothName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Invoice
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string orderId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string orderLabel;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 999)]
            public string qrData;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string invoiceNo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string refNo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string purchase;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string authCode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
            public string acqRefData;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 999)]
            public string processData;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string recordNo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string tranDeviceId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string DateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Product
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string menuId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string menuAliasCulture;
            public IntPtr options;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string numberOfOptions;
        }

        #endregion

        #region DLL Imports

        /// <summary>
        /// Initialize the DLL's internal HTTP server with the specified port
        /// Must be called before connectToBooths
        /// Returns: ServerInitializationErrorCode (0=Success, 1=InvalidPort, 2=PortUnavailable, 3=InternalError)
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int initializeServer(int port = 5000);

        /// <summary>
        /// Connect to one or more booths (unmanned systems)
        /// Returns: ConnectErrorCode (0=Success, see documentation for other codes)
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int connectToBooths(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IPAddressPort[] ipAddresses,
            int count
        );

        /// <summary>
        /// Get product status as JSON string
        /// Caller must free the returned string with freeString()
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getProductStatusWrapped();

        /// <summary>
        /// Get number of waiting orders
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getNumberOfWaitingOrders();

        /// <summary>
        /// Get estimated waiting time in minutes
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getEstimatedWaitingTime();

        // From working KioskFolletto codebase
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern OrderResult placeOrderWithInvoice(ref Invoice invoice, [In] Product[] products, string numberOfProducts);

        // Note: No shutdown function exists in this DLL version
        // Memory for returned strings is managed internally by the DLL

        #endregion

        #region Helper Methods

        /// <summary>
        /// Safely marshal IntPtr to string
        /// Note: Memory is managed by the DLL, do not free
        /// </summary>
        public static string? MarshalString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// Marshal array of strings to unmanaged memory for Product.options
        /// Caller must free returned IntPtr with FreeUnmanagedStringArray()
        /// </summary>
        public static IntPtr ConvertStringArrayToUnmanaged(string[]? options)
        {
            if (options == null || options.Length == 0)
                return IntPtr.Zero;

            var arrayPtr = Marshal.AllocHGlobal(IntPtr.Size * options.Length);

            for (var i = 0; i < options.Length; i++)
            {
                var strPtr = Marshal.StringToHGlobalAnsi(options[i]);
                Marshal.WriteIntPtr(arrayPtr, i * IntPtr.Size, strPtr);
            }
            return arrayPtr;
        }

        /// <summary>
        /// Free array of strings allocated by ConvertStringArrayToUnmanaged()
        /// </summary>
        public static void FreeUnmanagedStringArray(IntPtr arrayPtr, int count)
        {
            if (arrayPtr == IntPtr.Zero)
                return;

            for (var i = 0; i < count; i++)
            {
                var strPtr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                if (strPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(strPtr);
                }
            }
            Marshal.FreeHGlobal(arrayPtr);
        }

        #endregion
    }
}
