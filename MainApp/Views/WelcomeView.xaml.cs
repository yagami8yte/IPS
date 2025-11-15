using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IPS.MainApp.ViewModels;

namespace IPS.MainApp.Views
{
    /// <summary>
    /// WelcomeView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WelcomeView : UserControl
    {
        private int _currentVideoIndex = 0;

        public WelcomeView()
        {
            InitializeComponent();
            this.Loaded += WelcomeView_Loaded;
        }

        private void WelcomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Start playing video when view is loaded
            VideoPlayer.Play();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Play next video in playlist
            if (DataContext is WelcomeViewModel viewModel && viewModel.AdVideoPlaylist.Count > 0)
            {
                // Move to next video
                _currentVideoIndex++;

                // Loop back to first video if we've reached the end
                if (_currentVideoIndex >= viewModel.AdVideoPlaylist.Count)
                {
                    _currentVideoIndex = 0;
                    Console.WriteLine("[WelcomeView] Playlist completed, looping back to first video");
                }

                // Update the video path
                viewModel.AdVideoPath = viewModel.AdVideoPlaylist[_currentVideoIndex];
                Console.WriteLine($"[WelcomeView] Playing next video ({_currentVideoIndex + 1}/{viewModel.AdVideoPlaylist.Count}): {viewModel.AdVideoPath}");

                // Play the new video
                VideoPlayer.Position = TimeSpan.Zero;
                VideoPlayer.Play();
            }
            else
            {
                // Fallback: loop the current video if no playlist
                VideoPlayer.Position = TimeSpan.Zero;
                VideoPlayer.Play();
            }
        }
    }
}
