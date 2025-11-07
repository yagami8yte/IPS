using System;
using System.Collections.Generic;

namespace IPS.Core.Models
{
    /// <summary>
    /// Configuration for an unmanned system (IP address and port)
    /// </summary>
    public class SystemConfiguration
    {
        /// <summary>
        /// System identifier (e.g., "Coffee", "Food")
        /// </summary>
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the unmanned system
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Whether this system is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Application-wide configuration settings
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// List of configured unmanned systems
        /// </summary>
        public List<SystemConfiguration> Systems { get; set; } = new();

        /// <summary>
        /// Port for the DLL's internal HTTP server
        /// </summary>
        public int DllServerPort { get; set; } = 5000;
    }
}
