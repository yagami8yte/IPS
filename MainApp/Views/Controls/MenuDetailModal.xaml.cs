using System.Windows.Controls;
using System.Windows.Input;
using IPS.MainApp.ViewModels;

namespace IPS.MainApp.Views.Controls
{
    public partial class MenuDetailModal : UserControl
    {
        public MenuDetailModal()
        {
            InitializeComponent();
        }

        private void OnTemperatureHotClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MenuDetailModalViewModel vm)
            {
                vm.SelectedTemperature = "hot";
            }
        }

        private void OnTemperatureIceClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MenuDetailModalViewModel vm)
            {
                vm.SelectedTemperature = "ice";
            }
        }

        private void OnVariantLightClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MenuDetailModalViewModel vm)
            {
                vm.SelectedVariantType = "light";
            }
        }

        private void OnVariantRegularClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MenuDetailModalViewModel vm)
            {
                vm.SelectedVariantType = "regular";
            }
        }

        private void OnVariantExtraClicked(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MenuDetailModalViewModel vm)
            {
                vm.SelectedVariantType = "extra";
            }
        }
    }
}
