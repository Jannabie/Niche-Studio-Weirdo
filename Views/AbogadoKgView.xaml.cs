using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class AbogadoKgView : UserControl
    {
        public AbogadoKgView() 
        { 
            InitializeComponent();
        }

        private void BrowseWorkFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) WorkFolderTxt.Text = d.FolderName;
        }

        private void BrowseDsk_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "DSK Archives (*.dsk;*.DSK)|*.dsk;*.DSK|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) DskFileTxt.Text = d.FileName;
        }

        private void BrowsePft_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PFT Indexes (*.pft;*.PFT)|*.pft;*.PFT|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) PftFileTxt.Text = d.FileName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        // ─── UNPACK DSK → .KG files ──────────────────────────────────────────
        // Workflow step 1: DSK → extract raw .KG files ke Working Folder
        // Setelah ini, buka .KG di GARbro untuk decode ke PNG, edit, lalu lanjut
        private async void UnpackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih file .DSK dan .PFT terlebih dulu sebelum Unpack.");
                return;
            }

            // Auto-isi Working Folder jika kosong: buat subfolder di sebelah DSK
            string workDir = WorkFolderTxt.Text;
            if (string.IsNullOrWhiteSpace(workDir))
            {
                workDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(DskFileTxt.Text)!,
                    System.IO.Path.GetFileNameWithoutExtension(DskFileTxt.Text) + "_extracted");
                WorkFolderTxt.Text = workDir;
            }

            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcUNPACK.py <file.dsk> <file.pft> <output_folder>
            await ToolRunner.RunAsync(repoDir, py,
                $"ArcUNPACK.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{workDir}\"",
                GetMain());
        }

        // ─── PNG → KG Convert (ArcKGPACK.py) ────────────────────────────────
        // Workflow step 3: PNG di Working Folder → .KG di packed_kg/
        // (Sebelumnya: decode .KG → PNG pakai GARbro, lalu edit PNG)
        private async void ConvertPngKg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WorkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih Working Folder (folder berisi file .PNG yang sudah diedit).");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcKGPACK.py <folder_png>
            await ToolRunner.RunAsync(repoDir, py,
                $"ArcKGPACK.py \"{WorkFolderTxt.Text}\"",
                GetMain());
        }

        // ─── PATCH DSK in-place (ArcPATCH.py) ──────────────────────────────
        // Workflow step 4a: Suntik .KG hasil pack ke .DSK tanpa rebuild penuh
        private async void PatchDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(WorkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Isi .DSK, .PFT, dan Working Folder sebelum Patch.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;

            // ArcKGPACK.py menyimpan hasil ke packed_kg/ — gunakan itu kalau ada
            string kgFolder = System.IO.Path.Combine(WorkFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = WorkFolderTxt.Text;

            // python ArcPATCH.py <file.dsk> <file.pft> <folder_kg>
            await ToolRunner.RunAsync(repoDir, py,
                $"ArcPATCH.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{kgFolder}\"",
                GetMain());
        }

        // ─── REBUILD FULL DSK (ArcPACK.py) ──────────────────────────────────
        // Workflow step 4b: Bangun archive .DSK + .PFT baru dari nol
        private async void RebuildDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(WorkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Isi .DSK, .PFT, dan Working Folder sebelum Rebuild.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;

            // ArcPACK.py: <PFT_ASLI> <FOLDER_DATA> <NAMA_OUTPUT>
            // Cari folder packed_kg dulu; fallback ke WorkFolder
            string kgFolder = System.IO.Path.Combine(WorkFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = WorkFolderTxt.Text;

            // Output = nama DSK original + "_new" (jangan overwrite langsung)
            string outBase = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(DskFileTxt.Text)!,
                System.IO.Path.GetFileNameWithoutExtension(DskFileTxt.Text) + "_new");

            await ToolRunner.RunAsync(repoDir, py,
                $"ArcPACK.py \"{PftFileTxt.Text}\" \"{kgFolder}\" \"{outBase}\"",
                GetMain());
        }
    }
}
