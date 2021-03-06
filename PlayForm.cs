﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows.Forms;

// X coord is 'real'; Y coord is 'imaginary'
// MinX == MinR
// MinY == MinI

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

            BtnReset_Click(null,null);

            checkBox1.Checked = oAndW;

            button1.Click += Button1_Click;

            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;

            FillPalettes();

            MakePalette();
            _defPalette = _palette;
            MakeGradiant();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Region reg = GetValues();
            if (reg == null)
                return; // message
            MakeNew(reg);
        }

        private void MakeNew(Region reg)
        {
            int h = pictureBox1.Height;
            int w = pictureBox1.Width;

            var img = Mandelbrot.MakeImage(reg, _palette, _gradient, h, w);

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

            // TODO mark invalid textbox
            if (double.IsNaN(v1) || double.IsNaN(v2) || double.IsNaN(v3) || double.IsNaN(v4))
                return null;

            return new Region(new Complex(v1,v2), new Complex(v3,v4), checkBox1.Checked);
        }

        private static int NUMCOLORS = 768;
        private RgbValue[] _palette;

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

        //private const double PALETTE_BAILOUT = 1E10;
        private const double PALETTE_BAILOUT = 1E16;

        private void MakeGradiant()
        {
            var scaleDownFactor = Palette.CalculateScaleDownFactorForLinearMapping(Palette.FindPaletteColorLocation(_palette, RgbValue.BLACK));
            var root = 4.0;

            var maxIterationColor = Color.Black;
            _gradient = new Gradient(
                maxIterationColor,
                Palette.RecommendedGradientScale(_palette.Length, true, scaleDownFactor),
                0, PALETTE_BAILOUT,
                logIndex: true, rootIndex: false,
                root: root, minIterations: 1,
                indexScale: 100, weight: 1.0);
        }


        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (_rect.Width > 0 && _rect.Height > 0)
                {
                    e.Graphics.FillRectangle(_selectionBrush, _rect);
                }
            }

        }

        private Point _rectStartPoint;
        private Rectangle _rect;
        private readonly Brush _selectionBrush = new SolidBrush(Color.FromArgb(128, 72, 145, 220));


        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Determine the initial rectangle coordinates...
            _rectStartPoint = e.Location;
            Invalidate();
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            Point tempEndPoint = e.Location;
            _rect.Location = new Point(
                Math.Min(_rectStartPoint.X, tempEndPoint.X),
                Math.Min(_rectStartPoint.Y, tempEndPoint.Y));
            _rect.Size = new Size(
                Math.Abs(_rectStartPoint.X - tempEndPoint.X),
                Math.Abs(_rectStartPoint.Y - tempEndPoint.Y));
            pictureBox1.Invalidate();
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            var regio = GetValues();
            // TODO normalize?
            var origin = regio.Min;
            double w = regio.Max.Real - regio.Min.Real;
            double h = regio.Max.Imaginary - regio.Min.Imaginary;

            double newR = regio.Min.Real + (_rect.Left * w / pictureBox1.Width);
            double newI = regio.Min.Imaginary + (_rect.Top * h / pictureBox1.Height);

            double newR2 = regio.Min.Real + (_rect.Right * w / pictureBox1.Width);
            double newI2 = regio.Min.Imaginary + (_rect.Bottom * h / pictureBox1.Height);

            Region newRegio = new Region(new Complex(newR,newI),
                new Complex(newR2, newI2));
            _rect = new Rectangle();

            textBox1.Text = newR.ToString(CultureInfo.InvariantCulture);
            textBox2.Text = newI.ToString(CultureInfo.InvariantCulture);
            textBox3.Text = newR2.ToString(CultureInfo.InvariantCulture);
            textBox4.Text = newI2.ToString(CultureInfo.InvariantCulture);

            MakeNew(newRegio);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            coord1 = -2.1; //-2.5;
            coord2 = -1.2; //-1;
            coord3 = 0.8; //1;
            coord4 = 1.2; //1;
            oAndW = false;

            textBox1.Text = coord1.ToString();
            textBox2.Text = coord2.ToString();
            textBox3.Text = coord3.ToString();
            textBox4.Text = coord4.ToString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // TODO set up initial folder correctly
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.OverwritePrompt = true;
            sfd.FileName = "Image.png";
            if (DialogResult.OK != sfd.ShowDialog())
                return;
            pictureBox1.Image.Save(sfd.FileName, ImageFormat.Png);
        }

        private const string BUILTIN = "Built-in";

        private void FillPalettes()
        {
            cmbPalette.BeginUpdate();
            cmbPalette.Items.Add(BUILTIN);
            var palFiles = Palette2.FindPalettes();
            foreach (var palFile in palFiles)
            {
                cmbPalette.Items.Add(Path.GetFileNameWithoutExtension(palFile));
            }

            cmbPalette.SelectedIndex = 0;
            cmbPalette.EndUpdate();
        }

        private void CmbPalette_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_defPalette == null)
                return; // too soon
            var palName = cmbPalette.SelectedItem;
            if (palName.ToString() == BUILTIN)
                _palette = _defPalette;
            else
            {
                var palPath = Path.Combine(Application.StartupPath, "Palettes\\" + palName + ".map");
                var rgbvals = Palette2.LoadPalette(palPath);
                //List<Color> clrs = new List<Color>();
                //foreach (var anRGB in rgbvals)
                //{
                //    clrs.Add(anRGB.toColor());
                //}

                _palette = rgbvals;
            }
            MakeGradiant();
            Button1_Click(null,null);
        }

        private RgbValue[] _defPalette;

        private void PictureBox1_SizeChanged(object sender, EventArgs e)
        {
            var h = pictureBox1.ClientSize.Height;
            var w = pictureBox1.ClientSize.Width;

            // show picture size in title bar, aiming for square
            Text = $"PlayForm [{h} x {w}]";
        }
    }

    }
