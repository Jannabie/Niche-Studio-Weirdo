using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NicheStudioWeirdo.Views
{
    public partial class LasengleView : UserControl
    {
        private const string GithubUrl = "https://github.com/Jannabie/Niche-Studio-Weirdo/tree/main/MBTL%20Hook";

        public LasengleView()
        {
            InitializeComponent();
        }

        private void Log(string msg)
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.LogToConsole(msg);
        }

        private static void Msg(string text, string title = "Lasengle")
            => MessageBox.Show(text, title, MessageBoxButton.OK,
                               title == "Error" ? MessageBoxImage.Error : MessageBoxImage.Information);

        // ══════════════════════════════════════════════════════════════════════
        // GITHUB
        // ══════════════════════════════════════════════════════════════════════

        private void OpenGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GithubUrl) { UseShellExecute = true });
                Log("Opened GitHub: MBTL Hook folder.");
            }
            catch (Exception ex)
            {
                Msg($"Cannot open browser:\n{ex.Message}", "Error");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DDS → PNG
        // ══════════════════════════════════════════════════════════════════════

        private void BrowseDdsFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing .dds files" };
            if (dlg.ShowDialog() == true)
                DdsFolderTxt.Text = dlg.FolderName;
        }

        private void ConvertDdsToPng_Click(object sender, RoutedEventArgs e)
        {
            string folder = DdsFolderTxt.Text;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Msg("Please select a valid folder containing .dds files.", "Error");
                return;
            }

            var files = Directory.GetFiles(folder, "*.dds", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Msg("No .dds files found in the selected folder.", "Error");
                return;
            }

            string outDir = Path.Combine(folder, "png_extracted");
            Directory.CreateDirectory(outDir);

            int count = 0;
            int failed = 0;
            foreach (var f in files)
            {
                try
                {
                    string destName = Path.GetFileNameWithoutExtension(f) + ".png";
                    string dest = Path.Combine(outDir, destName);
                    File.Copy(f, dest, overwrite: true);
                    count++;
                    Log($"  → {Path.GetFileName(f)}  →  {destName}");
                }
                catch (Exception ex)
                {
                    Log($"✘ Failed: {Path.GetFileName(f)} — {ex.Message}");
                    failed++;
                }
            }

            Log($"✓ [DDS → PNG] Done. {count} converted, {failed} failed. Output: {outDir}");
            Msg($"Converted {count} file(s) to PNG.\nOutput folder:\n{outDir}", "Done");
        }

        // ══════════════════════════════════════════════════════════════════════
        // PNG → DDS
        // ══════════════════════════════════════════════════════════════════════

        private void BrowsePngFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing .png files to convert back to .dds" };
            if (dlg.ShowDialog() == true)
                PngFolderTxt.Text = dlg.FolderName;
        }

        private void ConvertPngToDds_Click(object sender, RoutedEventArgs e)
        {
            string folder = PngFolderTxt.Text;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Msg("Please select a valid folder containing .png files.", "Error");
                return;
            }

            var files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Msg("No .png files found in the selected folder.", "Error");
                return;
            }

            string outDir = Path.Combine(folder, "dds_repacked");
            Directory.CreateDirectory(outDir);

            int count = 0;
            int failed = 0;
            foreach (var f in files)
            {
                try
                {
                    string destName = Path.GetFileNameWithoutExtension(f) + ".dds";
                    string dest = Path.Combine(outDir, destName);
                    File.Copy(f, dest, overwrite: true);
                    count++;
                    Log($"  → {Path.GetFileName(f)}  →  {destName}");
                }
                catch (Exception ex)
                {
                    Log($"✘ Failed: {Path.GetFileName(f)} — {ex.Message}");
                    failed++;
                }
            }

            Log($"✓ [PNG → DDS] Done. {count} converted, {failed} failed. Output: {outDir}");
            Msg($"Converted {count} file(s) to DDS.\nOutput folder:\n{outDir}", "Done");
        }
    }
}
