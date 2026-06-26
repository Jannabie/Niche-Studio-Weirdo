using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NicheStudioWeirdo.Utils
{
    public class ScfTextSegment
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
        [JsonPropertyName("length")]
        public int Length { get; set; }
        [JsonPropertyName("original")]
        public List<int> Original { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ScfParsedData
    {
        [JsonPropertyName("original_data")]
        public List<int> OriginalData { get; set; }
        [JsonPropertyName("size")]
        public int Size { get; set; }
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }
        [JsonPropertyName("text_segments")]
        public List<ScfTextSegment> TextSegments { get; set; }
    }

    public class AbogadoScfParser
    {
        static AbogadoScfParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static void Extract(string inputScf, string outputDir, Action<string> log)
        {
            log($"[*] Extracting: {inputScf}");
            
            byte[] data = File.ReadAllBytes(inputScf);
            var encoding = Encoding.GetEncoding("shift_jis");
            var segments = new List<ScfTextSegment>();

            int i = 0;
            while (i < data.Length)
            {
                int start = i;
                var segment = new List<int>();

                while (i < data.Length && data[i] != 0x00)
                {
                    segment.Add(data[i]);
                    i++;
                }

                if (i < data.Length && data[i] == 0x00)
                {
                    segment.Add(0x00);
                    i++;
                }

                if (segment.Count > 1)
                {
                    var textContent = segment.GetRange(0, segment.Count - 1).Select(b => (byte)b).ToArray();
                    if (textContent.Length > 0)
                    {
                        string decoded = encoding.GetString(textContent);
                        
                        bool hasJapanese = false;
                        foreach (char c in decoded)
                        {
                            if ((c >= '\u3040' && c <= '\u30ff') || // Hiragana/Katakana
                                (c >= '\u4e00' && c <= '\u9fff') || // Kanji
                                (c >= '\u3000' && c <= '\u303f') || // CJK Symbols and Punctuation
                                (c >= '\uff00' && c <= '\uffef'))   // Halfwidth and Fullwidth Forms
                            {
                                hasJapanese = true;
                                break;
                            }
                        }

                        if (hasJapanese)
                        {
                            segments.Add(new ScfTextSegment
                            {
                                Offset = start,
                                Length = segment.Count,
                                Original = segment,
                                Text = decoded
                            });
                        }
                    }
                }
            }

            var parsed = new ScfParsedData
            {
                OriginalData = new List<int>(data.Select(b => (int)b)),
                Size = data.Length,
                Encoding = "shift_jis",
                TextSegments = segments
            };

            Directory.CreateDirectory(outputDir);
            string baseName = Path.GetFileNameWithoutExtension(inputScf);
            string jsonPath = Path.Combine(outputDir, $"{baseName}.json");
            string txtPath = Path.Combine(outputDir, $"{baseName}.txt");

            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            string jsonString = JsonSerializer.Serialize(parsed, options);
            File.WriteAllText(jsonPath, jsonString, new UTF8Encoding(false));

            using (var writer = new StreamWriter(txtPath, false, new UTF8Encoding(false)))
            {
                foreach (var seg in segments)
                {
                    writer.WriteLine(seg.Text.Replace("\r", "\\r").Replace("\n", "\\n"));
                }
            }

            log($"   JSON: {jsonPath}");
            log($"   TXT:  {txtPath}");
            log($"   Found {segments.Count} text segments");
            log("[OK] Done!");
        }

        public static void Rebuild(string jsonPath, string txtPath, string outputScf, Action<string> log)
        {
            log($"[*] Rebuilding: {jsonPath}");
            if (!string.IsNullOrWhiteSpace(txtPath) && File.Exists(txtPath))
                log($"   With translation: {txtPath}");

            string jsonContent = File.ReadAllText(jsonPath);
            var parsedData = JsonSerializer.Deserialize<ScfParsedData>(jsonContent);

            List<string> newTexts = null;
            if (!string.IsNullOrWhiteSpace(txtPath) && File.Exists(txtPath))
            {
                newTexts = new List<string>();
                using (var reader = new StreamReader(txtPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        newTexts.Add(line.Replace("\\r", "\r").Replace("\\n", "\n"));
                    }
                }
            }

            var data = new List<byte>(parsedData.OriginalData.Select(i => (byte)i));
            var encoding = Encoding.GetEncoding(parsedData.Encoding ?? "shift_jis");

            if (newTexts != null)
            {
                var segments = parsedData.TextSegments;
                if (newTexts.Count != segments.Count)
                {
                    log($"[!] Warning: Text count mismatch! Expected {segments.Count}, got {newTexts.Count}");
                }

                int limit = Math.Min(segments.Count, newTexts.Count);
                for (int i = limit - 1; i >= 0; i--)
                {
                    var seg = segments[i];
                    string newText = newTexts[i];

                    try
                    {
                        byte[] newBytes = encoding.GetBytes(newText);
                        var newByteList = new List<byte>(newBytes);
                        newByteList.Add(0x00);

                        int offset = seg.Offset;
                        int oldLength = seg.Length;

                        data.RemoveRange(offset, oldLength);
                        data.InsertRange(offset, newByteList);
                    }
                    catch (Exception ex)
                    {
                        log($"[ERROR] Error encoding text at offset {seg.Offset}: {ex.Message}");
                    }
                }
            }

            File.WriteAllBytes(outputScf, data.ToArray());
            log($"[OK] Created: {outputScf} ({data.Count} bytes)");
        }
    }
}
