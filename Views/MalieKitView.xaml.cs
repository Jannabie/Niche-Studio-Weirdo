using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class MalieKitView : UserControl
    {
        public MalieKitView() { InitializeComponent(); SetActiveTab("ARCHIVE"); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            SetActiveTab(btn?.Tag?.ToString() ?? "ARCHIVE");
        }
        private void SetActiveTab(string tag)
        {
            var dark = (System.Windows.Media.SolidColorBrush)FindResource("BgDarkestBrush");
            var light = (System.Windows.Media.SolidColorBrush)FindResource("BgLighterBrush");
            var textLight = (System.Windows.Media.SolidColorBrush)FindResource("TextLightBrush");
            var textMuted = (System.Windows.Media.SolidColorBrush)FindResource("TextMutedBrush");
            PanelArchive.Visibility = tag == "ARCHIVE" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            PanelScript.Visibility = tag == "SCRIPT" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            PanelGfx.Visibility = tag == "GFX" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            TabArchive.Background = tag == "ARCHIVE" ? light : dark; TabArchive.Foreground = tag == "ARCHIVE" ? textLight : textMuted;
            TabScript.Background = tag == "SCRIPT" ? light : dark; TabScript.Foreground = tag == "SCRIPT" ? textLight : textMuted;
            TabGfx.Background = tag == "GFX" ? light : dark; TabGfx.Foreground = tag == "GFX" ? textLight : textMuted;
        }

        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Malie Archives (*.dat;*.lib)|*.dat;*.lib|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ArchiveTxt.Text = d.FileName;
        }
        private void BrowseScriptDir_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "DAT Files (*.dat)|*.dat|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ScriptDirTxt.Text = d.FileName;
        }
        private void BrowseImageFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) ImageFolderTxt.Text = d.FolderName;
        }


        private async void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchiveTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"LauncherDatSource/execution/unpack.py \"{ArchiveTxt.Text}\"", GetMain());
        }

        private async void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchiveTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string py = SettingsManager.Config.PythonPath;

            string baseName = System.IO.Path.GetFileNameWithoutExtension(ArchiveTxt.Text);
            string dir = System.IO.Path.GetDirectoryName(ArchiveTxt.Text) ?? "";
            string inDir = System.IO.Path.Combine(dir, baseName);
            string outDat = System.IO.Path.Combine(dir, baseName + "_repack.dat");
            string metaJson = System.IO.Path.Combine(dir, baseName + "_entries.json");

            if (!System.IO.Directory.Exists(inDir) || !System.IO.File.Exists(metaJson))
            {
                GetMain().LogToConsole($"MalieKit: Cannot repack. Ensure you have extracted first, and that {inDir} and {metaJson} exist.");
                return;
            }

            string script = "LauncherDatSource/execution/repack_plain.py";
            await ToolRunner.RunAsync(repoDir, py, $"\"{script}\" \"{inDir}\" \"{outDat}\" \"{metaJson}\"", GetMain());
        }
        private async void ExportNames_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string outTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_strings.txt");
            await ToolRunner.RunAsync(repoDir, exe, $"-a -in \"{ScriptDirTxt.Text}\" -out \"{outTxt}\"", GetMain());
        }

        private async void ExportDialog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string outTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_dialog.txt");
            await ToolRunner.RunAsync(repoDir, exe, $"-e -in \"{ScriptDirTxt.Text}\" -out \"{outTxt}\"", GetMain());
        }

        private async void PatchNames_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string inTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_strings.txt");
            string outDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_patched.dat");
            await ToolRunner.RunAsync(repoDir, exe, $"-s -in \"{ScriptDirTxt.Text}\" -out \"{outDat}\" -txt \"{inTxt}\"", GetMain());
        }

        private async void PatchDialog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string inDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_patched.dat");
            string inTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_dialog.txt");
            string outDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_final.dat");
            await ToolRunner.RunAsync(repoDir, exe, $"-i -in \"{inDat}\" -out \"{outDat}\" -txt \"{inTxt}\"", GetMain());
        }

        private async void MgfToPng_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImageFolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string py = SettingsManager.Config.PythonPath;
            string script = "LauncherDatSource/execution/mgfpng_change.py";
            
            var files = System.IO.Directory.GetFiles(ImageFolderTxt.Text, "*.mgf", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                GetMain().LogToConsole("MalieKit: No .mgf files found in the selected folder.");
                return;
            }

            GetMain().LogToConsole($"MalieKit: Found {files.Length} .mgf files. Starting conversion...");
            foreach(var f in files)
            {
                await ToolRunner.RunAsync(repoDir, py, $"\"{script}\" \"{f}\" --to-png", GetMain());
            }
            GetMain().LogToConsole("MalieKit: MGF to PNG conversion finished.");
        }

        private async void PngToMgf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImageFolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie");
            string py = SettingsManager.Config.PythonPath;
            string script = "LauncherDatSource/execution/mgfpng_change.py";
            
            var files = System.IO.Directory.GetFiles(ImageFolderTxt.Text, "*.png", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                GetMain().LogToConsole("MalieKit: No .png files found in the selected folder.");
                return;
            }

            GetMain().LogToConsole($"MalieKit: Found {files.Length} .png files. Starting conversion...");
            foreach(var f in files)
            {
                await ToolRunner.RunAsync(repoDir, py, $"\"{script}\" \"{f}\" --to-mgf", GetMain());
            }
            GetMain().LogToConsole("MalieKit: PNG to MGF conversion finished.");
        }
    }
}
