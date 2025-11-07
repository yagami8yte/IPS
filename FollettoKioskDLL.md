# Folletto Kiosk DLL Documentation

[TOC]

## Version

### v0.0.9

**What's new:**

* Added a function for the Kiosk client to publish error.
* Compiled with MBCS <span style="color:red">(Only for Jet Cafe Kiosk Client)</span>



## Overview

`FollettoKioskApi`  is a C++ Dynamic Link Library designed to facilitate and enhance interactions between Kiosk systems and Robot Barista Booths. This library streamlines the process of data exchange and event handling, providing a efficient framework for kiosk-to-booth communication.

Key features of `FollettoKioskApi` include:

1. **Seamless Data Requesting:** It offers a variety of methods to efficiently request and retrieve data from Robot Barista Booths, ensuring timely and accurate communication between systems.
2. **Advanced Event Handling:** The library is equipped to manage and respond to a wide range of events originating from the booth, enabling kiosks to react dynamically to real-time scenarios.



> **Proprietary Notice:**
>
> `FollettoKioskApi` is exclusive property of Folletto Robotics. This library is provided for use under specific terms and conditions. Modification, redistribution, or use of this software in any form without prior written consent from Folletto Robotics is strictly prohibited. Usage must adhere to the stipulated guidelines and any licensing agreements in place.
>
> The features, functionality, and content of `FollettoKioskApi` are subject to change at any time without notice. Folletto Robotics reserves the right to make modifications, enhancements, or updates to the library as deemed necessary.
>
> Folletto Robotics is not responsible for any direct, indirect, incidental, or consequential damages or losses that may arise from the use, misuse, or inability to use `FollettoKioskApi`. Users assume all risks associated with the operation of this software and must agree to use it in accordance with its intended purpose and within the bounds of any applicable laws and regulations.



## Getting Started

### Installation

1. Download provided `FollettoKioskApi.dll` and `FollettoKioskApi.lib`
2. Place the DLL in your project's directory.
3. Add `FollettoKioskApi.lib` to your linker in the project settings.

### Dependencies

- Requires Microsoft Visual C++ Runtime.
- Compatible with C++17 standard and later.



### DLL Compilation Details

* Character set: 

  > * Default: Unicode
  > * Jet Cafe Kiosk Client: MBCS

* Architecture:

  > * 32-bit
  > * 64-bit



## Core Functionality

### > Initialize server

The `initializeServer` is a function provided by the DLL that initializes and starts the Http server inside the DLL. 

#### Function Signature

```c++
ServerInitializationErrorCode initializeServer(int port=5000);
```

#### Parameters

* `port` (`int`): Http server port. Default: `5000`.

#### Return Value

* `ServerInitilizationErrorCode`: An enumeration value representing the outcome of the server initialization attempt.

#### Error Codes

```c++
enum ServerInitializationErrorCode {
    Success = 0,
    InvalidPort,
    PortUnavailable,
    InternalError,
}
```

The `ServerInitializationErrorCode` enumeration includes the following values:

* `Success`: Server initialized successfully.
* `InvalidPort`: Port number is invalid or malformed.
* `PortUnavailable`: Provided port is unavailable or already in use.
* `InternalError`: Internal Error.





### > Connect

The `connectToBooths` method is a function provided by the DLL that establishes a network connection to the main program of the barista robot booth with specified IP and Port. This function establishes connection to the booth and perform necessary actions (e.g. download menu images).

#### Function Signature

```c++
ConnectErrorCode connectToBooths(const IPAddressPort* ipAddresses, int count);
```

#### Parameters

- **`ipAddresses`** (`const IPAddressPort*`): A pointer to the first element in an array of `IPAddressPort` structures. Each structure contains an IP address as a null-terminated string and a port number. <span style="color:red">For current version, only the first item of the array will be considered.</span>

  > ```c++
  > struct IPAddressPort {
  >     char ipAddress[16];
  >     unsigned short port;
  > };
  > ```
  >
  > * **`ipAddress`:** IP address of a booth.
  > * **`port`:** Port of a booth.
  
- **`count`** (`int`): The number of elements in the `ipAddresses` array, indicating how many endpoints the function should attempt to connect to.

#### Return Value

- **`ConnectErrorCode`**: An enumeration value representing the outcome of the connection attempt. The value `Success` indicates a successful connection, while other values indicate specific types of errors.

#### Error Codes

```c++
enum ConnectErrorCode {
    Success = 0,
    InvalidIPAddress,
    InvalidPort,
    IPAddressNotAvailable,
    PortUnavailable,
    ConnectionTimeout,
    NetworkUnreachable,
    ConnectionRefused,
    ResourceLimitation,
    NetworkProtocolError,
    InternalError,
    SecurityRestrictions,
    HostNameResolutionFailure,
    SocketInitializationFailure,
};
```

The `ConnectErrorCode` enumeration includes the following values:

- `Success` (0): Connection established successfully.
- `InvalidIPAddress` (1): The provided IP address is invalid or malformed.
- `InvalidPort` (2): The provided port number is invalid.
- `IPAddressNotAvailable` (3): The specified IP address is not available.
- `PortUnavailable` (4): The specified port on the target IP address is not open or not listening for connections.
- `ConnectionTimeout` (5): The connection attempt timed out, possibly due to network latency or the server not responding.
- `NetworkUnreachable` (6): The network required to reach the specified IP is unreachable.
- `ConnectionRefused` (7): The connection was actively refused by the server.
- `ResourceLimitation` (8): System-level resource limitations prevent establishing a new connection (e.g., too many open sockets).
- `NetworkProtocolError` (9): Errors related to the underlying network protocol.
- `InternalError` (10): Internal Error.
- `SecurityRestrictions` (11): Connection blocked due to security policies, firewall restrictions, or lack of necessary permissions.
- `HostNameResolutionFailure` (12): Failure to resolve a hostname to an IP address.
- `SocketInitializationFailure` (13): Failure in initializing the network socket, possibly due to issues with the network stack or incorrect socket configuration.

#### Example Usage

**Python3**

```python
import ctypes

class IPAddressPort(ctypes.Structure):
    _fields_ = [
        ("ipAddress", ctypes.c_char * 16),  
        ("port", ctypes.c_ushort),
    ]

dll.connectToBooths.argtypes = [ctypes.POINTER(IPAddressPort), ctypes.c_int]

# Load the DLL
dll = ctypes.CDLL('/path/to/dll_file.dll')

# Define the argument and return types for the connect function
dll.connectToBooths.argtypes = [ctypes.POINTER(IPAddressPort), ctypes.c_int]
dll.connectToBooths.restype = ctypes.c_int  # ConnectErrorCode is returned as an int

# Prepare the IPAddressPort array
ip_addresses = (IPAddressPort * 2)()  # Example with 2 IP addresses
ip_addresses[0].ipAddress = b"192.168.1.1"  # Byte string for the IP address
ip_addresses[0].port = 8080  # Port number as integer
ip_addresses[1].ipAddress = b"192.168.1.2"  # Second IP address
ip_addresses[1].port = 8080  

# Call the connect function with the array
result_code = dll.connectToBooths(ip_addresses, 2) 
if result_code == 0:  
    print("Connection successful.")
else:
    print(f"Connection failed with error code: {result_code}")
```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct IPAddressPort
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string ipAddress;  
    public ushort port;
}

