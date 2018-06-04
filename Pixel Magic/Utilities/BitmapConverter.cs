using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace Pixel_Magic.Utilities
{
    public static class BitmapConverter
    {
        internal static class NativeMethods
        {
            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteObject(IntPtr hObject);
        }


        //public static Image ToWpfBitmap(this Bitmap bitmap, int height, int width)
        //{
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        bitmap.Save(stream, ImageFormat.Bmp);

        //        stream.Position = 0;
        //        BitmapImage result = new BitmapImage();
        //        result.BeginInit();
        //        result.CacheOption = BitmapCacheOption.OnLoad;
        //        result.StreamSource = stream;
        //        result.EndInit();
        //        var imageResult = new Image
        //        {
        //            Source = result,
        //            Width = width,
        //            Height = height
        //        };


        //        return imageResult;
        //    }
        //}

        //public static Image ToWpfBitmap(this Bitmap bitmap, double height, double width)
        //{
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        bitmap.Save(stream, ImageFormat.Bmp);

        //        stream.Position = 0;
        //        BitmapImage result = new BitmapImage();
        //        result.BeginInit();
        //        result.CacheOption = BitmapCacheOption.OnLoad;
        //        result.StreamSource = stream;
        //        result.EndInit();
        //        result.Freeze();
        //        var imageResult = new Image
        //        {
        //            Source = result,
        //            Width = width,
        //            Height = height
        //        };

        //        return imageResult;
        //    }
        //}


        public static Image ToBitmapSource(this Bitmap source, double height, double width)
        {
            BitmapSource bitSrc;

            var hBitmap = source.GetHbitmap();

            try
            {
                bitSrc = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            var imageResult = new Image
            {
                Source = bitSrc,
                Width = width,
                Height = height
            };


            return imageResult;
        }
    }
}