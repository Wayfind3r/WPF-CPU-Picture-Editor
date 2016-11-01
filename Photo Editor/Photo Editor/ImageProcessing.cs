using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using Photo_Editor.DataModels;
using System.Collections.Generic;

namespace Photo_Editor
{
    /// <summary>
    /// Containes utility methods for processing images
    /// </summary>
    //Keep in mind that there's a lock inside GDI+ that prevents two threads from accessing a bitmap at the same time.
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
            Bitmap internalBitmap = ConvertBitmapImageToBitmap(targetBitmapImage);
            int width = internalBitmap.Width;
            int height = internalBitmap.Height;
            int realThreshold = threshold;

            realThreshold = Math.Max(threshold, 1);
            realThreshold = Math.Min(threshold, 255);

            var threadStack = new ConcurrentStack<Pixel>();

            var slices = SplitBitmap(internalBitmap);

            if (!color)
            {
                Parallel.ForEach(slices, slice =>
                {
                    var sliceWidth = slice.Bitmap.Width;
                    var sliceHeigth = slice.Bitmap.Height;
                    //Skip columns that are in the offset
                    for (var w = 0 + slice.OffsetLeft; w < sliceWidth - slice.OffsetRight; w++)
                    {
                        for (int h = 0; h < sliceHeigth; h++)
                        {
                            Color pixelColor = slice.Bitmap.GetPixel(w, h);

                            if (pixelColor.G >= realThreshold || pixelColor.B >= realThreshold ||
                                    pixelColor.R >= realThreshold)
                            {
                                var pixel = new Pixel
                                {
                                    X = w + slice.SliceXStartInOriginal,
                                    Y = h,
                                    Color = Color.Black
                                };

                                threadStack.Push(pixel);
                            }
                            else
                            {
                                var pixel = new Pixel
                                {
                                    X = w + slice.SliceXStartInOriginal,
                                    Y = h,
                                    Color = Color.White
                                };

                                threadStack.Push(pixel);
                            }
                        }
                    }
                });
            }
            else
            {
                Parallel.ForEach(slices, slice =>
                {
                    var sliceWidth = slice.Bitmap.Width;
                    var sliceHeigth = slice.Bitmap.Height;
                    //Skip columns that are in the offset
                    for (var w = 0 + slice.OffsetLeft; w < sliceWidth - slice.OffsetRight; w++)
                    {
                        int r, g, b;
                        for (int h = 0; h < sliceHeigth; h++)
                        {
                            Color pixelColor = slice.Bitmap.GetPixel(w, h);
                            if (pixelColor.R >= realThreshold)
                            { r = 255; }
                            else { r = 0; }
                            if (pixelColor.G >= realThreshold)
                            { g = 255; }
                            else { g = 0; }
                            if (pixelColor.B >= realThreshold)
                            { b = 255; }
                            else { b = 0; }

                            var pixel = new Pixel
                            {
                                X = w + slice.SliceXStartInOriginal,
                                Y = h,
                                Color = Color.FromArgb(r, b, g)
                            };

                            threadStack.Push(pixel);
                        }
                    }
                });
            }

            internalBitmap = ConstructBitmap(threadStack, internalBitmap);

