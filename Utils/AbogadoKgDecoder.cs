using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace NicheStudioWeirdo.Utils
{
    public class KgReader
    {
        private byte[] _data;
        private int _pos;
        private int _width;
        private int _height;
        private int _bpp;
        private int _pixelSize;
        private int _stride;
        private byte[] _output;
        private int _palOffset;
        private int _alphaOffset;

        private int _buf;
        private int _left;
        private byte[] _dict;

        public byte[] Output => _output;
        public int PixelSize => _pixelSize;
        public int AlphaOffset => _alphaOffset;

        public KgReader(byte[] data, int dataOffset, int width, int height, int bpp, int palOffset, int alphaOffset)
        {
            _data = data;
            _pos = dataOffset;
            _width = width;
            _height = height;
            _bpp = bpp;
            _pixelSize = bpp / 8;
            _stride = _pixelSize * width;
            _output = new byte[_stride * height];
            _palOffset = palOffset;
            _alphaOffset = alphaOffset;

            _buf = 0;
            _left = 0;
            _dict = new byte[0x800];
        }

        private void ResetDict()
        {
            for (int i = 0; i < 0x800; i++)
                _dict[i] = (byte)(i & 7);
        }

        private int GetBit()
        {
            if (_left == 0)
            {
                if (_pos >= _data.Length) return -1;
                _buf = _data[_pos++];
                _left = 8;
            }
            int b = (_buf >> (_left - 1)) & 1;
            _left--;
            return b;
        }

        private int GetBits(int n)
        {
            int r = 0;
            for (int i = 0; i < n; i++)
            {
                int bit = GetBit();
                if (bit == -1) return -1;
                r = (r << 1) | bit;
            }
            return r;
        }

        private void ResetBits()
        {
            _left = 0;
            _buf = 0;
        }

        private int GetCount()
        {
            int count = GetBits(2);
            if (count == -1) return 0;
            if (count == 0)
            {
                count = GetBits(4);
                if (count == -1) return 0;
                if (count != 0) count += 3;
                else
                {
                    count = GetBits(8);
                    if (count == -1) return 0;
                    if (count == 0)
                    {
                        count = GetBits(16);
                        if (count == -1) return 0;
                        if (count == 0)
                        {
                            int high = GetBits(16);
                            if (high == -1) return 0;
                            int low = GetBits(16);
                            if (low == -1) return 0;
                            count = (high << 16) | low;
                        }
                    }
                }
            }
            return count;
        }

        private byte GetPixel(int dst)
        {
            int bit1 = GetBits(1);
            if (bit1 == -1) return 0;
            if (bit1 == 1)
            {
                int b = GetBits(8);
                return b == -1 ? (byte)0 : (byte)b;
            }
            else
            {
                if (dst - _pixelSize < 0) return 0;
                int n = 8 * _output[dst - _pixelSize];
                int bits3 = GetBits(3);
                if (bits3 == -1) return 0;
                return _dict[n + bits3];
            }
        }

        private void UpdateDict(byte b, byte prev)
        {
            int s = 8 * prev;
            int i;
            for (i = 0; i < 8; i++)
            {
                if (_dict[s + i] == b) break;
            }
            if (i == 8) i = 7;

            if (i != 0)
            {
                Array.Copy(_dict, s, _dict, s + 1, i);
                _dict[s] = b;
            }
        }

        private void UnpackChannel(int dst)
        {
            _output[dst] = (byte)GetBits(8);
            dst += _pixelSize;
            _output[dst] = (byte)GetBits(8);
            dst += _pixelSize;

            while (dst < _output.Length)
            {
                int ctl = GetBits(1);
                if (ctl == -1) break;

                if (ctl == 0)
                {
                    byte b = GetPixel(dst);
                    if (dst < _output.Length)
                        _output[dst] = b;
                    if (dst - _pixelSize >= 0)
                        UpdateDict(b, _output[dst - _pixelSize]);
                    dst += _pixelSize;
                    continue;
                }

                if (GetBits(1) != 0)
                    ctl = GetBits(2);
                else
                    ctl = 4;

                int offset;
                if (ctl == 0) offset = _stride;
                else if (ctl == 1) offset = _stride - _pixelSize;
                else if (ctl == 2) offset = _stride + _pixelSize;
                else if (ctl == 3) offset = 2 * _pixelSize;
                else offset = _pixelSize;

                int count = GetCount();
                int src = dst - offset;
                for (int i = 0; i < count; i++)
                {
                    if (dst < _output.Length && src >= 0 && src < _output.Length)
                    {
                        _output[dst] = _output[src];
                    }
                    dst += _pixelSize;
                    src += _pixelSize;
                }
            }
        }

        private void ConvertToBgr32()
        {
            _stride = _width * 4;
            byte[] pixels = new byte[_stride * _height];
            int dst = 0;
            if (_pixelSize == 1)
            {
                // unused
            }
            else
            {
                for (int src = 0; src < _output.Length && dst + 2 < pixels.Length; src += _pixelSize)
                {
                    pixels[dst] = _output[src];
                    if (src + 1 < _output.Length) pixels[dst + 1] = _output[src + 1];
                    if (src + 2 < _output.Length) pixels[dst + 2] = _output[src + 2];
                    dst += 4;
                }
            }
            _output = pixels;
            _pixelSize = 4;
        }

        public void Unpack()
        {
            ResetDict();
            UnpackChannel(0);
            if (_pixelSize > 1)
            {
                UnpackChannel(1);
                UnpackChannel(2);
            }

            if (_alphaOffset != 0)
            {
                ConvertToBgr32();
                _pos = _alphaOffset;
                ResetBits();
                ResetDict();
                UnpackChannel(3);
            }
        }
    }

    public class AbogadoKgDecoder
    {
        public static byte[] DecodeKgToPng(byte[] kgData)
        {
            if (kgData.Length < 0x30) return null;
            if (kgData[0] != 'K' || kgData[1] != 'G') return null;

            int version = kgData[2];
            int bppCode = kgData[3];
            int width = BitConverter.ToUInt16(kgData, 0x04);
            int height = BitConverter.ToUInt16(kgData, 0x06);
            int palOffset = BitConverter.ToInt32(kgData, 0x0C);
            int dataOffset = BitConverter.ToInt32(kgData, 0x10);

            int alphaOffset = 0;
            if (version == 2)
            {
                alphaOffset = BitConverter.ToInt32(kgData, 0x2C);
            }

            if (width == 0 || height == 0) return null;
            if (dataOffset == 0 || dataOffset >= kgData.Length)
            {
                if (bppCode == 1) dataOffset = 0x30 + 1024;
                else dataOffset = 0x30;
            }

            int bpp = bppCode == 2 ? 24 : 8;

            try
            {
                var reader = new KgReader(kgData, dataOffset, width, height, bpp, palOffset, alphaOffset);
                reader.Unpack();

                byte[] bgra32Data = new byte[width * height * 4];

                if (bpp == 8)
                {
                    if (palOffset == 0) palOffset = 0x30;
                    byte[] palData = new byte[1024];
                    if (palOffset + 1024 <= kgData.Length)
                    {
                        Array.Copy(kgData, palOffset, palData, 0, 1024);
                    }

                    int px = width * height;
                    for (int i = 0; i < px; i++)
                    {
                        if (i >= reader.Output.Length) break;
                        byte idx = reader.Output[i];
                        bgra32Data[i * 4] = palData[idx * 4];     // B
                        bgra32Data[i * 4 + 1] = palData[idx * 4 + 1]; // G
                        bgra32Data[i * 4 + 2] = palData[idx * 4 + 2]; // R
                        bgra32Data[i * 4 + 3] = 255;                  // A
                    }
                }
                else
                {
                    int px = width * height;
                    if (reader.AlphaOffset != 0)
                    {
                        // 32-bit (reader converted it)
                        for (int i = 0; i < px; i++)
                        {
                            if (i * 4 + 3 >= reader.Output.Length) break;
                            bgra32Data[i * 4] = reader.Output[i * 4];         // B
                            bgra32Data[i * 4 + 1] = reader.Output[i * 4 + 1]; // G
                            bgra32Data[i * 4 + 2] = reader.Output[i * 4 + 2]; // R
                            bgra32Data[i * 4 + 3] = reader.Output[i * 4 + 3]; // A
                        }
                    }
                    else
                    {
                        // 24-bit
                        for (int i = 0; i < px; i++)
                        {
                            if (i * 3 + 2 >= reader.Output.Length) break;
                            bgra32Data[i * 4] = reader.Output[i * 3];         // B
                            bgra32Data[i * 4 + 1] = reader.Output[i * 3 + 1]; // G
                            bgra32Data[i * 4 + 2] = reader.Output[i * 3 + 2]; // R
                            bgra32Data[i * 4 + 3] = 255;                      // A
                        }
                    }
                }

                // Flip vertically (KG images are stored bottom-up)
                byte[] flippedData = new byte[bgra32Data.Length];
                int stride = width * 4;
                for (int y = 0; y < height; y++)
                {
                    int srcY = height - 1 - y;
                    Array.Copy(bgra32Data, srcY * stride, flippedData, y * stride, stride);
                }

                var bitmap = BitmapSource.Create(
                    width, height, 96, 96,
                    PixelFormats.Bgra32, null,
                    flippedData, stride);

                using (var ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(ms);
                    return ms.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }

    // ─── KG Encoder (PNG → KG) ──────────────────────────────────────────────
    public class KgBitWriter
    {
        private System.Collections.Generic.List<byte> _data = new();
        private int _currentByte = 0;
        private int _bitPos = 8;

        public void WriteBits(int value, int count)
        {
            while (count > 0)
            {
                int take = Math.Min(count, _bitPos);
                int shift = _bitPos - take;
                int mask = (1 << take) - 1;
                int bitsToWrite = (value >> (count - take)) & mask;
                _currentByte |= (bitsToWrite << shift);
                _bitPos -= take;
                count -= take;
                if (_bitPos == 0)
                {
                    _data.Add((byte)_currentByte);
                    _currentByte = 0;
                    _bitPos = 8;
                }
            }
        }

        public byte[] GetBytes()
        {
            if (_bitPos < 8) _data.Add((byte)_currentByte);
            return _data.ToArray();
        }
    }

    public class AbogadoKgEncoder
    {
        private static void EncodeCount(KgBitWriter w, int count)
        {
            if (count > 0 && count < 4)
                w.WriteBits(count, 2);
            else if (count < 19)
            { w.WriteBits(0, 2); w.WriteBits(count - 3, 4); }
            else if (count < 256)
            { w.WriteBits(0, 2); w.WriteBits(0, 4); w.WriteBits(count, 8); }
            else
            { w.WriteBits(0, 2); w.WriteBits(0, 4); w.WriteBits(0, 8); w.WriteBits(count, 16); }
        }

        private static void CompressChannel(byte[] data, KgBitWriter w)
        {
            if (data.Length == 0) return;
            w.WriteBits(data[0], 8);
            if (data.Length > 1) w.WriteBits(data[1], 8);

            int i = 2;
            while (i < data.Length)
            {
                byte cur = data[i - 1];
                int runLen = 0;
                int maxRun = Math.Min(data.Length - i, 65535);
                for (int k = 0; k < maxRun; k++)
                {
                    if (data[i + k] == cur) runLen++;
                    else break;
                }
                if (runLen >= 2)
                {
                    w.WriteBits(1, 1); w.WriteBits(0, 1);
                    EncodeCount(w, runLen);
                    i += runLen;
                }
                else
                {
                    w.WriteBits(0, 1); w.WriteBits(1, 1);
                    w.WriteBits(data[i], 8);
                    i++;
                }
            }
        }

        /// <summary>
        /// Encode a PNG file to KG format.
        /// Returns raw .KG bytes to write to disk.
        /// </summary>
        public static byte[] EncodePngToKg(byte[] pngBytes, int forceBpp = 0, byte[] originalHeader = null)
        {
            // Decode PNG using WPF
            BitmapSource src;
            using (var ms = new MemoryStream(pngBytes))
            {
                var dec = new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                src = dec.Frames[0];
            }

            int width = src.PixelWidth;
            int height = src.PixelHeight;

            // Extract alpha channel if present
            bool hasAlpha = false;
            byte[] aCh = new byte[width * height];
            byte[] alphaComp = null;
            if (src.Format == PixelFormats.Bgra32 || src.Format == PixelFormats.Pbgra32 || src.Format == PixelFormats.Prgba128Float || src.Format == PixelFormats.Prgba64 || src.Format == PixelFormats.Rgba128Float || src.Format == PixelFormats.Rgba64)
            {
                var bgra = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                int stride32 = width * 4;
                byte[] pixels32 = new byte[stride32 * height];
                bgra.CopyPixels(pixels32, stride32, 0);
                for (int y = 0; y < height; y++)
                {
                    int srcY = height - 1 - y;
                    int srcRow = srcY * stride32;
                    int dstRow = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        byte a = pixels32[srcRow + x * 4 + 3];
                        aCh[dstRow + x] = a;
                        if (a < 255) hasAlpha = true;
                    }
                }
            }

            if (hasAlpha)
            {
                var wAlpha = new KgBitWriter();
                CompressChannel(aCh, wAlpha);
                alphaComp = wAlpha.GetBytes();
            }

            // Determine bpp
            int bpp = forceBpp;
            if (bpp == 0)
            {
                // Auto-detect: if image has palette treat as 8bpp, else 24bpp
                bpp = src.Format == PixelFormats.Indexed8 || src.Format == PixelFormats.Indexed4 || src.Format == PixelFormats.Indexed2 || src.Format == PixelFormats.Indexed1 ? 8 : 24;
            }

            bool isIndexed = (bpp == 8);
            byte[] compressed = Array.Empty<byte>();
            byte[] paletteBuffer = new byte[1024];

            if (isIndexed)
            {
                // Convert to 8bpp indexed
                BitmapSource indexed;
                if (src.Format == PixelFormats.Indexed8)
                {
                    indexed = src;
                }
                else
                {
                    // Convert to 8bpp using a custom optimized palette
                    var customPalette = PaletteGenerator.GeneratePalette(src, 256);
                    indexed = new FormatConvertedBitmap(src, PixelFormats.Indexed8, customPalette, 0);
                }

                // Get raw indices
                int stride8 = (width * indexed.Format.BitsPerPixel + 7) / 8;
                byte[] pixels8 = new byte[stride8 * height];
                indexed.CopyPixels(pixels8, stride8, 0);
                
                byte[] flipped8 = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    int srcY = height - 1 - y; // Flip vertically!
                    int srcRow = srcY * stride8;
                    int dstRow = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        flipped8[dstRow + x] = pixels8[srcRow + x];
                    }
                }
                var w = new KgBitWriter();
                CompressChannel(flipped8, w);
                compressed = w.GetBytes();

                // Build palette (BGRA → KG uses BGRA layout in 4-byte entries)
                if (indexed.Palette != null)
                {
                    var colors = indexed.Palette.Colors;
                    for (int ci = 0; ci < Math.Min(colors.Count, 256); ci++)
                    {
                        paletteBuffer[ci * 4 + 0] = colors[ci].B;
                        paletteBuffer[ci * 4 + 1] = colors[ci].G;
                        paletteBuffer[ci * 4 + 2] = colors[ci].R;
                        paletteBuffer[ci * 4 + 3] = 0;
                    }
                }
            }
            else
            {
                // 24bpp RGB
                var rgb = new FormatConvertedBitmap(src, PixelFormats.Bgr24, null, 0);
                int stride24 = (width * rgb.Format.BitsPerPixel + 7) / 8;
                byte[] pixels24 = new byte[stride24 * height];
                rgb.CopyPixels(pixels24, stride24, 0);

                byte[] bCh = new byte[width * height];
                byte[] gCh = new byte[width * height];
                byte[] rCh = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    int srcY = height - 1 - y; // Flip vertically!
                    int srcRow = srcY * stride24;
                    int dstRow = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int pi = dstRow + x;
                        int srcOffset = srcRow + x * 3;
                        bCh[pi] = pixels24[srcOffset + 0];
                        gCh[pi] = pixels24[srcOffset + 1];
                        rCh[pi] = pixels24[srcOffset + 2];
                    }
                }

                var w = new KgBitWriter();
                CompressChannel(bCh, w);
                CompressChannel(gCh, w);
                CompressChannel(rCh, w);
                compressed = w.GetBytes();
            }

            // Now compute offsets and total size
            int headerPalOffset = isIndexed ? 0x30 : 0;
            int headerDataOffset = isIndexed ? 0x30 + 1024 : 0x30;
            
            // If we have the original header, use it as the base.
            // The original may use pal_offset=0x2C and data_offset=0x42C.
            // We must respect these because the game engine hard-validates them.
            if (originalHeader != null && originalHeader.Length >= 0x30)
            {
                int origPalOff  = BitConverter.ToInt32(originalHeader, 0x0C);
                int origDataOff = BitConverter.ToInt32(originalHeader, 0x10);
                // Use original offsets if they differ (e.g. 0x2C palette start)
                if (origPalOff  > 0 && isIndexed) headerPalOffset  = origPalOff;
                if (origDataOff > 0)               headerDataOffset = origDataOff;
            }

            int headerAlphaOffset = 0;
            if (hasAlpha && alphaComp != null)
            {
                headerAlphaOffset = headerDataOffset + compressed.Length;
            }

            // Rebuild file with correct offsets
            byte[] fileData;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Write zeros for header placeholder
                bw.Write(new byte[0x30]);
                // If 8bpp, write palette at correct offset
                if (isIndexed)
                {
                    // Pad from 0x30 to headerPalOffset if original has smaller header (e.g. 0x2C)
                    // Actually if origPalOffset < 0x30, we shrink the header!
                    // Reset and write with correct layout:
                }
                if (isIndexed) bw.Write(paletteBuffer);
                bw.Write(compressed);
                if (hasAlpha && alphaComp != null) bw.Write(alphaComp);
                fileData = ms.ToArray();
            }

            // If original header uses pal_offset=0x2C instead of 0x30,
            // we need to rebuild the file layout with a 0x2C-byte header.
            if (isIndexed && headerPalOffset == 0x2C && originalHeader != null)
            {
                // Rebuild: [0x2C header][1024 palette][compressed]
                using var ms2 = new MemoryStream();
                using var bw2 = new BinaryWriter(ms2);
                bw2.Write(new byte[0x2C]); // shorter header
                bw2.Write(paletteBuffer);
                bw2.Write(compressed);
                if (hasAlpha && alphaComp != null) bw2.Write(alphaComp);
                fileData = ms2.ToArray();
                // Recompute: data starts after 0x2C + 1024 palette
                headerDataOffset = 0x2C + 1024; // = 0x42C
                if (hasAlpha && alphaComp != null)
                    headerAlphaOffset = headerDataOffset + compressed.Length;
                else
                    headerAlphaOffset = 0;
            }

            int totalFileSize = fileData.Length;

            // Build header: start from original header bytes if available, then patch the variable fields
            byte[] header = new byte[Math.Min(0x30, fileData.Length)];
            if (originalHeader != null && originalHeader.Length >= 0x30)
            {
                // Copy original header verbatim — preserves alpha_flag, bpp_code, etc.
                Array.Copy(originalHeader, header, 0x30);
            }
            else
            {
                // Build header from scratch
                header[0] = (byte)'K'; header[1] = (byte)'G';
                header[2] = hasAlpha ? (byte)2 : (byte)0;
                header[3] = isIndexed ? (byte)1 : (byte)2;
                BitConverter.GetBytes((ushort)width).CopyTo(header, 0x04);
                BitConverter.GetBytes((ushort)height).CopyTo(header, 0x06);
            }

            // Always patch the variable fields (offsets and sizes change with new pixel data)
            BitConverter.GetBytes(headerPalOffset).CopyTo(header, 0x0C);
            BitConverter.GetBytes(headerDataOffset).CopyTo(header, 0x10);
            BitConverter.GetBytes(totalFileSize).CopyTo(header, 0x14);
            BitConverter.GetBytes(width).CopyTo(header, 0x24);
            BitConverter.GetBytes(height).CopyTo(header, 0x28);
            BitConverter.GetBytes(headerAlphaOffset).CopyTo(header, 0x2C);

            // Patch header into file data
            Array.Copy(header, 0, fileData, 0, header.Length);
            return fileData;
        }

        /// <summary>
        /// Convert all PNG files in a folder to KG.
        /// Reads kg_metadata.json if present to know target bpp per file.
        /// Output goes into packed_kg/ subfolder.
        /// </summary>
        public static void ConvertFolderPngToKg(string folderPath, Action<string> log)
        {
            string[] pngs = System.IO.Directory.GetFiles(folderPath, "*.png");
            log($"[*] Found {pngs.Length} PNG files in {folderPath}");

            // Load metadata — now stores original header bytes as base64
            var metaBpp = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var metaHeaders = new System.Collections.Generic.Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            string metaPath = System.IO.Path.Combine(folderPath, "kg_metadata.json");
            if (System.IO.File.Exists(metaPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(metaPath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.TryGetProperty("bpp", out var bppEl))
                            metaBpp[prop.Name] = bppEl.GetInt32();
                        if (prop.Value.TryGetProperty("orig_header", out var hdrEl))
                            metaHeaders[prop.Name] = Convert.FromBase64String(hdrEl.GetString() ?? "");
                    }
                    log($"[*] Loaded metadata for {metaBpp.Count} files ({metaHeaders.Count} with saved headers)");
                }
                catch (Exception ex) { log($"[WARN] Could not read kg_metadata.json: {ex.Message}"); }
            }

            string outDir = System.IO.Path.Combine(folderPath, "packed_kg");
            System.IO.Directory.CreateDirectory(outDir);

            int ok = 0, fail = 0;
            foreach (string png in pngs)
            {
                string name = System.IO.Path.GetFileName(png);
                int bpp = metaBpp.TryGetValue(name, out int b) ? b : 0;
                metaHeaders.TryGetValue(name, out byte[] origHeader);
                log($"[*] Encoding {name} (bpp={(bpp == 0 ? "auto" : bpp.ToString())}) ...");
                try
                {
                    byte[] pngBytes = System.IO.File.ReadAllBytes(png);
                    byte[] kgBytes = EncodePngToKg(pngBytes, bpp, origHeader);
                    string outName = System.IO.Path.GetFileNameWithoutExtension(png) + ".KG";
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, outName), kgBytes);
                    log($"    -> [OK] {outName} ({kgBytes.Length} bytes)");
                    ok++;
                }
                catch (Exception ex)
                {
                    log($"    -> [ERROR] {name}: {ex.Message}");
                    fail++;
                }
            }
            log($"\n[*] Done. {ok} OK, {fail} failed. Output: {outDir}");
        }

        /// <summary>
        /// Decode all raw KG/SCF files in a folder to PNG.
        /// </summary>
        public static void DecodeFolderKgToPng(string folderPath, Action<string> log)
        {
            string outDir = System.IO.Path.Combine(folderPath, "extracted_png");
            System.IO.Directory.CreateDirectory(outDir);

            string[] files = System.IO.Directory.GetFiles(folderPath, "*.*");
            log($"[*] Scanning {files.Length} files for KG data...");
            log($"[*] Output folder: {outDir}");
            int ok = 0;
            var metadata = new System.Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in files)
            {
                try
                {
                    byte[] data = System.IO.File.ReadAllBytes(f);
                    if (data.Length < 2 || data[0] != 'K' || data[1] != 'G') continue;

                    log($"[*] Decoding {System.IO.Path.GetFileName(f)} ...");
                    byte[] png = AbogadoKgDecoder.DecodeKgToPng(data);
                    if (png != null)
                    {
                        string baseName = System.IO.Path.GetFileNameWithoutExtension(f);
                        string outName = baseName + ".png";
                        string outPath = System.IO.Path.Combine(outDir, outName);
                        System.IO.File.WriteAllBytes(outPath, png);
                        log($"    -> [OK] {outName}");
                        
                        // Save bpp AND the full original 0x30-byte header so the encoder
                        // can reproduce it verbatim (pal_offset, alpha_flag, etc.)
                        int bppCode = data[3];
                        int bpp = bppCode == 2 ? 24 : 8;
                        byte[] origHdr = new byte[Math.Min(0x30, data.Length)];
                        Array.Copy(data, origHdr, origHdr.Length);
                        string origHdrB64 = Convert.ToBase64String(origHdr);
                        metadata[outName] = new { bpp = bpp, orig_header = origHdrB64 };
                        ok++;
                    }
                    else log($"    -> [WARN] Decode returned null for {System.IO.Path.GetFileName(f)}");
                }
                catch (Exception ex) { log($"    -> [ERROR] {System.IO.Path.GetFileName(f)}: {ex.Message}"); }
            }

            try
            {
                string metaPath = System.IO.Path.Combine(outDir, "kg_metadata.json");
                string json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(metaPath, json);
                log($"[*] Metadata saved to: {metaPath}");
            }
            catch (Exception ex) { log($"[WARN] Failed to save kg_metadata.json: {ex.Message}"); }

            log($"\n[*] Done. {ok} files decoded to PNG. Saved in extracted_png folder.");
        }
    }
}

