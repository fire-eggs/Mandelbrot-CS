using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mandelbrot
{
    public static class Mandelbrot
    {
        /// <summary>
        /// </summary>
        private static readonly double OneOverLog2 = 1 / Math.Log(2);

        /// <summary>
        /// </summary>
        private const double Epsilon = MathUtilities.Tolerance * MathUtilities.Tolerance;

        /// <summary>
        /// </summary>
        private const int BitDepthFor24BppRgb = 3;

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="size"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        public static byte[] DrawMandelbrot(Size threads, Size size, Region region, int maxIteration, RgbValue[] palette,
            Gradient gradient, double bailout)
            => DrawMandelbrot(threads, new Size(0, 0), size, region, maxIteration, palette, gradient, bailout);

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="region"></param>
        /// <param name="maxIterations"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        public static byte[] DrawMandelbrot(Size threads, Size start, Size end, Region region, int maxIterations, RgbValue[] palette,
            Gradient gradient, double bailout)
        {
            region = region.NormalizeRegion();
            //var cartesianRegion = new Region(
            //    min: new Complex(region.Min.Real, -region.Min.Imaginary),
            //    max: new Complex(region.Max.Real, -region.Max.Imaginary));
            int globalStartX = start.Width, globalEndX = end.Width;
            int globalStartY = start.Height, globalEndY = end.Height;

            int globalWidth = globalEndX - globalStartX,
                globalHeight = globalEndY - globalStartY;


            double globalRealStart = region.Min.Real,
                globalImaginaryStart = region.Min.Imaginary;

            var image = new byte[globalHeight * globalWidth * BitDepthFor24BppRgb];

            int xThreads = threads.Width, yThreads = threads.Height;

            var globalScale = MathUtilities.Scale(
                globalWidth, globalHeight,
                region.Min.Real, region.Max.Real,
                region.Min.Imaginary, region.Max.Imaginary);

            #region General Configuration For All Threads
            if (maxIterations < 1) throw new ArgumentException($"Max iterations must be >= 1, is {maxIterations}");
            var colors = palette.Length;
            var root = gradient.Exponent;
            double indexScale = gradient.IndexScale, indexWeight = gradient.Weight;  
            var rootMinIterations = gradient.RootIndex ? Math.Pow(gradient.MinIterations, root) : 0.0;
            var logBase = gradient.LogIndex ? Math.Log((double) maxIterations / gradient.MinIterations) : 0.0;
            var logMinIterations = gradient.LogIndex ? Math.Log(gradient.MinIterations, logBase) : 0.0;
            var logPaletteBailout = Math.Log(gradient.PaletteBailout);
            var halfOverLogPaletteBailout = 0.5 / logPaletteBailout;
            var bailoutSquared = bailout * bailout;
            var useSqrt = gradient.RootIndex && Math.Abs(gradient.Root - 2) < Epsilon;
            var maxIterationColor = gradient.MaxIterationColor;
            #endregion

            var tasks = new Thread[xThreads * yThreads];
            for (var iy = 0; iy < yThreads; ++iy)
            {
                for (var ix = 0; ix < xThreads; ++ix)
                {
                    var localRegion = MathUtilities.StartEndCoordinates(
                        globalStartX, globalEndX, globalStartY, globalEndY,
                        xThreads, ix, yThreads, iy);

                    int localStartX = localRegion.Item1,
                        localStartY = localRegion.Item3,
                        localEndX = localRegion.Item2,
                        localEndY = localRegion.Item4;

                    int localWidth = localEndX - localStartX,
                        localHeight = localEndY - localStartY;

                    var localStart =
                        MathUtilities.PixelToArgandCoordinates(
                            localStartX, localStartY,
                            globalScale.Real, globalScale.Imaginary,
                            globalRealStart, globalImaginaryStart);


                    var taskIndex = iy * xThreads + ix;
                    tasks[taskIndex] =
                        new Thread(() =>
                    DrawMandelbrot(
                        localStartX, localStartY,
                        localWidth, localHeight,
                        localStart.Real, localStart.Imaginary, maxIterations,
                        globalScale.Real, globalScale.Imaginary, globalWidth,
                        palette, gradient, image,
                        colors, bailoutSquared, halfOverLogPaletteBailout,
                        logBase, logMinIterations, root, rootMinIterations,
                        indexScale, indexWeight, useSqrt, maxIterationColor));
                    tasks[taskIndex].Start();
                }
            }
            // Wait for completion
            foreach (var task in tasks)
            {

                task.Join();
            }

            return image;
        }

        /// <summary>
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="realStart"></param>
        /// <param name="imaginaryStart"></param>
        /// <param name="maxIterations"></param>
        /// <param name="realScale"></param>
        /// <param name="imaginaryScale"></param>
        /// <param name="scan"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="image"></param>
        /// <param name="colors"></param>
        /// <param name="bailoutSquared"></param>
        /// <param name="halfOverLogBailout"></param>
        /// <param name="logBase"></param>
        /// <param name="logMinIterations"></param>
        /// <param name="root"></param>
        /// <param name="rootMinIterations"></param>
        /// <param name="indexScale"></param>
        /// <param name="indexWeight"></param>
        /// <param name="useSqrt"></param>
        /// <param name="maxIterationColor"></param>
        private static void DrawMandelbrot(
            int startX, int startY,
            int width, int height,
            double realStart, double imaginaryStart,
            int maxIterations,
            double realScale, double imaginaryScale,
            int scan, RgbValue[] palette, Gradient gradient, byte[] image, int colors,
            double bailoutSquared, double halfOverLogBailout,
            double logBase, double logMinIterations,
            double root, double rootMinIterations,
            double indexScale, double indexWeight,
            bool useSqrt, Color maxIterationColor)
        {
            for (var py = 0; py < height; ++py)
            {
                for (var px = 0; px < width; ++px)
                {
                    double x = 0.0, xp = 0.0; // x;
                    double y = 0.0, yp = 0.0; // y;
                    var modulusSquared = 0.0; // x * x + y * y;
                    var x0 = px * realScale + realStart;
                    var y0 = py * imaginaryScale + imaginaryStart;
                    var iterations = 0;
                    while (modulusSquared < bailoutSquared && iterations < maxIterations)
                    {
                        var xtemp = x * x - y * y + x0;
                        var ytemp = 2 * x * y + y0;
                        double dx = xtemp - x,
                            dy = ytemp - y,
                            dxp = xtemp - xp,
                            dyp = ytemp - yp;
                        if ((dx * dx < Epsilon && dy * dy < Epsilon) ||
                            (dxp * dxp < Epsilon && dyp * dyp < Epsilon))
                        {
                            iterations = maxIterations;
                            break;
                        }
                        xp = x;
                        yp = y;
                        x = xtemp;
                        y = ytemp;
                        modulusSquared = x * x + y * y;
                        ++iterations;
                    }
                    var smoothed = Math.Log(Math.Log(modulusSquared) * halfOverLogBailout) * OneOverLog2;
                    var index = indexScale * (iterations + 1 - indexWeight * smoothed);
                    if (useSqrt)
                    {
                        index = Math.Sqrt(index) - rootMinIterations;
                    }
                    else if (gradient.RootIndex)
                    {
                        index = Math.Pow(index, root) - rootMinIterations;
                    }
                    if (gradient.LogIndex)
                    {
                        index = Math.Log(index, logBase) - logMinIterations;
                    }
                    index = MathUtilities.NormalizeIndex(index, colors);
                    var actualIndex = MathUtilities.PreparePaletteIndex(index, colors, gradient);

                    byte red, green, blue;
                    if (iterations >= maxIterations)
                    {
                        red = maxIterationColor.R;
                        green = maxIterationColor.G;
                        blue = maxIterationColor.B;
                    }
                    else
                    {
                        var outval = RgbValue.LerpColors(
                            palette[actualIndex],
                            palette[(actualIndex + 1) % colors],
                            index - (long) index);
                        //Palette.Lerp(
                        //    palette[actualIndex],
                        //    palette[(actualIndex + 1) % colors],
                        //    index - (long)index,
                        //    out red, out green, out blue);
                        red = (byte)outval.red;
                        green = (byte)outval.green;
                        blue = (byte)outval.blue;
                    }
                    var offset = BitDepthFor24BppRgb * ((startY + py) * scan + (startX + px));
                    image[offset] = blue;
                    image[offset + 1] = green;
                    image[offset + 2] = red;
                }
            }
        }


        private static int WIDE = 1024;
        private static int HIGH = 1024;
        // TODO needs to auto-adjust
        private static int MAX_ITER = 5000;
        // TODO how to configure this?
        private static double BAILOUT = 1E10;

        private static void CopyArrayToBitmap(int width, int height, int depth, byte[] buffer, BitmapData img)
        {
            var arrRowLength = width * depth;
            var ptr = img.Scan0;
            for (var i = 0; i < height; i++)
            {
                Marshal.Copy(buffer, i * arrRowLength, ptr, arrRowLength);
                ptr += img.Stride;
            }
        }

        public static Image MakeImage(Region region, RgbValue[] palette, Gradient gradient)
        {
            return MakeImage(region, palette, gradient, HIGH, WIDE);
        }

        // TODO create something which can change palette w/o re-calculating

        public static Image MakeImage(Region region, RgbValue[] palette, Gradient gradient, int height, int width)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var img = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            var depth = Image.GetPixelFormatSize(img.PixelFormat) / 8; //bytes per pixel

            // Auto iteration count formula from https://github.com/cslarsen/mandelbrot-js/blob/master/mandelbrot.js
            var imaginD = Math.Abs(region.Min.Imaginary - region.Max.Imaginary);
            var realD = Math.Abs(region.Min.Real - region.Max.Real);
            var f = Math.Sqrt(0.001 + 2 * Math.Min(imaginD, realD));
            //var iter = (int)Math.Floor(223.0 / f);
            var iter = (int)Math.Floor(347.0 / f);
            iter = Math.Max(MAX_ITER, iter);

            var buffer =
                DrawMandelbrot(
                    new Size(1, Environment.ProcessorCount),
                    new Size(width, height),
                    region,
                    iter, palette, gradient, BAILOUT);

            CopyArrayToBitmap(width, height, depth, buffer, img);
            bmp.UnlockBits(img);

            return bmp;
        }

    }
}
