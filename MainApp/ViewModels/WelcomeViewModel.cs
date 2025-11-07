using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPS.MainApp.ViewModels
{
    public class WelcomeViewModel : BaseViewModel
    {
        public string AdVideoPath { get; private set; }

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

            Console.WriteLine("[WelcomeViewModel] Initialized");
        }
    }
}