[DllImport("path_to_your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
private static extern int connectToBooths([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IPAddressPort[] ipAddresses, int count);

```

```c#
using System;
using System.Runtime.InteropServices;

class Program
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct IPAddressPort
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string ipAddress;
        public ushort port;
    }

    [DllImport("path_to_your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int connectToBooths([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IPAddressPort[] ipAddresses, int count);

    static void Main()
    {
        IPAddressPort[] ipAddresses = new IPAddressPort[]
        {
            new IPAddressPort { ipAddress = "192.168.1.1", port = 8080 },
            new IPAddressPort { ipAddress = "192.168.1.2", port = 8080 }
            // Add more IP addresses and ports as needed
        };

        int resultCode = connectToBooths(ipAddresses, ipAddresses.Length);
        if (resultCode == 0) 
        {
            Console.WriteLine("Connection successful.");
        }
        else
        {
            Console.WriteLine($"Connection failed with error code: {resultCode}");
        }
    }
}
```

**Java**

```java
import com.sun.jna.*;

public class IPAddressPort extends Structure {
    public static class ByReference extends IPAddressPort implements Structure.ByReference {}

    public String ipAddress;
    public short port;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("ipAddress", "port");
    }
}
```

```java
public interface MyLibrary extends Library {
    MyLibrary INSTANCE = (MyLibrary) Native.load("path_to_your_dll", MyLibrary.class);
    int connectToBooths(IPAddressPort.ByReference[] ipAddresses, int count);
}
```

```java
import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Structure;

public class Main {
    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = (MyLibrary) Native.load("path_to_your_dll", MyLibrary.class);
        int connectToBooths(IPAddressPort.ByReference[] ipAddresses, int count);
    }

    public static void main(String[] args) {
        IPAddressPort.ByReference[] ipAddresses = (IPAddressPort.ByReference[]) new IPAddressPort.ByReference().toArray(2);
        
        ipAddresses[0].ipAddress = "192.168.1.1";
        ipAddresses[0].port = 8080;
        ipAddresses[1].ipAddress = "192.168.1.2";
        ipAddresses[1].port = 8080;

        int resultCode = MyLibrary.INSTANCE.connectToBooths(ipAddresses, ipAddresses.length);
        if (resultCode == 0) { 
            System.out.println("Connection successful.");
        } else {
            System.out.println("Connection failed with error code: " + resultCode);
        }
    }
}
```

**C++**

```c++
#include <iostream>

struct IPAddressPort {
    char ipAddress[16]; 
    unsigned short port;
};
```

```c++
extern "C" int connectToBooths(const IPAddressPort* ipAddresses, int count);
```

```c++
#include <iostream>

struct IPAddressPort {
    char ipAddress[16]; 
    unsigned short port;
};

extern "C" int connectToBooths(const IPAddressPort* ipAddresses, int count);

int main() {
    IPAddressPort ipAddresses[] = {
        {"192.168.1.1", 8080},
        {"192.168.1.2", 8080}
        // Add more IP addresses and ports as needed
    };

    int count = sizeof(ipAddresses) / sizeof(IPAddressPort);
    int resultCode = connectToBooths(ipAddresses, count);

    if (resultCode == 0) {
        std::cout << "Connection successful." << std::endl;
    } else {
        std::cout << "Connection failed with error code: " << resultCode << std::endl;
    }

    return 0;
}
```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const IPAddressPort = StructType({
  ipAddress: 'char[16]',
  port: 'ushort'
});
```

```javascript
const myDll = ffi.Library('path_to_your_dll', {
  'connectToBooths': ['int', [ref.refType(IPAddressPort), 'int']],
});
```

```javascript
const ffi = require('ffi-napi');
const ref = require('ref-napi');
const ArrayType = require('ref-array-di')(ref);
const StructType = require('ref-struct-di')(ref);

const IPAddressPort = StructType({
  ipAddress: 'char[16]', // Adjust for IPv6 if needed
  port: 'ushort'
});

const IPAddressPortArray = ArrayType(IPAddressPort);

const myDll = ffi.Library('path_to_your_dll', {
  'connectToBooths': ['int', [ref.refType(IPAddressPort), 'int']],
});

// Create an array of IPAddressPort structures
let ipAddresses = new IPAddressPortArray(2);
ipAddresses[0].ipAddress = "192.168.1.1";
ipAddresses[0].port = 8080;
ipAddresses[1].ipAddress = "192.168.1.2";
ipAddresses[1].port = 8080;

// Convert array to pointer and call the function
const result = myDll.connectToBooths(ipAddresses.ref(), ipAddresses.length);

if (result === 0) {
  console.log("Connection successful.");
} else {
  console.log(`Connection failed with error code: ${result}`);
}
```





### > Get DLL Version <a id="getDllVersion"> </a>

The `getDllVersion` function retrieves the current DLL version.

#### Function Signature

```c++
const char* getDllVersion();
```

#### Parameters

This function does not require any input parameters.

#### Return Value

* `version` `char*`: DLL version. 

  > The `version` is of form `MAJOR.MINOR.PATCH` where,
  >
  > **`MAJOR` version** indicates incompatible API changes. This number is incremented when changes are made that break backward compatibility with previous versions.
  >
  > **`MINOR` version** denotes the addition of functionality in a backward-compatible manner. 
  >
  > **`PATCH` version** is for backward compatible bug fixes that do not add new feature or change existing ones.





### > Get Booth Configuration <a id="getBoothConfig"> </a>

The `getBoothConfiguration` function retrieves the current booth configurations. 

#### Function Signature

```c++
BoothConfig* getBoothConfig();
```

#### Parameters

This function does not require any input parameters.

#### Return Value

* **`BoothConfig`**: A structure containing the current configuration settings of the booth.

  > `BoothConfig` structure.
  >
  > * `qrRequired` (`Boolean`): `true` if QR code is required at beverage pickup, `false` otherwise. If set to `true`, Kiosk should print a QR code with QR data (see `Place Order` section of this document) on the receipt for the customer to use it for beverage pickup. 
  >
  > * `maxProductsInGroup` (`integer`): maximum number of products in a single order. E.g. when set to `3` the maximum number of products that can be ordered at once (in a single group) should be limited to 3. 
  >   -> For example, if `maxProductsInGroup` is equal to 3:
  >
  >   * **Accepted scenario:** a single order containing a list of 3 products.
  >   * **Not accepted:** a single order containing less than 1 product or more than 3 products. 
  >
  > * `languages` (`AvailableLanguage`): Array of available language struct. See `AvailableLanguage` struct below.
  >
  >   > The structure of `AvailableLanguage` is as such:
  >   > ```c++
  >   > struct AvailableLanguage
  >   > {
  >   >        char id[3];             // e.g. "en", "kr", "jp", "es"
  >   >        char display[128]; 		// e.g. "English", "한국어", "日本語", "Español" 
  >   > };
  >   > ```
  >   >
  >   > * `id`: (`char*`): unique id of the language. This `id` will be used as a suffix for fields in Product and options. See <a href="#getProductStatus">Get Product Status</a>.
  >   > * `display` (`char*`): shows how this language will be displayed (UTF-8). 
  >
  > * `languagesCount` (`int`): to keep track of the number of languages

#### Special Note

The DLL provides `freeBoothConfig` method to free the memory allocated. The `freeBoothConfig` function has the following function signature:

```c++
void freeBoothConfig(BoothConfig* config);
```

#### Example Usage

**Python3**

```python
import ctypes
from ctypes import c_int, c_char, c_bool, POINTER, Structure

# Define the C structures in Python
class AvailableLanguage(Structure):
    _fields_ = [
        ("id", c_char * 50),
        ("display", c_char * 256),
    ]

class BoothConfig(Structure):
    _fields_ = [
        ("qrRequired", c_bool),
        ("maxProductsInGroup", c_int),
        ("languages", POINTER(AvailableLanguage)),
        ("languagesCount", c_int),
    ]

# Load the DLL
follett_kiosk_dll = ctypes.CDLL('./FollettKiosk.dll')

# Specify the return types of the functions if they are not int
follett_kiosk_dll.getBoothConfig.restype = BoothConfig

# Call getBoothConfig
get_booth_config = follett_kiosk_dll.getBoothConfig
config = get_booth_config()

# Access the BoothConfig data
print(f"QR Required: {config.qrRequired}")
print(f"Max Products In Group: {config.maxProductsInGroup}")
print(f"Languages Count: {config.languagesCount}")

for i in range(config.languagesCount):
    language = config.languages[i]
    print(f"Language {i}: ID={language.id.decode('utf-8')}, Display={language.display.decode('utf-8')}")

# When done, free the allocated memory
free_booth_config = follett_kiosk_dll.freeBoothConfig
free_booth_config(ctypes.byref(config))

```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct BoothConfig
{
    [MarshalAs(UnmanagedType.I1)]
    public bool qrRequired;
}

class Program
{
    [DllImport("your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern BoothConfig getBoothConfig();

    static void Main()
    {
        BoothConfig config = getBoothConfig();
        if (config.qrRequired)
        {
            // Handle QR code requirement
        }
        // Handle other configurations
    }
}
```

**Java**

```java
import com.sun.jna.*;

public class Main {
    public static class BoothConfig extends Structure {
        public boolean qrRequired;
        // ... other fields ...
    }

    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = Native.load("your_dll_name", MyLibrary.class);
        BoothConfig getBoothConfig();
    }

    public static void main(String[] args) {
        BoothConfig config = MyLibrary.INSTANCE.getBoothConfig();
        if (config.qrRequired) {
            System.out.println("QR code scanning is required.");
        }
        // Handle other configurations
    }
}
```

**C++**

```c++
int main() {
    BoothConfig config = getBoothConfig();
    if (config.qrRequired) {
        // Handle QR code requirement
    }
    // Handle other configurations
}
```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');
const ref = require('ref-napi');
const Struct = require('ref-struct-di')(ref);

const BoothConfig = Struct({
    'qrRequired': 'bool',
    // ... other fields ...
});
const myDll = ffi.Library('path_to_your_dll', {
    'getBoothConfig': [BoothConfig, []]
});

const config = myDll.getBoothConfig();
if (config.qrRequired) {
    console.log("QR code scanning is required.");
}
// Handle other configurations
```





### > Register Booth Configuration Update Callback

The `registerBoothConfigurationUpdateCallback` function is used to register booth configuration update callback function for notification when booth configuration update occurs. 

#### Function Signature

```c++
void registerBoothConfigUpdateCallback(BoothConfigUpdateCallback callback);
```

#### Parameters

* **callback** (`BoothConfigUpdateCallback`): A pointer to the client's callback function. This function should conform to the `BoothConfigUpdateCallback` signature, which is a function pointer type with no parameters and no return value.

#### Callback Function Signature

```c++
int (*BoothConfigUpdateCallback)();
```

#### Callback Function Return Value

* `int` indicating configuration update success/fail

  > `0`: success.
  >
  > `1`: Fail.

#### Example Usage

**Python3**

```python
import ctypes

# Define the callback type
BoothConfigUpdateCallback = ctypes.CFUNCTYPE(None)

# Load the DLL and define argument types
dll = ctypes.CDLL('/path/to/your_dll.dll')
dll.registerBoothConfigUpdateCallback.argtypes = [BoothConfigUpdateCallback]

# Python function to handle the callback
@ctypes.CFUNCTYPE(None)
def my_booth_config_update_handler():
    print("Booth configuration updated.")

# Register the callback
callback = BoothConfigUpdateCallback(my_booth_config_update_handler)
dll.registerBoothConfigUpdateCallback(callback)

```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void BoothConfigUpdateCallback();

    [DllImport("your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void registerBoothConfigUpdateCallback(BoothConfigUpdateCallback callback);

    static void MyBoothConfigUpdateHandler()
    {
        // Handle the configuration update
        Console.WriteLine("Booth configuration updated.");
    }

    static void Main()
    {
        BoothConfigUpdateCallback callbackDelegate = MyBoothConfigUpdateHandler;
        registerBoothConfigUpdateCallback(callbackDelegate);

        // Rest of your client code
    }
}

```

**Java**

```java
import com.sun.jna.*;
import com.sun.jna.ptr.*;

public class Main {
    // Define a callback interface matching the C++ callback signature
    public interface BoothConfigUpdateCallback extends Callback {
        void invoke();
    }

    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = Native.load("your_dll_name", MyLibrary.class);
        void registerBoothConfigUpdateCallback(BoothConfigUpdateCallback callback);
    }

    public static void main(String[] args) {
        BoothConfigUpdateCallback callback = new BoothConfigUpdateCallback() {
            public void invoke() {
                System.out.println("Booth configuration updated.");
            }
        };

        MyLibrary.INSTANCE.registerBoothConfigUpdateCallback(callback);

        // ... rest of your code ...
    }
}

```

**JavaScript**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');

const myDll = ffi.Library('path_to_your_dll', {
    'registerBoothConfigUpdateCallback': ['void', ['pointer']]
});

const callback = ffi.Callback('void', [], () => {
    console.log("Booth configuration updated.");
});

myDll.registerBoothConfigUpdateCallback(callback);

// Keep the callback reference to prevent it from being garbage-collected
process.on('exit', () => {
    callback
});

```





### > Notify Kiosk-side error

The `notifyKioskError` function publishes the error occurred from the Kiosk.

#### Function Signature

```c++
int notifyKioskError(char* errorCodes[], int size);
```

#### Parameters

* **`errorCodes`**: an array of error code strings.
* **`size`**: the size of elements in the error array.

#### Return Value:

* `int` indicating success/fail:

  > `0`: Success
  >
  > `1`: Fail





### > Get Number of Waiting Orders

The `getNumberOfWaitingOrders` function retrieves the current number of waiting orders.

#### Function Signature

```c++
int getNumberOfWaitingOrders();
```

#### Parameters

This function does not require any input parameters.

#### Return Value

* **`int`**: The function returns an integer value representing the number of waiting orders.

#### Example Usage

**Python3**

```python
import ctypes

# Load the DLL
dll = ctypes.CDLL('/path/to/your_dll.dll')

# Define the return type of the function
dll.getNumberOfWaitingOrders.restype = ctypes.c_int

# Call the function
waiting_orders = dll.getNumberOfWaitingOrders()
print(f"Number of waiting orders: {waiting_orders}")
```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("path_to_your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getNumberOfWaitingOrders();

    static void Main()
    {
        int waitingOrders = getNumberOfWaitingOrders();
        Console.WriteLine($"Number of waiting orders: {waitingOrders}");
    }
}
```

**Java**

```java
import com.sun.jna.Library;
import com.sun.jna.Native;

public class Main {
    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = (MyLibrary) Native.load("name_of_your_dll", MyLibrary.class);
        int getNumberOfWaitingOrders();
    }
    
    public static void main(String[] args) {
        int waitingOrders = MyLibrary.INSTANCE.getNumberOfWaitingOrders();
        System.out.println("Number of waiting orders: " + waitingOrders);
    }
}
```

**C++**

```c++
// main.cpp
#include <iostream>

extern "C" int getNumberOfWaitingOrders();

int main() {
    int waitingOrders = getNumberOfWaitingOrders();
    std::cout << "Number of waiting orders: " << waitingOrders << std::endl;

    return 0;
}
```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');

// Define the function signature
const myDll = ffi.Library('path_to_your_dll', {
  'getNumberOfWaitingOrders': ['int', []],
});

// Call the function
const numberOfWaitingOrders = myDll.getNumberOfWaitingOrders();
console.log("Number of waiting orders:", numberOfWaitingOrders);

```





### > Get Estimated Waiting Time

The `getEstimatedWaitingTime` function retrieves the current estimated waiting time.

> :warning:**Warning:** The estimated waiting time is subject to change based on booth conditions and customer interactions, and therefore, it should not be considered an exact measure of the actual waiting period.

#### Function Signature

```c++
int getEstimatedWaitingTime();
```

#### Parameters

This function does not require any input parameters.

#### Return Value

* **`int`**: The function returns an integer value representing the estimated waiting time in seconds.

#### Example Usage

**Python3**

```python
import ctypes

# Load the DLL
dll = ctypes.CDLL('/path/to/your_dll.dll')

# Define the return type of the function
dll.getEstimatedWaitingTime.restype = ctypes.c_int

# Call the function
estimated_waiting_time= dll.getEstimatedWaitingTime()
print(f"Estimated waiting time: {estimated_waiting_time}s")
```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("path_to_your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getEstimatedWaitingTime();

    static void Main()
    {
        int estimatedWaitingTime = getEstimatedWaitingTime();
        Console.WriteLine($"Estimated waiting time: {estimatedWaitingTime}s");
    }
}
```

**Java**

```java
import com.sun.jna.Library;
import com.sun.jna.Native;

public class Main {
    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = (MyLibrary) Native.load("name_of_your_dll", MyLibrary.class);
        int getEstimatedWaitingTime();
    }
    
    public static void main(String[] args) {
        int estimatedWaitingTime = MyLibrary.INSTANCE.getEstimatedWaitingTime();
        System.out.println("Estimated waiting time: " + estimatedWaitingTime + "s");
    }
}
```

**C++**

```c++
// main.cpp
#include <iostream>

