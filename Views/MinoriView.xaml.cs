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
            if (string.IsNullOrWhiteSpace(PazFileTxt.Text) || string.IsNullOrWhiteSpace(FolderTxt.Text)) return;

            int idx = GameIndexCombo.SelectedIndex; // 0-25, maps directly

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // fuckpaz.exe u "<file.paz>" <idx> "<output_folder>"
            await ToolRunner.RunAsync(repoDir, exe, $"u \"{PazFileTxt.Text}\" {idx} \"{FolderTxt.Text}\"", GetMain());
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderTxt.Text) || string.IsNullOrWhiteSpace(OutputPazTxt.Text)) return;

            int idx = GameIndexCombo.SelectedIndex; // 0-25, maps directly

            string repoDir = Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Minori");
            string exe = Path.Combine(repoDir, "tools", "fuckpaz.exe");

            // fuckpaz.exe p "<folder>" <idx> "<output.paz>"
            // Output .paz goes to same directory as the input folder's parent
            string outPath = Path.Combine(Path.GetDirectoryName(FolderTxt.Text) ?? repoDir, OutputPazTxt.Text);
            await ToolRunner.RunAsync(repoDir, exe, $"p \"{FolderTxt.Text}\" {idx} \"{outPath}\"", GetMain());
        }
    }
}
