using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MTUSDKNET;

namespace MTUSDKDemo
{
    public class FallbackManager
    {
        protected IFallbackAdapter mFallbackAdapter;
        protected ITransaction mTransaction;

        //protected bool mCardSwiped;
        protected bool mCardInserted;
        protected bool mMSRDetected;
        protected bool mChipDetected;
        protected int mChipFailureCount;
        protected bool mPendingOnUseChipReader;
        protected bool mPendingOnUseMSR;
        protected bool mPendingOnTryAgain;
        protected bool mPendingOnSignatureCaptureRequested;
        protected bool mFallbackToMSR;

        public FallbackManager(IFallbackAdapter fallbackAdapter, ITransaction transaction)
        {
            mFallbackAdapter = fallbackAdapter;
            mTransaction = transaction;

            reset();
        }

        protected void reset()
        {
            //mCardSwiped = false;
            mCardInserted = false;
            mMSRDetected = false;
            mChipDetected = false;
            mChipFailureCount = 0;
            mPendingOnUseChipReader = false;
            mPendingOnUseMSR = false;
            mPendingOnTryAgain = false;
            mPendingOnSignatureCaptureRequested = false;
            mFallbackToMSR = false;
        }

        private byte[] getTLVPayload(byte[] data)
        {
            byte[] payload = null;

            if (data != null)
            {
                int dataLen = data.Length;

                if (dataLen > 2)
                {
                    int tlvLen = (int)((data[0] & 0x000000FF) << 8) + (int)(data[1] & 0x000000FF);

                    payload = new byte[tlvLen];
                    Array.Copy(data, 2, payload, 0, tlvLen);
                }
            }

            return payload;
        }

        protected void sendCallbackOnUseChipReader()
        {
            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    if (mFallbackAdapter != null)
                    {
                        mFallbackAdapter.OnUseChipReader();
                    }
                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        protected void sendCallbackOnUseMSR()
        {
            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    if (mFallbackAdapter != null)
                    {
                        mFallbackAdapter.OnUseMSR();
                    }
                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        protected void sendCallbackOnTryAgain()
        {
            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(3000);

                try
                {
                    if (mFallbackAdapter != null)
                    {
                        mFallbackAdapter.OnTryAgain();
                    }
                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        protected void sendCallbackOnSignatureCaptureRequested()
        {
            Task task = Task.Factory.StartNew((Object obj) =>
            {
                Thread.Sleep(1000);

                try
                {
                    if (mFallbackAdapter != null)
                    {
                        mFallbackAdapter.OnSignatureCaptureRequested();
                    }
                }
                catch (Exception ex)
                {
                }
            }, this);
        }

        protected void processTransactionCompleted()
        {
            if (mChipDetected && mMSRDetected)
            {
                if (mChipFailureCount < 3)
                {
                    if (mCardInserted)
                    {
                        mPendingOnUseChipReader = true;
                    }
                    else
                    {
                        mChipDetected = false;
                        mMSRDetected = false;
                        sendCallbackOnUseChipReader();
                    }

                }
            }
        }

        protected void startOnUseMSR()
        {
            mFallbackToMSR = true;

            if (mCardInserted)
            {
                mPendingOnUseMSR = true;
            }
            else
            {
                sendCallbackOnUseMSR();
            }
        }

        public void OnEvent(EventType eventType, IData data)
        {
            switch (eventType)
            {
                case EventType.CardData:

                    break;
                case EventType.TransactionStatus:
                    TransactionStatus status = TransactionStatusBuilder.GetStatusCode(data.StringValue);
                    if (status == TransactionStatus.CardSwiped)
                    {
                        //mCardSwiped = true;
                    }
                    else if (status == TransactionStatus.CardInserted)
                    {
                        mCardInserted = true;
                        //mCardSwiped = false;
                    }
                    else if (status == TransactionStatus.CardRemoved)
                    {
                        mCardInserted = false;
                        //mCardSwiped = false;

                        if (mPendingOnUseChipReader)
                        {
                            mPendingOnUseChipReader = false;

                            sendCallbackOnUseChipReader();
                        }
                        else if (mPendingOnUseMSR)
                        {
                            mPendingOnUseMSR = false;

                            sendCallbackOnUseMSR();
                        }
                        else if (mPendingOnTryAgain)
                        {
                            mPendingOnTryAgain = false;

                            sendCallbackOnTryAgain();
                        }
                        else if (mPendingOnSignatureCaptureRequested)
                        {
                            mPendingOnSignatureCaptureRequested = false;

                            sendCallbackOnSignatureCaptureRequested();
                        }
                    }
                    else if (status == TransactionStatus.CardDetected)
                    {
                        //mCardSwiped = false;
                    }
                    else if (status == TransactionStatus.CardCollision)
                    {
                        //mCardSwiped = false;
                    }
                    else if (status == TransactionStatus.TimedOut)
                    {

                    }
                    else if (status == TransactionStatus.HostCancelled)
                    {

                    }
                    else if (status == TransactionStatus.TransactionCancelled)
                    {

                    }
                    else if (status == TransactionStatus.TransactionInProgress)
                    {

                    }
                    else if (status == TransactionStatus.TransactionError)
                    {

                    }
                    else if (status == TransactionStatus.QuickChipDeferred)
                    {
                        processTransactionCompleted();
                    }
                    else if (status == TransactionStatus.TransactionCompleted)
                    {
                        processTransactionCompleted();
                    }
                    else if (status == TransactionStatus.TransactionApproved)
                    {
                        processTransactionCompleted();
                    }
                    else if (status == TransactionStatus.TransactionDeclined)
                    {

                    }
                    else if (status == TransactionStatus.TransactionFailed)
                    {
                        startOnUseMSR();
                    }
                    else if (status == TransactionStatus.TransactionNotAccepted)
                    {
                        startOnUseMSR();
                    }
                    else if (status == TransactionStatus.TechnicalFallback)
                    {
                        mChipFailureCount++;

                        if (mChipFailureCount < 3)
                        {
                            if (mCardInserted)
                            {
                                mPendingOnTryAgain = true;
                            }
                            else
                            {
                                sendCallbackOnTryAgain();
                            }
                        }
                        else
                        {
                            startOnUseMSR();
                        }
                    }
                    else if (status == TransactionStatus.SignatureCaptureRequested)
                    {
                        if (mCardInserted)
                        {
                            mPendingOnSignatureCaptureRequested = true;
                            return;
                        }

                        sendCallbackOnSignatureCaptureRequested();
                    }
                    break;
                case EventType.AuthorizationRequest:
                    if (data != null)
                    {
                        List<Dictionary<String, String>> parsedTLVList = MTParser.parseTLV(getTLVPayload(data.ByteArray));
                        byte[] cardType = MTParser.getTagByteArrayValue(parsedTLVList, "DFDF52");
                        if ((cardType != null) && (cardType.Length > 0))
                        {
                            if (cardType[0] == 0x05)
                            {
                                mChipDetected = true;
                                mMSRDetected = false;
                            }
                            else if (cardType[0] == 0x06)
                            {
                                mChipDetected = true;
                                mMSRDetected = false;
                            }
                            else if (cardType[0] == 0x07)
                            {
                                mChipDetected = true;
                                mMSRDetected = true;
                            }

                        }
                    }
                    break;
                case EventType.TransactionResult:
                    break;
            }
        }
    }
}
