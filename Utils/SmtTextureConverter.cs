using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NicheStudioWeirdo.Utils
{
    public static class SmtTextureConverter
    {
        // ─────────────────────────────────────────────────────────
        // STEX HEADER LAYOUT (offset → field)
        //   0: Magic "STEX"
        //  12: Width  (int32)
        //  16: Height (int32)
        //  20: GL Type (uint32) — OpenGL pixel type constant
        //  24: PICA Code (uint32) — GPU texture format code
        //  28: Data Size (int32) — raw pixel data byte count
        //  32: Data Offset (int32) — byte offset where pixel data begins (usually 0x80)
        // ─────────────────────────────────────────────────────────

        // PICA texture format codes (at offset 24 in STEX header)
        private const uint PicaETC1   = 0x675A;
        private const uint PicaETC1A4 = 0x675B;

        // GL type constants (at offset 20 in STEX header)
        private const uint GL_UNSIGNED_BYTE           = 0x1401;
        private const uint GL_UNSIGNED_SHORT_4_4_4_4  = 0x8033;
        private const uint GL_UNSIGNED_SHORT_5_5_5_1  = 0x8034;
        private const uint GL_UNSIGNED_SHORT_5_6_5    = 0x8363;
        private const uint GL_LA4                     = 0x6760;
        private const uint GL_L4_A4                   = 0x6761;

        // ETC1 modifier table
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

        // SPICA 8×8 tile swizzle LUT (Morton Z-order within an 8×8 tile)
        private static readonly int[] SwizzleLUT = {
             0,  1,  8,  9,  2,  3, 10, 11,
            16, 17, 24, 25, 18, 19, 26, 27,
             4,  5, 12, 13,  6,  7, 14, 15,
            20, 21, 28, 29, 22, 23, 30, 31,
            32, 33, 40, 41, 34, 35, 42, 43,
            48, 49, 56, 57, 50, 51, 58, 59,
            36, 37, 44, 45, 38, 39, 46, 47,
            52, 53, 60, 61, 54, 55, 62, 63
        };

        // Internal format enum for cleaner switch statements
        private enum TexFmt { RGBA8, RGB8, RGBA4, RGBA5551, RGB565, LA8, LA4, L8, A8, L4, A4, ETC1, ETC1A4 }

        // ─────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────

        public static Task ConvertStexToPngAsync(string stexPath, string outputPngPath)
            => Task.Run(() => ConvertStexToPng(stexPath, outputPngPath));

        public static Task ConvertPngToStexAsync(string pngPath, string outputStexPath, string referenceStexPath)
            => Task.Run(() => ConvertPngToStex(pngPath, outputStexPath, referenceStexPath));

        public static Task ConvertTgaToPngAsync(string tgaPath, string outputPngPath)
            => Task.Run(() => ConvertTgaToPng(tgaPath, outputPngPath));

        public static Task ConvertPngToTgaAsync(string pngPath, string outputTgaPath, string referenceTgaPath)
            => Task.Run(() => ConvertPngToTga(pngPath, outputTgaPath, referenceTgaPath));

        // ─────────────────────────────────────────────────────────
        // STEX → PNG
        // ─────────────────────────────────────────────────────────

        private static void ConvertStexToPng(string stexPath, string outputPngPath)
        {
            byte[] file = File.ReadAllBytes(stexPath);
            if (file.Length < 36 || file[0] != 'S' || file[1] != 'T' || file[2] != 'E' || file[3] != 'X')
                throw new Exception("Invalid STEX file (bad magic).");

            int width      = BitConverter.ToInt32(file, 12);
            int height     = BitConverter.ToInt32(file, 16);
            uint glType    = BitConverter.ToUInt32(file, 20);
            uint picaCode  = BitConverter.ToUInt32(file, 24);
            int dataSize   = BitConverter.ToInt32(file, 28);
            int dataOffset = BitConverter.ToInt32(file, 32);

            if (width <= 0 || height <= 0)
                throw new Exception($"Invalid STEX dimensions: {width}×{height}");
            if (dataOffset < 32 || dataOffset >= file.Length)
                throw new Exception($"Invalid STEX data offset: 0x{dataOffset:X}");

            TexFmt fmt = DetermineFormat(glType, picaCode, width, height, dataSize);

            byte[] bgra = new byte[width * height * 4];

            if (fmt == TexFmt.ETC1 || fmt == TexFmt.ETC1A4)
            {
                DecodeETC1(file, dataOffset, bgra, width, height, fmt == TexFmt.ETC1A4);
            }
            else
            {
                DecodeSwizzled(file, dataOffset, bgra, width, height, fmt);
            }

            WritePng(bgra, width, height, outputPngPath, PixelFormats.Bgra32);
        }

        // ─────────────────────────────────────────────────────────
        // PNG → STEX
        // ─────────────────────────────────────────────────────────

        private static void ConvertPngToStex(string pngPath, string outputStexPath, string referenceStexPath)
        {
            byte[] refFile = File.ReadAllBytes(referenceStexPath);
            if (refFile.Length < 36 || refFile[0] != 'S' || refFile[1] != 'T' || refFile[2] != 'E' || refFile[3] != 'X')
                throw new Exception("Invalid reference STEX file.");

            int refWidth   = BitConverter.ToInt32(refFile, 12);
            int refHeight  = BitConverter.ToInt32(refFile, 16);
            uint glType    = BitConverter.ToUInt32(refFile, 20);
            uint picaCode  = BitConverter.ToUInt32(refFile, 24);
            int dataSize   = BitConverter.ToInt32(refFile, 28);
            int dataOff    = BitConverter.ToInt32(refFile, 32);

            TexFmt fmt = DetermineFormat(glType, picaCode, refWidth, refHeight, dataSize);

            byte[] pngPixels = ReadPngBgra32(pngPath, out int pngW, out int pngH);
            if (pngW != refWidth || pngH != refHeight)
                throw new Exception($"PNG size ({pngW}×{pngH}) doesn't match reference STEX ({refWidth}×{refHeight}).");

            byte[] pixelData;
            uint outGlType = glType;
            uint outPicaCode = picaCode;

            if (fmt == TexFmt.ETC1 || fmt == TexFmt.ETC1A4)
            {
                // Cannot re-encode ETC1 — convert to RGBA8 instead
                pixelData = EncodeSwizzled(pngPixels, refWidth, refHeight, TexFmt.RGBA8);
                outGlType = GL_UNSIGNED_BYTE;
                // Keep same PICA code — the game should still accept RGBA8 data
                // Update data size header field
            }
            else
            {
                pixelData = EncodeSwizzled(pngPixels, refWidth, refHeight, fmt);
            }

            byte[] outFile = new byte[dataOff + pixelData.Length];
            Array.Copy(refFile, 0, outFile, 0, Math.Min(dataOff, refFile.Length));
            WriteInt32(outFile, 20, (int)outGlType);
            WriteInt32(outFile, 24, (int)outPicaCode);
            WriteInt32(outFile, 28, pixelData.Length);
            Array.Copy(pixelData, 0, outFile, dataOff, pixelData.Length);
            File.WriteAllBytes(outputStexPath, outFile);
        }

        // ─────────────────────────────────────────────────────────
        // FORMAT DETECTION
        // Uses GL type (offset 20) + computed BPP to determine format.
        // For ETC1/ETC1A4, uses PICA code (offset 24) as differentiator.
        // ─────────────────────────────────────────────────────────

        private static TexFmt DetermineFormat(uint glType, uint picaCode, int w, int h, int dataSize)
        {
            // GL type directly specifies some formats
            switch (glType)
            {
                case GL_UNSIGNED_SHORT_4_4_4_4: return TexFmt.RGBA4;
                case GL_UNSIGNED_SHORT_5_5_5_1: return TexFmt.RGBA5551;
                case GL_UNSIGNED_SHORT_5_6_5:   return TexFmt.RGB565;
                case GL_LA4:                    return TexFmt.LA4;
                case GL_L4_A4:                  return TexFmt.L4;
            }

            // For GL_UNSIGNED_BYTE (0x1401), use BPP + PICA code
            int pixels = w * h;
            if (pixels <= 0) throw new Exception("Invalid texture dimensions for format detection.");

            int bpp = (dataSize * 8) / pixels;

            // Check for ETC1/ETC1A4 by PICA code, BUT verify BPP so we don't misidentify 
            // textures we re-encoded as RGBA8 (which keep the ETC1 PICA code for the game)
            if (picaCode == PicaETC1 && bpp == 4)  return TexFmt.ETC1;
            if (picaCode == PicaETC1A4 && bpp == 8) return TexFmt.ETC1A4;

            // Fall back to BPP-based detection
            switch (bpp)
            {
                case 32: return TexFmt.RGBA8;
                case 24: return TexFmt.RGB8;
                case 16: return TexFmt.LA8;
                case 8:  return TexFmt.L8;
                case 4:  return TexFmt.L4;
                default:
                    throw new Exception($"Cannot determine STEX format: GL=0x{glType:X4}, PICA=0x{picaCode:X4}, {w}×{h}, dataSize={dataSize}, bpp={bpp}");
            }
        }

        // ─────────────────────────────────────────────────────────
        // GENERIC SWIZZLED DECODER (all non-ETC formats)
        // Uses SPICA's SwizzleLUT for 8×8 Morton tiling.
        // ─────────────────────────────────────────────────────────

        private static void DecodeSwizzled(byte[] src, int srcOff, byte[] dst, int w, int h, TexFmt fmt)
        {
            int iOffs = srcOff;

            for (int tileY = 0; tileY < h; tileY += 8)
            for (int tileX = 0; tileX < w; tileX += 8)
            for (int px = 0; px < 64; px++)
            {
                int lx = SwizzleLUT[px] & 7;
                int ly = (SwizzleLUT[px] - lx) >> 3;

                int outX = tileX + lx;
                int outY = tileY + ly;

                if (outX >= w || outY < 0 || outY >= h)
                {
                    iOffs += GetBytesPerPixel(fmt, iOffs);
                    continue;
                }

                int di = (outY * w + outX) * 4;

                switch (fmt)
                {
                    case TexFmt.RGBA8:
                        if (iOffs + 3 >= src.Length) return;
                        dst[di + 0] = src[iOffs + 3]; // B
                        dst[di + 1] = src[iOffs + 2]; // G
                        dst[di + 2] = src[iOffs + 1]; // R
                        dst[di + 3] = src[iOffs + 0]; // A
                        iOffs += 4;
                        break;

                    case TexFmt.RGB8:
                        if (iOffs + 2 >= src.Length) return;
                        dst[di + 0] = src[iOffs + 2]; // B
                        dst[di + 1] = src[iOffs + 1]; // G
                        dst[di + 2] = src[iOffs + 0]; // R
                        dst[di + 3] = 255;
                        iOffs += 3;
                        break;

                    case TexFmt.RGBA4:
                        if (iOffs + 1 >= src.Length) return;
                        {
                            ushort v = (ushort)(src[iOffs] | (src[iOffs + 1] << 8));
                            dst[di + 2] = (byte)(((v >> 12) & 0xF) * 17); // R
                            dst[di + 1] = (byte)(((v >> 8) & 0xF) * 17);  // G
                            dst[di + 0] = (byte)(((v >> 4) & 0xF) * 17);  // B
                            dst[di + 3] = (byte)((v & 0xF) * 17);         // A
                        }
                        iOffs += 2;
                        break;

                    case TexFmt.RGBA5551:
                        if (iOffs + 1 >= src.Length) return;
                        {
                            ushort v = (ushort)(src[iOffs] | (src[iOffs + 1] << 8));
                            dst[di + 2] = Expand5((v >> 11) & 0x1F); // R
                            dst[di + 1] = Expand5((v >> 6) & 0x1F);  // G
                            dst[di + 0] = Expand5((v >> 1) & 0x1F);  // B
                            dst[di + 3] = (byte)((v & 1) == 1 ? 255 : 0);
                        }
                        iOffs += 2;
                        break;

                    case TexFmt.RGB565:
                        if (iOffs + 1 >= src.Length) return;
                        {
                            ushort v = (ushort)(src[iOffs] | (src[iOffs + 1] << 8));
                            dst[di + 2] = Expand5((v >> 11) & 0x1F);            // R
                            dst[di + 1] = (byte)(((v >> 5) & 0x3F) * 255 / 63); // G
                            dst[di + 0] = Expand5(v & 0x1F);                    // B
                            dst[di + 3] = 255;
                        }
                        iOffs += 2;
                        break;

                    case TexFmt.LA8:
                        if (iOffs + 1 >= src.Length) return;
                        {
                            byte a = src[iOffs];
                            byte l = src[iOffs + 1];
                            dst[di + 0] = l; // B
                            dst[di + 1] = l; // G
                            dst[di + 2] = l; // R
                            dst[di + 3] = a;
                        }
                        iOffs += 2;
                        break;

                    case TexFmt.LA4:
                        if (iOffs >= src.Length) return;
                        {
                            byte val = src[iOffs];
                            byte l = (byte)(((val >> 4) & 0xF) * 17);
                            byte a = (byte)((val & 0xF) * 17);
                            dst[di + 0] = l;
                            dst[di + 1] = l;
                            dst[di + 2] = l;
                            dst[di + 3] = a;
                        }
                        iOffs++;
                        break;

                    case TexFmt.L8:
                        if (iOffs >= src.Length) return;
                        dst[di + 0] = src[iOffs];
                        dst[di + 1] = src[iOffs];
                        dst[di + 2] = src[iOffs];
                        dst[di + 3] = 255;
                        iOffs++;
                        break;

                    case TexFmt.A8:
                        if (iOffs >= src.Length) return;
                        dst[di + 0] = 255;
                        dst[di + 1] = 255;
                        dst[di + 2] = 255;
                        dst[di + 3] = src[iOffs];
                        iOffs++;
                        break;

                    case TexFmt.L4:
                        {
                            int byteIdx = iOffs >> 1;
                            if (byteIdx >= src.Length) return;
                            int shift = (iOffs & 1) << 2;
                            byte l = (byte)(((src[byteIdx] >> shift) & 0xF) * 17);
                            dst[di + 0] = l;
                            dst[di + 1] = l;
                            dst[di + 2] = l;
                            dst[di + 3] = 255;
                        }
                        iOffs++;
                        break;

                    case TexFmt.A4:
                        {
                            int byteIdx = iOffs >> 1;
                            if (byteIdx >= src.Length) return;
                            int shift = (iOffs & 1) << 2;
                            byte a = (byte)(((src[byteIdx] >> shift) & 0xF) * 17);
                            dst[di + 0] = 255;
                            dst[di + 1] = 255;
                            dst[di + 2] = 255;
                            dst[di + 3] = a;
                        }
                        iOffs++;
                        break;
                }
            }
        }

        private static int GetBytesPerPixel(TexFmt fmt, int iOffs)
        {
            switch (fmt)
            {
                case TexFmt.RGBA8: return 4;
                case TexFmt.RGB8:  return 3;
                case TexFmt.RGBA4: case TexFmt.RGBA5551: case TexFmt.RGB565: case TexFmt.LA8: return 2;
                case TexFmt.LA4: case TexFmt.L8: case TexFmt.A8: return 1;
                case TexFmt.L4: case TexFmt.A4: return 1; // for iOffs tracking (nibble count)
                default: return 1;
            }
        }

        // ─────────────────────────────────────────────────────────
        // GENERIC SWIZZLED ENCODER (all non-ETC formats)
        // ─────────────────────────────────────────────────────────

        private static byte[] EncodeSwizzled(byte[] bgra, int w, int h, TexFmt fmt)
        {
            int tilesX = (w + 7) / 8, tilesY = (h + 7) / 8;
            int totalPixels = tilesX * tilesY * 64;
            int bytesNeeded;

            switch (fmt)
            {
                case TexFmt.RGBA8: bytesNeeded = totalPixels * 4; break;
                case TexFmt.RGB8:  bytesNeeded = totalPixels * 3; break;
                case TexFmt.RGBA4: case TexFmt.RGBA5551: case TexFmt.RGB565: case TexFmt.LA8:
                    bytesNeeded = totalPixels * 2; break;
                case TexFmt.LA4: case TexFmt.L8: case TexFmt.A8:
                    bytesNeeded = totalPixels; break;
                case TexFmt.L4: case TexFmt.A4:
                    bytesNeeded = totalPixels / 2; break;
                default: bytesNeeded = totalPixels * 4; break;
            }

            byte[] dst = new byte[bytesNeeded];
            int oOffs = 0;

            for (int tileY = 0; tileY < h; tileY += 8)
            for (int tileX = 0; tileX < w; tileX += 8)
            for (int px = 0; px < 64; px++)
            {
                int lx = SwizzleLUT[px] & 7;
                int ly = (SwizzleLUT[px] - lx) >> 3;

                int srcX = tileX + lx;
                int srcY = tileY + ly;

                byte b = 0, g = 0, r = 0, a = 255;
                if (srcX < w && srcY >= 0 && srcY < h)
                {
                    int si = (srcY * w + srcX) * 4;
                    b = bgra[si + 0];
                    g = bgra[si + 1];
                    r = bgra[si + 2];
                    a = bgra[si + 3];
                }

                switch (fmt)
                {
                    case TexFmt.RGBA8:
                        dst[oOffs + 0] = a;
                        dst[oOffs + 1] = r;
                        dst[oOffs + 2] = g;
                        dst[oOffs + 3] = b;
                        oOffs += 4;
                        break;

                    case TexFmt.RGB8:
                        dst[oOffs + 0] = r;
                        dst[oOffs + 1] = g;
                        dst[oOffs + 2] = b;
                        oOffs += 3;
                        break;

                    case TexFmt.RGBA4:
                        {
                            ushort v = (ushort)(((r >> 4) << 12) | ((g >> 4) << 8) | ((b >> 4) << 4) | (a >> 4));
                            dst[oOffs] = (byte)(v & 0xFF);
                            dst[oOffs + 1] = (byte)(v >> 8);
                        }
                        oOffs += 2;
                        break;

                    case TexFmt.RGBA5551:
                        {
                            ushort v = (ushort)(((r >> 3) << 11) | ((g >> 3) << 6) | ((b >> 3) << 1) | (a >= 128 ? 1 : 0));
                            dst[oOffs] = (byte)(v & 0xFF);
                            dst[oOffs + 1] = (byte)(v >> 8);
                        }
                        oOffs += 2;
                        break;

                    case TexFmt.RGB565:
                        {
                            ushort v = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
                            dst[oOffs] = (byte)(v & 0xFF);
                            dst[oOffs + 1] = (byte)(v >> 8);
                        }
                        oOffs += 2;
                        break;

                    case TexFmt.LA8:
                        {
                            byte l = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                            dst[oOffs] = a;
                            dst[oOffs + 1] = l;
                        }
                        oOffs += 2;
                        break;

                    case TexFmt.LA4:
                        {
                            byte l = (byte)(((r * 299 + g * 587 + b * 114) / 1000) >> 4);
                            dst[oOffs] = (byte)((l << 4) | (a >> 4));
                        }
                        oOffs++;
                        break;

                    case TexFmt.L8:
                        dst[oOffs] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                        oOffs++;
                        break;

                    case TexFmt.A8:
                        dst[oOffs] = a;
                        oOffs++;
                        break;

                    case TexFmt.L4:
                        {
                            byte l = (byte)(((r * 299 + g * 587 + b * 114) / 1000) >> 4);
                            int byteIdx = oOffs >> 1;
                            int shift = (oOffs & 1) << 2;
                            dst[byteIdx] = (byte)((dst[byteIdx] & ~(0xF << shift)) | (l << shift));
                        }
                        oOffs++;
                        break;

                    case TexFmt.A4:
                        {
                            byte a4 = (byte)(a >> 4);
                            int byteIdx = oOffs >> 1;
                            int shift = (oOffs & 1) << 2;
                            dst[byteIdx] = (byte)((dst[byteIdx] & ~(0xF << shift)) | (a4 << shift));
                        }
                        oOffs++;
                        break;
                }
            }

            return dst;
        }

        // ─────────────────────────────────────────────────────────
        // ETC1 / ETC1A4 DECODER
        // ─────────────────────────────────────────────────────────

        private static readonly int[] SubTileX = { 0, 4, 0, 4 };
        private static readonly int[] SubTileY = { 0, 0, 4, 4 };

        private static void DecodeETC1(byte[] src, int srcOff, byte[] dst, int w, int h, bool hasAlpha)
        {
            int pos = srcOff;

            for (int tileY = 0; tileY < h; tileY += 8)
            for (int tileX = 0; tileX < w; tileX += 8)
            {
                for (int sub = 0; sub < 4; sub++)
                {
                    ulong alphaBlock = 0xFFFFFFFFFFFFFFFFUL;
                    if (hasAlpha)
                    {
                        if (pos + 8 > src.Length) return;
                        alphaBlock = BitConverter.ToUInt64(src, pos);
                        pos += 8;
                    }

                    if (pos + 8 > src.Length) return;

                    uint blockLow  = (uint)((src[pos + 0] << 24) | (src[pos + 1] << 16) | (src[pos + 2] << 8) | src[pos + 3]);
                    uint blockHigh = (uint)((src[pos + 4] << 24) | (src[pos + 5] << 16) | (src[pos + 6] << 8) | src[pos + 7]);
                    pos += 8;

                    bool flip = (blockHigh & 0x1000000) != 0;
                    bool diff = (blockHigh & 0x2000000) != 0;

                    // 3DS ETC1: blockHigh byte layout is [flags][B][G][R] (not [flags][R][G][B])
                    // bits 23-16 = B, bits 15-8 = G, bits 7-0 = R
                    uint r1, g1, b1, r2, g2, b2;

                    if (diff)
                    {
                        r1 = blockHigh & 0xF8;
                        g1 = (blockHigh & 0x00f800) >> 8;
                        b1 = (blockHigh & 0xf80000) >> 16;

                        int dr = (int)(blockHigh & 0x07); if (dr > 3) dr -= 8;
                        int dg = (int)((blockHigh >> 8) & 0x07); if (dg > 3) dg -= 8;
                        int db = (int)((blockHigh >> 16) & 0x07); if (db > 3) db -= 8;

                        r2 = (uint)(((int)(r1 >> 3) + dr) & 0x1F);
                        g2 = (uint)(((int)(g1 >> 3) + dg) & 0x1F);
                        b2 = (uint)(((int)(b1 >> 3) + db) & 0x1F);

                        r1 |= r1 >> 5;
                        g1 |= g1 >> 5;
                        b1 |= b1 >> 5;

                        r2 = (r2 << 3) | (r2 >> 2);
                        g2 = (g2 << 3) | (g2 >> 2);
                        b2 = (b2 << 3) | (b2 >> 2);
                    }
                    else
                    {
                        r1 = blockHigh & 0xF0; r1 |= r1 >> 4;
                        g1 = (blockHigh >> 8) & 0xF0; g1 |= g1 >> 4;
                        b1 = (blockHigh >> 16) & 0xF0; b1 |= b1 >> 4;

                        r2 = (blockHigh & 0x0F) << 4; r2 |= r2 >> 4;
                        g2 = ((blockHigh >> 8) & 0x0F) << 4; g2 |= g2 >> 4;
                        b2 = ((blockHigh >> 16) & 0x0F) << 4; b2 |= b2 >> 4;
                    }

                    uint table1 = (blockHigh >> 29) & 7;
                    uint table2 = (blockHigh >> 26) & 7;

                    int subBaseX = tileX + SubTileX[sub];
                    int subBaseY = tileY + SubTileY[sub];

                    if (!flip)
                    {
                        for (int py = 0; py < 4; py++)
                        for (int px = 0; px < 4; px++)
                        {
                            uint tr = px < 2 ? r1 : r2, tg = px < 2 ? g1 : g2, tb = px < 2 ? b1 : b2;
                            uint tt = px < 2 ? table1 : table2;
                            WriteEtcPixel(dst, w, h, subBaseX + px, subBaseY + py, tr, tg, tb, px, py, blockLow, tt, alphaBlock, hasAlpha);
                        }
                    }
                    else
                    {
                        for (int py = 0; py < 4; py++)
                        for (int px = 0; px < 4; px++)
                        {
                            uint tr = py < 2 ? r1 : r2, tg = py < 2 ? g1 : g2, tb = py < 2 ? b1 : b2;
                            uint tt = py < 2 ? table1 : table2;
                            WriteEtcPixel(dst, w, h, subBaseX + px, subBaseY + py, tr, tg, tb, px, py, blockLow, tt, alphaBlock, hasAlpha);
                        }
                    }
                }
            }
        }

        private static void WriteEtcPixel(byte[] dst, int w, int h, int outX, int outY,
            uint r, uint g, uint b, int px, int py, uint blockLow, uint table,
            ulong alphaBlock, bool hasAlpha)
        {
            if (outX >= w || outY >= h) return;

            int index = px * 4 + py;
            int lsb, msb;
            if (index < 8)
            {
                lsb = (int)((blockLow >> (index + 24)) & 1);
                msb = (int)((blockLow >> (index + 8)) & 1);
            }
            else
            {
                lsb = (int)((blockLow >> (index + 8)) & 1);
                msb = (int)((blockLow >> (index - 8)) & 1);
            }
            int modifier = EtcModTable[table, lsb + msb * 2];

            int dstIdx = (outY * w + outX) * 4;
            dst[dstIdx + 2] = Clamp((int)r + modifier); // R
            dst[dstIdx + 1] = Clamp((int)g + modifier); // G
            dst[dstIdx + 0] = Clamp((int)b + modifier); // B

            if (hasAlpha)
            {
                int alphaShift = ((py & 3) * 4 + (px & 3)) * 4;
                byte a4 = (byte)((alphaBlock >> alphaShift) & 0xF);
                dst[dstIdx + 3] = (byte)((a4 << 4) | a4);
            }
            else
            {
                dst[dstIdx + 3] = 255;
            }
        }

        // ─────────────────────────────────────────────────────────
        // TGA ↔ PNG CONVERTERS
        // ─────────────────────────────────────────────────────────

        public static void ConvertTgaToPng(string tgaPath, string outputPngPath)
        {
            byte[] fileBytes = File.ReadAllBytes(tgaPath);
            if (fileBytes.Length < 18) throw new Exception("TGA file too short.");

            byte idLength = fileBytes[0];
            byte colorMapType = fileBytes[1];
            byte imageType = fileBytes[2];
            int width = BitConverter.ToUInt16(fileBytes, 12);
            int height = BitConverter.ToUInt16(fileBytes, 14);
            byte bpp = fileBytes[16];
            byte imgDesc = fileBytes[17];

            if (colorMapType != 0) throw new Exception("Color map TGA not supported.");
            if (imageType != 2 && imageType != 10) throw new Exception($"Unsupported TGA image type {imageType}.");
            if (bpp != 32 && bpp != 24) throw new Exception($"Unsupported TGA bpp {bpp}. Only 24/32 supported.");

            bool bottomToTop = (imgDesc & (1 << 5)) == 0;
            int bytesPerPixel = bpp / 8;
            byte[] bgraPixels = new byte[width * height * 4];
            int offset = 18 + idLength;

            if (imageType == 2)
            {
                for (int i = 0; i < width * height; i++)
                {
                    if (offset + bytesPerPixel > fileBytes.Length) break;
                    bgraPixels[i * 4 + 0] = fileBytes[offset++];
                    bgraPixels[i * 4 + 1] = fileBytes[offset++];
                    bgraPixels[i * 4 + 2] = fileBytes[offset++];
                    bgraPixels[i * 4 + 3] = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;
                }
            }
            else // imageType == 10 (RLE)
            {
                int pixelCount = 0;
                while (pixelCount < width * height && offset < fileBytes.Length)
                {
                    byte packetHeader = fileBytes[offset++];
                    int count = (packetHeader & 0x7F) + 1;
                    if ((packetHeader & 0x80) != 0)
                    {
                        byte cb = fileBytes[offset++];
                        byte cg = fileBytes[offset++];
                        byte cr = fileBytes[offset++];
                        byte ca = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;
                        for (int i = 0; i < count && pixelCount < width * height; i++, pixelCount++)
                        {
                            bgraPixels[pixelCount * 4 + 0] = cb;
                            bgraPixels[pixelCount * 4 + 1] = cg;
                            bgraPixels[pixelCount * 4 + 2] = cr;
                            bgraPixels[pixelCount * 4 + 3] = ca;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count && pixelCount < width * height; i++, pixelCount++)
                        {
                            bgraPixels[pixelCount * 4 + 0] = fileBytes[offset++];
                            bgraPixels[pixelCount * 4 + 1] = fileBytes[offset++];
                            bgraPixels[pixelCount * 4 + 2] = fileBytes[offset++];
                            bgraPixels[pixelCount * 4 + 3] = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;
                        }
                    }
                }
            }

            if (bottomToTop)
            {
                byte[] flipped = new byte[width * height * 4];
                int stride = width * 4;
                for (int y = 0; y < height; y++)
                    Array.Copy(bgraPixels, (height - 1 - y) * stride, flipped, y * stride, stride);
                bgraPixels = flipped;
            }

            WritePng(bgraPixels, width, height, outputPngPath, PixelFormats.Bgra32);
        }

        public static void ConvertPngToTga(string pngPath, string outputTgaPath, string referenceTgaPath)
        {
            byte[] refBytes = File.ReadAllBytes(referenceTgaPath);
            if (refBytes.Length < 18) throw new Exception("Reference TGA too short.");

            int refWidth = BitConverter.ToUInt16(refBytes, 12);
            int refHeight = BitConverter.ToUInt16(refBytes, 14);

            byte[] pngPixels = ReadPngBgra32(pngPath, out int pngW, out int pngH);
            if (pngW != refWidth || pngH != refHeight)
                throw new Exception($"PNG dimensions ({pngW}×{pngH}) don't match reference TGA ({refWidth}×{refHeight}).");

            using (FileStream fs = new FileStream(outputTgaPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((byte)0);      // id length
                bw.Write((byte)0);      // color map type
                bw.Write((byte)2);      // image type (uncompressed)
                bw.Write((short)0);     // color map start
                bw.Write((short)0);     // color map length
                bw.Write((byte)0);      // color map depth
                bw.Write((short)0);     // x offset
                bw.Write((short)0);     // y offset
                bw.Write((short)pngW);  // width
                bw.Write((short)pngH);  // height
                bw.Write((byte)32);     // bpp
                bw.Write((byte)0x28);   // image descriptor (top-to-bottom, 8-bit alpha)
                bw.Write(pngPixels);
            }
        }

        // ─────────────────────────────────────────────────────────
        // UTILITY METHODS
        // ─────────────────────────────────────────────────────────

        private static byte[] ReadPngBgra32(string pngPath, out int width, out int height)
        {
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
        private static byte Expand5(int v) => (byte)((v << 3) | (v >> 2));
    }
}