extern "C" int getEstimatedWaitingTime();

int main() {
    int estimatedWaitingTime = getEstimatedWaitingTime();
    std::cout << "Estimated waiting time: " << estimatedWaitingTime << std::endl;

    return 0;
}
```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');

// Define the function signature
const myDll = ffi.Library('path_to_your_dll', {
  'getEstimatedWaitingTime': ['int', []],
});

// Call the function
const estimatedWaitingTime = myDll.getEstimatedWaitingTime();
console.log("Estimated waiting time:", estimatedWaitingTime);

```





### > Get Product Status <a id="getProductStatus"> </a>

The `getProductStatus` retrieves all menu details and its status. It is recommended for the Kiosk Client to continuously poll the product status for display. The recommended polling interval is 1 second.

#### Function Signature

```c++
const char* getProductStatusWrapped();
```

#### Parameters

This function does not require any input parameters.

#### Return Value

* `const char*`: The function returns a serialized JSON string. This string represents a list of products, with each product having its own set of attributes.

* **Data Format:** 

  > The returned JSON string will be structured as follows:
  >
  > ```json
  > {
  >  "products": [
  >  {
  >         "menuId": String,
  >         "priceUnit": String,
  >         "description": String,
  >         "description_kr": String,
  >          // other language descriptions with suffix according to available language
  >         "alias": String,
  >         "alias_kr": String,
  >          // other alias for each available language 
  >         "price": Double,
  >         "menuImage": String,
  >         "availability": Boolean,
  >      	"categoryId": String,
  >         "categoryName": String,
  >         "categoryName_kr": String,
  >         // other category name for each available language
  >         "isTumblerEnabled": Boolean,
  >         "options": [
  >             {
  >                 "optionCategoryId": String,
  >                 "viewIndex": UnsignedInt,
  >                 "optionId": String,
  >                 "alias": String,
  >                 "alias_kr": String,
  >                 // other alias for each available language
  >                 "optionCategoryName": String,
  >                 "optionCategoryName_kr": String,
  >                 // other option category name for each available language
  >                 "price": Double,
  >                 "isEnabled": Boolean,
  >                 "extraInformation": []
  >                     {...}
  >                 ]
  >             }
  >         ]
  >  },
  >  {...},
  >  ]
  > }
  > ```
  >
  > Here is an example of the product status data:
  >
  > ```json
  > {
  >  "products": [
  >  {
  >         "menuId": "d82e0a96-63b5-41ef-a2f4-87e303bbdf90",
  >         "priceUnit": "KRW",
  >         "description": "This is Cafe Latte",
  >         "description_kr": "카페라떼입니다",
  >          // ... (other languages)
  >         "alias": "Cafe Latte",
  >         "alias_kr": "카페라떼",
  >          // ... (other languages)
  >         "price": 4000.0,
  >         "menuImage": "path_to_menu_image",
  >         "availability": true,
  >      	"categoryId": "cec9aba0-4771-4bdd-909f-ca03f617c483",
  >         "categoryName": Coffee,
  >         "categoryName_kr": 커피류,
  >          // ... (other languages)
  >         "isTumblerEnabled": false,
  >         "options": [
  >             {
  >                 "optionCategoryId": "53ed0dc0-33d2-42c6-ab3c-7126e0060479",
  >                 "optionId": "313db0fa-9910-410b-ad90-99edeb754c50",
  >                 "viewIndex": 0,
  >                 "alias": "Add ice",
  >                 "alias_kr": "얼음 추가",
  >                 // ... (other languages)
  >                 "optionCategoryName": "Ice",
  >                 "optionCategoryName_kr": "얼음양 조절",
  >                 // ... (other languages)
  >                 "price": 1.99,
  >                 "isEnabled": true,
  >                 "extraInformation": [
  >                     {
  >                         "key": "cal",
  >                         "value": "89"
  >                     }
  >                 ]
  >             }
  >         ]
  >  },
  >  {...},
  >  ]
  > }
  > ```
  >
  > **Data Field Description:**
  >
  > `products`: a list of all products retrieved from barista booth.
  >
  > `menuId`: a unique menu identifier. Each menu has its own identifier. 
  >
  > `priceUnit`: the price unit set for this product.
  >
  > `description`: menu description in English (default).
  >
  > `description_kr`: menu description in Korean. **Note: you can get description in other languages using the `id` of the `AvailableLangue` struct from `getBoothConfig` (see <a href="#getBoothConfig">Get Booth Config</a>) as a suffix.** For example, if `es` (for Español) is one of the available language, you can add the id as a suffix like so, `description_es` to get the description in Spanish. 
  >
  > `alias`: menu name in English (default).
  >
  > `alias_kr`: menu name in Korean.
  >
  > `price`: price.
  >
  > `menuImage`: path to the menu image.
  >
  > `availability`: `true` if the menu is available for sale, `false ` otherwise.
  >
  > `categoryName`: category name in English.
  >
  > `categoryName_kr`: category name in Korean.
  >
  > `isTumblerEnabled`: `true` if user should be able to select takeout or tumbler for this product. If set to `true`, the Kiosk client should provide a option for the user to select either takeout or tumbler from the Kiosk display. See <a href="#tumblerSequenceDiagram">**Tumbler Sequence diagram**</a> from Sequences section. <span style="color:red">Please note that the `isTumblerEnabled` field simply signifies that the product can be served in a tumbler; it does not imply that the product is exclusively available for service in a tumbler.</span>
  >
  > `options`: available options from the menu:
  >
  > * `optionCategoryId`: the ID of the option category. Users should be able to choose only one option from this category.
  > * `optionId`: option ID
  > * `viewIndex`: view priority among the same option category. (e.g. if we have 2 options for "more ice" with `viewIndex` 1 and "less ice" with `viewIndex` 0, "less ice" option should appear before "more ice" option).
  > * `alias`: option title in English (default)
  > * `optionCategoryName`: option category name in English (default)
  > * `alias_kr`: option title in Korean
  > * `optionCategoryName_kr`: option category name in Korean
  > * `price`: option price
  > * `isEnabled`: `true` if the option is available. `false` otherwise.
  > * `extraInformation`: extra information set by franchise owner.





