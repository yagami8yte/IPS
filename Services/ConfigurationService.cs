using System;
using System.IO;
using System.Text.Json;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Service for loading and saving application configuration
    /// </summary>
    public class ConfigurationService
    {
        private const string ConfigFileName = "appsettings.json";
        private readonly string _configFilePath;
        private AppConfiguration _configuration;

        public ConfigurationService()
        {
            // Store config in the same directory as the executable
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(appDirectory, ConfigFileName);

            _configuration = LoadConfiguration();
        }

        /// <summary>
        /// Get current application configuration
        /// </summary>
        public AppConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Save updated configuration to file
        /// </summary>
        public bool SaveConfiguration(AppConfiguration configuration)
        {
            try
            {
                _configuration = configuration;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(configuration, options);
                File.WriteAllText(_configFilePath, json);

                Console.WriteLine($"[ConfigurationService] Configuration saved to {_configFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigurationService] Failed to save configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load configuration from file, or create default if not exists
        /// </summary>
        private AppConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var config = JsonSerializer.Deserialize<AppConfiguration>(json, options);
                    if (config != null)
                    {
                        // If password hash is empty (migration from old config), set default
                        if (string.IsNullOrWhiteSpace(config.AdminPasswordHash))
                        {
                            var passwordService = new PasswordService();
                            config.AdminPasswordHash = passwordService.GetDefaultPasswordHash();
                            Console.WriteLine("[ConfigurationService] No password hash found, setting default PIN '0000'");
                            // Save the updated config
                            SaveConfiguration(config);
                        }

                        Console.WriteLine($"[ConfigurationService] Configuration loaded from {_configFilePath}");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigurationService] Failed to load configuration: {ex.Message}");
            }

            // Return default configuration
            Console.WriteLine("[ConfigurationService] Using default configuration");
            return CreateDefaultConfiguration();
        }

        /// <summary>
        /// Create default configuration with example coffee system
        /// </summary>
        private AppConfiguration CreateDefaultConfiguration()
        {
            var passwordService = new PasswordService();

            return new AppConfiguration
            {
                DllServerPort = 6000,  // DLL internal server port (different from booth port)
                AdminPasswordHash = passwordService.GetDefaultPasswordHash(),  // Default PIN: "0000"
                Systems = new()
                {
                    new SystemConfiguration
                    {
                        SystemName = "Coffee",
                        IpAddress = "127.0.0.1",  // Booth IP address
                        Port = 5000,  // Booth port
                        IsEnabled = true
                    }
                }
            };
        }
    }
}
