using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Common.Utility
{
    public static class ImageUtility
    {
        public static readonly string[] AllImageFileExtensions = new string[]
        {
            "ase", "art", "bmp", "blp", "cd5", "cit", "cpt", "cr2", "cut", "dds",
            "dib", "djvu", "egt", "exif", "gif", "gpl", "grf", "icns", "ico", "iff",
            "jng", "jpeg", "jpg", "jfif", "jp2", "jps", "lbm", "max", "miff", "mng",
            "msp", "nef", "nitf", "ota", "pbm", "pc1", "pc2", "pc3", "pcf", "pcx",
            "pdn", "pgm", "pi1", "pi2", "pi3", "pict", "pct", "pnm", "pns", "ppm",
            "psb", "psd", "pdd", "psp", "px", "pxm", "pxr", "qfx", "raw", "rle",
            "sct", "sgi", "rgb", "int", "bw", "tga", "tiff", "tif", "vtf", "xbm",
            "xcf", "xpm", "3dv", "amf", "ai", "awg", "cgm", "cdr", "cmx", "dxf",
            "e2d", "egt", "eps", "fs", "gbr", "odg", "svg", "stl", "vrml", "x3d",
            "sxd", "v2d", "vnd", "wmf", "emf", "art", "xar", "png", "webp", "jxr",
            "hdp", "wdp", "cur", "ecw", "iff", "lbm", "liff", "nrrd", "pam", "pcx",
            "pgf", "sgi", "rgb", "rgba", "bw", "int", "inta", "sid", "ras", "sun",
            "tga", "heic", "heif"
        };

        public static readonly string[] CommonWebImageFileExtensions = new string[]
        {
            ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".webp"
        };

        /// <summary>
        /// Creates an image from text, using PNG format.
        /// </summary>
        public static byte[] CreateImageFromText(
            string text,
            string fontFamily = "Arial",
            float fontSize = 20,
            FontStyle fontStyle = FontStyle.Regular,
            Color color = default)
        {
            return CreateImageFromText<SixLabors.ImageSharp.Formats.Png.PngEncoder>(
                text,
                fontFamily,
                fontSize,
                fontStyle,
                color);
        }

        /// <summary>
        /// Creates an image from text, using the supplied format.
        /// </summary>
        public static byte[] CreateImageFromText<TOutputFormat>(
            string text,
            string fontFamily = "Arial",
            float fontSize = 20,
            FontStyle fontStyle = FontStyle.Regular,
            Color color = default)
            where TOutputFormat : IImageEncoder, new()
        {
            if (color == default)
            {
                color = Color.Black;
            }

            if (!SystemFonts.TryGet(fontFamily, out var fontFamilyObject))
            {
                throw new ArgumentException($"Font {fontFamily} was not found", nameof(fontFamily));
            }

            var font = fontFamilyObject.CreateFont(fontSize, fontStyle);
            var fontRect = TextMeasurer.Measure(text, new TextOptions(font));

            using (Image<Rgba32> img = new Image<Rgba32>((int)fontRect.Width, (int)fontRect.Height))
            {
                img.Mutate(x => x.DrawText(text, font, color, new PointF(0, 0)));

                using (var msOut = new MemoryStream())
                {
                    img.Save(msOut, new TOutputFormat());
                    return msOut.ToArray();
                }
            }
        }

        [SupportedOSPlatform("windows")]
        public static System.Drawing.Bitmap CropWhitespace(System.Drawing.Bitmap bitmap)
        {
            var w = bitmap.Width;
            var h = bitmap.Height;

            static bool IsAllWhiteOrTransparentRow(int row, System.Drawing.Bitmap bitmap, int w)
            {
                for (int i = 0; i < w; i++)
                {
                    var pixel = bitmap.GetPixel(i, row);

                    if (pixel.A == 0)
                    {
                        continue;
                    }

                    if (pixel.R != 255)
                    {
                        return false;
                    }
                }

                return true;
            }

            static bool IsAllWhiteOrTransparentColumn(int col, System.Drawing.Bitmap bitmap, int h)
            {
                for (int i = 0; i < h; i++)
                {
                    var pixel = bitmap.GetPixel(col, i);

                    if (pixel.A == 0)
                    {
                        continue;
                    }

                    if (pixel.R != 255)
                    {
                        return false;
                    }
                }

                return true;
            }

            int leftMost = 0;
            for (int col = 0; col < w; col++)
            {
                if (IsAllWhiteOrTransparentColumn(col, bitmap, h))
                {
                    leftMost = col + 1;
                }
                else
                {
                    break;
                }
            }

            int rightMost = w - 1;
            for (int col = rightMost; col > 0; col--)
            {
                if (IsAllWhiteOrTransparentColumn(col, bitmap, h))
                {
                    rightMost = col - 1;
                }
                else
                {
                    break;
                }
            }

            int topMost = 0;
            for (int row = 0; row < h; row++)
            {
                if (IsAllWhiteOrTransparentRow(row, bitmap, w))
                {
                    topMost = row + 1;
                }
                else
                {
                    break;
                }
            }

            int bottomMost = h - 1;
            for (int row = bottomMost; row > 0; row--)
            {
                if (IsAllWhiteOrTransparentRow(row, bitmap, w))
                {
                    bottomMost = row - 1;
                }
                else
                {
                    break;
                }
            }

            if (rightMost == 0 && bottomMost == 0 && leftMost == w && topMost == h)
            {
                return bitmap;
            }

            var croppedWidth = rightMost - leftMost + 1;
            var croppedHeight = bottomMost - topMost + 1;

            var target = new System.Drawing.Bitmap(croppedWidth, croppedHeight);

            using (var g = System.Drawing.Graphics.FromImage(target))
            {
                g.DrawImage(
                    bitmap,
                    new System.Drawing.RectangleF(0, 0, croppedWidth, croppedHeight),
                    new System.Drawing.RectangleF(leftMost, topMost, croppedWidth, croppedHeight),
                    System.Drawing.GraphicsUnit.Pixel);
            }

            return target;
        }

        [SupportedOSPlatform("windows")]
        public static void Stroke(System.Drawing.Bitmap bitmap, System.Drawing.Color color)
        {
            for (var i = 0; i < bitmap.Width; i++)
            {
                bitmap.SetPixel(i, 0, color);
                bitmap.SetPixel(i, bitmap.Height - 1, color);
            }

            for (var i = 0; i < bitmap.Height; i++)
            {
                bitmap.SetPixel(0, i, color);
                bitmap.SetPixel(bitmap.Width - 1, i, color);
            }
        }
    }
}
