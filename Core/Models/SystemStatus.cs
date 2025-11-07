using System;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents the current operational status of an unmanned kiosk system
    /// </summary>
    public class SystemStatus
    {
        /// <summary>
        /// Whether the system is currently online and connected
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Whether the system is available to accept new orders
        /// A system can be online but unavailable (e.g., maintenance mode, out of ingredients)
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Current number of orders waiting to be fulfilled
        /// </summary>
        public int WaitingOrdersCount { get; set; }

        /// <summary>
        /// Estimated time in seconds until a new order would be ready
        /// Note: This is an estimate and may change based on system conditions
        /// </summary>
        public int EstimatedWaitingTimeSeconds { get; set; }

        /// <summary>
        /// Timestamp when this status was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Error message if the system is in an error state
        /// Null or empty if no errors
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional status information specific to the system
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }
}
