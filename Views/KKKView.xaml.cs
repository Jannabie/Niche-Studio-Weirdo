using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class KKKView : UserControl
    {
        public KKKView()
        {
            InitializeComponent();
        }

        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseGame_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) GameDirTxt.Text = d.FolderName;
        }

        private async void InstallPatch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GameDirTxt.Text) || GameDirTxt.Text.Contains("Select")) return;
            string repoDir = Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "KKK");
            string gameDir = GameDirTxt.Text;
            var main = GetMain();

            await Task.Run(() =>
            {
                try
                {
                    // Copy malie.ini
                    string iniSrc = Path.Combine(repoDir, "[KT] KKK", "malie.ini");
                    if (File.Exists(iniSrc)) File.Copy(iniSrc, Path.Combine(gameDir, "malie.ini"), true);

                    // Copy data6.dat if exists
                    string datSrc = Path.Combine(repoDir, "dependencies", "data6.dat");
                    if (File.Exists(datSrc)) File.Copy(datSrc, Path.Combine(gameDir, "data6.dat"), true);

                    // Copy messageframe SVG
                    string svgTargetDir = Path.Combine(gameDir, "data", "screen", "messageframe");
                    Directory.CreateDirectory(svgTargetDir);

                    string svgSrcDir;
                    bool isHoriz = false;
                    Application.Current.Dispatcher.Invoke(() => isHoriz = RadioHoriz.IsChecked == true);

                    if (isHoriz)
                        svgSrcDir = Path.Combine(repoDir, "Horizontal");
                    else
                        svgSrcDir = Path.Combine(repoDir, "data", "screen", "messageframe");

                    if (Directory.Exists(svgSrcDir))
                    {
                        foreach (var file in Directory.GetFiles(svgSrcDir, "*.svg", SearchOption.AllDirectories))
                        {
                            string dest = Path.Combine(svgTargetDir, Path.GetFileName(file));
                            File.Copy(file, dest, true);
                        }
                    }

                    main.Dispatcher.Invoke(() => main.LogToConsole("KKK: Base patch installed successfully to " + gameDir));
                }
                catch (System.Exception ex)
                {
                    main.Dispatcher.Invoke(() => main.LogToConsole("Error installing patch: " + ex.Message));
                }
            });
        }

        private async void Wordwrap_Click(object sender, RoutedEventArgs e)
        {
            string repoDir = Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "KKK");
            string depsDir = Path.Combine(repoDir, "dependencies");
            string py = SettingsManager.Config.PythonPath;
            await ToolRunner.RunAsync(depsDir, py, "wordwrap.py", GetMain());
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            string repoDir = Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "KKK");
            string exeDir = Path.Combine(repoDir, "dependencies", "malie tools", "compilar", "Malie_Script_Tool-main", "bin", "Debug");
            
            // Auto create .data\system to prevent DirectoryNotFoundException
            Directory.CreateDirectory(Path.Combine(exeDir, ".data", "system"));

            string exe = Path.Combine(exeDir, "Malie_Script_Tool.exe");
            await ToolRunner.RunAsync(exeDir, exe, "", GetMain());
        }

        private async void Pack_Click(object sender, RoutedEventArgs e)
        {
            string repoDir = Path.Combine(Utils.ExternalToolsResolver.GetToolPath(""), "KKK");
            string depsDir = Path.Combine(repoDir, "dependencies");
            string py = SettingsManager.Config.PythonPath;
            string dataDir = Path.Combine(repoDir, "data");

            await ToolRunner.RunAsync(depsDir, py, $"dat_pack.py \"{dataDir}\"", GetMain());
        }
    }
}
