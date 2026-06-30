using System;
using System.IO;
using System.Threading.Tasks;

namespace NicheStudioWeirdo.Utils
{
    public static class AlicesoftUtils
    {
        private static string GetAliceExe()
        {
            string repoDir = SettingsManager.Config.ReposPath;
            if (string.IsNullOrWhiteSpace(repoDir))
            {
                repoDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            // First check if it's in the repo directory structure
            string path1 = Path.Combine(repoDir, "NicheStudioWeirdo", "Utility", "Alicesoft", "alice.exe");
            if (File.Exists(path1)) return $"\"{path1}\"";
            
            // Fallback for published builds
            string path2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Alicesoft", "alice.exe");
            return $"\"{path2}\"";
        }

        private static string GetRepoDir()
        {
            return SettingsManager.Config.ReposPath;
        }

        // Archive Commands (AFA, ALD, DAT, etc.)
        public static async Task ExtractArchiveAsync(string archivePath, MainWindow main)
        {
            // Syntax: alice ar extract <archive> [-o <outdir>]
            string outDir = Path.Combine(Path.GetDirectoryName(archivePath) ?? "", Path.GetFileNameWithoutExtension(archivePath) + "_ext");
            string args = $"ar extract \"{archivePath}\" -o \"{outDir}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task PackArchiveAsync(string sourceFolder, string outArchive, MainWindow main)
        {
            // Syntax: alice ar pack <dir> <archive>
            string args = $"ar pack \"{sourceFolder}\" \"{outArchive}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task ListArchiveAsync(string archivePath, MainWindow main)
        {
            string args = $"ar list \"{archivePath}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        // Script Commands (AIN)
        public static async Task DumpAinAsync(string ainPath, MainWindow main)
        {
            // Syntax: alice ain dump <ain> -o <outdir>
            string outDir = Path.Combine(Path.GetDirectoryName(ainPath) ?? "", Path.GetFileNameWithoutExtension(ainPath) + "_dump");
            string args = $"ain dump \"{ainPath}\" -o \"{outDir}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task EditAinAsync(string originalAin, string modifiedJson, string outputAin, MainWindow main)
        {
            // Syntax: alice ain edit <ain> <json> -o <out>
            string args = $"ain edit \"{originalAin}\" \"{modifiedJson}\" -o \"{outputAin}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        // Image Commands (CG)
        public static async Task ConvertCgAsync(string inputCg, string outputImage, MainWindow main)
        {
            // Syntax: alice cg convert <in> <out>
            string args = $"cg convert \"{inputCg}\" \"{outputImage}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }
    }
}
