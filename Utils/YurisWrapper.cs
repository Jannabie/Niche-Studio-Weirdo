using System;
using System.IO;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;

/// <summary>
/// Native C# implementation of YU-RIS engine tools.
/// Ported from fengberd/YuRISTools (MIT License).
/// Covers: YPF unpack/pack, YSTB XOR cipher, YSTB text export/import.
/// </summary>
namespace NicheStudioWeirdo.Utils
{
    // ─────────────────────────────────────────────────────────────────────────
    // CHECKSUM HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    internal static class YurisCheckSum
    {
        private static readonly uint[] _crc32Table = new uint[256];

        static YurisCheckSum()
        {
            for (uint i = 0; i < _crc32Table.Length; i++)
            {
                uint k = i;
                for (int j = 8; j > 0; --j)
                    k = (k & 1) == 1 ? (k >> 1) ^ 0xEDB88320 : k >> 1;
                _crc32Table[i] = k;
            }
        }

        public static uint CRC32(byte[] data)
        {
            uint crc = 0xffffffff;
            foreach (byte b in data)
                crc = (crc >> 8) ^ _crc32Table[(byte)((crc & 0xff) ^ b)];
            return ~crc;
        }

        public static uint Adler32(byte[] data)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (byte c in data) { a = (a + c) % mod; b = (b + a) % mod; }
            return (b << 16) | a;
        }

