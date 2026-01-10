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

using MTUSDKNET;

namespace MTUSDKDemo
{
    /// <summary>
    /// Interaction logic for DeviceUIPage.xaml
    /// </summary>
    public partial class DeviceUIPage : Window
    {
        public DeviceUIPageSettings mSettings;

        public DeviceUIPage(DeviceUIPageSettings settings)
        {
            InitializeComponent();

            mSettings = settings;

            TitleCB.ItemsSource = settings.UIStringList;

            ButtonCB1.ItemsSource = settings.UIStringList;
            ButtonCB2.ItemsSource = settings.UIStringList;
            ButtonCB3.ItemsSource = settings.UIStringList;
            ButtonCB4.ItemsSource = settings.UIStringList;
            ButtonCB5.ItemsSource = settings.UIStringList;
            ButtonCB6.ItemsSource = settings.UIStringList;

            FButtonCB1.ItemsSource = settings.UIStringList;
            FButtonCB2.ItemsSource = settings.UIStringList;
            FButtonCB3.ItemsSource = settings.UIStringList;

            PageOptionCB.SelectedIndex = mSettings.PageOption;

            TimeoutTextBox.Text = mSettings.Timeout.ToString();
        }

        private int getStringIDValue(byte[] valueBytes, string[] stringList)
        {
            if ((valueBytes != null) && (stringList != null))
            {
                if (stringList.Length > 0)
                {
                    string line = stringList[0];
                    if ((line.Length >= 4) && (line[4] == ','))
                    {
                        string valueString = MTParser.getHexString(valueBytes);

                        for (int i = 0; i < stringList.Length; i++)
                        {
                            line = stringList[i];

                            if ((line.Length >= 4) && (line[4] == ','))
                            {
                                if (line.StartsWith(valueString))
                                {
                                    return i;
                                }
                            }
                        }
                    }
                }
            }

            int value = -1;

            try
            {
                if ((valueBytes != null) && (valueBytes.Length == 2))
                {
                    value = (valueBytes[0] * 256) + valueBytes[1];
                }
            }
            catch (Exception)
            {
            }

            return value;
        }

        private byte[] getStringIDBytes(int value, string[] stringList)
        {
            if ((value > 0) && (stringList != null))
            {
                if (value < stringList.Length)
                {
                    string line = stringList[value];
                    if ((line.Length >= 4) && (line[4] == ','))
                    {
                        byte[] byteArray = null;

                        string valueString = line.Substring(0, 4);

                        if (!valueString.StartsWith("0000"))
                        {
                            byteArray = MTParser.getByteArrayFromHexString(valueString);
                        }

                        return byteArray;
                    }
                }
            }

            byte[] valueBytes = null;

            try
            {
                if (value > 0)
                {
                    valueBytes = new byte[2];
                    valueBytes[0] = (byte)((value >> 8) & (0xFF));
                    valueBytes[1] = (byte)(value & (0xFF));
                }
            }
            catch (Exception)
            {
            }

            return valueBytes;
        }

