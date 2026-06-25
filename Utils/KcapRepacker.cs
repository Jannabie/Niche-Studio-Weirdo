using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicheStudioWeirdo.Utils
{
    public class KcapRepacker
    {
        public static async Task RepackAsync(string inputFolder, string outputPak, Action<string> logCallback)
        {
            await Task.Run(() =>
            {
                try
                {
                    logCallback($"WA2 KCAP Repacker (Native C#) - Scanning: {inputFolder}");

                    if (!Directory.Exists(inputFolder))
                    {
                        logCallback($"[ERROR] Input folder not found: {inputFolder}");
                        return;
                    }

                    var files = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);
                    if (files.Length == 0)
                    {
                        logCallback("[ERROR] No files to pack!");
                        return;
                    }

                    // Force Shift-JIS encoding provider
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    Encoding shiftJis = Encoding.GetEncoding(932); // Shift-JIS

                    int headerSize = 16;
                    int entrySize = 44;
                    int entriesTotalSize = entrySize * files.Length;
                    int dataOffset = headerSize + entriesTotalSize;

                    logCallback($"Found {files.Length} files.");
                    logCallback($"Header size: {headerSize} bytes, Entries size: {entriesTotalSize} bytes");
                    logCallback($"Building archive: {outputPak}");

                    using (var fs = new FileStream(outputPak, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new BinaryWriter(fs))
                    {
                        // Write Header
                        writer.Write(Encoding.ASCII.GetBytes("KCAP"));
                        writer.Write((uint)0); // unknown1
                        writer.Write((uint)0); // unknown2 (v2)
                        writer.Write((uint)files.Length); // entry_count

                        int currentOffset = dataOffset;

                        // Write Entries
                        foreach (var file in files.OrderBy(f => f)) // Sort alphabetically
                        {
                            var fi = new FileInfo(file);
                            string relativePath = file.Substring(inputFolder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            string filename = relativePath.Replace('\\', '/');

                            writer.Write((uint)0); // compressed_flag

                            // Filename 24 bytes (Shift-JIS)
                            byte[] nameBytes;
                            try
                            {
                                nameBytes = shiftJis.GetBytes(filename);
                            }
                            catch
                            {
                                nameBytes = Encoding.ASCII.GetBytes(filename);
                            }

                            if (nameBytes.Length > 23)
                            {
                                logCallback($"WARNING: Filename too long, truncating: {filename}");
                                Array.Resize(ref nameBytes, 23);
                            }

                            byte[] paddedName = new byte[24];
                            Array.Copy(nameBytes, paddedName, nameBytes.Length);
                            writer.Write(paddedName);

                            writer.Write((uint)0); // unknown1
                            writer.Write((uint)0); // unknown2
                            writer.Write((uint)currentOffset);
                            writer.Write((uint)fi.Length);

                            currentOffset += (int)fi.Length;
                        }

                        // Write File Data
                        for (int i = 0; i < files.Length; i++)
                        {
                            var file = files.OrderBy(f => f).ElementAt(i);
                            logCallback($"Packing [{i + 1}/{files.Length}]: {Path.GetFileName(file)}");
                            
                            byte[] data = File.ReadAllBytes(file);
                            writer.Write(data);
                        }
                    }

                    long finalSize = new FileInfo(outputPak).Length;
                    logCallback("============================================================");
                    logCallback("✓ SUCCESS! PAK file created with Shift-JIS encoding!");
                    logCallback($"Output: {outputPak}");
                    logCallback($"Size: {finalSize} bytes ({finalSize / 1024.0 / 1024.0:F2} MB)");
                    logCallback("============================================================");
                }
                catch (Exception ex)
                {
                    logCallback($"[ERROR] Failed to create PAK file: {ex.Message}");
                    logCallback(ex.StackTrace);
                }
            });
        }
    }
}
