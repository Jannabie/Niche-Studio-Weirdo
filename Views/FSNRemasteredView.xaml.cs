using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class FSNRemasteredView : UserControl
    {
        public FSNRemasteredView()
        {
            InitializeComponent();
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "All Files (*.*)|*.*";
                
                if (dialog.ShowDialog() == true)
                {
                    string? targetName = btn.Tag?.ToString();
                    var targetBox = targetName != null ? (TextBox)this.FindName(targetName) : null;
                    if (targetBox != null)
                    {
                        targetBox.Text = dialog.FileName;
                    }
                }
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                TargetDirTxt.Text = dialog.FolderName;
            }
        }

        private async void BuildPatch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch build \"{TargetDirTxt.Text}\" --main-exe \"{MainExeTxt.Text}\" --some-key \"{SomeKeyTxt.Text}\" --out \"{outDir}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void DeploySteam_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch deploy \"{outDir}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch launcher \"{outDir}\" --game-exe \"{MainExeTxt.Text}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }
        private async void UnpackAuto_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(TargetDirTxt.Text, "extracted");
            string args = $"fsn-tools.py unpack auto \"{TargetDirTxt.Text}\" --out \"{outDir}\"";
            
            // If they provided a decrypt key, use it
            if (!string.IsNullOrWhiteSpace(DecryptKeyTxt.Text))
                args += $" --key \"{DecryptKeyTxt.Text}\"";

            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void DecryptEpk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            var files = System.IO.Directory.GetFiles(TargetDirTxt.Text, "*.epk");
            if (files.Length == 0)
            {
                main.LogToConsole("Fuzz Inc: No .epk files found in the target directory.");
                return;
            }

            string outDir = System.IO.Path.Combine(TargetDirTxt.Text, "epk_dec");
            string fileArgs = string.Join(" ", System.Linq.Enumerable.Select(files, f => $"\"{f}\""));
            string args = $"fsn-tools.py epk dec --out \"{outDir}\"";
            
            if (!string.IsNullOrWhiteSpace(MainExeTxt.Text)) args += $" --main-exe \"{MainExeTxt.Text}\"";
            if (!string.IsNullOrWhiteSpace(SomeKeyTxt.Text)) args += $" --some-key \"{SomeKeyTxt.Text}\"";

            args += " " + fileArgs;

            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Fuzz Inc");
            string py = SettingsManager.Config.PythonPath;

            var files = System.IO.Directory.GetFiles(TargetDirTxt.Text, "*.epk_dec", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                main.LogToConsole("Fuzz Inc: No .epk_dec files found in the target directory. Did you decrypt them first?");
                return;
            }

            string outFile = System.IO.Path.Combine(TargetDirTxt.Text, "translation_export.json");
            string fileArgs = string.Join(" ", System.Linq.Enumerable.Select(files, f => $"\"{f}\""));
            string args = $"fsn-tools.py translate export --out \"{outFile}\" {fileArgs}";

            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

    }
}