### > Place Order

TBD





### > Register Order Cancellation Callback

TBD





### > Place Order with Invoice<a id="placeOrderWithInvoice" style="color:black; text-decoration:none"> </a>

<span style="color:red">**Only used for JETCAFE Kiosk application**</span>

The `placeOrderWithInvoice` is a function that allows client applications to place an order with a list of products. 

#### Function Signature

```c++
OrderResult placeOrderWithInvoice(const Invoice& invoice, Product* products, int numberOfProducts);
```

#### Parameters

* Invoice structure which contains various order invoice data.

  > The `Invoice` struct should include:
  >
  > * `orderId` (`char[50]`): A string representing a unique order identifier. (`UUID4`).
  > * `orderLabel` (`char[50]`): A string representing a order label. This information will be used to display order label on the display of the barista booth so that the customer can identify their order from the pickup station. (e.g. "001", "002", "003", etc).
  > * `qrData` (`char[999]`): A string representing a unique QR data. When QR pickup option is enabled from the booth, the Kiosk should print this QR code on the receipt and add this QR data to `qrData` field. The customer will be able to scan this printed QR code with the scanner for beverage pickup.
  > * `invoiceNo` (`char[50]`): A string representing the invoice number.
  > * `refNo` (`char[50]`): A string representing the reference number.
  > * `purchase` (`double`): A double representing the actual transaction amount for the order.
  > * `authCode` (`char[24]`): A string representing the authentication code.
  > * `acqRefData` (`char[200]`): A string representing the acquirer reference data.
  > * `processData` (`char[999]`): A string representing the process data.
  > * `recordNo` (`char[256]`): Token data.
  > * `tranDeviceId` (`char[24]`): Transaction Device ID.
  > * `DateTime` (`char[50]`): Date time of order registration (`YYYY-MM-DD HH:MM:SS.sss`).

