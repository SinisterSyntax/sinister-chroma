using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Media3D;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
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

        public enum ComparisonMode
        {
            Luminosity,
            Colorspace,
            ColorMine
        }

         
        public static ComparisonMode CurrentMode = ComparisonMode.Colorspace;
        private static readonly IColorSpaceComparison _comparer = new CieDe2000Comparison();
        public static ColourfulConverter converter = new ColourfulConverter { WhitePoint = Illuminants.D65 };


        public CustomPixel(Color color, int x, int y)
        {
            Color = color;
            this.x = x;
            this.y = y;
            Point = new Point3D(color.R, color.G, color.B); ;
            LAB = converter.ToLab(new RGBColor(color.R / 255.00, color.G / 255.00, color.B / 255.00));
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