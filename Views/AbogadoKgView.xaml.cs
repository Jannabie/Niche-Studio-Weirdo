using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class AbogadoKgView : UserControl
    {
        private bool _isBusy = false;

        public AbogadoKgView() 
        { 
            InitializeComponent();
        }

        private void BrowseDecodeFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) DecodeFolderTxt.Text = d.FolderName;
        }

        private void BrowseEncodeFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) EncodeFolderTxt.Text = d.FolderName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private bool TrySetBusy(bool busy)
        {
            if (busy && _isBusy) return false;
            _isBusy = busy;
            BtnDecodeKg.IsEnabled = !busy;
            BtnEncodePng.IsEnabled = !busy;
            return true;
        }

        // 笏笏笏 Decode: KG 竊・PNG 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏
        private async void DecodeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running 窶・please wait."); return; }

            if (string.IsNullOrWhiteSpace(DecodeFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select a folder containing .KG files first.");
                TrySetBusy(false);
                return;
            }

            string folder = DecodeFolderTxt.Text;
            GetMain().LogToConsole($"[*] Decoding all .KG 竊・PNG in: {folder}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoKgEncoder.DecodeFolderKgToPng(folder, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }

        // 笏笏笏 Encode: PNG 竊・KG 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏
        private async void ConvertPngKg_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running 窶・please wait."); return; }

            if (string.IsNullOrWhiteSpace(EncodeFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select a folder containing .PNG files to convert.");
                TrySetBusy(false);
                return;
            }

            string folder = EncodeFolderTxt.Text;
            GetMain().LogToConsole($"[*] Encoding all .PNG 竊・KG in: {folder}");
            GetMain().LogToConsole($"[*] Output: {System.IO.Path.Combine(folder, "packed_kg")}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoKgEncoder.ConvertFolderPngToKg(folder, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }
    }
}

