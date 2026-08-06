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

        private void BrowseMoonbeamFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "MBM/XML Files|*.mbm;*.xml|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                MoonbeamPath.Text = dlg.FileName;
                IsMassMode.IsChecked = false;
            }
        }

        private void BrowseMoonbeamFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select Folder for Mass Mode" };
            if (dlg.ShowDialog() == true)
            {
                MoonbeamPath.Text = dlg.FolderName;
                IsMassMode.IsChecked = true;
            }
        }

        private async void ExportMbm_Click(object sender, RoutedEventArgs e)
        {
            await RunMoonbeam("-e", "Exporting MBM to XML...");
        }

        private async void ImportMbm_Click(object sender, RoutedEventArgs e)
        {
            await RunMoonbeam("-i", "Importing XML to MBM...");
        }

        private async Task RunMoonbeam(string modeArg, string logMessage)
        {
            if (string.IsNullOrWhiteSpace(MoonbeamPath.Text))
            {
                Main.LogToConsole("Trikintul: Please select a file or folder for Moonbeam.");
                return;
            }

            string toolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Trikintul", "Moonbeam.exe");
            if (!File.Exists(toolPath))
            {
                Main.LogToConsole($"Trikintul: Missing tool -> {toolPath}");
                return;
            }

            // If path is directory but checkbox is false, force mass mode. Or if checkbox is true and path is dir.
            // Moonbeam natively supports recursive mode if we just pass the directory path.
            string args = $"{modeArg} \"{MoonbeamPath.Text}\"";
            Main.LogToConsole($"Trikintul: {logMessage}");
            await ToolRunner.RunAsync(Path.GetDirectoryName(toolPath) ?? "", toolPath, args, Main);
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri != null)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri) { UseShellExecute = true });
            }
        }
    }
}
