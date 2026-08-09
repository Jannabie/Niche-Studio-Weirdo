using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace NicheStudioWeirdo.Utils
{
    public class SjrArchiveBlueprint
    {
        public string Type { get; set; } = "";
        public int HeaderSize { get; set; }
        public List<string> Files { get; set; } = new List<string>();
    }

    public static class SjrFbinTool
    {
        public static void ExtractArchive(string filePath, Action<string> logCallback)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);
            byte[] data = File.ReadAllBytes(filePath);
            
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                if (data.Length < 16) throw new Exception("File is too small.");
                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                
                string baseDir = Path.GetDirectoryName(filePath) ?? "";
                string baseName = Path.GetFileNameWithoutExtension(filePath);
                string outDir = Path.Combine(baseDir, baseName + "_unpacked");
                Directory.CreateDirectory(outDir);
                
                var blueprint = new SjrArchiveBlueprint { Type = magic };
                
                if (magic == "FBIN")
                {
                    int fileCount = br.ReadInt32();
                    int headerSize = br.ReadInt32();
                    blueprint.HeaderSize = headerSize;
                    
                    int currentOffset = headerSize;
                    for (int i = 0; i < fileCount; i++)
                    {
                        ms.Position = 12 + (i * 4);
                        int fileSize = br.ReadInt32();
                        
                        byte[] fileData = new byte[fileSize];
                        Array.Copy(data, currentOffset, fileData, 0, fileSize);
                        currentOffset += fileSize;
                        
                        string ext = ".bin";
                        if (fileSize >= 8) {
                            string magic0 = Encoding.ASCII.GetString(fileData, 0, 4);
                            string magic4 = Encoding.ASCII.GetString(fileData, 4, 4);
                            if (magic0 == "TBB1" || magic4 == "TBB1") ext = ".tbb1";
                            else if (magic0 == "MSG2" || magic4 == "MSG2") ext = ".mbm";
                        }
                        
                        string fileName = $"{i:D3}{ext}";
                        blueprint.Files.Add(fileName);
                        File.WriteAllBytes(Path.Combine(outDir, fileName), fileData);
                    }
                    logCallback($"Extracted FBIN: {fileCount} files to {outDir}");
                }
                else if (magic == "TBB1")
                {
                    int offsetToOffsets = br.ReadInt32();
                    int fileCount = br.ReadInt32();
                    int totalSize = br.ReadInt32();
                    blueprint.HeaderSize = offsetToOffsets;
                    
                    var offsets = new List<int>();
                    ms.Position = offsetToOffsets;
                    for (int i = 0; i < fileCount; i++) {
                        offsets.Add(br.ReadInt32());
                    }
                    
                    for (int i = 0; i < fileCount; i++)
                    {
                        int fileStart = offsets[i];
                        int fileSize = (i < fileCount - 1) ? offsets[i + 1] - fileStart : totalSize - fileStart;
                        
                        byte[] fileData = new byte[fileSize];
                        Array.Copy(data, fileStart, fileData, 0, fileSize);
                        
                        string ext = ".bin";
                        if (fileSize >= 8) {
                            string magic0 = Encoding.ASCII.GetString(fileData, 0, 4);
                            string magic4 = Encoding.ASCII.GetString(fileData, 4, 4);
                            if (magic0 == "TBB1" || magic4 == "TBB1") ext = ".tbb1";
                            else if (magic0 == "MSG2" || magic4 == "MSG2") ext = ".mbm";
                        }
                        
                        string fileName = $"{i:D3}{ext}";
                        blueprint.Files.Add(fileName);
                        File.WriteAllBytes(Path.Combine(outDir, fileName), fileData);
                    }
                    logCallback($"Extracted TBB1: {fileCount} files to {outDir}");
                }
                else
                {
                    throw new Exception("Not a valid FBIN or TBB1 archive. Magic: " + magic);
                }
                
                string json = JsonSerializer.Serialize(blueprint, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(outDir, "_blueprint.json"), json);
            }
        }

        public static void RepackArchive(string folderPath, Action<string> logCallback)
        {
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException("Folder not found");
            string blueprintPath = Path.Combine(folderPath, "_blueprint.json");
            if (!File.Exists(blueprintPath)) throw new Exception("Blueprint not found. Select a folder that ends with _unpacked");
            
            var blueprint = JsonSerializer.Deserialize<SjrArchiveBlueprint>(File.ReadAllText(blueprintPath));
            if (blueprint == null) throw new Exception("Failed to read blueprint");
            
            string baseDir = Directory.GetParent(folderPath)?.FullName ?? "";
            string dirName = new DirectoryInfo(folderPath).Name;
            string baseName = dirName.EndsWith("_unpacked") ? dirName.Substring(0, dirName.Length - 9) : dirName;
            
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                if (blueprint.Type == "FBIN")
                {
                    bw.Write(Encoding.ASCII.GetBytes("FBIN"));
                    bw.Write(blueprint.Files.Count);
                    bw.Write(blueprint.HeaderSize);
                    
                    long sizesOffset = ms.Position;
                    for (int i = 0; i < blueprint.Files.Count; i++) bw.Write(0);
                    
                    while (ms.Position < blueprint.HeaderSize) bw.Write((byte)0);
                    
                    var sizes = new List<int>();
                    foreach (var file in blueprint.Files)
                    {
                        byte[] fData = File.ReadAllBytes(Path.Combine(folderPath, file));
                        sizes.Add(fData.Length);
                        bw.Write(fData);
                    }
                    
                    ms.Position = sizesOffset;
                    foreach(var size in sizes) bw.Write(size);
                    
                    string outPath = Path.Combine(baseDir, baseName + "_repack.bin");
                    File.WriteAllBytes(outPath, ms.ToArray());
                    logCallback($"Repacked FBIN: {Path.GetFileName(outPath)}");
                }
                else if (blueprint.Type == "TBB1")
                {
                    bw.Write(Encoding.ASCII.GetBytes("TBB1"));
                    bw.Write(blueprint.HeaderSize);
                    bw.Write(blueprint.Files.Count);
                    long totalSizeOffset = ms.Position;
                    bw.Write(0);
                    
                    while (ms.Position < blueprint.HeaderSize) bw.Write((byte)0);
                    
                    long offsetsTablePos = ms.Position;
                    for (int i = 0; i < blueprint.Files.Count; i++) bw.Write(0);
                    
                    long padding = (ms.Position % 16 == 0) ? 0 : 16 - (ms.Position % 16);
                    for (int i = 0; i < padding; i++) bw.Write((byte)0);
                    
                    var offsets = new List<int>();
                    foreach (var file in blueprint.Files)
                    {
                        // Add padding to align each file to a 16-byte boundary
                        long paddingInner = (ms.Position % 16 == 0) ? 0 : 16 - (ms.Position % 16);
                        for (int i = 0; i < paddingInner; i++) bw.Write((byte)0);

                        offsets.Add((int)ms.Position);
                        byte[] fData = File.ReadAllBytes(Path.Combine(folderPath, file));
                        bw.Write(fData);
                    }
                    int totalSize = (int)ms.Position;
                    
                    ms.Position = offsetsTablePos;
                    foreach (var off in offsets) bw.Write(off);
                    
                    ms.Position = totalSizeOffset;
                    bw.Write(totalSize);
                    
                    string outPath = Path.Combine(baseDir, baseName + "_repack.mbm");
                    File.WriteAllBytes(outPath, ms.ToArray());
                    logCallback($"Repacked TBB1: {Path.GetFileName(outPath)}");
                }
            }
        }

        public static void DeepExtract(string filePath, Action<string> logCallback)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);
            
            // Extract this archive
            ExtractArchive(filePath, logCallback);
            
            // Find the _unpacked folder we just created
            string baseDir = Path.GetDirectoryName(filePath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(filePath);
            string outDir = Path.Combine(baseDir, baseName + "_unpacked");
            
            if (!Directory.Exists(outDir)) return;
            
            // Recursively extract any .tbb1 or nested archives inside
            foreach (var childFile in Directory.GetFiles(outDir))
            {
                string ext = Path.GetExtension(childFile).ToLowerInvariant();
                if (ext == ".tbb1" || ext == ".mbm")
                {
                    // Check if it's actually an archive (TBB1/FBIN), not a raw MBM
                    try
                    {
                        byte[] header = new byte[8];
                        using (var fs = new FileStream(childFile, FileMode.Open, FileAccess.Read))
                        { fs.Read(header, 0, Math.Min(8, (int)fs.Length)); }
                        
                        string m0 = Encoding.ASCII.GetString(header, 0, 4);
                        string m4 = header.Length >= 8 ? Encoding.ASCII.GetString(header, 4, 4) : "";
                        
                        if (m0 == "TBB1" || m0 == "FBIN" || m4 == "TBB1" || m4 == "FBIN")
                        {
                            DeepExtract(childFile, logCallback);
                        }
                    }
                    catch { }
                }
            }
        }

        public static void DeepRepack(string folderPath, Action<string> logCallback)
        {
            if (folderPath.EndsWith("\\") || folderPath.EndsWith("/")) folderPath = folderPath.TrimEnd('\\', '/');
            if (!folderPath.EndsWith("_unpacked", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Please select a valid folder ending in '_unpacked'.");

            if (!Directory.Exists(folderPath))
                throw new Exception($"Directory does not exist: {folderPath}");

            // First, recursively repack any child _unpacked folders inside
            foreach (var childDir in Directory.GetDirectories(folderPath, "*_unpacked"))
            {
                DeepRepack(childDir, logCallback);
            }
            
            // Now repack this top-level folder
            RepackArchive(folderPath, logCallback);

            string baseDir = Directory.GetParent(folderPath)?.FullName ?? "";
            string dirName = new DirectoryInfo(folderPath).Name;
            string baseName = dirName.Substring(0, dirName.Length - 9);

            // Find the newly repacked file
            string repackedFile = Directory.GetFiles(baseDir)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith("_repack") && f.Contains(baseName)) ?? "";
            
            if (string.IsNullOrEmpty(repackedFile)) return;

            // Find the original file it should replace
            string originalFile = Directory.GetFiles(baseDir)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == baseName) ?? "";
            
            if (!string.IsNullOrEmpty(originalFile))
            {
                File.Copy(repackedFile, originalFile, true);
                File.Delete(repackedFile);
                logCallback($"✔ Updated original file: {Path.GetFileName(originalFile)}");
            }
            else 
            {
                logCallback($"✔ Repacked to: {Path.GetFileName(repackedFile)} (Could not find original file to overwrite)");
            }
        }
    }
}
