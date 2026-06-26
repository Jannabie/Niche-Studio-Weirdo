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

        // ─── Decode: KG ↁEPNG ───────────────────────────────────────────────
        private async void DecodeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(DecodeFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select a folder containing .KG files first.");
                TrySetBusy(false);
                return;
            }

            string folder = DecodeFolderTxt.Text;
            GetMain().LogToConsole($"[*] Decoding all .KG ↁEPNG in: {folder}");
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

        // ─── Encode: PNG ↁEKG ───────────────────────────────────────────────
        private async void ConvertPngKg_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(EncodeFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select a folder containing .PNG files to convert.");
                TrySetBusy(false);
                return;
            }

            string folder = EncodeFolderTxt.Text;
            GetMain().LogToConsole($"[*] Encoding all .PNG ↁEKG in: {folder}");
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
