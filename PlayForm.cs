using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mandelbrot
{
    public partial class PlayForm : Form
    {
        private double coord1;
        private double coord2;
        private double coord3;
        private double coord4;
        private bool oAndW;


        public PlayForm()
        {
            InitializeComponent();

            coord1 = -2.5;
            coord2 = -1;
            coord3 = 1;
            coord4 = 1;
            oAndW = false;

            textBox1.Text = coord1.ToString();
            textBox2.Text = coord2.ToString();
            textBox3.Text = coord3.ToString();
            textBox4.Text = coord4.ToString();

            checkBox1.Checked = oAndW;

            button1.Click += Button1_Click;

            MakePalette();
            MakeGradiant();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Region reg = GetValues();
            if (reg == null)
                return; // message

            var img = Mandelbrot.MakeImage(reg, _palette, _gradient);

            // TODO dispose old image
            pictureBox1.Image = img;
        }

        private double parse(TextBox tb)
        {
            if (double.TryParse(tb.Text, out var value))
            {
                return value;
            }
            return Double.NaN;
        }

        private Region GetValues()
        {
            var v1 = parse(textBox1);
            var v2 = parse(textBox2);
            var v3 = parse(textBox3);
            var v4 = parse(textBox4);

            if (double.IsNaN(v1) || double.IsNaN(v2) || double.IsNaN(v3) || double.IsNaN(v4))
                return null;

            return new Region(new Complex(v1,v2), new Complex(v3,v4), checkBox1.Checked);
        }

        private static int NUMCOLORS = 768;
        private Color[] _palette;

        private void MakePalette()
        {
            var numColors = NUMCOLORS;
            var initialPalette = new[]
            {
                Tuple.Create(0.0, Color.FromArgb(255, 0, 7, 100)),
                Tuple.Create(0.16, Color.FromArgb(255, 32, 107, 203)),
                Tuple.Create(0.42, Color.FromArgb(255, 237, 255, 255)),
                Tuple.Create(0.6425, Color.FromArgb(255, 255, 170, 0)),
                Tuple.Create(0.8575,  Color.FromArgb(255, 0, 2, 0)),
                Tuple.Create(1.0, Color.FromArgb(255, 0, 7, 100))
            };
            _palette = Palette.GenerateColorPalette(initialPalette, numColors);
        }

        private Gradient _gradient;

        private void MakeGradiant()
        {
            var scaleDownFactor = Palette.CalculateScaleDownFactorForLinearMapping(Palette.FindPaletteColorLocation(_palette, Color.Black));
            var root = 4.0;

            var maxIterationColor = Color.Black;
            _gradient = new Gradient(
                maxIterationColor,
                Palette.RecommendedGradientScale(_palette.Length, true, scaleDownFactor),
                0, 1E10,
                logIndex: true, rootIndex: false,
                root: root, minIterations: 1,
                indexScale: 100, weight: 1.0);
        }
    }

}
