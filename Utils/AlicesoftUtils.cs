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
            // --input-encoding sjis is required for ALD/older archives that use Shift-JIS filenames
            string args = $"ar extract --input-encoding sjis \"{archivePath}\" -o \"{outDir}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task PackArchiveAsync(string sourceFolder, string outArchive, MainWindow main)
        {
            // alice ar pack ONLY supports creating .afa archives (AFAv2).
            // .ald is a read-only legacy format — extraction works, repacking is not supported.
            string ext = System.IO.Path.GetExtension(outArchive).ToLowerInvariant();
            if (ext != ".afa")
            {
                main?.LogToConsole($"✘ [ERROR] alice-tools can only CREATE .afa archives. " +
                    $"Repacking to '{ext}' is not supported. " +
                    $"Extract from your .ald, edit the files, then repack as .afa instead.");
                return;
            }

            // ── MANIFEST FIX ──────────────────────────────────────────────────────────
            // alice's manifest parser crashes on absolute paths that contain ':' (drive letter).
            // Workaround: put the manifest in the OUTPUT directory, use only the bare
            // filename (relative) for the output archive, and run alice with that directory
            // as the working directory.
            string outDir  = System.IO.Path.GetDirectoryName(outArchive) ?? AppDomain.CurrentDomain.BaseDirectory;
            string outName = System.IO.Path.GetFileName(outArchive); // e.g. "script_repacked.afa"

            System.IO.Directory.CreateDirectory(outDir);
            string manifestPath = System.IO.Path.Combine(outDir, $"_alice_pack_{System.Guid.NewGuid():N}.mft");

            try
            {
                var allFiles = System.IO.Directory.GetFiles(sourceFolder, "*", System.IO.SearchOption.AllDirectories);
                var lines = new System.Collections.Generic.List<string>
                {
                    "#ALICEPACK",
                    $"\"{outName}\""   // relative — alice resolves it from its working dir
                };
                foreach (var f in allFiles)
                    lines.Add($"\"{f.Replace('\\', '/')}\"");

                System.IO.File.WriteAllLines(manifestPath, lines,
                    new System.Text.UTF8Encoding(false)); // UTF-8 no BOM

                // Run alice FROM outDir so the relative output name resolves correctly.
                // Use the safe ArgumentList overload (handles spaces in manifest path).
                var argList = new[] { "ar", "pack", manifestPath };
                await ToolRunner.RunAsync(outDir, GetAliceExe(), argList, main);
            }
            finally
            {
                if (System.IO.File.Exists(manifestPath))
                    System.IO.File.Delete(manifestPath);
            }
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
            // Correct syntax: alice ex dump -o <outfile> <ex>
            string args = $"ex dump -o \"{outFile}\" \"{exPath}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }

        public static async Task EditExAsync(string originalEx, string modifiedTxt, string outputEx, MainWindow main)
        {
            // Correct syntax: alice ex build -o <out.ex> <edited.x>
            // Note: 'originalEx' param is unused here - build only needs the edited text
            string args = $"ex build -o \"{outputEx}\" \"{modifiedTxt}\"";
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
            string inExt = System.IO.Path.GetExtension(inputCg).ToLower();
            string outExt = System.IO.Path.GetExtension(outputImage).ToLower();
            
            bool inIsStandard = inExt == ".png" || inExt == ".webp";
            bool outIsStandard = outExt == ".png" || outExt == ".webp";
            
            // If converting from PNG/WEBP back to an AliceSoft extension, we just rename/copy
            // because the System4.0 engine reads PNG files natively regardless of extension!
            if (inIsStandard && !outIsStandard)
            {
                try
                {
                    System.IO.File.Copy(inputCg, outputImage, true);
                    main?.LogToConsole($"✓ [SUCCESS] Copied standard image to AliceSoft format ({outExt}). The engine reads this natively.");
                }
                catch (Exception ex)
                {
                    main?.LogToConsole($"✘ [ERROR] Failed to copy image: {ex.Message}");
                }
                return;
            }

            // Otherwise, use alice-tools (extracting AliceSoft format to PNG/WEBP)
            // Syntax: alice cg convert <in> <out>
            string args = $"cg convert \"{inputCg}\" \"{outputImage}\"";
            await ToolRunner.RunAsync(GetRepoDir(), GetAliceExe(), args, main);
        }
    }
}
