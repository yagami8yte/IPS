using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUSDKDemo
{
    public interface IFallbackAdapter
    {
        void OnUseChipReader();
        void OnUseMSR();
        void OnTryAgain();
        void OnSignatureCaptureRequested();
    }
}
