using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class MajikaiView : UserControl
    {
        public MajikaiView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowsePac_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PAC Archives (*.pac)|*.pac|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) PacFileTxt.Text = d.FileName;
        }
        private void BrowsePacOut_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) PacOutTxt.Text = d.FolderName;
        }
        private void BrowseBin_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "BIN Scripts (*.bin)|*.bin|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) BinFileTxt.Text = d.FileName;
        }

        private async void UnpackPac_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PacFileTxt.Text) || string.IsNullOrWhiteSpace(PacOutTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "Majikoi-JAST-tools");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"minatosoft_pac.py unpack \"{PacFileTxt.Text}\" \"{PacOutTxt.Text}\"", GetMain());
        }

        private async void RepackPac_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PacFileTxt.Text) || string.IsNullOrWhiteSpace(PacOutTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "Majikoi-JAST-tools");
            string py = SettingsManager.Config.PythonPath;
            string newPac = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PacFileTxt.Text) ?? "", "Update_" + System.IO.Path.GetFileName(PacFileTxt.Text));
            await ToolRunner.RunAsync(repoDir, py, $"minatosoft_pac.py repack \"{PacOutTxt.Text}\" \"{newPac}\"", GetMain());
        }

        private async void ExtractBin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BinFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "Majikoi-JAST-tools");
            string py = SettingsManager.Config.PythonPath;
            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BinFileTxt.Text) ?? "", System.IO.Path.GetFileNameWithoutExtension(BinFileTxt.Text) + "_ext");
            await ToolRunner.RunAsync(repoDir, py, $"majikoi_bin_tool.py extract \"{BinFileTxt.Text}\" \"{outDir}\"", GetMain());
        }

        private async void RepackBin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BinFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "Majikoi-JAST-tools");
            string py = SettingsManager.Config.PythonPath;
            var mode = ((ComboBoxItem)RepackModeCombo.SelectedItem).Content.ToString().Contains("Preserve") ? "preserve" : "rebuild";
            string inDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BinFileTxt.Text) ?? "", System.IO.Path.GetFileNameWithoutExtension(BinFileTxt.Text) + "_ext");
            string outFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BinFileTxt.Text) ?? "", "patched_" + System.IO.Path.GetFileName(BinFileTxt.Text));
            await ToolRunner.RunAsync(repoDir, py, $"majikoi_bin_tool.py repack \"{inDir}\" \"{outFile}\" --mode {mode}", GetMain());
        }
    }
}
