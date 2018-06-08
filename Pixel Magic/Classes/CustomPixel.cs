using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Media3D;
using Colourful;
using Colourful.Difference;
using Colourful.Conversion;
using System.Collections.Generic;

namespace Pixel_Magic.Classes
{

    public class CustomPixel : IComparable<CustomPixel>
    {
        public Color Color;
        public int x, y;
        public LabColor LAB;
        public Point3D Point;
        public int D;
        public int R;
        public int G;
        public int B;

        public static ComparisonMode CurrentMode = ComparisonMode.Colorspace;
        //private static readonly IColorSpaceComparison _comparer = new CieDe2000Comparison();
        public static ColourfulConverter converter = new ColourfulConverter { WhitePoint = Illuminants.D65 };

        public enum ComparisonMode
        {
            Luminosity,
            Colorspace,
            ColorMine
        }

        public CustomPixel(Color color, int x, int y)
        {
            Color = color;
            this.x = x;
            this.y = y;
            Point = new Point3D(color.R, color.G, color.B); ;
            D = ((color.R + color.G + color.B) / 3);
            R = Color.R;
            G = Color.G;
            B = Color.B;
            LAB = converter.ToLab(new RGBColor(color.R / 255.00, color.G / 255.00, color.B / 255.00));
        }

        //public void SetGray(int d)
        //{
        //    D = d;
        //    Color = Color.FromArgb(d, d, d);

        //}

        public void SetRed(double r)
        {
            if (r > 255.00) r = 255;
            if (r < 0.0) r = 0;
            R = (int)r;
            //Color = Color.FromArgb((int)d, (int)d, (int)d);

        }
        public void SetGreen(double g)
        {
            if (g > 255.00) g = 255;
            if (g < 0.0) g = 0;
            G = (int)g;
            //Color = Color.FromArgb((int)d, (int)d, (int)d);

        }
        public void SetBlue(double b)
        {
            if (b > 255.00) b = 255;
            if (b < 0.0) b = 0;
            B = (int)b;
            //Color = Color.FromArgb((int)d, (int)d, (int)d);

        }


        public void SetGray(double d)
        {
            if (d > 255.00) d = 255;
            if (d < 0.0) d = 0;
            D = (int)d;
            //Color = Color.FromArgb((int)d, (int)d, (int)d);

        }

        public void GrayGenerate()
        {
            Color = Color.FromArgb((int)D, (int)D, (int)D);
        }

        public void ColorGenerate()
        {
            Color = Color.FromArgb((int)R, (int)G, (int)B);
            LAB = converter.ToLab(new RGBColor(Color.R / 255.00, Color.G / 255.00, Color.B / 255.00));
        }

        public int CompareTo(CustomPixel o)
        {
            switch (CurrentMode)
            {
                case ComparisonMode.Luminosity:
                    var lumen1 =
                        Math.Sqrt(Math.Pow(0.299*(o.Color.R), 2) + Math.Pow(0.587*(o.Color.G), 2) +
                                  Math.Pow(0.114*o.Color.B, 2));
                    var lumen2 =
                        Math.Sqrt(Math.Pow(0.299*(Color.R), 2) + Math.Pow(0.587*(Color.G), 2) +
                                  Math.Pow(0.114*Color.B, 2));

                    if (lumen1 > lumen2)
                    {
                       
                        return 1;
                    }
                    if (lumen1 < lumen2)
                        return -1;
                    return 0;

                
                case ComparisonMode.ColorMine:

                    if (LAB.L > o.LAB.L) return 1;
                    if (LAB.L < o.LAB.L) return -1;
                    return 0;


                case ComparisonMode.Colorspace:
                default:
                    var Y1 = 0.299*(Color.R) + 0.587*(Color.G) + 0.11*(Color.B);
                    var U1 = 0.492*(Color.B - Y1);
                    var V1 = 0.877*(Color.R - Y1);


                    var Y2 = 0.299*(o.Color.R) + 0.587*(o.Color.G) + 0.11*(o.Color.B);
                    var U2 = 0.492*(o.Color.B - Y2);
                    var V2 = 0.877*(o.Color.R - Y2);

                    if (((Y1*1) + (U1 + V1)*.5) > ((Y2*1) + (U2 + V2)*.5))
                    {
                        return -1;
                    }
                    if ((((Y1*1) + (U1 + V1)*.5) == ((Y2*1) + (U2 + V2)*.5)))
                    {
                        return 0;
                    }
                    return 1;
            }
        }
    }
}