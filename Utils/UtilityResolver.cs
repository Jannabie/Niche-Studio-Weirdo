using System;
using System.IO;

namespace NicheStudioWeirdo.Utils
{
    public static class UtilityResolver
    {
        public static string GetToolPath(string repoName, string scriptName = "")
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", repoName);
            return string.IsNullOrEmpty(scriptName) ? basePath : Path.Combine(basePath, scriptName);
        }

        public static string Python => SettingsManager.Config.PythonPath;
    }
}
