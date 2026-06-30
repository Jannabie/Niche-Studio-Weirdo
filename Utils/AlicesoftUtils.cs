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
            string tempTxt = ProcessTxtForEditing(modifiedTxt);
            try
            {
                // Syntax: alice ain edit <ain> -t <txt> -o <out>
                string args = $"ain edit \"{originalAin}\" -t \"{tempTxt}\" -o \"{outputAin}\"";
                await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
            }
            finally
            {
                if (System.IO.File.Exists(tempTxt)) System.IO.File.Delete(tempTxt);
            }
        }

        // Database Commands (EX)
        public static async Task DumpExAsync(string exPath, string outFile, MainWindow main)
        {
            // Syntax: alice ex dump <ex> -o <outfile>
            string args = $"ex dump \"{exPath}\" -o \"{outFile}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task EditExAsync(string originalEx, string modifiedTxt, string outputEx, MainWindow main)
        {
            // Syntax: alice ex edit <ex> <txt> -o <out>
            string args = $"ex edit \"{originalEx}\" \"{modifiedTxt}\" -o \"{outputEx}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        private static string ProcessTxtForEditing(string inputFile)
        {
            string tempFile = System.IO.Path.GetTempFileName();
            var lines = System.IO.File.ReadAllLines(inputFile, System.Text.Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";m[") || lines[i].StartsWith(";s["))
                {
                    lines[i] = lines[i].Substring(1); // Remove the leading semicolon only for valid text assignments
                }
            }
            System.IO.File.WriteAllLines(tempFile, lines, new System.Text.UTF8Encoding(false)); // UTF-8 without BOM
            return tempFile;
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
