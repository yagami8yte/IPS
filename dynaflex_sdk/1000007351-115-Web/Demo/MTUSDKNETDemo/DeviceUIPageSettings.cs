using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUSDKDemo
{
    public class DeviceUIPageSettings
    {
        public byte PageOption { get; set; }

        public byte Timeout { get; set; }

        public byte[] TitleTextStringID { get; set; }

        public string LineText1 { get; set; }
        public string LineText2 { get; set; }
        public string LineText3 { get; set; }
        public string LineText4 { get; set; }
        public string LineText5 { get; set; }

        public byte[] ButtonTextStringID1 { get; set; }
        public byte[] ButtonTextStringID2 { get; set; }
        public byte[] ButtonTextStringID3 { get; set; }
        public byte[] ButtonTextStringID4 { get; set; }
        public byte[] ButtonTextStringID5 { get; set; }
        public byte[] ButtonTextStringID6 { get; set; }

        public string ButtonAmountString1 { get; set; }
        public string ButtonAmountString2 { get; set; }
        public string ButtonAmountString3 { get; set; }
        public string ButtonAmountString4 { get; set; }
        public string ButtonAmountString5 { get; set; }
        public string ButtonAmountString6 { get; set; }

        public byte[] FButtonLeftTextStringID { get; set; }
        public byte[] FButtonMiddleTextStringID { get; set; }
        public byte[] FButtonRightTextStringID { get; set; }

        public byte FButtonLeftColor { get; set; }
        public byte FButtonMiddleColor { get; set; }
        public byte FButtonRightColor { get; set; }

        public string ImageFileName { get; set; }
        public byte[] ImageXPosition { get; set; }
        public byte[] ImageYPosition { get; set; }
        public byte[] ImageData { get; set; }

        public string[] UIStringList;

        public DeviceUIPageSettings()
        {
            PageOption = 1; // Text String Buttons

            Timeout = 0;

            TitleTextStringID = new byte[] { 0, 12 }; // What is the issue?

            LineText1 = "";
            LineText2 = "";
            LineText3 = "";
            LineText4 = "";
            LineText5 = "";

            ButtonTextStringID1 = new byte[] { 0, 14 }; // No hot water
            ButtonTextStringID2 = new byte[] { 0, 15 }; // Doesn't spin
            ButtonTextStringID3 = new byte[] { 0, 16 }; // Water leakage
            ButtonTextStringID4 = null; // (Disabled)
            ButtonTextStringID5 = null; // (Disabled)
            ButtonTextStringID6 = null; // (Disabled)

            ButtonAmountString1 = "";
            ButtonAmountString2 = "";
            ButtonAmountString3 = "";
            ButtonAmountString4 = "";
            ButtonAmountString5 = "";
            ButtonAmountString6 = "";

            FButtonLeftTextStringID = new byte[] { 0, 8 }; // Cancel
            FButtonLeftColor = 0; // Red
            FButtonMiddleTextStringID = null;  // (Disabled)
            FButtonMiddleColor = 0; // Red
            FButtonRightTextStringID = null;  // (Disabled)
            FButtonRightColor = 0; // Red

            ImageFileName = "";
            ImageXPosition = null;
            ImageYPosition = null;
            ImageData = null;

            UIStringList = DisplayStrings.StringList;
        }
    }
}
