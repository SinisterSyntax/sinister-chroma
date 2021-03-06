﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Colourful;
using Colourful.Difference;
using Microsoft.Win32;
using Pixel_Magic.Classes;
using Pixel_Magic.Utilities;
using Timer = System.Timers.Timer;
using Image = Pixel_Magic.Classes.Image;
using System.Threading.Tasks;
using MoreLinq;
using Xceed.Wpf.Toolkit;
using Colourful.Conversion;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using AnimatedGif;

namespace Pixel_Magic
{
    public partial class ProcessWindow : Window, INotifyPropertyChanged
    {
        

        private Image Palette;
        private Image Source;
        private Image Result;

        private List<KeyValuePair<Bitmap, int>> GifBuffer = new List<KeyValuePair<Bitmap, int>>();


        private bool Enabled = false;

        public bool UIEnabled { get { return Enabled; }
            set {


                Enabled = value;
                OnPropertyChanged("UIEnabled");
            }
        }

        private int _DisplayWidth = 0;
        private int _DisplayHeight = 0;
        private string _Resolution = "";

        public int DisplayWidth
        {
            get { return _DisplayWidth; }
            set
            {
                _DisplayWidth = value;
                UpdateResolution();
                OnPropertyChanged("DisplayWidth");
            }
        }

        public int DisplayHeight
        {
            get { return _DisplayHeight; }
            set
            {


                DisplayHeight = value;
                UpdateResolution();
                OnPropertyChanged("DisplayHeight");
            }
        }


        public string Resolution
        {
            get { return _Resolution; }
            set
            {


                _Resolution = value;
                OnPropertyChanged("Resolution");
            }
        }

        private void UpdateResolution()
        {
            if (Source == null)
            {
                _Resolution = "[0 x 0]";
                return;

            }
            _DisplayWidth = (int)(_rm * Source.Original.Width);
            _DisplayHeight = (int)(_rm * Source.Original.Height);
            Resolution = $"[{_DisplayWidth} x {_DisplayHeight}]";
            //OnPropertyChanged("_Resolution");
        }



        public event PropertyChangedEventHandler PropertyChanged;


        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        enum Pattern
        {
            Fan,
            Circular
        }

