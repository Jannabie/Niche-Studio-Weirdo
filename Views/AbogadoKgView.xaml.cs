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
            var d = new OpenFileDialog { Filter = "DSK Archives (*.dsk)|*.dsk|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) DskFileTxt.Text = d.FileName;
        }

        private void BrowsePft_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PFT Indexes (*.pft)|*.pft|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) PftFileTxt.Text = d.FileName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private async void UnpackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Please specify both DSK and PFT files to unpack.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Please specify an output folder (PNG SOURCE FOLDER field) to extract into.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcUNPACK.py <PFT> <DSK> <OUTPUT_FOLDER>
            await ToolRunner.RunAsync(repoDir, py, $"ArcUNPACK.py \"{PftFileTxt.Text}\" \"{DskFileTxt.Text}\" \"{PngFolderTxt.Text}\"", GetMain());
        }

        private async void ConvertPngKg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PngFolderTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            // python ArcKGPACK.py folder_gambar/
            await ToolRunner.RunAsync(repoDir, py, $"ArcKGPACK.py \"{PngFolderTxt.Text}\"", GetMain());
        }

        private async void PatchDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Please specify DSK, PFT, and the PNG/KG folder before patching.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;
            
            // Assume packed KGs are in a "packed_kg" subfolder if it exists, otherwise use the selected folder directly
            string kgFolder = System.IO.Path.Combine(PngFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = PngFolderTxt.Text;

            // python ArcPATCH.py GRAPHIC.dsk GRAPHIC.pft folder_packed_kg/
            await ToolRunner.RunAsync(repoDir, py, $"ArcPATCH.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{kgFolder}\"", GetMain());
        }

        private async void RebuildDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DskFileTxt.Text) || string.IsNullOrWhiteSpace(PftFileTxt.Text) || string.IsNullOrWhiteSpace(PngFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Please specify DSK, PFT, and the PNG/KG folder before rebuilding.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-KG");
            string py = SettingsManager.Config.PythonPath;

            string kgFolder = System.IO.Path.Combine(PngFolderTxt.Text, "packed_kg");
            if (!System.IO.Directory.Exists(kgFolder)) kgFolder = PngFolderTxt.Text;

            // python ArcPACK.py GRAPHIC.dsk GRAPHIC.pft folder_packed_kg/
            await ToolRunner.RunAsync(repoDir, py, $"ArcPACK.py \"{DskFileTxt.Text}\" \"{PftFileTxt.Text}\" \"{kgFolder}\"", GetMain());
        }
    }
}
