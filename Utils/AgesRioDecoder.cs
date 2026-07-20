using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NicheStudioWeirdo.Utils
{
    /// <summary>
    /// Handles decryption and extraction of AGES engine 7.0 (rUGP) .rio/.ici archives.
    /// Used by games such as Schwarzesmarken (Muv-Luv series) by âge.
    ///
    /// Reverse-engineered from GARbro (ArcRIO.cs) by morkt,
    /// with corrections to the key rotation algorithm for AGES 7.0.
    ///
    /// Archive format:
    ///   - muv_schB.rio.ici  : Encrypted index (Table of Contents)
    ///   - muv_schB.rio      : Part 1 of the archive data
    ///   - muv_schB.rio.002  : Part 2
    ///   - muv_schB.rio.003  : Part 3
    /// </summary>
    public static class AgesRioDecoder
    {
        // ──────────────────────────────────────────────────────────────────────
        // Constants (from GARbro ArcRIO.cs)
        // ──────────────────────────────────────────────────────────────────────
        private const uint IciKey              = 0xB29D5A0C;
        private const uint SizeMask1           = 0xC92E568B;
        private const uint SizeMask2           = 0xC92E568F;
        private const uint KeyRotateConstant   = 0xA3B376C9;

        static AgesRioDecoder()
        {
            // Register encoding provider for Shift-JIS (932) support in .NET Core / .NET 5+
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Public entry points
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Decrypts a .ici file and returns the raw (plaintext) TOC bytes.
        /// </summary>
        public static byte[] DecryptIci(string iciPath)
        {
            byte[] fileBytes = File.ReadAllBytes(iciPath);
            using var m_input = new BinaryReader(new MemoryStream(fileBytes));

            uint size1 = ~(m_input.ReadUInt32() ^ SizeMask1);
            uint size2 = (m_input.ReadUInt32() ^ SizeMask2) >> 3;

            if (size1 != size2)
                throw new InvalidDataException($"ICI size mismatch: size1=0x{size1:X} size2=0x{size2:X}. File may be corrupt.");

            byte[] payload = ReadEncryptedPayload(m_input, IciKey, size1);
            return ApplyIciPermutations(payload);
        }

        /// <summary>
        /// Parses the decrypted ICI bytes and returns a list of arc entries.
        /// Each entry contains: RioFileName, DiskLabel, and (optionally) file entries.
        /// </summary>
        public static List<AgesDiskInfo> ParseIci(byte[] iciData)
        {
            var disks = new List<AgesDiskInfo>();

            // Scan for all UTF-16 LE strings (prefixed with FF FE FF <length_byte>)
            var strings = ExtractUtf16Strings(iciData);

            // Group into disk blocks: each disk has a disk label (e.g. "age\muvluv_schwaB 001")
            // and a rio filename (e.g. "muv_schB.rio")
            AgesDiskInfo? current = null;
            foreach (var (pos, s) in strings)
            {
                if (s.Contains(".rio"))
                {
                    if (current != null && current.RioFileName == "")
                        current.RioFileName = s.Trim();
                    else if (current != null && current.DataRioFileName == "")
                        current.DataRioFileName = s.Trim();
                    else
                    {
                        // New entry — first .rio string is always the TOC reference
                        if (current != null) disks.Add(current);
                        current = new AgesDiskInfo { RioFileName = s.Trim() };
                    }
                }
                else if (s.Contains("age\\") || s.Contains("age/"))
                {
                    if (current == null) current = new AgesDiskInfo();
                    current.DiskLabel = s.Trim();
                }
                else if (s.Contains(".txt") || s.Contains(".dat") || s.Contains(".bin"))
                {
                    current?.LooseFiles.Add(s.Trim());
                }
            }
            if (current != null) disks.Add(current);

            return disks;
        }

        /// <summary>
        /// Extracts all script text strings from a .rio archive part using the TOC from the ICI.
        /// This scans the raw .rio data for readable Shift-JIS / UTF-16 dialogue patterns
        /// compatible with the CRsa (AGES script) format.
        /// </summary>
        public static List<string> ExtractScriptStrings(string rioPath, IProgress<string>? progress = null)
        {
            progress?.Report($"Reading {Path.GetFileName(rioPath)}...");
            byte[] data = File.ReadAllBytes(rioPath);

            var results = new List<string>();

            // AGES script text is stored as length-prefixed Shift-JIS or UTF-16 strings
            // Pattern: look for common VN script text — JP characters clusters (Hiragana/Katakana/CJK)
            var enc932 = Encoding.GetEncoding(932);
            string fullText = enc932.GetString(data);

            // Match Japanese text sequences that look like dialogue (4+ chars, mixed JP)
            var matches = Regex.Matches(fullText,
                @"[\u3040-\u9FFF\uFF00-\uFFEF]{4,}[^\x00-\x1F\x7F]*");

            int count = 0;
            foreach (Match m in matches)
            {
                string line = m.Value.Trim();
                if (line.Length >= 2 && line.Length < 500)
                {
                    results.Add(line);
                    count++;
                }
            }

            progress?.Report($"Found {count} strings in {Path.GetFileName(rioPath)}.");
            return results;
        }

        /// <summary>
        /// Exports script strings from a .rio file to a plain text file.
        /// </summary>
        public static int ExportScriptToTxt(string rioPath, string outputTxtPath,
            IProgress<string>? progress = null)
        {
            var strings = ExtractScriptStrings(rioPath, progress);

            using var writer = new StreamWriter(outputTxtPath, false, new UTF8Encoding(false));
            writer.WriteLine($"# AGES Script Export — {Path.GetFileName(rioPath)}");
            writer.WriteLine($"# Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"# Total strings: {strings.Count}");
            writer.WriteLine();

            int idx = 0;
            foreach (var s in strings)
            {
                writer.WriteLine($"[{idx:D5}] {s}");
                idx++;
            }

            progress?.Report($"Exported {strings.Count} strings to {Path.GetFileName(outputTxtPath)}");
            return strings.Count;
        }

        /// <summary>
        /// Returns summary information about a .ici file for display in the UI.
        /// </summary>
        public static string GetIciSummary(string iciPath)
        {
            try
            {
                byte[] decrypted = DecryptIci(iciPath);
                var disks = ParseIci(decrypted);

                var sb = new StringBuilder();
                sb.AppendLine($"ICI Decrypted OK — {decrypted.Length:N0} bytes");
                sb.AppendLine();

                if (disks.Count == 0)
                {
                    sb.AppendLine("No disk entries found.");
                    return sb.ToString();
                }

                foreach (var d in disks)
                {
                    sb.AppendLine($"  Disk : {d.DiskLabel}");
                    sb.AppendLine($"  File : {d.RioFileName}");
                    if (!string.IsNullOrEmpty(d.DataRioFileName))
                        sb.AppendLine($"  Data : {d.DataRioFileName}");
                    foreach (var f in d.LooseFiles)
                        sb.AppendLine($"    └ {f}");
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Internal helpers
        // ──────────────────────────────────────────────────────────────────────

        private static byte[] ReadEncryptedPayload(BinaryReader reader, uint key, uint size)
        {
            var data = new byte[size];
            int dst = 0;
            while (dst < data.Length)
            {
                int portion = Math.Min(0x20, data.Length - dst);
                byte[] raw = reader.ReadBytes(portion);
                ushort checksum = 0;

                for (int i = portion; i > 0; --i)
                {
                    byte b = (byte)(raw[portion - i] ^ key);
                    data[dst++] = b;
                    checksum += (ushort)(b * i);

                    // GARbro key rotation (ArcRIO.cs line 870-871)
                    uint bit = (key >> 15) & 1;
                    key = ~(bit + key * 2 + KeyRotateConstant);
                }

                if (portion < 0x20) break;

                ushort fileChecksum = reader.ReadUInt16();
                if (fileChecksum != checksum)
                    throw new InvalidDataException(
                        $"Encrypted chunk checksum mismatch at offset {dst}: computed=0x{checksum:X4}, file=0x{fileChecksum:X4}");
            }
            return data;
        }

        /// <summary>
        /// Applies the three permutation passes from GARbro's DecryptIci method.
        /// </summary>
        private static byte[] ApplyIciPermutations(byte[] input)
        {
            var output = new byte[input.Length];
            int src = 0, dst = 0, tail;

            // Pass 1 — interleave 6-column de-shuffle
            int n6 = Math.DivRem(input.Length, 6, out tail);
            for (int n = n6; n > 0; --n)
            {
                output[dst++] = input[src];
                output[dst++] = input[src + n6];
                output[dst++] = input[src + n6 * 2];
                output[dst++] = input[src + n6 * 3];
                output[dst++] = input[src + n6 * 4];
                output[dst++] = input[src + n6 * 5];
                ++src;
            }
            if (tail > 0) Buffer.BlockCopy(input, input.Length - tail, output, dst, tail);

            byte acc = 0;
            for (int i = 0; i < output.Length; ++i)
            {
                output[i] -= acc;
                acc += output[i];
                output[i] ^= 0xA5;
            }

            // Pass 2 — interleave 5-column de-shuffle
            src = 0; dst = 0;
            int n5 = Math.DivRem(input.Length, 5, out tail);
            for (int n = n5; n > 0; --n)
            {
                input[dst++] = output[src];
                input[dst++] = output[src + n5];
                input[dst++] = output[src + n5 * 2];
                input[dst++] = output[src + n5 * 3];
                input[dst++] = output[src + n5 * 4];
                ++src;
            }
            if (tail > 0) Buffer.BlockCopy(output, output.Length - tail, input, dst, tail);

            acc = 0;
            for (int i = input.Length - 1; i >= 0; --i)
            {
                input[i] -= acc;
                acc += input[i];
            }

            // Pass 3 — interleave 3-column de-shuffle with XOR bytes
            src = 0; dst = 0;
            int n3 = Math.DivRem(input.Length, 3, out tail);
            for (int n = n3; n > 0; --n)
            {
                output[dst++] = (byte)(input[src] ^ 0x18);
                output[dst++] = (byte)(input[src + n3] ^ 0x3F);
                output[dst++] = (byte)(input[src + n3 * 2] ^ 0xE2);
                ++src;
            }
            if (tail > 0) Buffer.BlockCopy(input, input.Length - tail, output, dst, tail);

            return output;
        }

        private static List<(int pos, string val)> ExtractUtf16Strings(byte[] data)
        {
            var result = new List<(int, string)>();
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xFE && data[i + 2] == 0xFF)
                {
                    int len = data[i + 3];
                    if (len > 0 && len < 0x80 && (i + 4 + len * 2) <= data.Length)
                    {
                        string s = Encoding.Unicode.GetString(data, i + 4, len * 2);
                        bool valid = true;
                        foreach (char c in s) if (c < 0x20 && c != 0) { valid = false; break; }
                        if (valid) result.Add((i, s));
                    }
                }
            }
            return result;
        }
    }

    public class AgesDiskInfo
    {
        public string DiskLabel       { get; set; } = "";
        public string RioFileName     { get; set; } = "";
        public string DataRioFileName { get; set; } = "";
        public List<string> LooseFiles { get; set; } = new();
    }
}