* Product which contains an array of Product structure. 

  > The `Product` struct should include:
  >
  > * `menuId` (`char[50]`): A string representing a unique menu identifier.
  > * `options` (`char**`): A pointer to array of option IDs (`char*`).
  > * `numberOfOptions` (`int`): Number of options added for this product. 

* `numberOfProducts` (`int`): Integer representing the number of products in order. 

#### Return Value

* `OrderResult` struct

  > `OrderResult` struct:
  >
  > ```c++
  > struct OrderResult {
  >        OrderRegistrationErrorCode errorCode;
  >        char boothName[256];
  > };
  > ```
  >
  > * `OrderRegistrationErrorCode` Enum representing order registration result. (See Error Code below).
  > * `boothName` to direct which booths the customer should go to (for multiple booths scenario). <span style="color:red">Do not need to consider for this version</span>

#### Error Code

The following shows the `OrderRegistrationErrorCode` and description.

```c++
enum OrderRegistrationErrorCode {
    Success = 0,
    OutOfStock,
    ExceedsMaximumAcceptableProductCount,
    Failed
};
```

* `Success`: represents successful order registration.
* `OutOfStock`: unsuccessful order placement because product is out of stock.
* `ExceedsMaximumAcceptableProductCount`: the received order contains more number of products than the maximum accepted product count in a single order (see `getBoothConfig` function).
* `Failed`: represents unsuccessful order registration due to internal error.

#### Note

* Tumbler option has a option ID as `uuid` of `d3b55e4a-cdc1-42d7-9983-22058474ec03`.  <a id="tumblerOptionUuid"> </a>

#### Example Usage

**Python3**

```python
import ctypes

class Invoice(ctypes.Structure):
    _fields_ = [
        ("orderId", ctypes.c_char * 50),
        ("orderLabel", ctypes.c_char * 50),
        ("qrData", ctypes.c_char * 999),
        ("invoiceNo", ctypes.c_char * 50),
        ("refNo", ctypes.c_char * 50),
        ("purchase", ctypes.c_double),
        ("authCode", ctypes.c_char * 24),
        ("acqRefData", ctypes.c_char * 200),
        ("processData", ctypes.c_char * 999),
        ("recordNo", ctypes.c_char * 256),
        ("tranDeviceId", ctypes.c_char * 24),
        ("DateTime", ctypes.c_char * 50),
        # Add other fields as necessary
    ]

class Product(ctypes.Structure):
    _fields_ = [("menuId", ctypes.c_char * 50)]

class OrderResult(ctypes.Structure):
    _fields_ = [
        ("errorCode", ctypes.c_int),
        ("boothName", ctypes.c_char * 256),
    ]

# Define function signature
dll = ctypes.CDLL('/path/to/your_dll.dll')
dll.placeOrderWithInvoice.argtypes = [ctypes.POINTER(Invoice), ctypes.POINTER(Product), ctypes.c_int]
dll.placeOrderWithInvoice.restype = OrderResult

# Usage
invoice = Invoice(orderId=b"a0f68b2d-04ff-4246-a002-2b6820153145", orderLabel=b"Label", qrData=b"QR Data", ...)  # Complete other fields as necessary
products = (Product * 2)(Product(menuId=b"53b053c3-442e-45db-ae6f-2c0e52db8228"), Product(menuId=b"another-menu-id"))
result = dll.placeOrderWithInvoice(ctypes.byref(invoice), products, 2)

if result.errorCode == 0: 
    print(f"Order successful. Proceed to booth: {result.boothName.decode('utf-8')}")
else:
    print(f"Order failed with error code: {result.errorCode}")

```

**C#**

```c#
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct OrderResult
{
    public int errorCode;  
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
    // Complete other fields
}

class Program
{
    [DllImport("your_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern OrderResult placeOrderWithInvoice(ref Invoice invoice, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Product[] products, int numberOfProducts);

    static void Main()
    {
        Invoice invoice = new Invoice 
        { 
            orderId = "a0f68b2d-04ff-4246-a002-2b6820153145",
            orderLabel = "Label",
            qrData = "QR Data",
            // Initialize other fields...
        };
        Product[] products = new Product[]
        {
            new Product { menuId = "53b053c3-442e-45db-ae6f-2c0e52db8228" },
            new Product { menuId = "another-menu-id" }
        };
        OrderResult result = placeOrderWithInvoice(ref invoice, products, products.Length);
        if (result.errorCode == 0) 
        {
            Console.WriteLine($"Order successful. Proceed to booth: {result.boothName}");
        }
        else
        {
            Console.WriteLine($"Order failed with error code: {result.errorCode}");
        }
    }
}

```

**Java**

```java
public class OrderResult extends Structure {
    public int errorCode;
    public byte[] boothName = new byte[256];
    
    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("errorCode", "boothName");
    }
}

public interface MyLibrary extends Library {
    MyLibrary INSTANCE = Native.load("your_dll", MyLibrary.class);
    
    OrderResult placeOrderWithInvoice(Invoice invoice, Product[] products, int numberOfProducts);
}

public class Invoice extends Structure {
    public byte[] orderId = new byte[50];
    // Add other fields similarly...
    
    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("orderId" /*, other field names */);
    }
}

public class Product extends Structure {
    public byte[] menuId = new byte[50];
    // Add other fields if needed...
    
    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("menuId");
    }
}

public class Main {
    public static void main(String[] args) {
        Invoice invoice = new Invoice();
        // Initialize invoice fields...
        
        Product[] products = (Product[]) new Product().toArray(2);
        products[0].menuId = "53b053c3-442e-45db-ae6f-2c0e52db8228".getBytes();
        products[1].menuId = "...".getBytes(); // Replace "..." with actual menu ID
        
        OrderResult result = MyLibrary.INSTANCE.placeOrderWithInvoice(invoice, products, products.length);
        
        if (result.errorCode == 0) { 
            System.out.println("Order successful. Proceed to booth: " + new String(result.boothName).trim());
        } else {
            System.out.println("Order failed with error code: " + result.errorCode);
        }
    }
}

```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');
const ref = require('ref-napi');
const Struct = require('ref-struct-di')(ref);

const OrderResult = Struct({
    'errorCode': 'int',
    'boothName': 'char[256]' 
});

const myDll = ffi.Library('path_to_your_dll', {
    'placeOrderWithInvoice': [OrderResult, [Invoice, ProductArray, 'int']]
});

const Invoice = Struct({
    'orderId': 'char[50]',
    // Add other fields...
});
const Product = Struct({
    'menuId': 'char[50]'
    // Add other fields if needed...
});

const invoice = new Invoice({ orderId: "a0f68b2d-04ff-4246-a002-2b6820153145" /*, other fields */ });
const products = new ProductArray(2);
products[0] = new Product({ menuId: "53b053c3-442e-45db-ae6f-2c0e52db8228" });
products[1] = new Product({ menuId: "another-menu-id" });

