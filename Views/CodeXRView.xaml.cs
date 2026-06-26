using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class CodeXRView : UserControl
    {
        public CodeXRView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "GSC Files (*.gsc)|*.gsc|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) TargetTxt.Text = d.FileName;
        }
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) TargetTxt.Text = d.FolderName;
        }

        private string GetEncoding() => ((ComboBoxItem)EncodingCombo.SelectedItem).Content.ToString().Contains("UTF") ? "utf-8" : "shift-jis";

        private string GetScriptPath() => Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "codeX-RScript-tools", "gsc_tool.py");

        private bool IsBatchMode()
        {
            bool isBatch = false;
            Application.Current.Dispatcher.Invoke(() => isBatch = ModeCombo.SelectedIndex == 1);
            return isBatch;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetTxt.Text)) return;
            var main = GetMain();
            string args = "";

            if (IsBatchMode())
            {
                string jsonDir = Path.Combine(TargetTxt.Text, "json");
                args = $"\"{GetScriptPath()}\" export-all \"{Path.Combine(TargetTxt.Text, "*.gsc")}\" -d \"{jsonDir}\"";
            }
            else
            {
                string jsonFile = Path.ChangeExtension(TargetTxt.Text, ".json");
                args = $"\"{GetScriptPath()}\" export \"{TargetTxt.Text}\" -o \"{jsonFile}\"";
            }

            main.LogToConsole($"CodeX: Exporting GSC ↁEJSON [{GetEncoding()}]...");
            _ = ToolRunner.RunAsync(Path.GetDirectoryName(GetScriptPath()) ?? "", SettingsManager.Config.PythonPath, args, main);
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetTxt.Text)) return;
            var main = GetMain();
            string args = "";

            if (IsBatchMode())
            {
                string jsonDir = Path.Combine(TargetTxt.Text, "json");
                string repackedDir = Path.Combine(TargetTxt.Text, "repacked");
                args = $"\"{GetScriptPath()}\" import-all \"{Path.Combine(TargetTxt.Text, "*.gsc")}\" -d \"{jsonDir}\" -o \"{repackedDir}\" --encoding {GetEncoding()}";
            }
            else
            {
                string jsonFile = Path.ChangeExtension(TargetTxt.Text, ".json");
                string repackedFile = Path.Combine(Path.GetDirectoryName(TargetTxt.Text) ?? "", Path.GetFileNameWithoutExtension(TargetTxt.Text) + "_translated.gsc");
                args = $"\"{GetScriptPath()}\" import \"{TargetTxt.Text}\" \"{jsonFile}\" -o \"{repackedFile}\" --encoding {GetEncoding()}";
            }

            main.LogToConsole($"CodeX: Importing JSON ↁEGSC [{GetEncoding()}]...");
            _ = ToolRunner.RunAsync(Path.GetDirectoryName(GetScriptPath()) ?? "", SettingsManager.Config.PythonPath, args, main);
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetTxt.Text)) return;
            var main = GetMain();
            string args = "";

            if (IsBatchMode())
            {
                args = $"\"{GetScriptPath()}\" verify \"{Path.Combine(TargetTxt.Text, "*.gsc")}\"";
            }
            else
            {
                args = $"\"{GetScriptPath()}\" verify \"{TargetTxt.Text}\"";
            }

            main.LogToConsole($"CodeX: Running 100% roundtrip verification...");
            _ = ToolRunner.RunAsync(Path.GetDirectoryName(GetScriptPath()) ?? "", SettingsManager.Config.PythonPath, args, main);
        }
    }
}
