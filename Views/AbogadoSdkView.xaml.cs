using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;


namespace NicheStudioWeirdo.Views
{
    public partial class AbogadoSdkView : UserControl
    {
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

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private async void UnpackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text) || string.IsNullOrWhiteSpace(SdkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih .PFT, .DSK, dan Folder Target untuk Unpack.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"sdk_tools.py unpack \"{SdkDskTxt.Text}\" \"{SdkPftTxt.Text}\" \"{SdkFolderTxt.Text}\"", GetMain());
        }

        private async void RepackDsk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text) || string.IsNullOrWhiteSpace(SdkFolderTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih .PFT, .DSK, dan Folder Source untuk Repack.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"sdk_tools.py repack \"{SdkDskTxt.Text}\" \"{SdkPftTxt.Text}\" \"{SdkFolderTxt.Text}\"", GetMain());
        }

        private async void VerifyIntegrity_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SdkPftTxt.Text) || string.IsNullOrWhiteSpace(SdkDskTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih file .PFT dan .DSK untuk Verify Integrity.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"sdk_verify.py verify \"{SdkPftTxt.Text}\" \"{SdkDskTxt.Text}\"", GetMain());
        }

        private async void ParseScf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScfFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"parser_scf.py \"{ScfFileTxt.Text}\"", GetMain());
        }

        private async void InjectTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TranslationJsonTxt.Text) || string.IsNullOrWhiteSpace(ScfFileTxt.Text))
            {
                GetMain().LogToConsole("[ERROR] Pilih .SCF file dan .JSON terjemahan terlebih dahulu.");
                return;
            }
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"injector_scf.py \"{TranslationJsonTxt.Text}\" \"{ScfFileTxt.Text}\"", GetMain());
        }
    }
}
