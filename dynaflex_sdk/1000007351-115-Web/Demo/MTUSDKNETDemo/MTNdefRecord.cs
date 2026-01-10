using System.IO;

namespace MTUSDKDemo
{
    public class MTNdefRecord
    {
        public static byte TNF_EMPTY = 0x00;
        public static byte TNF_WELL_KNOWN = 0x01;
        public static byte TNF_MIME_MEDIA = 0x02;
        public static byte TNF_ABSOLUTE_URI = 0x03;
        public static byte TNF_EXTERNAL_TYPE = 0x04;
        public static byte TNF_UNKNOWN = 0x05;
        public static byte TNF_UNCHANGED = 0x06;
        public static byte TNF_RESERVED = 0x07;

        public static byte[] RTD_TEXT = { 0x54 };  // "T"
        public static byte[] RTD_URI = { 0x55 };   // "U"
        public static byte[] RTD_SMART_POSTER = { 0x53, 0x70 };  // "Sp"

        public static string[] URI_MAP = new string[]
        {
            "", // 0x00
            "http://www.", // 0x01
            "https://www.", // 0x02
            "http://", // 0x03
            "https://", // 0x04
            "tel:", // 0x05
            "mailto:", // 0x06
            "ftp://anonymous:anonymous@", // 0x07
            "ftp://ftp.", // 0x08
            "ftps://", // 0x09
            "sftp://", // 0x0A
            "smb://", // 0x0B
            "nfs://", // 0x0C
            "ftp://", // 0x0D
            "dav://", // 0x0E
            "news:", // 0x0F
            "telnet://", // 0x10
            "imap:", // 0x11
            "rtsp://", // 0x12
            "urn:", // 0x13
            "pop:", // 0x14
            "sip:", // 0x15
            "sips:", // 0x16
            "tftp:", // 0x17
            "btspp://", // 0x18
            "btl2cap://", // 0x19
            "btgoep://", // 0x1A
            "tcpobex://", // 0x1B
            "irdaobex://", // 0x1C
            "file://", // 0x1D
            "urn:epc:id:", // 0x1E
            "urn:epc:tag:", // 0x1F
            "urn:epc:pat:", // 0x20
            "urn:epc:raw:", // 0x21
            "urn:epc:", // 0x22
            "urn:nfc:", // 0x23
        };

        public static MTNdefRecord createTextRecord(byte[] textPayload)
        {
            return new MTNdefRecord(MTNdefRecord.TNF_WELL_KNOWN, MTNdefRecord.RTD_TEXT, null, textPayload);
        }

        public static MTNdefRecord createUriRecord(byte[] uriPayload)
        {
            return new MTNdefRecord(MTNdefRecord.TNF_WELL_KNOWN, MTNdefRecord.RTD_URI, null, uriPayload);
        }

        public static MTNdefRecord createMimeRecord(byte[] mimeType, byte[] mimePayload)
        {
            return new MTNdefRecord(MTNdefRecord.TNF_MIME_MEDIA, mimeType, null, mimePayload);
        }

        public static MTNdefRecord createAbsoluteUriRecord(byte[] uri)
        {
            return new MTNdefRecord(MTNdefRecord.TNF_ABSOLUTE_URI, uri, null, null);
        }

        public static MTNdefRecord createExternalRecord(byte[] extType, byte[] extPayload)
        {
            return new MTNdefRecord(MTNdefRecord.TNF_EXTERNAL_TYPE, extType, null, extPayload);
        }

        public byte TNF;
        public byte[] Type;
        public byte[] ID;
        public byte[] Payload;

        public MTNdefRecord()
        {
            TNF = 0;
            Type = null;
            ID = null;
            Payload = null;
        }

        public MTNdefRecord(byte tnf, byte[] type, byte[] id, byte[] payload)
        {
            TNF = tnf;
            Type = type;
            ID = id;
            Payload = payload;
        }

