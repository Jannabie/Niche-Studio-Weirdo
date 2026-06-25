using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class HuneXMahoyoView : UserControl
    {
        public HuneXMahoyoView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseHfa_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "HFA Archive (*.hfa)|*.hfa|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) HfaFileTxt.Text = d.FileName;
        }

        private void BrowseHfaFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) HfaFolderTxt.Text = d.FolderName;
        }

        private void BrowseOrig_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Mahoyo Assets (*.ctd;*.cbg;*.mzp)|*.ctd;*.cbg;*.mzp|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) OriginalFileTxt.Text = d.FileName;
        }

        private void BrowseMod_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Modded Files (*.txt;*.png)|*.txt;*.png|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ModdedFileTxt.Text = d.FileName;
        }

        private async void UnpackHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFileTxt.Text) || HfaFileTxt.Text.Contains("Select")) return;
            string repoDir = Path.Combine(SettingsManager.Config.ReposPath, "HuneX-Scripting");
            string py = SettingsManager.Config.PythonPath;
            // hfa_tool.py unpack <file.hfa> -> outputs to same dir with _ext
            await ToolRunner.RunAsync(repoDir, py, $"hfa_tool.py unpack \"{HfaFileTxt.Text}\"", GetMain());
        }

        private async void RepackHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFolderTxt.Text) || HfaFolderTxt.Text.Contains("Select")) return;
            string repoDir = Path.Combine(SettingsManager.Config.ReposPath, "HuneX-Scripting");
            string py = SettingsManager.Config.PythonPath;
            string outHfa = Path.Combine(Path.GetDirectoryName(HfaFolderTxt.Text) ?? "", Path.GetFileName(HfaFolderTxt.Text) + "_new.hfa");
            // hfa_tool.py repack <folder> <output.hfa>
            await ToolRunner.RunAsync(repoDir, py, $"hfa_tool.py repack \"{HfaFolderTxt.Text}\" \"{outHfa}\"", GetMain());
        }

        private async void Decode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalFileTxt.Text) || OriginalFileTxt.Text.Contains("Select")) return;
            string repoDir = Path.Combine(SettingsManager.Config.ReposPath, "HuneX-Scripting");
            string py = SettingsManager.Config.PythonPath;
            string ext = Path.GetExtension(OriginalFileTxt.Text).ToLower();

            string script = ext switch
            {
                ".ctd" => "ctd_tool.py",
                ".cbg" => "cbg_tool.py",
                ".mzp" => "mzp_tool.py",
                _ => ""
            };

            if (script != "")
            {
                string cmd = ext == ".ctd" ? "decompress" : "decode";
                await ToolRunner.RunAsync(repoDir, py, $"{script} {cmd} \"{OriginalFileTxt.Text}\"", GetMain());
            }
        }

        private async void Encode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalFileTxt.Text) || OriginalFileTxt.Text.Contains("Select")) return;
            if (string.IsNullOrWhiteSpace(ModdedFileTxt.Text) || ModdedFileTxt.Text.Contains("Select")) return;

            string repoDir = Path.Combine(SettingsManager.Config.ReposPath, "HuneX-Scripting");
            string py = SettingsManager.Config.PythonPath;
            string ext = Path.GetExtension(OriginalFileTxt.Text).ToLower();

            string script = ext switch
            {
                ".ctd" => "ctd_tool.py",
                ".cbg" => "cbg_tool.py",
                ".mzp" => "mzp_tool.py",
                _ => ""
            };

            if (script != "")
            {
                string cmd = ext == ".ctd" ? "compress" : "encode";
                // python tool.py encode <modded> <original>
                await ToolRunner.RunAsync(repoDir, py, $"{script} {cmd} \"{ModdedFileTxt.Text}\" \"{OriginalFileTxt.Text}\"", GetMain());
            }
        }
    }
}
