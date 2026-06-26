using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class AbogadoSdkView : UserControl
    {
        private bool _isBusy = false;

        public AbogadoSdkView() 
        { 
            InitializeComponent();
        }

        private void BrowseSdkPft_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PFT Files (*.pft)|*.pft|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) SdkPftTxt.Text = d.FileName;
        }

        private void BrowseSdkDsk_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "DSK Archives (*.dsk)|*.dsk|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) SdkDskTxt.Text = d.FileName;
        }

        private void BrowseSdkFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) SdkFolderTxt.Text = d.FolderName;
        }

        private void BrowseScf_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "SCF Files (*.scf)|*.scf|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ScfFileTxt.Text = d.FileName;
        }

        private void BrowseJson_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (d.ShowDialog() == true) TranslationJsonTxt.Text = d.FileName;
        }

        private void BrowseTxt_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) TranslationTxtTxt.Text = d.FileName;
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        /// <summary>
        /// Locks all action buttons during a long operation to prevent double-fire.
        /// Returns false if already busy (caller should abort).
        /// </summary>
        private bool TrySetBusy(bool busy)
        {
            if (busy && _isBusy) return false; // already running, reject
            _isBusy = busy;
            BtnUnpack.IsEnabled = !busy;
            BtnRepack.IsEnabled = !busy;
            BtnVerify.IsEnabled = !busy;
            BtnParseScf.IsEnabled = !busy;
            BtnRebuildScf.IsEnabled = !busy;
            return true;
        }

        // ─── UNPACK DSK ──────────────────────────────────────────────────────
        private async void UnpackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text) || string.IsNullOrWhiteSpace(SdkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih .PFT, .DSK, dan Folder Target untuk Unpack.");
                TrySetBusy(false);
                return;
            }

            string pft = SdkPftTxt.Text;
            string dsk = SdkDskTxt.Text;
            string folder = SdkFolderTxt.Text;

            GetMain().LogToConsole($"[*] Unpack DSK: {System.IO.Path.GetFileName(dsk)}");
            GetMain().LogToConsole($"[*] Output: {folder}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoPftDsk.UnpackDsk(dsk, pft, folder, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }

        // ─── REPACK DSK ──────────────────────────────────────────────────────
        private async void RepackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text) || string.IsNullOrWhiteSpace(SdkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih .PFT, .DSK, dan Folder Source untuk Repack.");
                TrySetBusy(false);
                return;
            }

            string pft = SdkPftTxt.Text;
            string dsk = SdkDskTxt.Text;
            string folder = SdkFolderTxt.Text;
            // Output goes NEXT TO source folder  ENOT next to .DSK (may be in Program Files ↁEaccess denied!)
            string outFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(folder) ?? folder, "Repacked");

            GetMain().LogToConsole($"[*] Repack DSK from: {folder}");
            GetMain().LogToConsole($"[*] Output: {outFolder}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoPftDsk.RepackDsk(folder, pft, dsk, outFolder, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }

        // ─── VERIFY ──────────────────────────────────────────────────────────
        private async void VerifyIntegrity_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih file .PFT dan .DSK untuk Verify Integrity.");
                TrySetBusy(false);
                return;
            }

            string pft = SdkPftTxt.Text;
            string dsk = SdkDskTxt.Text;

            GetMain().LogToConsole($"[*] Verifying: {System.IO.Path.GetFileName(dsk)}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoPftDsk.VerifyDsk(pft, dsk, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }

        // ─── PARSE SCF ───────────────────────────────────────────────────────
        private async void ParseScf_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(ScfFileTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select a .SCF file first.");
                TrySetBusy(false);
                return;
            }

            string scf = ScfFileTxt.Text;
            // Output JSON and TXT next to the .SCF file itself
            string outDir = System.IO.Path.GetDirectoryName(scf) ?? System.IO.Path.GetTempPath();

            GetMain().LogToConsole($"[*] Parsing SCF: {System.IO.Path.GetFileName(scf)}");
            GetMain().LogToConsole($"[*] Output: {outDir}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoScfParser.Extract(scf, outDir, msg =>
                        Dispatcher.Invoke(() => GetMain().LogToConsole(msg)));
                }
                catch (System.Exception ex)
                {
                    Dispatcher.Invoke(() => GetMain().LogToConsole($"[FATAL ERROR] {ex.Message}"));
                }
            });
            TrySetBusy(false);
        }

        // ─── REBUILD SCF (inject translation) ────────────────────────────────
        private async void InjectTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetBusy(true)) { GetMain().LogToConsole("[WARN] Already running  Eplease wait."); return; }

            if (string.IsNullOrWhiteSpace(TranslationJsonTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Select the extracted .JSON file first (Required).");
                TrySetBusy(false);
                return;
            }

            string jsonFile = TranslationJsonTxt.Text;
            string txtFile = TranslationTxtTxt.Text;
            // Output _injected.SCF next to the .json file
            string outScf = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(jsonFile) ?? "",
                System.IO.Path.GetFileNameWithoutExtension(jsonFile) + "_injected.SCF");

            GetMain().LogToConsole($"[*] Rebuilding SCF from: {System.IO.Path.GetFileName(jsonFile)}");
            GetMain().LogToConsole($"[*] Output: {outScf}");
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Utils.AbogadoScfParser.Rebuild(jsonFile, txtFile, outScf, msg =>
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
