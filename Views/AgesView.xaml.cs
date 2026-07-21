using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NicheStudioWeirdo.Views
{
    // ─── JSON model ────────────────────────────────────────────────────────────
    public class JlxEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("jp")]
        public string Jp { get; set; } = "";

        [JsonPropertyName("tl")]
        public string Tl { get; set; } = "";
    }

    // ─── View ──────────────────────────────────────────────────────────────────
    public partial class AgesView : UserControl
    {
        private const string DriveUrl =
            "https://drive.google.com/drive/folders/1WODb-fN5Q18jnOjRPvTveSrcT_Iwg3RQ?usp=sharing";

        public AgesView()
        {
            InitializeComponent();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void Log(string msg)
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.LogToConsole(msg);
        }

        private static void Msg(string text, string title = "AGES")
            => MessageBox.Show(text, title, MessageBoxButton.OK,
                               title == "Error" ? MessageBoxImage.Error : MessageBoxImage.Information);

        // ── Section 1 — Google Drive ─────────────────────────────────────────

        private void OpenDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(DriveUrl) { UseShellExecute = true });
                Log("Opened Google Drive: Hook Toolkit folder.");
            }
            catch (Exception ex)
            {
                Msg($"Cannot open browser:\n{ex.Message}", "Error");
            }
        }

        // ── Section 2 — Browse helpers ──────────────────────────────────────

        private void BrowseOrgi_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JLX Files (*.jlx)|*.jlx|All Files (*.*)|*.*", Title = "Select orgi.jlx" };
            if (dlg.ShowDialog() == true)
            {
                OrgiJlxTxt.Text = dlg.FileName;
                // Auto-suggest trans.jlx alongside
                string dir = Path.GetDirectoryName(dlg.FileName) ?? "";
                string transCandidate = Path.Combine(dir, "trans.jlx");
                if (File.Exists(transCandidate) && string.IsNullOrWhiteSpace(TransJlxTxt.Text))
                    TransJlxTxt.Text = transCandidate;
                // Auto-suggest output json
                if (string.IsNullOrWhiteSpace(ParseOutTxt.Text))
                    ParseOutTxt.Text = Path.Combine(dir, "script.json");
            }
        }

        private void BrowseTrans_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JLX Files (*.jlx)|*.jlx|All Files (*.*)|*.*", Title = "Select trans.jlx" };
            if (dlg.ShowDialog() == true) TransJlxTxt.Text = dlg.FileName;
        }

        private void BrowseParseOut_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "JSON Files (*.json)|*.json", Title = "Save output JSON", FileName = "script.json" };
            if (dlg.ShowDialog() == true) ParseOutTxt.Text = dlg.FileName;
        }

        // ── Section 2 — Parse JLX → JSON ────────────────────────────────────

        private async void ParseJlx_Click(object sender, RoutedEventArgs e)
        {
            string orgiPath  = OrgiJlxTxt.Text.Trim();
            string transPath = TransJlxTxt.Text.Trim();
            string outPath   = ParseOutTxt.Text.Trim();

            if (!File.Exists(orgiPath))  { Msg("Please select a valid orgi.jlx file.",  "Error"); return; }
            if (!File.Exists(transPath)) { Msg("Please select a valid trans.jlx file.", "Error"); return; }
            if (string.IsNullOrWhiteSpace(outPath)) { Msg("Please specify an output JSON path.", "Error"); return; }

            Log($"Parsing JLX files → {Path.GetFileName(outPath)} …");

            try
            {
                int count = await Task.Run(() => ParseJlxToJson(orgiPath, transPath, outPath));
                Msg($"Parsed {count:N0} lines.\nSaved to:\n{outPath}", "Parse Complete");
                Log($"Done — {count:N0} entries written to {outPath}");
            }
            catch (Exception ex)
            {
                Msg($"Error during parsing:\n{ex.Message}", "Error");
                Log($"Parse error: {ex.Message}");
            }
        }

        private static int ParseJlxToJson(string orgiPath, string transPath, string outPath)
        {
            string orgiText  = File.ReadAllText(orgiPath,  Encoding.Unicode);
            string transText = File.ReadAllText(transPath, Encoding.Unicode);

            string[] orgiLines  = orgiText.Split(new[] { ":::::" }, StringSplitOptions.None);
            string[] transLines = transText.Split(new[] { ":::::" }, StringSplitOptions.None);

            int count = Math.Min(orgiLines.Length, transLines.Length);
            var entries = new List<JlxEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new JlxEntry
                {
                    Id = i,
                    Jp = orgiLines[i],
                    Tl = transLines[i]
                });
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(entries, options);
            File.WriteAllText(outPath, json, Encoding.UTF8);
            return count;
        }

        // ── Section 3 — Browse helpers ──────────────────────────────────────

        private void BrowseRepackJson_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*", Title = "Select translated JSON file" };
            if (dlg.ShowDialog() == true)
            {
                RepackJsonTxt.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(RepackOutTxt.Text))
                    RepackOutTxt.Text = Path.Combine(Path.GetDirectoryName(dlg.FileName) ?? "", "trans.jlx");
            }
        }

        private void BrowseRepackOut_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "JLX Files (*.jlx)|*.jlx|All Files (*.*)|*.*", Title = "Save trans.jlx", FileName = "trans.jlx" };
            if (dlg.ShowDialog() == true) RepackOutTxt.Text = dlg.FileName;
        }

        // ── Section 3 — Repack JSON → JLX ───────────────────────────────────

        private async void RepackJson_Click(object sender, RoutedEventArgs e)
        {
            string jsonPath = RepackJsonTxt.Text.Trim();
            string outPath  = RepackOutTxt.Text.Trim();

            if (!File.Exists(jsonPath)) { Msg("Please select a valid JSON file.", "Error"); return; }
            if (string.IsNullOrWhiteSpace(outPath)) { Msg("Please specify an output trans.jlx path.", "Error"); return; }

            Log($"Repacking {Path.GetFileName(jsonPath)} → trans.jlx …");

            try
            {
                int count = await Task.Run(() => RepackJsonToJlx(jsonPath, outPath));
                Msg($"Repacked {count:N0} lines.\nSaved to:\n{outPath}", "Repack Complete");
                Log($"Done — {count:N0} entries written to {outPath}");
            }
            catch (Exception ex)
            {
                Msg($"Error during repack:\n{ex.Message}", "Error");
                Log($"Repack error: {ex.Message}");
            }
        }

        private static int RepackJsonToJlx(string jsonPath, string outPath)
        {
            string jsonText = File.ReadAllText(jsonPath, Encoding.UTF8);
            var entries = JsonSerializer.Deserialize<List<JlxEntry>>(jsonText)
                          ?? throw new InvalidDataException("Failed to deserialize JSON.");

            // Sort by id to preserve original order
            entries.Sort((a, b) => a.Id.CompareTo(b.Id));

            string combined = string.Join(":::::", entries.Select(e => e.Tl));

            // Write UTF-16 LE without BOM (same as original jlx format)
            using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(fs, new UnicodeEncoding(false, false));
            sw.Write(combined);

            return entries.Count;
        }
    }
}
