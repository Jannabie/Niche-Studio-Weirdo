using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class WA2ArchView : UserControl
    {
        public WA2ArchView()
        {
            InitializeComponent();
        }

        private void BrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Executable (*.exe)|*.exe" };
            if (dialog.ShowDialog() == true) ExkizpakTxt.Text = dialog.FileName;
        }

        private void BrowseWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true) WorkspaceTxt.Text = dialog.FolderName;
        }

        private void BrowsePak_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "PAK Archives (*.pak)|*.pak|All Files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) PakFileTxt.Text = dialog.FileName;
        }

        private void Unpack_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            if (string.IsNullOrWhiteSpace(ExkizpakTxt.Text) || !File.Exists(ExkizpakTxt.Text))
            {
                main.LogToConsole("Error: Please locate exkizpak_v2.exe first.");
                return;
            }
            if (string.IsNullOrWhiteSpace(WorkspaceTxt.Text))
            {
                main.LogToConsole("Error: Please select an isolated workspace directory.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PakFileTxt.Text) || !File.Exists(PakFileTxt.Text))
            {
                main.LogToConsole("Error: Please select a valid .pak file.");
                return;
            }
            main.LogToConsole($"WA2: Unpacking {Path.GetFileName(PakFileTxt.Text)} ↁE{WorkspaceTxt.Text}");
            SettingsManager.Config.ExkizpakPath = ExkizpakTxt.Text;
            SettingsManager.Save();
            
            string arguments = $"-x \"{PakFileTxt.Text}\""; // Assumed arguments based on typical unpackers
            _ = ToolRunner.RunAsync(WorkspaceTxt.Text, ExkizpakTxt.Text, arguments, main);
        }

        private void Repack_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            if (string.IsNullOrWhiteSpace(WorkspaceTxt.Text))
            {
                main.LogToConsole("Error: Please select the workspace directory to repack from.");
                return;
            }
            if (string.IsNullOrWhiteSpace(ExkizpakTxt.Text) || !File.Exists(ExkizpakTxt.Text))
            {
                main.LogToConsole("Error: Please locate exkizpak_v2.exe first.");
                return;
            }
            
            main.LogToConsole($"WA2: Repacking workspace {WorkspaceTxt.Text} ↁE.pak");
            string arguments = $"-c \"{WorkspaceTxt.Text}\""; // Assumed arguments based on typical repackers
            _ = ToolRunner.RunAsync(WorkspaceTxt.Text, ExkizpakTxt.Text, arguments, main);
        }
    }
}
