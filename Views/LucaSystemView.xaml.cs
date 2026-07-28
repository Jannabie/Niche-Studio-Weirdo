using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class LucaSystemView : UserControl
    {
        // LuckSystem.exe lives here relative to the app's BaseDirectory
        private static readonly string ExePath = Path.Combine("Utility", "LuckSystemBin", "LuckSystem.exe");

        // Per-game plugin and opcode filenames (relative to LuckSystemBin\data\)
        private static readonly string[] GamePlugins = new[]
        {
            "LOOPERS.py",   // 0 LOOPERS
            "LB_EN.py",     // 1 Little Busters EN  — no dedicated .py, use LOOPERS base
            "SP.py",        // 2 Summer Pockets
            "CartagraHD.py",// 3 CartagraHD (JP)
            "CartagraENG.py",// 4 CartagraHD (EN)
            "KANON.py",     // 5 KANON
            "HARMONIA.py",  // 6 HARMONIA
            "LUNARiA.py",   // 7 LUNARiA
            "AIR.py",       // 8 AIR
            "PlanetarianSG.py" // 9 Planetarian SG
        };

        private static readonly string[] GameOpcodes = new[]
        {
            "LOOPERS.txt",
            "LB_EN.txt",
            "SP.txt",
            "CartagraHD.txt",
            "CartagraENG.txt",
            "KANON.txt",
            "HARMONIA.txt",
            "LUNARiA.txt",
            "AIR.txt",
            "PlanetarianSG.txt"
        };

        public LucaSystemView()
        {
            InitializeComponent();
        }

        // 笏笏 Helpers 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

        private string GetDataDir() =>
            Path.Combine((Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory), "Utility", "LuckSystemBin", "data");

        private (string plugin, string opcode) GetGameFiles()
        {
            int idx = GameProfileCombo.SelectedIndex;
            if (idx < 0 || idx >= GamePlugins.Length) idx = 0;
            string dataDir = GetDataDir();
            return (
                Path.Combine(dataDir, GamePlugins[idx]),
                Path.Combine(dataDir, GameOpcodes[idx])
            );
        }

        // 笏笏 SCRIPT DECOMPILE 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

        private void BrowseScriptDecompileInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PAK Archive (*.PAK;*.pak)|*.PAK;*.pak|All Files (*.*)|*.*", Title = "Select SCRIPT.PAK" };
            if (d.ShowDialog() == true) ScriptDecompileInputTxt.Text = d.FileName;
        }

        private async void ScriptDecompile_Click(object sender, RoutedEventArgs e)
        {
            string pakPath = ScriptDecompileInputTxt.Text.Trim();

            if (string.IsNullOrWhiteSpace(pakPath) || !File.Exists(pakPath))
            {
                MessageBox.Show("Please select a valid SCRIPT.PAK file.");
                return;
            }
            
            string outDir = Path.Combine(Path.GetDirectoryName(pakPath) ?? "", "Script_Decompiled");
            Directory.CreateDirectory(outDir);
            
            int idx = GameProfileCombo.SelectedIndex;
            if (idx < 0 || idx >= GamePlugins.Length) idx = 0;
            string pluginName = GamePlugins[idx];
            string opcodeName = GameOpcodes[idx];
            string dataDir = GetDataDir();

            // Run in the data directory so gpython can resolve "from base.xxx import *"
            string args = $"script decompile -s \"{pakPath}\" -O \"{opcodeName}\" -p \"{pluginName}\" -o \"{outDir}\"";

            string exeAbsPath = Path.Combine((Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory), ExePath);
            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync(dataDir, exeAbsPath, args, main);
        }

        // 笏笏 SCRIPT IMPORT 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

        private void BrowseScriptImportSource_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PAK Archive (*.PAK;*.pak)|*.PAK;*.pak|All Files (*.*)|*.*", Title = "Select ORIGINAL SCRIPT.PAK" };
            if (d.ShowDialog() == true) ScriptImportSourceTxt.Text = d.FileName;
        }

        private void BrowseScriptImportInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog { Title = "Select folder with edited text files..." };
            if (d.ShowDialog() == true) ScriptImportInputTxt.Text = d.FolderName;
        }

        private async void ScriptImport_Click(object sender, RoutedEventArgs e)
        {
            string srcPak  = ScriptImportSourceTxt.Text.Trim();
            string inDir   = ScriptImportInputTxt.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(srcPak) || !File.Exists(srcPak))
            {
                MessageBox.Show("Please select the original SCRIPT.PAK file.");
                return;
            }
            if (string.IsNullOrWhiteSpace(inDir) || !Directory.Exists(inDir))
            {
                MessageBox.Show("Please select a valid folder containing the modified text files.");
                return;
            }
            
            string outPak = Path.Combine(Path.GetDirectoryName(srcPak) ?? "", "SCRIPT_NEW.PAK");

            int idx = GameProfileCombo.SelectedIndex;
            if (idx < 0 || idx >= GamePlugins.Length) idx = 0;
            string pluginName = GamePlugins[idx];
            string opcodeName = GameOpcodes[idx];
            string dataDir = GetDataDir();

            // script import -s <original SCRIPT.PAK> -O <opcode> -p <plugin> -i <inDir> -o <outPak>
            string args = $"script import -s \"{srcPak}\" -O \"{opcodeName}\" -p \"{pluginName}\" -i \"{inDir}\" -o \"{outPak}\"";

            string exeAbsPath = Path.Combine((Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory), ExePath);
            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync(dataDir, exeAbsPath, args, main);
        }

        // 笏笏 IMAGE EXPORT (CZ 竊・PNG) 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

        private void BrowseCzExportInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog
            {
                Filter = "CZ Image (*.cz0;*.cz1;*.cz2;*.cz3;*.cz4)|*.cz0;*.cz1;*.cz2;*.cz3;*.cz4|All Files (*.*)|*.*",
                Title = "Select .CZ image file"
            };
            if (d.ShowDialog() == true) CzExportInputTxt.Text = d.FileName;
        }

        private async void CzExport_Click(object sender, RoutedEventArgs e)
        {
            string czPath  = CzExportInputTxt.Text.Trim();

            if (string.IsNullOrWhiteSpace(czPath) || !File.Exists(czPath))
            {
                MessageBox.Show("Please select a valid .CZ image file.");
                return;
            }

            string pngPath = Path.Combine(Path.GetDirectoryName(czPath) ?? "", Path.GetFileNameWithoutExtension(czPath) + ".png");

            // image export -i <cz> -o <png>
            string args = $"image export -i \"{czPath}\" -o \"{pngPath}\"";

            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync((Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory), ExePath, args, main);
        }

        // 笏笏 IMAGE IMPORT (PNG 竊・CZ) 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

        private void BrowseCzImportSource_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog
            {
                Filter = "CZ Image (*.cz0;*.cz1;*.cz2;*.cz3;*.cz4)|*.cz0;*.cz1;*.cz2;*.cz3;*.cz4|All Files (*.*)|*.*",
                Title = "Select ORIGINAL .CZ file (for format reference)"
            };
            if (d.ShowDialog() == true) CzImportSourceTxt.Text = d.FileName;
        }

        private void BrowseCzImportInput_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "PNG Image (*.png)|*.png|All Files (*.*)|*.*", Title = "Select your edited PNG file" };
            if (d.ShowDialog() == true) CzImportInputTxt.Text = d.FileName;
        }

        private async void CzImport_Click(object sender, RoutedEventArgs e)
        {
            string srcCz   = CzImportSourceTxt.Text.Trim();
            string pngPath = CzImportInputTxt.Text.Trim();

            if (string.IsNullOrWhiteSpace(srcCz) || !File.Exists(srcCz))
            {
                MessageBox.Show("Please select the original .CZ file.");
                return;
            }
            if (string.IsNullOrWhiteSpace(pngPath) || !File.Exists(pngPath))
            {
                MessageBox.Show("Please select a valid PNG file to import.");
                return;
            }
            string ext = Path.GetExtension(srcCz); // preserve original CZ type
            string outCz = Path.Combine(Path.GetDirectoryName(pngPath) ?? "",
                Path.GetFileNameWithoutExtension(pngPath) + "_imported" + ext);

            // image import -s <original.cz> -i <in.png> -o <out.cz>
            string args = $"image import -s \"{srcCz}\" -i \"{pngPath}\" -o \"{outCz}\"";

            var main = Window.GetWindow(this) as MainWindow;
            await ToolRunner.RunAsync((Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory), ExePath, args, main);
        }
    }
}

