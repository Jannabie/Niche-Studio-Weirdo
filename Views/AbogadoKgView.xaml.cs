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

        private void BrowsePngFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) PngFolderTxt.Text = d.FolderName;
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

        // ─── UNPACK DSK → KG → PNG + kg_metadata.json ───────────────────────
        private async void UnpackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Isi DSK dan PFT sebelum Unpack.");
                return;
            }
            // Output ke folder yang dipilih, atau buat subfolder di sebelah DSK
            string outDir = PngFolderTxt.Text;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(DskFileTxt.Text)!,
                    System.IO.Path.GetFileNameWithoutExtension(DskFileTxt.Text) + "_extracted");
                PngFolderTxt.Text = outDir;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcUNPACK.py <file.dsk> <file.pft> <output_folder>
            await ToolRunner.RunAsync(repoDir, py,
                $"ArcUNPACK.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{outDir}\"",
                GetMain());
        }

        // ─── PNG → KG (ArcKGPACK.py) ────────────────────────────────────────
        private async void ConvertPngKg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih PNG folder terlebih dulu.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcKGPACK.py folder_gambar/
            await ToolRunner.RunAsync(repoDir, py, $"ArcKGPACK.py \"{PngFolderTxt.Text}\"", GetMain());
        }

        // ─── PATCH DSK in-place (ArcPATCH.py) ──────────────────────────────
        private async void PatchDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Isi DSK, PFT, dan Folder sebelum Patch.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            
            // ArcPATCH.py mengambil folder yang berisi file .KG
            // Jika ada subfolder packed_kg, gunakan itu; jika tidak, gunakan folder yang dipilih
            string kgFolder = System.IO.Path.Combine(PngFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = PngFolderTxt.Text;

            // python ArcPATCH.py GRAPHIC.dsk GRAPHIC.pft folder_packed_kg/
            await ToolRunner.RunAsync(repoDir, py,
                $"ArcPATCH.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{kgFolder}\"",
                GetMain());
        }

        // ─── REBUILD FULL DSK (ArcPACK.py) ──────────────────────────────────
        private async void RebuildDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Isi DSK, PFT, dan Folder sebelum Rebuild.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;

            string kgFolder = System.IO.Path.Combine(PngFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = PngFolderTxt.Text;

            // ArcPACK.py <PFT_ASLI> <FOLDER_DATA> <NAMA_OUTPUT>
            // Output base = nama DSK tanpa ekstensi (sebagai output baru)
            string outBase = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(DskFileTxt.Text)!,
                System.IO.Path.GetFileNameWithoutExtension(DskFileTxt.Text) + "_new");

            await ToolRunner.RunAsync(repoDir, py,
                $"ArcPACK.py \"{PftFileTxt.Text}\" \"{kgFolder}\" \"{outBase}\"",
                GetMain());
        }
    }
}
