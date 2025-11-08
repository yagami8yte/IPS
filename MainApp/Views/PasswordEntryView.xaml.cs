using System;
using System.Windows;
using System.Windows.Controls;
using IPS.MainApp.ViewModels;

namespace IPS.MainApp.Views
{
    public partial class PasswordEntryView : UserControl
    {
        public PasswordEntryView()
        {
            InitializeComponent();

            // Wire up keypad events
            NumericKeypadControl.NumberPressed += OnNumberPressed;
            NumericKeypadControl.BackspacePressed += OnBackspacePressed;
            NumericKeypadControl.ClearPressed += OnClearPressed;

            // Subscribe to lifecycle events
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[PasswordEntryView] View loaded");
            Console.WriteLine($"[PasswordEntryView] DataContext type: {DataContext?.GetType().Name ?? "null"}");

            // ALWAYS reset all state when view loads - no exceptions
            if (DataContext is PasswordEntryViewModel viewModel)
            {
                Console.WriteLine($"[PasswordEntryView] Password length before reset: {viewModel.Password?.Length ?? 0}");
                Console.WriteLine("[PasswordEntryView] Calling ResetState to clear all inputs");

                viewModel.ResetState();

                Console.WriteLine($"[PasswordEntryView] Password length after reset: {viewModel.Password?.Length ?? 0}");
                Console.WriteLine("[PasswordEntryView] All inputs initialized to empty");
            }
            else
            {
                Console.WriteLine("[PasswordEntryView] WARNING: DataContext is not PasswordEntryViewModel!");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[PasswordEntryView] View unloaded");

            // Unsubscribe from events
            NumericKeypadControl.NumberPressed -= OnNumberPressed;
            NumericKeypadControl.BackspacePressed -= OnBackspacePressed;
            NumericKeypadControl.ClearPressed -= OnClearPressed;
        }

        private void OnNumberPressed(object? sender, string number)
        {
            if (DataContext is PasswordEntryViewModel viewModel)
            {
                // Limit PIN to 8 digits
                if (viewModel.Password.Length < 8)
                {
                    viewModel.Password += number;
                    Console.WriteLine($"[PasswordEntryView] Number pressed: {number}, Password length: {viewModel.Password.Length}");
                }
            }
        }

        private void OnBackspacePressed(object? sender, System.EventArgs e)
        {
            if (DataContext is PasswordEntryViewModel viewModel && viewModel.Password.Length > 0)
            {
                viewModel.Password = viewModel.Password.Substring(0, viewModel.Password.Length - 1);
                Console.WriteLine($"[PasswordEntryView] Backspace pressed, Password length: {viewModel.Password.Length}");
            }
        }

        private void OnClearPressed(object? sender, System.EventArgs e)
        {
            if (DataContext is PasswordEntryViewModel viewModel)
            {
                viewModel.Password = string.Empty;
                Console.WriteLine("[PasswordEntryView] Clear pressed");
            }
        }
    }
}
