using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class MeltyBloodView : UserControl
    {
        public MeltyBloodView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "P Archives (*.p)|*.p|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ArchiveTxt.Text = d.FileName;
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) FolderTxt.Text = d.FolderName;
        }

        private async void Unpack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchiveTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "MeltyBlood2002-tools");
            string py = SettingsManager.Config.PythonPath;
            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ArchiveTxt.Text) ?? "", "extracted_" + System.IO.Path.GetFileNameWithoutExtension(ArchiveTxt.Text));
            await ToolRunner.RunAsync(repoDir, py, $"mb_core.py unpack \"{ArchiveTxt.Text}\" \"{outDir}\"", GetMain());
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "MeltyBlood2002-tools");
            string py = SettingsManager.Config.PythonPath;
            string inDir = FolderTxt.Text;
            string outP = inDir + "_repacked.p";
            await ToolRunner.RunAsync(repoDir, py, $"mb_core.py repack \"{inDir}\" \"{outP}\"", GetMain());
        }

        private void HalfWidthInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HalfWidthInput == null || FullWidthOutput == null) return;
            var sb = new StringBuilder();
            foreach (char c in HalfWidthInput.Text)
            {
                // Convert ASCII printable to Fullwidth Unicode (offset = 0xFEE0)
                if (c >= 0x21 && c <= 0x7E)
                    sb.Append((char)(c + 0xFEE0));
                else if (c == ' ')
                    sb.Append('\u3000'); // ideographic space
                else
                    sb.Append(c);
            }
            FullWidthOutput.Text = sb.ToString();
        }

        private void CopyFullWidth_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FullWidthOutput.Text))
            {
                Clipboard.SetText(FullWidthOutput.Text);
                GetMain().LogToConsole("MeltyBlood: Fullwidth text copied to clipboard.");
            }
        }
    }
}
