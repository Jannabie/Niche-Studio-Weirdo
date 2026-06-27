using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NicheStudioWeirdo.Utils
{
    public class BgiUniversalParser
    {
        public static string[] ExtractStrings(byte[] data)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            List<string> results = new List<string>();
            for (int i = 0; i < data.Length - 8; i++)
            {
                if (data[i] == 3 && data[i + 1] == 0 && data[i + 2] == 0 && data[i + 3] == 0)
                {
                    int ptr = BitConverter.ToInt32(data, i + 4);
                    if (ptr > 0 && ptr < data.Length)
                    {
                        int end = ptr;
                        while (end < data.Length && data[end] != 0) end++;
                        if (end > ptr && end < data.Length)
                        {
                            try
                            {
                                string s = Encoding.GetEncoding("shift-jis").GetString(data, ptr, end - ptr);
                                int jpChars = 0;
                                foreach (char c in s) if (c > 0x7F) jpChars++;
                                if (jpChars >= 1)
                                {
                                    results.Add(s);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            return results.ToArray();
        }

        public static byte[] InjectStrings(byte[] data, string[] translated)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            List<int> pointerOffsets = new List<int>();
            for (int i = 0; i < data.Length - 8; i++)
            {
                if (data[i] == 3 && data[i + 1] == 0 && data[i + 2] == 0 && data[i + 3] == 0)
                {
                    int ptr = BitConverter.ToInt32(data, i + 4);
                    if (ptr > 0 && ptr < data.Length)
                    {
                        int end = ptr;
                        while (end < data.Length && data[end] != 0) end++;
                        if (end > ptr && end < data.Length)
                        {
                            try
                            {
                                string s = Encoding.GetEncoding("shift-jis").GetString(data, ptr, end - ptr);
                                int jpChars = 0;
                                foreach (char c in s) if (c > 0x7F) jpChars++;
                                if (jpChars >= 1)
                                {
                                    pointerOffsets.Add(i + 4);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            if (pointerOffsets.Count != translated.Length)
            {
                throw new Exception("Mismatch: Found " + pointerOffsets.Count + " strings in script, but JSON has " + translated.Length + ".");
            }

            // Calculate new size
            int addedSize = 0;
            List<byte[]> tBytesList = new List<byte[]>();
            foreach (string t in translated)
            {
                byte[] tBytes = Encoding.GetEncoding("shift-jis").GetBytes(t + "\0");
                tBytesList.Add(tBytes);
                addedSize += tBytes.Length;
            }

            byte[] newData = new byte[data.Length + addedSize];
            Array.Copy(data, newData, data.Length);
            
            int currentAppendOffset = data.Length;
            for (int i = 0; i < pointerOffsets.Count; i++)
            {
                byte[] tBytes = tBytesList[i];
                Array.Copy(tBytes, 0, newData, currentAppendOffset, tBytes.Length);
                
                byte[] ptrBytes = BitConverter.GetBytes(currentAppendOffset);
                Array.Copy(ptrBytes, 0, newData, pointerOffsets[i], 4);
                
                currentAppendOffset += tBytes.Length;
            }

            return newData;
        }
    }
}