        private void updatePage()
        {
            try
            {
                if (mSettings != null)
                {
                    byte pageOption = mSettings.PageOption;

                    switch (pageOption)
                    {
                        case 0: // Text Lines
                            TitleCB.SelectedIndex = -1;
                            ValueTextBox1.Text = mSettings.LineText1;
                            ValueTextBox2.Text = mSettings.LineText2;
                            ValueTextBox3.Text = mSettings.LineText3;
                            ValueTextBox4.Text = mSettings.LineText4;
                            ValueTextBox5.Text = mSettings.LineText5;
                            ValueTextBox6.Text = "";
                            FButtonCB1.SelectedIndex = -1;
                            ColorCB1.SelectedIndex = -1;
                            FButtonCB2.SelectedIndex = getStringIDValue(mSettings.FButtonMiddleTextStringID, mSettings.UIStringList);
                            ColorCB2.SelectedIndex = -1;
                            FButtonCB3.SelectedIndex = -1;
                            ColorCB3.SelectedIndex = -1;
                            break;
                        case 1: // Text String Buttons
                            TitleCB.SelectedIndex = getStringIDValue(mSettings.TitleTextStringID, mSettings.UIStringList);
                            ButtonCB1.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID1, mSettings.UIStringList);
                            ButtonCB2.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID2, mSettings.UIStringList);
                            ButtonCB3.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID3, mSettings.UIStringList);
                            ButtonCB4.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID4, mSettings.UIStringList);
                            ButtonCB5.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID5, mSettings.UIStringList);
                            ButtonCB6.SelectedIndex = getStringIDValue(mSettings.ButtonTextStringID6, mSettings.UIStringList);
                            FButtonCB1.SelectedIndex = getStringIDValue(mSettings.FButtonLeftTextStringID, mSettings.UIStringList);
                            ColorCB1.SelectedIndex = (FButtonCB1.SelectedIndex > 0) ? mSettings.FButtonLeftColor : -1;
                            FButtonCB2.SelectedIndex = getStringIDValue(mSettings.FButtonMiddleTextStringID, mSettings.UIStringList);
                            ColorCB2.SelectedIndex = (FButtonCB2.SelectedIndex > 0) ? mSettings.FButtonMiddleColor : -1;
                            FButtonCB3.SelectedIndex = getStringIDValue(mSettings.FButtonRightTextStringID, mSettings.UIStringList);
                            ColorCB3.SelectedIndex = (FButtonCB3.SelectedIndex > 0) ? mSettings.FButtonRightColor : -1;
                            break;
                        case 2: // Amount Buttons
                            TitleCB.SelectedIndex = getStringIDValue(mSettings.TitleTextStringID, mSettings.UIStringList);

