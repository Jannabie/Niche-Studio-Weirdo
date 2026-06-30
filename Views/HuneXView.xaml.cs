using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class HuneXView : UserControl
    {
        public HuneXView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);
        private string PythonPath => SettingsManager.Config.PythonPath;

        private string GetToolPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "Utility", "Hunex Tsukire", "mrg_tool.py");
        }

        private string GetToolDir()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "Utility", "Hunex Tsukire");
        }

        // ── Extract: MRG → TXT ───────────────────────────────────────────────────
        private void BrowseMrgInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "MRG Files (*.mrg)|*.mrg|All Files (*.*)|*.*", Title = "Select script_text.mrg" };
            if (d.ShowDialog() == true) MrgInputTxt.Text = d.FileName;
        }

        private void BrowseExtractOutput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select extract output directory..." };
            if (d.ShowDialog() == true) ExtractOutputTxt.Text = d.FolderName;
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            string mrgPath = MrgInputTxt.Text.Trim();
            string outTxt = ExtractOutputTxt.Text.Trim();

            if (string.IsNullOrEmpty(mrgPath) || string.IsNullOrEmpty(outTxt))
            {
                GetMain().LogToConsole("HuneX: Please fill in all fields before extracting.");
                return;
            }

            string toolDir = GetToolDir();
            string script = GetToolPath();

            if (!File.Exists(script))
            {
                GetMain().LogToConsole($"HuneX Error: Tool not found at {script}. Please ensure mrg_tool.py exists in the Utility\\Hunex Tsukire folder.");
                return;
            }

            await ToolRunner.RunAsync(toolDir, PythonPath, $"\"{script}\" extract \"{mrgPath}\" \"{outTxt}\"", GetMain());
        }

        // ── Repack: TXT → MRG ───────────────────────────────────────────────────
        private void BrowseRepackInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select input directory containing edited .txt files..." };
            if (d.ShowDialog() == true) RepackInputTxt.Text = d.FolderName;
        }

        private void BrowseRepackOutput_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog { Filter = "MRG Files (*.mrg)|*.mrg", DefaultExt = "mrg", FileName = "script_text_new.mrg", Title = "Save repacked .mrg as..." };
            if (d.ShowDialog() == true) RepackOutputTxt.Text = d.FileName;
        }

        private async void Repack_Click(object sender, RoutedEventArgs e)
        {
            string inTxt = RepackInputTxt.Text.Trim();
            string outMrg = RepackOutputTxt.Text.Trim();

            if (string.IsNullOrEmpty(inTxt) || string.IsNullOrEmpty(outMrg))
            {
                GetMain().LogToConsole("HuneX: Please fill in all fields before repacking.");
                return;
            }

            string toolDir = GetToolDir();
            string script = GetToolPath();

            if (!File.Exists(script))
            {
                GetMain().LogToConsole($"HuneX Error: Tool not found at {script}. Please ensure mrg_tool.py exists in the Utility\\Hunex Tsukire folder.");
                return;
            }

            await ToolRunner.RunAsync(toolDir, PythonPath, $"\"{script}\" repack \"{inTxt}\" \"{outMrg}\"", GetMain());
        }
    }
}
