using System;
using System.Collections.Generic;
using System.Windows.Threading;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Background service that continuously polls menu items and system status
    /// Recommended polling interval: 5 seconds
    /// </summary>
    public class SystemPollingService
    {
        private readonly SystemManagerService _systemManager;
        private readonly DispatcherTimer _pollingTimer;
        private bool _isPolling;
        private Dictionary<string, List<MenuItem>>? _lastMenuItems;
        private Dictionary<string, SystemStatus>? _lastStatuses;

        /// <summary>
        /// Event raised when menu items are updated from any system
        /// </summary>
        public event EventHandler<MenuItemsUpdatedEventArgs>? MenuItemsUpdated;

        /// <summary>
        /// Event raised when system status is updated
        /// </summary>
        public event EventHandler<SystemStatusUpdatedEventArgs>? SystemStatusUpdated;

        /// <summary>
        /// Polling interval in milliseconds (default: 5000ms = 5 seconds)
        /// </summary>
        public int PollingIntervalMs { get; set; } = 5000;

        public SystemPollingService(SystemManagerService systemManager)
        {
            _systemManager = systemManager ?? throw new ArgumentNullException(nameof(systemManager));

            // Use DispatcherTimer for UI thread compatibility
            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PollingIntervalMs)
            };
            _pollingTimer.Tick += OnPollingTick;
        }

        /// <summary>
        /// Starts background polling
        /// </summary>
        public void StartPolling()
        {
            if (_isPolling)
                return;

            _isPolling = true;
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(PollingIntervalMs);

            // Poll immediately on start (but async via Dispatcher)
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PollSystems();
            }));

            _pollingTimer.Start();
        }

        /// <summary>
        /// Stops background polling
        /// </summary>
        public void StopPolling()
        {
            if (!_isPolling)
                return;

            _isPolling = false;
            _pollingTimer.Stop();
        }

        /// <summary>
        /// Gets whether polling is currently active
        /// </summary>
        public bool IsPolling => _isPolling;

        private void OnPollingTick(object? sender, EventArgs e)
        {
            PollSystems();
        }

        private void PollSystems()
        {
            try
            {
                // Poll menu items from all systems
                var allMenuItems = _systemManager.GetAllMenuItems();

                // Only fire event if data changed
                if (HasMenuItemsChanged(allMenuItems))
                {
                    _lastMenuItems = allMenuItems;
                    MenuItemsUpdated?.Invoke(this, new MenuItemsUpdatedEventArgs(allMenuItems));
                }

                // Poll system statuses
                var allStatuses = _systemManager.GetAllSystemStatuses();

                // Only fire event if status changed
                if (HasStatusChanged(allStatuses))
                {
                    _lastStatuses = allStatuses;
                    SystemStatusUpdated?.Invoke(this, new SystemStatusUpdatedEventArgs(allStatuses));
                }
            }
            catch (Exception ex)
            {
                // Log error but continue polling
                Console.WriteLine($"[SystemPollingService] Error during polling: {ex.Message}");
            }
        }

        private bool HasMenuItemsChanged(Dictionary<string, List<MenuItem>> newMenuItems)
        {
            // First poll always fires
            if (_lastMenuItems == null)
                return true;

            // Check if system count changed
            if (_lastMenuItems.Count != newMenuItems.Count)
                return true;

            // Check each system's menu items
            foreach (var kvp in newMenuItems)
            {
                if (!_lastMenuItems.ContainsKey(kvp.Key))
                    return true;

                var oldItems = _lastMenuItems[kvp.Key];
                var newItems = kvp.Value;

                // Check if item count changed
                if (oldItems.Count != newItems.Count)
                    return true;

                // Check if availability or other properties changed
                for (int i = 0; i < oldItems.Count && i < newItems.Count; i++)
                {
                    if (oldItems[i].MenuId != newItems[i].MenuId ||
                        oldItems[i].IsAvailable != newItems[i].IsAvailable ||
                        oldItems[i].Price != newItems[i].Price ||
                        oldItems[i].Name != newItems[i].Name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasStatusChanged(Dictionary<string, SystemStatus> newStatuses)
        {
            // First poll always fires
            if (_lastStatuses == null)
                return true;

            // Check if system count changed
            if (_lastStatuses.Count != newStatuses.Count)
                return true;

            // Check each system's status
            foreach (var kvp in newStatuses)
            {
                if (!_lastStatuses.ContainsKey(kvp.Key))
                    return true;

                var oldStatus = _lastStatuses[kvp.Key];
                var newStatus = kvp.Value;

                if (oldStatus.IsOnline != newStatus.IsOnline ||
                    oldStatus.IsAvailable != newStatus.IsAvailable ||
                    oldStatus.ErrorMessage != newStatus.ErrorMessage ||
                    oldStatus.WaitingOrdersCount != newStatus.WaitingOrdersCount)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Forces an immediate poll (useful for manual refresh)
        /// </summary>
        public void PollNow()
        {
            PollSystems();
        }
    }

    /// <summary>
    /// Event args for menu items updates
    /// </summary>
    public class MenuItemsUpdatedEventArgs : EventArgs
    {
        public Dictionary<string, List<MenuItem>> MenuItemsBySystem { get; }

        public MenuItemsUpdatedEventArgs(Dictionary<string, List<MenuItem>> menuItemsBySystem)
        {
            MenuItemsBySystem = menuItemsBySystem;
        }
    }

    /// <summary>
    /// Event args for system status updates
    /// </summary>
    public class SystemStatusUpdatedEventArgs : EventArgs
    {
        public Dictionary<string, SystemStatus> StatusBySystem { get; }

        public SystemStatusUpdatedEventArgs(Dictionary<string, SystemStatus> statusBySystem)
        {
            StatusBySystem = statusBySystem;
        }
    }
}
