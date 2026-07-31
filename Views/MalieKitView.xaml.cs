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

        private void BrowseDecryptArchive_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Malie Archives (*.dat;*.lib)|*.dat;*.lib|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) DecryptArchiveTxt.Text = d.FileName;
        }

        private void BrowseEncryptArchive_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) EncryptArchiveTxt.Text = d.FolderName;
        }

        private void BrowseMetaJson_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "JSON Metadata (*.json)|*.json|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) MetaJsonTxt.Text = d.FileName;
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


        // Helper: run a Python script inside LauncherDatSource with PYTHONPATH set
        // so that `from formats.xxx import` works regardless of script subdirectory.
        private async Task RunMaliePython(string script, string extraArgs = "")
        {
            string launcherDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie", "LauncherDatSource");
            string py = SettingsManager.Config.PythonPath;

            // Build ProcessStartInfo manually so we can set PYTHONPATH
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = py,
                Arguments = $"\"{System.IO.Path.Combine(launcherDir, script)}\" {extraArgs}",
                WorkingDirectory = launcherDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUTF8"] = "1";
            // PYTHONPATH = launcherDir so Python can find formats/, malie/, gameres/ packages
            psi.EnvironmentVariables["PYTHONPATH"] = launcherDir;

            var main = GetMain();
            main.LogToConsole($"▶ Executing: {py} {psi.Arguments}");

            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) main.LogToConsole(e.Data); };
            process.ErrorDataReceived  += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) main.LogToConsole(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            main.LogToConsole(process.ExitCode == 0 ? "✔ Command completed successfully." : $"✘ [ERROR] Command exited with code {process.ExitCode}.");
        }

        private async void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DecryptArchiveTxt.Text)) return;
            await RunMaliePython("execution/unpack.py", $"\"{DecryptArchiveTxt.Text}\"");
        }

        private async void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EncryptArchiveTxt.Text)) return;
            string inDir = EncryptArchiveTxt.Text;
            string baseName = System.IO.Path.GetFileName(inDir);
            string dir = System.IO.Path.GetDirectoryName(inDir) ?? "";
            string outDat = System.IO.Path.Combine(dir, baseName + "_repack.dat");

            // Use manually browsed JSON if provided, otherwise auto-detect
            string metaJson = string.IsNullOrWhiteSpace(MetaJsonTxt.Text)
                ? System.IO.Path.Combine(dir, baseName + "_entries.json")
                : MetaJsonTxt.Text;

            if (!System.IO.File.Exists(metaJson))
            {
                // Fallback: user may have selected a subfolder (e.g. data3\data) instead of the root folder
                string parentDir = System.IO.Path.GetDirectoryName(dir) ?? "";
                string parentBaseName = System.IO.Path.GetFileName(dir);
                string parentMetaJson = System.IO.Path.Combine(parentDir, parentBaseName + "_entries.json");

                if (!string.IsNullOrEmpty(parentBaseName) && System.IO.File.Exists(parentMetaJson))
                {
                    GetMain().LogToConsole($"MalieKit: Auto-corrected folder from '{inDir}' to '{dir}' (parent extracted root).");
                    inDir = dir;
                    baseName = parentBaseName;
                    dir = parentDir;
                    outDat = System.IO.Path.Combine(dir, baseName + "_repack.dat");
                    metaJson = parentMetaJson;
                }
            }

            if (!System.IO.Directory.Exists(inDir) || !System.IO.File.Exists(metaJson))
            {
                GetMain().LogToConsole($"MalieKit: Cannot repack.");
                GetMain().LogToConsole($"  Folder : '{inDir}' — {(System.IO.Directory.Exists(inDir) ? "Found" : "NOT FOUND")}");
                GetMain().LogToConsole($"  JSON   : '{metaJson}' — {(System.IO.File.Exists(metaJson) ? "Found" : "NOT FOUND")}");
                GetMain().LogToConsole($"  Tip    : Select the root extracted folder (e.g. 'data3'), or manually browse the _entries.json file.");
                return;
            }

            GetMain().LogToConsole($"MalieKit: Repacking '{inDir}' → '{outDat}' using '{metaJson}'");
            await RunMaliePython("execution/repack_plain.py", $"\"{inDir}\" \"{outDat}\" \"{metaJson}\"");
        }
        private async void ExportNames_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie", "MalieScriptExtractor");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string outTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_strings.txt");
            await ToolRunner.RunAsync(repoDir, exe, $"-a -in \"{ScriptDirTxt.Text}\" -out \"{outTxt}\"", GetMain());
        }

        private async void ExportDialog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie", "MalieScriptExtractor");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string outTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_dialog.txt");
            await ToolRunner.RunAsync(repoDir, exe, $"-e -in \"{ScriptDirTxt.Text}\" -out \"{outTxt}\"", GetMain());
        }

        private async void PatchNames_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie", "MalieScriptExtractor");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string inTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_strings.txt");
            string outDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_patched.dat");
            await ToolRunner.RunAsync(repoDir, exe, $"-s -in \"{ScriptDirTxt.Text}\" -out \"{outDat}\" -txt \"{inTxt}\"", GetMain());
        }

        private async void PatchDialog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptDirTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Malie", "MalieScriptExtractor");
            string exe = System.IO.Path.Combine(repoDir, "Malie_Script_Tool.exe");
            string inDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_patched.dat");
            string inTxt = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_dialog.txt");
            string outDat = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ScriptDirTxt.Text) ?? "", "exec_final.dat");
            await ToolRunner.RunAsync(repoDir, exe, $"-i -in \"{inDat}\" -out \"{outDat}\" -txt \"{inTxt}\"", GetMain());
        }

        private async void MgfToPng_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImageFolderTxt.Text)) return;
            var files = System.IO.Directory.GetFiles(ImageFolderTxt.Text, "*.mgf", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0) { GetMain().LogToConsole("MalieKit: No .mgf files found in the selected folder."); return; }
            GetMain().LogToConsole($"MalieKit: Found {files.Length} .mgf files. Starting conversion...");
            foreach (var f in files)
                await RunMaliePython("execution/mgfpng_change.py", $"\"{f}\" --to-png");
            GetMain().LogToConsole("MalieKit: MGF to PNG conversion finished.");
        }

        private async void PngToMgf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImageFolderTxt.Text)) return;
            var files = System.IO.Directory.GetFiles(ImageFolderTxt.Text, "*.png", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0) { GetMain().LogToConsole("MalieKit: No .png files found in the selected folder."); return; }
            GetMain().LogToConsole($"MalieKit: Found {files.Length} .png files. Starting conversion...");
            foreach (var f in files)
                await RunMaliePython("execution/mgfpng_change.py", $"\"{f}\" --to-mgf");
            GetMain().LogToConsole("MalieKit: PNG to MGF conversion finished.");
        }
    }
}

