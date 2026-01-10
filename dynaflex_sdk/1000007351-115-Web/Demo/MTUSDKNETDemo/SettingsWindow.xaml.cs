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
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private MQTTSettings mMQTTSettings;

        public SettingsWindow(MQTTSettings mqttSettings)
        {
            InitializeComponent();

            mMQTTSettings = mqttSettings;

            try
            {
                BrokerURITextBox.Text = mMQTTSettings.URI;
                UsernameTextBox.Text = mMQTTSettings.Username;
                PasswordTextBox.Text = mMQTTSettings.Password;
                SubscribeTopicTextBox.Text = mMQTTSettings.SubscribeTopic;
                PublishTopicTextBox.Text = mMQTTSettings.PublishTopic;
                ClientCertTextBox.Text = mMQTTSettings.ClientCertificateFilePath;
                ClientCertPasswordTextBox.Text = mMQTTSettings.ClientCertificatePassword;
            }
            catch (Exception)
            {
            }
        }

        public MQTTSettings getMQTTSEttings()
        {
            return mMQTTSettings;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClientCertTextBox.Text = "";
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    string filePath = ofd.FileName;

                    ClientCertTextBox.Text = filePath;
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mMQTTSettings.URI = BrokerURITextBox.Text;
                mMQTTSettings.Username = UsernameTextBox.Text;
                mMQTTSettings.Password = PasswordTextBox.Text;
                mMQTTSettings.SubscribeTopic = SubscribeTopicTextBox.Text;
                mMQTTSettings.PublishTopic = PublishTopicTextBox.Text;
                mMQTTSettings.ClientCertificateFilePath = ClientCertTextBox.Text;
                mMQTTSettings.ClientCertificatePassword = ClientCertPasswordTextBox.Text;

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
