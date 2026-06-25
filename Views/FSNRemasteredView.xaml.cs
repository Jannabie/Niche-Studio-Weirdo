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
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "FSN-Remastered-Decompiler");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch build \"{TargetDirTxt.Text}\" --main-exe \"{MainExeTxt.Text}\" --some-key \"{SomeKeyTxt.Text}\" --out \"{outDir}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void DeploySteam_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "FSN-Remastered-Decompiler");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch deploy \"{outDir}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }

        private async void LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetDirTxt.Text)) return;
            var main = (MainWindow)Window.GetWindow(this);
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "FSN-Remastered-Decompiler");
            string py = SettingsManager.Config.PythonPath;

            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TargetDirTxt.Text) ?? "", "patch_build");
            string args = $"fsn-tools.py patch launcher \"{outDir}\" --game-exe \"{MainExeTxt.Text}\"";
            await ToolRunner.RunAsync(repoDir, py, args, main);
        }
    }
}
