using System;

namespace IPS.MainApp.ViewModels
{
    public class LoadingViewModel : BaseViewModel
    {
        private string _statusMessage = "Initializing system...";
        private int _progress = 0;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public LoadingViewModel()
        {
            StatusMessage = "Initializing system...";
            Progress = 0;
        }

        public void UpdateStatus(string message, int progress)
        {
            StatusMessage = message;
            Progress = progress;
        }
    }
}