const result = myDll.placeOrderWithInvoice(invoice.ref(), products, products.length);

if (result.errorCode === 0) {
    console.log(`Order successful. Proceed to booth: ${ref.readCString(result.boothName, 0)}`);
} else {
    console.log(`Order failed with error code: ${result.errorCode}`);
}
```





### > Register Order Cancellation Callback

<span style="color:red">**Only used for JETCAFE Kiosk application**</span>

The `registerOrderCancellationCallback` function allows client applications to register a callback function for order cancellation events. This function enables the DLL to notify the client whenever an order is cancelled, providing the order ID as a parameter to the callback.

#### Function Signature

```c++
void registerInvoiceOrderCancellationCallback(OrderCancellationCallback callback);
```

#### Parameter

* **callback** (`OrderCancellationCallback`): A pointer to the client's callback function. This function should match the `OrderCancellationCallback` type. 

#### Callback Function Signature

```c++
int (*OrderCancellationCallback)(const OrderCancellationInfo* info);
```

The `OrderCancellationInfo` structure passed to the callback includes the following fields:

* `orderId` (`char[50]`): A string representing the unique order identifier. (`UUID4`).
* `invoiceNo` (`char[50]`): A string representing the invoice number.
* `refNo` (`char[50]`): A string representing the reference number.
* `purchase` (`double`): A double representing the amount to be refunded.
* `authCode` (`char[24]`): A string representing the authentication code.
* `acqRefData` (`char[200]`): A string representing the acquirer reference data.
* `processData` (`char[999]`): A string representing the process data.
* `recordNo` (`char[256]`): Token data.
* `tranDeviceId` (`char[24]`): Transaction Device ID.
* `DateTime` (`char[50]`): Date time of order registration (`YYYY-MM-DD HH:MM:SS.sss`).

#### Callback Function Return Value

* `int` value indicating order cancellation success/fail.

  > `0`: Success.
  >
  > `1`: Fail.

#### Example Usage

**Python3**

```python
import ctypes

class OrderCancellationInfo(ctypes.Structure):
    _fields_ = [("orderId", ctypes.c_char * 50),
                ("refNo", ctypes.c_char * 50),
                ("invoiceNo", ctypes.c_char * 50),
                ("authCode", ctypes.c_char * 24)]
                # ... other fields ...

OrderCancellationCallback = ctypes.CFUNCTYPE(None, ctypes.POINTER(OrderCancellationInfo))

dll = ctypes.CDLL('/path/to/your_dll.dll')
dll.registerInvoiceOrderCancellationCallback.argtypes = [OrderCancellationCallback]

@OrderCancellationCallback
def my_callback(info):
    info = info.contents
    print(f"Order ID: {info.orderId.decode('utf-8')}")
    print(f"Ref No: {info.refNo.decode('utf-8')}")
    # ... handle other fields ...

dll.registerInvoiceOrderCancellationCallback(my_callback)

```

**C#**

```c#
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct OrderCancellationInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string orderId;
    [MarshalAs(UnmanagedType.R8)]
    public double purchase;
    // ... other fields like refNo, invoiceNo, etc., with appropriate attributes ...
}

// Delegate that matches the C++ callback function signature
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void OrderCancellationCallback(ref OrderCancellationInfo info);

