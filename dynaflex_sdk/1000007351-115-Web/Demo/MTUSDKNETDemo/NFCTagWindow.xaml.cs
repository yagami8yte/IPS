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
    /// Interaction logic for NFCTagWindow.xaml
    /// </summary>
    public partial class NFCTagWindow : Window
    {
        private string mTextString = "";
        private int mURIPrefix = 2;
        private string mURIString = "";
        private bool mAppendMode = true;

        public NFCTagWindow()
        {
            InitializeComponent();

            for (int i = 0; i < MTNdefRecord.URI_MAP.Length; i++)
            {
                URIPrefixCB.Items.Add(MTNdefRecord.URI_MAP[i]);

                if (i == mURIPrefix)
                {
                    URIPrefixCB.SelectedIndex = mURIPrefix;
                }
            }
        }

        public string getTextString()
        {
            return mTextString;
        }

        public int getURIPrefix()
        {
            return mURIPrefix;
        }
        
        public string getURIString()
        {
            return mURIString;
        }

        public bool getAppendMode()
        {
            return mAppendMode;
        }

        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mTextString =TextTextBox.Text;
                mURIPrefix = URIPrefixCB.SelectedIndex;
                mURIString = URITextBox.Text;
                mAppendMode = false;

                this.DialogResult = true;

                this.Close();
            }
            catch (Exception)
            {
            }
        }
        private void AppendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mTextString = TextTextBox.Text;
                mURIPrefix = URIPrefixCB.SelectedIndex;
                mURIString = URITextBox.Text;
                mAppendMode = true;

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
