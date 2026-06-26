using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NicheStudioWeirdo.Utils
{
    public class PftEntry
    {
        public string Name { get; set; }
        public uint Index { get; set; }
        public uint Size { get; set; }
    }

    public class AbogadoPftDsk
    {
        private const int BlockSize = 2048;

        public static List<PftEntry> ReadPft(string pftPath)
        {
            var entries = new List<PftEntry>();
            using (var fs = new FileStream(pftPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // Header is 16 bytes
                if (fs.Length < 16) return entries;
                fs.Seek(16, SeekOrigin.Begin);

                while (fs.Position + 16 <= fs.Length)
                {
                    byte[] nameBytes = reader.ReadBytes(8);
                    string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    if (string.IsNullOrEmpty(name)) break;

                    uint index = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

                    entries.Add(new PftEntry { Name = name, Index = index, Size = size });
                }
            }
            return entries;
        }

        public static void WritePft(string pftPath, List<PftEntry> entries, byte[] originalHeader = null)
        {
            using (var fs = new FileStream(pftPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                if (originalHeader != null && originalHeader.Length >= 16)
                {
                    byte[] header = (byte[])originalHeader.Clone();
                    BitConverter.GetBytes(entries.Count).CopyTo(header, 4);
                    writer.Write(header, 0, 16);
                }
                else
                {
                    writer.Write((uint)0x08000010);
                    writer.Write((uint)entries.Count);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(new byte[4]);
                }

                foreach (var entry in entries)
                {
                    byte[] nameBytes = new byte[8];
                    byte[] strBytes = Encoding.ASCII.GetBytes(entry.Name);
                    Array.Copy(strBytes, nameBytes, Math.Min(strBytes.Length, 8));
                    writer.Write(nameBytes);
                    writer.Write(entry.Index);
                    writer.Write(entry.Size);
                }
            }
        }

        public static void UnpackDsk(string dskPath, string pftPath, string outputDir, Action<string> log)
        {
            var entries = ReadPft(pftPath);
            log($"[*] Reading index: {pftPath}");
            log($"[*] Total scenes: {entries.Count}");

            Directory.CreateDirectory(outputDir);

            using (var fs = new FileStream(dskPath, FileMode.Open, FileAccess.Read))
            {
                int extracted = 0;
                foreach (var entry in entries)
                {
                    if (entry.Index * BlockSize < fs.Length)
                    {
                        byte[] data = new byte[entry.Size];
                        fs.Seek(entry.Index * BlockSize, SeekOrigin.Begin);
                        fs.Read(data, 0, (int)entry.Size);

                        string ext = ".SCF";
                        if (data.Length >= 2 && data[0] == 'K' && data[1] == 'G')
                        {
                            ext = ".KG";
                        }

                        string outPath = Path.Combine(outputDir, $"{entry.Name}{ext}");
                        File.WriteAllBytes(outPath, data);
                        log($"[+] Extracted: {entry.Name}{ext} (size={entry.Size})");
                        extracted++;
                    }
                    else
                    {
                        log($"[!] Error: {entry.Name} offset out of range");
                    }
                }
                log($"\n[*] Successfully extracted {extracted}/{entries.Count} files.");
            }
        }

        /// <summary>
        /// Find a file in a folder by base name, ignoring extension.
        /// Matches ArcPACK.py find_file_in_folder() logic.
        /// </summary>
        private static string? FindFileByBaseName(string inputDir, string baseName)
        {
            string target = baseName.ToUpperInvariant();
            foreach (string f in Directory.GetFiles(inputDir))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(f).ToUpperInvariant();
                if (nameNoExt == target)
                    return f;
            }
            return null;
        }

        public static void RepackDsk(string inputDir, string originalPft, string originalDsk, string outputDir, Action<string> log)
        {
            var originalEntries = ReadPft(originalPft);
            byte[] origHeader = new byte[16];
            using (var fs = new FileStream(originalPft, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length >= 16) fs.Read(origHeader, 0, 16);
            }

            string outPft = Path.Combine(outputDir, Path.GetFileName(originalPft));
            string outDsk = Path.Combine(outputDir, Path.GetFileName(originalDsk));
            
            Directory.CreateDirectory(outputDir);
            
            var newEntries = new List<PftEntry>();
            uint currentBlock = 1;

            using (var origDskFs = new FileStream(originalDsk, FileMode.Open, FileAccess.Read))
            using (var dskFs = new FileStream(outDsk, FileMode.Create, FileAccess.Write))
            using (var dskWriter = new BinaryWriter(dskFs))
            {
                // Pad block 0 (first 2048 bytes usually empty)
                dskWriter.Write(new byte[BlockSize]);

                int repacked = 0;
                int copied = 0;
                foreach (var entry in originalEntries)
                {
                    // Search by base name, ANY extension
                    string? filePath = FindFileByBaseName(inputDir, entry.Name);
                    byte[] fileData;
                    string sourceLog;

                    if (filePath != null)
                    {
                        // Use modified file from the input folder
                        fileData = File.ReadAllBytes(filePath);
                        sourceLog = $"Modified -> {Path.GetFileName(filePath)}";
                        repacked++;
                    }
                    else
                    {
                        // File not in folder: Copy exactly from the original DSK!
                        if (entry.Size == 0)
                        {
                            fileData = Array.Empty<byte>();
                            sourceLog = "Original -> (Empty)";
                        }
                        else
                        {
                            fileData = new byte[entry.Size];
                            origDskFs.Seek(entry.Index * BlockSize, SeekOrigin.Begin);
                            origDskFs.Read(fileData, 0, (int)entry.Size);
                            sourceLog = $"Original -> {entry.Name}";
                        }
                        copied++;
                    }

                    newEntries.Add(new PftEntry
                    {
                        Name = entry.Name,
                        Index = currentBlock,
                        Size = (uint)fileData.Length
                    });

                    dskWriter.Write(fileData);
                    
                    uint blocksNeeded = ((uint)fileData.Length + BlockSize - 1) / (uint)BlockSize;
                    uint paddedSize = blocksNeeded * BlockSize;
                    uint padding = paddedSize - (uint)fileData.Length;
                    
                    if (padding > 0)
                        dskWriter.Write(new byte[padding]);
                    
                    currentBlock += blocksNeeded;
                    
                    // Only log the modified ones to avoid console spam (there could be 10,000 original files)
                    if (filePath != null)
                        log($"[+] {sourceLog} (block={newEntries[^1].Index} size={fileData.Length})");
                }
                log($"\n[*] Done: {repacked} modified files injected, {copied} original files copied.");
                log($"[*] Output DSK: {outDsk}");
            }
            
            WritePft(outPft, newEntries, origHeader);
            log($"[*] Output PFT: {outPft}");
        }
        
        public static void VerifyDsk(string pftPath, string dskPath, Action<string> log)
        {
            var entries = ReadPft(pftPath);
            log($"[*] Verifying {entries.Count} scenes...");
            
            int errors = 0;
            using (var fs = new FileStream(dskPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var entry in entries)
                {
                    long offset = (long)entry.Index * BlockSize;
                    if (offset + entry.Size > fs.Length)
                    {
                        log($"[!] ERROR: {entry.Name} offset out of bounds! (Index: {entry.Index}, Size: {entry.Size})");
                        errors++;
                    }
                }
            }
            
            if (errors == 0)
                log("[OK] Verification passed! No out of bounds entries found.");
            else
                log($"[FAIL] Verification failed with {errors} errors.");
        }
    }
}
