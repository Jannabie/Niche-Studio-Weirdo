using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class MusicusView : UserControl
    {
        public MusicusView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseDat_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "DAT Files (*.dat)|*.dat|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) DatFileTxt.Text = d.FileName;
        }
        private void BrowseDec_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) DecFileTxt.Text = d.FolderName;
        }
        private void BrowseJson_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) JsonTxt.Text = d.FolderName;
        }
        private void BrowseDecFinal_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) DecFinalTxt.Text = d.FolderName;
        }

        private async void UnpackDat_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DatFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "YOX");
            string py = SettingsManager.Config.PythonPath;
            // Output folder name = dat filename without extension (e.g. script_en.dat ↁEscript_en/)
            string outDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(DatFileTxt.Text) ?? repoDir,
                System.IO.Path.GetFileNameWithoutExtension(DatFileTxt.Text));
            await ToolRunner.RunAsync(repoDir, py, $"musicus_tool.py unpack \"{DatFileTxt.Text}\" \"{outDir}\"", GetMain());
        }

        private async void ExtractJson_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DecFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "YOX");
            string py = SettingsManager.Config.PythonPath;
            // json_folder is a sibling of the dec folder
            string jsonDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(DecFileTxt.Text) ?? repoDir, "json_folder");
            await ToolRunner.RunAsync(repoDir, py, $"yox_tool.py extract_all \"{DecFileTxt.Text}\" \"{jsonDir}\"", GetMain());
        }

        private async void RepackDec_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DecFileTxt.Text) || string.IsNullOrWhiteSpace(JsonTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "YOX");
            string py = SettingsManager.Config.PythonPath;
            // CRITICAL: output MUST be same as dec folder so manifest.json is preserved!
            // yox_tool.py pack_all <dec_folder> <json_folder> <output_dec_folder>
            await ToolRunner.RunAsync(repoDir, py, $"yox_tool.py pack_all \"{DecFileTxt.Text}\" \"{JsonTxt.Text}\" \"{DecFileTxt.Text}\"", GetMain());
        }

        private async void RebuildDat_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DecFinalTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "YOX");
            string py = SettingsManager.Config.PythonPath;
            string outDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(DecFinalTxt.Text) ?? "", "patched.dat");
            await ToolRunner.RunAsync(repoDir, py, $"musicus_tool.py repack \"{DecFinalTxt.Text}\" \"{outDat}\"", GetMain());
        }
    }
}
