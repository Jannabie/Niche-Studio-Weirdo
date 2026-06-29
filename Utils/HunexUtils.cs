using System;
using System.IO;
using System.Threading.Tasks;

namespace NicheStudioWeirdo.Utils
{
    public static class HunexUtils
    {
        private static string GetRepoDir()
        {
            return UtilityResolver.GetToolPath("Hunex Mahoyo");
        }

        private static string GetPython()
        {
            return SettingsManager.Config.PythonPath;
        }

        public static async Task ListHfaAsync(string hfaPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"hfa_tool.py list \"{hfaPath}\"", main);
        }

        public static async Task UnpackHfaAsync(string hfaPath, string outputFolder, MainWindow main)
        {
            string args = $"hfa_tool.py unpack \"{hfaPath}\"";
            if (!string.IsNullOrWhiteSpace(outputFolder) && !outputFolder.Contains("Select folder"))
            {
                args += $" \"{outputFolder}\"";
            }
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), args, main);
        }

        public static async Task RepackHfaAsync(string folderPath, string outputHfa, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"hfa_tool.py repack \"{folderPath}\" \"{outputHfa}\"", main);
        }

        public static async Task DecompressCtdAsync(string ctdPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"ctd_tool.py decompress \"{ctdPath}\" \"{outPath}\"", main);
        }

        public static async Task CompressCtdAsync(string txtPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"ctd_tool.py compress \"{txtPath}\" \"{outPath}\"", main);
        }

        public static async Task DecodeCbgAsync(string cbgPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"cbg_tool.py decode \"{cbgPath}\" \"{outPath}\"", main);
        }

        public static async Task EncodeCbgAsync(string pngPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"cbg_tool.py encode \"{pngPath}\" \"{outPath}\"", main);
        }

        public static async Task DecodeMzpAsync(string mzpPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"mzp_tool.py decode \"{mzpPath}\" \"{outPath}\"", main);
        }

        public static async Task EncodeMzpAsync(string pngPath, string originalMzpPath, string outPath, MainWindow main)
        {
            await ToolRunner.RunAsync(GetRepoDir(), GetPython(), $"mzp_tool.py encode \"{pngPath}\" \"{originalMzpPath}\" \"{outPath}\"", main);
        }
    }
}
