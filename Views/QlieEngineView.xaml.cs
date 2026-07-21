using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NicheStudioWeirdo.Views
{
    public partial class QlieEngineView : UserControl
    {
        public QlieEngineView()
        {
            InitializeComponent();
        }

        private void BrowsePackInputBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select QLIE .pack file",
                Filter = "QLIE Archive (*.pack)|*.pack|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                PackInputBox.Text = dialog.FileName;
            }
        }

        private void BrowsePackOutputBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select extraction folder" };
            if (dialog.ShowDialog() == true)
            {
                PackOutputBox.Text = dialog.FolderName;
            }
        }

        private async void UnpackPackBtn_Click(object sender, RoutedEventArgs e)
        {
            string packFile = PackInputBox.Text;
            string outDir = PackOutputBox.Text;

            if (string.IsNullOrWhiteSpace(packFile) || !File.Exists(packFile))
            {
                MessageBox.Show("Please select a valid .pack file.");
                return;
            }
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetDirectoryName(packFile) ?? "", Path.GetFileNameWithoutExtension(packFile));
                PackOutputBox.Text = outDir;
            }

            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    var extractor = new NicheStudioWeirdo.Utils.QlieExtractor();
                    extractor.ExtractPack(packFile, outDir);
                });
                MessageBox.Show($"Extracted successfully to {outDir}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Extraction failed: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BrowseInputScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select folder containing extracted QLIE .s script files" };
            if (dialog.ShowDialog() == true)
            {
                InputScriptsFolderBox.Text = dialog.FolderName;
            }
        }

        private void BrowseOutputJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Translation JSON",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "qlie_translation.json"
            };
            if (dialog.ShowDialog() == true)
            {
                OutputJsonBox.Text = dialog.FileName;
            }
        }

        private async void ParseScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            string inFolder = InputScriptsFolderBox.Text;
            string outJson = OutputJsonBox.Text;

            if (string.IsNullOrWhiteSpace(inFolder) || !Directory.Exists(inFolder))
            {
                MessageBox.Show("Please select a valid folder containing the original .s script files.");
                return;
            }
            if (string.IsNullOrWhiteSpace(outJson))
            {
                outJson = Path.Combine(inFolder, "qlie_translation.json");
                OutputJsonBox.Text = outJson;
            }

            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;

            // VNTextPatch extractlocal infile|infolder scriptfile|scriptfolder
            // Notice VNTextPatch arguments: extractlocal <input_scripts_folder> <output_json>
            string args = $"extractlocal \"{inFolder}\" \"{outJson}\"";
            string workingDir = Path.Combine("Utility", "VNTextPatch");

            await ToolRunner.RunAsync(workingDir, "VNTextPatch.exe", args, main);
        }

        private void BrowseTranslatedJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Translated JSON File",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                TranslatedJsonBox.Text = dialog.FileName;
            }
        }

        private void BrowseOutputScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select output folder for the newly translated .s script files" };
            if (dialog.ShowDialog() == true)
            {
                OutputScriptsFolderBox.Text = dialog.FolderName;
            }
        }

        private async void RepackScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            string jsonFile = TranslatedJsonBox.Text;
            string inFolder = InputScriptsFolderBox.Text;
            string outFolder = OutputScriptsFolderBox.Text;

            if (string.IsNullOrWhiteSpace(jsonFile) || !File.Exists(jsonFile))
            {
                MessageBox.Show("Please select a valid translated JSON file.");
                return;
            }
            if (string.IsNullOrWhiteSpace(inFolder) || !Directory.Exists(inFolder))
            {
                MessageBox.Show("Please ensure the input scripts folder (STEP 2) points to the original .s files. VNTextPatch needs the originals to inject the translation.");
                return;
            }
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = Path.Combine(Path.GetDirectoryName(inFolder) ?? "", Path.GetFileName(inFolder) + "_translated");
                OutputScriptsFolderBox.Text = outFolder;
            }

            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }

            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;

            // VNTextPatch insertlocal infile|infolder scriptfile|scriptfolder outfile|outfolder
            // Usage for injecting: insertlocal <original_scripts_folder> <translation_json> <output_scripts_folder>
            string args = $"insertlocal \"{inFolder}\" \"{jsonFile}\" \"{outFolder}\"";
            string workingDir = Path.Combine("Utility", "VNTextPatch");

            await ToolRunner.RunAsync(workingDir, "VNTextPatch.exe", args, main);
        }
        private void BrowseRepackInputBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select folder with translated scripts to repack" };
            if (dialog.ShowDialog() == true)
            {
                RepackInputBox.Text = dialog.FolderName;
            }
        }

        private void BrowseRepackOutputBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Repacked .pack file",
                Filter = "QLIE Archive (*.pack)|*.pack|All files (*.*)|*.*",
                FileName = "data1_translated.pack"
            };
            if (dialog.ShowDialog() == true)
            {
                RepackOutputBox.Text = dialog.FileName;
            }
        }

        private async void RepackPackBtn_Click(object sender, RoutedEventArgs e)
        {
            string inDir = RepackInputBox.Text;
            string outPack = RepackOutputBox.Text;

            if (string.IsNullOrWhiteSpace(inDir) || !Directory.Exists(inDir))
            {
                MessageBox.Show("Please select a valid input folder containing the scripts.");
                return;
            }
            if (string.IsNullOrWhiteSpace(outPack))
            {
                MessageBox.Show("Please specify an output .pack file.");
                return;
            }

            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    var extractor = new NicheStudioWeirdo.Utils.QlieExtractor();
                    extractor.RepackPack(inDir, outPack);
                });
                MessageBox.Show($"Repacked successfully to {outPack}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Repack failed: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}
