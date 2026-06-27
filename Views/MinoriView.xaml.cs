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

                // Write to a .tmp file first so we never try to overwrite a locked existing file.
                // fuckpaz crashes with ACCESS_VIOLATION if the output file exists and is locked.
                string tmpPath = outPath + ".repack_tmp";
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }

                GetMain().LogToConsole($"▶ Repacking to temp: {tmpPath}");

                await ToolRunner.RunAsync(workDir, exe,
                    new[] { RepackOriginalPazTxt.Text, idx.ToString(), tmpPath }, GetMain());

                // If fuckpaz succeeded, rename temp → final output
                if (File.Exists(tmpPath) && new FileInfo(tmpPath).Length > 0)
                {
                    try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                    File.Move(tmpPath, outPath, overwrite: true);
                    GetMain().LogToConsole($"✔ Output saved to: {outPath}");
                }
                else if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                    GetMain().LogToConsole("✘ [ERROR] fuckpaz produced an empty output file. Check game index and target folder.");
                }
            }
            catch (Exception ex)
            {
                GetMain().LogToConsole($"✘ [ERROR] Repack failed: {ex.Message}");
            }
        }

    }
}
