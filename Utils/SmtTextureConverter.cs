using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NicheStudioWeirdo.Utils
{
    /// <summary>
    /// Converter for SMT IV / ATLUS 3DS texture formats.
    /// STEX header layout:
    ///   [0-3]   Magic "STEX"
    ///   [4]     Flags (0=normal, 1=variant)
    ///   [8-11]  Version (0x0DE1)
    ///   [12-15] Width (uint32 LE)
    ///   [16-19] Height (uint32 LE)
    ///   [20-23] GL Data Type (0x1401=UNSIGNED_BYTE, 0x8034=UNSIGNED_SHORT_4_4_4_4_REV)
    ///   [24-27] PICA Format  (0x674E+GPU_TEXFORMAT enum index)
    ///   [28-31] Pixel data size in bytes (uint32 LE)
    ///   [32-35] Pixel data start offset (uint32 LE) — typically 0x80 = 128
    /// </summary>
    public static class SmtTextureConverter
    {
        // PICA format codes = 0x674E + GPU_TEXFORMAT index
        private const int PicaBase = 0x674E;
        private const int FmtRGBA8    = 0;   // 0x674E: 32bpp raw, tiled 8x8, RGBA order
        private const int FmtRGB8     = 1;   // 0x674F: 24bpp raw, tiled 8x8
        private const int FmtRGBA5551 = 2;   // 0x6750: 16bpp, tiled 8x8
        private const int FmtRGB565   = 3;   // 0x6751: 16bpp, tiled 8x8
        private const int FmtRGBA4    = 4;   // 0x6752: 16bpp RGBA4444, tiled 8x8
        private const int FmtIA8      = 5;
        private const int FmtRG8      = 6;
        private const int FmtI8       = 7;
        private const int FmtA8       = 8;
        private const int FmtIA4      = 9;
        private const int FmtI4       = 10;
        private const int FmtA4       = 11;
        private const int FmtETC1     = 12;  // 0x675A: ETC1 compressed, 4bpp
        private const int FmtETC1A4   = 13;  // 0x675B: ETC1+A4, 8bpp

        // ETC1 modifier tables (8 codewords × 4 values)
        private static readonly int[,] EtcModTable = {
            {  2,  8,  -2,  -8 },
            {  5, 17,  -5, -17 },
            {  9, 29,  -9, -29 },
            { 13, 42, -13, -42 },
            { 18, 60, -18, -60 },
            { 24, 80, -24, -80 },
            { 33, 106, -33, -106 },
            { 47, 183, -47, -183 }
        };

        // ──────────────────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────────────────

        public static Task ConvertStexToPngAsync(string stexPath, string outputPngPath)
            => Task.Run(() => ConvertStexToPng(stexPath, outputPngPath));

        public static Task ConvertPngToStexAsync(string pngPath, string outputStexPath, string referenceStexPath)
            => Task.Run(() => ConvertPngToStex(pngPath, outputStexPath, referenceStexPath));

        public static Task ConvertTgaToPngAsync(string tgaPath, string outputPngPath)
            => Task.Run(() => ConvertTgaToPng(tgaPath, outputPngPath));

        public static Task ConvertPngToTgaAsync(string pngPath, string outputTgaPath, string referenceTgaPath)
            => Task.Run(() => ConvertPngToTga(pngPath, outputTgaPath, referenceTgaPath));

        // ──────────────────────────────────────────────────────────
        // STEX → PNG
        // ──────────────────────────────────────────────────────────

        private static void ConvertStexToPng(string stexPath, string outputPngPath)
        {
            byte[] file = File.ReadAllBytes(stexPath);
            if (file.Length < 36 || file[0] != 'S' || file[1] != 'T' || file[2] != 'E' || file[3] != 'X')
                throw new Exception("Invalid STEX file (bad magic).");

            int width      = BitConverter.ToInt32(file, 12);
            int height     = BitConverter.ToInt32(file, 16);
            int picaCode   = BitConverter.ToInt32(file, 24);
            int dataOffset = BitConverter.ToInt32(file, 32); // usually 0x80 = 128
            int fmt        = picaCode - PicaBase;

            if (width <= 0 || height <= 0)
                throw new Exception($"Invalid STEX dimensions: {width}×{height}");
            if (dataOffset < 32 || dataOffset >= file.Length)
                throw new Exception($"Invalid STEX data offset: 0x{dataOffset:X}");

            byte[] bgra = new byte[width * height * 4]; // BGRA32 output buffer

            switch (fmt)
            {
                case FmtRGBA8:
                    DecodeRGBA8Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtRGBA4:
                    DecodeRGBA4Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtRGBA5551:
                    DecodeRGBA5551Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtRGB565:
                    DecodeRGB565Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtETC1:
                    DecodeETC1(file, dataOffset, bgra, width, height, false);
                    break;
                case FmtETC1A4:
                    DecodeETC1(file, dataOffset, bgra, width, height, true);
                    break;
                case FmtA8:
                case FmtI8:
                    DecodeA8I8Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtI4:
                case FmtA4:
                    DecodeI4A4Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtIA8:
                    DecodeIA8Tiled(file, dataOffset, bgra, width, height);
                    break;
                case FmtIA4:
                    DecodeIA4Tiled(file, dataOffset, bgra, width, height);
                    break;
                default:
                    throw new Exception($"Unsupported PICA format: 0x{picaCode:X4} (enum {fmt}). Supported: RGBA8, RGBA4, RGBA5551, RGB565, ETC1, ETC1A4, A8, I8, A4, I4, IA8, IA4.");
            }

            WritePng(bgra, width, height, outputPngPath, PixelFormats.Bgra32);
        }

        // ──────────────────────────────────────────────────────────
        // PNG → STEX  (re-encodes as the same pixel format)
        // For ETC1/ETC1A4 the output is converted to RGBA8 (format updated in header).
        // ──────────────────────────────────────────────────────────

        private static void ConvertPngToStex(string pngPath, string outputStexPath, string referenceStexPath)
        {
            byte[] refFile = File.ReadAllBytes(referenceStexPath);
            if (refFile.Length < 36 || refFile[0] != 'S' || refFile[1] != 'T' || refFile[2] != 'E' || refFile[3] != 'X')
                throw new Exception("Invalid reference STEX file.");

            int refWidth  = BitConverter.ToInt32(refFile, 12);
            int refHeight = BitConverter.ToInt32(refFile, 16);
            int picaCode  = BitConverter.ToInt32(refFile, 24);
            int dataOff   = BitConverter.ToInt32(refFile, 32);
            int fmt       = picaCode - PicaBase;

            // Load PNG on STA thread (WPF requirement)
            byte[] pngPixels = ReadPngBgra32(pngPath, out int pngW, out int pngH);

            if (pngW != refWidth || pngH != refHeight)
                throw new Exception($"PNG size ({pngW}×{pngH}) doesn't match reference STEX ({refWidth}×{refHeight}).");

            byte[] pixelData;
            int outPicaCode = picaCode;
            int glType = BitConverter.ToInt32(refFile, 20);

            switch (fmt)
            {
                case FmtRGBA8:
                    pixelData = EncodeRGBA8Tiled(pngPixels, refWidth, refHeight);
                    break;
                case FmtRGBA4:
                    pixelData = EncodeRGBA4Tiled(pngPixels, refWidth, refHeight);
                    break;
                case FmtRGBA5551:
                    pixelData = EncodeRGBA5551Tiled(pngPixels, refWidth, refHeight);
                    break;
                case FmtRGB565:
                    pixelData = EncodeRGB565Tiled(pngPixels, refWidth, refHeight);
                    break;
                case FmtETC1:
                case FmtETC1A4:
                    // ETC1 encoding not supported: downgrade to RGBA8 uncompressed
                    pixelData  = EncodeRGBA8Tiled(pngPixels, refWidth, refHeight);
                    outPicaCode = PicaBase + FmtRGBA8;
                    glType      = 0x1401; // GL_UNSIGNED_BYTE
                    break;
                default:
                    throw new Exception($"Cannot encode back to PICA format 0x{picaCode:X4}.");
            }

            // Build output file: copy original header, patch format+size+data
            int newDataOffset = dataOff; // keep same offset
            byte[] outFile = new byte[newDataOffset + pixelData.Length];

            // Copy header from reference up to dataOff
            Array.Copy(refFile, 0, outFile, 0, Math.Min(dataOff, refFile.Length));

            // Patch header fields
            WriteInt32(outFile, 20, glType);
            WriteInt32(outFile, 24, outPicaCode);
            WriteInt32(outFile, 28, pixelData.Length);

            // Write pixel data
            Array.Copy(pixelData, 0, outFile, newDataOffset, pixelData.Length);

            File.WriteAllBytes(outputStexPath, outFile);
        }

        // ──────────────────────────────────────────────────────────
        // TGA → PNG
        // ──────────────────────────────────────────────────────────

        private static void ConvertTgaToPng(string tgaPath, string outputPngPath)
        {
            byte[] file = File.ReadAllBytes(tgaPath);
            if (file.Length < 18) throw new Exception("TGA file too short.");

            byte idLen     = file[0];
            byte cmType    = file[1];
            byte imgType   = file[2];
            int  width     = BitConverter.ToUInt16(file, 12);
            int  height    = BitConverter.ToUInt16(file, 14);
            byte bpp       = file[16];
            byte imgDesc   = file[17];

            if (cmType != 0) throw new Exception("Color-mapped TGA not supported.");
            if (imgType != 2 && imgType != 10)
                throw new Exception($"Unsupported TGA type {imgType}. Only 2 (raw) and 10 (RLE) supported.");
            if (bpp != 32 && bpp != 24)
                throw new Exception($"Unsupported TGA bpp {bpp}. Only 24/32 supported.");

            bool bottomToTop = (imgDesc & (1 << 5)) == 0; // bit 5 = 0 means bottom-to-top
            int  bpp4        = bpp / 8;
            int  dataStart   = 18 + idLen;

            byte[] bgra = new byte[width * height * 4];

            if (imgType == 2)
            {
                // Uncompressed
                int off = dataStart;
                for (int i = 0; i < width * height && off + bpp4 <= file.Length; i++, off += bpp4)
                {
                    bgra[i * 4 + 0] = file[off + 0]; // B
                    bgra[i * 4 + 1] = file[off + 1]; // G
                    bgra[i * 4 + 2] = file[off + 2]; // R
                    bgra[i * 4 + 3] = bpp4 == 4 ? file[off + 3] : (byte)255;
                }
            }
            else // imgType == 10
            {
                // RLE compressed
                int off = dataStart, px = 0;
                while (px < width * height && off < file.Length)
                {
                    byte hdr   = file[off++];
                    int  count = (hdr & 0x7F) + 1;
                    if ((hdr & 0x80) != 0)
                    {
                        // RLE: one pixel repeated
                        byte b = file[off++], g = file[off++], r = file[off++];
                        byte a = bpp4 == 4 ? file[off++] : (byte)255;
                        for (int i = 0; i < count && px < width * height; i++, px++)
                        { bgra[px*4]=b; bgra[px*4+1]=g; bgra[px*4+2]=r; bgra[px*4+3]=a; }
                    }
                    else
                    {
                        // Raw: sequential pixels
                        for (int i = 0; i < count && px < width * height && off + bpp4 <= file.Length; i++, px++, off += bpp4)
                        { bgra[px*4]=file[off]; bgra[px*4+1]=file[off+1]; bgra[px*4+2]=file[off+2]; bgra[px*4+3]=bpp4==4?file[off+3]:(byte)255; }
                    }
                }
            }

            if (bottomToTop)
            {
                byte[] flipped = new byte[bgra.Length];
                int stride = width * 4;
                for (int y = 0; y < height; y++)
                    Array.Copy(bgra, (height - 1 - y) * stride, flipped, y * stride, stride);
                bgra = flipped;
            }

            WritePng(bgra, width, height, outputPngPath, PixelFormats.Bgra32);
        }

        // ──────────────────────────────────────────────────────────
        // PNG → TGA (writes bottom-to-top, uncompressed, matching original format)
        // ──────────────────────────────────────────────────────────

        private static void ConvertPngToTga(string pngPath, string outputTgaPath, string referenceTgaPath)
        {
            byte[] refFile = File.ReadAllBytes(referenceTgaPath);
            if (refFile.Length < 18) throw new Exception("Reference TGA too short.");

            int refW   = BitConverter.ToUInt16(refFile, 12);
            int refH   = BitConverter.ToUInt16(refFile, 14);
            byte refBpp  = refFile[16];

            byte[] pngPixels = ReadPngBgra32(pngPath, out int pngW, out int pngH);
            if (pngW != refW || pngH != refH)
                throw new Exception($"PNG size ({pngW}×{pngH}) doesn't match reference TGA ({refW}×{refH}).");

            // Flip vertically (PNG is top-to-bottom, TGA output = bottom-to-top to match game originals)
            byte[] flipped = new byte[pngPixels.Length];
            int stride = pngW * 4;
            for (int y = 0; y < pngH; y++)
                Array.Copy(pngPixels, (pngH - 1 - y) * stride, flipped, y * stride, stride);

            using var fs = new FileStream(outputTgaPath, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            // TGA header (18 bytes exactly)
            bw.Write((byte)0);           // id length
            bw.Write((byte)0);           // color map type
            bw.Write((byte)2);           // image type: uncompressed true-color
            bw.Write((short)0);          // color map start
            bw.Write((short)0);          // color map length
            bw.Write((byte)0);           // color map depth
            bw.Write((short)0);          // x origin
            bw.Write((short)0);          // y origin
            bw.Write((short)refW);       // width
            bw.Write((short)refH);       // height
            bw.Write((byte)32);          // bpp
            bw.Write((byte)0x08);        // image descriptor: bottom-to-top (bit5=0), 8 alpha bits

            // Write pixel data: BGRA (TGA convention) — pixels are already BGRA from ReadPngBgra32
            bw.Write(flipped);
        }

        // ──────────────────────────────────────────────────────────
        // PIXEL FORMAT DECODERS
        // ──────────────────────────────────────────────────────────

        // 3DS uncompressed textures use Morton (Z-order) within 8x8 tiles.
        // Tiles are arranged in raster scan order (left→right, top→bottom).

        private static int MortonIndex(int x, int y)
        {
            int r = 0;
            for (int i = 0; i < 4; i++)
            {
                r |= ((x >> i) & 1) << (i * 2);
                r |= ((y >> i) & 1) << (i * 2 + 1);
            }
            return r;
        }

        private static void DecodeRGBA8Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            // PICA RGBA8: 4 bytes per pixel in R,G,B,A order, stored in 8x8 Morton tiles.
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64 * 4;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y) * 4;
                    if (si + 3 >= src.Length) continue;
                    int di = (py * w + px) * 4;
                    // Source: R,G,B,A → dest BGRA32: B,G,R,A
                    dst[di + 0] = src[si + 2]; // B
                    dst[di + 1] = src[si + 1]; // G
                    dst[di + 2] = src[si + 0]; // R
                    dst[di + 3] = src[si + 3]; // A
                }
            }
        }

        private static void DecodeRGBA4Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            // RGBA4444: 2 bytes per pixel, Morton-tiled 8x8. Each uint16 = RRRRGGGGBBBBAAAA.
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y) * 2;
                    if (si + 1 >= src.Length) continue;
                    ushort v = BitConverter.ToUInt16(src, si);
                    // 0xRGBA nibble order in 16-bit LE
                    byte r = (byte)(((v >> 12) & 0xF) * 17);
                    byte g = (byte)(((v >>  8) & 0xF) * 17);
                    byte b = (byte)(((v >>  4) & 0xF) * 17);
                    byte a = (byte)(((v >>  0) & 0xF) * 17);
                    int di = (py * w + px) * 4;
                    dst[di] = b; dst[di+1] = g; dst[di+2] = r; dst[di+3] = a;
                }
            }
        }

        private static void DecodeRGBA5551Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y) * 2;
                    if (si + 1 >= src.Length) continue;
                    ushort v = BitConverter.ToUInt16(src, si);
                    byte r = Expand5((v >> 11) & 0x1F);
                    byte g = Expand5((v >>  6) & 0x1F);
                    byte b = Expand5((v >>  1) & 0x1F);
                    byte a = (byte)((v & 1) == 1 ? 255 : 0);
                    int di = (py * w + px) * 4;
                    dst[di] = b; dst[di+1] = g; dst[di+2] = r; dst[di+3] = a;
                }
            }
        }

        private static void DecodeRGB565Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y) * 2;
                    if (si + 1 >= src.Length) continue;
                    ushort v = BitConverter.ToUInt16(src, si);
                    byte r = Expand5((v >> 11) & 0x1F);
                    byte g = (byte)(((v >> 5) & 0x3F) * 255 / 63);
                    byte b = Expand5(v & 0x1F);
                    int di = (py * w + px) * 4;
                    dst[di] = b; dst[di+1] = g; dst[di+2] = r; dst[di+3] = 255;
                }
            }
        }

        private static void DecodeA8I8Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y);
                    if (si >= src.Length) continue;
                    byte a = src[si];
                    int di = (py * w + px) * 4;
                    dst[di] = a; dst[di+1] = a; dst[di+2] = a; dst[di+3] = a;
                }
            }
        }

        private static void DecodeI4A4Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 32;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int idx = MortonIndex(x, y);
                    int si = tileOff + (idx / 2);
                    if (si >= src.Length) continue;
                    byte b = src[si];
                    byte v = (idx % 2 == 0) ? (byte)(b & 0xF) : (byte)(b >> 4);
                    v = (byte)((v << 4) | v);
                    int di = (py * w + px) * 4;
                    dst[di] = v; dst[di+1] = v; dst[di+2] = v; dst[di+3] = v;
                }
            }
        }

        private static void DecodeIA8Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 128; // 64 pixels * 2 bytes
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y) * 2;
                    if (si + 1 >= src.Length) continue;
                    byte a = src[si];     // I
                    byte i = src[si+1];   // A
                    int di = (py * w + px) * 4;
                    dst[di] = i; dst[di+1] = i; dst[di+2] = i; dst[di+3] = a;
                }
            }
        }

        private static void DecodeIA4Tiled(byte[] src, int srcOff, byte[] dst, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * 64; // 64 pixels * 1 byte
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    if (px >= w || py >= h) continue;
                    int si = tileOff + MortonIndex(x, y);
                    if (si >= src.Length) continue;
                    byte b = src[si];
                    byte a = (byte)((b & 0xF) * 17);
                    byte i = (byte)(((b >> 4) & 0xF) * 17);
                    int di = (py * w + px) * 4;
                    dst[di] = i; dst[di+1] = i; dst[di+2] = i; dst[di+3] = a;
                }
            }
        }

        // ETC1 and ETC1A4 decoder.
        // Blocks are arranged in raster scan order (left→right, top→bottom) for compressed textures.
        // ETC1: 8 bytes per 4×4 block.
        // ETC1A4: 16 bytes per 4×4 block — first 8 bytes = A4 alpha, next 8 bytes = ETC1 color.
        private static void DecodeETC1(byte[] src, int srcOff, byte[] dst, int w, int h, bool hasAlpha)
        {
            int tilesX = (w + 7) / 8;
            int tilesY = (h + 7) / 8;
            int blockBytes = hasAlpha ? 16 : 8;
            int tileBytes = 4 * blockBytes; // four 4x4 blocks in an 8x8 tile

            // 4 blocks in Morton order inside an 8x8 tile
            int[] bxOffset = { 0, 4, 0, 4 };
            int[] byOffset = { 0, 0, 4, 4 };

            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = srcOff + (ty * tilesX + tx) * tileBytes;

                for (int b = 0; b < 4; b++)
                {
                    int off = tileOff + b * blockBytes;
                    int alphaOff = hasAlpha ? off : -1;
                    int etc1Off  = hasAlpha ? off + 8 : off;

                    if (etc1Off + 8 > src.Length) continue;

                    DecodeETC1Block(src, etc1Off, hasAlpha ? src : null, alphaOff,
                                    dst, tx * 8 + bxOffset[b], ty * 8 + byOffset[b], w, h);
                }
            }
        }

        private static void DecodeETC1Block(
            byte[] etc, int etcOff,
            byte[]? alphaSrc, int alphaOff,
            byte[] dst, int bx, int by, int imgW, int imgH)
        {
            byte b3   = etc[etcOff + 3];
            bool diff = (b3 & 0x02) != 0;
            bool flip = (b3 & 0x01) != 0;
            int  cw0  = (b3 >> 5) & 0x07;
            int  cw1  = (b3 >> 2) & 0x07;

            byte r0, g0, b0, r1, g1, b1;
            if (diff)
            {
                int R1 = (etc[etcOff] >> 3) & 0x1F;
                int dR = etc[etcOff] & 0x07; if (dR > 3) dR -= 8;
                int G1 = (etc[etcOff+1] >> 3) & 0x1F;
                int dG = etc[etcOff+1] & 0x07; if (dG > 3) dG -= 8;
                int B1 = (etc[etcOff+2] >> 3) & 0x1F;
                int dB = etc[etcOff+2] & 0x07; if (dB > 3) dB -= 8;
                r0 = Expand5Byte(R1); g0 = Expand5Byte(G1); b0 = Expand5Byte(B1);
                r1 = Expand5Byte(R1+dR); g1 = Expand5Byte(G1+dG); b1 = Expand5Byte(B1+dB);
            }
            else
            {
                r0 = (byte)(((etc[etcOff]   >> 4) & 0xF) * 17);
                r1 = (byte)(( etc[etcOff]         & 0xF) * 17);
                g0 = (byte)(((etc[etcOff+1] >> 4) & 0xF) * 17);
                g1 = (byte)(( etc[etcOff+1]       & 0xF) * 17);
                b0 = (byte)(((etc[etcOff+2] >> 4) & 0xF) * 17);
                b1 = (byte)(( etc[etcOff+2]       & 0xF) * 17);
            }

            // Pixel indices: bytes 4–7
            // MSB of each 2-bit index is in bytes 4–5 (combined as 16-bit), LSB in bytes 6–7.
            // Pixels are ordered column-major: col*4 + row (from bit 15 downward).
            uint msbs = (uint)((etc[etcOff+4] << 8) | etc[etcOff+5]);
            uint lsbs = (uint)((etc[etcOff+6] << 8) | etc[etcOff+7]);

            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
            {
                int px = bx + col, py = by + row;
                if (px >= imgW || py >= imgH) continue;

                bool sub1 = flip ? (row >= 2) : (col >= 2);
                byte cr = sub1 ? r1 : r0, cg = sub1 ? g1 : g0, cb = sub1 ? b1 : b0;
                int  tw = sub1 ? cw1 : cw0;

                int bitPos = 15 - (col * 4 + row);
                int msb = (int)((msbs >> bitPos) & 1);
                int lsb = (int)((lsbs >> bitPos) & 1);
                int mod = EtcModTable[tw, (msb << 1) | lsb];

                byte fr = Clamp(cr + mod), fg = Clamp(cg + mod), fb = Clamp(cb + mod);

                byte a = 255;
                if (alphaSrc != null && alphaOff >= 0)
                {
                    // A4 alpha: column-major, lower nibble first
                    int pos = col * 4 + row;
                    int ab  = alphaOff + pos / 2;
                    byte nib = (pos % 2 == 0)
                        ? (byte)(alphaSrc[ab] & 0x0F)
                        : (byte)((alphaSrc[ab] >> 4) & 0x0F);
                    a = (byte)((nib << 4) | nib);
                }

                int di = (py * imgW + px) * 4;
                dst[di+0] = fb; dst[di+1] = fg; dst[di+2] = fr; dst[di+3] = a;
            }
        }

        // ──────────────────────────────────────────────────────────
        // PIXEL FORMAT ENCODERS (PNG → STEX pixel data)
        // ──────────────────────────────────────────────────────────

        private static byte[] EncodeRGBA8Tiled(byte[] bgraPixels, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            byte[] dst = new byte[tilesX * tilesY * 64 * 4];
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = (ty * tilesX + tx) * 64 * 4;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    int si = (py < h && px < w) ? (py * w + px) * 4 : -1;
                    int di = tileOff + MortonIndex(x, y) * 4;
                    if (si >= 0)
                    {
                        // BGRA → RGBA
                        dst[di+0] = bgraPixels[si+2]; // R
                        dst[di+1] = bgraPixels[si+1]; // G
                        dst[di+2] = bgraPixels[si+0]; // B
                        dst[di+3] = bgraPixels[si+3]; // A
                    }
                }
            }
            return dst;
        }

        private static byte[] EncodeRGBA4Tiled(byte[] bgraPixels, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            byte[] dst = new byte[tilesX * tilesY * 64 * 2];
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    int si = (py < h && px < w) ? (py * w + px) * 4 : -1;
                    int di = tileOff + MortonIndex(x, y) * 2;
                    if (si >= 0)
                    {
                        byte r = (byte)((bgraPixels[si+2] >> 4) & 0xF);
                        byte g = (byte)((bgraPixels[si+1] >> 4) & 0xF);
                        byte b = (byte)((bgraPixels[si+0] >> 4) & 0xF);
                        byte a = (byte)((bgraPixels[si+3] >> 4) & 0xF);
                        ushort v = (ushort)((r << 12) | (g << 8) | (b << 4) | a);
                        dst[di]   = (byte)(v & 0xFF);
                        dst[di+1] = (byte)(v >> 8);
                    }
                }
            }
            return dst;
        }

        private static byte[] EncodeRGBA5551Tiled(byte[] bgraPixels, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            byte[] dst = new byte[tilesX * tilesY * 64 * 2];
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    int si = (py < h && px < w) ? (py * w + px) * 4 : -1;
                    int di = tileOff + MortonIndex(x, y) * 2;
                    if (si >= 0)
                    {
                        int r = bgraPixels[si+2] >> 3;
                        int g = bgraPixels[si+1] >> 3;
                        int b = bgraPixels[si+0] >> 3;
                        int a = bgraPixels[si+3] >= 128 ? 1 : 0;
                        ushort v = (ushort)((r << 11) | (g << 6) | (b << 1) | a);
                        dst[di]   = (byte)(v & 0xFF);
                        dst[di+1] = (byte)(v >> 8);
                    }
                }
            }
            return dst;
        }

        private static byte[] EncodeRGB565Tiled(byte[] bgraPixels, int w, int h)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            byte[] dst = new byte[tilesX * tilesY * 64 * 2];
            for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int tileOff = (ty * tilesX + tx) * 64 * 2;
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    int px = tx * 8 + x, py = ty * 8 + y;
                    int si = (py < h && px < w) ? (py * w + px) * 4 : -1;
                    int di = tileOff + MortonIndex(x, y) * 2;
                    if (si >= 0)
                    {
                        int r = bgraPixels[si+2] >> 3;
                        int g = bgraPixels[si+1] >> 2;
                        int b = bgraPixels[si+0] >> 3;
                        ushort v = (ushort)((r << 11) | (g << 5) | b);
                        dst[di]   = (byte)(v & 0xFF);
                        dst[di+1] = (byte)(v >> 8);
                    }
                }
            }
            return dst;
        }

        // ──────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────

        private static byte[] ReadPngBgra32(string pngPath, out int width, out int height)
        {
            // Must be called on a thread that can use WPF types (or UI thread).
            // Task.Run is fine since BitmapImage/FormatConvertedBitmap work off-thread
            // as long as we don't touch the visual tree.
            var bmp = new BitmapImage(new Uri(Path.GetFullPath(pngPath), UriKind.Absolute));
            bmp.Freeze();
            var conv = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
            conv.Freeze();
            width  = conv.PixelWidth;
            height = conv.PixelHeight;
            byte[] pixels = new byte[width * height * 4];
            conv.CopyPixels(pixels, width * 4, 0);
            return pixels;
        }

        private static void WritePng(byte[] pixels, int width, int height, string path, PixelFormat fmt)
        {
            var bmp = BitmapSource.Create(width, height, 96, 96, fmt, null, pixels, width * (fmt.BitsPerPixel / 8));
            bmp.Freeze();
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = new FileStream(path, FileMode.Create);
            enc.Save(fs);
        }

        private static void WriteInt32(byte[] buf, int offset, int value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Array.Copy(b, 0, buf, offset, 4);
        }

        private static byte Clamp(int v) => v < 0 ? (byte)0 : v > 255 ? (byte)255 : (byte)v;
        private static byte Expand5Byte(int v) => (byte)((v << 3) | (v >> 2));
        private static byte Expand5(int v) => (byte)((v << 3) | (v >> 2));
    }
}
