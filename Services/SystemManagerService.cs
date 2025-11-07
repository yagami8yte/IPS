using System;
using System.Collections.Generic;
using System.Linq;
using IPS.Core.Interfaces;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Manages all unmanned system adapters (Coffee, Food, etc.)
    /// Provides centralized access to all connected systems
    /// </summary>
    public class SystemManagerService
    {
        private readonly Dictionary<string, IUnmannedSystem> _systems = new();

        /// <summary>
        /// Gets all registered system names
        /// </summary>
        public IReadOnlyList<string> SystemNames => _systems.Keys.ToList();

        /// <summary>
        /// Registers an unmanned system adapter
        /// </summary>
        public void RegisterSystem(IUnmannedSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (string.IsNullOrEmpty(system.SystemName))
                throw new ArgumentException("System name cannot be null or empty");

            if (_systems.ContainsKey(system.SystemName))
                throw new InvalidOperationException($"System '{system.SystemName}' is already registered");

            _systems[system.SystemName] = system;
        }

        /// <summary>
        /// Clears all registered systems
        /// </summary>
        public void ClearSystems()
        {
            // Dispose systems if they implement IDisposable
            foreach (var system in _systems.Values)
            {
                if (system is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _systems.Clear();
        }

        /// <summary>
        /// Gets an adapter by system name
        /// </summary>
        public IUnmannedSystem? GetSystem(string systemName)
        {
            return _systems.TryGetValue(systemName, out var system) ? system : null;
        }

        /// <summary>
        /// Gets menu items from a specific system
        /// </summary>
        public List<MenuItem> GetMenuItems(string systemName)
        {
            var system = GetSystem(systemName);
            if (system == null)
            {
                return new List<MenuItem>();
            }

            return system.GetMenuItems();
        }

        /// <summary>
        /// Gets menu items from all registered systems
        /// </summary>
        public Dictionary<string, List<MenuItem>> GetAllMenuItems()
        {
            var result = new Dictionary<string, List<MenuItem>>();

            foreach (var systemName in SystemNames)
            {
                result[systemName] = GetMenuItems(systemName);
            }

            return result;
        }

        /// <summary>
        /// Gets status from a specific system
        /// </summary>
        public SystemStatus? GetSystemStatus(string systemName)
        {
            var system = GetSystem(systemName);
            return system?.GetStatus();
        }

        /// <summary>
        /// Gets status from all registered systems
        /// </summary>
        public Dictionary<string, SystemStatus> GetAllSystemStatuses()
        {
            var result = new Dictionary<string, SystemStatus>();

            foreach (var systemName in SystemNames)
            {
                var status = GetSystemStatus(systemName);
                if (status != null)
                {
                    result[systemName] = status;
                }
            }

            return result;
        }

        /// <summary>
        /// Sends an order to the appropriate system
        /// </summary>
        public bool SendOrder(string systemName, OrderInfo order)
        {
            var system = GetSystem(systemName);
            if (system == null)
            {
                return false;
            }

            return system.SendOrder(order);
        }

        /// <summary>
        /// Sends a multi-system order (splits and sends to each system)
        /// </summary>
        public Dictionary<string, bool> SendMultiSystemOrder(OrderInfo order)
        {
            var results = new Dictionary<string, bool>();

            // Split order by system
            var systemOrders = OrderHelpers.SplitOrderBySystem(order);

            // Send to each system
            foreach (var (systemName, systemOrder) in systemOrders)
            {
                bool success = SendOrder(systemName, systemOrder);
                results[systemName] = success;
            }

            return results;
        }
    }
}