        private static readonly TaskFactory factory = new TaskFactory(CancellationToken.None,
        TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
        public static ColourfulConverter converter = new ColourfulConverter { WhitePoint = Illuminants.D65};

        private static Random rnd = new Random();


        private static Pattern _patternMode = Pattern.Fan;
        private static List<Bitmap> GifFrames = new List<Bitmap>();
        private static readonly Timer resizeTimer = new Timer(100) { Enabled = false };
        private static readonly Object _locker = new Object();
        private static string ImageDirectory = @"C:\Users\tsova\Documents\Projects\p";
        private static string SaveDirectory = @"C:\Users\tsova\Documents\Projects\s\";
        private static string PaletteDirectory = @"C:\Users\tsova\Documents\Projects\WindowsFormsApp2\WindowsFormsApp2";
        private static string BatchDir = @"C:\Users\tsova\Documents\Projects\Batch\";
        private static string MutateDir = @"C:\Users\tsova\Documents\Projects\Batch\Mutate\";
        private static string TargetDir = @"C:\Users\tsova\Documents\Projects\Batch\Output\Targets\";
        private static string FramesDir = @"C:\Users\tsova\Documents\Projects\s\GIF\Frames\";




        public static bool _break = false;


        //public static double _ditherCenterWeight = 5; //5 for CIE1976
        //public static double _ditherWeight = 1;
        //public static int _ditherLimit = 1000;

        public static double _ditherCenterWeight { get; set; } = 1; //5 for CIE1976
        public static double _ditherWeight { get; set; } = 0.01; //1 for CIE1976 //0.25 for CIE2000?
        public static bool _ditherUpdate { get; set; } = false;
        public static bool _ditherOrdered { get; set; } = false;
        public static int _ditherIterations { get; set; } = 1000;

        

        public static double _rm { get; set; } = 1;
        public static int _iterations { get; set; } = 100;
        public static int _refreshRate { get; set; } = 10;
        public static int _sampleSize { get; set; } = 100;
        public static int _paletteSize = 16;
        private const int _sort_RefreshRate = 15000;
        private static int _ditherPaletteSize = 8;
        private static bool _continuous = true;
        private static int _continuousRefreshRate = int.MaxValue-1;
        public static bool _displayUpdates { get; set; } = false;
        private static int _continuousRatio = 5;
        public static bool _EnableThreshold = true;
        public static float _Threshold = .00025f;


        public static System.Windows.Controls.RichTextBox Console;
        public static System.Windows.Controls.ProgressBar Progress;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ProcessWindow()
        {

            System.Windows.FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
            InitializeComponent();

            PaletteSorter.GenerateWebColors();
            Console = ConsoleBox;
            Progress = ProgressBar1;
            ProcessWindow.WriteLine(" ");
            ProcessWindow.WriteLine("Initializing...");
            DataContext = this;
            resizeTimer.Elapsed += ResizingDone;
            //lblResolution.Text = "[0, 0]";
            UIEnabled = true;
            ProcessWindow.WriteLine("Ready!");
            ProcessWindow.WriteLine("------");
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void ResizingDone(object sender, ElapsedEventArgs e)
        {
            resizeTimer.Stop();
            if (!ImagesPresent()) return;

            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                if (CanvasPalette.Children.Count > 0)
                {
                    CanvasPalette.Children.Clear();
                    CanvasPalette.Children.Insert(0, Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));
                }
                frame.Continue = false;
                return null;
            }), null);
            CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                if (CanvasSource.Children.Count > 0)
                {
                    CanvasSource.Children.Clear();
                    CanvasSource.Children.Insert(0, Source.Working.ToBitmapSource(CanvasSource.ActualHeight, CanvasSource.ActualWidth));
                }
                frame.Continue = false;
                return null;
            }), null);

            if (Result != null) CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
             {
                 //GifFrames.Add(new Bitmap(Result.Working));
                 if (CanvasResult.Children.Count > 0)
                 {
                     CanvasResult.Children.Clear();
                     CanvasResult.Children.Insert(0,
                         Result.Working.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));
                 }
                 frame.Continue = false;
                 return null;
             }), null);
            Dispatcher.PushFrame(frame);
        }

        public void UpdateWindowSize(object sender, RoutedEventArgs e)
        {
            resizeTimer.Stop();
            resizeTimer.Start();
        }

        private void OpenPaletteImage()
        {
            var imageDialog = new OpenFileDialog
            {
                Filter =
                    "All Graphics Types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff" +
                    "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff|"
                    ,
                InitialDirectory = ImageDirectory
            };
            bool? result = imageDialog.ShowDialog();
            if (result != true) return;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Task.Factory.StartNew(() => {


                var frame = new DispatcherFrame();
                bsyPalette.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
                {
                    bsyPalette.IsBusy = true;

                    frame.Continue = false;
                    return null;
                }), null);
                Dispatcher.PushFrame(frame);


                Palette = new Image(new Bitmap(imageDialog.FileName));
                ProcessWindow.WriteLine("Palette: " + imageDialog.FileName);


                CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                {
                    CanvasPalette.Children.Clear();
                    CanvasPalette.Children.Insert(0, Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                    frame.Continue = false;
                    return null;
                }), null);
                Dispatcher.PushFrame(frame);

                bsyPalette.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
                {
                    bsyPalette.IsBusy = false;

                    frame.Continue = false;
                    return null;
                }), null);
                Dispatcher.PushFrame(frame);



            }, _tokenSource.Token,
               TaskCreationOptions.None,
               TaskScheduler.Default)//Note TaskScheduler.Default here
            .ContinueWith(
                    t =>
                    {

                    }
                , TaskScheduler.FromCurrentSynchronizationContext());






            //Palette = new Image(new Bitmap(imageDialog.FileName));
            //ProcessWindow.WriteLine("Palette: " + imageDialog.FileName);
            //CanvasPalette.Children.Clear();
            //CanvasPalette.Children.Insert(0, Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));
        }

        private void OpenSourceImage()
        {

            var imageDialog = new OpenFileDialog
            {
                Filter =
                    "All Graphics Types|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff" +
                    "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff|",
                InitialDirectory = ImageDirectory
            };
            bool? result = imageDialog.ShowDialog();

            if (result != true) return;



            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Task.Factory.StartNew(() => {
                var frame = new DispatcherFrame();
                bsySource.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
                {
                    bsySource.IsBusy = true;

                    frame.Continue = false;
                    return null;
                }), null);




                Source = new Image(new Bitmap(imageDialog.FileName));
                ProcessWindow.WriteLine("Source: " + imageDialog.FileName);



                CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                {
                    CanvasSource.Children.Clear();
                    CanvasSource.Children.Insert(0, Source.Working.ToBitmapSource(CanvasSource.ActualHeight, CanvasSource.ActualWidth));

                    frame.Continue = false;
                    return null;
                }), null);



                bsySource.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
                {
                    bsySource.IsBusy = false;

                    frame.Continue = false;
                    return null;
                }), null);
                Dispatcher.PushFrame(frame);

                UpdateResolution();

            }, _tokenSource.Token,
               TaskCreationOptions.None,
               TaskScheduler.Default)//Note TaskScheduler.Default here
            .ContinueWith(
                    t =>
                    {
                        //finish...
                        //if (OnFinishWorkEventHandler != null)
                        //    OnFinishWorkEventHandler(this, EventArgs.Empty);
                    }
                , TaskScheduler.FromCurrentSynchronizationContext());



            //Task<Image> task = Task<Image>.Run(() =>
            //{
            //    return new Image(new Bitmap(imageDialog.FileName));
            //});



            //Source = task.Result;


        }

        private void OpenImageSequence()
        {
            var videoDialog = new OpenFileDialog
            {
                InitialDirectory = ImageDirectory
            };
            bool? result = videoDialog.ShowDialog();

            if (result != true) return;


            



        }

        private bool ImagesPresent()
        {
            if (Palette == null || Source == null) return false;
            return Palette.Working != null && Source.Working != null;
        }

        public void PrepareImages()
        {

            ProcessWindow.WriteLine("Preparing images...");
            if (!ImagesPresent())
            {
                ProcessWindow.WriteLine("Images not ready");
                return;
            }
            WriteLine($"Original Resolution: ({Source.Width},{Source.Height})");

            Source.Resize(_rm);
            Palette.Resize(Source.Width, Source.Height);

            _continuousRefreshRate = (Source.Width * Source.Height) / _continuousRatio;

            ProcessWindow.WriteLine($"Scaled Resolution: ({Source.Width},{Source.Height})");

            if (Source.Width * Source.Height != Palette.Width * Palette.Height)
                throw new Exception("ImageSizeMismatchExcecption");


            var frame = new DispatcherFrame();
            lblResolution.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate
                {
                    //lblResolution.Text = $"[{Source.Width}, {Source.Height}]";

                    frame.Continue = false;
                    return null;
                }), null);
            Dispatcher.PushFrame(frame);

            for (int i = 0; i < 5; i++)
            {
                GifFrames.Add(new Bitmap(Palette.Working));
            }

            _break = false;
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RandomSortButton_Click(object sender, RoutedEventArgs e)
        {


        }

        private void BestFitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        //=====================================================================
        //=====================================================================
        //=====================================================================
        //=====================================================================

        public void Process_Mutate(string path, Image mp, Image ms, int width, int height)
        {

            Image MutatePalette = new Image(mp.Original);
            MutatePalette.Resize(width, height);
            Random r1 = new Random();
            //Source.Resize(0.5);

            ProcessWindow.WriteLine("Start");

            Bitmap ResultImage = new Bitmap(width, height);
            CustomPixel save;
            int refreshCounter = 0;
            int randomselection1;
            int randomselection2;
            int swapped = 0;
            
            int max = ms.PixelList.Count;


            while (refreshCounter < (width * height * 100))//  (width*height*81)) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            {

                randomselection1 = r1.Next(1, max);
                randomselection2 = r1.Next(1, max);

                if ((Math.Abs(DeltaE.Distance(MutatePalette.PixelList[randomselection1].LAB, ms.PixelList[randomselection2].LAB)) <
                         Math.Abs(DeltaE.Distance(ms.PixelList[randomselection2].LAB, MutatePalette.PixelList[randomselection2].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(MutatePalette.PixelList[randomselection2].LAB, ms.PixelList[randomselection1].LAB)) <
                         Math.Abs(DeltaE.Distance(ms.PixelList[randomselection2].LAB, MutatePalette.PixelList[randomselection2].LAB))))
                {
                    save = MutatePalette.PixelList[randomselection2];
                    MutatePalette.PixelList[randomselection2] = MutatePalette.PixelList[randomselection1];
                    MutatePalette.PixelList[randomselection1] = save;
                    swapped++;
                }

            
                refreshCounter++;

            }


            for (int p = 0; p < ms.PixelList.Count; p++)
            {
                ResultImage.SetPixel(ms.PixelList[p].x,
                    ms.PixelList[p].y,
                    MutatePalette.PixelList[p].Color);
            }

            //ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            //System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            //EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            //EncoderParameters myEncoderParameters = new EncoderParameters(1);
            //myEncoderParameters.Param[0] = myEncoderParameter;



            //DirectoryInfo dir = new DirectoryInfo(SaveDirectory);
            //FileInfo[] files = dir.GetFiles("*" + "Output-" + "*.*");
            //var last = files.OrderBy(f => f.CreationTime)
            //            .ToList().Last();
            //var num = last.Name.Substring(7, 4);

            //string newName = (Convert.ToInt32(num) + 1).ToString("D4");

            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = @"C:\Users\tsova\Documents\Projects\Batch\Output\";
                dlg.FileName = Path.GetFileName(path) + "_" + r1.Next(0,100000); // Default file name
                dlg.DefaultExt = ".png"; // Default file extension
                //dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
                //Result._Original.Save(SaveDirectory + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                //Palette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                ResultImage.Save(@"C:\Users\tsova\Documents\Projects\Batch\Output\" + dlg.FileName + ".png");
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }

            ProcessWindow.WriteLine("Done, swapped: " + swapped + " - " + refreshCounter);

        }

        public void Process_Sort()
        {

            ProcessWindow.WriteLine("Sort:");
            WriteLine("=====");
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();


            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);

            //ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    ProgressBar1.Maximum = Source.Width * Source.Height;
            //    ProgressBar1.Value = 0;
            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);

            ProcessWindow.WriteLine("Sorting Palette");

            Task.Run(() =>
            {
                Palette.PixelList.Sort();
                
            });

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = (ProgressBar1.Maximum / 6 * 3);
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            ProcessWindow.WriteLine("Sorting Source");
            Task.Run(() =>
            {

                Source.PixelList.Sort();

                
            });

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = (ProgressBar1.Maximum / 6 * 4);
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);






            SubtractFrom1 = new Bitmap(Palette.Working);


            using (Graphics graph = Graphics.FromImage(ResultImage))
            {
                Rectangle ImageSize = new Rectangle(0, 0, ResultImage.Width, ResultImage.Height);
                graph.FillRectangle(Brushes.White, ImageSize);
            }


            for (int i = 0; i < Source.PixelList.Count; i++)
            {
                lock (_locker)
                {
                    ResultImage.SetPixel(Source.PixelList[i].x,
                        Source.PixelList[i].y,
                        Palette.PixelList[i].Color);

                    SubtractFrom1.SetPixel(Palette.PixelList[i].x,
                        Palette.PixelList[i].y, Color.Black);
                }

                if ((i % ((ResultImage.Width * ResultImage.Height) / 60) == 0))
                {
                    frame = new DispatcherFrame();
                    CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            CanvasPalette.Children.Clear();
                            lock (_locker)
                            {
                                CanvasPalette.Children.Insert(0,
                                    SubtractFrom1.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));
                            }
                            frame.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame);

                    //Result = new Image(new Bitmap(ResultImage));

                    frame = new DispatcherFrame();
                    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {

                            CanvasResult.Children.Clear();
                            lock (_locker)
                            {
                                GifFrames.Add(new Bitmap(ResultImage));
                                CanvasResult.Children.Insert(0,
                                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));
                            }
                            frame.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame);

                    ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            ProgressBar1.Value = i * 2 + ProgressBar1.Maximum / 6 * 4;
                            frame.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame);
                }
            }
            frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0, Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            frame = new DispatcherFrame();
            CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                CanvasSource.Children.Clear();
                CanvasSource.Children.Insert(0, Source.Working.ToBitmapSource(CanvasSource.ActualHeight, CanvasSource.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            
            
            Result = new Image(new Bitmap(ResultImage));

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                lock (_locker)
                {
                    GifFrames.Add(new Bitmap(ResultImage));
                    CanvasResult.Children.Clear();
                    CanvasResult.Children.Insert(0,
                        ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));
                }
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }

        public void Process_RandomSort()
        {
            ProcessWindow.WriteLine("Random Sample:");
            ProcessWindow.WriteLine("==============");
            PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            int numberSwapped = 0;
            int randomselection;

            //int size = OriginalFirst.Width*OriginalFirst.Height;
            CustomPixel save;
            var sw = Stopwatch.StartNew();
            ProcessWindow.WriteLine("Starting Sampling");
            for (var j = 1; j <= _iterations; j++)
            {
                for (int i = 0; i < Source.PixelList.Count; i++)
                {
                    randomselection = rnd.Next(1, (Source.PixelList.Count));

                    if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection].LAB, Source.PixelList[i].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(Palette.PixelList[i].LAB, Source.PixelList[randomselection].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB))))
                    {
                        save = Palette.PixelList[i];
                        numberSwapped++;
                        Palette.PixelList[i] = Palette.PixelList[randomselection];
                        Palette.PixelList[randomselection] = save;
                    }
                }
                var readout = numberSwapped;
                ProcessWindow.WriteLine("Pixels Swapped: " + readout);
                numberSwapped = 0;


                //Task.Run(() =>{






                //});

                for (int p = 0; p < Source.PixelList.Count; p++)
                {
                    ResultImage.SetPixel(Source.PixelList[p].x,
                        Source.PixelList[p].y,
                        Palette.PixelList[p].Color);
                }

                frame = new DispatcherFrame();
                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                        GifFrames.Add(new Bitmap(ResultImage)); ;
                        CanvasResult.Children.Clear();
                        CanvasResult.Children.Insert(0,
                            ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                        frame.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame);


                ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                        ProgressBar1.Value++;
                        frame.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame);

                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }

            }

            for (int p = 0; p < Source.PixelList.Count; p++)
            {
                ResultImage.SetPixel(Source.PixelList[p].x,
                    Source.PixelList[p].y,
                    Palette.PixelList[p].Color);
            }
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage)); ;
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            Result = new Image(new Bitmap(ResultImage));

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            WriteLine(sw.ElapsedMilliseconds.ToString());
        }

        public void Process_RandomSortContinuous()
        {

            
            ProcessWindow.WriteLine("Random Sample Continuous:");
            ProcessWindow.WriteLine("==============");
            PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            int refreshCounter = 0;
            int randomselection1;
            int randomselection2;
            int displayCounter = 0;
            int swapped = 0;

            //int size = OriginalFirst.Width*OriginalFirst.Height;
            CustomPixel save;

            //for (int i = 0; i < 10; i++)
            //{
            //    GifBuffer.Add(Palette.Working);
            //}
            

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Maximum = _iterations * Source.PixelList.Count;
                ProgressBar1.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            int count = Source.PixelList.Count;
            ProcessWindow.WriteLine("Starting Sampling");

            Stopwatch s = Stopwatch.StartNew();
            while (!_break && !(refreshCounter > Source.PixelList.Count * _iterations)) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            {

                randomselection1 = rnd.Next(1, (count));
                randomselection2 = rnd.Next(1, (count));

                if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection1].LAB, Source.PixelList[randomselection2].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection2].LAB, Source.PixelList[randomselection1].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB))))
                {
                    save = Palette.PixelList[randomselection2];
                    Palette.PixelList[randomselection2] = Palette.PixelList[randomselection1];
                    Palette.PixelList[randomselection1] = save;
                    swapped++;
                }


                refreshCounter++;


                if (refreshCounter % _continuousRefreshRate == 0)
                {

                    ProcessWindow.WriteLine("Swapped: " + swapped);
                    if (swapped < Source.PixelList.Count * _Threshold) break;
                    swapped = 0;
                    ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                    {

                        ProgressBar1.Value = refreshCounter;
                        frame.Continue = false;
                        return null;
                    }), null);
                    Dispatcher.PushFrame(frame);

                    if (_displayUpdates)
                    {

                        displayCounter++;
                        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                        Task.Factory.StartNew(() =>
                        {
                            Bitmap newResult = new Bitmap(Source.Width, Source.Height);

                            for (int p = 0; p < count; p++)
                            {
                                newResult.SetPixel(Source.PixelList[p].x,
                                    Source.PixelList[p].y,
                                    Palette.PixelList[p].Color);
                            }

                            var frame2 = new DispatcherFrame();
                            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                new DispatcherOperationCallback(delegate
                                {
                                //GifFrames.Add(new Bitmap(ResultImage)); ;
                                CanvasResult.Children.Clear();
                                    CanvasResult.Children.Insert(0,
                                        newResult.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                                    frame2.Continue = false;
                                    return null;
                                }), null);
                            //Dispatcher.PushFrame(frame2);

                            if (refreshCounter % 2 == 0)
                            {
                                //GifBuffer.Add(newResult);
                            }



                        }, _tokenSource.Token,
                   TaskCreationOptions.None,
                   TaskScheduler.Default)//Note TaskScheduler.Default here
                .ContinueWith(
                        t =>
                        {
                        //finish...
                        //if (OnFinishWorkEventHandler != null)
                        //    OnFinishWorkEventHandler(this, EventArgs.Empty);
                    }
                    , TaskScheduler.FromCurrentSynchronizationContext());




                    }
                }
            }
            ProcessWindow.WriteLine("======= UI updated:" + displayCounter);
            ProcessWindow.WriteLine("======= " + s.ElapsedMilliseconds);
            ProcessWindow.WriteLine("Finalizing...");
            

            for (int p = 0; p < count; p++)
            {
                ResultImage.SetPixel(Source.PixelList[p].x,
                    Source.PixelList[p].y,
                    Palette.PixelList[p].Color);
            }
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage)); ;
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            Result = new Image(new Bitmap(ResultImage));

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            ProcessWindow.WriteLine("Finished! : " + s.ElapsedMilliseconds);
            _break = false;
            Stop(null, null);

            

        }

        //public void Process_MoverSort()
        //{
        //    ProcessWindow.WriteLine("MoverSort:");
        //    ProcessWindow.WriteLine("==============");
        //    PrepareImages();
        //    DispatcherFrame frame = new DispatcherFrame();

        //    Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
        //    Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


        //    int numberSwapped = 0;
        //    CustomPixel current;
        //    CustomPixel up;
        //    CustomPixel down;
        //    CustomPixel left;
        //    CustomPixel right;

        //    //int size = OriginalFirst.Width*OriginalFirst.Height;
        //    CustomPixel save;
        //    var sw = Stopwatch.StartNew();
        //    ProcessWindow.WriteLine("Starting Sampling");
        //    for (var j = 1; j <= _iterations; j++)
        //    {

        //        for (int x = 0; x < Source.Working.Width; x++)
        //        {
        //            for (int y = 0; y < Source.Working.Height; y++)
        //            {

        //                current = Source.Pixel2DArray[x, y];
        //                up = Source.Pixel2DArray[x, y-1];
        //                down = Source.Pixel2DArray[x, y + 1];
        //                left = Source.Pixel2DArray[x-1, y];
        //                right = Source.Pixel2DArray[x+1, y];

        //                double diffUP = DeltaE.Distance(Palette.Pixel2DArray[x, y].LAB, Source.Pixel2DArray[x, y - 1].LAB);



        //            }
        //        }



        //        for (int i = 0; i < Source.PixelList.Count; i++)
        //        {//foreach pixel

                    
        //            up = Source.p

        //            if ((Math.Abs(DeltaE.Distance(Palette.PixelList[up].LAB, Source.PixelList[i].LAB)) <
        //                 Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB)))
        //                &&
        //                (Math.Abs(DeltaE.Distance(Palette.PixelList[i].LAB, Source.PixelList[up].LAB)) <
        //                 Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB))))
        //            {
        //                save = Palette.PixelList[i];
        //                numberSwapped++;
        //                Palette.PixelList[i] = Palette.PixelList[up];
        //                Palette.PixelList[up] = save;
        //            }
        //        }
        //        var readout = numberSwapped;
        //        ProcessWindow.WriteLine("Pixels Swapped: " + readout);
        //        numberSwapped = 0;


        //        //Task.Run(() =>{






        //        //});

        //        for (int p = 0; p < Source.PixelList.Count; p++)
        //        {
        //            ResultImage.SetPixel(Source.PixelList[p].x,
        //                Source.PixelList[p].y,
        //                Palette.PixelList[p].Color);
        //        }

        //        frame = new DispatcherFrame();
        //        CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
        //            new DispatcherOperationCallback(delegate
        //            {
        //                GifFrames.Add(new Bitmap(ResultImage)); ;
        //                CanvasResult.Children.Clear();
        //                CanvasResult.Children.Insert(0,
        //                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

        //                frame.Continue = false;
        //                return null;
        //            }), null);
        //        Dispatcher.PushFrame(frame);


        //        ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
        //            new DispatcherOperationCallback(delegate
        //            {
        //                ProgressBar1.Value++;
        //                frame.Continue = false;
        //                return null;
        //            }), null);
        //        Dispatcher.PushFrame(frame);

        //        if (_break)
        //        {
        //            ProcessWindow.WriteLine("_____Break_____");
        //            _break = false;
        //            break;
        //        }

        //    }

        //    for (int p = 0; p < Source.PixelList.Count; p++)
        //    {
        //        ResultImage.SetPixel(Source.PixelList[p].x,
        //            Source.PixelList[p].y,
        //            Palette.PixelList[p].Color);
        //    }
        //    frame = new DispatcherFrame();
        //    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
        //    {
        //        GifFrames.Add(new Bitmap(ResultImage)); ;
        //        CanvasResult.Children.Clear();
        //        CanvasResult.Children.Insert(0,
        //            ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

        //        frame.Continue = false;
        //        return null;
        //    }), null);
        //    Dispatcher.PushFrame(frame);
        //    Result = new Image(new Bitmap(ResultImage));

        //    ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
        //    {
        //        ProgressBar1.Value = 0;
        //        frame.Continue = false;
        //        return null;
        //    }), null);
        //    Dispatcher.PushFrame(frame);


        //    WriteLine(sw.ElapsedMilliseconds.ToString());
        //}

        public void Process_RandomSort_Batch()
        {
            Random newR = new Random();
            Image randPalette = GeneratePalette();

            ProcessWindow.WriteLine("Random Sample Batch:");
            ProcessWindow.WriteLine("==============");
            //PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap final = new Bitmap(Source.Width, Source.Height);
            //Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            int refreshCounter = 0;
            int randomselection1;
            int randomselection2;

            //int size = OriginalFirst.Width*OriginalFirst.Height;
            CustomPixel save;

            //ProcessWindow.WriteLine("Starting Sampling");

            //var s = Stopwatch.StartNew();
            while (!(refreshCounter > 100000000)) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            {

                randomselection1 = newR.Next(1, (Source.PixelList.Count));
                randomselection2 = newR.Next(1, (Source.PixelList.Count));

                if ((Math.Abs(DeltaE.Distance(randPalette.PixelList[randomselection1].LAB, Source.PixelList[randomselection2].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, randPalette.PixelList[randomselection2].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(randPalette.PixelList[randomselection2].LAB, Source.PixelList[randomselection1].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, randPalette.PixelList[randomselection2].LAB))))
                {
                    save = randPalette.PixelList[randomselection2];
                    randPalette.PixelList[randomselection2] = randPalette.PixelList[randomselection1];
                    randPalette.PixelList[randomselection1] = save;
                }


                refreshCounter++;

            }

            //ProcessWindow.WriteLine("======= " + s.ElapsedMilliseconds);
            //ProcessWindow.WriteLine("Finalizing...");


            for (int p = 0; p < Source.PixelList.Count; p++)
            {
                final.SetPixel(Source.PixelList[p].x,
                    Source.PixelList[p].y,
                    randPalette.PixelList[p].Color);
            }



            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = SaveDirectory;

                dlg.FileName = Guid.NewGuid().ToString(); // Default file name
                dlg.DefaultExt = ".jpg"; // Default file extension
                dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
                var path = BatchDir + dlg.FileName + ".jpg";
                final.Save(path);
                //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                ProcessWindow.WriteLine("SAVED!");
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }
            


            //ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    ProgressBar1.Value = 0;
            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);
            
            //_break = false;
            //Stop(null, null);



        }

        public void Process_RandomSort_Batch_FixedPalette()
        {
            Random newR = new Random();

            var colors = HistogramGenerator.GenerateRandomColorPair(Source, Source.PixelList.Count/1000);

            Bitmap final = new Bitmap(Source.Width, Source.Height);

            foreach (CustomPixel p in Source.PixelList)
            {


                ColorPair bestMatch = colors.MinBy(x => DeltaE.Distance(x.LAB,p.LAB));

                final.SetPixel(p.x, p.y, bestMatch.Color);

            }

       


            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = SaveDirectory;

                dlg.FileName = Guid.NewGuid().ToString(); // Default file name
                dlg.DefaultExt = ".jpg"; // Default file extension
                dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
                var path = BatchDir + dlg.FileName + ".jpg";
                final.Save(path);
                //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                ProcessWindow.WriteLine("SAVED!");
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }







            //ProcessWindow.WriteLine("Random Sample Batch Fixed Palette:");
            //ProcessWindow.WriteLine("==============");
            ////PrepareImages();
            //DispatcherFrame frame = new DispatcherFrame();

            //Bitmap final = new Bitmap(Source.Width, Source.Height);
            ////Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            //int refreshCounter = 0;
            //int randomselection1;
            //int randomselection2;

            ////int size = OriginalFirst.Width*OriginalFirst.Height;
            //CustomPixel save;

            ////ProcessWindow.WriteLine("Starting Sampling");

            ////var s = Stopwatch.StartNew();
            //while (!(refreshCounter > 100000000)) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            //{

            //    randomselection1 = newR.Next(1, (Source.PixelList.Count));
            //    randomselection2 = newR.Next(1, (Source.PixelList.Count));

            //    if ((Math.Abs(DeltaE.Distance(randPalette.PixelList[randomselection1].LAB, Source.PixelList[randomselection2].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, randPalette.PixelList[randomselection2].LAB)))
            //            &&
            //            (Math.Abs(DeltaE.Distance(randPalette.PixelList[randomselection2].LAB, Source.PixelList[randomselection1].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, randPalette.PixelList[randomselection2].LAB))))
            //    {
            //        save = randPalette.PixelList[randomselection2];
            //        randPalette.PixelList[randomselection2] = randPalette.PixelList[randomselection1];
            //        randPalette.PixelList[randomselection1] = save;
            //    }


            //    refreshCounter++;

            //}

            ////ProcessWindow.WriteLine("======= " + s.ElapsedMilliseconds);
            ////ProcessWindow.WriteLine("Finalizing...");


            //for (int p = 0; p < Source.PixelList.Count; p++)
            //{
            //    final.SetPixel(Source.PixelList[p].x,
            //        Source.PixelList[p].y,
            //        randPalette.PixelList[p].Color);
            //}



            //try
            //{
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;

            //    dlg.FileName = Guid.NewGuid().ToString(); // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    var path = BatchDir + dlg.FileName + ".jpg";
            //    final.Save(path);
            //    //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            //    ProcessWindow.WriteLine("SAVED!");
            //}
            //catch (Exception ex)
            //{
            //    WriteLine(ex.StackTrace);
            //}



            //ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    ProgressBar1.Value = 0;
            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);

            //_break = false;
            //Stop(null, null);



        }

        public void Process_RandomSort_Batch_MutatePalette(int v, bool positive)
        {
            Random newR = new Random();

            //var colors = HistogramGenerator.GenerateRandomColorPair(Source, Source.PixelList.Count / 1000);

            var colors = HistogramGenerator.GenerateRandomColorPair(Source, 64);



            Bitmap final = new Bitmap(Source.Width, Source.Height);

            var result = new List<ColorPair>();


            foreach (ColorPair c in colors)
            {
                if (positive)
                {
                    try
                    {
                        result.Add(new ColorPair(Color.FromArgb(c.Color.R + v, c.Color.G + v, c.Color.B + v), converter.ToLab(new RGBColor((c.Color.R + v) / 255.00 , (c.Color.G + v) / 255.00, (c.Color.B + v) / 255.00))));
                    }
                    catch (ArgumentException e) { }
                }
                else
                {

                    try
                    {
                        result.Add(new ColorPair(Color.FromArgb(c.Color.R - v, c.Color.G - v, c.Color.B - v), converter.ToLab(new RGBColor((c.Color.R - v) / 255.00, (c.Color.G - v) / 255.00, (c.Color.B - v) / 255.00))));
                    }
                    catch (ArgumentException e) { }
                }
            }
  


            foreach (CustomPixel p in Source.PixelList)
            {
                ColorPair bestMatch = result.MinBy(x => DeltaE.Distance(x.LAB, p.LAB));

                final.SetPixel(p.x, p.y, bestMatch.Color);
            }
            //GifBuffer.Add(final);
            //=====
            
            //=====
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = SaveDirectory;

                dlg.FileName = Guid.NewGuid().ToString(); // Default file name
                dlg.DefaultExt = ".jpg"; // Default file extension
                dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
                var path = BatchDir + dlg.FileName + ".jpg";
                final.Save(path);
                //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                ProcessWindow.WriteLine("SAVED!");
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }



            


        }

        public void GenerateByPalette(List<Color> colors)
        {
            

            Bitmap final = new Bitmap(Source.Width, Source.Height);


            //var colors = HistogramGenerator.GenerateMedianHistogram(Source, paletteSize);

            //if(colors.Count != paletteSize)
            //{
            //    return;
            //}

            List<ColorPair> result = HistogramGenerator.GenerateLabValues(colors);
 

            for (int i = 0; i < Source.PixelList.Count; i++)
            {

                ColorPair bestMatch = result.MinBy(x => DeltaE.Distance(x.LAB, Source.PixelList[i].LAB));


                //Result.PixelList.Add(Source.PixelList[i]);
                //Result.PixelList[i].Color = bestMatch.Color;
                //Result.PixelList[i].LAB = bestMatch.LAB;
                final.SetPixel(Source.PixelList[i].x,
                    Source.PixelList[i].y,
                    bestMatch.Color);
            }


            //GifBuffer.Add(final);
            ProcessWindow.WriteLine("Finish === " + colors.Count);


            //try
            //{
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;

            //    dlg.FileName = "G-" + colors.Count; // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    var path = BatchDir + dlg.FileName + ".jpg";
            //    dlg.OverwritePrompt = false;
            //    final.Save(path);
            //    //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            //    ProcessWindow.WriteLine("Finish === " + colors.Count);
            //}
            //catch (Exception ex)
            //{
            //    Thread.Sleep(1000);
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;

            //    dlg.FileName = "G-" + colors.Count; // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    var path = BatchDir + dlg.FileName + ".jpg";
            //    dlg.OverwritePrompt = false;
            //    final.Save(path);
            //    WriteLine(ex.StackTrace);
            //}

            ProcessWindow.WriteLine("Finished!");

        }

        public void GenerateByPaletteOrder(List<Color> colors, int order)
        {


            Bitmap final = new Bitmap(Source.Width, Source.Height);


            //var colors = HistogramGenerator.GenerateMedianHistogram(Source, paletteSize);

            //if(colors.Count != paletteSize)
            //{
            //    return;
            //}

            List<ColorPair> result = HistogramGenerator.GenerateLabValues(colors);


            for (int i = 0; i < Source.PixelList.Count; i++)
            {

                ColorPair bestMatch = result.MinBy(x => DeltaE.Distance(x.LAB, Source.PixelList[i].LAB));


                //Result.PixelList.Add(Source.PixelList[i]);
                //Result.PixelList[i].Color = bestMatch.Color;
                //Result.PixelList[i].LAB = bestMatch.LAB;
                final.SetPixel(Source.PixelList[i].x,
                    Source.PixelList[i].y,
                    bestMatch.Color);
            }


            if (order == 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    final.Save(FramesDir + order + "_" + order + ".png");
                }
            }


            final.Save(FramesDir + order + ".png");

            GifBuffer.Add(new KeyValuePair<Bitmap, int>(final, order));
            ProcessWindow.WriteLine("Finish === " + colors.Count);


            //try
            //{
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;

            //    dlg.FileName = "G-" + colors.Count; // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    var path = BatchDir + dlg.FileName + ".jpg";
            //    dlg.OverwritePrompt = false;
            //    final.Save(path);
            //    //randPalette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            //    ProcessWindow.WriteLine("Finish === " + colors.Count);
            //}
            //catch (Exception ex)
            //{
            //    Thread.Sleep(1000);
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;

            //    dlg.FileName = "G-" + colors.Count; // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    var path = BatchDir + dlg.FileName + ".jpg";
            //    dlg.OverwritePrompt = false;
            //    final.Save(path);
            //    WriteLine(ex.StackTrace);
            //}

            //ProcessWindow.WriteLine("Finished!");

        }

        public void Process_RandomSortContinuous_Multithread()
        {
            //int worker = 0;
            //int io = 0;





            //ThreadPool.GetAvailableThreads(out worker, out io);

            //WriteLine("Thread pool threads available at startup: ");
            //WriteLine("   Worker threads: "+ worker);
            //WriteLine("   Asynchronous I/O threads: " + io);
            //ProcessWindow.WriteLine("Random Sample Continuous:");
            //ProcessWindow.WriteLine("==============");

            PrepareImages();
            Result = new Image(new Bitmap(Source.Original));
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            int refreshCounter = 0;
            int randomselection1;
            int randomselection2;

            BlockingCollection<CustomPixel> Test = new BlockingCollection<CustomPixel>();

            //Palette.Shuffle();

            //foreach (var p in Palette.PixelList)
            //{
            //    Test.Add(p);
            //}

            Action action = () =>
            {

                int counter = 0;
                int swapped = 0;

                while (Test.TryTake(out CustomPixel p1) && Test.TryTake(out CustomPixel p2))
                {
                    counter++;
                    //if (counter % 10000 == 0)
                    //{
                    //    //WriteLine("Update");
                    //    frame = new DispatcherFrame();
                    //    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                    //    {

                    //        CanvasResult.Children.Clear();
                    //        CanvasResult.Children.Insert(0,
                    //            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                    //        frame.Continue = false;
                    //        return null;
                    //    }), null);
                    //    Dispatcher.PushFrame(frame);




                    //    }
                    Color save;
                    LabColor save1;
                    //CustomPixel l;
                    //Test.TryTake(out CustomPixel p1);
                    //Test.TryTake(out CustomPixel p2);


                    //Palette.Pixel2DArray[p1.x, p1.y].Color = Color.Red;
                    //Palette.Pixel2DArray[p2.x, p2.y].Color = Color.Green;



                    if ((Math.Abs(DeltaE.Distance(p1.LAB, Source.Pixel2DArray[p2.x, p2.y].LAB)) < // 
                                 Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB))) // point 2 vs
                                &&
                                (Math.Abs(DeltaE.Distance(p2.LAB, Source.Pixel2DArray[p1.x, p1.y].LAB)) <
                                 Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB))))
                    {

                        swapped++;


                        //l = Palette.Pixel2DArray[p2.x, p2.y];
                        //Palette.Pixel2DArray[p2.x, p2.y] = Palette.Pixel2DArray[p1.x, p1.y];
                        //Palette.Pixel2DArray[p1.x, p1.y] = l;

                        save1 = Palette.Pixel2DArray[p2.x, p2.y].LAB;
                        Palette.Pixel2DArray[p2.x, p2.y].LAB = Palette.Pixel2DArray[p1.x, p1.y].LAB;
                        Palette.Pixel2DArray[p1.x, p1.y].LAB = save1;



                        save = Palette.Pixel2DArray[p2.x, p2.y].Color;
                        Palette.Pixel2DArray[p2.x, p2.y].Color = Palette.Pixel2DArray[p1.x, p1.y].Color;
                        Palette.Pixel2DArray[p1.x, p1.y].Color = save;


                        //int sX = Palette.Pixel2DArray[p2.x, p2.y].x;
                        //Palette.Pixel2DArray[p2.x, p2.y].x = Palette.Pixel2DArray[p1.x, p1.y].x;
                        //Palette.Pixel2DArray[p1.x, p1.y].x = sX;



                        //int sY = Palette.Pixel2DArray[p2.x, p2.y].y;
                        //Palette.Pixel2DArray[p2.x, p2.y].y = Palette.Pixel2DArray[p1.x, p1.y].y;
                        //Palette.Pixel2DArray[p1.x, p1.y].y = sY;

                    }




                    //Test.TryAdd(p2);
                    //Test.TryAdd(p1);


                }
                //Task.Run(() =>
                //{

                //    //WriteLine($"Thread 1 Done: {swapped} Swapped");
                //    //WriteLine("Thread ID: {1}" + Thread.CurrentThread.ManagedThreadId);


                //});
            };

            ////Parallel.Invoke(action);
            Stopwatch s1 = Stopwatch.StartNew();

            for (int i = 0; i <  _iterations; i++)
            {
                //Palette.ArrayToList(); //Clears pixellist and puts 2darray into it
                //Palette.Shuffle(); //shuffles
                //Test = new BlockingCollection<CustomPixel>();
                foreach (var p in Palette.PixelList)
                {
                    Test.Add(p);
                }
                //Test.CompleteAdding();
                //Task.Run(() => action );
                Parallel.Invoke(new ParallelOptions { MaxDegreeOfParallelism= 8},action, action, action, action, action, action, action, action);
                WriteLine("Cycle: " + i);
                //Palette.PixelList = Test.ToList();
                Palette.ArrayToList();
                Palette.Shuffle();


                //frame = new DispatcherFrame();
                //CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
                //{

                //    CanvasResult.Children.Clear();
                //    CanvasResult.Children.Insert(0,
                //        ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                //    frame.Continue = false;
                //    return null;
                //}), null);
                //Dispatcher.PushFrame(frame);


            }

            //{
            //    //Palette.Shuffle();
            //    foreach (var p in Palette.PixelList)
            //    {
            //        Test.Add(p);
            //    }
            //    Parallel.Invoke(action);

            //    //Palette.ArrayToList(); //puts all of 2dPixelArray into PixelList
            //    //Palette.Shuffle(); //Shuffles PixelList






            //    //WriteLine("Ran out");
            //}



            //Palette.Shuffle();



            //for (int i = 0; i < Source.; i++)
            //{

            //}









            //for (int i = 0; i < 9; i++)
            //{





            //}




            //SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            //Task.Factory.StartNew(() => {
            //    int counter = 0;
            //    int swapped = 0;

            //    while(Test.Count > 0)
            //    {
            //        counter++;
            //        //if(counter % 10000 == 0)
            //        //{
            //        //    //WriteLine("Update");
            //        //    frame = new DispatcherFrame();
            //        //    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //        //    {

            //        //        CanvasResult.Children.Clear();
            //        //        CanvasResult.Children.Insert(0,
            //        //            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

            //        //        frame.Continue = false;
            //        //        return null;
            //        //    }), null);
            //        //    Dispatcher.PushFrame(frame);




            //        //}
            //        Color save;
            //        LabColor save1;
            //        Test.TryTake(out CustomPixel p1);
            //        Test.TryTake(out CustomPixel p2);


            //        //Palette.Pixel2DArray[p1.x, p1.y].Color = Color.Red;
            //        //Palette.Pixel2DArray[p2.x, p2.y].Color = Color.Green;



            //        if ((Math.Abs(DeltaE.Distance(p1.LAB, Source.Pixel2DArray[p1.x, p1.y].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB)))
            //            &&
            //            (Math.Abs(DeltaE.Distance(p2.LAB, Source.Pixel2DArray[p1.x, p1.y].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB))))
            //        {

            //            swapped++;
            //            save1 = Palette.Pixel2DArray[p2.x, p2.y].LAB;
            //            Palette.Pixel2DArray[p2.x, p2.y].LAB = Palette.Pixel2DArray[p1.x, p1.y].LAB;
            //            Palette.Pixel2DArray[p1.x, p1.y].LAB = save1;



            //            save = Palette.Pixel2DArray[p2.x, p2.y].Color;
            //            Palette.Pixel2DArray[p2.x, p2.y].Color = Palette.Pixel2DArray[p1.x, p1.y].Color;
            //            Palette.Pixel2DArray[p1.x, p1.y].Color = save;
            //        }






            //    }
            //    WriteLine($"Thread 1 Done: {swapped} Swapped");



            //}, _tokenSource.Token,
            //   TaskCreationOptions.None,
            //   TaskScheduler.Default)//Note TaskScheduler.Default here
            //.ContinueWith(
            //        t =>
            //        {

            //        }
            //    , TaskScheduler.FromCurrentSynchronizationContext());

            //Task.Factory.StartNew(() => {
            //    int counter = 0;
            //    int swapped = 0;

            //    while (Test.Count > 0)
            //    {
            //        counter++;
            //        //if (counter % 10000 == 0)
            //        //{
            //        //    //WriteLine("Update");
            //        //    frame = new DispatcherFrame();
            //        //    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //        //    {

            //        //        CanvasResult.Children.Clear();
            //        //        CanvasResult.Children.Insert(0,
            //        //            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

            //        //        frame.Continue = false;
            //        //        return null;
            //        //    }), null);
            //        //    Dispatcher.PushFrame(frame);




            //        //}
            //        Color save;
            //        LabColor save1;
            //        Test.TryTake(out CustomPixel p1);
            //        Test.TryTake(out CustomPixel p2);


            //        //Palette.Pixel2DArray[p1.x, p1.y].Color = Color.Red;
            //        //Palette.Pixel2DArray[p2.x, p2.y].Color = Color.Green;



            //        if ((Math.Abs(DeltaE.Distance(p1.LAB, Source.Pixel2DArray[p1.x, p1.y].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB)))
            //            &&
            //            (Math.Abs(DeltaE.Distance(p2.LAB, Source.Pixel2DArray[p1.x, p1.y].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.Pixel2DArray[p2.x, p2.y].LAB, p2.LAB))))
            //        {

            //            swapped++;
            //            save1 = Palette.Pixel2DArray[p2.x, p2.y].LAB;
            //            Palette.Pixel2DArray[p2.x, p2.y].LAB = Palette.Pixel2DArray[p1.x, p1.y].LAB;
            //            Palette.Pixel2DArray[p1.x, p1.y].LAB = save1;



            //            save = Palette.Pixel2DArray[p2.x, p2.y].Color;
            //            Palette.Pixel2DArray[p2.x, p2.y].Color = Palette.Pixel2DArray[p1.x, p1.y].Color;
            //            Palette.Pixel2DArray[p1.x, p1.y].Color = save;
            //        }






            //    }
            //    WriteLine($"Thread 2 Done: {swapped} Swapped");



            //}, _tokenSource.Token,
            //   TaskCreationOptions.None,
            //   TaskScheduler.Default)//Note TaskScheduler.Default here
            //.ContinueWith(
            //        t =>
            //        {

            //        }
            //    , TaskScheduler.FromCurrentSynchronizationContext());






            //int size = OriginalFirst.Width*OriginalFirst.Height;
            //CustomPixel save;

            //ProcessWindow.WriteLine("Starting Sampling");

            //var s = Stopwatch.StartNew();
            //while (!_break) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            //{

            //    randomselection1 = rnd.Next(1, (Source.PixelList.Count));
            //    randomselection2 = rnd.Next(1, (Source.PixelList.Count));

            //    if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection1].LAB, Source.PixelList[randomselection2].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB)))
            //            &&
            //            (Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection2].LAB, Source.PixelList[randomselection1].LAB)) <
            //             Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB))))
            //    {
            //        save = Palette.PixelList[randomselection2];
            //        Palette.PixelList[randomselection2] = Palette.PixelList[randomselection1];
            //        Palette.PixelList[randomselection1] = save;
            //    }


            //    refreshCounter++;





            //    if (refreshCounter % _continuousRefreshRate == 0)
            //    {

            //        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            //        Task.Factory.StartNew(() =>
            //        {
            //            Bitmap newResult = new Bitmap(Source.Width, Source.Height);

            //            for (int p = 0; p < Source.PixelList.Count; p++)
            //            {
            //                newResult.SetPixel(Source.PixelList[p].x,
            //                    Source.PixelList[p].y,
            //                    Palette.PixelList[p].Color);
            //            }

            //            var frame2 = new DispatcherFrame();
            //            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
            //                new DispatcherOperationCallback(delegate
            //                {
            //                    //GifFrames.Add(new Bitmap(ResultImage)); ;
            //                    CanvasResult.Children.Clear();
            //                    CanvasResult.Children.Insert(0,
            //                        newResult.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

            //                    frame2.Continue = false;
            //                    return null;
            //                }), null);
            //            Dispatcher.PushFrame(frame2);


            //        }, _tokenSource.Token,
            //   TaskCreationOptions.None,
            //   TaskScheduler.Default)//Note TaskScheduler.Default here
            //.ContinueWith(
            //        t =>
            //        {
            //            //finish...
            //            //if (OnFinishWorkEventHandler != null)
            //            //    OnFinishWorkEventHandler(this, EventArgs.Empty);
            //        }
            //    , TaskScheduler.FromCurrentSynchronizationContext());




            //    }

            //}

            //ProcessWindow.WriteLine("======= " + s.ElapsedMilliseconds);
            //ProcessWindow.WriteLine("Finalizing...");


            //for (int p = 0; p < Source.PixelList.Count; p++)
            //{
            //    ResultImage.SetPixel(Source.PixelList[p].x,
            //        Source.PixelList[p].y,
            //        Palette.PixelList[p].Color);
            //}
            //frame = new DispatcherFrame();
            //CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    GifFrames.Add(new Bitmap(ResultImage)); ;
            //    CanvasResult.Children.Clear();
            //    CanvasResult.Children.Insert(0,
            //        ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);
            //Result = new Image(new Bitmap(ResultImage));

            //ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    ProgressBar1.Value = 0;
            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            ProcessWindow.WriteLine("Finished! " + s1.ElapsedMilliseconds);
            
            //_break = false;
            //Stop(null, null);

        }

        public void Process_BestFit()
        {
            ProcessWindow.WriteLine("Best Fit");
            ProcessWindow.WriteLine("========");

            PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();


            Bitmap ResultImage = new Bitmap(Palette.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Palette.Width, Source.Height);


            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            int randomSelection1;
            double bestMatch1 = 9999999;
            var bestMatchIndex1 = 0;
            double currentValue1;
            int randomSelection2;
            double bestMatch2 = 9999999;
            var bestMatchIndex2 = 0;
            double currentValue2;
            var k = Palette.PixelList.Count / 2;


            ProcessWindow.WriteLine("Starting Sampling");
            for (int j = Palette.PixelList.Count / 2; j < Palette.PixelList.Count; j++)
            {
                for (int i = 0; i < _sampleSize; i++)
                {
                    randomSelection2 = rnd.Next(0, 2) == 1 ? rnd.Next(j, Palette.PixelList.Count) : rnd.Next(0, k);

                    currentValue2 = DeltaE.Distance(Palette.PixelList[randomSelection2].LAB, Source.PixelList[j].LAB);
                    if (currentValue2 < bestMatch2)
                    {
                        bestMatch2 = currentValue2;
                        bestMatchIndex2 = randomSelection2;
                    }

                    randomSelection1 = rnd.Next(0, 2) == 1 ? rnd.Next(j, Palette.PixelList.Count) : rnd.Next(0, k);

                    currentValue1 = DeltaE.Distance(Palette.PixelList[randomSelection1].LAB, Source.PixelList[k].LAB);
                    if (!(currentValue1 < bestMatch1)) continue;
                    bestMatch1 = currentValue1;
                    bestMatchIndex1 = randomSelection1;
                }

                var save = Palette.PixelList[k];
                Palette.PixelList[k] = Palette.PixelList[bestMatchIndex1];
                Palette.PixelList[bestMatchIndex1] = save;
                bestMatch1 = 9999999;

                var save2 = Palette.PixelList[j];
                Palette.PixelList[j] = Palette.PixelList[bestMatchIndex2];
                Palette.PixelList[bestMatchIndex2] = save2;
                bestMatch2 = 9999999;
                if (j % (ResultImage.Width * _refreshRate) == 0)
                {
                    for (int i = 0; i < Source.PixelList.Count; i++)
                    {
                        ResultImage.SetPixel(Source.PixelList[i].x,
                            Source.PixelList[i].y,
                            Palette.PixelList[i].Color);
                    }

                    frame = new DispatcherFrame();
                    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            GifFrames.Add(new Bitmap(ResultImage));
                            CanvasResult.Children.Clear();
                            CanvasResult.Children.Insert(0,
                                ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                            frame.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame);
                }
                k--;
                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }

            for (int i = 0; i < Source.PixelList.Count; i++)
            {
                ResultImage.SetPixel(Source.PixelList[i].x,
                    Source.PixelList[i].y,
                    Palette.PixelList[i].Color);
            }

            Result = new Image(new Bitmap(ResultImage));
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }

        public void Process_BestFitCircular()
        {
            var s = Stopwatch.StartNew();
            ProcessWindow.WriteLine("Best Fit Circular");
            ProcessWindow.WriteLine("========");
            PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();


            Bitmap ResultImage = new Bitmap(Palette.Width, Palette.Height);
            Bitmap SubtractFrom1 = new Bitmap(Palette.Width, Palette.Height);

            for (int i = 0; i < 15; i++)
            {
                //GifBuffer.Add(new Bitmap(Palette.Working));
            }
            

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            List<System.Drawing.Point> usedList = new List<System.Drawing.Point>(Source.Working.Width*Source.Working.Height);
            List<System.Drawing.Point> spiralList = new List<System.Drawing.Point>(Source.Working.Width * Source.Working.Height);

            int X = Source.Width - 1;
            int Y = Source.Height - 1;

            int x, y, dx, dy;
            x = y = dx = 0;
            dy = -1;
            int t = Math.Max(X, Y);
            int maxI = t * t;
            for (int i = 0; i < maxI; i++)
            {
                if ((-X / 2 <= x) && (x <= X / 2) && (-Y / 2 <= y) && (y <= Y / 2))
                {
                    spiralList.Add(new System.Drawing.Point(x + (int)Math.Floor(Source.Width / 2.0) - 1, y + (int)Math.Floor(Source.Height / 2.0) - 1));
                }
                if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }
                x += dx;
                y += dy;
            }


            for (int l = 0; l < spiralList.Count; l++)
            {

                double bestError = 0;
                int BEIx = 0;
                int BEIy = 0;

                try
                {
                    BEIx = spiralList[l].X;
                    BEIy = spiralList[l].Y;
                    bestError = DeltaE.Distance(Palette.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB, Source.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB);

                }
                catch (Exception)
                {

                    throw;
                }



                for (int h = 0; h < _sampleSize; h++)
                {

                    var n = rnd.Next(l, spiralList.Count - 1);

                    try
                    {

                        double error = DeltaE.Distance(Palette.Pixel2DArray[spiralList[n].X, spiralList[n].Y].LAB, Source.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB);

                        if (error < bestError)
                        {
                            BEIx = spiralList[n].X;
                            BEIy = spiralList[n].Y;
                            bestError = error;

                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {

                    }

                    

                }

                try
                {
                    Color save = Palette.Pixel2DArray[spiralList[l].X, spiralList[l].Y].Color;
                    LabColor save1 = Palette.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB;

                    Palette.Pixel2DArray[spiralList[l].X, spiralList[l].Y].Color = Palette.Pixel2DArray[BEIx, BEIy].Color;
                    Palette.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB = Palette.Pixel2DArray[BEIx, BEIy].LAB;

                    Palette.Pixel2DArray[BEIx, BEIy].Color = save;
                    Palette.Pixel2DArray[BEIx, BEIy].LAB = save1;
                }
                catch (Exception)
                {

                    throw;
                }


                if (l % (ResultImage.Width * _refreshRate) == 0)
                {
                   //Task.Run(() =>
                    //{

                        //lock (_locker)
                        //{

                            for (int i = 0; i < Source.PixelList.Count; i++)
                            {


                        ResultImage.SetPixel(Source.PixelList[i].x,
                            Source.PixelList[i].y,
                            Palette.PixelList[i].Color);

                        //ResultImage.SetPixel(0,
                        //        0,
                        //        Color.Violet);

                    }
                    //GifBuffer.Add(new Bitmap(ResultImage));
                            frame = new DispatcherFrame();
                            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                new DispatcherOperationCallback(delegate
                                {
                            //GifFrames.Add(new Bitmap(ResultImage));
                            CanvasResult.Children.Clear();
                                    CanvasResult.Children.Insert(0,
                                        ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                                    frame.Continue = false;
                                    return null;
                                }), null);
                            Dispatcher.PushFrame(frame);
                       // }

                    //});

                }
                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }
            for (int i = 0; i < Source.PixelList.Count; i++)
            {
                ResultImage.SetPixel(Source.PixelList[i].x,
                    Source.PixelList[i].y,
                    Palette.PixelList[i].Color);
            }

            Result = new Image(new Bitmap(ResultImage));
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            WriteLine("* " + s.ElapsedMilliseconds);
            Stop(null, null);
        }

        public void Process_RandomSort_WithPreSort()
        {
            ProcessWindow.WriteLine("Random Sample With Presort");
            ProcessWindow.WriteLine("==========================");
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Palette.Width, Palette.Height);
            Bitmap SubtractFrom1 = new Bitmap(Palette.Width, Palette.Height);

            Palette.PixelList.Sort();
            Source.PixelList.Sort();

            int numberSwapped = 0;
            int randomSelection;

            CustomPixel save;
            ProcessWindow.WriteLine("Starting Sampling");
            for (int j = 1; j <= _iterations; j++)
            {
                for (int i = 0; i < Source.PixelList.Count; i++)
                {
                    randomSelection = rnd.Next(1, (Palette.PixelList.Count));

                    if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomSelection].LAB, Source.PixelList[i].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(Palette.PixelList[i].LAB, Source.PixelList[randomSelection].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[i].LAB, Palette.PixelList[i].LAB))))
                    {
                        save = Palette.PixelList[i];
                        numberSwapped++;
                        Palette.PixelList[i] = Palette.PixelList[randomSelection];
                        Palette.PixelList[randomSelection] = save;
                    }
                }
                var readout = numberSwapped;
                ProcessWindow.WriteLine("Pixels Swapped: " + readout);
                Dispatcher.PushFrame(frame);
                numberSwapped = 0;
                for (int p = 0; p < Source.PixelList.Count; p++)
                {
                    ResultImage.SetPixel(Source.PixelList[p].x,
                        Source.PixelList[p].y,
                        Palette.PixelList[p].Color);
                }

                frame = new DispatcherFrame();
                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                        GifFrames.Add(new Bitmap(ResultImage));
                        CanvasResult.Children.Clear();
                        CanvasResult.Children.Insert(0,
                            ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                        frame.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame);


                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }

            }

            for (int p = 0; p < Source.PixelList.Count; p++)
            {
                ResultImage.SetPixel(Source.PixelList[p].x,
                    Source.PixelList[p].y,
                    Palette.PixelList[p].Color);
            }
            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            Result = new Image(new Bitmap(ResultImage));
        }

        public void Process_Dither()
        {
            ProcessWindow.WriteLine("Dithering");
            ProcessWindow.WriteLine("=========");
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();
            int swapCount = 0;

            //Random r = new Random();
            while (!_break)
            {
                for (int curX = 0; curX < Palette.Width; curX++)
                {
                    for (int curY = 0; curY < Palette.Height; curY++)
                    {

                        var bestErrorIndex = Tuple.Create(curX, curY);
                        int bestErrorX = curX;
                        int bestErrorY = curY;

                        //If the starting pixel is an edge, just ignore and move on
                        if (curX == 0 || curY == 0 || curX == Palette.Width - 1 || curY == Palette.Height - 1)
                            continue;

                        //Select pixels randomly, but dont pick edges
                        var randomPixelX = rnd.Next(1, Palette.Width - 1);// 0-500
                        var randomPixelY = rnd.Next(1, Palette.Height - 1);// 0-500

                        List<CustomPixel> currentPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };


                        List<CustomPixel> randomPointPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> sourceCurrentNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[curX - 1, curY - 1],//1
                        Source.Pixel2DArray[curX, curY - 1],//2
                        Source.Pixel2DArray[curX + 1, curY - 1],//3
                        Source.Pixel2DArray[curX - 1, curY],//4
                        Source.Pixel2DArray[curX, curY],//Center
                        Source.Pixel2DArray[curX + 1, curY],//6
                        Source.Pixel2DArray[curX - 1, curY + 1],//7
                        Source.Pixel2DArray[curX, curY + 1],//8
                        Source.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> sourceRandomPointNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Source.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Source.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Source.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };



                        List<CustomPixel> currentPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> RandomPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };


                        double avg1L = 0;// 
                        double avg1A = 0;// 
                        double avg1B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg1L += _ditherCenterWeight;
                                avg1A += _ditherCenterWeight;
                                avg1B += _ditherCenterWeight;
                            }

                            avg1L += currentPaletteNeighbors[k].LAB.L;
                            avg1A += currentPaletteNeighbors[k].LAB.a;
                            avg1B += currentPaletteNeighbors[k].LAB.b;
                        }
                        avg1L = avg1L / 9;//average the 9 pixels
                        avg1A = avg1A / 9;//average the 9 pixels
                        avg1B = avg1B / 9;//average the 9 pixels
                        LabColor lab1 = new LabColor(avg1L, avg1A, avg1B);

                        double avg2L = 0;// 
                        double avg2A = 0;// 
                        double avg2B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg2L += _ditherCenterWeight;
                                avg2A += _ditherCenterWeight;
                                avg2B += _ditherCenterWeight;
                            }

                            avg2L += randomPointPaletteNeighbors[k].LAB.L;
                            avg2A += randomPointPaletteNeighbors[k].LAB.a;
                            avg2B += randomPointPaletteNeighbors[k].LAB.b;
                        }
                        avg2L = avg2L / 9;//average the 9 pixels
                        avg2A = avg2A / 9;//average the 9 pixels
                        avg2B = avg2B / 9;//average the 9 pixels
                        LabColor lab2 = new LabColor(avg2L, avg2A, avg2B);



                        double avg3L = 0;// 
                        double avg3A = 0;// 
                        double avg3B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg3L += _ditherCenterWeight;
                                avg3A += _ditherCenterWeight;
                                avg3B += _ditherCenterWeight;
                            }

                            avg3L += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                            avg3A += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                            avg3B += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;
                        }
                        avg3L = avg3L / 9;//average the 9 pixels
                        avg3A = avg3A / 9;//average the 9 pixels
                        avg3B = avg3B / 9;//average the 9 pixels
                        LabColor lab3 = new LabColor(avg3L, avg3A, avg3B);


                        double avg4L = 0;// 
                        double avg4A = 0;// 
                        double avg4B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg4L += _ditherCenterWeight;
                                avg4A += _ditherCenterWeight;
                                avg4B += _ditherCenterWeight;
                            }

                            avg4L += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                            avg4A += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                            avg4B += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;
                        }
                        avg4L = avg4L / 9;//average the 9 pixels
                        avg4A = avg4A / 9;//average the 9 pixels
                        avg4B = avg4B / 9;//average the 9 pixels
                        LabColor lab4 = new LabColor(avg4L, avg4A, avg4B);

                        double avg5L = 0;// 
                        double avg5A = 0;// 
                        double avg5B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg5L += _ditherCenterWeight;
                                avg5A += _ditherCenterWeight;
                                avg5B += _ditherCenterWeight;
                            }
                            avg5L += sourceCurrentNeighbors[k].LAB.L;
                            avg5A += sourceCurrentNeighbors[k].LAB.a;
                            avg5B += sourceCurrentNeighbors[k].LAB.b;
                        }
                        avg5L = avg5L / 9;//average the 9 pixels
                        avg5A = avg5A / 9;//average the 9 pixels
                        avg5B = avg5B / 9;//average the 9 pixels
                        LabColor lab5 = new LabColor(avg5L, avg5A, avg5B);


                        double avg6L = 0;// 
                        double avg6A = 0;// 
                        double avg6B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg6L += _ditherCenterWeight;
                                avg6A += _ditherCenterWeight;
                                avg6B += _ditherCenterWeight;
                            }
                            avg6L += sourceRandomPointNeighbors[k].LAB.L;
                            avg6A += sourceRandomPointNeighbors[k].LAB.a;
                            avg6B += sourceRandomPointNeighbors[k].LAB.b;
                        }
                        avg6L = avg6L / 9;//average the 9 pixels
                        avg6A = avg6A / 9;//average the 9 pixels
                        avg6B = avg6B / 9;//average the 9 pixels
                        LabColor lab6 = new LabColor(avg6L, avg6A, avg6B);






                        var distance1 = DeltaE.Distance(lab1, lab5);
                        var distance2 = DeltaE.Distance(lab2, lab6);
                        var currentError = distance1 + distance2;

                        var distance3 = DeltaE.Distance(lab3, lab5);
                        var distance4 = DeltaE.Distance(lab4, lab6);
                        var newError = distance3 + distance4;


                        double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleError2 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalCurrentError = singleError1 + singleError2;


                        double singleProposedError1 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleProposedError2 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalProposedError = singleProposedError1 + singleProposedError2;

                        double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
                        //add weight for single pixel, if distance is too much dont swap
                        //if (Math.Abs(finalError) < 5) singleGate = true;
                        double finalNeighborError = Math.Abs(newError - currentError);

                        if (newError < currentError)
                        //if (newError < currentError && finalNeighborError < (finalSingleError * _ditherWeight))
                        {
                            Color save = Palette.Pixel2DArray[curX, curY].Color;
                            LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;

                            Palette.Pixel2DArray[curX, curY].Color = Palette.Pixel2DArray[randomPixelX, randomPixelY].Color;
                            Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

                            Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = save;
                            Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
                            swapCount++;
                        }
                    }
                }
                var readout = swapCount;
                ProcessWindow.WriteLine("Swapped: " + readout);

                swapCount = 0;
                frame = new DispatcherFrame();
                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                        GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
                        CanvasResult.Children.Clear();
                        CanvasResult.Children.Insert(0,
                            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                        frame.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame);
                Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }

        }

        public void Process_Dither_Advanced()
        {

            ProcessWindow.WriteLine("Dithering");
            ProcessWindow.WriteLine("=========");
            _break = false;
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();
            int swapCount = 0;
            int ditherCount = 0;

            int ditherMultiplier = _ditherOrdered ? 3 : 1;

            var s = Stopwatch.StartNew();
            while (!_break)
            {
                if (ditherCount >= _ditherIterations)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    break;
                }
                for (int curX = 1; curX < Palette.Width; curX+= ditherMultiplier)
                {
                    for (int curY = 1; curY < Palette.Height; curY+= ditherMultiplier)
                    {

                        //var bestErrorIndex = Tuple.Create(curX, curY);
                        int bestErrorX = curX;
                        int bestErrorY = curY;
                        //double bestError = 0; //set to original neighberhood vs originalsourceneighborhood
                        //bool foundBetter = false;


                        //If the starting pixel is an edge, just ignore and move on
                        if (curX == 0 || curY == 0 || curX == Palette.Width - 1 || curY == Palette.Height - 1)
                            continue;

                        //Select pixels randomly, but dont pick edges
                        //var randomPixelX = rnd.Next(1, Palette.Width - 1);// 0-500
                        //var randomPixelY = rnd.Next(1, Palette.Height - 1);// 0-500
                        var randomPixelX = rnd.Next(4, ((Palette.Width/ ditherMultiplier))) * ditherMultiplier - 3;// 0-500
                        var randomPixelY = rnd.Next(4, ((Palette.Height/ ditherMultiplier))) * ditherMultiplier - 3;
                        List<CustomPixel> currentPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };


                        List<CustomPixel> randomPointPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> sourceCurrentNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[curX - 1, curY - 1],//1
                        Source.Pixel2DArray[curX, curY - 1],//2
                        Source.Pixel2DArray[curX + 1, curY - 1],//3
                        Source.Pixel2DArray[curX - 1, curY],//4
                        Source.Pixel2DArray[curX, curY],//Center
                        Source.Pixel2DArray[curX + 1, curY],//6
                        Source.Pixel2DArray[curX - 1, curY + 1],//7
                        Source.Pixel2DArray[curX, curY + 1],//8
                        Source.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> sourceRandomPointNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Source.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Source.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Source.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> currentPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> RandomPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        //-128 to 128 ish
                        double avg1L = 0;// 
                        double avg1A = 0;// 
                        double avg1B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg1L += currentPaletteNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg1A += currentPaletteNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg1B += currentPaletteNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg1L += currentPaletteNeighbors[k].LAB.L;
                                avg1A += currentPaletteNeighbors[k].LAB.a;
                                avg1B += currentPaletteNeighbors[k].LAB.b;

                            }


                        }
                        avg1L = avg1L / (9 + (_ditherCenterWeight - 1));
                        avg1A = avg1A / (9 + (_ditherCenterWeight - 1));
                        avg1B = avg1B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab1 = new LabColor(avg1L, avg1A, avg1B);

                        double avg2L = 0;// 
                        double avg2A = 0;// 
                        double avg2B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg2L += randomPointPaletteNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg2A += randomPointPaletteNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg2B += randomPointPaletteNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg2L += randomPointPaletteNeighbors[k].LAB.L;
                                avg2A += randomPointPaletteNeighbors[k].LAB.a;
                                avg2B += randomPointPaletteNeighbors[k].LAB.b;
                            }


                        }
                        avg2L = avg2L / (9 + (_ditherCenterWeight - 1));
                        avg2A = avg2A / (9 + (_ditherCenterWeight - 1));
                        avg2B = avg2B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab2 = new LabColor(avg2L, avg2A, avg2B);



                        double avg3L = 0;// 
                        double avg3A = 0;// 
                        double avg3B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg3L += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L * _ditherCenterWeight;
                                avg3A += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a * _ditherCenterWeight;
                                avg3B += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg3L += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                                avg3A += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                                avg3B += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;

                            }


                        }
                        avg3L = avg3L / (9 + (_ditherCenterWeight - 1));
                        avg3A = avg3A / (9 + (_ditherCenterWeight - 1));
                        avg3B = avg3B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab3 = new LabColor(avg3L, avg3A, avg3B);


                        double avg4L = 0;// 
                        double avg4A = 0;// 
                        double avg4B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg4L += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L * _ditherCenterWeight;
                                avg4A += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a * _ditherCenterWeight;
                                avg4B += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg4L += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                                avg4A += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                                avg4B += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;

                            }


                        }
                        avg4L = avg4L / (9 + (_ditherCenterWeight - 1));
                        avg4A = avg4A / (9 + (_ditherCenterWeight - 1));
                        avg4B = avg4B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab4 = new LabColor(avg4L, avg4A, avg4B);

                        double avg5L = 0;// 
                        double avg5A = 0;// 
                        double avg5B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg5L += sourceCurrentNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg5A += sourceCurrentNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg5B += sourceCurrentNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg5L += sourceCurrentNeighbors[k].LAB.L;
                                avg5A += sourceCurrentNeighbors[k].LAB.a;
                                avg5B += sourceCurrentNeighbors[k].LAB.b;
                            }

                        }
                        avg5L = avg5L / (9 + (_ditherCenterWeight - 1));
                        avg5A = avg5A / (9 + (_ditherCenterWeight - 1));
                        avg5B = avg5B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab5 = new LabColor(avg5L, avg5A, avg5B);


                        double avg6L = 0;// 
                        double avg6A = 0;// 
                        double avg6B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg6L += sourceRandomPointNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg6A += sourceRandomPointNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg6B += sourceRandomPointNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg6L += sourceRandomPointNeighbors[k].LAB.L;
                                avg6A += sourceRandomPointNeighbors[k].LAB.a;
                                avg6B += sourceRandomPointNeighbors[k].LAB.b;
                            }

                        }
                        avg6L = avg6L / (9 + (_ditherCenterWeight - 1));
                        avg6A = avg6A / (9 + (_ditherCenterWeight - 1));
                        avg6B = avg6B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab6 = new LabColor(avg6L, avg6A, avg6B);






                        var distance1 = DeltaE.Distance(lab1, lab5);
                        var distance2 = DeltaE.Distance(lab2, lab6);
                        var currentError = distance1 + distance2;

                        var distance3 = DeltaE.Distance(lab3, lab5);
                        var distance4 = DeltaE.Distance(lab4, lab6);
                        var newError = distance3 + distance4;


                        double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleError2 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalCurrentError = singleError1 + singleError2;


                        double singleProposedError1 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleProposedError2 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalProposedError = singleProposedError1 + singleProposedError2;

                        double finalSingleError = totalCurrentError - totalProposedError;
                        //double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
                        //add weight for single pixel, if distance is too much dont swap
                        //if (Math.Abs(finalError) < 5) singleGate = true;
                        double finalNeighborError = newError - currentError;

                        //  is the neighborhood better?         
                        if (newError < currentError && finalNeighborError < (finalSingleError * _ditherWeight))
                        //if (finalNeighborError < finalSingleError * _ditherWeight)
                        { 
                            Color save = Palette.Pixel2DArray[curX, curY].Color;
                            LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;

                            Palette.Pixel2DArray[curX, curY].Color = Palette.Pixel2DArray[randomPixelX, randomPixelY].Color;
                            Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

                            Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = save;
                            Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
                            swapCount++;
                        }

                    }
                }
                ditherCount++;
                var readout = swapCount;
                ProcessWindow.WriteLine("Swapped: " + readout);


                if (_ditherUpdate)
                {


                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                    Task.Factory.StartNew(() =>
                    {

                        var frame2 = new DispatcherFrame();
                        CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new DispatcherOperationCallback(delegate
                            {
                            //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
                            CanvasResult.Children.Clear();
                                CanvasResult.Children.Insert(0, ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                                frame2.Continue = false;
                                return null;
                            }), null);
                        Dispatcher.PushFrame(frame2);
                        Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

                    }, _tokenSource.Token,
                   TaskCreationOptions.None,
                   TaskScheduler.Default)//Note TaskScheduler.Default here
                .ContinueWith(
                        t =>
                        {

                        }
                    , TaskScheduler.FromCurrentSynchronizationContext());
                }

                swapCount = 0;

                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Task.Factory.StartNew(() => {

                var frame2 = new DispatcherFrame();
                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                            //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
                            CanvasResult.Children.Clear();
                        CanvasResult.Children.Insert(0,
                            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                        frame2.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame2);
                Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

            }, _tokenSource.Token,
           TaskCreationOptions.None,
           TaskScheduler.Default)//Note TaskScheduler.Default here
        .ContinueWith(
                t =>
                {

                }
            , TaskScheduler.FromCurrentSynchronizationContext());
            s.Stop();
            ProcessWindow.WriteLine("Finished! - " + s.ElapsedMilliseconds);
            
            Stop(null, null);
        }

        public void Process_Dither_Advanced_XYZ_Average()
        {

            ProcessWindow.WriteLine("Dithering");
            ProcessWindow.WriteLine("=========");
            _break = false;
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();
            int swapCount = 0;
            int ditherCount = 0;


            while (!_break)
            {
                if (ditherCount >= _ditherIterations)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    break;
                }
                for (int curX = 0; curX < Palette.Width; curX++)
                {
                    for (int curY = 0; curY < Palette.Height; curY++)
                    {

                        //var bestErrorIndex = Tuple.Create(curX, curY);
                        int bestErrorX = curX;
                        int bestErrorY = curY;
                        //double bestError = 0; //set to original neighberhood vs originalsourceneighborhood
                        //bool foundBetter = false;


                        //If the starting pixel is an edge, just ignore and move on
                        if (curX == 0 || curY == 0 || curX == Palette.Width - 1 || curY == Palette.Height - 1)
                            continue;

                        //Select pixels randomly, but dont pick edges
                        var randomPixelX = rnd.Next(1, Palette.Width - 1);// 0-500
                        var randomPixelY = rnd.Next(1, Palette.Height - 1);// 0-500

                        List<CustomPixel> currentPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };


                        List<CustomPixel> randomPointPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> sourceCurrentNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[curX - 1, curY - 1],//1
                        Source.Pixel2DArray[curX, curY - 1],//2
                        Source.Pixel2DArray[curX + 1, curY - 1],//3
                        Source.Pixel2DArray[curX - 1, curY],//4
                        Source.Pixel2DArray[curX, curY],//Center
                        Source.Pixel2DArray[curX + 1, curY],//6
                        Source.Pixel2DArray[curX - 1, curY + 1],//7
                        Source.Pixel2DArray[curX, curY + 1],//8
                        Source.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> sourceRandomPointNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Source.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Source.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Source.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };



                        List<CustomPixel> currentPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> RandomPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };


  

                        

                        double a1x = 0;// 
                        double a1y = 0;// 
                        double a1z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(currentPaletteNeighbors[k].LAB);
                            if (k == 4)
                            {
                                a1x += p.X * _ditherCenterWeight;
                                a1y += p.Y * _ditherCenterWeight;
                                a1z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a1x += p.X;
                                a1y += p.Y;
                                a1z += p.Z;
                            }
                        }
                        a1x = a1x / (9 + (_ditherCenterWeight - 1));
                        a1y = a1y / (9 + (_ditherCenterWeight - 1));
                        a1z = a1z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab1 = converter.ToLab(new XYZColor(a1x, a1y, a1z));


                        double a2x = 0;// 
                        double a2y = 0;// 
                        double a2z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(randomPointPaletteNeighbors[k].LAB);
                            if (k == 4)
                            {
                                a2x += p.X * _ditherCenterWeight;
                                a2y += p.Y * _ditherCenterWeight;
                                a2z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a2x += p.X;
                                a2y += p.Y;
                                a2z += p.Z;
                            }
                        }
                        a2x = a2x / (9 + (_ditherCenterWeight - 1));
                        a2y = a2y / (9 + (_ditherCenterWeight - 1));
                        a2z = a2z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab2 = converter.ToLab(new XYZColor(a2x, a2y, a2z));



                        double a3x = 0;// 
                        double a3y = 0;// 
                        double a3z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB);
                            if (k == 4)
                            {
                                a3x += p.X * _ditherCenterWeight;
                                a3y += p.Y * _ditherCenterWeight;
                                a3z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a3x += p.X;
                                a3y += p.Y;
                                a3z += p.Z;
                            }
                        }
                        a3x = a3x / (9 + (_ditherCenterWeight - 1));
                        a3y = a3y / (9 + (_ditherCenterWeight - 1));
                        a3z = a3z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab3 = converter.ToLab(new XYZColor(a3x, a3y, a3z));


                        double a4x = 0;// 
                        double a4y = 0;// 
                        double a4z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB);
                            if (k == 4)
                            {
                                a4x += p.X * _ditherCenterWeight;
                                a4y += p.Y * _ditherCenterWeight;
                                a4z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a4x += p.X;
                                a4y += p.Y;
                                a4z += p.Z;
                            }
                        }
                        a4x = a4x / (9 + (_ditherCenterWeight - 1));
                        a4y = a4y / (9 + (_ditherCenterWeight - 1));
                        a4z = a4z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab4 = converter.ToLab(new XYZColor(a4x, a4y, a4z));


                        double a5x = 0;// 
                        double a5y = 0;// 
                        double a5z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(sourceCurrentNeighbors[k].LAB);
                            if (k == 4)
                            {
                                a5x += p.X * _ditherCenterWeight;
                                a5y += p.Y * _ditherCenterWeight;
                                a5z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a5x += p.X;
                                a5y += p.Y;
                                a5z += p.Z;
                            }
                        }
                        a5x = a5x / (9 + (_ditherCenterWeight - 1));
                        a5y = a5y / (9 + (_ditherCenterWeight - 1));
                        a5z = a5z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab5 = converter.ToLab(new XYZColor(a5x, a5y, a5z));


                        double a6x = 0;// 
                        double a6y = 0;// 
                        double a6z = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            var p = converter.ToXYZ(sourceRandomPointNeighbors[k].LAB);
                            if (k == 4)
                            {
                                a6x += p.X * _ditherCenterWeight;
                                a6y += p.Y * _ditherCenterWeight;
                                a6z += p.Z * _ditherCenterWeight;
                            }
                            else
                            {
                                a6x += p.X;
                                a6y += p.Y;
                                a6z += p.Z;
                            }
                        }
                        a6x = a6x / (9 + (_ditherCenterWeight - 1));
                        a6y = a6y / (9 + (_ditherCenterWeight - 1));
                        a6z = a6z / (9 + (_ditherCenterWeight - 1));
                        LabColor lab6 = converter.ToLab(new XYZColor(a6x, a6y, a6z));






                        var distance1 = DeltaE.Distance(lab1, lab5);
                        var distance2 = DeltaE.Distance(lab2, lab6);
                        var currentError = distance1 + distance2;

                        var distance3 = DeltaE.Distance(lab3, lab5);
                        var distance4 = DeltaE.Distance(lab4, lab6);
                        var newError = distance3 + distance4;


                        double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleError2 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalCurrentError = singleError1 + singleError2;


                        double singleProposedError1 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleProposedError2 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalProposedError = singleProposedError1 + singleProposedError2;

                        //double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
                        double finalSingleError = totalProposedError - totalCurrentError;
                        //add weight for single pixel, if distance is too much dont swap
                        //if (Math.Abs(finalError) < 5) singleGate = true;
                        double finalNeighborError = newError - currentError;

                        //  is the neighborhood better?         
                        if (newError < currentError && finalNeighborError < (finalSingleError * _ditherWeight))
                        {
                            Color save = Palette.Pixel2DArray[curX, curY].Color;
                            LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;

                            Palette.Pixel2DArray[curX, curY].Color = Palette.Pixel2DArray[randomPixelX, randomPixelY].Color;
                            Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

                            Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = save;
                            Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
                            swapCount++;
                        }

                    }
                }
                ditherCount++;
                var readout = swapCount;
                ProcessWindow.WriteLine("Swapped: " + readout);
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                Task.Factory.StartNew(() => {

                    var frame2 = new DispatcherFrame();
                    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
                            CanvasResult.Children.Clear();
                            CanvasResult.Children.Insert(0,
                                ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                            frame2.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame2);
                    Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

                }, _tokenSource.Token,
               TaskCreationOptions.None,
               TaskScheduler.Default)//Note TaskScheduler.Default here
            .ContinueWith(
                    t =>
                    {

                    }
                , TaskScheduler.FromCurrentSynchronizationContext());












                swapCount = 0;


                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }

        public void Process_Dither_Advanced_Grid()
        {

            ProcessWindow.WriteLine("Dithering_Grid");
            ProcessWindow.WriteLine("=========");
            _break = false;
            PrepareImages();

            DispatcherFrame frame = new DispatcherFrame();
            int swapCount = 0;
            int ditherCount = 0;

            //int ditherMultiplier = _ditherOrdered ? 3 : 1;
            int offset = 1;

            var s = Stopwatch.StartNew();
            while (!_break)
            {
                if (ditherCount >= _ditherIterations)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    break;
                }
                for (int curX = 1; curX < Palette.Width; curX += 1)
                {
                    if (curX % 2 == 0)
                    {
                        offset = 2;
                    }
                    else
                    {
                        offset = 1;
                    }

                    for (int curY = offset; curY < Palette.Height; curY += 2)
                    {
                        

                        //var bestErrorIndex = Tuple.Create(curX, curY);
                        int bestErrorX = curX;
                        int bestErrorY = curY;
                        //double bestError = 0; //set to original neighberhood vs originalsourceneighborhood
                        //bool foundBetter = false;


                        //If the starting pixel is an edge, just ignore and move on
                        if (curX == 0 || curY == 0 || curX == Palette.Width - 1 || curY == Palette.Height - 1)
                            continue;

                        //Select pixels randomly, but dont pick edges
                        //var randomPixelX = rnd.Next(1, Palette.Width - 1);// 0-500
                        //var randomPixelY = rnd.Next(1, Palette.Height - 1);// 0-500


                        var randomPixelX = rnd.Next(1, Palette.Width - 1);
                        var randomPixelY = 0;

                        if (randomPixelX % 2 == 0)
                        {
                            randomPixelY = (rnd.Next(1, (Palette.Height-1) / 2) * 2);
                        }
                        else
                        {
                            randomPixelY = (rnd.Next(1, (Palette.Height-1) / 2) * 2) + 1;
                        }


                        //var randomPixelX = rnd.Next(1, Palette.Width - 1);
                        //var randomPixelY = 0;

                        //if (randomPixelX % 2 == 0)
                        //{
                        //    int r1 = rnd.Next(1, Palette.Height);
                        //    while (r1 % 2 != 0)
                        //    {
                        //        r1 = rnd.Next(1, Palette.Height);
                        //    }

                        //    randomPixelY = r1;
                        //}
                        //else
                        //{
                        //    int r1 = rnd.Next(1, Palette.Height);
                        //    while (r1 % 2 == 0)
                        //    {
                        //        r1 = rnd.Next(1, Palette.Height);
                        //    }

                        //    randomPixelY = r1;
                        //}


                        //Color save = Palette.Pixel2DArray[curX, curY].Color;
                        //LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;
                        //if (Palette.Pixel2DArray[curX, curY].Color != Color.GreenYellow)
                        //{
                        //    Palette.Pixel2DArray[curX, curY].Color = Color.Red;
                        //}

                        //Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

                        //Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = Color.GreenYellow;
                        //Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
                        //swapCount++;





                        List<CustomPixel> currentPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };


                        List<CustomPixel> randomPointPaletteNeighbors = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> sourceCurrentNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[curX - 1, curY - 1],//1
                        Source.Pixel2DArray[curX, curY - 1],//2
                        Source.Pixel2DArray[curX + 1, curY - 1],//3
                        Source.Pixel2DArray[curX - 1, curY],//4
                        Source.Pixel2DArray[curX, curY],//Center
                        Source.Pixel2DArray[curX + 1, curY],//6
                        Source.Pixel2DArray[curX - 1, curY + 1],//7
                        Source.Pixel2DArray[curX, curY + 1],//8
                        Source.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> sourceRandomPointNeighbors = new List<CustomPixel>
                    {
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Source.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Source.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Source.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Source.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Source.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        List<CustomPixel> currentPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[curX - 1, curY - 1],//1
                        Palette.Pixel2DArray[curX, curY - 1],//2
                        Palette.Pixel2DArray[curX + 1, curY - 1],//3
                        Palette.Pixel2DArray[curX - 1, curY],//4
                        Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
                        Palette.Pixel2DArray[curX + 1, curY],//6
                        Palette.Pixel2DArray[curX - 1, curY + 1],//7
                        Palette.Pixel2DArray[curX, curY + 1],//8
                        Palette.Pixel2DArray[curX + 1, curY + 1]//9
                    };

                        List<CustomPixel> RandomPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
                    {
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
                        Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
                        Palette.Pixel2DArray[curX, curY],//Center
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
                        Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
                        Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
                        Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
                    };

                        //-128 to 128 ish
                        double avg1L = 0;// 
                        double avg1A = 0;// 
                        double avg1B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg1L += currentPaletteNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg1A += currentPaletteNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg1B += currentPaletteNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg1L += currentPaletteNeighbors[k].LAB.L;
                                avg1A += currentPaletteNeighbors[k].LAB.a;
                                avg1B += currentPaletteNeighbors[k].LAB.b;

                            }


                        }
                        avg1L = avg1L / (9 + (_ditherCenterWeight - 1));
                        avg1A = avg1A / (9 + (_ditherCenterWeight - 1));
                        avg1B = avg1B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab1 = new LabColor(avg1L, avg1A, avg1B);

                        double avg2L = 0;// 
                        double avg2A = 0;// 
                        double avg2B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg2L += randomPointPaletteNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg2A += randomPointPaletteNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg2B += randomPointPaletteNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg2L += randomPointPaletteNeighbors[k].LAB.L;
                                avg2A += randomPointPaletteNeighbors[k].LAB.a;
                                avg2B += randomPointPaletteNeighbors[k].LAB.b;
                            }


                        }
                        avg2L = avg2L / (9 + (_ditherCenterWeight - 1));
                        avg2A = avg2A / (9 + (_ditherCenterWeight - 1));
                        avg2B = avg2B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab2 = new LabColor(avg2L, avg2A, avg2B);



                        double avg3L = 0;// 
                        double avg3A = 0;// 
                        double avg3B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg3L += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L * _ditherCenterWeight;
                                avg3A += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a * _ditherCenterWeight;
                                avg3B += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg3L += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                                avg3A += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                                avg3B += currentPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;

                            }


                        }
                        avg3L = avg3L / (9 + (_ditherCenterWeight - 1));
                        avg3A = avg3A / (9 + (_ditherCenterWeight - 1));
                        avg3B = avg3B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab3 = new LabColor(avg3L, avg3A, avg3B);


                        double avg4L = 0;// 
                        double avg4A = 0;// 
                        double avg4B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg4L += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L * _ditherCenterWeight;
                                avg4A += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a * _ditherCenterWeight;
                                avg4B += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg4L += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.L;
                                avg4A += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.a;
                                avg4B += RandomPaletteNeighborsWITHCENTERSWAPPED[k].LAB.b;

                            }


                        }
                        avg4L = avg4L / (9 + (_ditherCenterWeight - 1));
                        avg4A = avg4A / (9 + (_ditherCenterWeight - 1));
                        avg4B = avg4B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab4 = new LabColor(avg4L, avg4A, avg4B);

                        double avg5L = 0;// 
                        double avg5A = 0;// 
                        double avg5B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg5L += sourceCurrentNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg5A += sourceCurrentNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg5B += sourceCurrentNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg5L += sourceCurrentNeighbors[k].LAB.L;
                                avg5A += sourceCurrentNeighbors[k].LAB.a;
                                avg5B += sourceCurrentNeighbors[k].LAB.b;
                            }

                        }
                        avg5L = avg5L / (9 + (_ditherCenterWeight - 1));
                        avg5A = avg5A / (9 + (_ditherCenterWeight - 1));
                        avg5B = avg5B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab5 = new LabColor(avg5L, avg5A, avg5B);


                        double avg6L = 0;// 
                        double avg6A = 0;// 
                        double avg6B = 0;// 
                        for (int k = 0; k < 9; k++)
                        {
                            if (k == 4)
                            {
                                avg6L += sourceRandomPointNeighbors[k].LAB.L * _ditherCenterWeight;
                                avg6A += sourceRandomPointNeighbors[k].LAB.a * _ditherCenterWeight;
                                avg6B += sourceRandomPointNeighbors[k].LAB.b * _ditherCenterWeight;
                            }
                            else
                            {
                                avg6L += sourceRandomPointNeighbors[k].LAB.L;
                                avg6A += sourceRandomPointNeighbors[k].LAB.a;
                                avg6B += sourceRandomPointNeighbors[k].LAB.b;
                            }

                        }
                        avg6L = avg6L / (9 + (_ditherCenterWeight - 1));
                        avg6A = avg6A / (9 + (_ditherCenterWeight - 1));
                        avg6B = avg6B / (9 + (_ditherCenterWeight - 1));
                        LabColor lab6 = new LabColor(avg6L, avg6A, avg6B);






                        var distance1 = DeltaE.Distance(lab1, lab5);
                        var distance2 = DeltaE.Distance(lab2, lab6);
                        var currentError = distance1 + distance2;

                        var distance3 = DeltaE.Distance(lab3, lab5);
                        var distance4 = DeltaE.Distance(lab4, lab6);
                        var newError = distance3 + distance4;


                        double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleError2 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalCurrentError = singleError1 + singleError2;


                        double singleProposedError1 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleProposedError2 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalProposedError = singleProposedError1 + singleProposedError2;

                        double finalSingleError = totalCurrentError - totalProposedError;
                        //double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
                        //add weight for single pixel, if distance is too much dont swap
                        //if (Math.Abs(finalError) < 5) singleGate = true;
                        double finalNeighborError = newError - currentError;

                        //  is the neighborhood better?         
                        if (newError < currentError && finalNeighborError < (finalSingleError * _ditherWeight))
                        //if (finalNeighborError < finalSingleError * _ditherWeight)
                        {
                            Color save = Palette.Pixel2DArray[curX, curY].Color;
                            LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;

                            Palette.Pixel2DArray[curX, curY].Color = Palette.Pixel2DArray[randomPixelX, randomPixelY].Color;
                            Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

                            Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = save;
                            Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
                            swapCount++;
                        }

                    }
                }
                ditherCount++;
                var readout = swapCount;
                ProcessWindow.WriteLine("Swapped: " + readout);


                if (_ditherUpdate)
                {


                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                    Task.Factory.StartNew(() =>
                    {

                        var frame2 = new DispatcherFrame();
                        CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new DispatcherOperationCallback(delegate
                            {
                                //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
                                CanvasResult.Children.Clear();
                                CanvasResult.Children.Insert(0, ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                                frame2.Continue = false;
                                return null;
                            }), null);
                        Dispatcher.PushFrame(frame2);
                        Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

                    }, _tokenSource.Token,
                   TaskCreationOptions.None,
                   TaskScheduler.Default)//Note TaskScheduler.Default here
                .ContinueWith(
                        t =>
                        {

                        }
                    , TaskScheduler.FromCurrentSynchronizationContext());
                }

                swapCount = 0;

                if (_break)
                {
                    ProcessWindow.WriteLine("_____Break_____");
                    _break = false;
                    break;
                }
            }

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Task.Factory.StartNew(() => {

                var frame2 = new DispatcherFrame();
                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(delegate
                    {
                        //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
                        CanvasResult.Children.Clear();
                        CanvasResult.Children.Insert(0,
                            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                        frame2.Continue = false;
                        return null;
                    }), null);
                Dispatcher.PushFrame(frame2);
                Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

            }, _tokenSource.Token,
           TaskCreationOptions.None,
           TaskScheduler.Default)//Note TaskScheduler.Default here
        .ContinueWith(
                t =>
                {

                }
            , TaskScheduler.FromCurrentSynchronizationContext());
            s.Stop();
            ProcessWindow.WriteLine("Finished! - " + s.ElapsedMilliseconds);

            Stop(null, null);
        }

        //public void Process_Dither_Advanced_Grid_BW()
        //{

        //    ProcessWindow.WriteLine("Dithering_Grid");
        //    ProcessWindow.WriteLine("=========");
        //    _break = false;
        //    PrepareImages();

        //    CustomPixel black = new CustomPixel(Color.Black, 0, 0);
        //    CustomPixel white = new CustomPixel(Color.White, 0, 0);

        //    DispatcherFrame frame = new DispatcherFrame();
        //    int swapCount = 0;
        //    int ditherCount = 0;

            

        //    int ditherMultiplier = _ditherOrdered ? 3 : 1;
        //    int offset = 1;

        //    var s = Stopwatch.StartNew();
        //    while (!_break)
        //    {
        //        if (ditherCount >= _ditherIterations)
        //        {
        //            ProcessWindow.WriteLine("_____Break_____");
        //            break;
        //        }
        //        for (int curX = 1; curX < Source.Width; curX += ditherMultiplier)
        //        {
        //            for (int curY = 1; curY < Source.Height; curY += ditherMultiplier)
        //            {

        //                //var bestErrorIndex = Tuple.Create(curX, curY);
        //                int bestErrorX = curX;
        //                int bestErrorY = curY;
        //                //double bestError = 0; //set to original neighberhood vs originalsourceneighborhood
        //                //bool foundBetter = false;


        //                //If the starting pixel is an edge, just ignore and move on
        //                if (curX == 0 || curY == 0 || curX == Source.Width - 1 || curY == Source.Height - 1)
        //                    continue;

        //                //Select pixels randomly, but dont pick edges
        //                //var randomPixelX = rnd.Next(1, Palette.Width - 1);// 0-500
        //                //var randomPixelY = rnd.Next(1, Palette.Height - 1);// 0-500
        //                var randomPixelX = rnd.Next(4, ((Source.Width / ditherMultiplier))) * ditherMultiplier - 3;// 0-500
        //                var randomPixelY = rnd.Next(4, ((Source.Height / ditherMultiplier))) * ditherMultiplier - 3;





        //            //    List<CustomPixel> currentPaletteNeighbors = new List<
        //            >
        //            //{
        //            //    Source.Pixel2DArray[curX - 1, curY - 1],//1
        //            //    Source.Pixel2DArray[curX, curY - 1],//2
        //            //    Source.Pixel2DArray[curX + 1, curY - 1],//3
        //            //    Source.Pixel2DArray[curX - 1, curY],//4
        //            //    Source.Pixel2DArray[curX, curY],//Center
        //            //    Source.Pixel2DArray[curX + 1, curY],//6
        //            //    Source.Pixel2DArray[curX - 1, curY + 1],//7
        //            //    Source.Pixel2DArray[curX, curY + 1],//8
        //            //    Source.Pixel2DArray[curX + 1, curY + 1]//9
        //            //};


        //            //    List<CustomPixel> randomPointPaletteNeighbors = new List<CustomPixel>
        //            //{
        //            //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
        //            //    Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
        //            //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
        //            //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
        //            //    Palette.Pixel2DArray[randomPixelX, randomPixelY],//Center
        //            //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
        //            //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
        //            //    Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
        //            //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
        //            //};

        //            List<CustomPixel> sourceCurrentNeighbors = new List<CustomPixel>
        //            {
        //                Palette.Pixel2DArray[curX - 1, curY - 1],//1
        //                Palette.Pixel2DArray[curX, curY - 1],//2
        //                Palette.Pixel2DArray[curX + 1, curY - 1],//3
        //                Palette.Pixel2DArray[curX - 1, curY],//4
        //                Palette.Pixel2DArray[curX, curY],//Center
        //                Palette.Pixel2DArray[curX + 1, curY],//6
        //                Palette.Pixel2DArray[curX - 1, curY + 1],//7
        //                Palette.Pixel2DArray[curX, curY + 1],//8
        //                Palette.Pixel2DArray[curX + 1, curY + 1]//9
        //            };

        //                //    List<CustomPixel> sourceRandomPointNeighbors = new List<CustomPixel>
        //                //{
        //                //    Source.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
        //                //    Source.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
        //                //    Source.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
        //                //    Source.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
        //                //    Source.Pixel2DArray[randomPixelX, randomPixelY],//Center
        //                //    Source.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
        //                //    Source.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
        //                //    Source.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
        //                //    Source.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
        //                //};

        //                List<CustomPixel> SourceBlack = new List<CustomPixel>
        //            {
        //                Source.Pixel2DArray[curX - 1, curY - 1],//1
        //                Source.Pixel2DArray[curX, curY - 1],//2
        //                Source.Pixel2DArray[curX + 1, curY - 1],//3
        //                Source.Pixel2DArray[curX - 1, curY],//4
        //                black,//Center
        //                Source.Pixel2DArray[curX + 1, curY],//6
        //                Source.Pixel2DArray[curX - 1, curY + 1],//7
        //                Source.Pixel2DArray[curX, curY + 1],//8
        //                Source.Pixel2DArray[curX + 1, curY + 1]//9
        //            };

        //                List<CustomPixel> SourceWhite = new List<CustomPixel>
        //            {
        //                Source.Pixel2DArray[curX - 1, curY - 1],//1
        //                Source.Pixel2DArray[curX, curY - 1],//2
        //                Source.Pixel2DArray[curX + 1, curY - 1],//3
        //                Source.Pixel2DArray[curX - 1, curY],//4
        //                white,//Center
        //                Source.Pixel2DArray[curX + 1, curY],//6
        //                Source.Pixel2DArray[curX - 1, curY + 1],//7
        //                Source.Pixel2DArray[curX, curY + 1],//8
        //                Source.Pixel2DArray[curX + 1, curY + 1]//9
        //            };

        //                //List<CustomPixel> RandomPaletteNeighborsWITHCENTERSWAPPED = new List<CustomPixel>
        //                //{
        //                //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY - 1],//1
        //                //    Palette.Pixel2DArray[randomPixelX, randomPixelY - 1],//2
        //                //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY - 1],//3
        //                //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY],//4
        //                //    Palette.Pixel2DArray[curX, curY],//Center
        //                //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY],//6
        //                //    Palette.Pixel2DArray[randomPixelX - 1, randomPixelY + 1],//7
        //                //    Palette.Pixel2DArray[randomPixelX, randomPixelY + 1],//8
        //                //    Palette.Pixel2DArray[randomPixelX + 1, randomPixelY + 1]//9
        //                //};

        //                //-128 to 128 ish
        //                double avg1L = 0;// 
        //                double avg1A = 0;// 
        //                double avg1B = 0;// 
        //                for (int k = 0; k < 9; k++)
        //                {
        //                    if (k == 4)
        //                    {
        //                        avg1L += sourceCurrentNeighbors[k].LAB.L * _ditherCenterWeight;
        //                        avg1A += sourceCurrentNeighbors[k].LAB.a * _ditherCenterWeight;
        //                        avg1B += sourceCurrentNeighbors[k].LAB.b * _ditherCenterWeight;
        //                    }
        //                    else
        //                    {
        //                        avg1L += sourceCurrentNeighbors[k].LAB.L;
        //                        avg1A += sourceCurrentNeighbors[k].LAB.a;
        //                        avg1B += sourceCurrentNeighbors[k].LAB.b;

        //                    }


        //                }
        //                avg1L = avg1L / (9 + (_ditherCenterWeight - 1));
        //                avg1A = avg1A / (9 + (_ditherCenterWeight - 1));
        //                avg1B = avg1B / (9 + (_ditherCenterWeight - 1));
        //                LabColor original = new LabColor(avg1L, avg1A, avg1B);

        //                //double avg2L = 0;// 
        //                //double avg2A = 0;// 
        //                //double avg2B = 0;// 
        //                //for (int k = 0; k < 9; k++)
        //                //{
        //                //    if (k == 4)
        //                //    {
        //                //        avg2L += randomPointPaletteNeighbors[k].LAB.L * _ditherCenterWeight;
        //                //        avg2A += randomPointPaletteNeighbors[k].LAB.a * _ditherCenterWeight;
        //                //        avg2B += randomPointPaletteNeighbors[k].LAB.b * _ditherCenterWeight;
        //                //    }
        //                //    else
        //                //    {
        //                //        avg2L += randomPointPaletteNeighbors[k].LAB.L;
        //                //        avg2A += randomPointPaletteNeighbors[k].LAB.a;
        //                //        avg2B += randomPointPaletteNeighbors[k].LAB.b;
        //                //    }


        //                //}
        //                //avg2L = avg2L / (9 + (_ditherCenterWeight - 1));
        //                //avg2A = avg2A / (9 + (_ditherCenterWeight - 1));
        //                //avg2B = avg2B / (9 + (_ditherCenterWeight - 1));
        //                //LabColor lab2 = new LabColor(avg2L, avg2A, avg2B);



        //                double avg3L = 0;// 
        //                double avg3A = 0;// 
        //                double avg3B = 0;// 
        //                for (int k = 0; k < 9; k++)
        //                {
        //                    if (k == 4)
        //                    {
        //                        avg3L += SourceBlack[k].LAB.L * _ditherCenterWeight;
        //                        avg3A += SourceBlack[k].LAB.a * _ditherCenterWeight;
        //                        avg3B += SourceBlack[k].LAB.b * _ditherCenterWeight;
        //                    }
        //                    else
        //                    {
        //                        avg3L += SourceBlack[k].LAB.L;
        //                        avg3A += SourceBlack[k].LAB.a;
        //                        avg3B += SourceBlack[k].LAB.b;

        //                    }


        //                }
        //                avg3L = avg3L / (9 + (_ditherCenterWeight - 1));
        //                avg3A = avg3A / (9 + (_ditherCenterWeight - 1));
        //                avg3B = avg3B / (9 + (_ditherCenterWeight - 1));
        //                LabColor blackswap = new LabColor(avg3L, avg3A, avg3B);


        //                double avg4L = 0;// 
        //                double avg4A = 0;// 
        //                double avg4B = 0;// 
        //                for (int k = 0; k < 9; k++)
        //                {
        //                    if (k == 4)
        //                    {
        //                        avg4L += SourceWhite[k].LAB.L * _ditherCenterWeight;
        //                        avg4A += SourceWhite[k].LAB.a * _ditherCenterWeight;
        //                        avg4B += SourceWhite[k].LAB.b * _ditherCenterWeight;
        //                    }
        //                    else
        //                    {
        //                        avg4L += SourceWhite[k].LAB.L;
        //                        avg4A += SourceWhite[k].LAB.a;
        //                        avg4B += SourceWhite[k].LAB.b;

        //                    }


        //                }
        //                avg4L = avg4L / (9 + (_ditherCenterWeight - 1));
        //                avg4A = avg4A / (9 + (_ditherCenterWeight - 1));
        //                avg4B = avg4B / (9 + (_ditherCenterWeight - 1));
        //                LabColor whiteswap = new LabColor(avg4L, avg4A, avg4B);

        //                //double avg5L = 0;// 
        //                //double avg5A = 0;// 
        //                //double avg5B = 0;// 
        //                //for (int k = 0; k < 9; k++)
        //                //{
        //                //    if (k == 4)
        //                //    {
        //                //        avg5L += sourceCurrentNeighbors[k].LAB.L * _ditherCenterWeight;
        //                //        avg5A += sourceCurrentNeighbors[k].LAB.a * _ditherCenterWeight;
        //                //        avg5B += sourceCurrentNeighbors[k].LAB.b * _ditherCenterWeight;
        //                //    }
        //                //    else
        //                //    {
        //                //        avg5L += sourceCurrentNeighbors[k].LAB.L;
        //                //        avg5A += sourceCurrentNeighbors[k].LAB.a;
        //                //        avg5B += sourceCurrentNeighbors[k].LAB.b;
        //                //    }

        //                //}
        //                //avg5L = avg5L / (9 + (_ditherCenterWeight - 1));
        //                //avg5A = avg5A / (9 + (_ditherCenterWeight - 1));
        //                //avg5B = avg5B / (9 + (_ditherCenterWeight - 1));
        //                //LabColor lab5 = new LabColor(avg5L, avg5A, avg5B);


        //                //double avg6L = 0;// 
        //                //double avg6A = 0;// 
        //                //double avg6B = 0;// 
        //                //for (int k = 0; k < 9; k++)
        //                //{
        //                //    if (k == 4)
        //                //    {
        //                //        avg6L += sourceRandomPointNeighbors[k].LAB.L * _ditherCenterWeight;
        //                //        avg6A += sourceRandomPointNeighbors[k].LAB.a * _ditherCenterWeight;
        //                //        avg6B += sourceRandomPointNeighbors[k].LAB.b * _ditherCenterWeight;
        //                //    }
        //                //    else
        //                //    {
        //                //        avg6L += sourceRandomPointNeighbors[k].LAB.L;
        //                //        avg6A += sourceRandomPointNeighbors[k].LAB.a;
        //                //        avg6B += sourceRandomPointNeighbors[k].LAB.b;
        //                //    }

        //                //}
        //                //avg6L = avg6L / (9 + (_ditherCenterWeight - 1));
        //                //avg6A = avg6A / (9 + (_ditherCenterWeight - 1));
        //                //avg6B = avg6B / (9 + (_ditherCenterWeight - 1));
        //                //LabColor lab6 = new LabColor(avg6L, avg6A, avg6B);






        //                //var distance1 = DeltaE.Distance(lab1, lab5);
        //                //var distance2 = DeltaE.Distance(lab2, lab6);
        //                //var currentError = distance1 + distance2;

        //                //var distance3 = DeltaE.Distance(lab3, lab5);
        //                //var distance4 = DeltaE.Distance(lab4, lab6);
        //                //var newError = distance3 + distance4;


        //                //double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
        //                //double singleError2 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
        //                //double totalCurrentError = singleError1 + singleError2;


        //                //double singleProposedError1 = DeltaE.Distance(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
        //                //double singleProposedError2 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
        //                //double totalProposedError = singleProposedError1 + singleProposedError2;


        //                //double originalError = DeltaE.Distance(lab1, lab5);

        //                //double wbs = DeltaE.Distance(blackswap, lab);

        //                double wbs = DeltaE.Distance(original, blackswap);
        //                double wws = DeltaE.Distance(original, whiteswap);


        //                if (wbs < wws)
        //                {
        //                    Source.Pixel2DArray[curX, curY].Color = Color.Black;
        //                    Source.Pixel2DArray[curX, curY].LAB = new LabColor(0, 0, 0);
        //                }
        //                else
        //                {
        //                    Source.Pixel2DArray[curX, curY].Color = Color.White;
        //                    Source.Pixel2DArray[curX, curY].LAB = new LabColor(100, 0, 0);
        //                }

        //                // finalSingleError = totalCurrentError - totalProposedError;
        //                //double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
        //                //add weight for single pixel, if distance is too much dont swap
        //                //if (Math.Abs(finalError) < 5) singleGate = true;
        //                //double finalNeighborError = newError - currentError;

        //                //  is the neighborhood better?         
        //                //if (newError < currentError && finalNeighborError < (finalSingleError * _ditherWeight))
        //                ////if (finalNeighborError < finalSingleError * _ditherWeight)
        //                //{
        //                //    Color save = Palette.Pixel2DArray[curX, curY].Color;
        //                //    LabColor save1 = Palette.Pixel2DArray[curX, curY].LAB;

        //                //    Palette.Pixel2DArray[curX, curY].Color = Palette.Pixel2DArray[randomPixelX, randomPixelY].Color;
        //                //    Palette.Pixel2DArray[curX, curY].LAB = Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB;

        //                //    Palette.Pixel2DArray[randomPixelX, randomPixelY].Color = save;
        //                //    Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB = save1;
        //                //    swapCount++;
        //                //}

        //            }
        //        }
        //        ditherCount++;
        //        var readout = swapCount;
        //        ProcessWindow.WriteLine("Swapped: " + readout);


        //        if (_ditherUpdate)
        //        {


        //            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        //            Task.Factory.StartNew(() =>
        //            {

        //                var frame2 = new DispatcherFrame();
        //                CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
        //                    new DispatcherOperationCallback(delegate
        //                    {
        //                        //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
        //                        CanvasResult.Children.Clear();
        //                        CanvasResult.Children.Insert(0, ConvertToBitmap(Source.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

        //                        frame2.Continue = false;
        //                        return null;
        //                    }), null);
        //                Dispatcher.PushFrame(frame2);
        //                Result = new Image(ConvertToBitmap(Source.Pixel2DArray));

        //            }, _tokenSource.Token,
        //           TaskCreationOptions.None,
        //           TaskScheduler.Default)//Note TaskScheduler.Default here
        //        .ContinueWith(
        //                t =>
        //                {

        //                }
        //            , TaskScheduler.FromCurrentSynchronizationContext());
        //        }

        //        swapCount = 0;

        //        if (_break)
        //        {
        //            ProcessWindow.WriteLine("_____Break_____");
        //            _break = false;
        //            break;
        //        }
        //    }

        //    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        //    Task.Factory.StartNew(() => {

        //        var frame2 = new DispatcherFrame();
        //        CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
        //            new DispatcherOperationCallback(delegate
        //            {
        //                //GifBuffer.Add(ConvertToBitmap(Palette.Pixel2DArray));
        //                CanvasResult.Children.Clear();
        //                CanvasResult.Children.Insert(0,
        //                    ConvertToBitmap(Source.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

        //                frame2.Continue = false;
        //                return null;
        //            }), null);
        //        Dispatcher.PushFrame(frame2);
        //        Result = new Image(ConvertToBitmap(Source.Pixel2DArray));

        //    }, _tokenSource.Token,
        //   TaskCreationOptions.None,
        //   TaskScheduler.Default)//Note TaskScheduler.Default here
        //.ContinueWith(
        //        t =>
        //        {

        //        }
        //    , TaskScheduler.FromCurrentSynchronizationContext());
        //    s.Stop();
        //    ProcessWindow.WriteLine("Finished! - " + s.ElapsedMilliseconds);

        //    Stop(null, null);
        //}


        //=====================================================================
        //=====================================================================
        //=====================================================================
        //=====================================================================






        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (((ComboBoxItem)sender).Content.Equals("Colorspace"))
                CustomPixel.CurrentMode = CustomPixel.ComparisonMode.Colorspace;
            else if (((ComboBoxItem)sender).Content.Equals("Luminosity"))
                CustomPixel.CurrentMode = CustomPixel.ComparisonMode.Luminosity;
            else if (((ComboBoxItem)sender).Content.Equals("ColorMine"))
                CustomPixel.CurrentMode = CustomPixel.ComparisonMode.ColorMine;
        }

        private void SaveImage()
        {

            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = SaveDirectory;
                dlg.FileName = "Output-"; // Default file name
                dlg.DefaultExt = ".png"; // Default file extension
                dlg.Filter = "Image (.png)|*.png"; // Filter files by extension
                bool? result = dlg.ShowDialog();
                if (result == true)
                {
                    Result._Original.Save(dlg.FileName);
                }
            }
            catch (Exception)
            {
            }
        }

        private void QuickSave()
        {

            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = myEncoderParameter;



            //DirectoryInfo dir = new DirectoryInfo(SaveDirectory);
            //FileInfo[] files = dir.GetFiles("*" + "Output-" + "*.*");
            //var last = files.OrderBy(f => f.CreationTime)
            //            .ToList().Last();
            //var num = last.Name.Substring(7, 4);

            //string newName = (Convert.ToInt32(num) + 1).ToString("D4");


            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = SaveDirectory;
                dlg.FileName = "Output-" + rnd.Next(5000, 10000); // Default file name
                dlg.DefaultExt = ".png"; // Default file extension
                dlg.Filter = "Image (.png)|*.png"; // Filter files by extension
                Result._Original.Save(SaveDirectory + dlg.FileName + ".png");
                Palette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".png");


      
                
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }


            //try
            //{
            //    SaveFileDialog dlg = new SaveFileDialog();
            //    dlg.InitialDirectory = SaveDirectory;
            //    dlg.FileName = "Output-" + rnd.Next(5000, 10000); // Default file name
            //    dlg.DefaultExt = ".jpg"; // Default file extension
            //    dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
            //    Result._Original.Save(SaveDirectory + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            //    Palette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            //}
            //catch (Exception ex)
            //{
            //    WriteLine(ex.StackTrace);
            //}
        }

        private Boolean TextBoxTextAllowed(String text)
        {
            return Array.TrueForAll(text.ToCharArray(),
                c => Char.IsDigit(c) || Char.IsControl(c));
        }

        private void IterationsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _iterations = Convert.ToInt32(((TextBox)sender).Text);
        }

        private void IterationsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextBoxTextAllowed(e.Text);
        }

        private void SampleSizeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextBoxTextAllowed(e.Text);
        }

        private void SampleSizeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _sampleSize = Convert.ToInt32(((TextBox)sender).Text);
        }

        private void RefreshRateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _refreshRate = Convert.ToInt32(((TextBox)sender).Text);
        }

        private void RefreshRateTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !TextBoxTextAllowed(e.Text);
        }

        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SaveResultMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveImage();
        }

        private void QuickSaveResultMenuItem_Click(object sender, RoutedEventArgs e)
        {
            QuickSave();
        }

        private void OpenSource(object sender, RoutedEventArgs e)
        {
            string[] images = Directory.GetFiles(ImageDirectory);

            int index = rnd.Next(0, images.Length);

            Source = new Image(new Bitmap(images[index]));

            CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                CanvasSource.Children.Clear();
                CanvasSource.Children.Insert(0, Source.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));


                return null;
            }), null);
        }

        private void OpenPalette(object sender, RoutedEventArgs e)
        {
            string[] images = Directory.GetFiles(ImageDirectory);

            int index = rnd.Next(0, images.Length);

            Palette = new Image(new Bitmap(images[index]));


            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0, Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));


                return null;
            }), null);

        }

        private void ContinuousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var continuous = ((CheckBox)sender).IsChecked;

            if (continuous != null && continuous == true)
            {
                _iterations = 1000;
                IterationsTextBox.Text = _iterations.ToString();
            }
            else
            {
                _iterations = 25;
                IterationsTextBox.Text = _iterations.ToString();
            }

        }

        private void Break_Click(object sender, RoutedEventArgs e)
        {
            _break = true;
        }

        private void DitherButton_Click(object sender, RoutedEventArgs e)
        {
            if (Palette == null) Palette = Result;
            if (!ImagesPresent() || Result == null) return;
            //Thread ditherThread = new Thread(Process_Dither);
            Thread ditherThread = new Thread(Process_Dither_Advanced);
            Palette = new Image(new Bitmap(Result._Original));
            ditherThread.Start();

        }

        public Bitmap ConvertToBitmap(CustomPixel[,] p)
        {
            Bitmap r = new Bitmap(Palette.Width, Palette.Height);

            foreach (CustomPixel pix in p)
            {
                r.SetPixel(pix.x, pix.y, pix.Color);
            }
            return r;

        }

        private void ResetFrames_Click(object sender, RoutedEventArgs e)
        {
            GifFrames = new List<Bitmap>();
            GifFrames.Add(new Bitmap(Palette.Working));

            //GifBuffer = new List<Bitmap>();
            //GifBuffer.Add(new Bitmap(Palette.Working));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        public static void WriteLine(string text)
        {
            Console.Dispatcher.Invoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate
            {
                Console.AppendText(text + "\r");
                Console.ScrollToEnd();
                return null;
            }), null);
        }

        //private void DifferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    switch (((ComboBox)sender).SelectedValue.ToString())
        //    {
        //        case "CIE1976":
        //            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE1976;
        //            break;
        //        case "CIE1994":
        //            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE1994;
        //            break;
        //        case "CIE2000":
        //            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE2000;
        //            break;
        //    }
        //}

        private void Select_CIE2000(object sender, RoutedEventArgs e)
        {
            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE2000;
        }

        private void Select_CIE1994(object sender, RoutedEventArgs e)
        {
            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE1994;
        }

        private void Select_CIE1976(object sender, RoutedEventArgs e)
        {
            DeltaE._COLORSPACE = DeltaE.Colorspace.CIE1976;
        }

        private void CanvasResult_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Result = null;
            CanvasResult.Children.Clear();
        }


        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {

            int w;
            int h;


            if (Source != null)
            {
                w = Source.Width;
                h = Source.Height;
            }
            else
            {
                w = 500;
                h = 500;
            }



            List<ColorPicker> colors = new List<ColorPicker>();

            if (cp1.SelectedColor != null) colors.Add(cp1);
            if (cp2.SelectedColor != null) colors.Add(cp2);
            if (cp3.SelectedColor != null) colors.Add(cp3);
            if (cp4.SelectedColor != null) colors.Add(cp4);
            if (cp5.SelectedColor != null) colors.Add(cp5);
            if (cp6.SelectedColor != null) colors.Add(cp6);


            int paletteSize = colors.Count;
            int currentIndex = 0;


            Bitmap flag = new Bitmap(w, h);
            Graphics flagGraphics = Graphics.FromImage(flag);

            foreach (ColorPicker p in colors)
            {

                Color c = System.Drawing.Color.FromArgb(p.SelectedColor.Value.A, p.SelectedColor.Value.R, p.SelectedColor.Value.G, p.SelectedColor.Value.B);

                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * w / paletteSize), 0, w / paletteSize, h);
                currentIndex++;
            }

            Palette = new Image(new Bitmap(flag));


            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0,
                    Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


        }

        private void btnGenerateGradient_Click(object sender, RoutedEventArgs e)
        {
            //int w = Source.Width;
            //int h = Source.Height;

            //Bitmap flag = new Bitmap(w, h);
            //Graphics flagGraphics = Graphics.FromImage(flag);

            //Color c1 = System.Drawing.Color.FromArgb(cp1.SelectedColor.Value.A, cp1.SelectedColor.Value.R, cp1.SelectedColor.Value.G, cp1.SelectedColor.Value.B);
            //Color c2 = System.Drawing.Color.FromArgb(cp2.SelectedColor.Value.A, cp2.SelectedColor.Value.R, cp2.SelectedColor.Value.G, cp2.SelectedColor.Value.B);


            //flagGraphics.FillRectangle(new LinearGradientBrush(new Rectangle(0, 0, w, h), c1, c2, 180f), 0,0,w,h  ) ; 

            //Palette = new Image(new Bitmap(flag));


            //var frame = new DispatcherFrame();
            //CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            //{
            //    CanvasPalette.Children.Clear();
            //    CanvasPalette.Children.Insert(0,
            //        Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

            //    frame.Continue = false;
            //    return null;
            //}), null);
            //Dispatcher.PushFrame(frame);

        }

        private void CanvasResult_MouseEnter(object sender, MouseEventArgs e)
        {
            CanvasResult.Opacity = 100;
        }

        private void CanvasResult_MouseLeave(object sender, MouseEventArgs e)
        {
            CanvasResult.Opacity = 0;
        }

        private void QuickSavePaletteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo dir = new DirectoryInfo(PaletteDirectory);
            FileInfo[] files = dir.GetFiles("*" + "_00-" + "*.*");
            var last = files.OrderBy(f => f.CreationTime)
                        .ToList().Last();
            var num = last.Name.Substring(4, 4);

            string newName = (Convert.ToInt32(num) + 1).ToString("D4");

            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = PaletteDirectory;
                dlg.FileName = "_00-" + newName; // Default file name
                dlg.DefaultExt = ".png"; // Default file extension
                dlg.Filter = "Image (.png)|*.png"; // Filter files by extension
                Palette.Working.Save(PaletteDirectory + "/" + dlg.FileName + ".png");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void btnGeneratePalette_Click(object sender, RoutedEventArgs e)
        {
            if (Source == null) return;
            int w;
            int h;


            if (Source != null)
            {
                w = Source.Width;
                h = Source.Height;
            }
            else
            {
                w = 500;
                h = 500;
            }

            //var colors = GenerateHistogram(Source,_paletteSize);
            //var colors = HistogramGenerator.GenerateMedianHistogram(Source, _paletteSize);
            var colors = HistogramGenerator.GenerateRandomSampleHistogram(Source, _paletteSize);
            int paletteSize = colors.Count;
            int currentIndex = 0;

            Bitmap flag = new Bitmap(w, h);
            Graphics flagGraphics = Graphics.FromImage(flag);

            foreach (Color c in colors)
            {
                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * w / paletteSize), 0, w / paletteSize, h);
                currentIndex++;
            }

            Palette = new Image(new Bitmap(flag));



            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0,
                    Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        public Image GeneratePalette()
        {
            //if (Source == null) return;
            int w;
            int h;


            if (Source != null)
            {
                w = Source.Width;
                h = Source.Height;
            }
            else
            {
                w = 500;
                h = 500;
            }

            //var colors = GenerateHistogram(Source,_paletteSize);
            //var colors = HistogramGenerator.GenerateMedianHistogram(Source, _paletteSize);
            var colors = HistogramGenerator.GenerateRandomSampleHistogram(Source, 256);
            int paletteSize = colors.Count;
            int currentIndex = 0;

            Bitmap flag = new Bitmap(w, h);
            Graphics flagGraphics = Graphics.FromImage(flag);

            foreach (Color c in colors)
            {
                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * w / paletteSize), 0, w / paletteSize, h);
                currentIndex++;
            }

            return new Image(new Bitmap(flag));



      
        }


        private void SaveGifMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Imaging.GifBitmapEncoder gEnc = new GifBitmapEncoder();

            ConsoleBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ConsoleBox.AppendText("===========================\r");
                ConsoleBox.AppendText("Frames: " + GifFrames.Count + "\r");
                ConsoleBox.AppendText("===========================\r");
                ConsoleBox.ScrollToEnd();
                return null;
            }), null);

            for (int i = 0; i < 5; i++)
            {
                GifFrames.Add(GifFrames.Last());
            }

            foreach (System.Drawing.Bitmap bmpImage in GifFrames)
            {

                var bmp = bmpImage.GetHbitmap();
                var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bmp,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                gEnc.Frames.Add(BitmapFrame.Create(src));
                NativeMethods.DeleteObject(bmp); // recommended, handle memory leak
            }
            using (FileStream fs = new FileStream(@"C:\Users\tsova\Documents\Projects\s\GIF\" + rnd.Next(0, 10000)+ ".gif", FileMode.Create))
            {
                gEnc.Save(fs);
            }

        }

        private void ShowPalette(object sender, MouseEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasPalette.Opacity = 100;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void HidePalette(object sender, MouseEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasPalette.Opacity = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }


        private void OpenPalette(object sender, MouseButtonEventArgs e)
        {
            OpenPaletteImage();
        }

        private void ShowSource(object sender, MouseEventArgs e)
        {

            var frame = new DispatcherFrame();
            CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasSource.Opacity = 100;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

        }

        private void HideSource(object sender, MouseEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasSource.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasSource.Opacity = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void OpenSource(object sender, MouseButtonEventArgs e)
        {
            OpenSourceImage();
        }


        private void ShowResult(object sender, MouseEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasResult.Opacity = 100;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void HideResult(object sender, MouseEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            {
                CanvasResult.Opacity = 0;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

        }

        private void btnUsePalette_Click(object sender, RoutedEventArgs e)
        {

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Result = new Image(new Bitmap(Source.Working));
            Result.PixelList = new List<CustomPixel>();

            for (int i = 0; i < 25; i++)
            {
                //GifBuffer.Add(Source.Working);
            }


            for (int i = 0; i < 25; i++)
            {
                //GifBuffer.Add(ResultImage);
            }
           

            var s = Stopwatch.StartNew();
            for (int i = 0; i < Source.PixelList.Count; i++)
            {
                Result.PixelList.Add(Source.PixelList[i]);
                Result.PixelList[i].Color = PaletteSorter.GetClosestColor(Result.PixelList[i].LAB);
                ResultImage.SetPixel(Result.PixelList[i].x,
                    Result.PixelList[i].y,
                    Result.PixelList[i].Color);
            }
            ProcessWindow.WriteLine("==========   " + s.ElapsedMilliseconds);

            Result = new Image(new Bitmap(ResultImage));
            var frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                //GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }

        private void btnDitherTest_Click(object sender, RoutedEventArgs e)
        {


            //PrepareImages();

            PrepareImages();



            double w8 = 8.0 / 42.0;
            double w7 = 7.0 / 42.0;
            double w5 = 5.0 / 42.0;
            double w4 = 4.0 / 42.0;
            double w2 = 2.0 / 42.0;
            double w1 = 1.0 / 42.0;

            //double w1 = 7.0 / 16.0;
            //double w2 = 3.0 / 16.0;
            //double w3 = 5.0 / 16.0;
            //double w4 = 1.0 / 16.0;

            var s = Stopwatch.StartNew();
            for (var y = 0; y < Palette.Height; y++)
            {
                for (var x = 0; x < Palette.Width; x++)
                {
                    int oldpixel = Palette.Pixel2DArray[x, y].D;

                    if (oldpixel < 128) Palette.Pixel2DArray[x, y].SetGray(0); else Palette.Pixel2DArray[x, y].SetGray(255);
                    //if (oldpixel.D < 128) newpixel.D = 0; else newpixel.D = 255;



                    int quant_error = (oldpixel - Palette.Pixel2DArray[x, y].D);




                    try
                    {

                        //Palette.Pixel2DArray[x + 1, y].SetGray(Palette.Pixel2DArray[x + 1, y].D + w1 * quant_error);
                        //Palette.Pixel2DArray[x, y + 1].SetGray(Palette.Pixel2DArray[x, y + 1].D + w3 * quant_error);
                        //Palette.Pixel2DArray[x - 1, y + 1].SetGray(Palette.Pixel2DArray[x - 1, y + 1].D + w2 * quant_error);
                        //Palette.Pixel2DArray[x + 1, y + 1].SetGray(Palette.Pixel2DArray[x + 1, y + 1].D + w4 * quant_error);



                        Palette.Pixel2DArray[x + 1, y].SetGray(Palette.Pixel2DArray[x + 1, y].D + (w7 * quant_error));
                        Palette.Pixel2DArray[x + 2, y].SetGray(Palette.Pixel2DArray[x + 2, y].D + w5 * quant_error);
                        Palette.Pixel2DArray[x - 2, y + 1].SetGray(Palette.Pixel2DArray[x - 2, y + 1].D + w2 * quant_error);
                        Palette.Pixel2DArray[x - 1, y + 1].SetGray(Palette.Pixel2DArray[x - 1, y + 1].D + w4 * quant_error);
                        Palette.Pixel2DArray[x, y + 1].SetGray(Palette.Pixel2DArray[x, y + 1].D + w8 * quant_error);
                        Palette.Pixel2DArray[x + 1, y + 1].SetGray(Palette.Pixel2DArray[x + 1, y + 1].D + w4 * quant_error);
                        Palette.Pixel2DArray[x + 2, y + 1].SetGray(Palette.Pixel2DArray[x + 2, y + 1].D + w2 * quant_error);
                        Palette.Pixel2DArray[x - 2, y + 2].SetGray(Palette.Pixel2DArray[x - 2, y + 2].D + w1 * quant_error);
                        Palette.Pixel2DArray[x - 1, y + 2].SetGray(Palette.Pixel2DArray[x - 1, y + 2].D + w2 * quant_error);
                        Palette.Pixel2DArray[x, y + 2].SetGray(Palette.Pixel2DArray[x, y + 2].D + w4 * quant_error);
                        Palette.Pixel2DArray[x + 1, y + 2].SetGray(Palette.Pixel2DArray[x + 1, y + 2].D + w2 * quant_error);
                        Palette.Pixel2DArray[x + 2, y + 2].SetGray(Palette.Pixel2DArray[x + 2, y + 2].D + w1 * quant_error);
                    }
                    catch
                    {
                    }

                }
                //var frame = new DispatcherFrame();
                //CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                //    new DispatcherOperationCallback(delegate
                //    {
                //        //GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
                //        CanvasResult.Children.Clear();
                //        CanvasResult.Children.Insert(0,
                //            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                //        frame.Continue = false;
                //        return null;
                //    }), null);
                //Dispatcher.PushFrame(frame);


                //Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));
            }
            WriteLine("============= " + s.ElapsedMilliseconds);


            foreach (var p in Palette.Pixel2DArray)
            {
                p.GrayGenerate();
            }

            var frame2 = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate
                {
                    //GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
                    CanvasResult.Children.Clear();
                    CanvasResult.Children.Insert(0,
                        ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                    frame2.Continue = false;
                    return null;
                }), null);
            Dispatcher.PushFrame(frame2);
            Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

        }

        private void btnDitherTest2_Click(object sender, RoutedEventArgs e)
        {

            List<Color> colors = new List<Color>();
            Palette.Resize(_rm);
            ProcessWindow.WriteLine("Getting Colors");
            //colors = HistogramGenerator.GenerateHistogram(Palette, _ditherPaletteSize);
            colors = PaletteSorter.GetWebSafe(32);
            //colors = PaletteSorter.GetAllWebSafe();
            //colors = new List<Color>();
            // colors.Add(Color.Black);
            //colors.Add(Color.White);
            //colors = HistogramGenerator.GenerateRandomSampleHistogram(Palette, 5);
            ProcessWindow.WriteLine("Colors Aquired: " + colors.Count);
            //colors = new List<Color>();
            //colors.Add(Color.Black);
            //colors.Add(Color.White);

            //double w8 = 8.0 / 42.0;
            //double w7 = 7.0 / 42.0;
            //double w5 = 5.0 / 42.0;
            //double w4 = 4.0 / 42.0;
            //double w2 = 2.0 / 42.0;
            //double w1 = 1.0 / 42.0;

            double w1 = 7.0 / 16.0;
            double w2 = 3.0 / 16.0;
            double w3 = 5.0 / 16.0;
            double w4 = 1.0 / 16.0;
            ProcessWindow.WriteLine("Starting dither");
            var s = Stopwatch.StartNew();
            for (var y = 0; y < Palette.Height; y++)
            {
                for (var x = 0; x < Palette.Width; x++)
                {
                    int oldR = Palette.Pixel2DArray[x, y].R;
                    int oldG = Palette.Pixel2DArray[x, y].G;
                    int oldB = Palette.Pixel2DArray[x, y].B;




                    //if (oldpixel < 128) Palette.Pixel2DArray[x, y].SetGray(0); else Palette.Pixel2DArray[x, y].SetGray(255);
                    //if (oldpixel.D < 128) newpixel.D = 0; else newpixel.D = 255;

                    //Color select = colors.MinBy(h => DeltaE.DistanceCIE1976(converter.ToLab(new RGBColor(Palette.Pixel2DArray[x, y].R / 255.0, Palette.Pixel2DArray[x, y].G / 255.0, Palette.Pixel2DArray[x, y].B / 255.0)), converter.ToLab(new RGBColor(h.R/255.0, h.G/255.0, h.B/255.0))));

                    Color select = colors.MinBy(h => DeltaE.DistanceRGB(Palette.Pixel2DArray[x, y].R, h.R, Palette.Pixel2DArray[x, y].G, h.G, Palette.Pixel2DArray[x, y].B, h.B));

                    //Color select = Color.Red; 

                    Palette.Pixel2DArray[x, y].Color = select;
                    Palette.Pixel2DArray[x, y].R = select.R;
                    Palette.Pixel2DArray[x, y].G = select.G;
                    Palette.Pixel2DArray[x, y].B = select.B;



                    int red_error = (oldR - Palette.Pixel2DArray[x, y].R);
                    int green_error = (oldG - Palette.Pixel2DArray[x, y].G);
                    int blue_error = (oldB - Palette.Pixel2DArray[x, y].B);






                    try { Palette.Pixel2DArray[x + 1, y].SetRed(Palette.Pixel2DArray[x + 1, y].R + w1 * red_error); } catch { }
                    try { Palette.Pixel2DArray[x, y + 1].SetRed(Palette.Pixel2DArray[x, y + 1].R + w3 * red_error); } catch { }
                    try { Palette.Pixel2DArray[x - 1, y + 1].SetRed(Palette.Pixel2DArray[x - 1, y + 1].R + w2 * red_error); } catch { }
                    try { Palette.Pixel2DArray[x + 1, y + 1].SetRed(Palette.Pixel2DArray[x + 1, y + 1].R + w4 * red_error); } catch { }
                    try { Palette.Pixel2DArray[x + 1, y].SetGreen(Palette.Pixel2DArray[x + 1, y].G + w1 * green_error); } catch { }
                    try { Palette.Pixel2DArray[x, y + 1].SetGreen(Palette.Pixel2DArray[x, y + 1].G + w3 * green_error); } catch { }
                    try { Palette.Pixel2DArray[x - 1, y + 1].SetGreen(Palette.Pixel2DArray[x - 1, y + 1].G + w2 * green_error); } catch { }
                    try { Palette.Pixel2DArray[x + 1, y + 1].SetGreen(Palette.Pixel2DArray[x + 1, y + 1].G + w4 * green_error); } catch { }
                    try { Palette.Pixel2DArray[x + 1, y].SetBlue(Palette.Pixel2DArray[x + 1, y].B + w1 * blue_error); } catch { }
                    try { Palette.Pixel2DArray[x, y + 1].SetBlue(Palette.Pixel2DArray[x, y + 1].B + w3 * blue_error); } catch { }
                    try { Palette.Pixel2DArray[x - 1, y + 1].SetBlue(Palette.Pixel2DArray[x - 1, y + 1].B + w2 * blue_error); } catch { }
                    try { Palette.Pixel2DArray[x + 1, y + 1].SetBlue(Palette.Pixel2DArray[x + 1, y + 1].B + w4 * blue_error); } catch { }
                    //Palette.Pixel2DArray[x + 1, y].SetRed(Palette.Pixel2DArray[x + 1, y].R + (w7 * red_error));
                    //Palette.Pixel2DArray[x + 2, y].SetRed(Palette.Pixel2DArray[x + 2, y].R + w5 * red_error);
                    //Palette.Pixel2DArray[x - 2, y + 1].SetRed(Palette.Pixel2DArray[x - 2, y + 1].R + w2 * red_error);
                    //Palette.Pixel2DArray[x - 1, y + 1].SetRed(Palette.Pixel2DArray[x - 1, y + 1].R + w4 * red_error);
                    //Palette.Pixel2DArray[x, y + 1].SetRed(Palette.Pixel2DArray[x, y + 1].R + w8 * red_error);
                    //Palette.Pixel2DArray[x + 1, y + 1].SetRed(Palette.Pixel2DArray[x + 1, y + 1].R + w4 * red_error);
                    //Palette.Pixel2DArray[x + 2, y + 1].SetRed(Palette.Pixel2DArray[x + 2, y + 1].R + w2 * red_error);
                    //Palette.Pixel2DArray[x - 2, y + 2].SetRed(Palette.Pixel2DArray[x - 2, y + 2].R + w1 * red_error);
                    //Palette.Pixel2DArray[x - 1, y + 2].SetRed(Palette.Pixel2DArray[x - 1, y + 2].R + w2 * red_error);
                    //Palette.Pixel2DArray[x, y + 2].SetRed(Palette.Pixel2DArray[x, y + 2].R + w4 * red_error);
                    //Palette.Pixel2DArray[x + 1, y + 2].SetRed(Palette.Pixel2DArray[x + 1, y + 2].R + w2 * red_error);
                    //Palette.Pixel2DArray[x + 2, y + 2].SetRed(Palette.Pixel2DArray[x + 2, y + 2].R + w1 * red_error);

                    //Palette.Pixel2DArray[x + 1, y].SetGreen(Palette.Pixel2DArray[x + 1, y].G + (w7 * green_error));
                    //Palette.Pixel2DArray[x + 2, y].SetGreen(Palette.Pixel2DArray[x + 2, y].G + w5 * green_error);
                    //Palette.Pixel2DArray[x - 2, y + 1].SetGreen(Palette.Pixel2DArray[x - 2, y + 1].G + w2 * green_error);
                    //Palette.Pixel2DArray[x - 1, y + 1].SetGreen(Palette.Pixel2DArray[x - 1, y + 1].G + w4 * green_error);
                    //Palette.Pixel2DArray[x, y + 1].SetGreen(Palette.Pixel2DArray[x, y + 1].G + w8 * green_error);
                    //Palette.Pixel2DArray[x + 1, y + 1].SetGreen(Palette.Pixel2DArray[x + 1, y + 1].G + w4 * green_error);
                    //Palette.Pixel2DArray[x + 2, y + 1].SetGreen(Palette.Pixel2DArray[x + 2, y + 1].G + w2 * green_error);
                    //Palette.Pixel2DArray[x - 2, y + 2].SetGreen(Palette.Pixel2DArray[x - 2, y + 2].G + w1 * green_error);
                    //Palette.Pixel2DArray[x - 1, y + 2].SetGreen(Palette.Pixel2DArray[x - 1, y + 2].G + w2 * green_error);
                    //Palette.Pixel2DArray[x, y + 2].SetGreen(Palette.Pixel2DArray[x, y + 2].G + w4 * green_error);
                    //Palette.Pixel2DArray[x + 1, y + 2].SetGreen(Palette.Pixel2DArray[x + 1, y + 2].G + w2 * green_error);
                    //Palette.Pixel2DArray[x + 2, y + 2].SetGreen(Palette.Pixel2DArray[x + 2, y + 2].G + w1 * green_error);

                    //Palette.Pixel2DArray[x + 1, y].SetBlue(Palette.Pixel2DArray[x + 1, y].B + (w7 * blue_error));
                    //Palette.Pixel2DArray[x + 2, y].SetBlue(Palette.Pixel2DArray[x + 2, y].B + w5 * blue_error);
                    //Palette.Pixel2DArray[x - 2, y + 1].SetBlue(Palette.Pixel2DArray[x - 2, y + 1].B + w2 * blue_error);
                    //Palette.Pixel2DArray[x - 1, y + 1].SetBlue(Palette.Pixel2DArray[x - 1, y + 1].B + w4 * blue_error);
                    //Palette.Pixel2DArray[x, y + 1].SetBlue(Palette.Pixel2DArray[x, y + 1].B + w8 * blue_error);
                    //Palette.Pixel2DArray[x + 1, y + 1].SetBlue(Palette.Pixel2DArray[x + 1, y + 1].B + w4 * blue_error);
                    //Palette.Pixel2DArray[x + 2, y + 1].SetBlue(Palette.Pixel2DArray[x + 2, y + 1].B + w2 * blue_error);
                    //Palette.Pixel2DArray[x - 2, y + 2].SetBlue(Palette.Pixel2DArray[x - 2, y + 2].B + w1 * blue_error);
                    //Palette.Pixel2DArray[x - 1, y + 2].SetBlue(Palette.Pixel2DArray[x - 1, y + 2].B + w2 * blue_error);
                    //Palette.Pixel2DArray[x, y + 2].SetBlue(Palette.Pixel2DArray[x, y + 2].B + w4 * blue_error);
                    //Palette.Pixel2DArray[x + 1, y + 2].SetBlue(Palette.Pixel2DArray[x + 1, y + 2].B + w2 * blue_error);
                    //Palette.Pixel2DArray[x + 2, y + 2].SetBlue(Palette.Pixel2DArray[x + 2, y + 2].B + w1 * blue_error);
                    //}

                    //Palette.Pixel2DArray[x, y].ColorGenerate();
                }
                if (y % 10 == 0)
                {
                    var frame = new DispatcherFrame();
                    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            //GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
                            CanvasResult.Children.Clear();
                            CanvasResult.Children.Insert(0,
                                ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                            frame.Continue = false;
                            return null;
                        }), null);
                    Dispatcher.PushFrame(frame);
                }

                //Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));
            }
            WriteLine("============= " + s.ElapsedMilliseconds);


            //foreach (var p in Palette.Pixel2DArray)
            //{
            //    p.ColorGenerate();
            //}

            var frame2 = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate
                {
                    //GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
                    CanvasResult.Children.Clear();
                    CanvasResult.Children.Insert(0,
                        ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                    frame2.Continue = false;
                    return null;
                }), null);
            Dispatcher.PushFrame(frame2);
            Result = new Image(ConvertToBitmap(Palette.Pixel2DArray));

        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            if (!ImagesPresent()) return;
            _break = false;


            var frame = new DispatcherFrame();
            btnStop.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                btnStop.Visibility = Visibility.Visible;
                frame.Continue = false;
                return null;
            }), null);
            btnStart.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                btnStart.Visibility = Visibility.Collapsed;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            WriteLine("START");


            Thread newThread;

            switch (tabProcess.SelectedIndex)
            {
                case 0:

                    ProgressBar1.Maximum = Convert.ToInt32(IterationsTextBox.Text);
                    ProgressBar1.Value = 0;

                    if (_continuous)
                    {

                            newThread = new Thread(Process_RandomSortContinuous);

                    }
                    else
                    {
                        newThread = new Thread(Process_RandomSort);
                    }

                    break;

                case 1:


                    switch (_patternMode)
                    {
                        case Pattern.Fan:
                            newThread = new Thread(Process_BestFit);
                            break;
                        case Pattern.Circular:
                            newThread = new Thread(Process_BestFitCircular);
                            break;
                        default:
                            newThread = new Thread(Process_BestFit);
                            break;
                    }


                    break;
                case 2:

                    newThread = new Thread(Process_Sort);
                    ProgressBar1.Maximum = Convert.ToInt32(IterationsTextBox.Text);
                    ProgressBar1.Value = 0;

                    break;
                case 3:
                    if (Result != null) Palette = new Image(new Bitmap(Result._Original));
                    newThread = new Thread(Process_Dither_Advanced);
                    break;
                default:
                    newThread = new Thread(Process_Sort);
                    break;
            }

            LockUI();

            newThread.Start();

            
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            _break = true;




            var frame = new DispatcherFrame();
            btnStop.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                btnStop.Visibility = Visibility.Collapsed;
                frame.Continue = false;
                return null;
            }), null);
            btnStart.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                btnStart.Visibility = Visibility.Visible;
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);


            UnlockUI();
        }

        private void LockUI()
        {

            UIEnabled = false;


        }

        private void UnlockUI()
        {

            UIEnabled = true;

        }

        private void sldResolution_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateResolution();
        }

        private void sldDitherWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //UpdateResolution();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }



        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            
            //if (Result != null)
            //{
            //    var frame = new DispatcherFrame();
            //    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(delegate
            //    {

            //        CanvasResult.Children.Clear();
            //        CanvasResult.Children.Insert(0,
            //            ConvertToBitmap(Palette.Pixel2DArray).ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

            //        frame.Continue = false;
            //        return null;
            //    }), null);
            //    Dispatcher.PushFrame(frame);
            //}
            
        }

        private void OpenPaletteSelector(object sender, RoutedEventArgs e)
        {

            PaletteSelectorWindow PSW = new PaletteSelectorWindow();
            PSW.Show();
        }

        private void Batch_Click(object sender, RoutedEventArgs e)
        {
            PrepareImages();

            //Thread newThread;
            //newThread = new Thread(Process_RandomSort_Batch);


            Thread t1 = new Thread(Process_RandomSort_Batch);
            Thread t2 = new Thread(Process_RandomSort_Batch);
            Thread t3 = new Thread(Process_RandomSort_Batch);
            Thread t4 = new Thread(Process_RandomSort_Batch);
            Thread t5 = new Thread(Process_RandomSort_Batch);
            Thread t6 = new Thread(Process_RandomSort_Batch);
            Thread t7 = new Thread(Process_RandomSort_Batch);
            Thread t8 = new Thread(Process_RandomSort_Batch);

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t5.Start();
            t6.Start();
            t7.Start();
            t8.Start();

            //SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            //    for (int i = 0; i < 7; i++)
            //    {


            //        Task.Factory.StartNew(() =>
            //        {
            //            Process_RandomSort_Batch();


            //        }, _tokenSource.Token,
            //   TaskCreationOptions.None,
            //   TaskScheduler.Default)//Note TaskScheduler.Default here
            //.ContinueWith(
            //        t =>
            //        {
            //        //finish...
            //        //if (OnFinishWorkEventHandler != null)
            //        //    OnFinishWorkEventHandler(this, EventArgs.Empty);
            //    }
            //    , TaskScheduler.FromCurrentSynchronizationContext());




            //    }
        }

        private void Batch_Click_Palette(object sender, RoutedEventArgs e)
        {
            PrepareImages();
            Random rnd = new Random();

            //Thread newThread;
            //newThread = new Thread(Process_RandomSort_Batch);


            for (int i = 0; i < 8; i++)
            {
                
                Thread t1 = new Thread(() => Process_RandomSort_Batch_MutatePalette(rnd.Next(-15,15), true));
                t1.Start();
            }

           
        }

        private void Sequence_Click(object sender, RoutedEventArgs e)
        {

            //var colors = HistogramGenerator.GenerateMedianHistogram(Source, 32);
            var colors = PaletteSorter.GetAllWebSafe();
            //var colors = PaletteSorter.GetRGBPalette();
            //var colors = HistogramGenerator.GenerateRandomSampleHistogram(Source, 128);

            //colors.AddRange(colors1);
            //colors.AddRange(colors2);

            WriteLine($"Colors : {colors.Count}");

            for (int i = 0; i < 25; i++)
            {
                GifBuffer.Add(new KeyValuePair<Bitmap, int>(Source.Original, 0));
            }

            //colors = colors.RandomSubset(colors.Count).ToList();

            //colors = colors.RandomSubset(colors.Count).ToList(); 

            int counter = 1;

            for (int i = 0; i < colors.Count; i++)
            {
                int scopey = i + 1;
                int l = i;
                Thread t1 = new Thread(() => GenerateByPaletteOrder(colors.Take(scopey).ToList(), l));
                t1.Start();

            }
            for (int i = 216; i < 250; i++)
            {
                Source._Original.Save(FramesDir + i + ".png");
            }
            


        }

        private void Save_Buffer(object sender, RoutedEventArgs e)
        {
            Save_Gif();
        }


        public void Save_Gif()
        {
            //GifBuffer = GifBuffer.TakeEvery().ToList();

            //for (int i = 0; i < 100; i++)
            //{
            //    GifBuffer.Prepend(GifBuffer.First());
            //}

            //for (int i = 0; i < 50; i++)
            //{
            //    GifBuffer.Add(GifBuffer.Last());
            //}
            GifBuffer = GifBuffer.OrderBy(x => x.Value).ToList();
            

            for (int i = 0; i < 12; i++)
            {
                GifBuffer.Prepend(GifBuffer.First());
            }

            for (int i = 0; i < 12; i++)
            {
                GifBuffer.Add(GifBuffer.Last());
            }

            GifBuffer.Add(GifBuffer.Last());

            using (var gif = AnimatedGif.AnimatedGif.Create(@"C:\Users\tsova\Documents\Projects\s\GIF\" + rnd.Next(0, 10000) + @".gif", 50, 0))
            {
                foreach (var bmpImage in GifBuffer)
                {
                    gif.AddFrame(bmpImage.Key, 50, GifQuality.Bit8);

                }


          
                //for (int i = 0; i < 15; i++)
                //{
                //    gif.AddFrame(GifBuffer.Last());
                //}


                //GifBuffer.Reverse();
                //foreach (System.Drawing.Bitmap bmpImage in GifBuffer)
                //{
                //    gif.AddFrame(bmpImage, -1, GifQuality.Bit8);

                //}
            }


            //System.Windows.Media.Imaging.GifBitmapEncoder gEnc = new GifBitmapEncoder();

            //foreach (System.Drawing.Bitmap bmpImage in GifBuffer)
            //{

            //    var bmp = bmpImage.GetHbitmap();
            //    var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            //        bmp,
            //        IntPtr.Zero,
            //        Int32Rect.Empty,
            //        BitmapSizeOptions.FromEmptyOptions());
            //    gEnc.Frames.Add(BitmapFrame.Create(src));
            //    NativeMethods.DeleteObject(bmp); // recommended, handle memory leak
            //}
            //using (FileStream fs = new FileStream(@"C:\Users\tsova\Documents\Projects\Batch\A.gif", FileMode.Create))
            //{
            //    gEnc.Save(fs);
            //}

            GifBuffer.Clear();
        }

        private void btnUsePaletteGen_Click(object sender, RoutedEventArgs e)
        {
            var colors = HistogramGenerator.GenerateMedianHistogramPair(Source, _paletteSize);
            ProcessWindow.WriteLine("C: " + colors.Count);
            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Result = new Image(new Bitmap(Source.Working));
            Result.PixelList = new List<CustomPixel>();

            Bitmap flag = new Bitmap(Source.Width, Source.Height);
            Graphics flagGraphics = Graphics.FromImage(flag);
            int paletteSize = colors.Count;
            int currentIndex = 0;

            foreach (ColorPair p in colors)
            {

                Color c = System.Drawing.Color.FromArgb(p.Color.A, p.Color.R, p.Color.G, p.Color.B);

                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * Source.Width / paletteSize), 0, Source.Width / paletteSize, Source.Height);
                currentIndex++;
            }

            Palette = new Image(new Bitmap(flag));
            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0,
                    Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);



            var s = Stopwatch.StartNew();
            for (int i = 0; i < Source.PixelList.Count; i++)
            {


                Result.PixelList.Add(Source.PixelList[i]);
                Result.PixelList[i].Color = colors.MinBy(x => DeltaE.Distance(x.LAB, Result.PixelList[i].LAB)).Color;
                ResultImage.SetPixel(Result.PixelList[i].x,
                    Result.PixelList[i].y,
                    Result.PixelList[i].Color);
            }
            ProcessWindow.WriteLine("==========   " + s.ElapsedMilliseconds);

            Result = new Image(new Bitmap(ResultImage));

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                //GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }

        private void Mutate_Click(object sender, RoutedEventArgs e)
        {
            string[] targets = Directory.GetFiles(TargetDir);

            foreach (string s in targets)
            {



                Image MutateSource = new Image(new Bitmap(s));

                //Mutate 1 thread per image, 

                //BASE IMAGE TO MUTATE AGAINST
                //Source.Resize(_rm);
                int w = MutateSource.Working.Width;
                int h = MutateSource.Working.Height;


                string[] files = Directory.GetFiles(MutateDir);

                foreach (string f in files)
                {
                    string path = f.ToString();
                    Image MutatePalette = new Image(new Bitmap(path));
                    //Image MutateSource = new Image(Source.Original);
                    int w1 = w;
                    int h1 = h;


                    //Process_Mutate(path, MutatePalette, MutateSource, w1, h1);
                    Thread thread = new Thread(() => Process_Mutate(path, MutatePalette, MutateSource,  w1, h1));
                    thread.Start();



                }

            }
        }

        private void btnUseRGBPalette_Click(object sender, RoutedEventArgs e)
        {
            List<ColorPair> colors = HistogramGenerator.GenerateLabValues(PaletteSorter.GetRandomPalette(64));
            ProcessWindow.WriteLine("C: " + colors.Count);
            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Result = new Image(new Bitmap(Source.Working));
            Result.PixelList = new List<CustomPixel>();

            Bitmap flag = new Bitmap(Source.Width, Source.Height);
            Graphics flagGraphics = Graphics.FromImage(flag);
            int paletteSize = colors.Count;
            int currentIndex = 0;

            foreach (ColorPair p in colors)
            {

                Color c = System.Drawing.Color.FromArgb(p.Color.A, p.Color.R, p.Color.G, p.Color.B);

                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * Source.Width / paletteSize), 0, Source.Width / paletteSize, Source.Height);
                currentIndex++;
            }

            Palette = new Image(new Bitmap(flag));
            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0,
                    Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);



            var s = Stopwatch.StartNew();
            for (int i = 0; i < Source.PixelList.Count; i++)
            {


                Result.PixelList.Add(Source.PixelList[i]);
                Result.PixelList[i].Color = colors.MinBy(x => DeltaE.Distance(x.LAB, Result.PixelList[i].LAB)).Color;
                ResultImage.SetPixel(Result.PixelList[i].x,
                    Result.PixelList[i].y,
                    Result.PixelList[i].Color);
            }
            ProcessWindow.WriteLine("==========   " + s.ElapsedMilliseconds);

            Result = new Image(new Bitmap(ResultImage));

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                //GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);

        }

        private void btnUseBWPalette_Click(object sender, RoutedEventArgs e)
        {


            List<Color> bw = new List<Color>()
            {
                Color.White,
                Color.Black
            };

            List<ColorPair> colors = HistogramGenerator.GenerateLabValues(bw);
            ProcessWindow.WriteLine("C: " + colors.Count);
            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Result = new Image(new Bitmap(Source.Working));
            Result.PixelList = new List<CustomPixel>();

            Bitmap flag = new Bitmap(Source.Width, Source.Height);
            Graphics flagGraphics = Graphics.FromImage(flag);
            int paletteSize = colors.Count;
            int currentIndex = 0;

            foreach (ColorPair p in colors)
            {

                Color c = System.Drawing.Color.FromArgb(p.Color.A, p.Color.R, p.Color.G, p.Color.B);

                flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * Source.Width / paletteSize), 0, Source.Width / paletteSize, Source.Height);
                currentIndex++;
            }

            Palette = new Image(new Bitmap(flag));
            var frame = new DispatcherFrame();
            CanvasPalette.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {

                CanvasPalette.Children.Clear();
                CanvasPalette.Children.Insert(0,
                    Palette.Working.ToBitmapSource(CanvasPalette.ActualHeight, CanvasPalette.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);



            var s = Stopwatch.StartNew();
            for (int i = 0; i < Source.PixelList.Count; i++)
            {


                Result.PixelList.Add(Source.PixelList[i]);
                Result.PixelList[i].Color = colors.MinBy(x => DeltaE.Distance(x.LAB, Result.PixelList[i].LAB)).Color;
                ResultImage.SetPixel(Result.PixelList[i].x,
                    Result.PixelList[i].y,
                    Result.PixelList[i].Color);
            }
            ProcessWindow.WriteLine("==========   " + s.ElapsedMilliseconds);

            Result = new Image(new Bitmap(ResultImage));

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                //GifFrames.Add(new Bitmap(ResultImage));
                CanvasResult.Children.Clear();
                CanvasResult.Children.Insert(0,
                    ResultImage.ToBitmapSource(CanvasResult.ActualHeight, CanvasResult.ActualWidth));

                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
            ProcessWindow.WriteLine("Finished!");
            Stop(null, null);
        }
    }
   

}


public static class NativeMethods
{
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
}


    


