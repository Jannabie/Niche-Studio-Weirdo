using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class AgesView : UserControl
    {
        public AgesView()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void Log(string msg)
        {
            // Forward to the global console if accessible
            if (Application.Current.MainWindow is MainWindow mw)
                mw.LogToConsole(msg);
        }

        private static void Msg(string text, string title = "AGES")
            => MessageBox.Show(text, title, MessageBoxButton.OK,
                               title == "Error" ? MessageBoxImage.Error : MessageBoxImage.Information);

        // ─────────────────────────────────────────────────────────────────────
        // Browse helpers
        // ─────────────────────────────────────────────────────────────────────

        private void BrowseIci_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "AGES Index Files (*.ici)|*.ici|All Files (*.*)|*.*",
                Title  = "Select .ici index file"
            };
            if (dlg.ShowDialog() == true) IciPathTxt.Text = dlg.FileName;
        }

        private void BrowseRio_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "AGES Archive Files (*.rio;*.rio.*)|*.rio;*.rio.*|All Files (*.*)|*.*",
                Title  = "Select .rio archive file"
            };
            if (dlg.ShowDialog() == true)
            {
                RioPathTxt.Text = dlg.FileName;
                // Auto-fill output folder with same directory
                if (string.IsNullOrWhiteSpace(RioOutTxt.Text))
                    RioOutTxt.Text = Path.GetDirectoryName(dlg.FileName) ?? "";
            }
        }

        private void BrowseRioOut_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select output folder for extracted script" };
            if (dlg.ShowDialog() == true) RioOutTxt.Text = dlg.FolderName;
        }

        private void BrowseBatchFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select game folder containing .rio files" };
            if (dlg.ShowDialog() == true) BatchFolderTxt.Text = dlg.FolderName;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section 1 — ICI Inspector
        // ─────────────────────────────────────────────────────────────────────

        private async void InspectIci_Click(object sender, RoutedEventArgs e)
        {
            string iciPath = IciPathTxt.Text.Trim();
            if (!File.Exists(iciPath))
            {
                Msg("Please select a valid .ici file.", "Error");
                return;
            }

            IciResultTxt.Text = "Decrypting .ici …";
            Log($"Inspecting ICI: {iciPath}");

            try
            {
                string summary = await Task.Run(() => AgesRioDecoder.GetIciSummary(iciPath));
                IciResultTxt.Text = summary;
                Log("ICI inspection complete.");
            }
            catch (Exception ex)
            {
                IciResultTxt.Text = $"Error: {ex.Message}";
                Log($"ICI Error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section 2 — Single .rio script extraction
        // ─────────────────────────────────────────────────────────────────────

        private async void ExtractScript_Click(object sender, RoutedEventArgs e)
        {
            string rioPath = RioPathTxt.Text.Trim();
            string outDir  = RioOutTxt.Text.Trim();

            if (!File.Exists(rioPath))
            {
                Msg("Please select a valid .rio file.", "Error");
                return;
            }
            if (string.IsNullOrWhiteSpace(outDir))
                outDir = Path.GetDirectoryName(rioPath)!;

            Directory.CreateDirectory(outDir);

            string outFile = Path.Combine(outDir,
                Path.GetFileName(rioPath) + "_script.txt");

            Log($"Extracting scripts from: {Path.GetFileName(rioPath)}");

            var progress = new Progress<string>(msg => Log(msg));

            try
            {
                int count = await Task.Run(
                    () => AgesRioDecoder.ExportScriptToTxt(rioPath, outFile, progress));

                Msg($"Extracted {count} strings.\nSaved to:\n{outFile}",
                    "Extraction Complete");
                Log($"Done — {count} strings exported to {outFile}");
            }
            catch (Exception ex)
            {
                Msg($"Error during extraction:\n{ex.Message}", "Error");
                Log($"Extraction error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section 3 — Batch extraction
        // ─────────────────────────────────────────────────────────────────────

        private async void BatchExtract_Click(object sender, RoutedEventArgs e)
        {
            string folder = BatchFolderTxt.Text.Trim();
            if (!Directory.Exists(folder))
            {
                Msg("Please select a valid game folder.", "Error");
                return;
            }

            // Collect all .rio files in the folder (including .rio.002, .rio.003 etc.)
            string[] rioFiles = Directory.GetFiles(folder, "*.rio*",
                SearchOption.TopDirectoryOnly);

            if (rioFiles.Length == 0)
            {
                Msg("No .rio files found in the selected folder.", "Error");
                return;
            }

            string outDir = Path.Combine(folder, "ages_script_export");
            Directory.CreateDirectory(outDir);

            Log($"Batch extract: {rioFiles.Length} .rio files found in {folder}");

            var progress = new Progress<string>(msg => Log(msg));
            int totalStrings = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (string rioPath in rioFiles)
                    {
                        string outFile = Path.Combine(outDir,
                            Path.GetFileName(rioPath) + "_script.txt");
                        int count = AgesRioDecoder.ExportScriptToTxt(rioPath, outFile, progress);
                        totalStrings += count;
                    }
                });

                Msg($"Batch extraction complete!\n\n" +
                    $"Files processed : {rioFiles.Length}\n" +
                    $"Total strings   : {totalStrings}\n" +
                    $"Output folder   : {outDir}",
                    "Batch Complete");

                Log($"Batch done — {totalStrings} strings total from {rioFiles.Length} files.");
            }
            catch (Exception ex)
            {
                Msg($"Batch error:\n{ex.Message}", "Error");
                Log($"Batch error: {ex.Message}");
            }
        }
    }
}
