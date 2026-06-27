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

            int idx = GameIndexCombo.SelectedIndex; // 0-25, maps directly

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // Correct syntax: fuckpaz.exe <input.paz> <game_index>
            // fuckpaz auto-extracts to a folder named after the .paz in the working directory.
            // We set the working directory to the OUTPUT FOLDER if specified, otherwise the paz's parent.
            string workDir = !string.IsNullOrWhiteSpace(ExtractFolderTxt.Text)
                ? ExtractFolderTxt.Text
                : (Path.GetDirectoryName(PazFileTxt.Text) ?? repoDir);
            Directory.CreateDirectory(workDir);
            await ToolRunner.RunAsync(workDir, exe, $"\"{PazFileTxt.Text}\" {idx}", GetMain());
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RepackOriginalPazTxt.Text) || 
                string.IsNullOrWhiteSpace(RepackFolderTxt.Text) || 
                string.IsNullOrWhiteSpace(OutputPazTxt.Text)) return;

            int idx = GameIndexCombo.SelectedIndex; // 0-25, maps directly

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // Correct syntax: fuckpaz.exe <original.paz> <game_index> <output.paz>
            // fuckpaz will use the folder (same name as original paz) inside the working directory to fetch files.
            string workDir = RepackFolderTxt.Text;
            string outPath = Path.Combine(workDir, OutputPazTxt.Text);
            await ToolRunner.RunAsync(workDir, exe, $"\"{RepackOriginalPazTxt.Text}\" {idx} \"{outPath}\"", GetMain());
        }
    }
}