            BitmapImage output = ConvertBitmapToBitmapImage(internalBitmap);
            return output;
        }
        public static BitmapImage ApplyGrayScale(BitmapImage targetBitmapImage, double brightness)
        {
            var internalBitmap = ConvertBitmapImageToBitmap(targetBitmapImage);

            //GRAY = (byte)(.299 * R + .587 * G + .114 * B);
            var redMultiplier = 0.299F;
            var greenMultiplier = 0.587F;
            var blueMultiplier = 0.114F;
            var bightness = (float)brightness / 100F - 1.0f;

            var threadStack = new ConcurrentStack<Pixel>();

            var slices = SplitBitmap(internalBitmap, 32, 0);

            //create a blank bitmap the same size as original
            var resultBitmap = new Bitmap(internalBitmap.Width, internalBitmap.Height);

            //get a graphics object from the new image
            var g = Graphics.FromImage(resultBitmap);

            //create the grayscale ColorMatrix
            var colorMatrix = new ColorMatrix(
               new float[][]
               {
                 new float[] { redMultiplier, redMultiplier, redMultiplier, 0, 0},//red
                 new float[] { greenMultiplier, greenMultiplier, greenMultiplier, 0, 0},//green
                 new float[] { blueMultiplier, blueMultiplier, blueMultiplier, 0, 0},//blue
                 new float[] {0, 0, 0, 1, 0},//alpha
                 new float[] { bightness, bightness, bightness, 0, 1}
               });

            //create some image attributes
            var attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            g.DrawImage(internalBitmap, 
               new Rectangle(0, 0, internalBitmap.Width, internalBitmap.Height),// destination rectangle 
               0, 0,// upper-left corner of source rectangle 
               internalBitmap.Width, internalBitmap.Height, 
               GraphicsUnit.Pixel, attributes);

            var output = ConvertBitmapToBitmapImage(resultBitmap);
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
            Bitmap internalBitmap = ConvertBitmapImageToBitmap(targetBitmapImage);
            int width = internalBitmap.Width;
            int height = internalBitmap.Height;

            var slices = SplitBitmap(internalBitmap);

            var threadStack = new ConcurrentStack<Pixel>();

            Parallel.ForEach(slices, slice =>
            {
                var sliceWidth = slice.Bitmap.Width;
                var sliceHeigth = slice.Bitmap.Height;
                //Skip columns that are in the offset
                for (var w = 0 + slice.OffsetLeft; w < sliceWidth - slice.OffsetRight; w++)
                {
                    for (var h = 0; h < sliceHeigth; h++)
                    {
                        int red = 0;
                        int green = 0;
                        int blue = 0;
                        for (int r = w - (matrix.GetLength(0) / 2), matrixRow = 0; r <= w + (matrix.GetLength(0) / 2); r++, matrixRow++)
                        {
                            if (r < 0 || r >= sliceWidth) continue;
                            for (int c = h - (matrix.GetLength(1) / 2), matrixCol = 0; c <= h + (matrix.GetLength(1) / 2); c++, matrixCol++)
                            {
                                if (c < 0 || c >= sliceHeigth) continue;
                                if (matrix[matrixRow, matrixCol] != 0)
                                {
                                    var currentColor = slice.Bitmap.GetPixel(r, c);
                                    red += (currentColor.R) * (matrix[matrixRow, matrixCol]);
                                    blue += (currentColor.B) * (matrix[matrixRow, matrixCol]);
                                    green += (currentColor.G) * (matrix[matrixRow, matrixCol]);
                                }
                            }
                        }

                        red = Math.Min(Math.Max((int)(red / factor + offset), 0), 255);
                        green = Math.Min(Math.Max((int)(green / factor + offset), 0), 255);
                        blue = Math.Min(Math.Max((int)(blue / factor + offset), 0), 255);

                        var pixel = new Pixel
                        {
                            X = w + slice.SliceXStartInOriginal,
                            Y = h,
                            Color = Color.FromArgb(red, green, blue)
                        };

                        threadStack.Push(pixel);
                    }
                }
            });
            internalBitmap = ConstructBitmap(threadStack, internalBitmap);

            BitmapImage outputBitmapImage = ConvertBitmapToBitmapImage(internalBitmap);
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
            Bitmap originalImage = ConvertBitmapImageToBitmap(targetBitmap);
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
            BitmapImage outputBitmapImage = ConvertBitmapToBitmapImage(adjustedImage);
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
            BitmapImage outputBitmapImage = ConvertBitmapToBitmapImage(destImage);
            return outputBitmapImage;
        }

        /// <summary>
        /// Returns a new bitmap, as combination between the stack and the base Bitmap
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="baseBitmap"></param>
        /// <returns></returns>
        private static Bitmap ConstructBitmap(ConcurrentStack<Pixel> stack, Bitmap baseBitmap)
        {
            var result = new Bitmap(baseBitmap);

            //Set all processed pixels
            var poppedPixel = new Pixel();
            while (stack.TryPop(out poppedPixel))
            {
                result.SetPixel(poppedPixel.X, poppedPixel.Y, poppedPixel.Color);
            }

            return result;
        }

        /// <summary>
        /// Splits a Bitmap into Slices with a fixed max width and a buffer of a few pixels
        /// </summary>
        /// <param name="target"></param>
        /// <param name="widthPerPiece"></param>
        /// <param name="sideOffset">Set this to Kernel matrix radius</param>
        /// <returns></returns>
        /// There's a lock inside GDI+ that prevents two threads from accessing a Bitmap at the same time.
        /// You can use this method to split the Bitmap into BitmapSlices to use for Parallel tasks
        private static List<BitmapSlice> SplitBitmap(Bitmap target, int maxWidthPerPiece = 32, int sideOffset = 1)
        {
            var result = new List<BitmapSlice>();

            var sliceStart = 0;
            var sliceEnd = 0;
            var targetWidth = target.Width;
            var targetHeight = target.Height;
            var bufferLeft = 0;
            var bufferRight = 0;

            var rectangle = new Rectangle();

            while(sliceEnd < targetWidth - 1)
            {
                //Confirm the true slice end X
                sliceEnd = Math.Min(sliceStart + maxWidthPerPiece, targetWidth - 1);
                //Make sure there is space for a offset on each side
                bufferLeft = sliceStart == 0 ? 0 : sideOffset;
                bufferRight = sliceEnd == targetWidth - 1 ? 0 : sideOffset;

                var slice = new BitmapSlice
                {
                    //Later on we will ignore the pixels in the offset columns
                    OffsetLeft = bufferLeft,
                    OffsetRight = bufferRight,
                    SliceWidth = sliceEnd - sliceStart + bufferRight + bufferLeft,
                    SliceXStartInOriginal = sliceStart
                };

                rectangle =  new Rectangle(sliceStart - bufferLeft ,0 ,slice.SliceWidth ,targetHeight);

                slice.Bitmap = target.Clone(rectangle, target.PixelFormat);

                result.Add(slice);
                //Modify the next slice Start with this slice's width
                sliceStart += slice.SliceWidth - bufferRight - 1;
            }

            return result;
        }
    }
}