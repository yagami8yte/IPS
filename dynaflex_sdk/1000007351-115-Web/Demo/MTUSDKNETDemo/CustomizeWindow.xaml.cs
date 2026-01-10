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
    /// Interaction logic for CustomizeWindow.xaml
    /// </summary>
    public partial class CustomizeWindow : Window
    {
        public CustomizedSettings mSettings;

        public CustomizeWindow(CustomizedSettings settings)
        {
            InitializeComponent();

            mSettings = settings;

            TipModeCB.SelectedIndex = mSettings.TipMode;

            Tip1DisplayModeCB.SelectedIndex = mSettings.TipButton1Display;
            Tip2DisplayModeCB.SelectedIndex = mSettings.TipButton2Display;
            Tip3DisplayModeCB.SelectedIndex = mSettings.TipButton3Display;
            Tip4DisplayModeCB.SelectedIndex = mSettings.TipButton4Display;
            Tip5DisplayModeCB.SelectedIndex = mSettings.TipButton5Display;
            Tip6DisplayModeCB.SelectedIndex = mSettings.TipButton6Display;

            TaxSurchargeRateLabel.Content = mSettings.UseSurcharge ? "Surcharge Rate: " : "Tax Rate: ";
            TaxSurchargeRateTextBox.Text = mSettings.TaxSurchargeRate;

            FButtonRightCB.ItemsSource = settings.UIStringList;

            FButtonRightCB.SelectedIndex = getStringIDValue(mSettings.PresentCardFunctionalButtonRightOption, settings.UIStringList);
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

        private string fractionToPercent(string fraction)
        {
            string percent = "0.00";

            try
            {
                if (fraction.Length > 0)
                {

                    double fractionValue = 0;

                    double.TryParse(fraction, out fractionValue);

                    double percentValue = fractionValue * 100;

                    percent = percentValue.ToString("F2");
                }
            }
            catch (Exception ex)
            {
            }

            return percent;
        }

        private string percentToFraction(string percent)
        {
            string fraction = "0.0000";

            try
            {
                if (percent.Length > 0)
                {
                    double percentValue = 0;

                    double.TryParse(percent, out percentValue);

                    double fractionValue = percentValue / 100;

                    fraction = fractionValue.ToString("F4");
                }
            }
            catch (Exception ex)
            {
            }

            return fraction;
        }

        private void updateTipDisplay(int index)
        {
            int tipMode = TipModeCB.SelectedIndex;

            int displayMode = 0;
            Label amountLabel = null;
            TextBox valueTextBox = null;
            Label percentLabel = null;
            string tipValue = "";

            switch (index)
            {
                case 1:
                    displayMode = Tip1DisplayModeCB.SelectedIndex;
                    amountLabel = Tip1AmountLabel;
                    percentLabel = Tip1PercentLabel;
                    valueTextBox = Tip1ValueTextBox;
                    tipValue = mSettings.Tip1Value;
                    break;
                case 2:
                    displayMode = Tip2DisplayModeCB.SelectedIndex;
                    amountLabel = Tip2AmountLabel;
                    percentLabel = Tip2PercentLabel;
                    valueTextBox = Tip2ValueTextBox;
                    tipValue = mSettings.Tip2Value;
                    break;
                case 3:
                    displayMode = Tip3DisplayModeCB.SelectedIndex;
                    amountLabel = Tip3AmountLabel;
                    percentLabel = Tip3PercentLabel;
                    valueTextBox = Tip3ValueTextBox;
                    tipValue = mSettings.Tip3Value;
                    break;
                case 4:
                    displayMode = Tip4DisplayModeCB.SelectedIndex;
                    amountLabel = Tip4AmountLabel;
                    percentLabel = Tip4PercentLabel;
                    valueTextBox = Tip4ValueTextBox;
                    tipValue = mSettings.Tip4Value;
                    break;
                case 5:
                    displayMode = Tip5DisplayModeCB.SelectedIndex;
                    amountLabel = Tip5AmountLabel;
                    percentLabel = Tip5PercentLabel;
                    valueTextBox = Tip5ValueTextBox;
                    tipValue = mSettings.Tip5Value;
                    break;
                case 6:
                    displayMode = Tip6DisplayModeCB.SelectedIndex;
                    amountLabel = Tip6AmountLabel;
                    percentLabel = Tip6PercentLabel;
                    valueTextBox = Tip6ValueTextBox;
                    tipValue = mSettings.Tip6Value;
                    break;
            }

            if ((amountLabel != null) && (percentLabel != null) && (valueTextBox != null))
            {
                if ((tipMode == 0) || (tipMode == 2)) // %
                {
                    amountLabel.Visibility = Visibility.Hidden;

                    percentLabel.Visibility = (displayMode == 0) ? Visibility.Visible : Visibility.Hidden;

                    valueTextBox.Text = (displayMode == 0) ? fractionToPercent(tipValue) : "";
                }
                else if ((tipMode == 1) || (tipMode == 3)) // $
                {
                    amountLabel.Visibility = (displayMode == 0) ? Visibility.Visible : Visibility.Hidden;

                    percentLabel.Visibility = Visibility.Hidden;

                    valueTextBox.Text = (displayMode == 0) ? tipValue : "";
                }
            }
        }

        private void TipModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 1; i <= 6; i++)
            {
                updateTipDisplay(i);
            }
        }

        private void Tip1DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(1);
        }

        private void Tip2DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(2);
        }

        private void Tip3DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(3);
        }

        private void Tip4DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(4);
        }

        private void Tip5DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(5);
        }

        private void Tip6DisplayModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateTipDisplay(6);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mSettings.TipMode = (byte) TipModeCB.SelectedIndex;

                mSettings.TipButton1Display = (byte) Tip1DisplayModeCB.SelectedIndex;
                mSettings.TipButton2Display = (byte) Tip2DisplayModeCB.SelectedIndex;
                mSettings.TipButton3Display = (byte) Tip3DisplayModeCB.SelectedIndex;
                mSettings.TipButton4Display = (byte) Tip4DisplayModeCB.SelectedIndex;
                mSettings.TipButton5Display = (byte) Tip5DisplayModeCB.SelectedIndex;
                mSettings.TipButton6Display = (byte) Tip6DisplayModeCB.SelectedIndex;

                if ((mSettings.TipMode == 0) || (mSettings.TipMode == 2)) // %
                {
                    mSettings.Tip1Value = percentToFraction(Tip1ValueTextBox.Text);
                    mSettings.Tip2Value = percentToFraction(Tip2ValueTextBox.Text);
                    mSettings.Tip3Value = percentToFraction(Tip3ValueTextBox.Text);
                    mSettings.Tip4Value = percentToFraction(Tip4ValueTextBox.Text);
                    mSettings.Tip5Value = percentToFraction(Tip5ValueTextBox.Text);
                    mSettings.Tip6Value = percentToFraction(Tip6ValueTextBox.Text);
                }
                else if ((mSettings.TipMode == 1) || (mSettings.TipMode == 3)) // $
                {
                    mSettings.Tip1Value = Tip1ValueTextBox.Text;
                    mSettings.Tip2Value = Tip2ValueTextBox.Text;
                    mSettings.Tip3Value = Tip3ValueTextBox.Text;
                    mSettings.Tip4Value = Tip4ValueTextBox.Text;
                    mSettings.Tip5Value = Tip5ValueTextBox.Text;
                    mSettings.Tip6Value = Tip6ValueTextBox.Text;
                }

                mSettings.TaxSurchargeRate = TaxSurchargeRateTextBox.Text;

                mSettings.PresentCardFunctionalButtonRightOption = getStringIDBytes(FButtonRightCB.SelectedIndex, mSettings.UIStringList);

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
