using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class FvpEngineView : UserControl
    {
        public FvpEngineView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Application.Current.MainWindow;

        private void Msg(string msg, string title) => MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);

        // ─────────────────────────────────────────────────────────────────────
        // Browse Helpers
        // ─────────────────────────────────────────────────────────────────────
        private void BrowseBinInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "BIN Archives (*.bin)|*.bin|All Files (*.*)|*.*", Title = "Select .bin archive" };
            if (dlg.ShowDialog() == true) BinInputTxt.Text = dlg.FileName;
        }

        private void BrowseBinRepackFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder to repack into .bin" };
            if (dlg.ShowDialog() == true) BinRepackFolderTxt.Text = dlg.FolderName;
        }

        private void BrowseHcbDecompile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "HCB Scripts (*.hcb)|*.hcb|All Files (*.*)|*.*", Title = "Select .hcb script" };
            if (dlg.ShowDialog() == true) HcbDecompileTxt.Text = dlg.FileName;
        }

        private void BrowseHcbCompileFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder containing strings.txt and script.dat" };
            if (dlg.ShowDialog() == true) HcbCompileFolderTxt.Text = dlg.FolderName;
        }

        private void BrowseNvsgDecode_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "NVSG Images (*.hzc)|*.hzc|All Files (*.*)|*.*", Title = "Select image to decode" };
            if (dlg.ShowDialog() == true) NvsgDecodeTxt.Text = dlg.FileName;
        }

        private void BrowseNvsgEncode_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "PNG Images (*.png)|*.png", Title = "Select .png to encode" };
            if (dlg.ShowDialog() == true) NvsgEncodeTxt.Text = dlg.FileName;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BIN Archive Extract
        // ─────────────────────────────────────────────────────────────────────
        private async void BinExtract_Click(object sender, RoutedEventArgs e)
        {
            string file = BinInputTxt.Text.Trim();
            if (!File.Exists(file)) { Msg("Please select a valid .bin file.", "Error"); return; }

            string outFolder = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + "_extracted");
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "bin_archiver.py");

            if (!File.Exists(scriptPath)) { Msg($"Python script not found at {scriptPath}", "Error"); return; }

            try
            {
                await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, "python", $"\"{scriptPath}\" -d \"{file}\" \"{outFolder}\"", GetMain());
                
                MessageBox.Show($"BIN extraction complete!\nSaved to: {outFolder}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // BIN Archive Repack
        // ─────────────────────────────────────────────────────────────────────
        private async void BinRepack_Click(object sender, RoutedEventArgs e)
        {
            string folder = BinRepackFolderTxt.Text.Trim();
            if (!Directory.Exists(folder)) { Msg("Please select a valid folder.", "Error"); return; }

            string outFile = folder + "_repacked.bin";
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "bin_archiver.py");

            try
            {
                await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, "python", $"\"{scriptPath}\" -c \"{folder}\" \"{outFile}\"", GetMain());
                
                MessageBox.Show($"BIN repack complete!\nSaved to: {outFile}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HCB Decompile
        // ─────────────────────────────────────────────────────────────────────
        private async void HcbDecompile_Click(object sender, RoutedEventArgs e)
        {
            string file = HcbDecompileTxt.Text.Trim();
            if (!File.Exists(file)) { Msg("Please select a valid .hcb file.", "Error"); return; }

            string workDir = Path.GetDirectoryName(file)!;
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "hcb_compiler.py");

            try
            {
                await ToolRunner.RunAsync(workDir, "python", $"\"{scriptPath}\" -d \"{file}\"", GetMain());
                
                MessageBox.Show($"HCB decompiled!\nstrings.txt and script.dat generated in:\n{workDir}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HCB Compile
        // ─────────────────────────────────────────────────────────────────────
        private async void HcbCompile_Click(object sender, RoutedEventArgs e)
        {
            string folder = HcbCompileFolderTxt.Text.Trim();
            if (!Directory.Exists(folder)) { Msg("Please select a valid folder.", "Error"); return; }
            if (!File.Exists(Path.Combine(folder, "strings.txt"))) { Msg("strings.txt not found in the selected folder.", "Error"); return; }

            string outFile = "script_compiled.hcb"; // written to workDir
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "hcb_compiler.py");

            try
            {
                await ToolRunner.RunAsync(folder, "python", $"\"{scriptPath}\" -c \"{outFile}\"", GetMain());
                
                MessageBox.Show($"HCB compiled!\nSaved as: {Path.Combine(folder, outFile)}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // NVSG Decode
        // ─────────────────────────────────────────────────────────────────────
        private async void NvsgDecode_Click(object sender, RoutedEventArgs e)
        {
            string file = NvsgDecodeTxt.Text.Trim();
            if (!File.Exists(file)) { Msg("Please select a valid image file.", "Error"); return; }

            string outFile = Path.ChangeExtension(file, ".png");
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "nvsg_converter.py");

            try
            {
                await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, "python", $"\"{scriptPath}\" --decode \"{file}\" \"{outFile}\"", GetMain());
                
                MessageBox.Show($"NVSG decoded!\nSaved to: {outFile}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // NVSG Encode
        // ─────────────────────────────────────────────────────────────────────
        private async void NvsgEncode_Click(object sender, RoutedEventArgs e)
        {
            string file = NvsgEncodeTxt.Text.Trim();
            if (!File.Exists(file)) { Msg("Please select a valid .png file.", "Error"); return; }

            string outFile = Path.ChangeExtension(file, ".hzc");
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "FVP", "nvsg_converter.py");

            try
            {
                await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, "python", $"\"{scriptPath}\" --encode \"{file}\" \"{outFile}\"", GetMain());
                
                MessageBox.Show($"PNG encoded!\nSaved to: {outFile}", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Msg(ex.Message, "Error"); }
        }
    }
}
