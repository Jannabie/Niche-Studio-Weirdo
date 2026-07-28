using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class LeafView : UserControl
    {
        public LeafView()
        {
            InitializeComponent();
        }

        // ===== ARCHIVE SECTION =====

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

            string exkizpakPath = UtilityResolver.GetToolPath("Leaf", "WA2-Arch-main\\exkizpak_v2.exe");
            if (!File.Exists(exkizpakPath))
            {
                main.LogToConsole("[ERROR] exkizpak_v2.exe not found in embedded tools.");
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

            string pakName = Path.GetFileName(PakFileTxt.Text);
            main.LogToConsole($"WA2: Unpacking {pakName} into {WorkspaceTxt.Text}");
            main.LogToConsole($"> Executing Embedded WA2-Arch tool on \"{PakFileTxt.Text}\"");
            main.LogToConsole($"NOTE: After extraction, move '{pakName}' OUT of the workspace before repacking.");

            var args = new List<string> { PakFileTxt.Text };
            _ = ToolRunner.RunAsync(WorkspaceTxt.Text, exkizpakPath, args, main);
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);

            if (string.IsNullOrWhiteSpace(WorkspaceTxt.Text) || !Directory.Exists(WorkspaceTxt.Text))
            {
                main.LogToConsole("[ERROR] Workspace directory not found.");
                return;
            }

            string pakName = Path.GetFileName(PakFileTxt.Text);
            if (string.IsNullOrWhiteSpace(pakName)) pakName = "output.pak";

            string outPak = Path.Combine(
                Path.GetDirectoryName(WorkspaceTxt.Text) ?? WorkspaceTxt.Text,
                "repacked_" + pakName);

            main.LogToConsole($"WA2: Native Repacking {WorkspaceTxt.Text} -> {outPak}");

            await KcapRepacker.RepackAsync(WorkspaceTxt.Text, outPak, (msg) =>
            {
                main.Dispatcher.Invoke(() => main.LogToConsole(msg));
            });
        }

        // ===== SCRIPT PARSER SECTION =====

        private void BrowseCsvFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", Title = "Select the raw comma-separated script .txt file" };
            if (dlg.ShowDialog() == true) CsvFileTxt.Text = dlg.FileName;
        }

        private void ParseCsv_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CsvFileTxt.Text) || !File.Exists(CsvFileTxt.Text))
            {
                MessageBox.Show("Please select a valid .txt file to parse.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string inputPath = CsvFileTxt.Text;
                string dir = Path.GetDirectoryName(inputPath) ?? string.Empty;
                string name = Path.GetFileNameWithoutExtension(inputPath);
                string ext = Path.GetExtension(inputPath);
                string outputPath = Path.Combine(dir, $"{name}_parsed{ext}");

                LeafTxtTool.ParseCsvToTxt(inputPath, outputPath);

                ((MainWindow)Application.Current.MainWindow).LogToConsole($"Parsed {Path.GetFileName(inputPath)} -> {Path.GetFileName(outputPath)}");
                MessageBox.Show($"File successfully parsed!\nSaved to:\n{outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseTxtFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", Title = "Select the translated parsed .txt file" };
            if (dlg.ShowDialog() == true) TxtFileTxt.Text = dlg.FileName;
        }

        private void InjectTxt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFileTxt.Text) || !File.Exists(TxtFileTxt.Text))
            {
                MessageBox.Show("Please select a valid translated .txt file to inject.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string inputPath = TxtFileTxt.Text;
                string dir = Path.GetDirectoryName(inputPath) ?? string.Empty;
                string name = Path.GetFileNameWithoutExtension(inputPath);

                if (name.EndsWith("_parsed"))
                    name = name.Substring(0, name.Length - 7);

                string ext = Path.GetExtension(inputPath);
                string outputPath = Path.Combine(dir, $"{name}_repacked{ext}");

                LeafTxtTool.InjectTxtToCsv(inputPath, outputPath);

                ((MainWindow)Application.Current.MainWindow).LogToConsole($"Injected {Path.GetFileName(inputPath)} -> {Path.GetFileName(outputPath)}");
                MessageBox.Show($"Translation successfully injected!\nSaved to:\n{outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to inject translation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
