using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NicheStudioWeirdo.Utils
{
    public static class LeafTxtTool
    {
        public static void ParseCsvToTxt(string inputCsvFile, string outputTxtFile)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding enc = Encoding.GetEncoding("shift_jis");

            string content = File.ReadAllText(inputCsvFile, enc);
            List<string> entries = ParseFlatCsv(content);

            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                sb.AppendLine($"// [{i:D4}]");
                sb.AppendLine(entries[i].Replace("\\n", Environment.NewLine));
                sb.AppendLine();
            }

            File.WriteAllText(outputTxtFile, sb.ToString(), new UTF8Encoding(true));
        }

        public static void InjectTxtToCsv(string translatedTxtFile, string outputCsvFile)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding enc = Encoding.GetEncoding("shift_jis");

            string[] lines = File.ReadAllLines(translatedTxtFile, Encoding.UTF8);
            List<string> entries = new List<string>();

            StringBuilder currentEntry = new StringBuilder();
            bool insideEntry = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("// ["))
                {
                    if (insideEntry)
                    {
                        string content = currentEntry.ToString().TrimEnd('\r', '\n');
                        content = content.Replace(Environment.NewLine, "\\n").Replace("\n", "\\n");
                        entries.Add(content);
                        currentEntry.Clear();
                    }
                    insideEntry = true;
                    continue;
                }

                if (insideEntry)
                {
                    currentEntry.AppendLine(line);
                }
            }
            // Add the last entry
            if (insideEntry)
            {
                string content = currentEntry.ToString().TrimEnd('\r', '\n');
                content = content.Replace(Environment.NewLine, "\\n").Replace("\n", "\\n");
                entries.Add(content);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                string text = entries[i];
                
                // For translators using actual commas in TXT, replace with ~ for game engine safety
                text = text.Replace(",", "~");

                sb.Append(text);
                
                if (i < entries.Count - 1)
                {
                    sb.Append(",");
                }
            }

            File.WriteAllText(outputCsvFile, sb.ToString(), enc);
        }

        private static List<string> ParseFlatCsv(string content)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder current = new StringBuilder();
            
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            
            return result;
        }
    }
}
