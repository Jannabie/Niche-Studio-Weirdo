using Microsoft.Win32;
using NicheStudioWeirdo.Engines;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;
using NicheStudioWeirdo.Utils;
using System;

namespace NicheStudioWeirdo.Views
{
    public partial class TrikintulView : UserControl
    {
        private MainWindow Main => (MainWindow)Application.Current.MainWindow;

        public TrikintulView()
        {
            InitializeComponent();
        }

        private void BrowseDecrypt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "3DS/CIA Files|*.3ds;*.cia|All Files|*.*" };
            if (dlg.ShowDialog() == true) InputDecrypt.Text = dlg.FileName;
        }

        private async void DecryptArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputDecrypt.Text))
            {
                Main.LogToConsole("Trikintul: Please select a .3ds or .cia file to decrypt.");
                return;
            }

            await Nintendo3DSEngine.DecryptArchive(InputDecrypt.Text, Main);
        }

        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "3DS/CIA Files|*.3ds;*.cia|All Files|*.*" };
            if (dlg.ShowDialog() == true) InputArchive.Text = dlg.FileName;
        }

        private async void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputArchive.Text))
            {
                Main.LogToConsole("Trikintul: Please select a .3ds or .cia file.");
                return;
            }

            await Nintendo3DSEngine.ExtractArchive(InputArchive.Text, Main);
        }

        private void BrowseRepackSource_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select _Unpacked Folder" };
            if (dlg.ShowDialog() == true) RepackSource.Text = dlg.FolderName;
        }

        private async void RepackCIA_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RepackSource.Text))
            {
                Main.LogToConsole("Trikintul: Please select an _Unpacked folder to repack.");
                return;
            }
            await Nintendo3DSEngine.RepackArchive(RepackSource.Text, true, Main);
        }

        private async void Repack3DS_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RepackSource.Text))
            {
                Main.LogToConsole("Trikintul: Please select an _Unpacked folder to repack.");
                return;
            }
            await Nintendo3DSEngine.RepackArchive(RepackSource.Text, false, Main);
        }

        private void BrowseRomFs_Click(object sender, RoutedEventArgs e)
        {
            // Allow both file (.bin) and folder selection by presenting a message box to choose, or just use OpenFileDialog and user can manually paste folder if needed.
            // Better: just check if they want to browse file or folder
            MessageBoxResult result = MessageBox.Show("Do you want to select a RomFS file to extract?\n\nYes = Select File (.bin)\nNo = Select Folder (for rebuilding)", "Select Path Type", MessageBoxButton.YesNoCancel);
            
            if (result == MessageBoxResult.Yes)
            {
                var dlg = new OpenFileDialog { Filter = "RomFS Binaries|*.bin|All Files|*.*" };
                if (dlg.ShowDialog() == true) RomFsPath.Text = dlg.FileName;
            }
            else if (result == MessageBoxResult.No)
            {
                var dlg = new OpenFolderDialog { Title = "Select Extracted RomFS Folder" };
                if (dlg.ShowDialog() == true) RomFsPath.Text = dlg.FolderName;
            }
        }

        private async void ExtractRomFs_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RomFsPath.Text) || !File.Exists(RomFsPath.Text))
            {
                Main.LogToConsole("Trikintul: Please select a valid RomFS .bin file to extract.");
                return;
            }

            string toolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Trikintul", "RomFSExtractor.exe");
            if (!File.Exists(toolPath))
            {
                Main.LogToConsole($"Trikintul: Missing tool -> {toolPath}");
                return;
            }

            string args = $"\"{RomFsPath.Text}\"";
            string workDir = Path.GetDirectoryName(RomFsPath.Text) ?? "";
            Main.LogToConsole($"Extracting RomFS: {Path.GetFileName(RomFsPath.Text)}...");
            Main.LogToConsole($"Output will be placed next to the .bin file.");
            await ToolRunner.RunAsync(workDir, toolPath, args, Main);
        }

        private async void BuildRomFs_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RomFsPath.Text) || !Directory.Exists(RomFsPath.Text))
            {
                Main.LogToConsole("Trikintul: Please select a valid folder to build RomFS.");
                return;
            }

            string toolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Trikintul", "RomFSBuilder.exe");
            if (!File.Exists(toolPath))
            {
                Main.LogToConsole($"Trikintul: Missing tool -> {toolPath}");
                return;
            }

            string outBin = RomFsPath.Text + "_repack.bin";
            string args = $"\"{RomFsPath.Text}\" \"{outBin}\"";
            Main.LogToConsole($"Building RomFS from folder: {RomFsPath.Text}...");
            await ToolRunner.RunAsync(Path.GetDirectoryName(toolPath) ?? "", toolPath, args, Main);
        }

        private void BrowseMoonbeamExportFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "MBM Files|*.mbm|All Files|*.*" };
            if (dlg.ShowDialog() == true) MoonbeamExportPath.Text = dlg.FileName;
        }

        private void BrowseMoonbeamExportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select Folder containing MBM files" };
            if (dlg.ShowDialog() == true) MoonbeamExportPath.Text = dlg.FolderName;
        }

        private void BrowseMoonbeamImportFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "XML Files|*.xml|All Files|*.*" };
            if (dlg.ShowDialog() == true) MoonbeamImportPath.Text = dlg.FileName;
        }

        private void BrowseMoonbeamImportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select Folder containing XML files" };
            if (dlg.ShowDialog() == true) MoonbeamImportPath.Text = dlg.FolderName;
        }

        private async void ExportMbm_Click(object sender, RoutedEventArgs e)
        {
            await RunMoonbeam("-e", MoonbeamExportPath.Text, "Exporting MBM to XML...");
        }

        private async void ImportMbm_Click(object sender, RoutedEventArgs e)
        {
            await RunMoonbeam("-i", MoonbeamImportPath.Text, "Importing XML to MBM...");
        }

        private async Task RunMoonbeam(string modeArg, string targetPath, string logMessage)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || targetPath.StartsWith("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a valid file or folder for Moonbeam.");
                return;
            }

            string toolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Trikintul", "Moonbeam.exe");
            if (!File.Exists(toolPath))
            {
                Main.LogToConsole($"Trikintul: Missing tool -> {toolPath}");
                return;
            }

            string args = $"{modeArg} \"{targetPath}\"";
            Main.LogToConsole($"Trikintul: {logMessage}");
            await ToolRunner.RunAsync(Path.GetDirectoryName(toolPath) ?? "", toolPath, args, Main);
        }

        // ═══════════════════════════════════════════════════════
        // STRANGE JOURNEY REDUX FBIN TOOLS
        // ═══════════════════════════════════════════════════════

        private void BrowseSjrExtractFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "BIN/MBM Files|*.bin;*.mbm|All Files|*.*" };
            if (dlg.ShowDialog() == true) SjrExtractPath.Text = dlg.FileName;
        }

        private void BrowseSjrExtractFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing EventMessage .bin files" };
            if (dlg.ShowDialog() == true) SjrExtractPath.Text = dlg.FolderName;
        }

        private void BrowseSjrRepackFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "MBM Files|*.mbm|All Files|*.*" };
            if (dlg.ShowDialog() == true) SjrRepackPath.Text = dlg.FileName;
        }

        private void BrowseSjrRepackFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing .mbm files to repack" };
            if (dlg.ShowDialog() == true) SjrRepackPath.Text = dlg.FolderName;
        }

        private void ExtractFbin_Click(object sender, RoutedEventArgs e)
        {
            string path = SjrExtractPath.Text;
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a valid FBIN file or folder.");
                return;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    SjrFbinTool.ProcessDirectoryExtract(path, msg => Main.LogToConsole($"Trikintul: {msg}"));
                }
                else if (File.Exists(path))
                {
                    SjrFbinTool.ExtractFbin(path, msg => Main.LogToConsole($"Trikintul: {msg}"));
                }
                else
                {
                    Main.LogToConsole("Trikintul: Path does not exist.");
                }
            }
            catch (Exception ex)
            {
                Main.LogToConsole($"Trikintul: ERROR - {ex.Message}");
            }
        }

        private void RepackFbin_Click(object sender, RoutedEventArgs e)
        {
            string path = SjrRepackPath.Text;
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a valid .mbm file or folder to repack.");
                return;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    SjrFbinTool.ProcessDirectoryRepack(path, msg => Main.LogToConsole($"Trikintul: {msg}"));
                }
                else if (File.Exists(path))
                {
                    SjrFbinTool.RepackFbin(path, msg => Main.LogToConsole($"Trikintul: {msg}"));
                }
                else
                {
                    Main.LogToConsole("Trikintul: Path does not exist.");
                }
            }
            catch (Exception ex)
            {
                Main.LogToConsole($"Trikintul: ERROR - {ex.Message}");
            }
        }

        private void BrowseStexInput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "STEX Files|*.stex|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                StexInputPath.Text = dlg.FileName;
            }
        }

        private void BrowsePngInput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "PNG Files|*.png|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                PngInputPath.Text = dlg.FileName;
            }
        }

        private void BrowseRefStex_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Title = "Select Reference STEX File", Filter = "STEX Files|*.stex|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                RefStexPath.Text = dlg.FileName;
            }
        }

        private async void StexToPng_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StexInputPath.Text) || StexInputPath.Text.Contains("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a STEX file.");
                return;
            }

            string outPath = Path.ChangeExtension(StexInputPath.Text, ".png");
            Main.LogToConsole($"Trikintul: Converting STEX -> PNG...");
            try
            {
                await SmtTextureConverter.ConvertStexToPngAsync(StexInputPath.Text, outPath);
                Main.LogToConsole($"Trikintul: [DONE] STEX -> PNG -> {outPath}");
            }
            catch (Exception ex)
            {
                Main.LogToConsole($"Trikintul Error: {ex.Message}");
            }
        }

        private async void PngToStex_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PngInputPath.Text) || PngInputPath.Text.Contains("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a PNG file.");
                return;
            }

            if (string.IsNullOrWhiteSpace(RefStexPath.Text) || RefStexPath.Text.Contains("Select"))
            {
                Main.LogToConsole("Trikintul: Please select a Reference STEX file.");
                return;
            }

            string outPath = Path.ChangeExtension(PngInputPath.Text, ".stex");
            Main.LogToConsole("Trikintul: Converting PNG -> STEX... (ETC1 originals are re-saved as RGBA8)");
            try
            {
                await SmtTextureConverter.ConvertPngToStexAsync(PngInputPath.Text, outPath, RefStexPath.Text);
                Main.LogToConsole($"Trikintul: [DONE] PNG -> STEX -> {outPath}");
            }
            catch (Exception ex)
            {
                Main.LogToConsole($"Trikintul Error: {ex.Message}");
            }
        }

        private void BrowseMoflexInput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Moflex Video|*.moflex;*.mods|All Files|*.*" };
            if (dialog.ShowDialog() == true) MoflexInputPath.Text = dialog.FileName;
        }

        private async void ExtractToMp4_Click(object sender, RoutedEventArgs e)
        {
            string input = MoflexInputPath.Text;
            
            // Auto-detect ffmpeg.exe in the app directory or 'Tools' subdirectory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpeg = System.IO.Path.Combine(baseDir, "ffmpeg.exe");
            if (!System.IO.File.Exists(ffmpeg)) ffmpeg = System.IO.Path.Combine(baseDir, "Tools", "ffmpeg.exe");
            
            if (string.IsNullOrWhiteSpace(input) || !System.IO.File.Exists(input))
            {
                Main.LogToConsole("Trikintul Error: Please select a valid .moflex file.");
                return;
            }
            if (!System.IO.File.Exists(ffmpeg))
            {
                Main.LogToConsole("Trikintul Error: 'ffmpeg.exe' not found! Please place ffmpeg.exe in the same folder as NicheStudioWeirdo.exe or in a 'Tools' subfolder.");
                return;
            }

            string output = System.IO.Path.ChangeExtension(input, ".mp4");
            Main.LogToConsole($"Extracting Moflex to MP4 (High Quality): {output}...");

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    NicheStudioWeirdo.Utils.Mobius.MobiusTranscoder.Transcode(input, output, ffmpeg, msg => 
                    {
                        Dispatcher.Invoke(() => Main.LogToConsole($"[Mobius] {msg}"));
                    });
                    Dispatcher.Invoke(() => Main.LogToConsole("Extraction complete. Output: " + output + "\nUse this .mp4 as input for the Official Mobiclip Encoder."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Main.LogToConsole($"Trikintul Error extracting Moflex: {ex.Message}"));
                }
            });
        }

        private async void EncodeToMoflex_Click(object sender, RoutedEventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string runAsDate = Path.Combine(baseDir, "Tools", "RunAsDate", "RunAsDate.exe");
            string encoder = Path.Combine(baseDir, "Tools", "MobiclipEncoder", "MobiclipMulticoreEncoder.exe");

            if (!File.Exists(runAsDate))
            {
                Main.LogToConsole("Trikintul Error: RunAsDate.exe not found in " + runAsDate);
                return;
            }
            if (!File.Exists(encoder))
            {
                Main.LogToConsole("Trikintul Error: MobiclipMulticoreEncoder.exe not found in " + encoder);
                return;
            }

            Main.LogToConsole("Launching Official Mobiclip Encoder (Date Bypassed to 10/09/2023)...");

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = runAsDate,
                        Arguments = $"10\\09\\2023 12:00:00 \"{encoder}\"",
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Main.LogToConsole($"Trikintul Error launching encoder: {ex.Message}"));
                }
            });
        }

        private void OpenKuriimuGithub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "https://github.com/FanTranslatorsInternational/Kuriimu2";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Main.LogToConsole($"Trikintul Error opening link: {ex.Message}");
            }
        }
    }
}
