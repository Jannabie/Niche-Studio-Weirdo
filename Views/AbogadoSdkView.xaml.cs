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
    }
}
