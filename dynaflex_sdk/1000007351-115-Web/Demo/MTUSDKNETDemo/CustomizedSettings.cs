using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUSDKDemo
{
    public class CustomizedSettings
    {
        public byte TipMode { get; set; }

        public byte TipButton1Display { get; set; } 
        public byte TipButton2Display { get; set; }
        public byte TipButton3Display { get; set; }
        public byte TipButton4Display { get; set; }
        public byte TipButton5Display { get; set; }
        public byte TipButton6Display { get; set; }

        public string Tip1Value { get; set; }
        public string Tip2Value { get; set; }
        public string Tip3Value { get; set; }
        public string Tip4Value { get; set; }
        public string Tip5Value { get; set; }
        public string Tip6Value { get; set; }

        public string TaxSurchargeRate { get; set; }

        public bool UseSurcharge { get; set; }
        public byte[] PresentCardFunctionalButtonRightOption { get; set; }

        public string[] UIStringList;

        public CustomizedSettings()
        {
            TipMode = 0;
            TipButton1Display = 0;
            TipButton2Display = 0;
            TipButton3Display = 0;
            TipButton4Display = 1;
            TipButton5Display = 2;
            TipButton6Display = 3;
            Tip1Value = "0.10";
            Tip2Value = "0.15";
            Tip3Value = "0.20";
            Tip4Value = "";
            Tip5Value = "";
            Tip6Value = "";
            TaxSurchargeRate = "10.00";
            UseSurcharge = false;

            PresentCardFunctionalButtonRightOption = null;

            UIStringList = DisplayStrings.StringList;
        }
    }
}