                            ValueTextBox1.Text = mSettings.ButtonAmountString1;
                            ValueTextBox2.Text = mSettings.ButtonAmountString2;
                            ValueTextBox3.Text = mSettings.ButtonAmountString3;
                            ValueTextBox4.Text = mSettings.ButtonAmountString4;
                            ValueTextBox5.Text = mSettings.ButtonAmountString5;
                            ValueTextBox6.Text = mSettings.ButtonAmountString6;
                            FButtonCB1.SelectedIndex = getStringIDValue(mSettings.FButtonLeftTextStringID, mSettings.UIStringList);
                            ColorCB1.SelectedIndex = (FButtonCB1.SelectedIndex > 0) ? mSettings.FButtonLeftColor : -1;
                            FButtonCB2.SelectedIndex = getStringIDValue(mSettings.FButtonMiddleTextStringID, mSettings.UIStringList);
                            ColorCB2.SelectedIndex = (FButtonCB2.SelectedIndex > 0) ? mSettings.FButtonMiddleColor : -1;
                            FButtonCB3.SelectedIndex = getStringIDValue(mSettings.FButtonRightTextStringID, mSettings.UIStringList);
                            ColorCB3.SelectedIndex = (FButtonCB3.SelectedIndex > 0) ? mSettings.FButtonRightColor : -1;
                            break;
                        case 3: // Custom Image
                            TitleCB.SelectedIndex = getStringIDValue(mSettings.TitleTextStringID, mSettings.UIStringList);
                            FButtonCB1.SelectedIndex = -1;
                            ColorCB1.SelectedIndex = -1;
                            FButtonCB2.SelectedIndex = -1;
                            ColorCB2.SelectedIndex = -1;
                            FButtonCB3.SelectedIndex = getStringIDValue(mSettings.FButtonRightTextStringID, mSettings.UIStringList);
                            ColorCB3.SelectedIndex = -1;
                            ImageFileTextBox.Text = mSettings.ImageFileName;
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void updateForPageOption(byte pageOption)
        {
            try
            {
                switch (pageOption)
                {
                    case 0: // Text Lines
                        enableSetTitle(false);
                        enableTextStringButtons(false);
                        enableTextLines(true);
                        enableFButtons(false, true, false);
                        enableColorFButtons(false, false, false);
                        ColorCB2.SelectedIndex = 1; // Green
                        enableImageFile(false);
                        break;
                    case 1: // Text String Buttons
                        enableSetTitle(true);
                        enableTextStringButtons(true);
                        enableAmountButtons(false);
                        enableFButtons(true, true, true);
                        enableColorFButtons(true, true, true);
                        enableImageFile(false);
                        break;
                    case 2: // Amount Buttons
                        enableSetTitle(true);
                        enableTextStringButtons(false);
                        enableAmountButtons(true);
                        enableFButtons(true, true, true);
                        enableColorFButtons(true, true, true);
                        enableImageFile(false);
                        break;
                    case 3: // Custom Image
                        enableSetTitle(true);
                        enableTextStringButtons(false);
                        enableAmountButtons(false);
                        enableFButtons(false, false, true);
                        enableColorFButtons(false, false, false);
                        ColorCB3.SelectedIndex = 1; // Green
                        enableImageFile(true);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private void enableSetTitle(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    TitleCB.SelectedIndex = -1;
                }

                TitleLabel.IsEnabled = enabled;
                TitleCB.IsEnabled = enabled;

            }
            catch (Exception)
            {
            }
        }

        private void enableTextLines(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    ValueLabel1.Content = "";
                    ValueLabel2.Content = "";
                    ValueLabel3.Content = "";
                    ValueLabel4.Content = "";
                    ValueLabel5.Content = "";
                    ValueLabel6.Content = "";
                }

                ValueLabel1.IsEnabled = enabled;
                ValueTextBox1.IsEnabled = enabled;

                ValueLabel2.IsEnabled = enabled;
                ValueTextBox2.IsEnabled = enabled;

                ValueLabel3.IsEnabled = enabled;
                ValueTextBox3.IsEnabled = enabled;

                ValueLabel4.IsEnabled = enabled;
                ValueTextBox4.IsEnabled = enabled;

                ValueLabel5.IsEnabled = enabled;
                ValueTextBox5.IsEnabled = enabled;

                ValueLabel6.IsEnabled = false;
                ValueTextBox6.IsEnabled = false;

                if (enabled)
                {
                    ValueLabel1.Content = "Line 1:";
                    ValueLabel2.Content = "Line 2:";
                    ValueLabel3.Content = "Line 3:";
                    ValueLabel4.Content = "Line 4:";
                    ValueLabel5.Content = "Line 5:";
                    ValueLabel6.Content = "";
                }
            }
            catch (Exception)
            {
            }
        }
        private void enableTextStringButtons(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    ButtonCB1.SelectedIndex = -1;
                    ButtonCB2.SelectedIndex = -1;
                    ButtonCB3.SelectedIndex = -1;
                    ButtonCB4.SelectedIndex = -1;
                    ButtonCB5.SelectedIndex = -1;
                    ButtonCB6.SelectedIndex = -1;
                }

                ButtonLabel1.IsEnabled = enabled;
                ButtonLabel2.IsEnabled = enabled;
                ButtonLabel3.IsEnabled = enabled;
                ButtonLabel4.IsEnabled = enabled;
                ButtonLabel5.IsEnabled = enabled;
                ButtonLabel6.IsEnabled = enabled;

                ButtonCB1.IsEnabled = enabled;
                ButtonCB2.IsEnabled = enabled;
                ButtonCB3.IsEnabled = enabled;
                ButtonCB4.IsEnabled = enabled;
                ButtonCB5.IsEnabled = enabled;
                ButtonCB6.IsEnabled = enabled;
            }
            catch (Exception)
            {
            }
        }

        private void enableAmountButtons(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    ValueLabel1.Content = "";
                    ValueLabel2.Content = "";
                    ValueLabel3.Content = "";
                    ValueLabel4.Content = "";
                    ValueLabel5.Content = "";
                    ValueLabel6.Content = "";
                }

                ValueLabel1.IsEnabled = enabled;
                ValueTextBox1.IsEnabled = enabled;

                ValueLabel2.IsEnabled = enabled;
                ValueTextBox2.IsEnabled = enabled;

                ValueLabel3.IsEnabled = enabled;
                ValueTextBox3.IsEnabled = enabled;

                ValueLabel4.IsEnabled = enabled;
                ValueTextBox4.IsEnabled = enabled;

                ValueLabel5.IsEnabled = enabled;
                ValueTextBox5.IsEnabled = enabled;

                ValueLabel6.IsEnabled = enabled;
                ValueTextBox6.IsEnabled = enabled;

                if (enabled)
                {
                    ValueLabel1.Content = "Amt 1:";
                    ValueLabel2.Content = "Amt 2:";
                    ValueLabel3.Content = "Amt 3:";
                    ValueLabel4.Content = "Amt 4:";
                    ValueLabel5.Content = "Amt 5:";
                    ValueLabel6.Content = "Amt 6:";
                }
            }
            catch (Exception)
            {
            }
        }

        private void enableFButtons(bool enabled1, bool enabled2, bool enabled3)
        {
            try
            {
                FButtonLabel1.IsEnabled = enabled1;
                FButtonCB1.IsEnabled = enabled1;
                FButtonCB1.SelectedIndex = enabled1 ? 0 : -1;

                FButtonLabel2.IsEnabled = enabled2;
                FButtonCB2.IsEnabled = enabled2;
                FButtonCB2.SelectedIndex = enabled2 ? 0 : -1;

                FButtonLabel3.IsEnabled = enabled3;
                FButtonCB3.IsEnabled = enabled3;
                FButtonCB3.SelectedIndex = enabled3 ? 0 : -1;
            }
            catch (Exception)
            {
            }
        }

        private void enableColorFButtons(bool enabled1, bool enabled2, bool enabled3)
        {
            try
            {
                ColorLabel1.IsEnabled = enabled1;
                ColorCB1.IsEnabled = enabled1;
                ColorCB1.SelectedIndex = enabled1 ? 0 : -1;

                ColorLabel2.IsEnabled = enabled2;
                ColorCB2.IsEnabled = enabled2;
                ColorCB2.SelectedIndex = enabled2 ? 0 : -1;

                ColorLabel3.IsEnabled = enabled3;
                ColorCB3.IsEnabled = enabled3;
                ColorCB3.SelectedIndex = enabled3 ? 0 : -1;
            }
            catch (Exception)
            {
            }
        }

        private void enableImageFile(bool enabled)
        {
            try
            {
                ImageFileLabel.IsEnabled = enabled;
                ImageFileTextBox.IsEnabled = enabled;
                ImageFileButton.IsEnabled = enabled;

                if (!enabled)
                {
                    ImageFileTextBox.Text = "";
                }
            }
            catch (Exception)
            {
            }
        }

        private void PageOptionCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            byte pageOption = (byte)PageOptionCB.SelectedIndex;

            mSettings.PageOption = pageOption;

            updateForPageOption(pageOption);
            updatePage();
        }


        private void FButtonCB1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FButtonCB1.SelectedIndex < 1)
                {
                    ColorCB1.SelectedIndex = -1;
                }

            }
            catch (Exception)
            {
            }
        }

        private void FButtonCB2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FButtonCB2.SelectedIndex < 1)
                {
                    ColorCB2.SelectedIndex = -1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void FButtonCB3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FButtonCB3.SelectedIndex < 1)
                {
                    ColorCB3.SelectedIndex = -1;
                }
            }
            catch (Exception)
            {
            }
        }

        private void ImageFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

                bool bShow = ofd.ShowDialog() ?? false;

                if (bShow)
                {
                    string fileName = ofd.SafeFileName;

                    ImageFileTextBox.Text = fileName;

                    mSettings.ImageFileName = fileName;
                    mSettings.ImageXPosition = null;
                    mSettings.ImageYPosition = null;
                    mSettings.ImageData = System.IO.File.ReadAllBytes(ofd.FileName);
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void saveSettings()
        {
            try
            {
                if (mSettings != null)
                {
                    byte pageOption = (byte)PageOptionCB.SelectedIndex;

                    mSettings.PageOption = pageOption;

                    try
                    {
                        string timeoutString = TimeoutTextBox.Text;

                        mSettings.Timeout = (byte)Convert.ToInt32(timeoutString);
                    }
                    catch (Exception)
                    {
                    }

                    switch (pageOption)
                    {
                        case 0: // Text Lines
                            mSettings.TitleTextStringID = null;
                            mSettings.LineText1 = ValueTextBox1.Text;
                            mSettings.LineText2 = ValueTextBox2.Text;
                            mSettings.LineText3 = ValueTextBox3.Text;
                            mSettings.LineText4 = ValueTextBox4.Text;
                            mSettings.LineText5 = ValueTextBox5.Text;
                            mSettings.FButtonLeftTextStringID = null;
                            mSettings.FButtonLeftColor = 0;
                            mSettings.FButtonMiddleTextStringID = getStringIDBytes(FButtonCB2.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonMiddleColor = 0;
                            mSettings.FButtonRightTextStringID = null;
                            mSettings.FButtonRightColor = 0;
                            break;
                        case 1: // Text String Buttons
                            mSettings.TitleTextStringID = getStringIDBytes(TitleCB.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID1 = getStringIDBytes(ButtonCB1.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID2 = getStringIDBytes(ButtonCB2.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID3 = getStringIDBytes(ButtonCB3.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID4 = getStringIDBytes(ButtonCB4.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID5 = getStringIDBytes(ButtonCB5.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonTextStringID6 = getStringIDBytes(ButtonCB6.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonLeftTextStringID = getStringIDBytes(FButtonCB1.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonLeftColor = (byte)ColorCB1.SelectedIndex;
                            mSettings.FButtonMiddleTextStringID = getStringIDBytes(FButtonCB2.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonMiddleColor = (byte)ColorCB2.SelectedIndex;
                            mSettings.FButtonRightTextStringID = getStringIDBytes(FButtonCB3.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonRightColor = (byte)ColorCB3.SelectedIndex;
                            break;
                        case 2: // Amount Buttons
                            mSettings.TitleTextStringID = getStringIDBytes(TitleCB.SelectedIndex, mSettings.UIStringList);
                            mSettings.ButtonAmountString1 = ValueTextBox1.Text;
                            mSettings.ButtonAmountString2 = ValueTextBox2.Text;
                            mSettings.ButtonAmountString3 = ValueTextBox3.Text;
                            mSettings.ButtonAmountString4 = ValueTextBox4.Text;
                            mSettings.ButtonAmountString5 = ValueTextBox5.Text;
                            mSettings.ButtonAmountString6 = ValueTextBox6.Text;
                            mSettings.FButtonLeftTextStringID = getStringIDBytes(FButtonCB1.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonLeftColor = (byte)ColorCB1.SelectedIndex;
                            mSettings.FButtonMiddleTextStringID = getStringIDBytes(FButtonCB2.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonMiddleColor = (byte)ColorCB2.SelectedIndex;
                            mSettings.FButtonRightTextStringID = getStringIDBytes(FButtonCB3.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonRightColor = (byte)ColorCB3.SelectedIndex;
                            break;
                        case 3: // Custom Image
                            mSettings.TitleTextStringID = getStringIDBytes(TitleCB.SelectedIndex, mSettings.UIStringList);
                            mSettings.ImageFileName = ImageFileTextBox.Text;
                            mSettings.FButtonLeftTextStringID = null;
                            mSettings.FButtonLeftColor = 0;
                            mSettings.FButtonMiddleTextStringID = null;
                            mSettings.FButtonMiddleColor = 0;
                            mSettings.FButtonRightTextStringID = getStringIDBytes(FButtonCB3.SelectedIndex, mSettings.UIStringList);
                            mSettings.FButtonRightColor = (byte)ColorCB3.SelectedIndex;
                            break;
                    }
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
                saveSettings();

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
