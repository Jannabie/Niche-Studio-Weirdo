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
            {
                RepackOriginalPazTxt.Text = d.FileName;
                // Auto-fill the base folder to be the parent directory of the PAZ file
                RepackFolderTxt.Text = Path.GetDirectoryName(d.FileName) ?? "";
            }
        }

        private void BrowseRepackFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) RepackFolderTxt.Text = d.FolderName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private async void Unpack_Click(object sender, RoutedEventArgs e)
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

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RepackOriginalPazTxt.Text) || 
                string.IsNullOrWhiteSpace(RepackFolderTxt.Text) || 
                string.IsNullOrWhiteSpace(OutputPazTxt.Text)) return;

            int idx = GameIndexCombo.SelectedIndex;

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // fuckpaz syntax: fuckpaz.exe <original.paz> <game_index> <output.paz>
            // It reads file listing from original.paz TOC, then opens each file by relative name
            // from the working directory (RepackFolderTxt) which must directly contain those files.
            string workDir = RepackFolderTxt.Text;
            string outPath = Path.Combine(workDir, OutputPazTxt.Text);

            // Use ArgumentList to safely handle paths with spaces / em-dashes / parentheses.
            await ToolRunner.RunAsync(workDir, exe, new[] { RepackOriginalPazTxt.Text, idx.ToString(), outPath }, GetMain());
        }
    }
}
