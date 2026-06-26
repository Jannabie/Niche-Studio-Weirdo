using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class WagaHimeView : UserControl
    {
        public WagaHimeView()
        {
            InitializeComponent();
        }

        private void BrowseDat_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "ACV1 Archive (*.dat)|*.dat|All Files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                DatFileTxt.Text = dialog.FileName;
            }
        }

        private void BrowseDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                ExtractedDirTxt.Text = dialog.FolderName;
            }
        }

        private async void Unpack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DatFileTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            // Scripts are in the WagaHime-Arc subfolder
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "WagaHime-Tools", "WagaHime-Arc");
            string py = SettingsManager.Config.PythonPath;
            
            // acv1_extractor.py <file.dat> [options]  Eoutput auto goes to <name>_extracted/
            string args = $"acv1_extractor.py \"{DatFileTxt.Text}\" --master-key {MasterKeyTxt.Text} --script-key {ScriptKeyTxt.Text}";
            if (SkipRawChk.IsChecked == true) args += " --no-raw";
            if (SkipTextChk.IsChecked == true) args += " --no-text";
            
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DatFileTxt.Text) || string.IsNullOrWhiteSpace(ExtractedDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            // Scripts are in the WagaHime-Arc subfolder
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "WagaHime-Tools", "WagaHime-Arc");
            string py = SettingsManager.Config.PythonPath;

            string datFile = DatFileTxt.Text;
            string extractedDir = ExtractedDirTxt.Text;
            string outDat = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(datFile) ?? "",
                System.IO.Path.GetFileNameWithoutExtension(datFile) + "_patched.dat");

            // acv1_repacker.py <original.dat> <extracted_dir> --out <output.dat> [options]
            string args = $"acv1_repacker.py \"{datFile}\" \"{extractedDir}\" --out \"{outDat}\" --master-key {MasterKeyTxt.Text} --script-key {ScriptKeyTxt.Text} --level {(int)ZlibSlider.Value}";
            
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }
    }
}
