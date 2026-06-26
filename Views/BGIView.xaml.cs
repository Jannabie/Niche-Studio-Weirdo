using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using NicheStudioWeirdo.Utils;

namespace NicheStudioWeirdo.Views
{
    public partial class BGIView : UserControl
    {
        public BGIView() { InitializeComponent(); }
        private MainWindow GetMain() => (MainWindow)Window.GetWindow(this);

        private void BrowseScript_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) OriginalScriptTxt.Text = d.FileName;
        }

        private void BrowseJson_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog { Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*" };
            if (d.ShowDialog() == true) JsonFileTxt.Text = d.FileName;
        }

        private string GetDllPath()
        {
            return Path.Combine(Utils.UtilityResolver.GetToolPath(""), "Buriko", "EthornellEditor.dll");
        }

        private void ParseScript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalScriptTxt.Text) || string.IsNullOrWhiteSpace(JsonFileTxt.Text))
            {
                GetMain().LogToConsole("BGI [Error]: Select both the original script and the target JSON file.");
                return;
            }

            try
            {
                var wrapper = new BurikoWrapper();
                if (!wrapper.Load(GetDllPath()))
                {
                    GetMain().LogToConsole($"BGI [Error]: Failed to load EthornellEditor.dll - {wrapper.LastError}");
                    return;
                }

                byte[] raw = File.ReadAllBytes(OriginalScriptTxt.Text);
                string[] originalText = wrapper.Import(raw);

                if (originalText == null)
                {
                    GetMain().LogToConsole("BGI [Error]: Script parsed but returned null strings.");
                    return;
                }

                File.WriteAllText(JsonFileTxt.Text, JsonSerializer.Serialize(originalText, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
                GetMain().LogToConsole($"BGI: Successfully parsed {originalText.Length} strings to {Path.GetFileName(JsonFileTxt.Text)}");
            }
            catch (Exception ex)
            {
                GetMain().LogToConsole($"BGI [Error]: {ex.Message}");
            }
        }

        private void InjectScript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OriginalScriptTxt.Text) || string.IsNullOrWhiteSpace(JsonFileTxt.Text))
            {
                GetMain().LogToConsole("BGI [Error]: Select both the original script and the translated JSON file.");
                return;
            }

            try
            {
                var wrapper = new BurikoWrapper();
                if (!wrapper.Load(GetDllPath()))
                {
                    GetMain().LogToConsole($"BGI [Error]: Failed to load EthornellEditor.dll - {wrapper.LastError}");
                    return;
                }

                // 1. We must Import the original script to load the internal state
                byte[] raw = File.ReadAllBytes(OriginalScriptTxt.Text);
                wrapper.Import(raw);

                // 2. Read the translated strings from JSON
                string[] translatedText = JsonSerializer.Deserialize<string[]>(File.ReadAllText(JsonFileTxt.Text));

                // 3. Export to a new script byte array
                byte[] newScript = wrapper.Export(translatedText);

                // 4. Save to _new
                string outPath = OriginalScriptTxt.Text + "_new";
                File.WriteAllBytes(outPath, newScript);

                GetMain().LogToConsole($"BGI: Successfully injected translations and saved to {Path.GetFileName(outPath)}");
            }
            catch (Exception ex)
            {
                GetMain().LogToConsole($"BGI [Error]: {ex.Message}");
            }
        }
    }
}
