using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class HuneXMahoyoView : UserControl
    {
        public HuneXMahoyoView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseHfa_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "HFA Archive (*.hfa)|*.hfa|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) HfaFileTxt.Text = d.FileName;
        }

        private void BrowseHfaFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) HfaFolderTxt.Text = d.FolderName;
        }

        private void BrowseOrig_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Mahoyo Assets (*.ctd;*.cbg;*.mzp)|*.ctd;*.cbg;*.mzp|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) 
            {
                OriginalFileTxt.Text = d.FileName;
                
                // Smart Auto-fill modded file
                string ext = Path.GetExtension(d.FileName).ToLower();
                string basePath = d.FileName.Substring(0, d.FileName.Length - ext.Length);
                if (ext == ".ctd" && File.Exists(basePath + ".txt"))
                    ModdedFileTxt.Text = basePath + ".txt";
                else if ((ext == ".cbg" || ext == ".mzp") && File.Exists(basePath + ".png"))
                    ModdedFileTxt.Text = basePath + ".png";
            }
        }

        private void BrowseMod_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Modded Files (*.txt;*.png)|*.txt;*.png|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) 
            {
                ModdedFileTxt.Text = d.FileName;
                
                // Smart Auto-fill original file
                string ext = Path.GetExtension(d.FileName).ToLower();
                string basePath = d.FileName.Substring(0, d.FileName.Length - ext.Length);
                if (ext == ".txt" && File.Exists(basePath + ".ctd"))
                    OriginalFileTxt.Text = basePath + ".ctd";
                else if (ext == ".png")
                {
                    if (File.Exists(basePath + ".cbg")) OriginalFileTxt.Text = basePath + ".cbg";
                    else if (File.Exists(basePath + ".mzp")) OriginalFileTxt.Text = basePath + ".mzp";
                }
            }
        }

        private async void ListHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFileTxt.Text) || HfaFileTxt.Text.Contains("Select")) return;
            await Utils.HunexUtils.ListHfaAsync(HfaFileTxt.Text, GetMain());
        }

        private async void UnpackHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFileTxt.Text) || HfaFileTxt.Text.Contains("Select")) return;
            await Utils.HunexUtils.UnpackHfaAsync(HfaFileTxt.Text, HfaFolderTxt.Text, GetMain());
        }

        private async void RepackHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFolderTxt.Text) || HfaFolderTxt.Text.Contains("Select")) return;
            string outHfa = Path.Combine(Path.GetDirectoryName(HfaFolderTxt.Text) ?? "", Path.GetFileName(HfaFolderTxt.Text) + "_new.hfa");
            await Utils.HunexUtils.RepackHfaAsync(HfaFolderTxt.Text, outHfa, GetMain());
        }

        private async void Decode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalFileTxt.Text) || OriginalFileTxt.Text.Contains("Select")) return;
            string ext = Path.GetExtension(OriginalFileTxt.Text).ToLower();
            
            string basePath = OriginalFileTxt.Text.Substring(0, OriginalFileTxt.Text.Length - ext.Length);
            
            if (ext == ".ctd") await Utils.HunexUtils.DecompressCtdAsync(OriginalFileTxt.Text, basePath + "_new.txt", GetMain());
            else if (ext == ".cbg") await Utils.HunexUtils.DecodeCbgAsync(OriginalFileTxt.Text, basePath + "_new.png", GetMain());
            else if (ext == ".mzp") await Utils.HunexUtils.DecodeMzpAsync(OriginalFileTxt.Text, basePath + "_new.png", GetMain());
        }

        private async void Encode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalFileTxt.Text) || OriginalFileTxt.Text.Contains("Select")) return;
            if (string.IsNullOrWhiteSpace(ModdedFileTxt.Text) || ModdedFileTxt.Text.Contains("Select")) return;

            string ext = Path.GetExtension(OriginalFileTxt.Text).ToLower();
            
            string basePath = OriginalFileTxt.Text.Substring(0, OriginalFileTxt.Text.Length - ext.Length);

            if (ext == ".ctd") await Utils.HunexUtils.CompressCtdAsync(ModdedFileTxt.Text, basePath + "_new.ctd", GetMain());
            else if (ext == ".cbg") await Utils.HunexUtils.EncodeCbgAsync(ModdedFileTxt.Text, basePath + "_new.cbg", GetMain());
            else if (ext == ".mzp") await Utils.HunexUtils.EncodeMzpAsync(ModdedFileTxt.Text, OriginalFileTxt.Text, basePath + "_new.mzp", GetMain());
        }
    }
}
