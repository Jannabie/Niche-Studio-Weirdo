using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NicheStudioWeirdo.Views
{
    public partial class AlicesoftView : UserControl
    {
        public AlicesoftView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        // Archive
        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Alicesoft Archives|*.afa;*.ald;*.dat|All Files|*.*" };
            if (dlg.ShowDialog() == true) ArchivePathTxt.Text = dlg.FileName;
        }

        private void BrowseArchiveDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                ArchiveDirTxt.Text = dialog.FolderName;
            }
        }

        private async void ListArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchivePathTxt.Text) || ArchivePathTxt.Text.Contains("Select")) return;
            await Utils.AlicesoftUtils.ListArchiveAsync(ArchivePathTxt.Text, GetMain());
        }

        private async void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchivePathTxt.Text) || ArchivePathTxt.Text.Contains("Select")) return;
            var dialog = new OpenFolderDialog { Title = "Select Output Folder for Extraction" };
            if (dialog.ShowDialog() == true)
            {
                await Utils.AlicesoftUtils.ExtractArchiveAsync(ArchivePathTxt.Text, dialog.FolderName, GetMain());
            }
        }

        private async void PackArchive_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArchiveDirTxt.Text) || ArchiveDirTxt.Text.Contains("Select")) return;
            SaveFileDialog dlg = new SaveFileDialog { Filter = "Alicesoft Archive (*.afa)|*.afa|Alicesoft Archive (*.ald)|*.ald|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                await Utils.AlicesoftUtils.PackArchiveAsync(ArchiveDirTxt.Text, dlg.FileName, GetMain());
            }
        }

        // Script (AIN)
        private void BrowseAin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "AIN Files (*.ain)|*.ain|All Files|*.*" };
            if (dlg.ShowDialog() == true) AinFileTxt.Text = dlg.FileName;
        }

        private void BrowseAinJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json|All Files|*.*" };
            if (dlg.ShowDialog() == true) AinJsonTxt.Text = dlg.FileName;
        }

        private async void DumpAin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AinFileTxt.Text) || AinFileTxt.Text.Contains("Select")) return;
            var dialog = new OpenFolderDialog { Title = "Select Output Folder for Dump" };
            if (dialog.ShowDialog() == true)
            {
                await Utils.AlicesoftUtils.DumpAinAsync(AinFileTxt.Text, dialog.FolderName, GetMain());
            }
        }

        private async void EditAin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AinFileTxt.Text) || AinFileTxt.Text.Contains("Select")) return;
            if (string.IsNullOrWhiteSpace(AinJsonTxt.Text) || AinJsonTxt.Text.Contains("Select")) return;
            
            SaveFileDialog dlg = new SaveFileDialog { Filter = "AIN Files (*.ain)|*.ain|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                await Utils.AlicesoftUtils.EditAinAsync(AinFileTxt.Text, AinJsonTxt.Text, dlg.FileName, GetMain());
            }
        }

        // Image (CG)
        private void BrowseCg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Image Files (*.cg;*.png;*.webp)|*.cg;*.png;*.webp|All Files|*.*" };
            if (dlg.ShowDialog() == true) CgFileTxt.Text = dlg.FileName;
        }

        private async void ConvertCg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CgFileTxt.Text) || CgFileTxt.Text.Contains("Select")) return;
            
            string ext = Path.GetExtension(CgFileTxt.Text).ToLower();
            string outFilter = ext == ".cg" ? "Image Files (*.png;*.webp)|*.png;*.webp" : "CG Files (*.cg)|*.cg";
            
            SaveFileDialog dlg = new SaveFileDialog { Filter = outFilter };
            if (dlg.ShowDialog() == true)
            {
                await Utils.AlicesoftUtils.ConvertCgAsync(CgFileTxt.Text, dlg.FileName, GetMain());
            }
        }
    }
}
