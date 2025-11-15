using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
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
        private string _adVideoPath = string.Empty;
        private bool _hasVideo = false;

        /// <summary>
        /// List of all video files found in Assets/Ads folder
        /// </summary>
        public List<string> AdVideoPlaylist { get; private set; } = new List<string>();

        /// <summary>
        /// Current video path being played
        /// </summary>
        public string AdVideoPath
        {
            get => _adVideoPath;
            set
            {
                _adVideoPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether there are any videos available
        /// </summary>
        public bool HasVideo
        {
            get => _hasVideo;
            set
            {
                _hasVideo = value;
                OnPropertyChanged();
            }
        }

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
            // Scan Assets/Ads folder for all video files
            LoadVideoPlaylist();

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

            Console.WriteLine("[WelcomeViewModel] Initialized with {0} video(s) in playlist", AdVideoPlaylist.Count);
        }

        /// <summary>
        /// Scans Assets/Ads folder for all video files and creates a playlist
        /// </summary>
        private void LoadVideoPlaylist()
        {
            try
            {
                string adsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Ads");
                Console.WriteLine($"[WelcomeViewModel] Scanning for videos in: {adsFolder}");

                if (!Directory.Exists(adsFolder))
                {
                    Console.WriteLine($"[WelcomeViewModel] Ads folder not found, creating: {adsFolder}");
                    Directory.CreateDirectory(adsFolder);
                    return;
                }

                // Common video file extensions
                string[] videoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".flv", ".webm" };

                var videoFiles = Directory.GetFiles(adsFolder)
                    .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .OrderBy(file => Path.GetFileName(file)) // Sort alphabetically
                    .ToList();

                foreach (var videoFile in videoFiles)
                {
                    // Convert absolute path to pack URI format for WPF
                    string relativePath = $"Assets/Ads/{Path.GetFileName(videoFile)}";
                    AdVideoPlaylist.Add(relativePath);
                    Console.WriteLine($"[WelcomeViewModel] Added video to playlist: {relativePath}");
                }

                // Set the first video as the initial video
                if (AdVideoPlaylist.Count > 0)
                {
                    AdVideoPath = AdVideoPlaylist[0];
                    HasVideo = true;
                    Console.WriteLine($"[WelcomeViewModel] Initial video: {AdVideoPath}");
                }
                else
                {
                    HasVideo = false;
                    Console.WriteLine("[WelcomeViewModel] No video files found in Assets/Ads folder");
                }
            }
            catch (Exception ex)
            {
                HasVideo = false;
                Console.WriteLine($"[WelcomeViewModel] Error loading video playlist: {ex.Message}");
            }
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            CurrentDate = now.ToString("MM/dd/yyyy");  // US format: 11/09/2025
            CurrentTime = now.ToString("HH:mm:ss");  // 24-hour format without AM/PM: 14:30:45
        }
    }
}
