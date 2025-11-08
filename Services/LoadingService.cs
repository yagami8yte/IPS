using System;
using System.ComponentModel;

namespace IPS.Services
{
    /// <summary>
    /// Global service for managing loading state across the application
    /// Singleton pattern ensures consistent state
    /// </summary>
    public class LoadingService : INotifyPropertyChanged
    {
        private static LoadingService? _instance;
        private static readonly object _lock = new object();

        private bool _isLoading = false;
        private string _statusMessage = "Loading...";
        private int _progress = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Singleton instance of LoadingService
        /// </summary>
        public static LoadingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoadingService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Whether loading overlay is currently visible
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// Status message to display during loading
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Progress
        {
            get => _progress;
            private set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private LoadingService()
        {
        }

        /// <summary>
        /// Show loading overlay with message
        /// </summary>
        public void Show(string message = "Loading...", int progress = 0)
        {
            StatusMessage = message;
            Progress = progress;
            IsLoading = true;
            Console.WriteLine($"[LoadingService] Loading shown: {message}");
        }

        /// <summary>
        /// Hide loading overlay
        /// </summary>
        public void Hide()
        {
            IsLoading = false;
            Console.WriteLine("[LoadingService] Loading hidden");
        }

        /// <summary>
        /// Update loading progress and message
        /// </summary>
        public void UpdateProgress(string message, int progress)
        {
            StatusMessage = message;
            Progress = progress;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
