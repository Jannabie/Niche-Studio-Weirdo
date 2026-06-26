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
            if (d.ShowDialog() == true) OriginalFileTxt.Text = d.FileName;
        }

        private void BrowseMod_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Modded Files (*.txt;*.png)|*.txt;*.png|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ModdedFileTxt.Text = d.FileName;
        }

        private async void UnpackHfa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HfaFileTxt.Text) || HfaFileTxt.Text.Contains("Select")) return;
            await Utils.HunexUtils.UnpackHfaAsync(HfaFileTxt.Text, GetMain());
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

            if (ext == ".ctd") await Utils.HunexUtils.DecompressCtdAsync(OriginalFileTxt.Text, GetMain());
            else if (ext == ".cbg") await Utils.HunexUtils.DecodeCbgAsync(OriginalFileTxt.Text, GetMain());
            else if (ext == ".mzp") await Utils.HunexUtils.DecodeMzpAsync(OriginalFileTxt.Text, GetMain());
        }

        private async void Encode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalFileTxt.Text) || OriginalFileTxt.Text.Contains("Select")) return;
            if (string.IsNullOrWhiteSpace(ModdedFileTxt.Text) || ModdedFileTxt.Text.Contains("Select")) return;

            string ext = Path.GetExtension(OriginalFileTxt.Text).ToLower();

            if (ext == ".ctd") await Utils.HunexUtils.CompressCtdAsync(ModdedFileTxt.Text, OriginalFileTxt.Text, GetMain());
            else if (ext == ".cbg") await Utils.HunexUtils.EncodeCbgAsync(ModdedFileTxt.Text, OriginalFileTxt.Text, GetMain());
            else if (ext == ".mzp") await Utils.HunexUtils.EncodeMzpAsync(ModdedFileTxt.Text, OriginalFileTxt.Text, GetMain());
        }
    }
}
