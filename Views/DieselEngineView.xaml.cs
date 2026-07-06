using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class DieselEngineView : UserControl
    {
        public DieselEngineView()
        {
            InitializeComponent();
        }

        private void BrowseExtractInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "NPK Archive (*.npk)|*.npk|All Files (*.*)|*.*", Title = "Select .npk file to extract" };
            if (d.ShowDialog() == true) ExtractInputTxt.Text = d.FileName;
        }

        private void BrowseExtractOutput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select output directory..." };
            if (d.ShowDialog() == true) ExtractOutputTxt.Text = d.FolderName;
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            string npkPath = ExtractInputTxt.Text;
            string outDir = ExtractOutputTxt.Text;

            if (string.IsNullOrWhiteSpace(npkPath) || !File.Exists(npkPath))
            {
                MessageBox.Show("Please select a valid .npk file to extract.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetDirectoryName(npkPath) ?? "", Path.GetFileNameWithoutExtension(npkPath) + "_extracted");
                ExtractOutputTxt.Text = outDir;
            }

            int gameId = GameProfileCombo.SelectedIndex;

            string args = $"-GM {gameId}";

            // File filter — only extract matching extensions (e.g. ".nut")
            string filter = FilterTxt.Text?.Trim();
            if (!string.IsNullOrEmpty(filter))
                args += $" -fe \"{filter.Replace("\"", "")}\"";

            // Skip already-existing files
            if (ChkSkipExisting.IsChecked == true)
                args += " -se";

            args += $" -u \"{npkPath}\" \"{outDir}\"";
            string exePath = Path.Combine("Utility", "DieselEngineBin", "NPK3Tool.exe");

            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, exePath, args, main);
        }

        private void BrowseRepackInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select input directory containing extracted files..." };
            if (d.ShowDialog() == true) RepackInputTxt.Text = d.FolderName;
        }

        private void BrowseRepackOutput_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog { Filter = "NPK Archive (*.npk)|*.npk", DefaultExt = "npk", Title = "Save repacked .npk as..." };
            if (d.ShowDialog() == true) RepackOutputTxt.Text = d.FileName;
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            string inDir = RepackInputTxt.Text;
            string outNpk = RepackOutputTxt.Text;

            if (string.IsNullOrWhiteSpace(inDir) || !Directory.Exists(inDir))
            {
                MessageBox.Show("Please select a valid folder to repack.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outNpk))
            {
                outNpk = Path.Combine(Path.GetDirectoryName(inDir) ?? "", Path.GetFileName(inDir) + "_new.npk");
                RepackOutputTxt.Text = outNpk;
            }

            int gameId = GameProfileCombo.SelectedIndex;

            string args = $"-GM {gameId}";
            
            if (ChkEnableSeg.IsChecked == true) args += " -sg 1";
            if (ChkForceSeg.IsChecked == true) args += " -fg 1";
            if (ChkCompress.IsChecked == true) args += " -cp 1";

            args += $" -r \"{inDir}\" \"{outNpk}\"";
            string exePath = Path.Combine("Utility", "DieselEngineBin", "NPK3Tool.exe");

            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync(AppDomain.CurrentDomain.BaseDirectory, exePath, args, main);
        }

        private void BrowseNutInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Nitroplus Squirrel Script (*.nut)|*.nut|All Files (*.*)|*.*", Title = "Select .nut file" };
            if (d.ShowDialog() == true) NutInputTxt.Text = d.FileName;
        }

        private async void ExtractNut_Click(object sender, RoutedEventArgs e)
        {
            string nutPath = NutInputTxt.Text;
            if (string.IsNullOrWhiteSpace(nutPath) || !File.Exists(nutPath))
            {
                MessageBox.Show("Please select a valid .nut file.");
                return;
            }

            string jsonPath = Path.Combine(Path.GetDirectoryName(nutPath) ?? "", Path.GetFileNameWithoutExtension(nutPath) + ".json");
            var main = Window.GetWindow(this) as MainWindow;
            main?.LogToConsole($"Extracting text from {Path.GetFileName(nutPath)} to {Path.GetFileName(jsonPath)}...");

            try
            {
                await System.Threading.Tasks.Task.Run(() => NitroplusNutTool.ExtractToJson(nutPath, jsonPath));
                main?.LogToConsole("Extraction Complete! You can now edit the JSON file.");
            }
            catch (Exception ex)
            {
                main?.LogToConsole($"Error extracting: {ex.Message}");
                MessageBox.Show($"Error extracting: {ex.Message}");
            }
        }

        private async void InjectNut_Click(object sender, RoutedEventArgs e)
        {
            string nutPath = NutInputTxt.Text;
            if (string.IsNullOrWhiteSpace(nutPath) || !File.Exists(nutPath))
            {
                MessageBox.Show("Please select a valid .nut file (the original).");
                return;
            }

            string jsonPath = Path.Combine(Path.GetDirectoryName(nutPath) ?? "", Path.GetFileNameWithoutExtension(nutPath) + ".json");
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show($"Cannot find translation JSON file: {jsonPath}");
                return;
            }

            string outputNut = Path.Combine(Path.GetDirectoryName(nutPath) ?? "", Path.GetFileNameWithoutExtension(nutPath) + "_translated.nut");
            var main = Window.GetWindow(this) as MainWindow;
            main?.LogToConsole($"Injecting text from {Path.GetFileName(jsonPath)} into {Path.GetFileName(outputNut)}...");

            try
            {
                await System.Threading.Tasks.Task.Run(() => NitroplusNutTool.InjectFromJson(nutPath, jsonPath, outputNut));
                main?.LogToConsole("Injection Complete! You can now repack your folder.");
            }
            catch (Exception ex)
            {
                main?.LogToConsole($"Error injecting: {ex.Message}");
                MessageBox.Show($"Error injecting: {ex.Message}");
            }
        }

    }
}
