using Colourful;
using MoreLinq;
using Pixel_Magic.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixel_Magic.Utilities
{
    static class PaletteSorter
    {

        static string raw = @"000000,000033,000066,000099,0000cc,0000ff,003300,003333,003366,003399,0033cc,0033ff,006600,006633,006666,006699,0066cc,0066ff,009900,009933,009966,009999,0099cc,0099ff,00cc00,00cc33,00cc66,00cc99,00cccc,00ccff,00ff00,00ff33,00ff66,00ff99,00ffcc,00ffff,330000,330033,330066,330099,3300cc,3300ff,333300,333333,333366,333399,3333cc,3333ff,336600,336633,336666,336699,3366cc,3366ff,339900,339933,339966,339999,3399cc,3399ff,33cc00,33cc33,33cc66,33cc99,33cccc,33ccff,33ff00,33ff33,33ff66,33ff99,33ffcc,33ffff,660000,660033,660066,660099,6600cc,6600ff,663300,663333,663366,663399,6633cc,6633ff,666600,666633,666666,666699,6666cc,6666ff,669900,669933,669966,669999,6699cc,6699ff,66cc00,66cc33,66cc66,66cc99,66cccc,66ccff,66ff00,66ff33,66ff66,66ff99,66ffcc,66ffff,990000,990033,990066,990099,9900cc,9900ff,993300,993333,993366,993399,9933cc,9933ff,996600,996633,996666,996699,9966cc,9966ff,999900,999933,999966,999999,9999cc,9999ff,99cc00,99cc33,99cc66,99cc99,99cccc,99ccff,99ff00,99ff33,99ff66,99ff99,99ffcc,99ffff,cc0000,cc0033,cc0066,cc0099,cc00cc,cc00ff,cc3300,cc3333,cc3366,cc3399,cc33cc,cc33ff,cc6600,cc6633,cc6666,cc6699,cc66cc,cc66ff,cc9900,cc9933,cc9966,cc9999,cc99cc,cc99ff,cccc00,cccc33,cccc66,cccc99,cccccc,ccccff,ccff00,ccff33,ccff66,ccff99,ccffcc,ccffff,ff0000,ff0033,ff0066,ff0099,ff00cc,ff00ff,ff3300,ff3333,ff3366,ff3399,ff33cc,ff33ff,ff6600,ff6633,ff6666,ff6699,ff66cc,ff66ff,ff9900,ff9933,ff9966,ff9999,ff99cc,ff99ff,ffcc00,ffcc33,ffcc66,ffcc99,ffcccc,ffccff,ffff00,ffff33,ffff66,ffff99,ffffcc,ffffff";


        //public static string raw = "000000, FFFFFF";
        public static List<String> hexCodes;
        public static List<ColorPair> Colors = new List<ColorPair>();
 




        public static void GenerateWebColors()
        {
            var Converter = new ColorConverter();
            //raw = raw.Replace("\t", "").Replace(" ", ",").Replace(System.Environment.NewLine, ",");
            hexCodes = raw.Split(',').ToList();

            foreach (string h in hexCodes)
            {
                Color c = (Color)Converter.ConvertFromString("#" + h.ToUpper());
                LabColor lc = CustomPixel.converter.ToLab(new RGBColor(c.R / 255.00, c.G / 255.00, c.B / 255.00));


                Colors.Add(new ColorPair(c,lc));
            }

            //Colors = Colors.Where((x, i) => i % 16 == 0).ToList();
            Colors = Colors.TakeEvery(4).ToList();
        }

        public static List<Color> GetWebSafe(int take)
        {
            List<Color> list = new List<Color>();
            var Converter = new ColorConverter();
            //raw = raw.Replace("\t", "").Replace(" ", ",").Replace(System.Environment.NewLine, ",");
            hexCodes = raw.Split(',').ToList();

            foreach (string h in hexCodes)
            {
                Color c = (Color)Converter.ConvertFromString("#" + h.ToUpper());
                list.Add(c);
            }

            var s = list.Batch((list.Count/take)).ToList();
            list.Clear();
            foreach (var item in s)
            {
                list.Add(item.First());
            }
            list.Add(Color.White);
            return list;

        }

        public static List<Color> GetAllWebSafe()
        {
            List<Color> list = new List<Color>();
            var Converter = new ColorConverter();
            //raw = raw.Replace("\t", "").Replace(" ", ",").Replace(System.Environment.NewLine, ",");
            hexCodes = raw.Split(',').ToList();

            foreach (string h in hexCodes)
            {
                Color c = (Color)Converter.ConvertFromString("#" + h.ToUpper());
                list.Add(c);
            }


            //return list.OrderBy(x => (x.R + x.G + x.B)).ToList();

             
            return list.RandomSubset(list.Count).ToList();
        }


        public static List<Color> GetRGBPalette()
        {

            List <Color> list = new List<Color>();
            list.Add(Color.FromArgb(25, 25, 25));
            list.Add(Color.FromArgb(50, 50, 50));


            list.Add(Color.FromArgb(190, 190, 190));
            list.Add(Color.FromArgb(225, 225, 225));

            list.Add(Color.FromArgb(138, 51, 36));
            list.Add(Color.FromArgb(255, 112, 61));


            list.Add(Color.FromArgb(76, 90, 48));
            list.Add(Color.FromArgb(171, 252, 78));


            list.Add(Color.FromArgb(135,206,235));
            list.Add(Color.FromArgb(24, 111, 219));

            //======
            list.Add(Color.FromArgb(255, 236, 94));
            list.Add(Color.FromArgb(197, 255, 127));

            list.Add(Color.FromArgb(86, 66, 6));
            list.Add(Color.FromArgb(214, 192, 162));

            list.Add(Color.FromArgb(229, 160, 255));
            list.Add(Color.FromArgb(120, 7, 234));
            return list;
        }

        public static List<Color> GetRandomPalette(int paletteSize)
        {

            Random r1 = new Random();

            List<Color> list = new List<Color>();
            list.Add(Color.FromArgb(25, 25, 25));
            list.Add(Color.FromArgb(50, 50, 50));


            list.Add(Color.FromArgb(190, 190, 190));
            list.Add(Color.FromArgb(225, 225, 225));


            for (int i = 0; i < paletteSize-4; i++)
            {
                list.Add(Color.FromArgb(
                    r1.Next(0, 255),
                    r1.Next(0, 255),
                    r1.Next(0, 255)));
            }


            return list;
        }


        public static Color GetClosestColor(LabColor lc)
        {
            //ColorPair select = (ColorPair)(Colors.OrderBy(x => DeltaE.Distance(x.LAB, lc)).ToList().First());
            ColorPair select = Colors.MinBy(x => DeltaE.Distance(x.LAB, lc));

            return select.Color;
        }
    }
}
