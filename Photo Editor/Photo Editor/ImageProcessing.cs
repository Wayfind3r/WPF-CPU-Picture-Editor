using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Photo_Editor
{
    public static class ImageProcessing
    {
        /// <summary>
        ///     Convert BitmapImage to Bitmap for read-write convenience
        ///     Do not mistake
        ///     'System.Windows.Controls.Image' for 'System.Drawing.Image'
        ///     'System.Drawing.Image' -> abstract class for Bitmap
        /// </summary>
        public static Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                var bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
        /// <summary>
        ///     Convert Bitmap to BitmapImage for read-write convenience
        ///     Do not mistake
        ///     'System.Windows.Controls.Image' for 'System.Drawing.Image'
        ///     'System.Drawing.Image' -> abstract class for Bitmap
        /// </summary>
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public static BitmapImage ApplyThresholding(BitmapImage targetBitmapImage, int threshold, bool color)
        {
            Bitmap internalBitmap = ImageProcessing.ConvertBitmapImageToBitmap(targetBitmapImage);
            int width = internalBitmap.Width;
            int height = internalBitmap.Height;
            int realThreshold = threshold;
            if (threshold < 1) realThreshold = 1;
            if (threshold > 255) realThreshold = 255;
            if (!color)
            {
                for (int w = 0; w < width; w++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        Color pixelColor = internalBitmap.GetPixel(w, h);

                            if (pixelColor.G >= realThreshold || pixelColor.B >= realThreshold ||
                                pixelColor.R >= realThreshold)
                            {internalBitmap.SetPixel(w, h, Color.Black);}
                            else
                            {internalBitmap.SetPixel(w, h, Color.White);}
                    }
                }
            }
            else
            {
                int r, g, b;
                for (int w = 0; w < width; w++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        Color pixelColor = internalBitmap.GetPixel(w, h);
                        if (pixelColor.R >= realThreshold)
                        {r = 255;}
                        else {r = 0;}
                        if (pixelColor.G >= realThreshold)
                        {g = 255;}
                        else {g = 0;}
                        if (pixelColor.B>= realThreshold)
                        {b = 255;}
                        else {b = 0;}
                        internalBitmap.SetPixel(w, h, Color.FromArgb(r,g,b));
                    }
                }
            }
            BitmapImage output = ImageProcessing.ConvertBitmapToBitmapImage(internalBitmap);
            return output;
        }
        public static BitmapImage ApplyGrayScale(BitmapImage targetBitmapImage, double brightness)
        {
            Bitmap internalBitmap = ImageProcessing.ConvertBitmapImageToBitmap(targetBitmapImage);
            int width = internalBitmap.Width;
            int height = internalBitmap.Height;
            double realBrightness = brightness/100;
            //GRAY = (byte)(.299 * R + .587 * G + .114 * B);
            int rgb;
            for (var w = 0; w < width; w++)
            {
                for (var h = 0; h < height; h++)
                {
                    var pixelColor = internalBitmap.GetPixel(w, h);
                    double newDouble = (pixelColor.R*0.299 + pixelColor.G*0.587 + pixelColor.B*0.114)*realBrightness;
                    rgb = Convert.ToInt32(newDouble);
                    internalBitmap.SetPixel(w, h, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            BitmapImage output = ImageProcessing.ConvertBitmapToBitmapImage(internalBitmap);
            return output;
        }

        /// <summary>
        /// Slow, does not use pointers and Lockbits
        /// </summary>
        /// <param name="targetBitmapImage"></param>
        /// <param name="matrix"></param>
        /// <param name="offset"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static BitmapImage ApplyFilterFromMatrix(BitmapImage targetBitmapImage, int[,] matrix, int offset, float factor)
        {
            Bitmap internalBitmap = ImageProcessing.ConvertBitmapImageToBitmap(targetBitmapImage);
            Bitmap output = new Bitmap(internalBitmap);
            int width = internalBitmap.Width;
            int height = internalBitmap.Height;
            for(var w = 0; w < width; w++)
            {
                for (var h = 0; h < height; h++)
                {
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    for (int r = w - (matrix.GetLength(0)/2), matrixRow = 0; r <= w + (matrix.GetLength(0)/2); r++, matrixRow++)
                    {
                        if (r<0 || r>= width) continue;
                        for (int c = h - (matrix.GetLength(1)/2), matrixCol = 0; c <= h + (matrix.GetLength(1)/2); c++, matrixCol++)
                        {
                            if (c<0 || c>=height) continue;
                            if (matrix[matrixRow, matrixCol] != 0)
                            {
                                var currentColor = internalBitmap.GetPixel(r, c);
                                red += (currentColor.R)*(matrix[matrixRow,matrixCol]);
                                blue += (currentColor.B) * (matrix[matrixRow, matrixCol]);
                                green += (currentColor.G) * (matrix[matrixRow, matrixCol]);
                            }
                        }
                    }
                    red = (int)(red/factor + offset);
                    if (red < 0) red = 0;
                    if (red > 255) red = 255;
                    blue = (int)(blue / factor + offset);
                    if (blue < 0) blue = 0;
                    if (blue > 255) blue = 255;
                    green = (int)(green / factor + offset);
                    if (green < 0) green = 0;
                    if (green > 255) green = 255;
                    output.SetPixel(w, h, Color.FromArgb(red, green, blue));
                }
            }
            BitmapImage outputBitmapImage = ImageProcessing.ConvertBitmapToBitmapImage(output);
            return outputBitmapImage;
        }

        public static BitmapImage ApplyGaussianBlur(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,]{ {1,2,1},{2,4,2},{1,2,1} };
            int offset = 0;
            int factor = 16;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage ApplySharpen(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,] { { 0, -2, 0 }, { -2, 11, -2 }, { 0, -2, 0 } };
            int offset = 0;
            int factor = 3;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage ApplyEdgeEnhance(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 } };
            int offset = 0;
            int factor = 1;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage ApplyEdgeDetect(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,] { { -1, 0, -1 }, { 0, 4, 0 }, { -1, 0, -1 } };
            int offset = 127;
            int factor = 1;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage ApplyEmboss(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,] { { -1, 0, -1 }, { 0, 4, 0 }, { -1, 0, -1 } };
            int offset = 127;
            int factor = 1;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage ApplyMeanRemoval(BitmapImage targetBitmap)
        {
            int[,] matrix = new int[,] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
            int offset = 0;
            int factor = 1;
            BitmapImage output = new BitmapImage();
            output = ApplyFilterFromMatrix(targetBitmap, matrix, offset, factor);
            return output;
        }

        public static BitmapImage CustomColorSettings(BitmapImage targetBitmap, float brightness = 1f, float contrast = 1.0f, float gamma = 1.0f)
        {
            if (brightness != 0) brightness /= 100;
            if (contrast != 0) contrast /= 100;
            if (gamma != 0) gamma /= 100;
            Bitmap originalImage = ImageProcessing.ConvertBitmapImageToBitmap(targetBitmap);
            Bitmap adjustedImage = new Bitmap(originalImage);
            float adjustedBrightness = brightness - 1.0f;
            float[][] ptsArray ={
            new float[] {contrast, 0, 0, 0, 0}, // Red
            new float[] {0, contrast, 0, 0, 0}, // Green
            new float[] {0, 0, contrast, 0, 0}, // Blue
            new float[] {0, 0, 0, 1.0f, 0},
            new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            var imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(adjustedImage);
            g.DrawImage(adjustedImage, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
                , 0, 0, originalImage.Width, originalImage.Height,
                GraphicsUnit.Pixel, imageAttributes);
            BitmapImage outputBitmapImage = ImageProcessing.ConvertBitmapToBitmapImage(adjustedImage);
            return outputBitmapImage;
        }

        /// <summary>
        /// Resize the imageAfter to the specified width and height.
        /// </summary>
        /// <param name="targetBitmap">The imageAfter to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized imageAfter.</returns>
        public static BitmapImage ResizeImage(BitmapImage targetBitmap, int width, int height)
        {
            Bitmap imageAfter = ImageProcessing.ConvertBitmapImageToBitmap(targetBitmap);
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(imageAfter.HorizontalResolution, imageAfter.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(imageAfter, destRect, 0, 0, imageAfter.Width, imageAfter.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            BitmapImage outputBitmapImage = ImageProcessing.ConvertBitmapToBitmapImage(destImage);
            return outputBitmapImage;
        }
    }
}