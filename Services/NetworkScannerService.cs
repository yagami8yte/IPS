using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;

namespace IPS.Services
{
    /// <summary>
    /// Service for scanning network to discover unmanned system devices
    /// </summary>
    public class NetworkScannerService
    {
        /// <summary>
        /// Discovered device information
        /// </summary>
        public class DiscoveredDevice
        {
            public string IpAddress { get; set; } = "";
            public int Port { get; set; }
            public string DeviceType { get; set; } = "Unknown";
            public bool IsResponding { get; set; }
            public string DeviceName { get; set; } = "";
        }

        /// <summary>
        /// Get local IP address of this PC
        /// </summary>
        public string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkScanner] Failed to get local IP: {ex.Message}");
            }
            return "127.0.0.1";
        }

        /// <summary>
        /// Get subnet range based on local IP (e.g., 192.168.1.x)
        /// </summary>
        public string GetSubnet()
        {
            string localIp = GetLocalIPAddress();
            var parts = localIp.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return "192.168.1";
        }

        /// <summary>
        /// Scan network for devices on common ports used by unmanned systems
        /// </summary>
        public async Task<List<DiscoveredDevice>> ScanNetworkAsync(
            int startRange = 1,
            int endRange = 254,
            int[] portsToScan = null,
            IProgress<int> progress = null)
        {
            if (portsToScan == null)
            {
                // Common ports for unmanned systems
                portsToScan = new[] { 5000, 5001, 8080, 8081, 9000 };
            }

            var devices = new List<DiscoveredDevice>();
            string subnet = GetSubnet();

            Console.WriteLine($"[NetworkScanner] Scanning subnet {subnet}.x");
            Console.WriteLine($"[NetworkScanner] Range: {startRange}-{endRange}");
            Console.WriteLine($"[NetworkScanner] Ports: {string.Join(", ", portsToScan)}");

            var tasks = new List<Task>();
            object lockObj = new object();
            int completed = 0;
            int total = (endRange - startRange + 1) * portsToScan.Length;

            for (int i = startRange; i <= endRange; i++)
            {
                string ip = $"{subnet}.{i}";

                foreach (int port in portsToScan)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var device = await PingAndCheckPortAsync(ip, port);
                        if (device != null && device.IsResponding)
                        {
                            lock (lockObj)
                            {
                                devices.Add(device);
                                Console.WriteLine($"[NetworkScanner] ✓ Found device at {ip}:{port}");
                            }
                        }

                        lock (lockObj)
                        {
                            completed++;
                            progress?.Report((completed * 100) / total);
                        }
                    }));
                }
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"[NetworkScanner] Scan complete. Found {devices.Count} device(s)");
            return devices;
        }

        /// <summary>
        /// Ping an IP and check if specific port is open
        /// </summary>
        private async Task<DiscoveredDevice> PingAndCheckPortAsync(string ipAddress, int port)
        {
            var device = new DiscoveredDevice
            {
                IpAddress = ipAddress,
                Port = port,
                IsResponding = false
            };

            try
            {
                // Quick ping check first (timeout 100ms)
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ipAddress, 100);
                    if (reply.Status != IPStatus.Success)
                    {
                        return device; // Not responding to ping
                    }
                }

                // Check if port is open (timeout 500ms)
                using (var tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(ipAddress, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(500)) == connectTask && tcpClient.Connected)
                    {
                        device.IsResponding = true;
                        device.DeviceType = IdentifyDeviceType(port);
                        device.DeviceName = $"{device.DeviceType} ({ipAddress}:{port})";
                    }
                }
            }
            catch
            {
                // Device not responding, return with IsResponding = false
            }

            return device;
        }

        /// <summary>
        /// Identify device type based on port (heuristic)
        /// </summary>
        private string IdentifyDeviceType(int port)
        {
            return port switch
            {
                5000 => "Folletto Booth",
                5001 => "Secondary Booth",
                8080 => "HTTP Device",
                8081 => "HTTP Device",
                9000 => "Generic Device",
                _ => "Unknown Device"
            };
        }

        /// <summary>
        /// Test connection to specific device
        /// </summary>
        public async Task<bool> TestConnectionAsync(string ipAddress, int port, int timeoutMs = 2000)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(ipAddress, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) == connectTask && tcpClient.Connected)
                    {
                        Console.WriteLine($"[NetworkScanner] ✓ Connection test successful: {ipAddress}:{port}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkScanner] ✗ Connection test failed: {ipAddress}:{port} - {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Quick scan of localhost common ports
        /// </summary>
        public async Task<List<DiscoveredDevice>> ScanLocalhostAsync()
        {
            Console.WriteLine("[NetworkScanner] Scanning localhost...");
            var devices = new List<DiscoveredDevice>();
            int[] commonPorts = { 5000, 5001, 6000, 8080, 8081, 9000 };

            foreach (int port in commonPorts)
            {
                var device = await PingAndCheckPortAsync("127.0.0.1", port);
                if (device != null && device.IsResponding)
                {
                    devices.Add(device);
                    Console.WriteLine($"[NetworkScanner] ✓ Localhost service found on port {port}");
                }
            }

            return devices;
        }
    }
}
