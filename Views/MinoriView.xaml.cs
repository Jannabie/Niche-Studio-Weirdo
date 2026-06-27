using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class MinoriView : UserControl
    {
        public MinoriView()
        {
            InitializeComponent();
        }

        private void BrowsePaz_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PAZ Archives (*.paz)|*.paz|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) PazFileTxt.Text = d.FileName;
        }

        private void BrowseExtractFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) ExtractFolderTxt.Text = d.FolderName;
        }

        private void BrowseRepackOriginalPaz_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PAZ Archives (*.paz)|*.paz|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true)
                RepackOriginalPazTxt.Text = d.FileName;
        }

        private void BrowseRepackFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) RepackFolderTxt.Text = d.FolderName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private async void Unpack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PazFileTxt.Text)) return;

                int idx = GameIndexCombo.SelectedIndex;

                string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
                string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

                // fuckpaz extracts files FLAT into the working directory.
                // Set workDir to the output folder (or paz's parent if not specified).
                string workDir = !string.IsNullOrWhiteSpace(ExtractFolderTxt.Text)
                    ? ExtractFolderTxt.Text
                    : (Path.GetDirectoryName(PazFileTxt.Text) ?? repoDir);
                Directory.CreateDirectory(workDir);

                // Use ArgumentList to safely handle paths with spaces / em-dashes / special chars.
                await ToolRunner.RunAsync(workDir, exe, new[] { PazFileTxt.Text, idx.ToString() }, GetMain());
            }
            catch (Exception ex)
            {
                GetMain().LogToConsole($"✘ [ERROR] Unpack failed: {ex.Message}");
            }
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RepackOriginalPazTxt.Text) || 
                    string.IsNullOrWhiteSpace(RepackFolderTxt.Text) || 
                    string.IsNullOrWhiteSpace(OutputPazTxt.Text)) return;

                int idx = GameIndexCombo.SelectedIndex;

                string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
                string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

                string workDir = RepackFolderTxt.Text;
                string outPath = Path.Combine(workDir, OutputPazTxt.Text);

                // Use a unique temp path in the SYSTEM TEMP folder (not in workDir).
                // Files in workDir get scanned/locked by Windows Defender which causes
                // fuckpaz to crash with ACCESS_VIOLATION when it can't write the output.
                string tmpPath = Path.Combine(Path.GetTempPath(), $"fuckpaz_{Guid.NewGuid():N}.paz");

                GetMain().LogToConsole($"▶ Repacking via temp: {tmpPath}");

                await ToolRunner.RunAsync(workDir, exe,
                    new[] { RepackOriginalPazTxt.Text, idx.ToString(), tmpPath }, GetMain());

                // Move temp → final output if fuckpaz succeeded
                if (File.Exists(tmpPath) && new FileInfo(tmpPath).Length > 0)
                {
                    try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                    File.Move(tmpPath, outPath, overwrite: true);
                    GetMain().LogToConsole($"✔ Output saved to: {outPath}");
                }
                else
                {
                    try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
                    GetMain().LogToConsole("✘ [ERROR] Repack failed — check game index and that TARGET FOLDER directly contains the unpacked files.");
                }
            }
            catch (Exception ex)
            {
                GetMain().LogToConsole($"✘ [ERROR] Repack failed: {ex.Message}");
            }
        }


    }
}
