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
    /// Interaction logic for ClassicNFCTagWindow.xaml
    /// </summary>
    public partial class ClassicNFCTagWindow : Window
    {
        private List<string> mData = null;

        private int mWriteSector = 0;
        private string mWriteData = "";

        public ClassicNFCTagWindow(List<string> data)
        {
            InitializeComponent();

            mData = data;

            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    SectorCB.Items.Add("( " + i + " )");
                }

                SectorCB.SelectedIndex = 0;
            }
        }

        private void SectorCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BlockTextBox.Clear();
            DataTextBox.Clear();

            int sector = SectorCB.SelectedIndex;

            if ((mData != null) && (sector < mData.Count))
            {
                int nBlocks = 0;

                if (sector < 32)
                    nBlocks = 4;
                else
                    nBlocks = 16;

                string value = mData[sector];

                for (int i = 0; i < nBlocks; i++)
                {
                    BlockTextBox.AppendText("Block " + i);
                    BlockTextBox.AppendText(Environment.NewLine);

                    string blockValue = value.Substring(i * 32, 32);
                    DataTextBox.AppendText(blockValue);
                    DataTextBox.AppendText(Environment.NewLine);
                }
            }
        }

        public int getWriteSector()
        {
            return mWriteSector;
        }

        public string getWriteData()
        {
            return mWriteData;
        }

        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mWriteSector = SectorCB.SelectedIndex;
                mWriteData = DataTextBox.Text;

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
