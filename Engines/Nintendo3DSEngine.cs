using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NicheStudioWeirdo.Engines
{
    public static class Nintendo3DSEngine
    {
        // All tools live here. They MUST run from this directory so they can load their DLLs.
        private static string ToolsDir =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utility", "Trikintul");

        private static string ToolPath(string name) => Path.Combine(ToolsDir, name);

        /// <summary>
        /// Runs a tool from ITS OWN directory (required for DLL loading).
        /// All file arguments must be absolute paths.
        /// </summary>
        private static async Task<int> Run(string toolName, IEnumerable<string> args, MainWindow main)
        {
            string path = ToolPath(toolName);
            if (!File.Exists(path))
            {
                main.LogToConsole($"✘ [ERROR] Missing tool: {toolName}  (expected at {path})");
                return -1;
            }
            // Working directory = tools folder so DLLs are found.
            return await ToolRunner.RunAsync(ToolsDir, path, args, main);
        }

        private static void SafeRename(string src, string dst)
        {
            if (!File.Exists(src)) return;
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(src, dst);
        }

        private static void SafeDelete(string path) { if (File.Exists(path)) File.Delete(path); }
        private static void SafeDir(string path) { if (!Directory.Exists(path)) Directory.CreateDirectory(path); }

        // ──────────────────────────────────────────────────────────────────────
        // PUBLIC ENTRY POINTS
        // ──────────────────────────────────────────────────────────────────────

        public static async Task ExtractArchive(string inputPath, MainWindow main)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if      (ext == ".cia") await ExtractCIA(inputPath, main);
            else if (ext == ".3ds") await Extract3DS(inputPath, main);
            else main.LogToConsole("Unsupported format. Select a .cia or .3ds file.");
        }

        public static async Task RepackArchive(string unpackedDir, bool isCia, MainWindow main)
        {
            if (isCia) await RepackCIA(unpackedDir, main);
            else       await Repack3DS(unpackedDir, main);
        }

        // ──────────────────────────────────────────────────────────────────────
        // DECRYPT ARCHIVE
        // ──────────────────────────────────────────────────────────────────────

        public static async Task DecryptArchive(string inputPath, MainWindow main)
        {
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            if (ext != ".cia" && ext != ".3ds")
            {
                main.LogToConsole("Unsupported format. Select a .cia or .3ds file to decrypt.");
                return;
            }

            string baseDir  = Path.GetDirectoryName(inputPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string outPath  = Path.Combine(baseDir, baseName + "-decrypted" + ext);

            main.LogToConsole($"[Trikintul] Decrypting: {Path.GetFileName(inputPath)}");

            string ncchDir = Path.Combine(baseDir, baseName + "_NCCH");
            Directory.CreateDirectory(ncchDir);

            // ── Clear stale NCCH files from any previous run ──────────────────
            foreach (var stale in Directory.GetFiles(ncchDir, "*.ncch"))
            {
                main.LogToConsole($"  [Cleanup] Removing stale: {Path.GetFileName(stale)}");
                File.Delete(stale);
            }

            try
            {
                string decryptSrc = ToolPath("decrypt.exe");
                string seedSrc    = ToolPath("seeddb.bin");

                if (!File.Exists(decryptSrc) || !File.Exists(seedSrc))
                {
                    main.LogToConsole("✘ [ERROR] Missing decrypt.exe or seeddb.bin in Utility\\Trikintul.");
                    return;
                }

                string decryptTemp = Path.Combine(ncchDir, "decrypt.exe");
                File.Copy(decryptSrc, decryptTemp, true);
                File.Copy(seedSrc, Path.Combine(ncchDir, "seeddb.bin"), true);

                // ── Step 1: Run decrypt.exe ───────────────────────────────────
                main.LogToConsole($"[Step 1/2] Extracting NCCH to: {ncchDir}");
                if (await ToolRunner.RunAsync(ncchDir, decryptTemp, new[] { inputPath }, main) != 0)
                {
                    main.LogToConsole("✘ [ERROR] decrypt.exe failed. See log for details.");
                    return;
                }

                // ── Locate NCCH files: check ncchDir first, then baseDir ──────
                var ncchFiles = Directory.GetFiles(ncchDir, "*.ncch");
                if (ncchFiles.Length == 0)
                {
                    // decrypt.exe may have written next to the CIA file instead
                    main.LogToConsole("  [INFO] No NCCH in _NCCH folder, checking CIA's directory...");
                    var baseDirNcch = Directory.GetFiles(baseDir, $"{baseName}.*.ncch");
                    if (baseDirNcch.Length > 0)
                    {
                        foreach (var f in baseDirNcch)
                        {
                            var dest = Path.Combine(ncchDir, Path.GetFileName(f));
                            main.LogToConsole($"  [Move] {Path.GetFileName(f)} → {ncchDir}");
                            File.Move(f, dest, true);
                        }
                        ncchFiles = Directory.GetFiles(ncchDir, "*.ncch");
                    }
                }

                if (ncchFiles.Length == 0)
                {
                    main.LogToConsole("✘ [ERROR] No .ncch files were generated. Is the file already decrypted?");
                    return;
                }

                Array.Sort(ncchFiles);

                // ── Log NCCH sizes so we can see if they are correct ──────────
                main.LogToConsole($"  Found {ncchFiles.Length} NCCH file(s):");
                foreach (var f in ncchFiles)
                    main.LogToConsole($"    {Path.GetFileName(f)}: {new FileInfo(f).Length:N0} bytes");

                // ── Read TitleVersion from ctrtool ────────────────────────────
                int titleVersion = 0;
                if (ext == ".cia")
                {
                    string ctrToolSrc = ToolPath("ctrtool.exe");
                    if (File.Exists(ctrToolSrc))
                    {
                        var ctrPsi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName               = ctrToolSrc,
                            WorkingDirectory       = ToolsDir,
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            RedirectStandardInput  = true,
                            CreateNoWindow         = true,
                        };
                        ctrPsi.ArgumentList.Add($"--seeddb={seedSrc}");
                        ctrPsi.ArgumentList.Add(inputPath);

                        using var proc = System.Diagnostics.Process.Start(ctrPsi)!;
                        try { proc.StandardInput.WriteLine(); proc.StandardInput.Close(); } catch { }
                        string ctrOut = await proc.StandardOutput.ReadToEndAsync();
                        await proc.WaitForExitAsync();

                        foreach (var line in ctrOut.Split('\n'))
                        {
                            var t = line.Trim();
                            if (t.StartsWith("TitleVersion", StringComparison.OrdinalIgnoreCase) && t.Contains(':'))
                            {
                                var val = t.Substring(t.IndexOf(':') + 1).Trim();
                                if (int.TryParse(val, out int v)) { titleVersion = v; break; }
                            }
                        }
                        main.LogToConsole($"  TitleVersion: {titleVersion}");
                    }
                }

                // ── Step 2: makerom — use -i flag exactly like batch script ───
                main.LogToConsole($"[Step 2/2] Assembling decrypted {ext.ToUpper()} from NCCH files...");

                var makeromArgs = new List<string>
                {
                    "-f", ext == ".cia" ? "cia" : "cci",
                    "-ignoresign",
                    "-target", "p",
                };

                var partitionMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Main", 0 },
                    { "Manual", 1 },
                    { "DownloadPlay", 2 },
                    { "Partition4", 3 },
                    { "Partition5", 4 },
                    { "Partition6", 5 },
                    { "N3DSUpdateData", 6 },
                    { "UpdateData", 7 }
                };

                foreach (var file in ncchFiles)
                {
                    // The file is typically named "smtiv.Main.ncch" or "tmp.Main.ncch"
                    // Let's parse the name to find which partition it is.
                    string namePart = Path.GetFileNameWithoutExtension(file); // e.g. "smtiv.Main"
                    int dotIdx = namePart.LastIndexOf('.');
                    string key = dotIdx >= 0 ? namePart.Substring(dotIdx + 1) : namePart;
                    
                    int index = 0; // default
                    if (partitionMap.TryGetValue(key, out int mappedIdx))
                    {
                        index = mappedIdx;
                    }
                    else if (int.TryParse(key, out int parsedIdx)) // Just in case they are named 0, 1, 2
                    {
                        index = parsedIdx;
                    }

                    makeromArgs.Add("-i");
                    makeromArgs.Add($"{file}:{index}:{index}");
                }

                makeromArgs.Add("-ver");
                makeromArgs.Add(titleVersion.ToString());
                makeromArgs.Add("-o");
                makeromArgs.Add(outPath);

                if (await Run("makerom.exe", makeromArgs, main) == 0)
                {
                    long origSize = new FileInfo(inputPath).Length;
                    long outSize  = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                    double pctDiff = origSize > 0 ? Math.Abs(outSize - origSize) * 100.0 / origSize : 0;

                    main.LogToConsole($"COMPLETE! Output: {outPath}");
                    main.LogToConsole($"COMPLETE! Size: {outSize:N0} bytes  (original: {origSize:N0} bytes, diff: {pctDiff:F1}%)");
                    main.LogToConsole($"COMPLETE! NCCH files kept in: {ncchDir}");

                    if (pctDiff > 2.0)
                        main.LogToConsole($"⚠ WARNING: {pctDiff:F1}% size difference detected — check NCCH sizes above for clues.");
                }
                else
                {
                    main.LogToConsole("✘ [ERROR] makerom failed to build decrypted file.");
                }
            }
            finally
            {
                try
                {
                    string d = Path.Combine(ncchDir, "decrypt.exe");
                    string s = Path.Combine(ncchDir, "seeddb.bin");
                    if (File.Exists(d)) File.Delete(d);
                    if (File.Exists(s)) File.Delete(s);
                }
                catch { }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // EXTRACT CIA
        // ──────────────────────────────────────────────────────────────────────

        private static async Task ExtractCIA(string inputPath, MainWindow main)
        {
            string outDir = inputPath + "_Unpacked";
            SafeDir(outDir);
            main.LogToConsole($"[Trikintul] Extracting CIA: {Path.GetFileName(inputPath)}");

            // Step 1 – ctrtool: dump raw partitions
            main.LogToConsole("[Step 1/6] Decrypting CIA with ctrtool...");
            string contentPrefix = Path.Combine(outDir, "DecryptedApp");
            if (await Run("ctrtool.exe", new[] { $"--content={contentPrefix}", inputPath }, main) != 0)
            {
                main.LogToConsole("✘ ctrtool failed. Make sure the CIA is decrypted."); return;
            }

            // Step 2 – rename partitions
            main.LogToConsole("[Step 2/6] Renaming partition files...");
            foreach (var f in Directory.GetFiles(outDir, "DecryptedApp.000*.*"))
            {
                if (f.Contains(".0000.")) SafeRename(f, Path.Combine(outDir, "DecryptedPartition0.bin"));
                else if (f.Contains(".0001.")) SafeRename(f, Path.Combine(outDir, "DecryptedPartition1.bin"));
                else if (f.Contains(".0002.")) SafeRename(f, Path.Combine(outDir, "DecryptedPartition2.bin"));
            }

            bool isDecrypted = inputPath.Contains("-decrypted", StringComparison.OrdinalIgnoreCase);

            // Step 3 – extract CXI (ExeFS + RomFS + optional Logo/PlainRGN)
            main.LogToConsole("[Step 3/6] Extracting CXI partition...");
            main.LogToConsole("          (Errors about LogoLZ.bin / PlainRGN.bin are harmless if the game lacks them)");
            var cxiEx = new List<string> {
                "-xvtf", "cxi", Path.Combine(outDir, "DecryptedPartition0.bin"),
                "--header",  Path.Combine(outDir, "HeaderNCCH0.bin"),
                "--exh",     Path.Combine(outDir, "DecryptedExHeader.bin"), 
                "--exefs",   Path.Combine(outDir, "DecryptedExeFS.bin"),    
                "--romfs",   Path.Combine(outDir, "DecryptedRomFS.bin"),    
                "--logo",    Path.Combine(outDir, "LogoLZ.bin"),
                "--plain",   Path.Combine(outDir, "PlainRGN.bin"),
            };

            if (!isDecrypted)
            {
                cxiEx.Insert(cxiEx.IndexOf("--exh") + 2, "--exh-auto-key");
                cxiEx.Insert(cxiEx.IndexOf("--exefs") + 2, "--exefs-auto-key");
                cxiEx.Insert(cxiEx.IndexOf("--exefs") + 3, "--exefs-top-auto-key");
                cxiEx.Insert(cxiEx.IndexOf("--romfs") + 2, "--romfs-auto-key");
            }

            await Run("3dstool.exe", cxiEx, main); // non-fatal; logo/plain errors expected

            // Step 4 – extract CFA partitions (Manual, DownloadPlay) if they exist
            main.LogToConsole("[Step 4/6] Extracting CFA partitions (Manual, DownloadPlay)...");
            if (File.Exists(Path.Combine(outDir, "DecryptedPartition1.bin")))
            {
                var cfaEx1 = new List<string> { "-xvtf", "cfa", Path.Combine(outDir, "DecryptedPartition1.bin"),
                    "--header", Path.Combine(outDir, "HeaderNCCH1.bin"),
                    "--romfs",  Path.Combine(outDir, "DecryptedManual.bin") };
                if (!isDecrypted) cfaEx1.Add("--romfs-auto-key");
                await Run("3dstool.exe", cfaEx1, main);
            }

            if (File.Exists(Path.Combine(outDir, "DecryptedPartition2.bin")))
            {
                var cfaEx2 = new List<string> { "-xvtf", "cfa", Path.Combine(outDir, "DecryptedPartition2.bin"),
                    "--header", Path.Combine(outDir, "HeaderNCCH2.bin"),
                    "--romfs",  Path.Combine(outDir, "DecryptedDownloadPlay.bin") };
                if (!isDecrypted) cfaEx2.Add("--romfs-auto-key");
                await Run("3dstool.exe", cfaEx2, main);
            }

            SafeDelete(Path.Combine(outDir, "DecryptedPartition0.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition1.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition2.bin"));

            // Step 5 – extract ExeFS/RomFS dirs
            main.LogToConsole("[Step 5/6] Extracting ExeFS and RomFS directories...");
            if (File.Exists(Path.Combine(outDir, "DecryptedExeFS.bin")))
                await Run("3dstool.exe", new[] { "-xvtfu", "exefs", Path.Combine(outDir, "DecryptedExeFS.bin"),
                    "--header", Path.Combine(outDir, "HeaderExeFS.bin"),
                    "--exefs-dir", Path.Combine(outDir, "ExtractedExeFS") }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedRomFS.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedRomFS.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedRomFS") }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedManual.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedManual.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedManual") }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedDownloadPlay.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedDownloadPlay.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedDownloadPlay") }, main);

            // Step 6 – rename ExeFS files + extract banner
            main.LogToConsole("[Step 6/6] Renaming ExeFS files and extracting banner...");
            SafeRename(Path.Combine(outDir, "ExtractedExeFS", "banner.bnr"), Path.Combine(outDir, "banner.bin"));
            SafeRename(Path.Combine(outDir, "ExtractedExeFS", "icon.icn"),   Path.Combine(outDir, "ExtractedExeFS", "icon.bin"));
            if (File.Exists(Path.Combine(outDir, "banner.bin")))
            {
                await Run("3dstool.exe", new[] { "-xv", "-t", "banner", "-f", Path.Combine(outDir, "banner.bin"),
                    "--banner-dir", Path.Combine(outDir, "ExtractedBanner") }, main);
                SafeDelete(Path.Combine(outDir, "banner.bin"));
                SafeRename(Path.Combine(outDir, "ExtractedBanner", "banner0.bcmdl"),
                           Path.Combine(outDir, "ExtractedBanner", "banner.cgfx"));
            }

            main.LogToConsole($"✔ Done! Extracted to: {outDir}");
        }

        // ──────────────────────────────────────────────────────────────────────
        // REPACK CIA
        // ──────────────────────────────────────────────────────────────────────

        private static async Task RepackCIA(string unpacked, MainWindow main)
        {
            if (unpacked.EndsWith("\\")) unpacked = unpacked.TrimEnd('\\');

            // Derive original CIA path from folder name  e.g. "SMTIV.cia_Unpacked" → "SMTIV.cia"
            string originalCia = unpacked.EndsWith("_Unpacked", StringComparison.OrdinalIgnoreCase)
                ? unpacked[..^"_Unpacked".Length]
                : unpacked + ".cia";

            string outCia = Path.Combine(
                Path.GetDirectoryName(originalCia)!,
                Path.GetFileNameWithoutExtension(originalCia) + "_Edited.cia");

            main.LogToConsole($"[Trikintul] Repacking CIA from: {Path.GetFileName(unpacked)}");
            main.LogToConsole($"            Output will be:    {outCia}");

            // Step 1 – rebuild banner
            main.LogToConsole("[Step 1/5] Rebuilding banner...");
            SafeRename(Path.Combine(unpacked, "ExtractedBanner", "banner.cgfx"),
                       Path.Combine(unpacked, "ExtractedBanner", "banner0.bcmdl"));
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedBanner")))
                await Run("3dstool.exe", new[] { "-cv", "-t", "banner",
                    "-f", Path.Combine(unpacked, "banner.bin"),
                    "--banner-dir", Path.Combine(unpacked, "ExtractedBanner") }, main);
            SafeRename(Path.Combine(unpacked, "ExtractedBanner", "banner0.bcmdl"),
                       Path.Combine(unpacked, "ExtractedBanner", "banner.cgfx")); // restore
            SafeRename(Path.Combine(unpacked, "banner.bin"),
                       Path.Combine(unpacked, "ExtractedExeFS", "banner.bnr"));
            SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "icon.bin"),
                       Path.Combine(unpacked, "ExtractedExeFS", "icon.icn"));

            // Step 2 – rebuild ExeFS
            main.LogToConsole("[Step 2/5] Rebuilding ExeFS...");
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedExeFS")))
            {
                if (await Run("3dstool.exe", new[] { "-cvtfz", "exefs",
                    Path.Combine(unpacked, "CustomExeFS.bin"),
                    "--header",    Path.Combine(unpacked, "HeaderExeFS.bin"),
                    "--exefs-dir", Path.Combine(unpacked, "ExtractedExeFS") }, main) != 0)
                { main.LogToConsole("✘ ExeFS rebuild failed."); return; }
                // restore renamed files
                SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "banner.bnr"),
                           Path.Combine(unpacked, "ExtractedExeFS", "banner.bin"));
                SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "icon.icn"),
                           Path.Combine(unpacked, "ExtractedExeFS", "icon.bin"));
            }
            else
            {
                main.LogToConsole("✘ [ERROR] ExtractedExeFS directory is missing.");
                main.LogToConsole("          This usually happens if you tried to Extract an ENCRYPTED CIA.");
                main.LogToConsole("          Make sure you Decrypt the CIA first, then Extract the '-decrypted' file!");
                return;
            }

            // Step 3 – rebuild RomFS partitions
            main.LogToConsole("[Step 3/5] Rebuilding RomFS partitions...");
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedRomFS")))
            {
                if (await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomRomFS.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedRomFS") }, main) != 0)
                { main.LogToConsole("✘ RomFS rebuild failed."); return; }
            }
            else
            {
                main.LogToConsole("✘ [ERROR] ExtractedRomFS directory is missing.");
                main.LogToConsole("          Extraction probably failed. Ensure you Extract a DECRYPTED file.");
                return;
            }

            if (Directory.Exists(Path.Combine(unpacked, "ExtractedManual")))
                await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomManual.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedManual") }, main);

            if (Directory.Exists(Path.Combine(unpacked, "ExtractedDownloadPlay")))
                await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomDownloadPlay.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedDownloadPlay") }, main);

            // Step 4 – rebuild CXI + CFA partitions (clean slate first)
            main.LogToConsole("[Step 4/5] Rebuilding CXI and CFA partitions...");
            SafeDelete(Path.Combine(unpacked, "CustomPartition0.bin"));
            SafeDelete(Path.Combine(unpacked, "CustomPartition1.bin"));
            SafeDelete(Path.Combine(unpacked, "CustomPartition2.bin"));

            bool isDecrypted = unpacked.Contains("-decrypted", StringComparison.OrdinalIgnoreCase);

            var cxiArgs = new List<string> {
                "-cvtf", "cxi", Path.Combine(unpacked, "CustomPartition0.bin"),
                "--header",            Path.Combine(unpacked, "HeaderNCCH0.bin"),
                "--exh",               Path.Combine(unpacked, "DecryptedExHeader.bin"),
                "--exefs",             Path.Combine(unpacked, "CustomExeFS.bin"),
                "--romfs",             Path.Combine(unpacked, "CustomRomFS.bin")
            };

            if (!isDecrypted)
            {
                cxiArgs.Insert(cxiArgs.IndexOf("--exh") + 2, "--exh-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--exefs") + 2, "--exefs-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--exefs") + 3, "--exefs-top-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--romfs") + 2, "--romfs-auto-key");
            }

            if (File.Exists(Path.Combine(unpacked, "LogoLZ.bin")))   { cxiArgs.Add("--logo");  cxiArgs.Add(Path.Combine(unpacked, "LogoLZ.bin")); }
            if (File.Exists(Path.Combine(unpacked, "PlainRGN.bin")))  { cxiArgs.Add("--plain"); cxiArgs.Add(Path.Combine(unpacked, "PlainRGN.bin")); }

            if (await Run("3dstool.exe", cxiArgs, main) != 0)
            { main.LogToConsole("✘ [ERROR] CXI rebuild failed (CustomPartition0). Aborting."); return; }

            // Manual partition (only if both RomFS + header exist)
            if (File.Exists(Path.Combine(unpacked, "CustomManual.bin")) &&
                File.Exists(Path.Combine(unpacked, "HeaderNCCH1.bin")))
            {
                var cfaEx1 = new List<string> { "-cvtf", "cfa",
                    Path.Combine(unpacked, "CustomPartition1.bin"),
                    "--header", Path.Combine(unpacked, "HeaderNCCH1.bin"),
                    "--romfs",  Path.Combine(unpacked, "CustomManual.bin") };
                if (!isDecrypted) cfaEx1.Add("--romfs-auto-key");

                if (await Run("3dstool.exe", cfaEx1, main) != 0)
                { main.LogToConsole("✘ [ERROR] CFA rebuild failed (CustomPartition1/Manual). Aborting."); return; }
            }
            else main.LogToConsole("          (Skipping Partition1/Manual — header or RomFS missing, normal for some games)");

            // DownloadPlay partition (only if both RomFS + header exist)
            if (File.Exists(Path.Combine(unpacked, "CustomDownloadPlay.bin")) &&
                File.Exists(Path.Combine(unpacked, "HeaderNCCH2.bin")))
            {
                var cfaEx2 = new List<string> { "-cvtf", "cfa",
                    Path.Combine(unpacked, "CustomPartition2.bin"),
                    "--header", Path.Combine(unpacked, "HeaderNCCH2.bin"),
                    "--romfs",  Path.Combine(unpacked, "CustomDownloadPlay.bin") };
                if (!isDecrypted) cfaEx2.Add("--romfs-auto-key");

                if (await Run("3dstool.exe", cfaEx2, main) != 0)
                { main.LogToConsole("✘ [ERROR] CFA rebuild failed (CustomPartition2/DownloadPlay). Aborting."); return; }
            }
            else main.LogToConsole("          (Skipping Partition2/DownloadPlay — header or RomFS missing, normal for some games)");

            // Step 5 – assemble final CIA with makerom
            main.LogToConsole("[Step 5/5] Assembling final CIA with makerom...");
            var makeromArgs = new List<string> { "-target", "p", "-ignoresign", "-f", "cia" };
            if (File.Exists(Path.Combine(unpacked, "CustomPartition0.bin")))
            { makeromArgs.Add("-content"); makeromArgs.Add($"{Path.Combine(unpacked, "CustomPartition0.bin")}:0:0x00"); }
            if (File.Exists(Path.Combine(unpacked, "CustomPartition1.bin")))
            { makeromArgs.Add("-content"); makeromArgs.Add($"{Path.Combine(unpacked, "CustomPartition1.bin")}:1:0x01"); }
            if (File.Exists(Path.Combine(unpacked, "CustomPartition2.bin")))
            { makeromArgs.Add("-content"); makeromArgs.Add($"{Path.Combine(unpacked, "CustomPartition2.bin")}:2:0x02"); }
            makeromArgs.Add("-o");
            makeromArgs.Add(outCia);

            if (await Run("makerom.exe", makeromArgs, main) == 0)
                main.LogToConsole($"✔ Done! Output: {outCia}");
            else
                main.LogToConsole("✘ makerom failed to create CIA. Check the log above for details.");
        }

        // ──────────────────────────────────────────────────────────────────────
        // EXTRACT 3DS
        // ──────────────────────────────────────────────────────────────────────

        private static async Task Extract3DS(string inputPath, MainWindow main)
        {
            string outDir = inputPath + "_Unpacked";
            SafeDir(outDir);
            main.LogToConsole($"[Trikintul] Extracting 3DS: {Path.GetFileName(inputPath)}");

            main.LogToConsole("[Step 1/7] Extracting NCSD partitions...");
            var ncsdArgs = new List<string> {
                "-xvt01267f", "cci",
                Path.Combine(outDir, "DecryptedPartition0.bin"),
                Path.Combine(outDir, "DecryptedPartition1.bin"),
                Path.Combine(outDir, "DecryptedPartition2.bin"),
                Path.Combine(outDir, "DecryptedPartition6.bin"),
                Path.Combine(outDir, "DecryptedPartition7.bin"),
                inputPath,
                "--header", Path.Combine(outDir, "HeaderNCSD.bin"),
            };
            if (await Run("3dstool.exe", ncsdArgs, main) != 0)
            { main.LogToConsole("✘ 3dstool failed to extract NCSD. Ensure the 3DS is decrypted."); return; }

            main.LogToConsole("[Step 2/7] Extracting CXI partition...");
            main.LogToConsole("          (Errors about LogoLZ.bin / PlainRGN.bin are harmless if the game lacks them)");
            var cxiEx = new List<string> {
                "-xvtf", "cxi", Path.Combine(outDir, "DecryptedPartition0.bin"),
                "--header",  Path.Combine(outDir, "HeaderNCCH0.bin"),
                "--exh",     Path.Combine(outDir, "DecryptedExHeader.bin"), "--exh-auto-key",
                "--exefs",   Path.Combine(outDir, "DecryptedExeFS.bin"),    "--exefs-auto-key", "--exefs-top-auto-key",
                "--romfs",   Path.Combine(outDir, "DecryptedRomFS.bin"),    "--romfs-auto-key",
                "--logo",    Path.Combine(outDir, "LogoLZ.bin"),
                "--plain",   Path.Combine(outDir, "PlainRGN.bin"),
            };
            await Run("3dstool.exe", cxiEx, main);

            main.LogToConsole("[Step 3/7] Extracting CFA partitions...");
            if (File.Exists(Path.Combine(outDir, "DecryptedPartition1.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "cfa", Path.Combine(outDir, "DecryptedPartition1.bin"),
                    "--header", Path.Combine(outDir, "HeaderNCCH1.bin"),
                    "--romfs",  Path.Combine(outDir, "DecryptedManual.bin"), "--romfs-auto-key" }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedPartition2.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "cfa", Path.Combine(outDir, "DecryptedPartition2.bin"),
                    "--header", Path.Combine(outDir, "HeaderNCCH2.bin"),
                    "--romfs",  Path.Combine(outDir, "DecryptedDownloadPlay.bin"), "--romfs-auto-key" }, main);

            SafeDelete(Path.Combine(outDir, "DecryptedPartition0.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition1.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition2.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition6.bin"));
            SafeDelete(Path.Combine(outDir, "DecryptedPartition7.bin"));

            main.LogToConsole("[Step 4/7] Extracting ExeFS directory...");
            if (File.Exists(Path.Combine(outDir, "DecryptedExeFS.bin")))
                await Run("3dstool.exe", new[] { "-xvtfu", "exefs", Path.Combine(outDir, "DecryptedExeFS.bin"),
                    "--header", Path.Combine(outDir, "HeaderExeFS.bin"),
                    "--exefs-dir", Path.Combine(outDir, "ExtractedExeFS") }, main);

            main.LogToConsole("[Step 5/7] Extracting RomFS directories...");
            if (File.Exists(Path.Combine(outDir, "DecryptedRomFS.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedRomFS.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedRomFS") }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedManual.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedManual.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedManual") }, main);
            if (File.Exists(Path.Combine(outDir, "DecryptedDownloadPlay.bin")))
                await Run("3dstool.exe", new[] { "-xvtf", "romfs", Path.Combine(outDir, "DecryptedDownloadPlay.bin"),
                    "--romfs-dir", Path.Combine(outDir, "ExtractedDownloadPlay") }, main);

            main.LogToConsole("[Step 6/7] Renaming ExeFS files...");
            SafeRename(Path.Combine(outDir, "ExtractedExeFS", "banner.bnr"),
                       Path.Combine(outDir, "ExtractedExeFS", "banner.bin"));
            SafeRename(Path.Combine(outDir, "ExtractedExeFS", "icon.icn"),
                       Path.Combine(outDir, "ExtractedExeFS", "icon.bin"));

            main.LogToConsole("[Step 7/7] Extracting banner...");
            if (File.Exists(Path.Combine(outDir, "ExtractedExeFS", "banner.bin")))
            {
                File.Copy(Path.Combine(outDir, "ExtractedExeFS", "banner.bin"),
                          Path.Combine(outDir, "banner.bin"), true);
                await Run("3dstool.exe", new[] { "-xv", "-t", "banner",
                    "-f", Path.Combine(outDir, "banner.bin"),
                    "--banner-dir", Path.Combine(outDir, "ExtractedBanner") }, main);
                SafeDelete(Path.Combine(outDir, "banner.bin"));
                SafeRename(Path.Combine(outDir, "ExtractedBanner", "banner0.bcmdl"),
                           Path.Combine(outDir, "ExtractedBanner", "banner.cgfx"));
            }

            main.LogToConsole($"✔ Done! Extracted to: {outDir}");
        }

        // ──────────────────────────────────────────────────────────────────────
        // REPACK 3DS
        // ──────────────────────────────────────────────────────────────────────

        private static async Task Repack3DS(string unpacked, MainWindow main)
        {
            if (unpacked.EndsWith("\\")) unpacked = unpacked.TrimEnd('\\');

            string original3ds = unpacked.EndsWith("_Unpacked", StringComparison.OrdinalIgnoreCase)
                ? unpacked[..^"_Unpacked".Length]
                : unpacked + ".3ds";

            string out3ds = Path.Combine(
                Path.GetDirectoryName(original3ds)!,
                Path.GetFileNameWithoutExtension(original3ds) + "_Edited.3ds");

            main.LogToConsole($"[Trikintul] Repacking 3DS from: {Path.GetFileName(unpacked)}");

            // Step 1 – banner
            main.LogToConsole("[Step 1/5] Rebuilding banner...");
            SafeRename(Path.Combine(unpacked, "ExtractedBanner", "banner.cgfx"),
                       Path.Combine(unpacked, "ExtractedBanner", "banner0.bcmdl"));
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedBanner")))
                await Run("3dstool.exe", new[] { "-cv", "-t", "banner",
                    "-f", Path.Combine(unpacked, "banner.bin"),
                    "--banner-dir", Path.Combine(unpacked, "ExtractedBanner") }, main);
            SafeRename(Path.Combine(unpacked, "ExtractedBanner", "banner0.bcmdl"),
                       Path.Combine(unpacked, "ExtractedBanner", "banner.cgfx"));
            SafeRename(Path.Combine(unpacked, "banner.bin"),
                       Path.Combine(unpacked, "ExtractedExeFS", "banner.bnr"));
            SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "icon.bin"),
                       Path.Combine(unpacked, "ExtractedExeFS", "icon.icn"));

            // Step 2 – ExeFS
            main.LogToConsole("[Step 2/5] Rebuilding ExeFS...");
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedExeFS")))
            {
                if (await Run("3dstool.exe", new[] { "-cvtfz", "exefs",
                    Path.Combine(unpacked, "CustomExeFS.bin"),
                    "--header",    Path.Combine(unpacked, "HeaderExeFS.bin"),
                    "--exefs-dir", Path.Combine(unpacked, "ExtractedExeFS") }, main) != 0)
                { main.LogToConsole("✘ ExeFS rebuild failed."); return; }
                SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "banner.bnr"),
                           Path.Combine(unpacked, "ExtractedExeFS", "banner.bin"));
                SafeRename(Path.Combine(unpacked, "ExtractedExeFS", "icon.icn"),
                           Path.Combine(unpacked, "ExtractedExeFS", "icon.bin"));
            }
            else
            {
                main.LogToConsole("✘ [ERROR] ExtractedExeFS directory is missing.");
                main.LogToConsole("          Make sure you Decrypt the 3DS file first, then Extract the '-decrypted' file!");
                return;
            }

            // Step 3 – RomFS
            main.LogToConsole("[Step 3/5] Rebuilding RomFS partitions...");
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedRomFS")))
            {
                if (await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomRomFS.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedRomFS") }, main) != 0)
                { main.LogToConsole("✘ RomFS rebuild failed."); return; }
            }
            else
            {
                main.LogToConsole("✘ [ERROR] ExtractedRomFS directory is missing.");
                return;
            }
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedManual")))
                await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomManual.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedManual") }, main);
            if (Directory.Exists(Path.Combine(unpacked, "ExtractedDownloadPlay")))
                await Run("3dstool.exe", new[] { "-cvtf", "romfs",
                    Path.Combine(unpacked, "CustomDownloadPlay.bin"),
                    "--romfs-dir", Path.Combine(unpacked, "ExtractedDownloadPlay") }, main);

            // Step 4 – partitions
            main.LogToConsole("[Step 4/5] Rebuilding CXI and CFA partitions...");
            SafeDelete(Path.Combine(unpacked, "CustomPartition0.bin"));
            SafeDelete(Path.Combine(unpacked, "CustomPartition1.bin"));
            SafeDelete(Path.Combine(unpacked, "CustomPartition2.bin"));

            bool isDecrypted = unpacked.Contains("-decrypted", StringComparison.OrdinalIgnoreCase);

            var cxiArgs = new List<string> {
                "-cvtf", "cxi", Path.Combine(unpacked, "CustomPartition0.bin"),
                "--header",  Path.Combine(unpacked, "HeaderNCCH0.bin"),
                "--exh",     Path.Combine(unpacked, "DecryptedExHeader.bin"), 
                "--exefs",   Path.Combine(unpacked, "CustomExeFS.bin"), 
                "--romfs",   Path.Combine(unpacked, "CustomRomFS.bin")
            };

            if (!isDecrypted)
            {
                cxiArgs.Insert(cxiArgs.IndexOf("--exh") + 2, "--exh-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--exefs") + 2, "--exefs-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--exefs") + 3, "--exefs-top-auto-key");
                cxiArgs.Insert(cxiArgs.IndexOf("--romfs") + 2, "--romfs-auto-key");
            }

            if (File.Exists(Path.Combine(unpacked, "LogoLZ.bin")))  { cxiArgs.Add("--logo");  cxiArgs.Add(Path.Combine(unpacked, "LogoLZ.bin")); }
            if (File.Exists(Path.Combine(unpacked, "PlainRGN.bin"))) { cxiArgs.Add("--plain"); cxiArgs.Add(Path.Combine(unpacked, "PlainRGN.bin")); }

            if (await Run("3dstool.exe", cxiArgs, main) != 0)
            { main.LogToConsole("✘ CXI rebuild failed. Aborting."); return; }

            if (File.Exists(Path.Combine(unpacked, "CustomManual.bin")) && File.Exists(Path.Combine(unpacked, "HeaderNCCH1.bin")))
            {
                var cfaEx1 = new List<string> { "-cvtf", "cfa",
                    Path.Combine(unpacked, "CustomPartition1.bin"),
                    "--header", Path.Combine(unpacked, "HeaderNCCH1.bin"),
                    "--romfs",  Path.Combine(unpacked, "CustomManual.bin") };
                if (!isDecrypted) cfaEx1.Add("--romfs-auto-key");
                await Run("3dstool.exe", cfaEx1, main);
            }
            else
                main.LogToConsole("          (Skipping Partition1/Manual — header or RomFS missing)");

            if (File.Exists(Path.Combine(unpacked, "CustomDownloadPlay.bin")) && File.Exists(Path.Combine(unpacked, "HeaderNCCH2.bin")))
            {
                var cfaEx2 = new List<string> { "-cvtf", "cfa",
                    Path.Combine(unpacked, "CustomPartition2.bin"),
                    "--header", Path.Combine(unpacked, "HeaderNCCH2.bin"),
                    "--romfs",  Path.Combine(unpacked, "CustomDownloadPlay.bin") };
                if (!isDecrypted) cfaEx2.Add("--romfs-auto-key");
                await Run("3dstool.exe", cfaEx2, main);
            }
            else
                main.LogToConsole("          (Skipping Partition2/DownloadPlay — header or RomFS missing)");

            // Step 5 – assemble 3DS
            main.LogToConsole("[Step 5/5] Assembling final 3DS with 3dstool...");
            var cciBuildArgs = new List<string> { "-cvt01267f", "cci",
                File.Exists(Path.Combine(unpacked, "CustomPartition0.bin")) ? Path.Combine(unpacked, "CustomPartition0.bin") : "",
                File.Exists(Path.Combine(unpacked, "CustomPartition1.bin")) ? Path.Combine(unpacked, "CustomPartition1.bin") : "",
                File.Exists(Path.Combine(unpacked, "CustomPartition2.bin")) ? Path.Combine(unpacked, "CustomPartition2.bin") : "",
                File.Exists(Path.Combine(unpacked, "CustomPartition6.bin")) ? Path.Combine(unpacked, "CustomPartition6.bin") : "",
                File.Exists(Path.Combine(unpacked, "CustomPartition7.bin")) ? Path.Combine(unpacked, "CustomPartition7.bin") : "",
                out3ds,
                "--header", Path.Combine(unpacked, "HeaderNCSD.bin"),
            };

            if (await Run("3dstool.exe", cciBuildArgs, main) == 0)
                main.LogToConsole($"✔ Done! Output: {out3ds}");
            else
                main.LogToConsole("✘ 3dstool failed to build 3DS. Check log above.");
        }
    }
}
