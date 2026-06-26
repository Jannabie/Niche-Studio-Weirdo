using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NicheStudioWeirdo.Utils
{
    public static class PaletteGenerator
    {
        public static BitmapPalette GeneratePalette(BitmapSource source, int maxColors)
        {
            if (source.Format != PixelFormats.Bgra32 && source.Format != PixelFormats.Bgr24)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            var colorFreq = new Dictionary<int, int>();
            int bpp = source.Format.BitsPerPixel / 8;

            for (int i = 0; i < pixels.Length; i += bpp)
            {
                int b = pixels[i];
                int g = pixels[i + 1];
                int r = pixels[i + 2];
                // Quantize slightly to reduce unique colors (5 bits per channel)
                r = (r & 0xF8);
                g = (g & 0xF8);
                b = (b & 0xF8);
                int argb = (r << 16) | (g << 8) | b;
                
                if (colorFreq.TryGetValue(argb, out int count))
                    colorFreq[argb] = count + 1;
                else
                    colorFreq[argb] = 1;
            }

            var topColors = colorFreq.OrderByDescending(kv => kv.Value)
                                     .Select(kv => kv.Key)
                                     .Take(maxColors)
                                     .ToList();

            var colors = new List<Color>();
            foreach (int argb in topColors)
            {
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);
                colors.Add(Color.FromRgb(r, g, b));
            }

            while (colors.Count < maxColors)
                colors.Add(Colors.Black);

            return new BitmapPalette(colors);
        }
    }
}
