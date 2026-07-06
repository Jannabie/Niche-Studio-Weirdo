using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace NicheStudioWeirdo.Utils
{
    public class NutTranslationEntry
    {
        public int Offset { get; set; }
        public string Original { get; set; }
        public string Translated { get; set; }
        /// <summary>"utf8" or "sjis" — detected automatically during extraction, stored so inject uses the same encoding.</summary>
        public string Encoding { get; set; } = "sjis";
    }

    public static class NitroplusNutTool
    {
        static NitroplusNutTool()
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // ── Encoding Detection ─────────────────────────────────────────────────────
        /// <summary>
        /// Detects whether a byte span is UTF-8 or Shift-JIS.
        /// Pure ASCII is treated as SJIS (compatible with both).
        /// Any high-byte content that forms valid UTF-8 multi-byte sequences → UTF-8.
        /// Otherwise → Shift-JIS.
        /// </summary>
        private static System.Text.Encoding DetectEncoding(byte[] data, int offset, int length)
        {
            bool hasNonAscii = false;
            for (int i = offset; i < offset + length; i++)
                if (data[i] >= 0x80) { hasNonAscii = true; break; }

            // Pure ASCII — ambiguous, safe to use either. Default SJIS.
            if (!hasNonAscii)
                return System.Text.Encoding.GetEncoding(932);

            // Try strict UTF-8 (throws on any invalid byte sequence)
            try
            {
                var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                strictUtf8.GetString(data, offset, length);
                return System.Text.Encoding.UTF8;
            }
            catch (DecoderFallbackException) { }

            // Fallback → Shift-JIS
            return System.Text.Encoding.GetEncoding(932);
        }

        // ── Core Extraction ────────────────────────────────────────────────────────
        private static List<NutTranslationEntry> ExtractInternal(string nutFilePath)
        {
            byte[] data = File.ReadAllBytes(nutFilePath);
            var entries = new List<NutTranslationEntry>();

            // Safely scan for OT_STRING markers in Squirrel bytecode (0x08000010 LE → 10 00 00 08)
            for (int i = 0; i < data.Length - 8; i++)
            {
                if (data[i] == 0x10 && data[i + 1] == 0x00 && data[i + 2] == 0x00 && data[i + 3] == 0x08)
                {
                    int lenOffset = i + 4;
                    int len = BitConverter.ToInt32(data, lenOffset);

                    if (len <= 0 || len >= 10000 || lenOffset + 4 + len > data.Length)
                        continue;

                    // Auto-detect encoding for this string block
                    var enc = DetectEncoding(data, lenOffset + 4, len);
                    string text;
                    try { text = enc.GetString(data, lenOffset + 4, len); }
                    catch { continue; }

                    // Only include strings with meaningful text content
                    bool isText = false;
                    foreach (char c in text)
                    {
                        if (char.IsLetterOrDigit(c) || c > 127 || char.IsPunctuation(c))
                        { isText = true; break; }
                    }

                    if (isText)
                    {
                        entries.Add(new NutTranslationEntry
                        {
                            Offset    = lenOffset,
                            Original  = text,
                            Translated = text,
                            Encoding  = enc == System.Text.Encoding.UTF8 ? "utf8" : "sjis"
                        });
                    }
                }
            }
            return entries;
        }

        // ── Public API ─────────────────────────────────────────────────────────────
        public static void ExtractToJson(string nutFilePath, string jsonFilePath)
        {
            var entries = ExtractInternal(nutFilePath);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(entries, options);
            File.WriteAllText(jsonFilePath, json, System.Text.Encoding.UTF8);
        }

        public static void InjectFromJson(string originalNutPath, string jsonFilePath, string outputNutPath)
        {
            string json = File.ReadAllText(jsonFilePath, System.Text.Encoding.UTF8);
            var entries = JsonSerializer.Deserialize<List<NutTranslationEntry>>(json);
            InjectInternal(originalNutPath, outputNutPath, entries);
        }

        // ── Injection ──────────────────────────────────────────────────────────────
        private static void InjectInternal(string originalNutPath, string outputNutPath, List<NutTranslationEntry> entries)
        {
            byte[] data = File.ReadAllBytes(originalNutPath);
            var sjis = System.Text.Encoding.GetEncoding(932);

            if (entries == null || entries.Count == 0)
            {
                File.Copy(originalNutPath, outputNutPath, true);
                return;
            }

            // Sort by offset ascending so we patch forward-only
            entries.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            int currentIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                int bytesToWrite = entry.Offset - currentIndex;
                if (bytesToWrite < 0) continue; // Safety: skip overlapping entries

                bw.Write(data, currentIndex, bytesToWrite);

                // Use the encoding that was originally detected for this string
                var enc = string.Equals(entry.Encoding, "utf8", StringComparison.OrdinalIgnoreCase)
                    ? (System.Text.Encoding)System.Text.Encoding.UTF8
                    : sjis;

                string textToInject = string.IsNullOrEmpty(entry.Translated)
                    ? entry.Original
                    : entry.Translated;

                byte[] newStrBytes;
                try { newStrBytes = enc.GetBytes(textToInject); }
                catch
                {
                    // If encoding fails (e.g. char not in SJIS), fall back to UTF-8
                    newStrBytes = System.Text.Encoding.UTF8.GetBytes(textToInject);
                }

                bw.Write((int)newStrBytes.Length);
                bw.Write(newStrBytes);

                // Advance past the ORIGINAL string in the source data
                int originalLen = BitConverter.ToInt32(data, entry.Offset);
                currentIndex = entry.Offset + 4 + originalLen;
            }

            // Write any remaining data after the last patched string
            if (currentIndex < data.Length)
                bw.Write(data, currentIndex, data.Length - currentIndex);

            byte[] newData = ms.ToArray();

            // Update SCRP header sizes if present
            if (newData.Length >= 16
                && newData[0] == 'S' && newData[1] == 'C'
                && newData[2] == 'R' && newData[3] == 'P')
            {
                int headerSize   = BitConverter.ToInt32(newData, 4);
                int payloadSize  = newData.Length - headerSize;

                Buffer.BlockCopy(BitConverter.GetBytes(payloadSize), 0, newData, 8,  4);
                // In Muramasa, TotalSize does not include the 4-byte SCRP magic signature
                Buffer.BlockCopy(BitConverter.GetBytes(newData.Length - 4), 0, newData, 12, 4);
            }

            File.WriteAllBytes(outputNutPath, newData);
        }
    }
}
