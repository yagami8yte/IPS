using System;
using System.Collections.Generic;
using System.IO;

namespace MTUSDKDemo
{
    public class MTNdef
    {
        public static List<string> getNDEFMessages(string tlvString)
        {
            List<string> results = new List<string>();

            byte[] tagData = MTParser.getByteArrayFromHexString(tlvString);

            int offset = 0;
            while (offset < tagData.Length)
            {
                byte tag = tagData[offset++];

                if (tag == (byte)0x03) // NDEF Message
                {
                    int len = 0;

                    if (offset < tagData.Length)
                    {
                        len = (tagData[offset++] & 0x0FF);
                        if (len == 255) // 2-byte length follows
                        {
                            if ((offset + 1) < tagData.Length)
                            {
                                len = ((tagData[offset++] & 0x0FF) << 8);
                                len |= (tagData[offset++] & 0x0FF);
                            }
                        }
                    }

                    if (len > 0)
                    {
                        byte[] msgBytes = new byte[len];
                        Array.Copy(tagData, offset, msgBytes, 0, len);
                        string msgString = MTParser.getHexString(msgBytes);
                        results.Add(msgString);

                        offset += len;
                    }
                }
                else if (tag == (byte)0xFE)
                {
                    break;
                }
            }

            return results;
        }

        public static List<MTNdefRecord> Parse(byte[] data)
        {
            List<MTNdefRecord> records = new List<MTNdefRecord>();

            MemoryStream chunkStream = null;

            MTNdefRecord record = null; ;

            uint i = 0;
            while (i < data.Length)
            {
                if (record == null)
                    record = new MTNdefRecord();

                byte flag = data[i++];

                bool be = (flag & 0x80) != 0;
                bool me = (flag & 0x40) != 0;
                bool cf = (flag & 0x20) != 0;
                bool sr = (flag & 0x10) != 0;
                bool il = (flag & 0x08) != 0;

                record.TNF = (byte)(flag & 0x07);

                int headerLen = 1;
                headerLen += (sr ? 1 : 4);
                headerLen += (il ? 1 : 0);

                byte typeLen = 0;
                uint payloadLen = 0;

                if ((i + headerLen) < data.Length)
                {
                    typeLen = data[i++];

                    if (sr)
                    {
                        payloadLen = data[i++];
                    }
                    else
                    {
                        payloadLen |= (uint)((data[i++]) << 24);
                        payloadLen |= (uint)((data[i++]) << 16);
                        payloadLen |= (uint)((data[i++]) << 8);
                        payloadLen |= (uint)((data[i++]) << 0);
                    }

                }

                byte idLen = 0;

                if ((il) && (i < data.Length))
                {
                    idLen = data[i++];
                }

                uint totalLen = typeLen + payloadLen + idLen;

                if ((i + totalLen) <= data.Length)
                {
                    if (typeLen > 0)
                    {
                        record.Type = new byte[typeLen];
                        Array.Copy(data, (int)(i), record.Type, 0, typeLen);
                        i += (uint)typeLen;
                    }

                    if (idLen > 0)
                    {
                        record.ID = new byte[idLen];
                        Array.Copy(data, (int)(i), record.ID, 0, idLen);
                        i += (uint)idLen;
                    }

                    if (payloadLen > 0)
                    {
                        byte[] payload = new byte[payloadLen];
                        Array.Copy(data, (int)(i), payload, 0, (int)payloadLen);
                        i += payloadLen;

                        if (cf)
                        {
                            if (chunkStream == null)
                                chunkStream = new MemoryStream();

                            chunkStream.Write(payload, 0, payload.Length);
                        }
                        else if (chunkStream != null)
                        {
                            chunkStream.Write(payload, 0, payload.Length);
                            record.Payload = chunkStream.ToArray();
                            chunkStream = null;
                        }
                        else
                        {
                            record.Payload = payload;
                        }
                    }

                }

                if (!cf)
                {
                    records.Add(record);
                    record = null;

                    if (me)
                        break;
                }
            }

            return records;
        }
        public static byte[] BuildNDEFMessage(List<MTNdefRecord> records)
        {
            byte[] messageBytes = null;

            MemoryStream recordsStream = new MemoryStream();

            int n = records.Count;

            for (int i = 0; i < n; i++)
            {
                MTNdefRecord record = records[i];
                byte[] recordBytes = record.toBytes();

                if (i == 0)
                {
                    recordBytes[0] |= 0x80; // MB
                }
                else if (i == (n - 1))
                {
                    recordBytes[0] |= 0x40; // ME
                }

                recordsStream.Write(recordBytes, 0, recordBytes.Length);
            }

            byte[] recordsArray = recordsStream.ToArray();

            if (recordsArray != null)
            {
                int len = recordsArray.Length;

                if (len < 255)
                {
                    messageBytes = new byte[len + 2];
                    messageBytes[0] = 3; // NDEF Tag
                    messageBytes[1] = (byte)(len & 0xFF);
                    System.Array.Copy(recordsArray, 0, messageBytes, 2, len);
                }
                else
                {
                    messageBytes = new byte[len + 4];
                    messageBytes[0] = 3; // NDEF Tag
                    messageBytes[1] = 0xFF;
                    messageBytes[2] = (byte)((len >> 8) & 0xFF);
                    messageBytes[3] = (byte)(len & 0xFF);
                    System.Array.Copy(recordsArray, 0, messageBytes, 4, len);
                }
            }

            return messageBytes;
        }
    }
}
