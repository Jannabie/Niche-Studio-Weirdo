using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class BGIView : UserControl
    {
        public BGIView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseDep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var d = new OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (d.ShowDialog() == true)
                {
                    var box = (TextBox)this.FindName(btn.Tag.ToString());
                    if (box != null) box.Text = d.FileName;
                }
            }
        }
        private void BrowseScripts_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) ScriptFolderTxt.Text = d.FolderName;
        }

        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "ARC Files (*.arc)|*.arc|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ArchiveTxt.Text = d.FileName;
        }

        private async void ExtractScripts_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchiveTxt.Text) || string.IsNullOrWhiteSpace(ScriptFolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "BGI-Translator");
            await ToolRunner.RunAsync(repoDir, CSystemArcTxt.Text, $"extract \"{ArchiveTxt.Text}\" \"{ScriptFolderTxt.Text}\\\"", GetMain());
        }

        private async void ExportTsv_Click(object sender, RoutedEventArgs e)
        {
            // Using BgiDisassembler.exe to repack or process if needed, or we just map Repack
            if (string.IsNullOrWhiteSpace(ArchiveTxt.Text) || string.IsNullOrWhiteSpace(ScriptFolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "BGI-Translator");
            string outArc = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ArchiveTxt.Text) ?? "", "patched.arc");
            await ToolRunner.RunAsync(repoDir, CSystemArcTxt.Text, $"pack \"{ScriptFolderTxt.Text}\\\" \"{outArc}\"", GetMain());
        }

        private void ImportTsv_Click(object sender, RoutedEventArgs e)        => GetMain().LogToConsole("BGI: Importing TSV and injecting translations (Stubbed)");
        private void BatchGlossary_Click(object sender, RoutedEventArgs e)    => GetMain().LogToConsole("BGI: Batch glossary substitution running (Stubbed)");
    }
}
