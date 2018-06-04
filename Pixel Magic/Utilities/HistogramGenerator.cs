using Pixel_Magic.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image = Pixel_Magic.Classes.Image;

namespace Pixel_Magic.Utilities
{
    static class HistogramGenerator
    {

        public static List<Color> GenerateMedianHistogram(Image Source,int paletteSize)
        {
            ProcessWindow.WriteLine("Bucketing pixels...");



 
            using (StreamWriter outfile = new StreamWriter("C:/Test4.csv"))
            {
                //outfile.Write("R,G,B\n");
                int i = 0;
                foreach (CustomPixel p in Source.PixelList)
                {
                    if (i % (Source.PixelList.Count / 10000) == 0)
                    {
                        outfile.WriteLine($"{Math.Round(p.Color.R / 255.0, 3)},{Math.Round(p.Color.G / 255.0, 3)},{Math.Round(p.Color.B / 255.0, 3)}");
                    }
                    i++;
                }
            }


            using (StreamWriter outfile = new StreamWriter("C:/x.csv"))
            {
                //outfile.Write("X\n");
                int i = 0;
                foreach (CustomPixel p in Source.PixelList)
                {
                    if (i % (Source.PixelList.Count / 10000) == 0)
                    {
                        outfile.WriteLine($"{p.Color.R}");
                    }
                    i++;
                }
            }
            using (StreamWriter outfile = new StreamWriter("C:/y.csv"))
            {
                //outfile.Write("Y\n");
                int i = 0;
                foreach (CustomPixel p in Source.PixelList)
                {
                    if (i % (Source.PixelList.Count / 10000) == 0)
                    {
                        outfile.WriteLine($"{p.Color.G}");
                    }
                    i++;
                }
            }
            using (StreamWriter outfile = new StreamWriter("C:/z.csv"))
            {
                //outfile.Write("Z\n");
                int i = 0;
                foreach (CustomPixel p in Source.PixelList)
                {
                    if (i % (Source.PixelList.Count / 10000) == 0)
                    {
                        outfile.WriteLine($"{p.Color.B}");
                    }
                    i++;
                }
            }


            var sortedR = Source.PixelList.OrderBy(x => x.Color.R).ToList();
            //var firstR = sortedR.First();
            //var lastR = sortedR.Last();
            var medianR = sortedR[sortedR.Count() / 2];
            //var rangeR = lastR.Color.R - firstR.Color.R;
            var stdDevR = DeltaE.StdDev(sortedR.Select(x => (int)x.Color.R).ToList());
            var insideStdDevR = sortedR.Where(x => x.Color.R > medianR.Color.R - (stdDevR / 2) && x.Color.R < medianR.Color.R + (stdDevR / 2)).ToList();

            var x1 = sortedR[sortedR.Count / 3].Color.R;
            var x2 = sortedR[sortedR.Count - sortedR.Count / 3].Color.R;



            var sortedG = Source.PixelList.OrderBy(x => x.Color.G).ToList();
            //var firstG = sortedG.First();
            //var lastG = sortedG.Last();
            var medianG = sortedG[sortedG.Count() / 2];
            //var rangeG = lastG.Color.G - firstG.Color.G;
            var stdDevG = DeltaE.StdDev(sortedG.Select(x => (int)x.Color.G).ToList());
            var insideStdDevG = sortedG.Where(x => x.Color.G > medianR.Color.G - (stdDevG / 2) && x.Color.G < medianG.Color.G + (stdDevG / 2)).ToList();

            var y1 = sortedG[sortedG.Count / 3].Color.G;
            var y2 = sortedG[sortedG.Count - sortedG.Count / 3].Color.G;


            var sortedB = Source.PixelList.OrderBy(x => x.Color.B).ToList();
            //var firstB = sortedB.First();
            //var lastB = sortedB.Last();
            var medianB = sortedB[sortedB.Count() / 2];
            //var rangeB = lastB.Color.B - firstB.Color.B;
            var stdDevB = DeltaE.StdDev(sortedB.Select(x => (int)x.Color.B).ToList());

            var insideStdDevB = sortedB.Where(x => x.Color.B > medianR.Color.B - (stdDevB / 2) && x.Color.B < medianG.Color.B + (stdDevB / 2)).ToList();

            var z1 = sortedB[sortedB.Count / 3].Color.B;
            var z2 = sortedB[sortedB.Count - sortedB.Count / 3].Color.B;

            List<Color> colors = new List<Color>();

            List<List<CustomPixel>> buckets = new List<List<CustomPixel>>();
            var b1 = new List<CustomPixel>(); buckets.Add(b1);
            var b2 = new List<CustomPixel>(); buckets.Add(b2);
            var b3 = new List<CustomPixel>(); buckets.Add(b3);
            var b4 = new List<CustomPixel>(); buckets.Add(b4);
            var b5 = new List<CustomPixel>(); buckets.Add(b5);
            var b6 = new List<CustomPixel>(); buckets.Add(b6);
            var b7 = new List<CustomPixel>(); buckets.Add(b7);
            var b8 = new List<CustomPixel>(); buckets.Add(b8);
            var b9 = new List<CustomPixel>(); buckets.Add(b9);
            var b10 = new List<CustomPixel>(); buckets.Add(b10);
            var b11 = new List<CustomPixel>(); buckets.Add(b11);
            var b12 = new List<CustomPixel>(); buckets.Add(b12);
            var b13 = new List<CustomPixel>(); buckets.Add(b13);
            var b14 = new List<CustomPixel>(); buckets.Add(b14);
            var b15 = new List<CustomPixel>(); buckets.Add(b15);
            var b16 = new List<CustomPixel>(); buckets.Add(b16);
            var b17 = new List<CustomPixel>(); buckets.Add(b17);
            var b18 = new List<CustomPixel>(); buckets.Add(b18);
            var b19 = new List<CustomPixel>(); buckets.Add(b19);
            var b20 = new List<CustomPixel>(); buckets.Add(b20);
            var b21 = new List<CustomPixel>(); buckets.Add(b21);
            var b22 = new List<CustomPixel>(); buckets.Add(b22);
            var b23 = new List<CustomPixel>(); buckets.Add(b23);
            var b24 = new List<CustomPixel>(); buckets.Add(b24);
            var b25 = new List<CustomPixel>(); buckets.Add(b25);
            var b26 = new List<CustomPixel>(); buckets.Add(b26);
            var b27 = new List<CustomPixel>(); buckets.Add(b27);



            foreach (CustomPixel p in Source.PixelList)
            {
                var x = p.Point.X;
                var y = p.Point.Y;
                var z = p.Point.Z;

                if (x < x1 && y < y1 && z < z1)//Bucket 1
                {
                    b1.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y < y1 && z < z1)//Bucket 2
                {
                    b2.Add(p);
                    continue;
                }

                if (x > x2 && y < y1 && z < z1)//Bucket 3
                {
                    b3.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && (y > y1 && y < y2) && z < z1)//Bucket 4
                {
                    b4.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && (y > y1 && y < y2) && z < z1)//Bucket 5
                {
                    b5.Add(p);
                    continue;
                }

                if (x > x2 && (y > y1 && y < y2) && z < z1)//Bucket 6
                {
                    b6.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && y > y2 && z < z1)//Bucket 7
                {
                    b7.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y > y2 && z < z1)//Bucket 8
                {
                    b8.Add(p);
                    continue;
                }

                if (x > x2 && y > y2 && z < z1)//Bucket 9
                {
                    b9.Add(p);
                    continue;
                }

                //=========================================================================================================
                //=========================================================================================================

                if (x < x1 && y < y1 && (z > z1 && z < z2))//Bucket 10
                {
                    b10.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y < y1 && (z > z1 && z < z2))//Bucket 11
                {
                    b11.Add(p);
                    continue;
                }

                if (x > x2 && y < y1 && (z > z1 && z < z2))//Bucket 12
                {
                    b12.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && (y > y1 && y < y2) && (z > z1 && z < z2))//Bucket 13
                {
                    b13.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && (y > y1 && y < y2) && (z > z1 && z < z2))//Bucket 14
                {
                    b14.Add(p);
                    continue;
                }

                if (x > x1 && (y > y1 && y < y2) && (z > z1 && z < z2))//Bucket 15
                {
                    b15.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && y > y2 && (z > z1 && z < z2))//Bucket 16
                {
                    b16.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y > y2 && (z > z1 && z < z2))//Bucket 17
                {
                    b17.Add(p);
                    continue;
                }

                if (x > x2 && y > y2 && (z > z1 && z < z2))//Bucket 18
                {
                    b18.Add(p);
                    continue;
                }

                //=========================================================================================================
                //=========================================================================================================

                if (x < x1 && y < y1 && z > z2)//Bucket 19
                {
                    b19.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y < y1 && z > z2)//Bucket 20
                {
                    b20.Add(p);
                    continue;
                }

                if (x > x2 && y < y1 && z > z2)//Bucket 21
                {
                    b21.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && (y > y1 && y < y2) && z > z2)//Bucket 22
                {
                    b22.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && (y > y1 && y < y2) && z > z2)//Bucket 23
                {
                    b23.Add(p);
                    continue;
                }

                if (x > x2 && (y > y1 && y < y2) && z > z2)//Bucket 24
                {
                    b24.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < x1 && y > y2 && z > z2)//Bucket 25
                {
                    b25.Add(p);
                    continue;
                }

                if ((x > x1 && x < x2) && y > y2 && z > z2)//Bucket 26
                {
                    b26.Add(p);
                    continue;
                }

                if (x > x2 && y > y2 && z > z2)//Bucket 27
                {
                    b27.Add(p);
                    continue;
                }

                //=========================================================================================================
            }

            ProcessWindow.WriteLine("Ordering buckets by count...");

            buckets = buckets.OrderBy(d => d.Count).Reverse().ToList();

            //ProcessWindow.WriteLine($"Take {paletteSize}");
            //buckets = buckets.Take(paletteSize).ToList();
            buckets = buckets.Where(r => r.Count > ((double)Source.PixelList.Count / (buckets.Count * 10))).ToList();

            ProcessWindow.WriteLine("Averaging samples");




            foreach (List<CustomPixel> l in buckets)
            {
                if (l.Count == 0) continue;
                //if (colors.Count >= paletteSize) break;

                int avgR = 0;
                int avgG = 0;
                int avgB = 0;

                foreach (CustomPixel p in l)
                {
                    avgR += p.Color.R;
                    avgG += p.Color.G;
                    avgB += p.Color.B;
                }

                avgR = avgR / l.Count;
                avgG = avgG / l.Count;
                avgB = avgB / l.Count;

                double depth = (double)l.Count / (double)Source.PixelList.Count * buckets.Count;

                if (depth < 1) depth = 1;

                for (int i = 0; i < depth; i++)
                {
                    colors.Add(Color.FromArgb((int)avgR, (int)avgG, (int)avgB));
                }


            }
            ProcessWindow.WriteLine("Finished Generating Colors");
            return colors;
        }

