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

        public static void ProcessDirectoryExtract(string dirPath, Action<string> logCallback)
        {
            var files = Directory.GetFiles(dirPath, "*.*").Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".mbm", StringComparison.OrdinalIgnoreCase)).ToArray();
            int count = 0;
            foreach(var file in files) {
                if (file.EndsWith("_repack.bin", StringComparison.OrdinalIgnoreCase) || file.EndsWith("_repack.mbm", StringComparison.OrdinalIgnoreCase)) continue;
                try {
                    ExtractArchive(file, (msg) => { });
                    count++;
                } catch { }
            }
            logCallback($"Successfully extracted {count} archives in folder.");
        }

        public static void ProcessDirectoryRepack(string dirPath, Action<string> logCallback)
        {
            int count = 0;
            if (File.Exists(Path.Combine(dirPath, "_blueprint.json"))) {
                RepackArchive(dirPath, logCallback);
                count = 1;
            } else {
                var dirs = Directory.GetDirectories(dirPath, "*_unpacked");
                foreach(var dir in dirs) {
                    try {
                        RepackArchive(dir, (msg) => { });
                        count++;
                    } catch { }
                }
            }
            logCallback($"Successfully repacked {count} unpacked folders back to archives.");
        }
    }
}