        public byte[] toBytes()
        {
            MemoryStream recordStream = new MemoryStream();

            byte b0 = TNF;

            b0 |= 0x10;

            if (ID != null)
                b0 |= 0x08;

            if (Payload != null)
            {
                recordStream.Write(new byte[] { b0, 1, (byte)Payload.Length }, 0, 3);

                if (Type != null)
                {
                    recordStream.Write(Type, 0, Type.Length);
                }
                /*
                                if (ID != null)
                                {
                                    recordStream.Write(ID, 0, ID.Length);
                                }
                */
                if (Payload != null)
                {
                    recordStream.Write(Payload, 0, Payload.Length);
                }
            }

            byte[] recordBytes = recordStream.ToArray();

            return recordBytes;
        }

        public bool isRtdType(byte[] typeName)
        {
            bool result = false;

            //if (TNF == TNF_WELL_KNOWN) // Well Known Types
            {
                if ((Type != null) && (typeName != null) && (typeName.Length > 0))
                {
                    if (Type.Length == typeName.Length)
                    {
                        result = true;

                        for (int i = 0; i < typeName.Length; i++)
                        {
                            if (Type[i] != typeName[i])
                                result = false;
                        }
                    }
                }
            }

            return result;
        }

        public bool isUri()
        {
            return isRtdType(RTD_URI);
        }

        public bool isText()
        {
            return isRtdType(RTD_TEXT);
        }

        public bool isWellKnownType()
        {
            return (TNF == TNF_WELL_KNOWN);
        }

        public bool isMimeType()
        {
            return (TNF == TNF_MIME_MEDIA);
        }

        public bool isAbsoluteUriType()
        {
            return (TNF == TNF_ABSOLUTE_URI);
        }

        public bool isExternalType()
        {
            return (TNF == TNF_EXTERNAL_TYPE);
        }

        public string getUriString()
        {
            string uriString = "";

            if (isUri())
            {
                uriString = "";

                if ((Payload != null) && (Payload.Length > 1))
                {
                    byte uriPrefix = Payload[0];

                    if (uriPrefix >= 0 && uriPrefix <= 0x23)
                    {
                        uriString = URI_MAP[uriPrefix];
                    }

                    int len = Payload.Length - 1;

                    if (len > 0)
                    {
                        byte[] textBytes = new byte[len];

                        System.Array.Copy(Payload, 1, textBytes, 0, len);

                        uriString += System.Text.Encoding.UTF8.GetString(textBytes);
                    }
                }
            }
            else if (isAbsoluteUriType())
            {
                if (Type != null)
                {
                    uriString = System.Text.Encoding.UTF8.GetString(Type);
                }
            }

            return uriString;
        }

        public string getTextString()
        {
            string textString = "";

            if (isText())
            {
                if (Payload != null)
                {
                    int len = Payload.Length;
                    int i = 0;

                    if (len > 0)
                    {
                        bool utf8 = (Payload[0] & (byte)0x80) == 0;

                        byte lenLang = (byte)(Payload[i++] & 0x3F);

                        len--;

                        if (len >= lenLang)
                        {
                            byte[] langBytes = new byte[lenLang];

                            System.Array.Copy(Payload, i, langBytes, 0, lenLang);

                            i += lenLang;
                            len -= lenLang;
                        }

                        if (len > 0)
                        {
                            byte[] textBytes = new byte[len];

                            System.Array.Copy(Payload, i, textBytes, 0, len);

                            if (utf8)
                            {
                                textString = System.Text.Encoding.UTF8.GetString(textBytes);
                            }
                            else
                            {
                                textString = System.Text.Encoding.Unicode.GetString(textBytes);
                            }
                        }
                    }
                }
            }
            else if (isMimeType())
            {
                if (Payload != null)
                {
                    textString = System.Text.Encoding.UTF8.GetString(Payload);
                }
            }
            else if (isExternalType())
            {
                if (Payload != null)
                {
                    textString = System.Text.Encoding.UTF8.GetString(Payload);
                }
            }

            return textString;
        }
    }
}
