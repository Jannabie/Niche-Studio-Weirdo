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
                string dir = Path.GetDirectoryName(packFile) ?? "";
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                
                if (dir.StartsWith(progFiles, StringComparison.OrdinalIgnoreCase) || 
                    dir.StartsWith(progFilesX86, StringComparison.OrdinalIgnoreCase))
                {
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                
                outDir = Path.Combine(dir, Path.GetFileNameWithoutExtension(packFile));
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
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"Permission denied: You do not have write access to '{outDir}'.\n\nPlease select a different output folder, or run Niche Studio as Administrator.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var dialog = new OpenFileDialog
            {
                Title = "Select QLIE .s script file(s)",
                Filter = "QLIE Script files (*.s)|*.s|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                // If single file, show just that file; if multiple, show the folder
                if (dialog.FileNames.Length == 1)
                    InputScriptsFolderBox.Text = dialog.FileName;
                else
                    InputScriptsFolderBox.Text = Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
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
            string inPath = InputScriptsFolderBox.Text;
            string outJson = OutputJsonBox.Text;

            if (string.IsNullOrWhiteSpace(inPath))
            {
                MessageBox.Show("Please select a .s script file or folder.");
                return;
            }

            // Accept either a single file or a folder
            bool isFile = File.Exists(inPath);
            bool isFolder = Directory.Exists(inPath);
            if (!isFile && !isFolder)
            {
                MessageBox.Show("Please select a valid .s script file or folder.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outJson))
            {
                string baseDir = isFile ? Path.GetDirectoryName(inPath) ?? "" : inPath;
                outJson = Path.Combine(baseDir, "qlie_translation.json");
                OutputJsonBox.Text = outJson;
            }

            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;

            string vnTextPatchDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "VNTextPatch");
            string vnTextPatchExe = Path.Combine(vnTextPatchDir, "VNTextPatch.exe");
            string args = $"extractlocal \"{inPath}\" \"{outJson}\"";

            await ToolRunner.RunAsync(vnTextPatchDir, vnTextPatchExe, args, main);
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
            string inPath = InputScriptsFolderBox.Text;
            string outFolder = OutputScriptsFolderBox.Text;

            if (string.IsNullOrWhiteSpace(jsonFile) || !File.Exists(jsonFile))
            {
                MessageBox.Show("Please select a valid translated JSON file.");
                return;
            }

            bool isFile = File.Exists(inPath);
            bool isFolder = Directory.Exists(inPath);
            if (!isFile && !isFolder)
            {
                MessageBox.Show("Please ensure the input .s file/folder (STEP 3) is valid. VNTextPatch needs the originals to inject the translation.");
                return;
            }

            string baseDir = isFile ? Path.GetDirectoryName(inPath) ?? "" : inPath;
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                outFolder = Path.Combine(baseDir, "translated");
                OutputScriptsFolderBox.Text = outFolder;
            }

            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;

            string vnTextPatchDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "VNTextPatch");
            string vnTextPatchExe = Path.Combine(vnTextPatchDir, "VNTextPatch.exe");
            string args = $"insertlocal \"{inPath}\" \"{jsonFile}\" \"{outFolder}\"";

            await ToolRunner.RunAsync(vnTextPatchDir, vnTextPatchExe, args, main);
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
