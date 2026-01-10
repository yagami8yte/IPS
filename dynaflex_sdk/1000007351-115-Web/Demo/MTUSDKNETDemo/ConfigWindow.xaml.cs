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
using System.Windows.Shapes;

namespace MTUSDKDemo
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private int mConfigAction = 0;
        private string mConfigType = "";
        private string mConfigData = "";

        public ConfigWindow()
        {
            InitializeComponent();
        }

        public int getConfigAction()
        {
            return mConfigAction;
        }

        public string getConfigType()
        {
            return mConfigType;
        }

        public string getConfigData()
        {
            return mConfigData;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GetConfigRB.IsChecked == true)
                    mConfigAction = 0;
                else if (SetConfigRB.IsChecked == true)
                    mConfigAction = 1;
                else if (GetKeyInfoRB.IsChecked == true)
                    mConfigAction = 2;
                else if (UpdateKeyInfoRB.IsChecked == true)
                    mConfigAction = 3;

                mConfigType = ConfigTypeTextBox.Text;
                mConfigData = ConfigDataTextBox.Text;

                this.DialogResult = true;

                this.Close();
            }
            catch (Exception)
            {
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.DialogResult = false;

                this.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
