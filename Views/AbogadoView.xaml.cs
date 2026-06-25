using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class AbogadoView : UserControl
    {
        public AbogadoView() 
        { 
            InitializeComponent();
            // Default: SCF tab active
            SetActiveTab("SCF");
        }

        private void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            SetActiveTab(btn?.Tag?.ToString() ?? "SCF");
        }

        private void SetActiveTab(string tag)
        {
            var dark = (System.Windows.Media.SolidColorBrush)FindResource("BgDarkestBrush");
            var light = (System.Windows.Media.SolidColorBrush)FindResource("BgLighterBrush");
            var textLight = (System.Windows.Media.SolidColorBrush)FindResource("TextLightBrush");
            var textMuted = (System.Windows.Media.SolidColorBrush)FindResource("TextMutedBrush");

            bool scfActive = tag == "SCF";
            PanelScf.Visibility = scfActive ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            PanelGfx.Visibility = scfActive ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            TabScf.Background = scfActive ? light : dark;
            TabGfx.Background = scfActive ? dark : light;
            TabScf.Foreground = scfActive ? textLight : textMuted;
            TabGfx.Foreground = scfActive ? textMuted : textLight;
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

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private async void ParseScf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScfFileTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"parser_scf.py \"{ScfFileTxt.Text}\"", GetMain());
        }

        private async void InjectTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScfFileTxt.Text) || string.IsNullOrWhiteSpace(TranslationJsonTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"injector_scf.py \"{TranslationJsonTxt.Text}\" \"{ScfFileTxt.Text}\"", GetMain());
        }

        private async void VerifyIntegrity_Click(object sender, RoutedEventArgs e)
        {
            string repoDir = System.IO.Path.Combine(SettingsManager.Config.ReposPath, "Abogado-Arch-SDK");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(repoDir, py, $"sdk_verify.py", GetMain());
        }

        private void ConvertPngKg_Click(object sender, RoutedEventArgs e)  => GetMain().LogToConsole($"Abogado: Bulk converting PNG ↁEKG in {PngFolderTxt.Text} (Stubbed)");
        private void PatchDsk_Click(object sender, RoutedEventArgs e)      => GetMain().LogToConsole($"Abogado: Patching DSK in-place: {DskFileTxt.Text} (Stubbed)");
        private void RebuildDsk_Click(object sender, RoutedEventArgs e)    => GetMain().LogToConsole($"Abogado: Full rebuild of DSK: {DskFileTxt.Text} (Stubbed)");
    }
}
