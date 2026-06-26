using System;
using System.IO;

namespace NicheStudioWeirdo.Utils
{
    public static class ExternalToolsResolver
    {
        public static string GetToolPath(string repoName, string scriptName = "")
        {
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalTools", repoName);
            return string.IsNullOrEmpty(scriptName) ? basePath : Path.Combine(basePath, scriptName);
        }

        public static string Python => SettingsManager.Config.PythonPath;
    }
}
