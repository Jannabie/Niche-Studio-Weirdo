using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class YurisView : UserControl
    {
        public YurisView()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Browse helpers
        // ─────────────────────────────────────────────────────────────────────

        private void BrowseYpfInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "YPF Files (*.ypf)|*.ypf|All Files (*.*)|*.*",
                Title  = "Select .ypf archive to extract"
            };
            if (dlg.ShowDialog() == true) YpfInputTxt.Text = dlg.FileName;
        }

        private void BrowseYpfRepackFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder to pack into .ypf" };
            if (dlg.ShowDialog() == true) YpfRepackFolderTxt.Text = dlg.FolderName;
        }

        private void BrowseYbnFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing .ybn files" };
            if (dlg.ShowDialog() == true) YbnFolderTxt.Text = dlg.FolderName;
        }

        // ─────────────────────────────────────────────────────────────────────
        // SECTION 1 — YPF EXTRACT
        // ─────────────────────────────────────────────────────────────────────

        private async void YpfExtract_Click(object sender, RoutedEventArgs e)
        {
            string file = YpfInputTxt.Text.Trim();
            if (!File.Exists(file))
            {
                Msg("Please select a valid .ypf file.", "Error");
                return;
            }

            string outDir = Path.Combine(
                Path.GetDirectoryName(file)!,
                Path.GetFileNameWithoutExtension(file));

            try
            {
                SetBusy(true);
                AppendLog($"Extracting {Path.GetFileName(file)} …");
                int n = await Task.Run(() =>
                    YurisYpf.Extract(file, outDir,
                        msg => Dispatcher.Invoke(() => AppendLog(msg))));
                AppendLog($"✓ Done — {n} files extracted to: {outDir}");
                MessageBox.Show($"Extracted {n} files to:\n{outDir}", "Done",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Extract Error"); }
            finally { SetBusy(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SECTION 1 — YPF REPACK
        // ─────────────────────────────────────────────────────────────────────

        private async void YpfRepack_Click(object sender, RoutedEventArgs e)
        {
            string folder = YpfRepackFolderTxt.Text.Trim();
            if (!Directory.Exists(folder))
            {
                Msg("Please select a valid folder to repack.", "Error");
                return;
            }

            string versionStr = YpfEngineVersionTxt.Text.Trim();
            if (versionStr.StartsWith("0."))
            {
                versionStr = versionStr.Substring(2); // e.g., "0.479" -> "479"
            }

            if (!int.TryParse(versionStr, out int version) || version < 234 || version > 490)
            {
                Msg("Engine version must be a number between 234 and 490 (e.g. 479 or 0.479).", "Error");
                return;
            }

            bool useCrc32 = YpfUseCrc32Chk.IsChecked == true;
            string outputYpf = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".ypf";

            try
            {
                SetBusy(true);
                AppendLog($"Packing {folder} → {Path.GetFileName(outputYpf)} (engine v{version}, CRC32: {useCrc32}) …");
                await Task.Run(() =>
                    YurisYpf.Pack(folder, outputYpf, version, useCrc32,
                        msg => Dispatcher.Invoke(() => AppendLog(msg))));
                AppendLog($"✓ Done — saved to: {outputYpf}");
                MessageBox.Show($"Repacked successfully!\n\nSaved to:\n{outputYpf}", "Done",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Repack Error"); }
            finally { SetBusy(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SECTION 2 — YBN EXTRACT TEXT
        // ─────────────────────────────────────────────────────────────────────

        private async void YbnDecryptExtract_Click(object sender, RoutedEventArgs e)
        {
            string folder = YbnFolderTxt.Text.Trim();
            if (!Directory.Exists(folder))
            {
                Msg("Please select a valid folder containing .ybn files.", "Error");
                return;
            }

            try
            {
                SetBusy(true);

                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "VNTextPatch", "VNTextPatch.exe");
                if (!File.Exists(exePath))
                {
                    Msg($"Please place VNTextPatch.exe at:\n{exePath}", "Error");
                    return;
                }

                // Always wipe script_txt first so old .txt files don't contaminate output
                string textDir = Path.Combine(folder, "script_txt");
                if (Directory.Exists(textDir))
                    Directory.Delete(textDir, true);
                Directory.CreateDirectory(textDir);

                AppendLog("Extracting text via VNTextPatch (file by file) …");
                int ok = 0, fail = 0;
                var ybnFiles = Directory.GetFiles(folder, "*.ybn").OrderBy(f => f).ToArray();
                
                await Task.Run(async () =>
                {
                    foreach (var ybn in ybnFiles)
                    {
                        string fname = Path.GetFileNameWithoutExtension(ybn);
                        string outJson = Path.Combine(textDir, fname + ".json");
                        await RunVNTextPatchAsync("extractlocal", ybn, outJson, msg => { });
                        
                        if (File.Exists(outJson))
                        {
                            ok++;
                            Dispatcher.Invoke(() => AppendLog($"  ✓ {fname}.ybn"));
                        }
                    }
                });

                int jsonCount = Directory.GetFiles(textDir, "*.json").Length;
                AppendLog($"✓ Done — {jsonCount} JSON files in 'script_txt'.");
                MessageBox.Show(
                    $"Extraction complete!\n\n{jsonCount} JSON files created in:\n{textDir}\n\nEdit the \"message\" values, then click Insert Text.",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Extract Error"); }
            finally { SetBusy(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SECTION 3 — YBN INSERT TEXT
        // ─────────────────────────────────────────────────────────────────────

        private async void YbnInsertEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string folder = YbnFolderTxt.Text.Trim();
            if (!Directory.Exists(folder))
            {
                Msg("Please select a valid folder containing .ybn files.", "Error");
                return;
            }

            string textDir = Path.Combine(folder, "script_txt");
            if (!Directory.Exists(textDir))
            {
                Msg("No 'script_txt' folder found. Run Extract Text first.", "Error");
                return;
            }

            try
            {
                SetBusy(true);

                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "VNTextPatch", "VNTextPatch.exe");
                if (!File.Exists(exePath))
                {
                    Msg($"Please place VNTextPatch.exe at:\n{exePath}", "Error");
                    return;
                }

                // Step 1 — copy originals to script_patched
                AppendLog("Step 1 — Copying original .ybn files to script_patched …");
                string patchedDir = Path.Combine(folder, "script_patched");
                if (Directory.Exists(patchedDir))
                    Directory.Delete(patchedDir, true);
                Directory.CreateDirectory(patchedDir);
                await Task.Run(() =>
                {
                    foreach (var f in Directory.GetFiles(folder, "*.ybn"))
                        File.Copy(f, Path.Combine(patchedDir, Path.GetFileName(f)), true);
                });

                // Step 2 — inject via VNTextPatch individually
                AppendLog("Step 2 — Injecting translated JSON into .ybn files via VNTextPatch …");
                int ok = 0;
                var jsonFiles = Directory.GetFiles(textDir, "*.json").OrderBy(f => f).ToArray();
                
                await Task.Run(async () =>
                {
                    foreach (var json in jsonFiles)
                    {
                        string fname = Path.GetFileNameWithoutExtension(json);
                        string targetYbn = Path.Combine(patchedDir, fname + ".ybn");
                        if (File.Exists(targetYbn))
                        {
                            await RunVNTextPatchAsync("insertlocal", targetYbn, json, msg => { });
                            ok++;
                            Dispatcher.Invoke(() => AppendLog($"  ✓ injected {fname}.json"));
                        }
                    }
                });

                int patchedCount = Directory.GetFiles(patchedDir, "*.ybn").Length;
                AppendLog($"✓ Done — {ok} files patched in script_patched.");
                MessageBox.Show(
                    $"Injection complete!\n\nPatched {ok} .ybn files:\n{patchedDir}\n\nNow pack this folder into a YPF.",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Inject Error"); }
            finally { SetBusy(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shared helpers
        // ─────────────────────────────────────────────────────────────────────

        private void AppendLog(string msg)
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.LogToConsole(msg);
        }

        private void SetBusy(bool busy)
        {
            IsEnabled = !busy;
        }

        private static void Msg(string text, string title) =>
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>
        /// extractlocal: args = &lt;ybnFolder&gt; &lt;textDir&gt;
        /// insertlocal:  args = &lt;patchedDir&gt; &lt;textDir&gt; &lt;patchedDir&gt;
        /// </summary>
        private async Task RunVNTextPatchAsync(string mode, string ybnFolder, string scriptFolder, Action<string> log)
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "VNTextPatch", "VNTextPatch.exe");
            string arguments = mode == "extractlocal"
                ? $"extractlocal \"{ybnFolder}\" \"{scriptFolder}\""
                : $"insertlocal \"{ybnFolder}\" \"{scriptFolder}\" \"{ybnFolder}\"";

            var tcs = new TaskCompletionSource<bool>();
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName  = exePath,
                    Arguments = arguments,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    WorkingDirectory       = Path.GetDirectoryName(ybnFolder)
                },
                EnableRaisingEvents = true
            };
            proc.OutputDataReceived += (s, ev) => { if (!string.IsNullOrEmpty(ev.Data)) log(ev.Data); };
            proc.ErrorDataReceived  += (s, ev) => { if (!string.IsNullOrEmpty(ev.Data)) log("ERROR: " + ev.Data); };
            proc.Exited += (s, ev) => { tcs.TrySetResult(true); proc.Dispose(); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await tcs.Task;
        }
    }
}
