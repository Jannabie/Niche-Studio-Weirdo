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

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) FolderTxt.Text = d.FolderName;
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
            string workDir = !string.IsNullOrWhiteSpace(FolderTxt.Text)
                ? FolderTxt.Text
                : (Path.GetDirectoryName(PazFileTxt.Text) ?? repoDir);
            Directory.CreateDirectory(workDir);
            await ToolRunner.RunAsync(workDir, exe, $"\"{PazFileTxt.Text}\" {idx}", GetMain());
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderTxt.Text) || string.IsNullOrWhiteSpace(OutputPazTxt.Text)) return;

            int idx = GameIndexCombo.SelectedIndex; // 0-25, maps directly

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // Correct syntax: fuckpaz.exe <folder> <game_index> <output.paz>
            string outPath = Path.Combine(Path.GetDirectoryName(FolderTxt.Text) ?? repoDir, OutputPazTxt.Text);
            await ToolRunner.RunAsync(repoDir, exe, $"\"{FolderTxt.Text}\" {idx} \"{outPath}\"", GetMain());
        }
    }
}