        public static uint MurmurHash2(byte[] data)
        {
            const uint m = 0x5bd1e995;
            const int r = 24;
            int len = data.Length;
            uint h = (uint)len;
            int idx = 0;
            while (len >= 4)
            {
                uint k = BitConverter.ToUInt32(data, idx);
                k *= m; k ^= k >> r; k *= m;
                h *= m; h ^= k;
                idx += 4; len -= 4;
            }
            if (len > 2) h ^= (uint)(data[idx + 2] << 16);
            if (len > 1) h ^= (uint)(data[idx + 1] << 8);
            if (len > 0) { h ^= data[idx]; h *= m; }
            h ^= h >> 13; h *= m; h ^= h >> 15;
            return h;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YPF PACK / UNPACK
    // ─────────────────────────────────────────────────────────────────────────

    public static class YurisYpf
    {

        /// <summary>
        /// Extract all files from a .ypf archive to the given output directory.
        /// Returns number of files extracted.
        /// </summary>
        public static int Extract(string ypfPath, string outputDir, Action<string> log = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var shiftJis = Encoding.GetEncoding("SHIFT-JIS");

            using var fs = File.OpenRead(ypfPath);
            using var reader = new BinaryReader(fs);

            var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "YPF\0")
                throw new InvalidDataException("Not a valid YPF file (bad magic).");

            int version = reader.ReadInt32();
            int count   = reader.ReadInt32();
            reader.BaseStream.Position = 32;

            var names   = new List<string>();
            var entries = new List<(bool compressed, int size, int compSize, long offset, uint hash)>();

            for (int i = 0; i < count; i++)
            {
                uint   nameHash  = reader.ReadUInt32();
                byte   rawByte   = reader.ReadByte();
                // Decode length: rawByte = ~tableIndex, so tableIndex = ~rawByte
                // Then table[tableIndex] = actual byte length
                byte[] extractTable = new byte[] {
                    0x00,0x01,0x02,0x48,0x04,0x05,0x35,0x07,0x08,0x0B,0x0A,0x09,0x10,0x13,0x0E,0x0F,
                    0x0C,0x19,0x12,0x0D,0x14,0x1B,0x16,0x17,0x18,0x11,0x1A,0x15,0x1E,0x1D,0x1C,0x1F,
                    0x23,0x21,0x22,0x20,0x24,0x25,0x29,0x27,0x28,0x26,0x2A,0x2B,0x2F,0x2D,0x32,0x2C,
                    0x30,0x31,0x2E,0x33,0x34,0x06,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,
                    0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x03,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,0x4F,
                    0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x5B,0x5C,0x5D,0x5E,0x5F,
                    0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6A,0x6B,0x6C,0x6D,0x6E,0x6F,
                    0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x7B,0x7C,0x7D,0x7E,0x7F,
                    0x80,0x81,0x82,0x83,0x84,0x85,0x86,0x87,0x88,0x89,0x8A,0x8B,0x8C,0x8D,0x8E,0x8F,
                    0x90,0x91,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9A,0x9B,0x9C,0x9D,0x9E,0x9F,
                    0xA0,0xA1,0xA2,0xA3,0xA4,0xA5,0xA6,0xA7,0xA8,0xA9,0xAA,0xAB,0xAC,0xAD,0xAE,0xAF,
                    0xB0,0xB1,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,
                    0xC0,0xC1,0xC2,0xC3,0xC4,0xC5,0xC6,0xC7,0xC8,0xC9,0xCA,0xCB,0xCC,0xCD,0xCE,0xCF,
                    0xD0,0xD1,0xD2,0xD3,0xD4,0xD5,0xD6,0xD7,0xD8,0xD9,0xDA,0xDB,0xDC,0xDD,0xDE,0xDF,
                    0xE0,0xE1,0xE2,0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xEB,0xEC,0xED,0xEE,0xEF,
                    0xF0,0xF1,0xF2,0xF3,0xF4,0xF5,0xF6,0xF7,0xF8,0xF9,0xFA,0xFB,0xFC,0xFD,0xFE,0xFF
                };
                int    rawLen    = extractTable[(byte)~rawByte];
                // NOT-decode the name bytes first, then decode as ASCII.
                // IMPORTANT: do NOT use Shift-JIS here — in Shift-JIS, 0x5C is '¥' not '\',
                // which corrupts the path separator. YPF names are always ASCII paths.
                byte[] nameBytes = reader.ReadBytes(rawLen).Select(c => (byte)~c).ToArray();
                string name      = Encoding.ASCII.GetString(nameBytes);

                reader.ReadByte(); // resource type
                bool compressed = reader.ReadByte() != 0;
                int  size       = reader.ReadInt32();
                int  compSize   = reader.ReadInt32();
                // v479+ uses 8-byte offset
                long offset     = version >= 479 ? reader.ReadInt64() : reader.ReadInt32();
                uint dataHash   = version >= 473 ? reader.ReadUInt32() : 0;

                names.Add(name);
                entries.Add((compressed, size, compSize, offset, dataHash));
            }

            Directory.CreateDirectory(outputDir);
            int extracted = 0;

            for (int i = 0; i < count; i++)
            {
                var (compressed, size, compSize, offset, _) = entries[i];
                string name = names[i];

                reader.BaseStream.Position = offset;
                byte[] raw = reader.ReadBytes(compSize);

                byte[] data;
                if (compressed)
                {
                    using var ms    = new MemoryStream();
                    using var input = new MemoryStream(raw);
                    input.Position  = 2; // skip zlib header 0x78 0xDA
                    using var deflate = new DeflateStream(input, CompressionMode.Decompress, false);
                    deflate.CopyTo(ms);
                    data = ms.ToArray();
                }
                else
                {
                    data = raw;
                }

                // Preserve subfolder structure (names use backslash)
                string outPath = Path.Combine(outputDir, name.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                File.WriteAllBytes(outPath, data);
                extracted++;
                log?.Invoke($"[{i+1}/{count}] {name}");
            }

            return extracted;
        }

        public static void Pack(string inputDir, string outputYpf, int engineVersion = 479, bool useCrc32 = false, Action<string> log = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var shiftJis = Encoding.GetEncoding(932); // Shift-JIS

            // Pack everything under inputDir as the archive root.
            // Full relative paths (e.g. ysbin\yscfg.ybn) are preserved exactly.
            string packRoot = inputDir;

            // --- LengthSwappingTable (full 256-byte table, exactly from ypf-repacker) ---
            // For version < 500 (our game is 481):
            byte[] lengthSwappingTable = new byte[] {
                0x00,0x01,0x02,0x48,0x04,0x05,0x35,0x07,0x08,0x0B,0x0A,0x09,0x10,0x13,0x0E,0x0F,
                0x0C,0x19,0x12,0x0D,0x14,0x1B,0x16,0x17,0x18,0x11,0x1A,0x15,0x1E,0x1D,0x1C,0x1F,
                0x23,0x21,0x22,0x20,0x24,0x25,0x29,0x27,0x28,0x26,0x2A,0x2B,0x2F,0x2D,0x32,0x2C,
                0x30,0x31,0x2E,0x33,0x34,0x06,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,
                0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x03,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,0x4F,
                0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x5B,0x5C,0x5D,0x5E,0x5F,
                0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6A,0x6B,0x6C,0x6D,0x6E,0x6F,
                0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x7B,0x7C,0x7D,0x7E,0x7F,
                0x80,0x81,0x82,0x83,0x84,0x85,0x86,0x87,0x88,0x89,0x8A,0x8B,0x8C,0x8D,0x8E,0x8F,
                0x90,0x91,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9A,0x9B,0x9C,0x9D,0x9E,0x9F,
                0xA0,0xA1,0xA2,0xA3,0xA4,0xA5,0xA6,0xA7,0xA8,0xA9,0xAA,0xAB,0xAC,0xAD,0xAE,0xAF,
                0xB0,0xB1,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,
                0xC0,0xC1,0xC2,0xC3,0xC4,0xC5,0xC6,0xC7,0xC8,0xC9,0xCA,0xCB,0xCC,0xCD,0xCE,0xCF,
                0xD0,0xD1,0xD2,0xD3,0xD4,0xD5,0xD6,0xD7,0xD8,0xD9,0xDA,0xDB,0xDC,0xDD,0xDE,0xDF,
                0xE0,0xE1,0xE2,0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xEB,0xEC,0xED,0xEE,0xEF,
                0xF0,0xF1,0xF2,0xF3,0xF4,0xF5,0xF6,0xF7,0xF8,0xF9,0xFA,0xFB,0xFC,0xFD,0xFE,0xFF
            };

            // Encryption key: 0x00 for version < 500
            byte fileNameEncryptionKey = (byte)(engineVersion >= 500 ? 0x36 : engineVersion == 290 ? 0x40 : 0x00);

            // Checksum selection
            bool isOldHash = useCrc32 || engineVersion < 479;
            Func<byte[], uint> nameHash = isOldHash ? YurisCheckSum.CRC32   : YurisCheckSum.MurmurHash2;
            Func<byte[], uint> dataHash = isOldHash ? YurisCheckSum.Adler32 : YurisCheckSum.MurmurHash2;

            // Gather files
            var allFiles = Directory.GetFiles(packRoot, "*", SearchOption.AllDirectories)
                                    .Where(f => !f.Contains("script_extracted"))
                                    .ToArray();

            // Build entries — sort by name hash (engine uses binary search)
            var entries = allFiles.Select(file =>
            {
                string relPath    = Path.GetRelativePath(packRoot, file).Replace(Path.DirectorySeparatorChar, '\\');
                byte[] rawEncoded = shiftJis.GetBytes(relPath);

                // Encode name bytes: ~(b ^ key) — for key=0x00 this is just ~b
                byte[] encodedName = rawEncoded.Select(b => (byte)(~(b ^ fileNameEncryptionKey))).ToArray();

                // CRITICAL FIX: Name hash MUST be computed from RAW Shift-JIS bytes,
                // NOT from the encoded (inverted) bytes. The engine hashes the plain
                // filename string when looking up files at runtime. Using the encoded
                // bytes produces wrong hashes → engine cannot find any file → crash.
                uint hash = nameHash(rawEncoded);

                return new { file, relPath, encodedName, hash };
            }).OrderBy(e => e.hash).ToList();

            // Calculate total header size: 32 (fixed) + per-entry sizes
            // Per entry: 4(hash) + 1(lenByte) + encodedName.Length + 1(type) + 1(comp) + 4(rawSize) + 4(compSize) + 8(offset for v>=479) + 4(dataHash)
            int headerSize = 32;
            foreach (var e in entries)
                headerSize += 4 + 1 + e.encodedName.Length + 1 + 1 + 4 + 4 + (engineVersion >= 479 ? 8 : 4) + 4;

            using var fs     = File.Create(outputYpf);
            using var writer = new BinaryWriter(fs);

            // Write fixed header section (32 bytes total)
            writer.Write(new byte[] { 0x59, 0x50, 0x46, 0x00 }); // YPF\0
            writer.Write(engineVersion);
            writer.Write(entries.Count);
            writer.Write(headerSize);  // byte 12: where file data starts (after all directory entries)
            writer.Write(0L);          // bytes 16-23: zero padding
            writer.Write(0L);          // bytes 24-31: zero padding

            // Jump to data section — write all file data first
            fs.Position = headerSize;

            var fileDataOffsets   = new List<long>();
            var fileDataCompSizes = new List<int>();
            var fileDataRawSizes  = new List<int>();
            var fileDataHashes    = new List<uint>();
            var fileDataIsComp    = new List<bool>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry  = entries[i];
                byte[] raw = File.ReadAllBytes(entry.file);

                // Only compress file types that benefit from zlib compression.
                // Audio/image files are already compressed internally and should
                // be stored as-is to avoid wasted CPU and potential size increase.
                string entryExt = Path.GetExtension(entry.file).ToLowerInvariant();
                bool skipCompress = entryExt == ".ogg" || entryExt == ".wav"
                                 || entryExt == ".png" || entryExt == ".jpg"
                                 || entryExt == ".gif" || entryExt == ".mpg"
                                 || entryExt == ".avi" || entryExt == ".mp4";

                byte[] toWrite = raw;
                bool useCompressed = false;

                if (!skipCompress)
                {
                    byte[] comp;
                    using (var ms = new MemoryStream())
                    {
                        using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                            zlib.Write(raw, 0, raw.Length);
                        comp = ms.ToArray();
                    }
                    if (comp.Length < raw.Length)
                    {
                        toWrite = comp;
                        useCompressed = true;
                    }
                }

                fileDataOffsets.Add(fs.Position);
                fileDataCompSizes.Add(toWrite.Length);
                fileDataRawSizes.Add(raw.Length);
                fileDataIsComp.Add(useCompressed);
                fileDataHashes.Add(dataHash(toWrite));

                writer.Write(toWrite);
                log?.Invoke($"[{i + 1}/{entries.Count}] {entry.relPath}");
            }

            // Go back and write directory entries at position 32
            fs.Position = 32;

            var fileTypeMap = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase) {
                { ".bmp", 1 },
                { ".png", 2 },
                { ".jpg", 3 },
                { ".gif", 4 },
                { ".wav", 5 },
                { ".ogg", 6 },
                { ".psd", 7 },
                { ".ycg", 8 },
                { ".psb", 9 }
            };

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Name hash
                writer.Write(entry.hash);

                // Length byte: ~(indexOf(byteLen, table))
                int byteLen  = entry.encodedName.Length;
                int tableIdx = Array.IndexOf(lengthSwappingTable, (byte)byteLen);
                if (tableIdx < 0) tableIdx = byteLen; // fallback if not in table
                writer.Write((byte)~tableIdx);

                // Encoded name bytes
                writer.Write(entry.encodedName);

                // File type byte
                byte fileType = GetFileType(Path.GetExtension(entry.relPath));
                writer.Write(fileType);

                // Compressed flag
                writer.Write((byte)(fileDataIsComp[i] ? 1 : 0));

                // Raw (uncompressed) size
                writer.Write(fileDataRawSizes[i]);

                // Compressed size (or raw size if uncompressed)
                writer.Write(fileDataCompSizes[i]);

                // Data offset
                if (engineVersion >= 479)
                    writer.Write(fileDataOffsets[i]);      // 8-byte Int64
                else
                    writer.Write((int)fileDataOffsets[i]); // 4-byte Int32

                // Data checksum
                writer.Write(fileDataHashes[i]);
            }
        }

        private static byte GetFileType(string ext)
        {
            return ext.ToLowerInvariant() switch
            {
                ".bmp"  => 1,
                ".png"  => 2,
                ".jpg"  => 3,
                ".gif"  => 4,
                ".wav"  => 5,
                ".ogg"  => 6,
                ".psd"  => 7,
                ".ycg"  => 8,
                ".psb"  => 9,
                _       => 0, // text / ybn / unknown
            };
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    // YSTB XOR CIPHER
    // ─────────────────────────────────────────────────────────────────────────

    public static class YurisYstb
    {
        private const string SIG = "YSTB";

        /// <summary>
        /// Brute-force guess the XOR key from a .ybn file.
        /// Returns key as int, or -1 if not found / file is not encrypted.
        /// </summary>
        public static int GuessKey(string ybnPath)
        {
            byte[] data = File.ReadAllBytes(ybnPath);
            if (data.Length < 32) return -1;
            if (Encoding.ASCII.GetString(data, 0, 4) != SIG) return -1;

            // If header is clear (already decrypted), the content after 32 bytes needs no key
            // The 4 block sizes live at bytes 16..31. XOR key applies to blocks after offset 32.
            // We can guess by trying to find a key that makes the first bytes of the code block
            // match known YSTB instruction patterns. But the simplest reliable approach is:
            // check if the file is already clear (no encryption needed).
            int version = BitConverter.ToInt32(data, 4);
            if (version >= 234 && version <= 490)
            {
                // Try key = 0 first (unencrypted)
                return 0;
            }

            // Try all 32-bit keys — but we can narrow this down:
            // The version field after XOR with key[0..3] must be 234-490.
            // data[4..7] ^ key[0..3] = version (234-490 = 0xEA-0x1EA)
            // So key[0] = data[4] ^ (version & 0xFF) etc.
            for (int v = 234; v <= 490; v++)
            {
                byte k0 = (byte)(data[4] ^ (v & 0xFF));
                byte k1 = (byte)(data[5] ^ ((v >> 8) & 0xFF));
                byte k2 = (byte)(data[6] ^ 0);
                byte k3 = (byte)(data[7] ^ 0);
                int key = (k0 << 24) | (k1 << 16) | (k2 << 8) | k3;

                // Validate: re-check version
                int testV = BitConverter.ToInt32(new byte[] { (byte)(data[4] ^ k0), (byte)(data[5] ^ k1), (byte)(data[6] ^ k2), (byte)(data[7] ^ k3) }, 0);
                if (testV == v)
                    return key;
            }

            return -1;
        }

        /// <summary>
        /// Apply XOR cipher to a .ybn file. Works for both encrypt and decrypt
        /// (XOR is its own inverse). key=0 means no cipher (pass-through).
        /// Returns the resulting bytes.
        /// </summary>
        public static byte[] Cipher(byte[] data, int key)
        {
            if (data.Length < 32) return data;
            if (Encoding.ASCII.GetString(data, 0, 4) != SIG) return data;

            if (key == 0) return data; // already clear

            var keyTable = new byte[] { (byte)(key >> 24), (byte)(key >> 16), (byte)(key >> 8), (byte)key };

            // Fixed for v481 (which has instCount @ 8, and sizes shifted left by 4)
            int codeSize     = BitConverter.ToInt32(data, 12);
            int argumentSize = BitConverter.ToInt32(data, 16);
            int resourceSize = BitConverter.ToInt32(data, 20);
            int wtfSize      = BitConverter.ToInt32(data, 24);

            var result = new byte[data.Length];
            Array.Copy(data, result, 32); // copy header as-is

            int pos = 32;
            foreach (int blockSize in new[] { codeSize, argumentSize, resourceSize, wtfSize })
            {
                for (int i = 0; i < blockSize && pos + i < data.Length; i++)
                    result[pos + i] = (byte)(data[pos + i] ^ keyTable[i & 3]);
                pos += blockSize;
            }

            return result;
        }

        /// <summary>
        /// Decrypt all .ybn files in the given folder.
        /// Automatically detects the encryption key from the largest file.
        /// Overwrites the files in-place.
        /// Returns the key used, or 0 if files were already clear.
        /// </summary>
        public static int DecryptFolder(string folderPath, Action<string> log = null)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var ybnFiles = Directory.GetFiles(folderPath, "*.ybn", SearchOption.TopDirectoryOnly);
            if (ybnFiles.Length == 0)
                throw new InvalidOperationException("No .ybn files found in folder.");

            // Find largest file for key detection
            string largestFile = ybnFiles.OrderByDescending(f => new FileInfo(f).Length).First();
            byte[] sample = File.ReadAllBytes(largestFile);

            int key = GuessKeyFromBytes(sample);
            log?.Invoke($"Key detected: 0x{key:X8}");

            int processed = 0;
            foreach (var f in ybnFiles)
            {
                byte[] raw = File.ReadAllBytes(f);
                byte[] decrypted = Cipher(raw, key);
                File.WriteAllBytes(f, decrypted);
                processed++;
                log?.Invoke($"[{processed}/{ybnFiles.Length}] Decrypted: {Path.GetFileName(f)}");
            }

            return key;
        }

        private static int GuessKeyFromBytes(byte[] data)
        {
            if (data.Length < 32) return 0;
            if (Encoding.ASCII.GetString(data, 0, 4) != SIG) return 0;

            int version = BitConverter.ToInt32(data, 4);
            if (version >= 234 && version <= 490) return 0; // already clear

            // Brute-force version field
            for (int v = 234; v <= 490; v++)
            {
                byte k0 = (byte)(data[4] ^ (v & 0xFF));
                byte k1 = (byte)(data[5] ^ ((v >> 8) & 0xFF));
                int testV = BitConverter.ToInt32(new byte[] {
                    (byte)(data[4] ^ k0), (byte)(data[5] ^ k1), 0, 0 }, 0);
                if (testV == v)
                    return (k0 << 24) | (k1 << 16);
            }
            return 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEXT EXTRACT / INJECT (for WORD opcodes in YSTB scripts)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Extract Japanese text from a decrypted .ybn file.
        /// Returns lines as a list (null = non-text / skip line).
        /// Also writes a .txt sidecar file next to the .ybn.
        /// </summary>
        public static List<string> ExtractText(string ybnPath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var shiftJis = Encoding.GetEncoding("SHIFT-JIS");

            byte[] data = File.ReadAllBytes(ybnPath);
            if (data.Length < 32 || Encoding.ASCII.GetString(data, 0, 4) != SIG)
                return new List<string>();

            var lines = new List<string>();
            using var ms     = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            reader.BaseStream.Position = 4;
            int version      = reader.ReadInt32();
            int instCount    = reader.ReadInt32();
            int codeSize     = reader.ReadInt32();
            int argumentSize = reader.ReadInt32();
            int resourceSize = reader.ReadInt32();
            /*int wtfSize =*/ reader.ReadInt32();

            long argBase      = 32 + codeSize;
            long resourceBase = argBase + argumentSize;

            reader.BaseStream.Position = 32;

            for (int i = 0; i < instCount; i++)
            {
                if (reader.BaseStream.Position >= argBase) break;
                byte opCode  = reader.ReadByte();
                byte argCount = reader.ReadByte();
                reader.ReadUInt16(); // wtf

                for (int j = 0; j < argCount; j++)
                {
                    long argPos  = argBase + (i * argCount + j) * 12; // not correct for all cases
                    // We don't have YSCM here, so we scan resource block for Shift-JIS strings
                }
            }

            // Simpler approach: scan the resource block for null-terminated Shift-JIS strings
            // that contain Japanese chars (> 0x80). This is reliable and doesn't need YSCM.
            reader.BaseStream.Position = resourceBase;
            byte[] resourceData = reader.ReadBytes(resourceSize);

            var result = ScanShiftJisStrings(resourceData, shiftJis);
            string txtPath = Path.ChangeExtension(ybnPath, ".txt");
            File.WriteAllLines(txtPath, result, new UTF8Encoding(false));
            return result;
        }

        private static List<string> ScanShiftJisStrings(byte[] data, System.Text.Encoding enc)
        {
            var strings = new List<string>();
            int i = 0;
            while (i < data.Length)
            {
                // Find a run of bytes that looks like Shift-JIS text (contains bytes > 0x80)
                int start = i;
                bool hasJapanese = false;
                while (i < data.Length && data[i] != 0)
                {
                    if (data[i] > 0x80) hasJapanese = true;
                    i++;
                }
                if (hasJapanese && i > start)
                {
                    try
                    {
                        string s = enc.GetString(data, start, i - start).Trim();
                        if (s.Length > 0) strings.Add(s);
                    }
                    catch { /* skip invalid sequences */ }
                }
                i++; // skip null terminator
            }
            return strings;
        }

        /// <summary>
        /// Extract text from all .ybn files in a folder.
        /// Creates one .txt file per .ybn.
        /// </summary>
        public static void ExtractTextFolder(string folderPath, Action<string> log = null)
        {
            var files = Directory.GetFiles(folderPath, "*.ybn", SearchOption.TopDirectoryOnly);
            int count = 0;
            foreach (var f in files)
            {
                var lines = ExtractText(f);
                count++;
                log?.Invoke($"[{count}/{files.Length}] Extracted {lines.Count} strings from {Path.GetFileName(f)}");
            }
        }

        /// <summary>
        /// NOT YET IMPLEMENTED: inject translated .txt back into .ybn.
        /// This requires YSCM (script metadata), which is game-specific.
        /// For now, throws NotImplementedException with a clear message.
        /// </summary>
        public static void InjectTextFolder(string folderPath, Action<string> log = null)
        {
            throw new NotImplementedException(
                "Text injection into .ybn requires YSCM (script opcode metadata), which is game-specific.\n" +
                "This feature is not yet supported. Please use GARbro or YuRISTools GUI for patching.");
        }
    }
}
