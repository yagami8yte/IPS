using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using IPS.Services;

namespace IPS.MainApp.ViewModels
{
    /// <summary>
    /// ViewModel for PIN entry dialog when accessing admin panel
    /// Uses numeric-only PIN with touchscreen keypad (no keyboard required)
    /// </summary>
    public class PasswordEntryViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configService;
        private readonly PasswordService _passwordService;
        private readonly Action _onPasswordCorrect;
        private readonly Action _onCancel;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;

        /// <summary>
        /// The password entered by user
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                // Clear error when user types
                if (_hasError)
                {
                    HasError = false;
                    ErrorMessage = string.Empty;
                }
            }
        }

        /// <summary>
        /// Error message to display
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether an error is currently displayed
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to verify password and proceed
        /// </summary>
        public ICommand SubmitCommand { get; }

        /// <summary>
        /// Command to cancel and go back
        /// </summary>
        public ICommand CancelCommand { get; }

        public PasswordEntryViewModel(
            ConfigurationService configService,
            PasswordService passwordService,
            Action onPasswordCorrect,
            Action onCancel)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _onPasswordCorrect = onPasswordCorrect ?? throw new ArgumentNullException(nameof(onPasswordCorrect));
            _onCancel = onCancel ?? throw new ArgumentNullException(nameof(onCancel));

            SubmitCommand = new RelayCommand(OnSubmit);
            CancelCommand = new RelayCommand(OnCancel);

            // Explicitly clear all state
            _password = string.Empty;
            _errorMessage = string.Empty;
            _hasError = false;

            Console.WriteLine("[PasswordEntryViewModel] Initialized - Password cleared");
        }

        /// <summary>
        /// Reset all input state - called when view is shown
        /// </summary>
        public void ResetState()
        {
            Console.WriteLine("[PasswordEntryViewModel] ResetState called");
            Password = string.Empty;
            ErrorMessage = string.Empty;
            HasError = false;
            Console.WriteLine("[PasswordEntryViewModel] State reset complete");
        }

        private void OnSubmit()
        {
            Console.WriteLine($"[PasswordEntryViewModel] Submit called, Password length: {Password?.Length ?? 0}");
            Console.WriteLine($"[PasswordEntryViewModel] Password value: '{Password}'");

            if (string.IsNullOrWhiteSpace(Password))
            {
                HasError = true;
                ErrorMessage = "Please enter a PIN";
                Console.WriteLine("[PasswordEntryViewModel] Empty PIN");
                return;
            }

            // Get stored password hash
            var config = _configService.GetConfiguration();
            Console.WriteLine($"[PasswordEntryViewModel] Retrieved config, hash exists: {!string.IsNullOrEmpty(config.AdminPasswordHash)}");

            // Verify password
            bool isValid = _passwordService.VerifyPassword(Password, config.AdminPasswordHash);
            Console.WriteLine($"[PasswordEntryViewModel] Password verification result: {isValid}");

            if (isValid)
            {
                Console.WriteLine("[PasswordEntryViewModel] PIN correct - granting access");
                HasError = false;
                ErrorMessage = string.Empty;
                Password = string.Empty;  // Clear PIN on success
                _onPasswordCorrect();
            }
            else
            {
                Console.WriteLine("[PasswordEntryViewModel] Incorrect PIN");
                HasError = true;
                ErrorMessage = "Incorrect PIN. Please try again.";
                Password = string.Empty;  // Clear PIN field
                Console.WriteLine("[PasswordEntryViewModel] PIN cleared");
            }
        }

        private void OnCancel()
        {
            Console.WriteLine("[PasswordEntryViewModel] Cancelled - returning to welcome");
            _onCancel();
        }
    }
}
