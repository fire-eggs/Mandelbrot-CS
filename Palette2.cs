using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mandelbrot
{
    class Palette2
    {
        public static string[] FindPalettes()
        {
            var palPath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Palettes");
            if (!Directory.Exists(palPath))
                return null;
            var files = Directory.EnumerateFiles(palPath);
            return files.ToArray();
        }

        public static RgbValue[] LoadPalette(string path)
        {
            List<RgbValue> pallete = new List<RgbValue>();
            using (StreamReader palleteData = new StreamReader(path))
            {
                while (!palleteData.EndOfStream)
                {
                    try
                    {
                        string palleteString = palleteData.ReadLine();
                        if (string.IsNullOrWhiteSpace(palleteString))
                            continue;

                        string[] palleteTokens =
                            palleteString.Split(new char[1] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        int r = int.Parse(palleteTokens[0]);
                        int g = int.Parse(palleteTokens[1]);
                        int b = int.Parse(palleteTokens[2]);
                        RgbValue color = new RgbValue(r, g, b);
                        pallete.Add(color);
                    }
                    catch (FormatException)
                    {
                    }
                }

                return pallete.ToArray();
            }
        }

    }
}
