using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Pixel_Magic.Classes
{
    class Image
    {

        public readonly Bitmap _Original;
        public Bitmap Original { get { return _Original; } }
        public Bitmap Working;

        public static Random rnd = new Random();


        private int WidthOriginal { get; set; }
        private int HeightOriginal { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }


        public List<CustomPixel> PixelList { get; set; }
        //public CustomPixel[] PixelArray { get; set; }
        public CustomPixel[,] Pixel2DArray { get; set; }


        public Image(Bitmap b)
        {
            _Original = new Bitmap(b);
            Working = new Bitmap(b);
            WidthOriginal = _Original.Width;
            HeightOriginal = _Original.Height;
            Width = _Original.Width;
            Height = _Original.Height;
            //ProcessWindow.WriteLine("Converting to List/Array");

           
            


            PixelList = ConvertToList(_Original, Width, Height);
            Pixel2DArray = ConvertTo2DArray(PixelList, Width, Height);


            //ProcessWindow.WriteLine("Image Ready");

        }

        public void Resize(double factor)
        {
            ProcessWindow.WriteLine("Resizing...");
            Working.SetResolution(WidthOriginal * (float)factor, HeightOriginal * (float)factor);

            Bitmap newImage = new Bitmap(Convert.ToInt32(WidthOriginal * factor), Convert.ToInt32(HeightOriginal * factor));
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.CompositingMode = CompositingMode.SourceCopy;
                gr.CompositingQuality = CompositingQuality.HighQuality;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(_Original, new Rectangle(0, 0, Convert.ToInt32(WidthOriginal * factor), Convert.ToInt32(HeightOriginal * factor)));
            }

            Working = new Bitmap(newImage);
            Width = Convert.ToInt32(WidthOriginal * factor);
            Height = Convert.ToInt32(HeightOriginal * factor);
            PixelList = ConvertToList(Working, Width, Height);
            Pixel2DArray = ConvertTo2DArray(PixelList, Width, Height);
        }

        public void Resize(int w, int h)
        {
            ProcessWindow.WriteLine("Resizing...");
            Working.SetResolution(w, h);

            Bitmap newImage = new Bitmap(Convert.ToInt32(w), Convert.ToInt32(h));
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.CompositingMode = CompositingMode.SourceCopy;
                gr.CompositingQuality = CompositingQuality.HighQuality;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(_Original, new Rectangle(0, 0, Convert.ToInt32(w), Convert.ToInt32(h)));
            }

            Working = new Bitmap(newImage);
            Width = Convert.ToInt32(w);
            Height = Convert.ToInt32(h);
            PixelList = ConvertToList(Working, Width, Height);
            Pixel2DArray = ConvertTo2DArray(PixelList, Width, Height);
        }


        public static CustomPixel[,] ConvertTo2DArray(List<CustomPixel> l, int w, int h)
        {
            CustomPixel[,] pixels = new CustomPixel[w, h];

            foreach (CustomPixel p in l)
            {
                pixels[p.x, p.y] = p;
            }
            return pixels;
        }

        public static List<CustomPixel> ConvertToList(Bitmap b, int w, int h)
        {
            
            List<CustomPixel> list = new List<CustomPixel>(w*h);
            var frame = new DispatcherFrame();
            ProcessWindow.Progress.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                ProcessWindow.Progress.Maximum = w;
                ProcessWindow.Progress.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    list.Add(new CustomPixel(b.GetPixel(i, j), i, j));
                }
                ProcessWindow.Progress.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
                {
                    ProcessWindow.Progress.Value = i;
                    frame.Continue = false;
                    return null;
                }), null);
                Dispatcher.PushFrame(frame);

            }
            

            ProcessWindow.Progress.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            { 
                ProcessWindow.Progress.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            return list;
        }

        public List<CustomPixel> Shuffle()
        {
            Random r = new Random();
            List<CustomPixel> test = new List<CustomPixel>();

            foreach (CustomPixel p in PixelList)
            {
                test.Add(p);
            }




            int n = test.Count;
            while (n > 1)
            {
                n--;
                //int k = r.Next(n + 1);
                CustomPixel value = test[k];
                test[k] = test[n];
                test[n] = value;
            }
            //int counter = 0;
            //for (int i = 0; i < Width; i++)
            //{
            //    for (int j = 0; j < Height; j++)
            //    {
            //        test[counter].x = i;
            //        test[counter].y = j;
            //        counter++;
            //    }
            //}
            return test;
        }

        public void ArrayToList()
        {
            PixelList.Clear();
            foreach (CustomPixel p in Pixel2DArray)
            {
                PixelList.Add(p);
            }
        }

    }
}
