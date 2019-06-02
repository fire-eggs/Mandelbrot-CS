using System;
using System.Drawing;

namespace Mandelbrot
{
    public struct RgbValue
    {
        public int red;
        public int green;
        public int blue;
        public RgbValue(int r, int g, int b)
        {
            red = r;
            green = g;
            blue = b;
        }

        public static RgbValue BLACK = new RgbValue(0,0,0);

        public static double Lerp(double v0, double v1, double t) => (1 - t) * v0 + t * v1;

        public static RgbValue LerpColors(RgbValue a, RgbValue b, double alpha)
        {
            // Initialize final color
            RgbValue c = new RgbValue();

            // Linear interpolate red, green, and blue values.
            c.red = (int)Lerp(a.red, b.red, alpha);

            c.green = (int)Lerp(a.green, b.green, alpha);

            c.blue = (int)Lerp(a.blue, b.blue, alpha);

            return c;
        }

        public Color toColor()
        {
            return Color.FromArgb(Math.Min(red,255), 
                Math.Min(green,255), 
                Math.Min(blue,255));
        }
    }
}
