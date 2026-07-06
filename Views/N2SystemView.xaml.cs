using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NicheStudioWeirdo.Views
{
    public partial class N2SystemView : UserControl
    {
        public N2SystemView()
        {
            InitializeComponent();
        }

        private void BrowseExtractInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "NPA Archive (*.npa)|*.npa|All Files (*.*)|*.*", Title = "Select .npa file to extract" };
            if (d.ShowDialog() == true) ExtractInputTxt.Text = d.FileName;
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            string npaPath = ExtractInputTxt.Text;

            if (string.IsNullOrWhiteSpace(npaPath) || !File.Exists(npaPath))
            {
                MessageBox.Show("Please select a valid .npa file to extract.");
                return;
            }

            // Read the internal nipa GameID from the Tag, not the human-readable Content
            string gameId = ((ComboBoxItem)GameProfileCombo.SelectedItem)?.Tag?.ToString() ?? "";

            string args = !string.IsNullOrEmpty(gameId)
                ? $"-xg \"{npaPath}\" \"{gameId}\""
                : $"-x \"{npaPath}\"";

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "N2SystemBin", "nipa.exe");

            // nipa.exe creates the folder in the current working directory,
            // so set the working directory to the archive's own folder.
            string workingDir = Path.GetDirectoryName(npaPath) ?? AppDomain.CurrentDomain.BaseDirectory;

            var main = Window.GetWindow(this) as MainWindow;
            main?.LogToConsole($"▶ N2System: Extracting \"{Path.GetFileName(npaPath)}\"...");

            await RunNipaSilentAsync(exePath, args, workingDir, main);

            main?.LogToConsole("✅ Extraction complete! Check the folder next to your .npa file.");
        }

        private void BrowseRepackInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select input directory containing extracted files..." };
            if (d.ShowDialog() == true) RepackInputTxt.Text = d.FolderName;
        }

        private void BrowseRepackOutput_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog { Filter = "NPA Archive (*.npa)|*.npa", DefaultExt = "npa", Title = "Save repacked .npa as..." };
            if (d.ShowDialog() == true) RepackOutputTxt.Text = d.FileName;
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            string inDir = RepackInputTxt.Text;
            string outNpa = RepackOutputTxt.Text;

            if (string.IsNullOrWhiteSpace(inDir) || !Directory.Exists(inDir))
            {
                MessageBox.Show("Please select a valid folder to repack.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outNpa))
            {
                outNpa = Path.Combine(Path.GetDirectoryName(inDir) ?? "", Path.GetFileName(inDir) + "_new.npa");
                RepackOutputTxt.Text = outNpa;
            }

            // Read the internal nipa GameID from the Tag, not the human-readable Content
            string gameId = ((ComboBoxItem)GameProfileCombo.SelectedItem)?.Tag?.ToString() ?? "";

            bool compress = ChkCompress.IsChecked == true;

            string flags = "-c";
            if (compress) flags += "z";
            if (!string.IsNullOrEmpty(gameId)) flags += "g";

            string args = $"{flags} \"{inDir}\" \"{outNpa}\"";
            if (!string.IsNullOrEmpty(gameId))
                args += $" \"{gameId}\"";

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "N2SystemBin", "nipa.exe");

            var main = Window.GetWindow(this) as MainWindow;
            main?.LogToConsole($"▶ N2System: Repacking \"{Path.GetFileName(inDir)}\" → \"{Path.GetFileName(outNpa)}\"...");

            await RunNipaSilentAsync(exePath, args, AppDomain.CurrentDomain.BaseDirectory, main);

            main?.LogToConsole($"✅ Repack complete! Saved to \"{Path.GetFileName(outNpa)}\".");
        }

        /// <summary>
        /// Runs nipa.exe silently. nipa uses _UNICODE/_O_WTEXT wide-char output mode which
        /// cannot be reliably parsed by C#'s OutputDataReceived — so we suppress all output
        /// entirely and just check the exit code for success/failure.
        /// </summary>
        private static async Task RunNipaSilentAsync(
            string exePath, string args, string workingDir, MainWindow? main)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    // Redirect but discard all output — nipa's wide-char stdout
                    // cannot be read as text reliably from C#.
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = psi };

                // Do NOT call BeginOutputReadLine — just start and wait.
                // The redirected streams are discarded automatically when the process exits.
                process.Start();

                // Read and discard streams asynchronously to prevent deadlock
                var drainOut = process.StandardOutput.ReadToEndAsync();
                var drainErr = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                await drainOut;
                await drainErr;

                if (process.ExitCode != 0)
                    main?.LogToConsole($"✘ [ERROR] nipa exited with code {process.ExitCode}. Make sure you selected the correct game.");
            }
            catch (Exception ex)
            {
                main?.LogToConsole($"✘ [EXCEPTION] Failed to run nipa: {ex.Message}");
            }
        }
    }
}
