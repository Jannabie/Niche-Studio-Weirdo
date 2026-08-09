using System;
using System.IO;
using System.Text;

namespace NicheStudioWeirdo.Utils
{
    public static class SjrFbinTool
    {
        public static void ExtractFbin(string binPath, Action<string> logCallback)
        {
            if (!File.Exists(binPath)) throw new FileNotFoundException("File not found", binPath);
            
            byte[] data = File.ReadAllBytes(binPath);
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                if (data.Length < 96) throw new Exception("File is too small to be FBIN.");
                
                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (magic != "FBIN") throw new Exception("Not a valid FBIN file. Magic mismatch.");
                
                br.BaseStream.Position = 8;
                int headerSize = br.ReadInt32(); // 64
                if (headerSize != 64) logCallback("Warning: FBIN header size is not 64.");
                
                br.BaseStream.Position = headerSize;
                string innerMagic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (innerMagic != "TBB1") throw new Exception("No TBB1 signature found inside FBIN.");
                
                br.BaseStream.Position = headerSize + 8;
                int fileCount = br.ReadInt32();
                if (fileCount != 1) throw new Exception($"Unsupported file count in TBB1. Expected 1, found {fileCount}");
                
                br.BaseStream.Position = headerSize + 0x10;
                int offset = br.ReadInt32(); // 32
                
                int fileStart = headerSize + offset;
                if (fileStart >= data.Length) throw new Exception("Invalid TBB1 offset.");
                
                int fileSize = data.Length - fileStart;
                
                // Extract Header
                byte[] headerBytes = new byte[fileStart];
                Array.Copy(data, 0, headerBytes, 0, fileStart);
                
                // Extract MBM
                byte[] mbmBytes = new byte[fileSize];
                Array.Copy(data, fileStart, mbmBytes, 0, fileSize);
                
                string baseDir = Path.GetDirectoryName(binPath) ?? "";
                string baseName = Path.GetFileNameWithoutExtension(binPath);
                
                string headerPath = Path.Combine(baseDir, baseName + ".header");
                string mbmPath = Path.Combine(baseDir, baseName + ".mbm");
                
                File.WriteAllBytes(headerPath, headerBytes);
                File.WriteAllBytes(mbmPath, mbmBytes);
                
                logCallback($"Extracted: {Path.GetFileName(mbmPath)} ({mbmBytes.Length} bytes)");
            }
        }

        public static void RepackFbin(string mbmPath, Action<string> logCallback)
        {
            if (!File.Exists(mbmPath)) throw new FileNotFoundException("MBM file not found", mbmPath);
            
            string baseDir = Path.GetDirectoryName(mbmPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(mbmPath);
            if (mbmPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) {
                if (baseName.EndsWith(".mbm", StringComparison.OrdinalIgnoreCase)) {
                    baseName = Path.GetFileNameWithoutExtension(baseName);
                } else {
                    baseName = Path.GetFileNameWithoutExtension(mbmPath);
                }
            }
            
            string headerPath = Path.Combine(baseDir, baseName + ".header");
            if (!File.Exists(headerPath)) {
                throw new FileNotFoundException($"Cannot find blueprint '{Path.GetFileName(headerPath)}'. Please extract the FBIN first.", headerPath);
            }
            
            byte[] headerBytes = File.ReadAllBytes(headerPath);
            byte[] mbmBytes = File.ReadAllBytes(mbmPath);
            
            byte[] repacked = new byte[headerBytes.Length + mbmBytes.Length];
            Array.Copy(headerBytes, 0, repacked, 0, headerBytes.Length);
            Array.Copy(mbmBytes, 0, repacked, headerBytes.Length, mbmBytes.Length);
            
            string outBin = Path.Combine(baseDir, baseName + "_repack.bin");
            File.WriteAllBytes(outBin, repacked);
            
            logCallback($"Repack successful: {Path.GetFileName(outBin)}");
        }
        
        public static void ProcessDirectoryExtract(string dirPath, Action<string> logCallback)
        {
            var files = Directory.GetFiles(dirPath, "*.bin");
            int count = 0;
            foreach(var file in files) {
                if (file.EndsWith("_repack.bin", StringComparison.OrdinalIgnoreCase)) continue;
                try {
                    ExtractFbin(file, (msg) => { });
                    count++;
                } catch {
                    // ignore non-FBIN bins like ControlRoom01.bin
                }
            }
            logCallback($"Successfully extracted {count} FBIN files in folder.");
        }

        public static void ProcessDirectoryRepack(string dirPath, Action<string> logCallback)
        {
            var files = Directory.GetFiles(dirPath, "*.mbm");
            int count = 0;
            foreach(var file in files) {
                try {
                    RepackFbin(file, (msg) => { });
                    count++;
                } catch {
                }
            }
            logCallback($"Successfully repacked {count} MBM files back to FBIN.");
        }
    }
}
