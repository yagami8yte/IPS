using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPS.MainApp.ViewModels
{
    public class WelcomeViewModel : BaseViewModel
    {
        private readonly DispatcherTimer _clockTimer;
        private string _currentDate = string.Empty;
        private string _currentTime = string.Empty;

        public string AdVideoPath { get; private set; }

        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartOrderCommand { get; }
        public ICommand OpenAdminCommand { get; }

        public WelcomeViewModel(Action onStartOrder, Action onOpenAdmin)
        {
            AdVideoPath = "Assets/Ads/WelcomeVideo.mp4";

            StartOrderCommand = new RelayCommand(() =>
            {
                Console.WriteLine("[WelcomeViewModel] StartOrderCommand executed - calling onStartOrder callback");
                onStartOrder();
                Console.WriteLine("[WelcomeViewModel] StartOrderCommand completed");
            });

            OpenAdminCommand = new RelayCommand(() =>
            {
                Console.WriteLine("[WelcomeViewModel] OpenAdminCommand executed - calling onOpenAdmin callback");
                onOpenAdmin();
                Console.WriteLine("[WelcomeViewModel] OpenAdminCommand completed");
            });

            // Initialize clock
            UpdateDateTime();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateDateTime();
            _clockTimer.Start();

            Console.WriteLine("[WelcomeViewModel] Initialized");
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentDate = now.ToString("MM/dd/yyyy");  // US format: 11/09/2025
            CurrentTime = now.ToString("HH:mm:ss");  // 24-hour format without AM/PM: 14:30:45
        }
    }
}
