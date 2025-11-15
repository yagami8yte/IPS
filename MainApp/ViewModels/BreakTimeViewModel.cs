using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for break time screen
    /// Displays when the kiosk is on break
    /// </summary>
    public class BreakTimeViewModel : BaseViewModel
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly Action? _onOpenAdmin;
        private string _currentDate = string.Empty;
        private string _currentTime = string.Empty;
        private string _breakMessage = string.Empty;
        private string _breakEndTime = string.Empty;
        private bool _isInstantBreak = false;

        /// <summary>
        /// Current date for display
        /// </summary>
        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Current time for display
        /// </summary>
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Custom break message from admin
        /// </summary>
        public string BreakMessage
        {
            get => _breakMessage;
            set
            {
                _breakMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Break end time display (e.g., "3:00 PM")
        /// </summary>
        public string BreakEndTime
        {
            get => _breakEndTime;
            set
            {
                _breakEndTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether to show custom message (true) or default message (false)
        /// </summary>
        public bool HasCustomMessage => !string.IsNullOrWhiteSpace(BreakMessage);

        /// <summary>
        /// Whether this is an instant break (manual) vs scheduled break
        /// </summary>
        public bool IsInstantBreak
        {
            get => _isInstantBreak;
            set
            {
                _isInstantBreak = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether to show scheduled end time (only for scheduled breaks)
        /// </summary>
        public bool ShowScheduledEndTime => !IsInstantBreak;

        /// <summary>
        /// Command to open admin panel
        /// </summary>
        public ICommand OpenAdminCommand { get; }

        public BreakTimeViewModel(int breakEndHour, int breakEndMinute, string breakMessage = "", bool isInstantBreak = false, Action? onOpenAdmin = null)
        {
            BreakMessage = breakMessage;
            IsInstantBreak = isInstantBreak;
            _onOpenAdmin = onOpenAdmin;

            OpenAdminCommand = new RelayCommand(() =>
            {
                Console.WriteLine("[BreakTimeViewModel] OpenAdminCommand executed - calling onOpenAdmin callback");
                _onOpenAdmin?.Invoke();
                Console.WriteLine("[BreakTimeViewModel] OpenAdminCommand completed");
            });

            // Format break end time (only relevant for scheduled breaks)
            if (!isInstantBreak)
            {
                var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, breakEndHour, breakEndMinute, 0);
                BreakEndTime = endTime.ToString("h:mm tt");
            }

            // Initialize clock
            UpdateDateTime();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateDateTime();
            _clockTimer.Start();

            Console.WriteLine($"[BreakTimeViewModel] Initialized - Instant: {isInstantBreak}, Break ends at: {BreakEndTime}");
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentDate = now.ToString("MM/dd/yyyy");
            CurrentTime = now.ToString("HH:mm:ss");
        }

        public void Cleanup()
        {
            _clockTimer.Stop();
        }
    }
}
