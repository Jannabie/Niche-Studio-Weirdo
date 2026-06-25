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
            // Pre-fill from saved settings
            if (!string.IsNullOrEmpty(SettingsManager.Config.ExkizpakPath))
                ExkizpakTxt.Text = SettingsManager.Config.ExkizpakPath;
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
                main.LogToConsole("[ERROR] exkizpak_v2.exe not found. Please locate it first.");
                return;
            }
            if (string.IsNullOrWhiteSpace(WorkspaceTxt.Text) || !Directory.Exists(WorkspaceTxt.Text))
            {
                main.LogToConsole("[ERROR] Workspace directory not found. Please select a valid empty folder.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PakFileTxt.Text) || !File.Exists(PakFileTxt.Text))
            {
                main.LogToConsole("[ERROR] .pak file not found. Please select a valid .pak file.");
                return;
            }

            SettingsManager.Config.ExkizpakPath = ExkizpakTxt.Text;
            SettingsManager.Save();

            string pakName = Path.GetFileName(PakFileTxt.Text);
            main.LogToConsole($"WA2: Unpacking {pakName} into {WorkspaceTxt.Text}");
            main.LogToConsole($"> Executing: {ExkizpakTxt.Text} \"{PakFileTxt.Text}\"");
            main.LogToConsole($"NOTE: After extraction, move '{pakName}' and 'exkizpak_v2.exe' OUT of the workspace before repacking.");

            // exkizpak_v2.exe takes the .pak file as a direct argument — no flags needed.
            // It will extract into a subfolder inside the workspace.
            // We run it from the workspace directory so output lands there.
            string arguments = $"\"{PakFileTxt.Text}\"";
            _ = ToolRunner.RunAsync(WorkspaceTxt.Text, ExkizpakTxt.Text, arguments, main);
        }

        private void Repack_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);

            if (string.IsNullOrWhiteSpace(WorkspaceTxt.Text) || !Directory.Exists(WorkspaceTxt.Text))
            {
                main.LogToConsole("[ERROR] Workspace directory not found.");
                return;
            }

            // For repacking, we use kcap_repack.py from the WA2-Arch repo.
            // Command: python kcap_repack.py <extracted_folder> <output.pak>
            string repoDir = Path.Combine(SettingsManager.Config.ReposPath, "WA2-Arch", "WA2-Arch-main");
            string py = SettingsManager.Config.PythonPath;

            if (string.IsNullOrWhiteSpace(py) || !File.Exists(py))
            {
                main.LogToConsole("[ERROR] Python not found. Please set Python path in Settings.");
                return;
            }

            string kcapScript = Path.Combine(repoDir, "kcap_repack.py");
            if (!File.Exists(kcapScript))
            {
                main.LogToConsole($"[ERROR] kcap_repack.py not found at: {kcapScript}");
                main.LogToConsole("Please make sure the WA2-Arch repo is inside your repos folder.");
                return;
            }

            // The workspace folder should contain only the extracted subfolder (e.g. "script/").
            // Output .pak will be placed next to the workspace.
            string pakName = Path.GetFileName(PakFileTxt.Text);
            if (string.IsNullOrWhiteSpace(pakName)) pakName = "output.pak";

            string outPak = Path.Combine(
                Path.GetDirectoryName(WorkspaceTxt.Text) ?? WorkspaceTxt.Text,
                "repacked_" + pakName);

            main.LogToConsole($"WA2: Repacking {WorkspaceTxt.Text} -> {outPak}");
            main.LogToConsole($"> python kcap_repack.py \"{WorkspaceTxt.Text}\" \"{outPak}\"");

            // python kcap_repack.py <folder> <output.pak>
            string args = $"kcap_repack.py \"{WorkspaceTxt.Text}\" \"{outPak}\"";
            _ = ToolRunner.RunAsync(repoDir, py, args, main);
        }
    }
}
