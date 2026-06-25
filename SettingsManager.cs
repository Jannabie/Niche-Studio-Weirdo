using System;
using System.IO;
using System.Text.Json;

namespace NicheStudioWeirdo
{
    public class AppSettings
    {
        public string ReposPath { get; set; } = @"c:\Users\user\Downloads\Solving Software Visual Novel\repos";
        public string PythonPath { get; set; } = "python";
        public string ExkizpakPath { get; set; } = "";
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        public static AppSettings Config { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    Config = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    // Fallback to default if corrupted
                    Config = new AppSettings();
                }
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
