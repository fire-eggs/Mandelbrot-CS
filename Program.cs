using System;
using System.Windows.Forms;

namespace Mandelbrot
{
    internal class Display
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlayForm());
        }
    }
}