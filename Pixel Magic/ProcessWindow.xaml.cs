using System;
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
using Xceed.Wpf.Toolkit;
using Timer = System.Timers.Timer;
using Image = Pixel_Magic.Classes.Image;
using System.Threading.Tasks;
using MoreLinq;
using Colourful.Conversion;
using System.Drawing.Imaging;

namespace Pixel_Magic
{
    public partial class ProcessWindow : Window, INotifyPropertyChanged
    {
        
        private Image Palette;
        private Image Source;
        private Image Result;


        private bool Enabled = false;
        
        public bool UIEnabled { get { return Enabled; }
            set {


                Enabled = value;
                OnPropertyChanged("UIEnabled");
            } 
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
        public static ColourfulConverter converter = new ColourfulConverter { WhitePoint = Illuminants.D65 };

        private static Random rnd = new Random(666);
        private static Pattern _patternMode = Pattern.Circular;
        private static List<Bitmap> GifFrames = new List<Bitmap>();
        private static readonly Timer resizeTimer = new Timer(100) { Enabled = false };
        private static readonly Object _locker = new Object();
        private static string ImageDirectory = @"C:\Users\tsova\Documents\Projects\p";
        private static string SaveDirectory = @"C:\Users\tsova\Documents\Projects\s\";
        private static string PaletteDirectory = @"C:\Users\tsova\Documents\Projects\WindowsFormsApp2\WindowsFormsApp2";

        public static bool _break = false;
        public static double _ditherCenterWeight = 5; //5 for CIE1976
        public static double _ditherWeight = 1;
        public static int _ditherLimit = 1000;
        public static double _rm { get; set; } = 0.5;
        public static int _iterations { get; set; } = 100;
        public static int _refreshRate { get; set; } = 10;
        public static int _sampleSize { get; set; } = 100;
        public static int _paletteSize = 4;
        private const int _sort_RefreshRate = 5000;
        private static int _ditherPaletteSize = 8;
        private static bool _continuous = true;
        private static int _continuousRefreshRate = 10000;
        private static int _continuousRatio = 5;
        

        public static System.Windows.Controls.RichTextBox Console;
        public static System.Windows.Controls.ProgressBar Progress;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ProcessWindow()
        {
            
            InitializeComponent();
            PaletteSorter.GenerateWebColors();
            Console = ConsoleBox;
            Progress = ProgressBar1;
            ProcessWindow.WriteLine(" ");
            ProcessWindow.WriteLine("Initializing...");
            DataContext = this;
            resizeTimer.Elapsed += ResizingDone;
            lblResolution.Text = "[0, 0]";
            UIEnabled = true;
            ProcessWindow.WriteLine("Ready!");
            ProcessWindow.WriteLine("------");
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

            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
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

        private bool ImagesPresent()
        {
            if (Palette == null || Source == null) return false;
            return Palette.Working != null && Source.Working != null;
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

        public void Process_Sort()
        {
            ProcessWindow.WriteLine("Sort:");
            WriteLine("=====");


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
            Palette.PixelList.Sort();
            

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = (ProgressBar1.Maximum/6*3);
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);

            ProcessWindow.WriteLine("Sorting Source");
            Source.PixelList.Sort();

            ProgressBar1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                ProgressBar1.Value = (ProgressBar1.Maximum/6*4);
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

                if ((i% _sort_RefreshRate) == 0)
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
                    GifFrames.Add(new Bitmap(ResultImage));
                    frame = new DispatcherFrame();
                    CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new DispatcherOperationCallback(delegate
                        {
                            
                            CanvasResult.Children.Clear();
                            lock (_locker)
                            {
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
                            ProgressBar1.Value = i*2 + ProgressBar1.Maximum/6*4;
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

            frame = new DispatcherFrame();
            CanvasResult.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                //GifFrames.Add(new Bitmap(ResultImage));;
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

            ProcessWindow.WriteLine("Finished!");
            Stop(null,null);
        }

        public void Process_RandomSort()
        {
            ProcessWindow.WriteLine("Random Sample:");
            ProcessWindow.WriteLine("==============");
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);

        
            int numberSwapped = 0;
            int randomselection;
           
            //int size = OriginalFirst.Width*OriginalFirst.Height;
            CustomPixel save;

            ProcessWindow.WriteLine("Starting Sampling");
            for (var j = 1; j <= _iterations; j++)
            {
                for (int i = 0; i < Source.PixelList.Count; i++)
                {
                    randomselection = rnd.Next(1, (Source.PixelList.Count));

                    if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection].LAB,Source.PixelList[i].LAB)) <
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
                        GifFrames.Add(new Bitmap(ResultImage));;
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
                GifFrames.Add(new Bitmap(ResultImage));;
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

        }

        public void Process_RandomSortContinuous()
        {
            ProcessWindow.WriteLine("Random Sample Continuous:");
            ProcessWindow.WriteLine("==============");
            DispatcherFrame frame = new DispatcherFrame();

            Bitmap ResultImage = new Bitmap(Source.Width, Source.Height);
            Bitmap SubtractFrom1 = new Bitmap(Source.Width, Source.Height);


            int refreshCounter = 0;
            int randomselection1;
            int randomselection2;

            //int size = OriginalFirst.Width*OriginalFirst.Height;
            CustomPixel save;

            ProcessWindow.WriteLine("Starting Sampling");

            var s = Stopwatch.StartNew();
            while (!_break) // && !(refreshCounter > Source.PixelList.Count/_iterations)
            {

                randomselection1 = rnd.Next(1, (Source.PixelList.Count));
                randomselection2 = rnd.Next(1, (Source.PixelList.Count));

                if ((Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection1].LAB, Source.PixelList[randomselection2].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB)))
                        &&
                        (Math.Abs(DeltaE.Distance(Palette.PixelList[randomselection2].LAB, Source.PixelList[randomselection1].LAB)) <
                         Math.Abs(DeltaE.Distance(Source.PixelList[randomselection2].LAB, Palette.PixelList[randomselection2].LAB))))
                {
                    save = Palette.PixelList[randomselection2];
                    Palette.PixelList[randomselection2] = Palette.PixelList[randomselection1];
                    Palette.PixelList[randomselection1] = save;
                }


                refreshCounter++;
                if(refreshCounter % _continuousRefreshRate == 0)
                {
                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                    Task.Factory.StartNew(() =>
                    {
                        Bitmap newResult = new Bitmap(Source.Width, Source.Height);

                        for (int p = 0; p < Source.PixelList.Count; p++)
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
                        Dispatcher.PushFrame(frame2);


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

            ProcessWindow.WriteLine("======= " + s.ElapsedMilliseconds);
            ProcessWindow.WriteLine("Finalizing...");


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
            ProcessWindow.WriteLine("Finished!");
            _break = false;
            Stop(null, null);

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
            var k = Palette.PixelList.Count/2;


            ProcessWindow.WriteLine("Starting Sampling");
            for (int j = Palette.PixelList.Count/2; j < Palette.PixelList.Count; j++)
            {
                for (int i = 0; i < _sampleSize; i++)
                {
                    randomSelection2 = rnd.Next(0, 2) == 1 ? rnd.Next(j, Palette.PixelList.Count) : rnd.Next(0, k);

                    currentValue2 = DeltaE.Distance(Palette.PixelList[randomSelection2].LAB,Source.PixelList[j].LAB);
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
                if (j%(ResultImage.Width*_refreshRate) == 0)
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
            ProcessWindow.WriteLine("Best Fit Circular");
            ProcessWindow.WriteLine("========");
            PrepareImages();
            DispatcherFrame frame = new DispatcherFrame();


            Bitmap ResultImage = new Bitmap(Palette.Width, Palette.Height);
            Bitmap SubtractFrom1 = new Bitmap(Palette.Width, Palette.Height);


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


            List<System.Drawing.Point> usedList = new List<System.Drawing.Point>();
            List<System.Drawing.Point> spiralList = new List<System.Drawing.Point>();

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

                    double error = DeltaE.Distance(Palette.Pixel2DArray[spiralList[n].X, spiralList[n].Y].LAB, Source.Pixel2DArray[spiralList[l].X, spiralList[l].Y].LAB);


                    if (error < bestError)
                    {
                        BEIx = spiralList[n].X;
                        BEIy = spiralList[n].Y;
                        bestError = error;

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
            Stop(null,null);
        }

        public void Process_RandomSort_WithPreSort()
        {
            ProcessWindow.WriteLine("Random Sample With Presort");
            ProcessWindow.WriteLine("==========================");

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


                        double singleError1 = DeltaE.Distance(Palette.Pixel2DArray[curX, curY].LAB,Source.Pixel2DArray[curX, curY].LAB);
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

            
            while (!_break)
            {
                if (ditherCount >= _ditherLimit)
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
                        avg1L = avg1L / 9 + (_ditherCenterWeight-1);
                        avg1A = avg1A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg1B = avg1B / 9 + (_ditherCenterWeight - 1) + 127;
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
                        avg2L = avg2L / 9 + (_ditherCenterWeight - 1);
                        avg2A = avg2A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg2B = avg2B / 9 + (_ditherCenterWeight - 1) + 127;
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
                        avg3L = avg3L / 9 + (_ditherCenterWeight - 1);
                        avg3A = avg3A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg3B = avg3B / 9 + (_ditherCenterWeight - 1) + 127;
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
                        avg4L = avg4L / 9 + (_ditherCenterWeight - 1);
                        avg4A = avg4A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg4B = avg4B / 9 + (_ditherCenterWeight - 1) + 127;
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
                        avg5L = avg5L / 9 + (_ditherCenterWeight - 1);
                        avg5A = avg5A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg5B = avg5B / 9 + (_ditherCenterWeight - 1) + 127;
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
                        avg6L = avg6L / 9 + (_ditherCenterWeight - 1);
                        avg6A = avg6A / 9 + (_ditherCenterWeight - 1) + 127;
                        avg6B = avg6B / 9 + (_ditherCenterWeight - 1) + 127;
                        LabColor lab6 = new LabColor(avg6L, avg6A, avg6B);






                        var distance1 = DeltaE.DistanceCIE1976(lab1, lab5);
                        var distance2 = DeltaE.DistanceCIE1976(lab2, lab6);
                        var currentError = distance1 + distance2;

                        var distance3 = DeltaE.DistanceCIE1976(lab3, lab5);
                        var distance4 = DeltaE.DistanceCIE1976(lab4, lab6);
                        var newError = distance3 + distance4;


                        double singleError1 = DeltaE.DistanceCIE1976(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleError2 = DeltaE.DistanceCIE1976(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalCurrentError = singleError1 + singleError2;


                        double singleProposedError1 = DeltaE.DistanceCIE1976(Palette.Pixel2DArray[randomPixelX, randomPixelY].LAB, Source.Pixel2DArray[curX, curY].LAB);
                        double singleProposedError2 = DeltaE.DistanceCIE1976(Palette.Pixel2DArray[curX, curY].LAB, Source.Pixel2DArray[randomPixelX, randomPixelY].LAB);
                        double totalProposedError = singleProposedError1 + singleProposedError2;

                        double finalSingleError = Math.Abs(totalProposedError - totalCurrentError);
                        //add weight for single pixel, if distance is too much dont swap
                        //if (Math.Abs(finalError) < 5) singleGate = true;
                        double finalNeighborError = Math.Abs(newError - currentError);

                        //if (newError < currentError)
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
                        //GifFrames.Add(ConvertToBitmap(Palette.Pixel2DArray));
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
            Palette.Resize(Source.Width,Source.Height);

            _continuousRefreshRate = (Source.Width * Source.Height) / _continuousRatio;

            ProcessWindow.WriteLine($"Scaled Resolution: ({Source.Width},{Source.Height})");

            var frame = new DispatcherFrame();
            lblResolution.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate
                {
                    lblResolution.Text = $"[{Source.Width}, {Source.Height}]";

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

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (((ComboBoxItem) sender).Content.Equals("Colorspace"))
                CustomPixel.CurrentMode = CustomPixel.ComparisonMode.Colorspace;
            else if (((ComboBoxItem) sender).Content.Equals("Luminosity"))
                CustomPixel.CurrentMode = CustomPixel.ComparisonMode.Luminosity;
            else if (((ComboBoxItem) sender).Content.Equals("ColorMine"))
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
                dlg.DefaultExt = ".jpg"; // Default file extension
                dlg.Filter = "Image (.jpg)|*.jpg"; // Filter files by extension
                Result._Original.Save(SaveDirectory + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
                Palette._Original.Save(SaveDirectory + "Y" + dlg.FileName + ".jpg", jpgEncoder, myEncoderParameters);
            }
            catch (Exception ex)
            {
                WriteLine(ex.StackTrace);
            }
        }

        private Boolean TextBoxTextAllowed(String text)
        {
            return Array.TrueForAll(text.ToCharArray(),
                c => Char.IsDigit(c) || Char.IsControl(c));
        }

        private void IterationsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _iterations = Convert.ToInt32(((TextBox) sender).Text);
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
            _sampleSize = Convert.ToInt32(((TextBox) sender).Text);
        }

        private void RefreshRateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _refreshRate = Convert.ToInt32(((TextBox) sender).Text);
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
            OpenSourceImage();
        }

        private void OpenPalette(object sender, RoutedEventArgs e)
        {
            OpenPaletteImage();
        }

        private void ContinuousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var continuous  = ((CheckBox)sender).IsChecked;

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
            
            //int w;
            //int h;


            //if (Source != null)
            //{
            //    w = Source.Width;
            //    h = Source.Height; 
            //}
            //else
            //{
            //    w = 500;
            //    h = 500;
            //}


      
            //List<ColorPicker> colors = new List<ColorPicker>();

            //if (cp1.SelectedColor != null) colors.Add(cp1);
            //if (cp2.SelectedColor != null) colors.Add(cp2);
            //if (cp3.SelectedColor != null) colors.Add(cp3);
            //if (cp4.SelectedColor != null) colors.Add(cp4);
            //if (cp5.SelectedColor != null) colors.Add(cp5);
            //if (cp6.SelectedColor != null) colors.Add(cp6);


            //int paletteSize = colors.Count;
            //int currentIndex = 0;
            

            //Bitmap flag = new Bitmap(w,h);
            //Graphics flagGraphics = Graphics.FromImage(flag);

            //foreach (ColorPicker p in colors)
            //{

            //    Color c = System.Drawing.Color.FromArgb(p.SelectedColor.Value.A, p.SelectedColor.Value.R, p.SelectedColor.Value.G, p.SelectedColor.Value.B);

            //    flagGraphics.FillRectangle(new SolidBrush(c), (currentIndex * w / paletteSize), 0, w / paletteSize, h);
            //    currentIndex++;
            //}

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
                Palette.Working.Save(PaletteDirectory +"/" + dlg.FileName + ".png");
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
            var colors = HistogramGenerator.GenerateMedianHistogram(Source,_paletteSize);
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
            using (FileStream fs = new FileStream(@"C:\Users\tsova\Documents\Projects\s\Z.gif", FileMode.Create))
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
            
            Bitmap ResultImage = new Bitmap(Source.Width,Source.Height);
            Result = new Image(new Bitmap(Source.Working));
            Result.PixelList = new List<CustomPixel>();

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



                    int quant_error =  (oldpixel - Palette.Pixel2DArray[x, y].D);



                   
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
            //colors = PaletteSorter.GetWebSafe(32);
            //colors = PaletteSorter.GetAllWebSafe();
            //colors = new List<Color>();
           // colors.Add(Color.Black);
            //colors.Add(Color.White);
            colors = HistogramGenerator.GenerateRandomSampleHistogram(Palette, 16);
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

                    Color select = colors.MinBy(h => DeltaE.DistanceRGB(Palette.Pixel2DArray[x, y].R, h.R , Palette.Pixel2DArray[x, y].G, h.G , Palette.Pixel2DArray[x, y].B, h.B ));

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
                        try { Palette.Pixel2DArray[x + 1, y + 1].SetBlue(Palette.Pixel2DArray[x + 1, y + 1].B + w4 * blue_error);} catch { }
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

            PrepareImages();
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
                    if(Result != null) Palette = new Image(new Bitmap(Result._Original));
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

    }   


}


public static class NativeMethods
    {
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
    }


    