        public static List<Color> GenerateHistogram(Image Source, int paletteSize)
        {
            ProcessWindow.WriteLine("Bucketing pixels...");

            int firstThird = 85;
            int secondThird = 170;

            List<Color> colors = new List<Color>();

            List<List<CustomPixel>> buckets = new List<List<CustomPixel>>();
            var b1 = new List<CustomPixel>(); buckets.Add(b1);
            var b2 = new List<CustomPixel>(); buckets.Add(b2);
            var b3 = new List<CustomPixel>(); buckets.Add(b3);
            var b4 = new List<CustomPixel>(); buckets.Add(b4);
            var b5 = new List<CustomPixel>(); buckets.Add(b5);
            var b6 = new List<CustomPixel>(); buckets.Add(b6);
            var b7 = new List<CustomPixel>(); buckets.Add(b7);
            var b8 = new List<CustomPixel>(); buckets.Add(b8);
            var b9 = new List<CustomPixel>(); buckets.Add(b9);
            var b10 = new List<CustomPixel>(); buckets.Add(b10);
            var b11 = new List<CustomPixel>(); buckets.Add(b11);
            var b12 = new List<CustomPixel>(); buckets.Add(b12);
            var b13 = new List<CustomPixel>(); buckets.Add(b13);
            var b14 = new List<CustomPixel>(); buckets.Add(b14);
            var b15 = new List<CustomPixel>(); buckets.Add(b15);
            var b16 = new List<CustomPixel>(); buckets.Add(b16);
            var b17 = new List<CustomPixel>(); buckets.Add(b17);
            var b18 = new List<CustomPixel>(); buckets.Add(b18);
            var b19 = new List<CustomPixel>(); buckets.Add(b19);
            var b20 = new List<CustomPixel>(); buckets.Add(b20);
            var b21 = new List<CustomPixel>(); buckets.Add(b21);
            var b22 = new List<CustomPixel>(); buckets.Add(b22);
            var b23 = new List<CustomPixel>(); buckets.Add(b23);
            var b24 = new List<CustomPixel>(); buckets.Add(b24);
            var b25 = new List<CustomPixel>(); buckets.Add(b25);
            var b26 = new List<CustomPixel>(); buckets.Add(b26);
            var b27 = new List<CustomPixel>(); buckets.Add(b27);


            foreach (CustomPixel p in Source.PixelList)
            {
                var x = p.Point.X;
                var y = p.Point.Y;
                var z = p.Point.Z;

                if (x < firstThird && y < firstThird && z < firstThird)//Bucket 1
                {
                    b1.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y < firstThird && z < firstThird)//Bucket 2
                {
                    b2.Add(p);
                    continue;
                }

                if (x > secondThird && y < firstThird && z < firstThird)//Bucket 3
                {
                    b3.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && (y > firstThird && y < secondThird) && z < firstThird)//Bucket 4
                {
                    b4.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && (y > firstThird && y < secondThird) && z < firstThird)//Bucket 5
                {
                    b5.Add(p);
                    continue;
                }

                if (x > secondThird && (y > firstThird && y < secondThird) && z < firstThird)//Bucket 6
                {
                    b6.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && y > secondThird && z < firstThird)//Bucket 7
                {
                    b7.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y > secondThird && z < firstThird)//Bucket 8
                {
                    b8.Add(p);
                    continue;
                }

                if (x > secondThird && y > secondThird && z < firstThird)//Bucket 9
                {
                    b9.Add(p);
                    continue;
                }

                //=========================================================================================================
                //=========================================================================================================

                if (x < firstThird && y < firstThird && (z > firstThird && z < secondThird))//Bucket 10
                {
                    b10.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y < firstThird && (z > firstThird && z < secondThird))//Bucket 11
                {
                    b11.Add(p);
                    continue;
                }

                if (x > secondThird && y < firstThird && (z > firstThird && z < secondThird))//Bucket 12
                {
                    b12.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && (y > firstThird && y < secondThird) && (z > firstThird && z < secondThird))//Bucket 13
                {
                    b13.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && (y > firstThird && y < secondThird) && (z > firstThird && z < secondThird))//Bucket 14
                {
                    b14.Add(p);
                    continue;
                }

                if (x > secondThird && (y > firstThird && y < secondThird) && (z > firstThird && z < secondThird))//Bucket 15
                {
                    b15.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && y > secondThird && (z > firstThird && z < secondThird))//Bucket 16
                {
                    b16.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y > secondThird && (z > firstThird && z < secondThird))//Bucket 17
                {
                    b17.Add(p);
                    continue;
                }

                if (x > secondThird && y > secondThird && (z > firstThird && z < secondThird))//Bucket 18
                {
                    b18.Add(p);
                    continue;
                }

                //=========================================================================================================
                //=========================================================================================================

                if (x < firstThird && y < firstThird && z > secondThird)//Bucket 19
                {
                    b19.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y < firstThird && z > secondThird)//Bucket 20
                {
                    b20.Add(p);
                    continue;
                }

                if (x > secondThird && y < firstThird && z > secondThird)//Bucket 21
                {
                    b21.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && (y > firstThird && y < secondThird) && z > secondThird)//Bucket 22
                {
                    b22.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && (y > firstThird && y < secondThird) && z > secondThird)//Bucket 23
                {
                    b23.Add(p);
                    continue;
                }

                if (x > secondThird && (y > firstThird && y < secondThird) && z > secondThird)//Bucket 24
                {
                    b24.Add(p);
                    continue;
                }

                //=========================================================================================================

                if (x < firstThird && y > secondThird && z > secondThird)//Bucket 25
                {
                    b25.Add(p);
                    continue;
                }

                if ((x > firstThird && x < secondThird) && y > secondThird && z > secondThird)//Bucket 26
                {
                    b26.Add(p);
                    continue;
                }

                if (x > secondThird && y > secondThird && z > secondThird)//Bucket 27
                {
                    b27.Add(p);
                    continue;
                }

                //=========================================================================================================
            }
            ProcessWindow.WriteLine("Ordering buckets by count...");

            buckets = buckets.OrderBy(d => d.Count).Reverse().ToList();

            ProcessWindow.WriteLine($"Take {paletteSize}");
            //buckets = buckets.Take(paletteSize).ToList();
            buckets = buckets.Where(r => r.Count > ((double)Source.PixelList.Count / 500)).ToList();

            ProcessWindow.WriteLine("Averaging samples");




            foreach (List<CustomPixel> l in buckets)
            {
                //if (colors.Count >= paletteSize) break;

                double avgR = 0;
                double avgG = 0;
                double avgB = 0;

                foreach (CustomPixel p in l)
                {
                    avgR += p.Color.R;
                    avgG += p.Color.G;
                    avgB += p.Color.B;
                }

                avgR = avgR / l.Count;
                avgG = avgG / l.Count;
                avgB = avgB / l.Count;

                double depth = (double)l.Count / (double)Source.PixelList.Count * 10.00;

                if (depth < 1) depth = 1;

                for (int i = 0; i < depth; i++)
                {
                    colors.Add(Color.FromArgb((int)avgR, (int)avgG, (int)avgB));
                }


            }
            ProcessWindow.WriteLine("Finished Generating Colors");
            return colors;
        }
    }
}
