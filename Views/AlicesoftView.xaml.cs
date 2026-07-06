using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class AlicesoftView : UserControl
    {
        public AlicesoftView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        // ═══════════════════════════════════════════════════════
        // EXTRACT ARCHIVE
        // ═══════════════════════════════════════════════════════

        private void BrowseExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Alicesoft Archives|*.afa;*.ald;*.dat|All Files|*.*", Title = "Select archive to extract" };
            if (dlg.ShowDialog() == true) ExtractArchiveTxt.Text = dlg.FileName;
        }

        private void BrowseExtractOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select output folder for extraction" };
            if (dlg.ShowDialog() == true) ExtractOutputTxt.Text = dlg.FolderName;
        }

        private async void ListArchive_Click(object sender, RoutedEventArgs e)
        {
            string archivePath = ExtractArchiveTxt.Text;
            if (string.IsNullOrWhiteSpace(archivePath) || archivePath.StartsWith("Select")) return;
            await AlicesoftUtils.ListArchiveAsync(archivePath, GetMain());
        }

        private async void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            string archivePath = ExtractArchiveTxt.Text;
            if (string.IsNullOrWhiteSpace(archivePath) || archivePath.StartsWith("Select") || !File.Exists(archivePath))
            {
                MessageBox.Show("Please select a valid archive file first.", "Missing Input");
                return;
            }

            // Auto-generate output dir next to archive if not specified
            string outDir = ExtractOutputTxt.Text;
            if (string.IsNullOrWhiteSpace(outDir) || outDir.StartsWith("Select"))
            {
                outDir = Path.Combine(
                    Path.GetDirectoryName(archivePath) ?? "",
                    Path.GetFileNameWithoutExtension(archivePath) + "_extracted");
                ExtractOutputTxt.Text = outDir;
            }

            await AlicesoftUtils.ExtractArchiveAsync(archivePath, outDir, GetMain());
        }

        // ═══════════════════════════════════════════════════════
        // PACK AFA
        // ═══════════════════════════════════════════════════════

        private void BrowsePackInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog { Title = "Select folder of files to pack" };
            if (dlg.ShowDialog() == true) PackInputTxt.Text = dlg.FolderName;
        }

        private void BrowsePackOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save output AFA archive as...",
                Filter = "AliceSoft AFA Archive (*.afa)|*.afa",
                DefaultExt = "afa"
            };
            if (dlg.ShowDialog() == true) PackOutputTxt.Text = dlg.FileName;
        }

        private async void PackArchive_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder = PackInputTxt.Text;
            if (string.IsNullOrWhiteSpace(inputFolder) || inputFolder.StartsWith("Select") || !Directory.Exists(inputFolder))
            {
                MessageBox.Show("Please select a valid folder to pack.", "Missing Input");
                return;
            }

            // Auto-generate output name if not specified
            string outAfa = PackOutputTxt.Text;
            if (string.IsNullOrWhiteSpace(outAfa) || outAfa.StartsWith("Select"))
            {
                string parent = Path.GetDirectoryName(inputFolder.TrimEnd('\\', '/')) ?? "";
                string folderName = Path.GetFileName(inputFolder.TrimEnd('\\', '/'));
                outAfa = Path.Combine(parent, folderName + "_repacked.afa");
                PackOutputTxt.Text = outAfa;
            }

            await AlicesoftUtils.PackArchiveAsync(inputFolder, outAfa, GetMain());
        }

        // ═══════════════════════════════════════════════════════
        // SCRIPT TOOLS (.ain)
        // ═══════════════════════════════════════════════════════

        private void BrowseAin_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "AIN Files (*.ain)|*.ain|All Files|*.*" };
            if (dlg.ShowDialog() == true) AinFileTxt.Text = dlg.FileName;
        }

        private void BrowseAinTxt_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true) AinTxtTxt.Text = dlg.FileName;
        }

        private async void DumpAin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AinFileTxt.Text) || AinFileTxt.Text.StartsWith("Select")) return;
            var dlg = new SaveFileDialog { Title = "Save Dumped TXT As", Filter = "Text File (*.txt)|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true)
                await AlicesoftUtils.DumpAinAsync(AinFileTxt.Text, dlg.FileName, GetMain());
        }

        private async void EditAin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AinFileTxt.Text) || AinFileTxt.Text.StartsWith("Select")) return;
            if (string.IsNullOrWhiteSpace(AinTxtTxt.Text) || AinTxtTxt.Text.StartsWith("Select")) return;
            var dlg = new SaveFileDialog { Filter = "AIN Files (*.ain)|*.ain|All Files|*.*" };
            if (dlg.ShowDialog() == true)
                await AlicesoftUtils.EditAinAsync(AinFileTxt.Text, AinTxtTxt.Text, dlg.FileName, GetMain());
        }

        // ═══════════════════════════════════════════════════════
        // DATABASE TOOLS (.ex)
        // ═══════════════════════════════════════════════════════

        private void BrowseEx_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "EX Files (*.ex)|*.ex|All Files|*.*" };
            if (dlg.ShowDialog() == true) ExFileTxt.Text = dlg.FileName;
        }

        private void BrowseExTxt_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true) ExTxtTxt.Text = dlg.FileName;
        }

        private async void DumpEx_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ExFileTxt.Text) || ExFileTxt.Text.StartsWith("Select")) return;
            var dlg = new SaveFileDialog { Title = "Save Dumped TXT As", Filter = "Text File (*.txt)|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true)
                await AlicesoftUtils.DumpExAsync(ExFileTxt.Text, dlg.FileName, GetMain());
        }

        private async void EditEx_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ExFileTxt.Text) || ExFileTxt.Text.StartsWith("Select")) return;
            if (string.IsNullOrWhiteSpace(ExTxtTxt.Text) || ExTxtTxt.Text.StartsWith("Select")) return;
            var dlg = new SaveFileDialog { Filter = "EX Files (*.ex)|*.ex|All Files|*.*" };
            if (dlg.ShowDialog() == true)
                await AlicesoftUtils.EditExAsync(ExFileTxt.Text, ExTxtTxt.Text, dlg.FileName, GetMain());
        }

        // ═══════════════════════════════════════════════════════
        // IMAGE TOOLS (.cg)
        // ═══════════════════════════════════════════════════════

        private void BrowseCg_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Image Files (*.cg;*.png;*.webp)|*.cg;*.png;*.webp|All Files|*.*" };
            if (dlg.ShowDialog() == true) CgFileTxt.Text = dlg.FileName;
        }

        private async void ConvertCg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CgFileTxt.Text) || CgFileTxt.Text.StartsWith("Select")) return;
            string ext = Path.GetExtension(CgFileTxt.Text).ToLower();
            string outFilter = ext == ".cg" ? "Image Files (*.png;*.webp)|*.png;*.webp" : "CG Files (*.cg)|*.cg";
            var dlg = new SaveFileDialog { Filter = outFilter };
            if (dlg.ShowDialog() == true)
                await AlicesoftUtils.ConvertCgAsync(CgFileTxt.Text, dlg.FileName, GetMain());
        }
    }
}
