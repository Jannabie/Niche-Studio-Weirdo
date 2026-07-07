using AdvancedBinary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NPK3Tool
{
    static class NPK
    {
        public static bool EnableCompression = true;
        public static bool EnableSegmentation = true;
        public static bool ForceSegmentation = true;
        public static int NPKVersion = 3;
        public static uint NPKMinorVersion = 1;//Not Sure
        public static uint MaxSectionSize = 0x10000;
        public static Encoding Encoding = Encoding.UTF8;
        public static string[] DontCompress = { "png", "ogg", "jpg", "mpg" };

        // Extraction filters
        /// <summary>If non-null/non-empty, only extract files whose extension is in this set (e.g. "nut", "json").</summary>
        public static HashSet<string> FilterExtensions = null;
        /// <summary>If true, skip extracting files that already exist in the output directory.</summary>
        public static bool SkipExisting = false;

        public static byte[] CurrentKey;
        public static byte[] CurrentIV = new byte[] { 0x42, 0x79, 0x20, 0x4D, 0x61, 0x72, 0x63, 0x75, 0x73, 0x73, 0x61, 0x63, 0x61, 0x6E, 0x61, 0x00 };

        public static void Repack(string InputDirectory, string OutNPK = null) {
            InputDirectory = Path.GetFullPath(InputDirectory);
            if (!InputDirectory.EndsWith(Path.DirectorySeparatorChar) && !InputDirectory.EndsWith(Path.AltDirectorySeparatorChar))
                InputDirectory += Path.DirectorySeparatorChar;

            if (OutNPK == null) {
                OutNPK = InputDirectory.TrimEnd('\\', '/', '~');
                OutNPK = Path.Combine(Path.GetDirectoryName(OutNPK), Path.GetFileNameWithoutExtension(OutNPK) + "_New.npk");
            }

            string[] FilesPath = Directory.GetFiles(InputDirectory, "*.*", SearchOption.AllDirectories);
            string[] RelativeFiles = (from x in FilesPath select x.Substring(InputDirectory.Length).TrimStart('\\', '/')).ToArray();

            using (Stream Output = File.Create(OutNPK)) {
                switch (NPKVersion) {
                    case 3: Output.WriteUIn32(0x334B504Eu); break;
                    case 2: Output.WriteUIn32(0x324B504Eu); break;
                    default: throw new NotSupportedException("NPK Version Not Supported");
                }
                Output.WriteUIn32(NPKMinorVersion);
                Output.WriteBytes(CurrentIV);
                Output.WriteUIn32((uint)FilesPath.Length);

                var Entries = CreateInitialEntries(RelativeFiles, FilesPath);

                uint TableSize;
                using (Stream TBuilder = BuildEntries(Entries))
                using (Stream TEncryptor = TBuilder.CreateEncryptor(CurrentKey, CurrentIV))
                using (Stream TBuffer = TEncryptor.ToMemory())
                    TableSize = (uint)TBuffer.Length;

                Output.WriteUIn32(TableSize);
                long TablePos = Output.Position;
                Output.WriteBytes(new byte[TableSize]);

                // ── REPACK: Read files sequentially (no HDD thrashing), compress in parallel ──
                // We process files in batches: read one batch sequentially, compress that batch
                // in parallel, then write in order. Keeps memory bounded and CPU fully busy.
                int batchSize = Math.Max(8, Environment.ProcessorCount * 2);
                Console.WriteLine($"Repacking {FilesPath.Length} files...");
                int doneCount = 0;

                for (int batchStart = 0; batchStart < FilesPath.Length; batchStart += batchSize) {
                    int batchEnd = Math.Min(batchStart + batchSize, FilesPath.Length);
                    int count = batchEnd - batchStart;

                    // Step A: Read this batch of files into memory sequentially
                    var fileBuffers = new byte[count][];
                    for (int b = 0; b < count; b++) {
                        fileBuffers[b] = File.ReadAllBytes(FilesPath[batchStart + b]);
                    }

                    // Step B: Compress + encrypt every file in this batch in parallel
                    var processed = new byte[count][][];  // [fileIdx][segIdx] = encrypted bytes
                    Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, b => {
                        int i = batchStart + b;
                        string Ext = Path.GetExtension(FilesPath[i]).ToLower().TrimStart('.');
                        bool Compress = EnableCompression && !DontCompress.Contains(Ext);

                        var entry = Entries[i];

                        using (var hashStream = new MemoryStream(fileBuffers[b], false)) {
                            entry.SHA256 = hashStream.SHA256Checksum();
                        }

                        var segs = new byte[entry.SegmentsInfo.Length][];
                        long ReadPos = 0;

                        for (int x = 0; x < entry.SegmentsInfo.Length; x++) {
                            uint segLen = entry.SegmentsInfo[x].DecompressedSize;
                            using var SegData = new MemoryStream(fileBuffers[b], (int)ReadPos, (int)segLen, false);
                            ReadPos += segLen;

                            byte[] rawBytes = new byte[segLen];
                            Array.Copy(fileBuffers[b], (int)(ReadPos - segLen), rawBytes, 0, (int)segLen);

                            byte[] compressedBytes = rawBytes;
                            bool didCompress = false;
                            if (Compress) {
                                using var Compressed = SegData.Compress(NPKVersion);
                                if (Compressed.Length < segLen) {
                                    compressedBytes = ((MemoryStream)Compressed).ToArray();
                                    didCompress = true;
                                }
                            }

                            using var CompStream = new MemoryStream(compressedBytes);
                            using var Crypted = CompStream.CreateEncryptor(CurrentKey, CurrentIV).ToMemory();
                            segs[x] = ((MemoryStream)Crypted).ToArray();

                            entry.SegmentsInfo[x].RealSize = (uint)compressedBytes.Length;
                            entry.SegmentsInfo[x].AlignedSize = (uint)segs[x].Length;
                        }
                        Entries[i] = entry;
                        processed[b] = segs;
                    });

                    // Step C: Write this batch sequentially (must be single-threaded)
                    for (int b = 0; b < count; b++) {
                        int i = batchStart + b;
                        for (int x = 0; x < processed[b].Length; x++) {
                            Entries[i].SegmentsInfo[x].Offset = (uint)Output.Position;
                            Output.Write(processed[b][x], 0, processed[b][x].Length);
                        }
                        
                        doneCount++;
                        if (doneCount % 100 == 0 || doneCount == FilesPath.Length)
                            Console.WriteLine($"> Repacking File [{doneCount}/{FilesPath.Length}]: {RelativeFiles[i]}");
                    }
                    // Free batch memory immediately
                    for (int b = 0; b < count; b++) fileBuffers[b] = null;
                }

                Output.Position = TablePos;
                using var TableData = BuildEntries(Entries);
                using var TableEncryptor = TableData.CreateEncryptor(CurrentKey, CurrentIV);
                using var OutTableData = TableEncryptor.ToMemory();
                Debug.Assert(OutTableData.Length == TableSize);
                OutTableData.CopyTo(Output);

                Console.WriteLine($"Done. {FilesPath.Length} files repacked to: {OutNPK}");
            }
        }

        public static void SetIV(string IVHex) {
            IVHex = IVHex.Trim().Replace(" ", "");
            if (IVHex.Length != CurrentIV.Length * 2) {
                Console.WriteLine("Warning: Invalid IV");
                return;
            }
            for (int x = 0; x < IVHex.Length; x += 2) {
                string HByte = IVHex.Substring(x, 2);
                CurrentIV[x / 2] = Convert.ToByte(HByte, 16);
            }
        }
        public static void SetKey(string KeyHex) {
            CurrentKey = new byte[0x20];
            KeyHex = KeyHex.Trim().Replace(" ", "");
            if (KeyHex.Length != CurrentKey.Length * 2) {
                Console.WriteLine("Warning: Invalid KEY");
                return;
            }
            for (int x = 0; x < KeyHex.Length; x += 2) {
                string HByte = KeyHex.Substring(x, 2);
                CurrentKey[x / 2] = Convert.ToByte(HByte, 16);
            }
        }

        public static void SetEncoding(string Name) {
            Encoding = Name.ToEncoding();
        }

        public static void SetMaxSectionSize(string MaxSize) {
            MaxSize = MaxSize.Trim();
            if (MaxSize.ToLower().StartsWith("0x"))
            {
                MaxSize = MaxSize.Substring(2);
                MaxSectionSize = uint.Parse(MaxSize, System.Globalization.NumberStyles.HexNumber);
            }
            else
                MaxSectionSize = uint.Parse(MaxSize);
        }

        public static NPK3Entry[] CreateInitialEntries(string[] Files, string[] FilesPath) {
            NPK3Entry[] Entries = new NPK3Entry[Files.Length];
            for (int i = 0; i < Files.Length; i++) {
                NPK3Entry Entry = new NPK3Entry();
                Entry.FilePath = Files[i].Replace("\\", "/");
                
                var fileInfo = new FileInfo(FilesPath[i]);
                Entry.FileSize = (uint)fileInfo.Length;
                // Initialize SHA256 to empty array so BuildEntries doesn't crash when calculating TableSize
                Entry.SHA256 = new byte[0x20];

                long Reaming = Entry.FileSize;
                if (EnableSegmentation || Reaming > uint.MaxValue || ForceSegmentation)
                {
                    Entry.SegmentsInfo = new NPKSegmentInfo[1 + (Entry.FileSize / MaxSectionSize)];
                    Entry.SegmentationMode = (byte)(Entry.SegmentsInfo.Length > 1 ? 0 : 1);

                    if (ForceSegmentation)
                        Entry.SegmentationMode = 0;

                    for (int x = 0; x < Entry.SegmentsInfo.Length; x++)
                    {
                        uint MaxBytes = Reaming < MaxSectionSize ? (uint)Reaming : MaxSectionSize;
                        Entry.SegmentsInfo[x] = new NPKSegmentInfo()
                        {
                            Offset = 0,
                            DecompressedSize = MaxBytes,
                            RealSize = MaxBytes,
                            AlignedSize = MaxBytes + (0x10 - (MaxBytes % 0x10))
                        };

                        Reaming -= MaxBytes;
                    }
                }
                else
                {
                    Entry.SegmentationMode = 1;
                    Entry.SegmentsInfo = new NPKSegmentInfo[] {
                        new NPKSegmentInfo(){
                            Offset = 0,
                            DecompressedSize = (uint)Reaming,
                            RealSize = (uint)Reaming,
                            AlignedSize = (uint)Reaming + (0x10 - ((uint)Reaming % 0x10))
                        }
                    };
                }

                Entries[i] = Entry;
            }

            return Entries;
        }

        public static Stream BuildEntries(NPK3Entry[] Entries) {
            Stream Output = new MemoryStream();
            StructWriter Writer = new StructWriter(Output, Encoding: Encoding);
            for (int i = 0; i < Entries.Length; i++) {
                var Entry = Entries[i];
                Writer.WriteStruct(ref Entry);
            }
            Output.Position = 0;
            return Output;
        }

        public static void Unpack(string Package, string OutDir = null)
        {
            if (OutDir == null)
                OutDir = Path.Combine(Path.GetDirectoryName(Package), Path.GetFileName(Package) + "~");

            if (new FileInfo(Package).IsReadOnly)
                throw new Exception("Can't Unpack Read-Only Files");

            // ── Parse header only (tiny read at start of file) ──
            NPK3Entry[] Entries;
            using (var fs = new FileStream(Package, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            {
                switch (fs.ReadUInt32(0)) {
                    case 0x334B504Eu: NPKVersion = 3; break;
                    case 0x324B504Eu: NPKVersion = 2; break;
                    default: throw new NotSupportedException("NPK Version Not Supported");
                }
                CurrentIV = fs.ReadBytes(8, 0x10);
                using var Table = GetEntryTable(fs);
                Entries = GetEntries(Table);
            }

            Console.WriteLine($"Extracting {Entries.Length} files...");

            // Pre-build all output paths and create directories up front
            var allPaths = new string[Entries.Length];
            var dirSet = new HashSet<string>();
            for (int i = 0; i < Entries.Length; i++) {
                allPaths[i] = Path.Combine(OutDir, Entries[i].FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                dirSet.Add(Path.GetDirectoryName(allPaths[i]));
            }
            foreach (var d in dirSet)
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);

            // Sort entry indices by segment offset so we read the NPK file strictly forward.
            // This is critical: sequential reads = full disk throughput, no seeking.
            var readOrder = Enumerable.Range(0, Entries.Length)
                .OrderBy(i => Entries[i].SegmentsInfo.Length > 0 ? Entries[i].SegmentsInfo[0].Offset : 0L)
                .ToArray();

            // ── Producer-Consumer Pipeline ──
            // Producer  : one thread reads segments from disk sequentially → queues raw byte[] buffers
            // Consumers : N threads decrypt + decompress + write to disk in parallel
            // Semaphore : caps the number of items in-flight to bound peak RAM usage
            //             (prevents "makin lama makin lambat" GC pressure explosion)
            int maxInFlight = Math.Max(16, Environment.ProcessorCount * 3);
            var sem = new SemaphoreSlim(maxInFlight, maxInFlight);
            int doneCount = 0;
            int totalEntries = Entries.Length;

            // Capture statics for thread safety
            int  snapVersion = NPKVersion;
            byte[] snapIV  = CurrentIV;
            byte[] snapKey = CurrentKey;

            // Use a list of tasks so we can WaitAll at the end
            var tasks = new List<Task>(Entries.Length);
            var exceptions = new ConcurrentBag<Exception>();

            // ── Producer: sequential disk reader (4 MB OS read buffer) ──
            using (var fs = new FileStream(Package, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * 1024 * 1024))
            {
                foreach (int idx in readOrder)
                {
                    var entry   = Entries[idx];
                    var outPath = allPaths[idx];

                    // ── FILTER: skip this entry without reading if it doesn't match ──
                    string ext = Path.GetExtension(entry.FilePath).TrimStart('.').ToLower();
                    bool filteredOut = FilterExtensions != null && FilterExtensions.Count > 0 && !FilterExtensions.Contains(ext);
                    bool skipExist  = SkipExisting && File.Exists(outPath);

                    if (filteredOut || skipExist)
                    {
                        // Advance the file position past all segments (keep reads sequential)
                        if (!filteredOut && skipExist)
                        { /* file exists, seek past */ }
                        // Skip without touching the FileStream — the next entry's Seek() will handle it
                        Interlocked.Increment(ref doneCount);
                        continue;
                    }

                    // Read all segments for this entry sequentially
                    var segBuffers = new byte[entry.SegmentsInfo.Length][];
                    for (int x = 0; x < entry.SegmentsInfo.Length; x++)
                    {
                        var seg = entry.SegmentsInfo[x];
                        // Seek only when necessary (handles any alignment gaps)
                        if (fs.Position != (long)seg.Offset)
                            fs.Seek((long)seg.Offset, SeekOrigin.Begin);

                        segBuffers[x] = new byte[seg.AlignedSize];
                        int totalRead = 0;
                        while (totalRead < segBuffers[x].Length) {
                            int n = fs.Read(segBuffers[x], totalRead, segBuffers[x].Length - totalRead);
                            if (n == 0) break;
                            totalRead += n;
                        }
                    }

                    // Block producer if consumers are lagging (bounded queue)
                    sem.Wait();

                    // Capture loop variables for the closure
                    var captEntry    = entry;
                    var captBuffers  = segBuffers;
                    var captPath     = outPath;

                    tasks.Add(Task.Run(() =>
                    {
                        try {
                            using var output = new MemoryStream((int)captEntry.FileSize + 256);
                            for (int x = 0; x < captBuffers.Length; x++) {
                                using var raw    = new MemoryStream(captBuffers[x], false);
                                using var reader = raw.CreateDecryptor(snapKey, snapIV);
                                if (captEntry.SegmentsInfo[x].IsCompressed) {
                                    using var decomp = reader.CreateDecompressor(snapVersion);
                                    decomp.CopyTo(output);
                                } else {
                                    reader.CopyTo(output);
                                }
                                // Free buffer as soon as we're done with it
                                captBuffers[x] = null;
                            }
                            // Write fully decompressed data to disk in one syscall
                            File.WriteAllBytes(captPath, output.ToArray());

                            int done = Interlocked.Increment(ref doneCount);
                            if (done % 100 == 0 || done == totalEntries)
                                Console.WriteLine($"> Extracting File [{done}/{totalEntries}]: {captEntry.FilePath}");
                        } catch (Exception ex) {
                            exceptions.Add(ex);
                        } finally {
                            sem.Release(); // allow producer to queue next item
                        }
                    }));
                }
            } // producer done — FileStream closed

            Task.WaitAll(tasks.ToArray());

            if (!exceptions.IsEmpty)
                throw new AggregateException("Extraction failed for some files.", exceptions);

            Console.WriteLine($"Done. {totalEntries} files extracted to: {OutDir}");
        }
        public static NPK3Entry[] GetEntries(Stream EntryTable) {
			List<NPK3Entry> Entries = new List<NPK3Entry>();
			StructReader Reader = new StructReader(EntryTable, Encoding: Encoding);
			while (Reader.BaseStream.Position + 1 < Reader.BaseStream.Length) {
				var Entry = new NPK3Entry();
				Reader.ReadStruct(ref Entry);
				Entries.Add(Entry);
			}
			return Entries.ToArray();
		}

		public static Stream GetEntryTable(Stream Package) {
			uint TableSize = Package.ReadUInt32(0x1C);
			var CryptedTable = Package.CreateStream(0x20, TableSize);
			return CryptedTable.CreateDecryptor(CurrentKey, CurrentIV).ToMemory();
		}
    }
#pragma warning disable 0219, 0649
	struct NPK3Entry
	{
		public byte SegmentationMode;//0 = With Segmentation, 1 = Without Segmentation

		[PString(PrefixType = Const.UINT16)]
		public string FilePath;

		public uint FileSize;

		[FArray(Length = 0x20)]
		public byte[] SHA256;

		[PArray(PrefixType = Const.UINT32), StructField]
		public NPKSegmentInfo[] SegmentsInfo;
	}

	struct NPKSegmentInfo {
		public long Offset;
		public uint AlignedSize;
		public uint RealSize;
		public uint DecompressedSize;

		[Ignore]
		public bool IsCompressed => RealSize < DecompressedSize;
	}
#pragma warning restore 0219, 0649
}
