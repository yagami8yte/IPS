using System.Windows.Controls;

namespace IPS.MainApp.Views
{
    /// <summary>
    /// Interaction logic for SalesContentView.xaml
    /// </summary>
    public partial class SalesContentView : UserControl
    {
        public SalesContentView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler to set row numbers in the Top Items DataGrid
        /// </summary>
        private void TopItemsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Set row header to row index + 1 (to show rank starting from 1)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