public class Program
{
    // Import the function from the DLL
    [DllImport("your_dll_name.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void registerInvoiceOrderCancellationCallback(OrderCancellationCallback callback);

    // Callback method
    private static int MyOrderCancellationCallback(ref OrderCancellationInfo info)
    {
        Console.WriteLine($"Order ID: {info.orderId}");
        // ... handle other fields ...
    }

    static void Main(string[] args)
    {
        // Register the callback
        registerInvoiceOrderCancellationCallback(MyOrderCancellationCallback);

        // ... rest of your code ...
    }
}

```

**Java**

```java
import com.sun.jna.*;
import com.sun.jna.ptr.*;

public class Main {
    public static class OrderCancellationInfo extends Structure {
        public static class ByReference extends OrderCancellationInfo implements Structure.ByReference {}

        public byte[] orderId = new byte[50];
        public byte[] refNo = new byte[50];
        public byte[] invoiceNo = new byte[50];
        public byte[] authCode = new byte[24];
        // ... other fields ...
    }

    public interface MyLibrary extends Library {
        MyLibrary INSTANCE = Native.load("your_dll", MyLibrary.class);
        void registerInvoiceOrderCancellationCallback(Callback callback);
    }

    public static class OrderCancellationCallback implements Callback {
        public int invoke(OrderCancellationInfo.ByReference info) {
            String orderId = Native.toString(info.orderId);
            String refNo = Native.toString(info.refNo);
            // ... handle other fields ...
            System.out.println("Order ID: " + orderId);
            // ...
        }
    }

    public static void main(String[] args) {
        MyLibrary.INSTANCE.registerInvoiceOrderCancellationCallback(new OrderCancellationCallback());
    }
}

```

**JavaScript (e.g. Electron App)**

**Install `ffi-napi`**

```bash
npm install ffi-napi
```

**Using DLL with `ffi-napi`**

```javascript
const ffi = require('ffi-napi');
const ref = require('ref-napi');
const Struct = require('ref-struct-di')(ref);

const OrderCancellationInfo = Struct({
  'orderId': 'string',
  'refNo': 'string',
  'invoiceNo': 'string',
  'authCode': 'string'
  // ... other fields ...
});
const OrderCancellationInfoPtr = ref.refType(OrderCancellationInfo);
const OrderCancellationCallback = ffi.Callback('int', [OrderCancellationInfoPtr], function(infoPtr) {
  let info = infoPtr.deref();
  console.log("Order ID:", info.orderId);
  // ... handle other fields ...
});

const myDll = ffi.Library('path_to_your_dll', {
  'registerInvoiceOrderCancellationCallback': ['int', [OrderCancellationCallback]]
});

myDll.registerInvoiceOrderCancellationCallback(OrderCancellationCallback);

```





## Sequences

### > Initial Sequences

This section shows the initial interaction between DLL client and the `FollettKioskDllApi`.

#### Initial Sequence Overview

This sequence diagram shows the initial interaction between client and DLL.

![initial_sequence_overview](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\initial_sequence_overview.jpg)

#### Initial Connection Sequence (Success)

This sequence diagram depicts the successful establishment of an initial connection.

<img src="C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\connection_sequence_success.jpg" alt="connection_sequence_success" style="width:700px"/>

#### Initial Connection Sequence (Failure)

This sequence diagram depicts the unsuccessful establishment of an initial connection.

![connection_sequence_failure](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\connection_sequence_failure.jpg)

#### Initial Callback Registration

This sequence diagram depicts the callback registration sequence after successful connection.

![initial_callback_registration](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\initial_callback_registration.jpg)





### > Product Status Polling Sequence

This section shows the sequence flow for product status polling after initialization. The Kiosk application should continuously poll product status to update available products. 

> :bulb:Note: The recommended polling interval is 1 second. 

<img src="C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\product_status_polling.jpg" alt="product_status_polling" style="width:700px"/>



### > Order Placement Sequence

This section shows the sequence flow for order placement. 

#### Successful Order Placement

This sequence diagram shows the successful order placement sequence.

![order_placement_sequence](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\order_placement_sequence.jpg)

> :bulb: Several points to note:
>
> * When the `FollettoKioskDllApi` receives the order request, it checks if all products in the order are currently available. If so, returns `0` (which represents `success` according to `placeOrderWithInvoice`).
> * It is **highly recommended** to perform the actual payment transaction only upon receiving this `success` response.

#### Unsuccessful Order Placement

This sequence diagram shows the unsuccessful order placement sequence. 

![unsuccessful_order_registration](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\unsuccessful_order_registration.jpg)

> :bulb: Several points to note:
>
> * When the one or all of the products in the order request is not available, the DLL returns an error code according to `placeOrderWithInvoice`. 
> * It is **highly recommended** to perform the actual payment only upon receiving the `success` response (not when other error code is received). 



### > Order Cancellation Sequence

This section shows the sequence flow for order cancellation. 

![order_cancellation_sequence](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\order_cancellation_sequence.jpg)

> * Order cancel request is received from external source (e.g. Franchise App).
> * The DLL sends order cancellation request to the client.
> * The client performs order cancellation and returns success or failure. 



### > Tumbler Sequence (Example)<a id="tumblerSequenceDiagram"> </a>

This section shows the sequence flow for tumbler order.

![tumbler_sequence](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\tumbler_sequence.jpg)

In this example:

> * The Kiosk client continuously poll product status when idle. 
> * When the customer uses the Kiosk to select a product which has `isTumblerEnabled` field set to `true`, the Kiosk displays various information about the allowed tumbler size or shape. 
> * If the customer then selects tumbler option (instead of takeout), order should be placed with tumbler option where the tumbler option uuid should be included in `Product`'s `options` array. See tumbler option uuid <a href="#tumblerOptionUuid">here</a>





## Testing with Barista Booth Simulator Module

### Installation

* Place `Booth Simulator Setup 1.0.0.exe` and `main` folder at `Desktop`. 

  > Note: `main` folder must be located at the Desktop directory as such:
  >
  > <img src="C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\desktop_capture.jpg" alt="desktop_capture" style="width:300px"/>

* Open `Booth Simulator Setup 1.0.0.exe` and install `Booth Simulator` program.

### How to Use

Set Kiosk IP address and port for the booth to communicate with Kiosk DLL. In real scenario, this will be set in the booth settings page. 

![kiosk_connection_setting_page](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\kiosk_connection_setting_page.jpg)

> 1: Kiosk IP and Port Inputs
>
> 2: Submit button to set the Kiosk IP and Port. When using the DLL, this port number should be same as the value passed to the parameter of `initializeServer` function. 

The following shows the main page after setting the Kiosk IP and Port

![main_page](C:\Users\user\Desktop\Documents\Folletto\FollettoKioskApi\diagrams\main_page.jpg)

> 1. Shows a section for booth configuration update testing. You can get the booth configuration using `getBoothConfig` function. Also, given that you successfully registered the booth configuration update callback using `registerBoothConfigUpdateCallback`, you can test the callback by clicking the "Apply" button. 
>
> 2. Shows a menu section. You can get the product details by using `getProductStatusWrapped` function.
>
> 3. Shows the order list section. You can add new order using `placeOrderWithInvoice` function. 
>
>    <span style="color:red">Note: if you can't see the new order entry, press `ctrl + r` to refresh the page.</span>
>    Given that you registered order cancellation callback function using `registerInvoiceOrderCancellationCallback`, you can test the order cancellation by clicking "Delete" button. 
>
> 4. Shows the IP and Port of the booth program. This information is required when using `connectToBooths` function (which takes IP and Port of the booths).





## Appendix

### Version Log

#### v0.0.8

**What's new:**

* Changed the return type of `getBoothConfig` to pointer.
* Bug fixed for option registration when placing order.



#### v0.0.7

**What's new:**

* Converted download logic to threads.



#### v0.0.6

**What's new:**

* Added image download functionality.



#### v0.0.5

**What's new:**

* Added `isTumblerEnabled` to product fields in `getProductStatus` function. Also added sequence diagrams to illustrate possible tumbler option interaction sequence between booth and the kiosk client.
* Added tumbler option `uuid` in <a href="#placeOrderWithInvoice">Place Order with Invoice</a> section.
* Added `viewIndex` to product option field for option rendering.
* Added available languages to `getBoothConfig` and language fields in `getProductStatus`. See <a href="#getBoothConfig">Get Booth Configuration</a> and <a href="#getProductStatus">Get Product Status</a>.

**Documentation Update:**

* Added <a href="#sampleData">Sample Data</a> section in the Appendix.



#### v0.0.4

**What's new:**

* Added new fields for options in `getProductStatusWrapped` and `placeOrderWithInvoice` function.



#### v0.0.3

**What's new:**

* Added `getDllVersion` to get DLL version.

* Converted data types to accommodate one kiosk, multiple booths scenario. 

  > **Affected:**
  >
  > * **`connect` function:** changed input parameter to an array of struct to connect to multiple booths.
  > * **`getBoothConfig` function:** removed `boothId` field from the return value.
  > * **`placeOrderWithInvoice` function:** changed return value to struct.



#### v0.0.2

**What's new:**

* Added `categoryNameKr` and `categoryNameEn` in `getProductStatusWrapped` (Get Product Status) return value.
* Added `boothId` in `getBoothConfig` (Get Booth Configuration) return value. 





### Sample Data <a id="sampleData"> </a>

This section shows some of the sample data (return data) from the booth (currently based on Jet Cafe robot barista booth).

#### Get DLL version

The following shows the sample output returned from the DLL using <a href="#getDllVersion">Get Version</a> function.

```shel
0.0.5
```



#### Get Booth Configuration

The following shows the sample output returned from the Robot barista booth by using <a href="#getBoothConfig">Get Booth Config</a> function.

```c++
BoothConfig* -> POINTER to BoothConfig
```

where the returned sample data is as such:

```c++
BoothConfig.qrRequired -> true (or false if set to false)
BoothConfig.maxProductsInGroup -> 3 (currently set to 3 due to hardware limitation)
BoothConfig.languages -> 
    struct AvailableLanguage
    {
     char id[3];             // "en", "kr", "es"
     char display[128]; 	 // "English", "한국어", "Español" 
    };
```



#### Get Product Status Sample Data

The following shows the sample product data returned from Jet Cafe robot barista booth by using <a href="#getProductStatus">Get Product Status</a> function.

```json
{
    "products": [
        {
            "menuId": "fb96e8fc-4103-40aa-8a4a-98c1353d4c69",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Americano",
            "alias_kr": "핫 아메리카노",
            "alias_es": "Hot Americano(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "cec9aba0-4771-4bdd-909f-ca03f617c483",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": true,
            "options": [
                {
                    "optionCategoryId": "8f0f6a90-3707-48af-9a33-caec64ecf843",
                    "viewIndex": 0,
                    "optionId": "d3b55e4a-cdc1-42d7-9983-22058474ec03",
                    "alias": "Use Tumbler",
                    "alias_kr": "텀블러 사용",
                    "alias_es": "Use Tumbler (ES)",
                    "optionCategoryName": "Takeout option",
                    "optionCategoryName_kr": "테이크 아웃 옵션",
                    "optionCategoryName_es": "Takeout option (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                },
                {
                    "optionCategoryId": "9e0ec2c7-1965-4bbb-92c8-795fe7f8cfab",
                    "viewIndex": 0,
                    "optionId": "cb513f50-0510-4ee6-a758-67c78572b7e3",
                    "alias": "Light",
                    "alias_kr": "연한맛",
                    "alias_es": "Light (ES)",
                    "optionCategoryName": "Strength",
                    "optionCategoryName_kr": "커피 맛",
                    "optionCategoryName_es": "Strength (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                },
                {
                    "optionCategoryId": "9e0ec2c7-1965-4bbb-92c8-795fe7f8cfab",
                    "viewIndex": 1,
                    "optionId": "723c6250-06c4-4d12-a283-625997c1784f",
                    "alias": "Deep",
                    "alias_kr": "진한맛",
                    "alias_es": "Deep (ES)",
                    "optionCategoryName": "Strength",
                    "optionCategoryName_kr": "커피 맛",
                    "optionCategoryName_es": "Strength (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                }
            ]
        },
        {
            "menuId": "7c4d66ee-b966-44d8-9e5f-8245416c724e",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Americano",
            "alias_kr": "아이스 아메리카노",
            "alias_es": "Iced Americano(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "cec9aba0-4771-4bdd-909f-ca03f617c483",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
                {
                    "optionCategoryId": "9e0ec2c7-1965-4bbb-92c8-795fe7f8cfab",
                    "viewIndex": 0,
                    "optionId": "cb513f50-0510-4ee6-a758-67c78572b7e3",
                    "alias": "Light",
                    "alias_kr": "연한맛",
                    "alias_es": "Light (ES)",
                    "optionCategoryName": "Strength",
                    "optionCategoryName_kr": "커피 맛",
                    "optionCategoryName_es": "Strength (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                },
                {
                    "optionCategoryId": "9e0ec2c7-1965-4bbb-92c8-795fe7f8cfab",
                    "viewIndex": 1,
                    "optionId": "723c6250-06c4-4d12-a283-625997c1784f",
                    "alias": "Deep",
                    "alias_kr": "진한맛",
                    "alias_es": "Deep (ES)",
                    "optionCategoryName": "Strength",
                    "optionCategoryName_kr": "커피 맛",
                    "optionCategoryName_es": "Strength (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                }
            ]
        },
        {
            "menuId": "890696f8-5e09-4281-ae0e-54f089d032c7",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Cafe Latte",
            "alias_kr": "핫 카페라떼",
            "alias_es": "Hot Cafe Latte (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": true,
            "options": [
                {
                    "optionCategoryId": "8f0f6a90-3707-48af-9a33-caec64ecf843",
                    "viewIndex": 0,
                    "optionId": "d3b55e4a-cdc1-42d7-9983-22058474ec03",
                    "alias": "Use Tumbler",
                    "alias_kr": "텀블러 사용",
                    "alias_es": "Use Tumbler (ES)",
                    "optionCategoryName": "Takeout option",
                    "optionCategoryName_kr": "테이크 아웃 옵션",
                    "optionCategoryName_es": "Takeout option (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                }
            ]
        },
        {
            "menuId": "dba481fd-9e0f-4436-b5a3-4a1c7aa4ba9b",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Cafe Latte",
            "alias_kr": "아이스 카페라떼",
            "alias_es": "Iced Cafe Latte(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "3dfd7513-b60d-4c72-b38c-babd8af27ff6",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Cappuccino",
            "alias_kr": "핫 카푸치노",
            "alias_es": "Hot Cappuccino (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": true,
            "options": [
                {
                    "optionCategoryId": "8f0f6a90-3707-48af-9a33-caec64ecf843",
                    "viewIndex": 0,
                    "optionId": "d3b55e4a-cdc1-42d7-9983-22058474ec03",
                    "alias": "Use Tumbler",
                    "alias_kr": "텀블러 사용",
                    "alias_es": "Use Tumbler (ES)",
                    "optionCategoryName": "Takeout option",
                    "optionCategoryName_kr": "테이크 아웃 옵션",
                    "optionCategoryName_es": "Takeout option (ES)",
                    "price": 0.00,
                    "isEnabled": true,
                    "extraInformation": []
                }
            ]
        },
        {
            "menuId": "efb299a8-4aed-4130-89a2-38a0c3236817",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Cappuccino",
            "alias_kr": "아이스 카푸치노",
            "alias_es": "Iced Cappuccino(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "890f6f57-51a6-415a-b2de-9e1a505dafa8",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Cafe Mocha",
            "alias_kr": "핫 카페 모카",
            "alias_es": "Hot Cafe Mocha (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [

            ]
        },
        {
            "menuId": "b9dca819-00f6-41b7-a9d7-098552ff01ec",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Cafe Mocha",
            "alias_kr": "아이스 카페모카",
            "alias_es": "Iced Cafe Mocha(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "10c4ea1e-fc3d-4522-b318-72ea227036c1",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Vanila Latte",
            "alias_kr": "핫 바닐라 라떼",
            "alias_es": "Hot Vanila Latte (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [

            ]
        },
        {
            "menuId": "8de7c76b-4a85-4e6f-a48d-7c3c06cc5a05",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Vanila Latte",
            "alias_kr": "아이스 바닐라 라떼",
            "alias_es": "Iced Vanila Latte (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "b7875d51-a9c8-4171-a789-15851a39774e",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Caramel Macchiato",
            "alias_kr": "핫 캬라멜 마끼야또",
            "alias_es": "Hot Caramel Macchiato (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Hot)",
            "categoryName_kr": "핫 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [

            ]
        },
        {
            "menuId": "30cf3d4a-a35e-4a1a-918c-499c4d11c643",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Caramel Macchiato",
            "alias_kr": "아이스 캬라멜 마끼야또",
            "alias_es": "Iced Caramel Macchiato (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Coffee (Iced)",
            "categoryName_kr": "아이스 커피류",
            "categoryName_es": "Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "f89f6435-32c4-429d-ab6f-46c41e2cd6cd",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Chocolate Milk",
            "alias_kr": "핫 초코 우유",
            "alias_es": "Hot Chocolate Milk (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Non-Coffee (Hot)",
            "categoryName_kr": "핫 음료류",
            "categoryName_es": "Non-Coffee(es)",
            "isTumblerEnabled": false,
            "options": [

            ]
        },
        {
            "menuId": "b93862e8-d3de-4cd2-99ae-9178bfe470b9",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Chocolate Milk",
            "alias_kr": "아이스 초코 우유",
            "alias_es": "Iced Chocolate Milk (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Non-Coffee (Iced)",
            "categoryName_kr": "아이스 음료류",
            "categoryName_es": "Non-Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "0475408f-79d6-4581-a308-f1353e9528ee",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Hot Caramel Milk",
            "alias_kr": "핫 캬라멜 우유",
            "alias_es": "Hot Caramel Milk (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Non-Coffee (Hot)",
            "categoryName_kr": "핫 음료류",
            "categoryName_es": "Non-Coffee(es)",
            "isTumblerEnabled": false,
            "options": [

            ]
        },
        {
            "menuId": "8e34796c-a4fe-4529-a77a-8be43f117030",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Caramel Milk",
            "alias_kr": "아이스 캬라멜 우유",
            "alias_es": "Iced Caramel Milk (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "aac47eb7-65a5-46fa-b1a2-9f414c1ec754",
            "categoryName": "Non-Coffee (Iced)",
            "categoryName_kr": "아이스 음료류",
            "categoryName_es": "Non-Coffee(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "6d4c71a3-46c1-478e-9b9f-24a56a337530",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Iced Grapefruit Ade",
            "alias_kr": "자몽 에이드",
            "alias_es": "Iced Grapefruit Ade (es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "52429859-858e-4e0c-8b7f-9432681fa726",
            "categoryName": "Ade",
            "categoryName_kr": "에이드류",
            "categoryName_es": "Ade(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "64f7a4fa-5229-4538-9a1d-de9fe43db73f",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Lemonade",
            "alias_kr": "레몬 에이드",
            "alias_es": "Lemonade(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "52429859-858e-4e0c-8b7f-9432681fa726",
            "categoryName": "Ade",
            "categoryName_kr": "에이드류",
            "categoryName_es": "Ade(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        },
        {
            "menuId": "64f7a4fa-5229-4538-9a1d-de9fe43db73f",
            "priceUnit": "USD",
            "description": "Description in English",
            "description_kr": "한국어 설명",
            "description_es": "Descripción en español",
            "alias": "Green Grape Ade",
            "alias_kr": "청포도 에이드",
            "alias_es": "Green Grape Ade(es)",
            "price": 3.10,
            "menuImage": "absolute_path_to_image",
            "availability": true,
            "categoryId": "52429859-858e-4e0c-8b7f-9432681fa726",
            "categoryName": "Ade",
            "categoryName_kr": "에이드류",
            "categoryName_es": "Ade(es)",
            "isTumblerEnabled": false,
            "options": [
            ]
        }
    ]
}
```















