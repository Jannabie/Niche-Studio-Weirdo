using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class HuneXView : UserControl
    {
        public HuneXView() { InitializeComponent(); SetActiveTab("MRG"); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            SetActiveTab(btn?.Tag?.ToString() ?? "MRG");
        }
        private void SetActiveTab(string tag)
        {
            var dark = (System.Windows.Media.SolidColorBrush)FindResource("BgDarkestBrush");
            var light = (System.Windows.Media.SolidColorBrush)FindResource("BgLighterBrush");
            var textLight = (System.Windows.Media.SolidColorBrush)FindResource("TextLightBrush");
            var textMuted = (System.Windows.Media.SolidColorBrush)FindResource("TextMutedBrush");
            bool mrg = tag == "MRG";
            PanelMrg.Visibility = mrg ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            PanelEditor.Visibility = mrg ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            TabMrg.Background = mrg ? light : dark; TabMrg.Foreground = mrg ? textLight : textMuted;
            TabEditor.Background = mrg ? dark : light; TabEditor.Foreground = mrg ? textMuted : textLight;
        }

        private void BrowseAllscr_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "MRG Files (*.mrg)|*.mrg|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) AllscrTxt.Text = d.FileName;
        }
        private void BrowseScriptText_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "MRG Files (*.mrg)|*.mrg|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) ScriptTextTxt.Text = d.FileName;
        }
        private void BrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (d.ShowDialog() == true) DbJsonTxt.Text = d.FileName;
        }

        private async void BuildDb_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AllscrTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "TsukiRe-mrg-txt");
            string py = SettingsManager.Config.PythonPath;
            string outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AllscrTxt.Text) ?? "", "extracted_mrg");
            await ToolRunner.RunAsync(repoDir, py, $"mrg_tool.py extract \"{AllscrTxt.Text}\" \"{outDir}\"", GetMain());
        }

        private async void PatchMrg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptTextTxt.Text) || string.IsNullOrWhiteSpace(AllscrTxt.Text)) return;
            string repoDir = System.IO.Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "TsukiRe-mrg-txt");
            string py = SettingsManager.Config.PythonPath;
            string inDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AllscrTxt.Text) ?? "", "extracted_mrg");
            await ToolRunner.RunAsync(repoDir, py, $"mrg_tool.py repack \"{inDir}\" \"{ScriptTextTxt.Text}\"", GetMain());
        }

        private void InsertTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) GetMain().LogToConsole($"HuneX: Tag '{btn.Tag}' ready to insert into active line.");
        }
        private void RunLinter_Click(object sender, RoutedEventArgs e) => GetMain().LogToConsole("HuneX: Linter running  Echecking for ASCII in ruby fields (Stubbed)...");
    }
}
