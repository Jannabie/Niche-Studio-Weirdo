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
    }
}
