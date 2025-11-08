using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPS.Core.Models
{
    /// <summary>
    /// Represents a configuration section tab in the admin panel
    /// </summary>
    public class ConfigSection : INotifyPropertyChanged
    {
        private bool _isSelected;

        /// <summary>
        /// Unique identifier for the section
        /// </summary>
        public string SectionId { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the section tab
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Icon text (emoji or symbol) for the section
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Whether this section is currently selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
