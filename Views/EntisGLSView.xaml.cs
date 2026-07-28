using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NicheStudioWeirdo.Views
{
    public partial class EntisGLSView : UserControl
    {
        private MainWindow Main => (MainWindow)Window.GetWindow(this);

        public EntisGLSView()
        {
            InitializeComponent();
        }

        // --- Browse Buttons ---
        private void BrowseInputNoa_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "NOA Archive (*.noa)|*.noa|All files (*.*)|*.*" };
            if (dlg.ShowDialog() == true) InputNoaFile.Text = dlg.FileName;
        }

        private void BrowseSrcxmlFolder_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new OpenFolderDialog { Title = "Select folder containing .srcxml files" };
            if (fbd.ShowDialog() == true) SrcxmlFolder.Text = fbd.FolderName;
        }

        private void BrowseTxtFolder_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new OpenFolderDialog { Title = "Select folder for TXT translations" };
            if (fbd.ShowDialog() == true) TxtFolder.Text = fbd.FolderName;
        }

        private void BrowsePackSource_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new OpenFolderDialog { Title = "Select folder to pack into NOA" };
            if (fbd.ShowDialog() == true) PackSourceFolder.Text = fbd.FolderName;
        }

        // --- Action Buttons ---
        private async void UnpackNoa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputNoaFile.Text))
            {
                Main.LogToConsole("✘ Please specify Input NOA file.");
                return;
            }

            // Ask user where to unpack
            var fbd = new OpenFolderDialog { Title = "Select output folder to unpack NOA into" };
            if (fbd.ShowDialog() != true) return;
            string outputFolder = fbd.FolderName;

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "EntisGLS", "arc_unpacker.exe");
            if (!File.Exists(exePath))
            {
                Main.LogToConsole($"✘ arc_unpacker.exe not found at: {exePath}");
                return;
            }

            // arc_unpacker.exe --dec=entis/noa --out="outputFolder" "InputNoaFile"
            string[] args = { "--dec=entis/noa", $"--out={outputFolder}", InputNoaFile.Text };
            await ToolRunner.RunAsync(Path.GetDirectoryName(exePath), exePath, args, Main);
        }

        private async void PackNoa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PackSourceFolder.Text))
            {
                Main.LogToConsole("✘ Please specify Source Folder.");
                return;
            }

            // Ask user where to save the output .noa
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save output NOA archive as...",
                Filter = "NOA Archive (*.noa)|*.noa",
                FileName = "output.noa"
            };
            if (dlg.ShowDialog() != true) return;
            string outNoa = dlg.FileName;

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "EntisGLS", "noa32c.exe");
            if (!File.Exists(exePath))
            {
                Main.LogToConsole($"✘ noa32c.exe not found at: {exePath}");
                return;
            }

            // noa32c.exe /p /erisa * "outNoa" (running inside the source folder)
            string[] args = { "/p", "/erisa", "*", outNoa };
            await ToolRunner.RunAsync(PackSourceFolder.Text, exePath, args, Main);
        }

        // --- Parser / Injector Logic (Pure C#) ---
        private async void ParseSrcxml_Click(object sender, RoutedEventArgs e)
        {
            string srcDir = SrcxmlFolder.Text;
            string outDir = TxtFolder.Text;

            if (string.IsNullOrWhiteSpace(srcDir) || string.IsNullOrWhiteSpace(outDir))
            {
                Main.LogToConsole("✘ Please specify SRCXML folder and TXT folder.");
                return;
            }

            void Log(string msg) => Dispatcher.Invoke(() => Main.LogToConsole(msg));

            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                    var files = Directory.GetFiles(srcDir, "*.srcxml", SearchOption.AllDirectories);
                    Log($"▶ Parsing {files.Length} .srcxml files...");

                    int totalMessages = 0;
                    Regex msgRegex = new Regex(@"<msg\s(.*?)/>", RegexOptions.Singleline);
                    Regex attrRegex = new Regex(@"(\w+|@l)=""([^""]*)""");

                    foreach (var file in files)
                    {
                        string content = File.ReadAllText(file, System.Text.Encoding.UTF8);
                        var entries = msgRegex.Matches(content);
                        if (entries.Count == 0) continue;

                        string baseName = Path.GetFileNameWithoutExtension(file);
                        string outPath = Path.Combine(outDir, baseName + ".txt");
                        using StreamWriter sw = new StreamWriter(outPath, false, new System.Text.UTF8Encoding(true));

                        sw.WriteLine($"# Source: {Path.GetFileName(file)}");
                        sw.WriteLine($"# Total messages: {entries.Count}");
                        sw.WriteLine();

                        foreach (Match m in entries)
                        {
                            var attrs = attrRegex.Matches(m.Groups[1].Value)
                                                 .Cast<Match>()
                                                 .ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);

                            string idx = attrs.ContainsKey("index") ? attrs["index"] : "";
                            string name = attrs.ContainsKey("name") ? attrs["name"] : "";
                            if (string.IsNullOrEmpty(name)) name = "（地の文）";
                            string text = attrs.ContainsKey("text") ? attrs["text"] : "";
                            text = text.Replace("\n", "<NL>").Replace("\r", "");

                            string key = $"{baseName}:{idx}";
                            sw.WriteLine($"◇{key}◇{name}│{text}");
                            sw.WriteLine($"◆{key}◆{name}│");
                            sw.WriteLine();
                            totalMessages++;
                        }
                    }
                    Log($"✔ Done! Extracted {totalMessages} messages from {files.Length} files.");
                }
                catch (Exception ex)
                {
                    Log($"✘ Error during parse: {ex.Message}");
                }
            });
        }

        private async void InjectSrcxml_Click(object sender, RoutedEventArgs e)
        {
            string srcDir = SrcxmlFolder.Text;
            string txtDir = TxtFolder.Text;

            if (string.IsNullOrWhiteSpace(srcDir) || string.IsNullOrWhiteSpace(txtDir))
            {
                Main.LogToConsole("✘ Please specify SRCXML folder and TXT folder.");
                return;
            }

            void Log(string msg) => Dispatcher.Invoke(() => Main.LogToConsole(msg));

            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(srcDir, "*.srcxml", SearchOption.AllDirectories);
                    Log($"▶ Injecting into {files.Length} .srcxml files...");

                    int totalInjected = 0;
                    Regex msgRegex = new Regex(@"<msg\s(.*?)/>", RegexOptions.Singleline);
                    Regex idxRegex = new Regex(@"index=""([^""]*)""");
                    Regex txtAttrRegex = new Regex(@"text=""[^""]*""");
                    Regex nameAttrRegex = new Regex(@"name=""[^""]*""");
                    Regex transLineRegex = new Regex(@"^◆([^◆]+)◆([^│]*)│(.*)");

                    foreach (var file in files)
                    {
                        string baseName = Path.GetFileNameWithoutExtension(file);
                        string txtPath = Path.Combine(txtDir, baseName + ".txt");
                        if (!File.Exists(txtPath)) continue;

                        // Load translations: key → (translatedName, translatedText)
                        var translations = new System.Collections.Generic.Dictionary<string, (string name, string text)>();
                        foreach (string line in File.ReadAllLines(txtPath, System.Text.Encoding.UTF8))
                        {
                            if (!line.StartsWith("◆")) continue;
                            var tm = transLineRegex.Match(line);
                            if (tm.Success)
                            {
                                string key = tm.Groups[1].Value;
                                string translatedName = tm.Groups[2].Value;
                                string trans = tm.Groups[3].Value;
                                if (!string.IsNullOrWhiteSpace(trans))
                                {
                                    translations[key] = (translatedName, trans.Replace("<NL>", "\n"));
                                }
                            }
                        }

                        if (translations.Count == 0) continue;

                        string content = File.ReadAllText(file, System.Text.Encoding.UTF8);
                        string newContent = msgRegex.Replace(content, m =>
                        {
                            string attrsStr = m.Groups[1].Value;
                            var idxMatch = idxRegex.Match(attrsStr);
                            if (!idxMatch.Success) return m.Value;

                            string key = $"{baseName}:{idxMatch.Groups[1].Value}";
                            if (translations.ContainsKey(key))
                            {
                                var (tName, tText) = translations[key];

                                // Replace text attribute
                                string escapedText = tText.Replace("&", "&amp;")
                                                          .Replace("\"", "&quot;")
                                                          .Replace("<", "&lt;")
                                                          .Replace(">", "&gt;");
                                string newAttrs = txtAttrRegex.Replace(attrsStr, $"text=\"{escapedText}\"");

                                // Also replace name attribute if translator filled it in
                                if (!string.IsNullOrWhiteSpace(tName))
                                {
                                    string escapedName = tName.Replace("&", "&amp;")
                                                              .Replace("\"", "&quot;")
                                                              .Replace("<", "&lt;")
                                                              .Replace(">", "&gt;");
                                    newAttrs = nameAttrRegex.Replace(newAttrs, $"name=\"{escapedName}\"");
                                }

                                totalInjected++;
                                return $"<msg {newAttrs}/>";
                            }
                            return m.Value;
                        });

                        File.WriteAllText(file, newContent, new System.Text.UTF8Encoding(false));
                    }
                    Log($"✔ Done! Injected {totalInjected} translated messages.");
                }
                catch (Exception ex)
                {
                    Log($"✘ Error during inject: {ex.Message}");
                }
            });
        }
    }
}
