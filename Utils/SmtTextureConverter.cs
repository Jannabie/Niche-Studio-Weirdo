using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NicheStudioWeirdo.Utils
{
    public static class SmtTextureConverter
    {
        private static int MortonIndex(int x, int y)
        {
            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                result |= ((x >> i) & 1) << (i * 2);
                result |= ((y >> i) & 1) << (i * 2 + 1);
            }
            return result;
        }

        public static void ConvertStexToPng(string stexPath, string outputPngPath)
        {
            byte[] fileBytes = File.ReadAllBytes(stexPath);
            if (fileBytes.Length < 32 || fileBytes[0] != 'S' || fileBytes[1] != 'T' || fileBytes[2] != 'E' || fileBytes[3] != 'X')
                throw new Exception("Invalid STEX file (magic doesn't match).");

            int width = BitConverter.ToInt32(fileBytes, 12);
            int height = BitConverter.ToInt32(fileBytes, 16);
            uint format = BitConverter.ToUInt32(fileBytes, 20);

            if (format != 0x1401)
                throw new Exception($"Unsupported STEX format: 0x{format:X4}. Only RGBA8 (0x1401) is supported.");

            int dataOffset = 32;
            byte[] rgbaPixels = new byte[width * height * 4];

            int tilesX = (width + 7) / 8;
            int tilesY = (height + 7) / 8;

            int currentDataOffset = dataOffset;

            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            int px = tx * 8 + x;
                            int py = ty * 8 + y;

                            // 3DS also flips y within each tile
                            int mortonIndex = MortonIndex(x, 7 - y);
                            int byteIndex = currentDataOffset + mortonIndex * 4;

                            if (px < width && py < height && byteIndex + 3 < fileBytes.Length)
                            {
                                int destIndex = (py * width + px) * 4;
                                // STEX format is ABGR8 -> read A, B, G, R
                                byte a = fileBytes[byteIndex];
                                byte b = fileBytes[byteIndex + 1];
                                byte g = fileBytes[byteIndex + 2];
                                byte r = fileBytes[byteIndex + 3];

                                // PNG BGRA32 wants B, G, R, A or RGBA depending on PixelFormats
                                // We will use PixelFormats.Bgra32 (B, G, R, A)
                                rgbaPixels[destIndex] = b;
                                rgbaPixels[destIndex + 1] = g;
                                rgbaPixels[destIndex + 2] = r;
                                rgbaPixels[destIndex + 3] = a;
                            }
                        }
                    }
                    currentDataOffset += 64 * 4; // Advance to next tile
                }
            }

            WritePng(rgbaPixels, width, height, outputPngPath, PixelFormats.Bgra32);
        }

        public static void ConvertPngToStex(string pngPath, string outputStexPath, string referenceStexPath)
        {
            byte[] refBytes = File.ReadAllBytes(referenceStexPath);
            if (refBytes.Length < 32 || refBytes[0] != 'S' || refBytes[1] != 'T' || refBytes[2] != 'E' || refBytes[3] != 'X')
                throw new Exception("Invalid reference STEX file.");

            int refWidth = BitConverter.ToInt32(refBytes, 12);
            int refHeight = BitConverter.ToInt32(refBytes, 16);
            uint refFormat = BitConverter.ToUInt32(refBytes, 20);

            if (refFormat != 0x1401)
                throw new Exception($"Unsupported reference STEX format: 0x{refFormat:X4}. Only RGBA8 (0x1401) is supported.");

            var bitmap = new BitmapImage(new Uri(Path.GetFullPath(pngPath), UriKind.Absolute));
            if (bitmap.PixelWidth != refWidth || bitmap.PixelHeight != refHeight)
                throw new Exception($"PNG dimensions ({bitmap.PixelWidth}x{bitmap.PixelHeight}) do not match reference STEX ({refWidth}x{refHeight}).");

            // Convert to Bgra32
            FormatConvertedBitmap converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            byte[] pngPixels = new byte[width * height * 4];
            converted.CopyPixels(pngPixels, width * 4, 0);

            byte[] outBytes = new byte[refBytes.Length];
            Array.Copy(refBytes, outBytes, 32);

            int tilesX = (width + 7) / 8;
            int tilesY = (height + 7) / 8;

            int currentDataOffset = 32;

            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            int px = tx * 8 + x;
                            int py = ty * 8 + y;

                            int mortonIndex = MortonIndex(x, 7 - y);
                            int byteIndex = currentDataOffset + mortonIndex * 4;

                            if (px < width && py < height && byteIndex + 3 < outBytes.Length)
                            {
                                int srcIndex = (py * width + px) * 4;
                                byte b = pngPixels[srcIndex];
                                byte g = pngPixels[srcIndex + 1];
                                byte r = pngPixels[srcIndex + 2];
                                byte a = pngPixels[srcIndex + 3];

                                // STEX format is ABGR8
                                outBytes[byteIndex] = a;
                                outBytes[byteIndex + 1] = b;
                                outBytes[byteIndex + 2] = g;
                                outBytes[byteIndex + 3] = r;
                            }
                        }
                    }
                    currentDataOffset += 64 * 4;
                }
            }

            File.WriteAllBytes(outputStexPath, outBytes);
        }

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
                // Uncompressed
                for (int i = 0; i < width * height; i++)
                {
                    if (offset + bytesPerPixel > fileBytes.Length) break;
                    byte b = fileBytes[offset++];
                    byte g = fileBytes[offset++];
                    byte r = fileBytes[offset++];
                    byte a = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;
                    bgraPixels[i * 4] = b;
                    bgraPixels[i * 4 + 1] = g;
                    bgraPixels[i * 4 + 2] = r;
                    bgraPixels[i * 4 + 3] = a;
                }
            }
            else if (imageType == 10)
            {
                // RLE
                int pixelCount = 0;
                while (pixelCount < width * height && offset < fileBytes.Length)
                {
                    byte packetHeader = fileBytes[offset++];
                    int count = (packetHeader & 0x7F) + 1;
                    if ((packetHeader & 0x80) != 0)
                    {
                        // RLE packet
                        byte b = fileBytes[offset++];
                        byte g = fileBytes[offset++];
                        byte r = fileBytes[offset++];
                        byte a = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;

                        for (int i = 0; i < count && pixelCount < width * height; i++)
                        {
                            bgraPixels[pixelCount * 4] = b;
                            bgraPixels[pixelCount * 4 + 1] = g;
                            bgraPixels[pixelCount * 4 + 2] = r;
                            bgraPixels[pixelCount * 4 + 3] = a;
                            pixelCount++;
                        }
                    }
                    else
                    {
                        // Raw packet
                        for (int i = 0; i < count && pixelCount < width * height; i++)
                        {
                            byte b = fileBytes[offset++];
                            byte g = fileBytes[offset++];
                            byte r = fileBytes[offset++];
                            byte a = bytesPerPixel == 4 ? fileBytes[offset++] : (byte)255;
                            bgraPixels[pixelCount * 4] = b;
                            bgraPixels[pixelCount * 4 + 1] = g;
                            bgraPixels[pixelCount * 4 + 2] = r;
                            bgraPixels[pixelCount * 4 + 3] = a;
                            pixelCount++;
                        }
                    }
                }
            }

            if (bottomToTop)
            {
                byte[] flipped = new byte[width * height * 4];
                int stride = width * 4;
                for (int y = 0; y < height; y++)
                {
                    Array.Copy(bgraPixels, (height - 1 - y) * stride, flipped, y * stride, stride);
                }
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

            var bitmap = new BitmapImage(new Uri(Path.GetFullPath(pngPath), UriKind.Absolute));
            if (bitmap.PixelWidth != refWidth || bitmap.PixelHeight != refHeight)
                throw new Exception($"PNG dimensions do not match reference TGA ({refWidth}x{refHeight}).");

            FormatConvertedBitmap converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            byte[] pngPixels = new byte[width * height * 4];
            converted.CopyPixels(pngPixels, width * 4, 0);

            // Write as uncompressed TGA (type 2) matching dimensions, 32bpp, no matter what ref was
            using (FileStream fs = new FileStream(outputTgaPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((byte)0); // id length
                bw.Write((byte)0); // color map type
                bw.Write((byte)2); // image type (uncompressed)
                bw.Write((short)0); // color map start
                bw.Write((short)0); // color map length
                bw.Write((byte)0);  // color map depth
                bw.Write((short)0); // x offset
                bw.Write((short)0); // y offset
                bw.Write((short)width); // width
                bw.Write((short)height); // height
                bw.Write((byte)32); // bpp
                bw.Write((byte)8); // image descriptor (8 bits alpha, top-to-bottom=0 => bottom-to-top, so wait)
                // wait, if we write top to bottom, bit 5 must be 1. So (1<<5) | 8 = 32 | 8 = 40 (0x28).
                // Let's write top-to-bottom to match our pixel array simply, so descriptor = 0x28.
                bw.Write((byte)0x28); 

                bw.Write(pngPixels);
            }
        }

        private static void WritePng(byte[] pixels, int width, int height, string path, PixelFormat format)
        {
            var bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, width * (format.BitsPerPixel / 8));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var fs = new FileStream(path, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}
