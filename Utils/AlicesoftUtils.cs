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
            if (File.Exists(path1)) return path1;
            
            // Fallback for published builds
            string path2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Alicesoft", "alice.exe");
            return path2;
        }

        private static string GetRepoDir()
        {
            string repoDir = SettingsManager.Config.ReposPath;
            if (string.IsNullOrWhiteSpace(repoDir) || !Directory.Exists(repoDir))
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
            return repoDir;
        }

        // Archive Commands (AFA, ALD, DAT, etc.)
        public static async Task ExtractArchiveAsync(string archivePath, string outDir, MainWindow main)
        {
            // Syntax: alice ar extract <archive> [-o <outdir>]
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
        public static async Task DumpAinAsync(string ainPath, string outFile, MainWindow main)
        {
            // Syntax: alice ain dump <ain> -t -o <outfile>
            string args = $"ain dump \"{ainPath}\" -t -o \"{outFile}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task EditAinAsync(string originalAin, string modifiedTxt, string outputAin, MainWindow main)
        {
            // Syntax: alice ain edit <ain> -t <txt> -o <out>
            string args = $"ain edit \"{originalAin}\" -t \"{modifiedTxt}\" -o \"{outputAin}\"";
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
