using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace IPS.MainApp.ViewModels
{
    public class OrderResultViewModel : BaseViewModel
    {
        private readonly Action _onNavigateToWelcome;

        public bool IsSuccess { get; }
        public string OrderLabel { get; }
        public string Message { get; }

        public ICommand DoneCommand { get; }

        public OrderResultViewModel(bool isSuccess, string orderLabel, string message, Action onNavigateToWelcome)
        {
            IsSuccess = isSuccess;
            OrderLabel = orderLabel;
            Message = message;
            _onNavigateToWelcome = onNavigateToWelcome ?? throw new ArgumentNullException(nameof(onNavigateToWelcome));

            DoneCommand = new RelayCommand(OnDone);
        }

        private void OnDone()
        {
            _onNavigateToWelcome();
        }
    }
}
