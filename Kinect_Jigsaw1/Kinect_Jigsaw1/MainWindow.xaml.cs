using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Coding4Fun.Kinect.Wpf.Controls;
using System.Runtime.InteropServices;
using System.Threading;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Threading;
using System.Diagnostics;
using KinectMouseController;
using System.Drawing;
using Kinect.Toolbox;
using Kinect_Jigsaw1.Properties;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Win32;

[assembly: CLSCompliant(true)]
namespace Kinect_Jigsaw1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region timerAttributes
        DispatcherTimer dispatcherTimer;
        public  Stopwatch sw = new Stopwatch();
        #endregion timerAttributes

        #region attributes
        List<Piece> currentSelection = new List<Piece>();
        int selectionAngle = 0;
        List<Piece> pieces = new List<Piece>();
        List<Piece> shadowPieces = new List<Piece>();
        int columns = 5;
        int rows = 4;
        double scale = 1.0;
        BitmapImage imageSource;
        string srcFileName = "";
        string destFileName;
        DropShadowBitmapEffect shadowEffect;
        Point lastCell = new Point(-1, 0);
        ScaleTransform stZoomed = new ScaleTransform() { ScaleX = 1.1, ScaleY = 1.1 };
        ViewMode currentViewMode = ViewMode.Puzzle;
        PngBitmapEncoder png;
        double offsetX = -1;
        double offsetY = -1;
        double lastMouseDownX = -1;
        double lastMouseDownY = -1;
        bool moving = false;
        double initialRectangleX = 0;
        double initialRectangleY = 0;
        System.Windows.Shapes.Rectangle rectSelection = new System.Windows.Shapes.Rectangle();
        string imgSrc = "";
        string defLink = "";
        bool beg_bool = false, inter_bool = false, adv_bool = false;
        #endregion attributes

        # region  Kinect Variables

        private const double ScrollErrorMargin = 0.001;

        private const int PixelScrollByAmount = 20;

        private readonly KinectSensorChooser sensorChooser;

        public Joint hand = new Joint();
        HoverButton h;
        bool closing = false;
        const int skeletonCount = 6;
        public static Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        public const float SkeletonMaxX = 0.60f;
        public const float SkeletonMaxY = 0.40f;
        #endregion Kinect Variables

        #region MainWindows_Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class. 
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            win.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            win.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            // initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.kinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);
            BindingOperations.SetBinding(this.kinectRegion1, KinectRegion.KinectSensorProperty, regionSensorBinding);
            BindingOperations.SetBinding(this.kinectRegion2, KinectRegion.KinectSensorProperty, regionSensorBinding);
            BindingOperations.SetBinding(this.kinectRegion3, KinectRegion.KinectSensorProperty, regionSensorBinding);

            

            destFileName = Settings.Default.DestinationFile;


            cnvPuzzle.MouseLeftButtonUp += new MouseButtonEventHandler(cnvPuzzle_MouseLeftButtonUp);
            cnvPuzzle.MouseDown += new MouseButtonEventHandler(cnvPuzzle_MouseDown);
            cnvPuzzle.MouseMove += new MouseEventHandler(cnvPuzzle_MouseMove);
            cnvPuzzle.MouseWheel += new MouseWheelEventHandler(cnvPuzzle_MouseWheel);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);
            

            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,
                Direction = 320,
                ShadowDepth = 25,
                Softness = 1,
                Opacity = 0.5
            };

            
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }
        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
        }
        #endregion MainWindows_Methods

        #region Timer_Methods
        string currentTime = string.Empty;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            
            if (sw.IsRunning)
            {
                TimeSpan ts = sw.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                ClockTextBlock.Text = currentTime;
            }
        }

        public void start_Timer()
        {
            ClockTextBlock.Visibility = System.Windows.Visibility.Visible;
            sw.Start();
            dispatcherTimer.Start();
        }

        #endregion Timer_Methods

        #region Kinect_Skeleton&Gesture_Method
        public Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try
                    {
                        
                        if (args.NewSensor.IsRunning == true)
                        {
                            args.NewSensor.DepthStream.Range = DepthRange.Near;
                            args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                            args.NewSensor.AllFramesReady += NewSensor_AllFramesReady;
                        }

                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        private void NewSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            foreach (Skeleton sd in allSkeletons)
            {
                // the first found/tracked skeleton moves the mouse cursor
                if (sd.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // make sure both hands are tracked
                    if (sd.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked &&
                        sd.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                    {
                        int cursorX, cursorY;

                        // get the left and right hand Joints
                        Joint jointRight = sd.Joints[JointType.HandRight];
                        Joint jointLeft = sd.Joints[JointType.HandLeft];

                        // scale those Joints to the primary screen width and height
                        Joint scaledRight = jointRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
                        Joint scaledLeft = jointLeft.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);

                            cursorX = (int)scaledRight.Position.X;
                            cursorY = (int)scaledRight.Position.Y;
                            
                            NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, false, false);
                            runevent(first, e);
                            return;
                    }
                }
            }

            Thread.Sleep(1);
          //  throw new NotImplementedException();
        }

        // To detect the joints and their movements
        void runevent(Skeleton first, AllFramesReadyEventArgs e)
        {
            int cursorX, cursorY;
            Joint wristLeft = first.Joints[JointType.WristLeft];
            Joint handRight = first.Joints[JointType.HandRight];
            Joint handLeft = first.Joints[JointType.HandLeft];
            Joint shoulderRight = first.Joints[JointType.ShoulderRight];
            Joint shoulderLeft = first.Joints[JointType.ShoulderLeft];
            Joint hipLeft = first.Joints[JointType.HipLeft];
            Joint hipRight = first.Joints[JointType.HipRight];
            Joint kneeLeft = first.Joints[JointType.KneeLeft];
            // scale those Joints to the primary screen width and height
            Joint scaledRight = handRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
            Joint scaledLeft = handLeft.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);

            cursorX = (int)scaledRight.Position.X;
            cursorY = (int)scaledRight.Position.Y;


            float var = 0.2f;
            if (wristLeft.Position.Y - shoulderLeft.Position.Y > 0.2f)
            {
                Thread.Sleep(500);
                //MessageBox.Show("Left Clicked");
                NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, true, false);
            }
            if (jointDistance(wristLeft, shoulderLeft) <= var)
            {
                Thread.Sleep(500);
                //MessageBox.Show("Right Clicked");
                NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, false, true);
            }


        }

        // Gestures detection for controlling clicks
        private float jointDistance(Joint first, Joint second)
        {
            float dX = first.Position.X - second.Position.X;
            float dY = first.Position.Y - second.Position.Y;
            float dZ = first.Position.Z - second.Position.Z;

            return (float)Math.Sqrt((dX * dX) + (dY * dY) + (dZ * dZ));
        }

        #endregion Kinect_Skeleton&Gesture_Method

        #region HomePage_Methods
        private void beg_Click(object sender, RoutedEventArgs e)
        {
            beg_bool = true;
            
            kinectRegion1.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion1.IsEnabled = false;
            kinectRegion.Visibility = System.Windows.Visibility.Visible;
            kinectRegion.IsEnabled = true;

            Home.IsEnabled = true;
            Home.Visibility = System.Windows.Visibility.Visible;

            AppLogo.Visibility = System.Windows.Visibility.Hidden;

            im1.Source = new BitmapImage(new Uri("Images/beg1.jpg", UriKind.RelativeOrAbsolute));
            im2.Source = new BitmapImage(new Uri("Images/beg2.jpg", UriKind.RelativeOrAbsolute));
            im3.Source = new BitmapImage(new Uri("Images/beg3.jpg", UriKind.RelativeOrAbsolute));
            im4.Source = new BitmapImage(new Uri("Images/beg4.jpg", UriKind.RelativeOrAbsolute));
            im5.Source = new BitmapImage(new Uri("Images/beg5.jpg", UriKind.RelativeOrAbsolute));
            im6.Source = new BitmapImage(new Uri("Images/beg6.jpg", UriKind.RelativeOrAbsolute));
            im7.Source = new BitmapImage(new Uri("Images/beg7.jpg", UriKind.RelativeOrAbsolute));
            im8.Source = new BitmapImage(new Uri("Images/beg8.jpg", UriKind.RelativeOrAbsolute));
            im9.Source = new BitmapImage(new Uri("Images/beg9.jpg", UriKind.RelativeOrAbsolute));
            im10.Source = new BitmapImage(new Uri("Images/beg10.jpg", UriKind.RelativeOrAbsolute));
        }

        private void inter_Click(object sender, RoutedEventArgs e)
        {
            inter_bool = true;
            kinectRegion1.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion1.IsEnabled = false;
            kinectRegion.Visibility = System.Windows.Visibility.Visible;
            kinectRegion.IsEnabled = true;
            Home.IsEnabled = true;
            Home.Visibility = System.Windows.Visibility.Visible;
            AppLogo.Visibility = System.Windows.Visibility.Hidden;
            im1.Source = new BitmapImage(new Uri("Images/inter1.jpg", UriKind.RelativeOrAbsolute));
            im2.Source = new BitmapImage(new Uri("Images/inter2.jpg", UriKind.RelativeOrAbsolute));
            im3.Source = new BitmapImage(new Uri("Images/inter3.jpg", UriKind.RelativeOrAbsolute));
            im4.Source = new BitmapImage(new Uri("Images/inter4.jpg", UriKind.RelativeOrAbsolute));
            im5.Source = new BitmapImage(new Uri("Images/inter5.jpg", UriKind.RelativeOrAbsolute));
            im6.Source = new BitmapImage(new Uri("Images/inter6.jpg", UriKind.RelativeOrAbsolute));
            im7.Source = new BitmapImage(new Uri("Images/inter7.jpg", UriKind.RelativeOrAbsolute));
            im8.Source = new BitmapImage(new Uri("Images/inter8.jpg", UriKind.RelativeOrAbsolute));
            im9.Source = new BitmapImage(new Uri("Images/inter9.jpg", UriKind.RelativeOrAbsolute));
            im10.Source = new BitmapImage(new Uri("Images/inter10.jpg", UriKind.RelativeOrAbsolute));
        }

        private void adv_Click(object sender, RoutedEventArgs e)
        {
            adv_bool = true;
            kinectRegion1.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion1.IsEnabled = false;
            kinectRegion.Visibility = System.Windows.Visibility.Visible;
            kinectRegion.IsEnabled = true;
            Home.IsEnabled = true;
            Home.Visibility = System.Windows.Visibility.Visible;
            AppLogo.Visibility = System.Windows.Visibility.Hidden;
            im1.Source = new BitmapImage(new Uri("Images/adv1.jpg", UriKind.RelativeOrAbsolute));
            im2.Source = new BitmapImage(new Uri("Images/adv2.jpg", UriKind.RelativeOrAbsolute));
            im3.Source = new BitmapImage(new Uri("Images/adv3.jpg", UriKind.RelativeOrAbsolute));
            im4.Source = new BitmapImage(new Uri("Images/adv4.jpg", UriKind.RelativeOrAbsolute));
            im5.Source = new BitmapImage(new Uri("Images/adv5.jpg", UriKind.RelativeOrAbsolute));
            im6.Source = new BitmapImage(new Uri("Images/adv6.jpg", UriKind.RelativeOrAbsolute));
            im7.Source = new BitmapImage(new Uri("Images/adv7.jpg", UriKind.RelativeOrAbsolute));
            im8.Source = new BitmapImage(new Uri("Images/adv8.jpg", UriKind.RelativeOrAbsolute));
            im9.Source = new BitmapImage(new Uri("Images/adv9.jpg", UriKind.RelativeOrAbsolute));
            im10.Source = new BitmapImage(new Uri("Images/adv10.jpg", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Hover to open an image for puzzle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void importHover_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "All Image Files ( JPEG,GIF,BMP,PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files ( *.jpg;*.jpeg )|*.jpg;*.jpeg|GIF Files ( *.gif )|*.gif|BMP Files ( *.bmp )|*.bmp|PNG Files ( *.png )|*.png",
                Title = "Select an image file for generating the puzzle"
            };

            bool? result = ofd.ShowDialog(this);

            if (result.Value)
            {
                try
                {
                    DestroyReferences();
                    srcFileName = ofd.FileName;
                    using (Stream streamSource = LoadImage(srcFileName))
                    {
                        CreatePuzzle(streamSource);
                    }
                    btnShowImage.IsEnabled = true;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
                kinectRegion.Visibility = System.Windows.Visibility.Hidden;
                kinectRegion.IsEnabled = false;
                kinectRegion2.Visibility = System.Windows.Visibility.Visible;
                kinectRegion2.IsEnabled = true;
            }


        }
        #endregion HomePage_Methods

        #region PuzzleLogic_Methods
        private void CreatePuzzle(Stream streamSource)
        {
            Random rnd = new Random();
            var connections = new int[] { (int)ConnectionType.Tab, (int)ConnectionType.Blank };

            png = null;

            imageSource = null;
            var uri = new Uri(destFileName);

            //We do this to avoid memory leaks
            using (WrappingStream wrapper = new WrappingStream(streamSource))
            using (BinaryReader reader = new BinaryReader(wrapper))
            {
                imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.StreamSource = reader.BaseStream; // streamSource;
                imageSource.EndInit();
                imageSource.Freeze();
            }

            imgShowImage.Source = imageSource;

            scvImage.Visibility = Visibility.Hidden;
            cnvPuzzle.Visibility = Visibility.Visible;

            var angles = new int[] { 0, 90, 180, 270 };



            int index = 0;
            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    if (x != 1000)
                    {
                        int upperConnection = (int)ConnectionType.None;
                        int rightConnection = (int)ConnectionType.None;
                        int bottomConnection = (int)ConnectionType.None;
                        int leftConnection = (int)ConnectionType.None;

                        if (y != 0)
                            upperConnection = -1 * pieces[(y - 1) * columns + x].BottomConnection;

                        if (x != columns - 1)
                            rightConnection = connections[rnd.Next(2)];

                        if (y != rows - 1)
                            bottomConnection = connections[rnd.Next(2)];

                        if (x != 0)
                            leftConnection = -1 * pieces[y * columns + x - 1].RightConnection;

                        int angle = 0;

                        var piece = new Piece(imageSource, x, y, 0.1, 0.1, (int)upperConnection, (int)rightConnection, (int)bottomConnection, (int)leftConnection, false, index, scale);
                        piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);
                        
                        piece.MouseEnter += new MouseEventHandler(piece_MouseEnter);
                        piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                        piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);
                        piece.Rotate(piece, angle);
                        

                        var shadowPiece = new Piece(imageSource, x, y, 0.1, 0.1, (int)upperConnection, (int)rightConnection, (int)bottomConnection, (int)leftConnection, true, shadowPieces.Count(), scale);
                        shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);
                        shadowPiece.Rotate(piece, angle);

                        pieces.Add(piece);
                        shadowPieces.Add(shadowPiece);
                        index++;
                    }
                }
            }

            var tt = new TranslateTransform() { X = 20, Y = 20 };

            foreach (var p in pieces)
            {
                Random random = new Random();
                int i = random.Next(0, pnlPickUp.Children.Count);

                p.ScaleTransform.ScaleX = 1.0;
                p.ScaleTransform.ScaleY = 1.0;
                p.RenderTransform = tt;
                p.X = -1;
                p.Y = -1;
                p.IsSelected = false;

                pnlPickUp.Children.Insert(i, p);

                double angle = angles[rnd.Next(0, 4)];
                p.Rotate(p, angle);
                shadowPieces[p.Index].Rotate(p, angle);
            }


            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);

            rectSelection.StrokeDashArray = new DoubleCollection(new double[] { 4, 4, 4, 4 });
            cnvPuzzle.Children.Add(rectSelection);
        }
        private void DestroyReferences()
        {
            for (var i = cnvPuzzle.Children.Count - 1; i >= 0; i--)
            {
                if (cnvPuzzle.Children[i] is Piece)
                {
                    Piece p = (Piece)cnvPuzzle.Children[i];
                    p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    p.ClearImage();
                    cnvPuzzle.Children.Remove(p);
                }
            }

            cnvPuzzle.Children.Clear();
            SetSelectionRectangle(-1, -1, -1, -1);

            for (var i = pnlPickUp.Children.Count - 1; i >= 0; i--)
            {
                Piece p = (Piece)pnlPickUp.Children[i];
                p.ClearImage();
                p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                pnlPickUp.Children.Remove(p);
            }

            pnlPickUp.Children.Clear();

            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                pieces[i].ClearImage();
            }

            for (var i = shadowPieces.Count - 1; i >= 0; i--)
            {
                shadowPieces[i].ClearImage();
            }

            shadowPieces.Clear();
            pieces.Clear();
            imgShowImage.Source = null;
            imageSource = null;
        }

        private Stream LoadImage(string srcFileName)
        {
            imgSrc = srcFileName;

            imageSource = new BitmapImage(new Uri(srcFileName,UriKind.RelativeOrAbsolute));
            columns = (int)Math.Ceiling(imageSource.PixelWidth / 100.0);
            rows = (int)Math.Ceiling(imageSource.PixelHeight / 100.0);

            var bi = new BitmapImage(new Uri(srcFileName,UriKind.RelativeOrAbsolute));
            var imgBrush = new ImageBrush(bi);
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.UniformToFill;

            RenderTargetBitmap rtb = new RenderTargetBitmap((columns + 1) * 100, (rows + 1) * 100, bi.DpiX, bi.DpiY, PixelFormats.Pbgra32);

            var rectBlank = new System.Windows.Shapes.Rectangle();
            rectBlank.Width = columns * 100;
            rectBlank.Height = rows * 100;
            rectBlank.HorizontalAlignment = HorizontalAlignment.Left;
            rectBlank.VerticalAlignment = VerticalAlignment.Top;
            rectBlank.Fill = new SolidColorBrush(Colors.White);
            rectBlank.Arrange(new Rect(0, 0, columns * 100, rows * 100));

            var rectImage = new System.Windows.Shapes.Rectangle();
            rectImage.Width = imageSource.PixelWidth;
            rectImage.Height = imageSource.PixelHeight;
            rectImage.HorizontalAlignment = HorizontalAlignment.Left;
            rectImage.VerticalAlignment = VerticalAlignment.Top;
            rectImage.Fill = imgBrush;
            rectImage.Arrange(new Rect((columns * 100 - imageSource.PixelWidth) / 2, (rows * 100 - imageSource.PixelHeight) / 2, imageSource.PixelWidth, imageSource.PixelHeight));

            rectImage.Margin = new Thickness(
                (columns * 100 - imageSource.PixelWidth) / 2,
                (rows * 100 - imageSource.PixelHeight) / 2,
                (rows * 100 - imageSource.PixelHeight) / 2,
                (columns * 100 - imageSource.PixelWidth) / 2);

            rtb.Render(rectBlank);
            rtb.Render(rectImage);

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            Stream ret = new MemoryStream();

            png.Save(ret);

            return ret;
        }

        private bool IsPuzzleCompleted()
        {
            //All pieces must have rotation of 0 degrees
            var query = from p in pieces
                        where p.Angle != 0
                        select p;

            if (query.Any())
                return false;

            //All pieces must be connected horizontally
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - 1
                    where (p1.Index % columns < columns - 1) && (p1.X + 1 != p2.X)
                    select p1;

            if (query.Any())
                return false;

            //All pieces must be connected vertically
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - columns
                    where (p1.Y + 1 != p2.Y)
                    select p1;

            if (query.Any())
                return false;

            return true;
        }

        private void ResetZIndexes()
        {
            int zIndex = 0;
            foreach (var p in shadowPieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
            foreach (var p in pieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
        }

        private bool TrySetCurrentPiecePosition(double newX, double newY)
        {
            bool ret = true;

            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);

            var firstPiece = currentSelection[0];

            foreach (var currentPiece in currentSelection)
            {
                var relativeCellX = currentPiece.X - firstPiece.X;
                var relativeCellY = currentPiece.Y - firstPiece.Y;

                double rotatedCellX = 0;
                double rotatedCellY = 0;
                rotatedCellX = relativeCellX;
                rotatedCellY = relativeCellY;

                var q = from p in pieces
                        where (
                                (p.Index != currentPiece.Index) &&
                                (!p.IsSelected) &&
                                (cellX + rotatedCellX > 0) &&
                                (cellY + rotatedCellY > 0) &&
                                (
                                ((p.X == cellX + rotatedCellX) && (p.Y == cellY + rotatedCellY))
                                || ((p.X == cellX + rotatedCellX - 1) && (p.Y == cellY + rotatedCellY) && 
                                (p.RightConnection + currentPiece.LeftConnection != 0))
                                || ((p.X == cellX + rotatedCellX + 1) && (p.Y == cellY + rotatedCellY) && 
                                (p.LeftConnection + currentPiece.RightConnection != 0))
                                || ((p.X == cellX + rotatedCellX) && (p.Y == cellY - 1 + rotatedCellY) && 
                                (p.BottomConnection + currentPiece.UpperConnection != 0))
                                || ((p.X == cellX + rotatedCellX) && (p.Y == cellY + 1 + rotatedCellY) && 
                                (p.UpperConnection + currentPiece.BottomConnection != 0))
                                )
                              )
                        select p;

                if (q.Any())
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        private Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);

            var firstPiece = currentSelection[0];

            var relativeCellX = currentPiece.X - firstPiece.X;
            var relativeCellY = currentPiece.Y - firstPiece.Y;

            double rotatedCellX = relativeCellX;
            double rotatedCellY = relativeCellY;

            currentPiece.X = cellX + rotatedCellX;
            currentPiece.Y = cellY + rotatedCellY;

            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            return new Point(cellX, cellY);
        }

        private void SetSelectionRectangle(double x1, double y1, double x2, double y2)
        {
            double x = (x2 >= x1) ? x1 : x2;
            double y = (y2 >= y1) ? y1 : y2;
            double width = Math.Abs(x2 - x1);
            double height = Math.Abs(y2 - y1);
            rectSelection.Visibility = System.Windows.Visibility.Visible;
            rectSelection.Width = width;
            rectSelection.Height = height;
            rectSelection.StrokeThickness = 4;
            rectSelection.Stroke = new SolidColorBrush(Colors.Red);

            rectSelection.SetValue(Canvas.LeftProperty, x);
            rectSelection.SetValue(Canvas.TopProperty, y);
        }

        private void MouseUp()
        {
            if (currentSelection.Count == 0)
            {
                double x1 = (double)rectSelection.GetValue(Canvas.LeftProperty) - 20;
                double y1 = (double)rectSelection.GetValue(Canvas.TopProperty) - 20;
                double x2 = x1 + rectSelection.Width;
                double y2 = y1 + rectSelection.Height;

                int cellX1 = (int)(x1 / 100);
                int cellY1 = (int)(y1 / 100);
                int cellX2 = (int)(x2 / 100);
                int cellY2 = (int)(y2 / 100);

                var query = from p in pieces
                            where
                            (p.X >= cellX1) && (p.X <= cellX2) &&
                            (p.Y >= cellY1) && (p.Y <= cellY2)
                            select p;

                //all pieces within that area will be selected
                foreach (var currentPiece in query)
                {
                    currentSelection.Add(currentPiece);

                    currentPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[currentPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    currentPiece.BitmapEffect = shadowEffect;

                    currentPiece.RenderTransform = stZoomed;
                    currentPiece.IsSelected = true;
                    shadowPieces[currentPiece.Index].RenderTransform = stZoomed;
                }
                SetSelectionRectangle(-1, -1, -1, -1);
            }
            else
            {
                var newX = Mouse.GetPosition(cnvPuzzle).X - 20;
                var newY = Mouse.GetPosition(cnvPuzzle).Y - 20;
                if (TrySetCurrentPiecePosition(newX, newY))
                {
                    int count = currentSelection.Count;
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var currentPiece = currentSelection[i];

                        currentPiece.BitmapEffect = null;
                        ScaleTransform st = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };
                        currentPiece.RenderTransform = st;
                        currentPiece.IsSelected = false;
                        shadowPieces[currentPiece.Index].RenderTransform = st;

                        lastCell = SetCurrentPiecePosition(currentPiece, newX, newY);

                        ResetZIndexes();

                        currentPiece = null;
                    }

                    currentSelection.Clear();

                    if (IsPuzzleCompleted())
                    {
                        if (sw.IsRunning)
                            sw.Stop();
                        var result = MessageBox.Show("Congratulations! You have solved the puzzle in"+ currentTime +" !\r\n", "Puzzle Completed", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                           // SavePuzzle();
                        }
                    }
                }
                selectionAngle = 0;
            }
        }

        #endregion PuzzleLogic_Methods

        #region events

        void piece_MouseEnter(object sender, MouseEventArgs e)
        {
            
                
            
        }

        void piece_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Thread.Sleep(1000);
            if (currentSelection.Count > 0)
            {
                var axisPiece = currentSelection[0];
                foreach (var currentPiece in currentSelection)
                {
                    double deltaX = axisPiece.X - currentPiece.X;
                    double deltaY = axisPiece.Y - currentPiece.Y;

                    double targetCellX = deltaY;
                    double targetCellY = -deltaX;

                    currentPiece.Rotate(axisPiece, 90);
                    shadowPieces[currentPiece.Index].Rotate(axisPiece, 90);
                }
                selectionAngle += 90;
                if (selectionAngle == 360)
                    selectionAngle = 0;
            }
        }

        void piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var chosenPiece = (Piece)sender;

            if (chosenPiece.Parent is WrapPanel)
            {
                if (currentSelection.Count() > 0)
                {
                    var p = currentSelection[0];
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 20, Y = 20 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = System.Windows.Visibility.Visible;
                    pnlPickUp.Children.Add(p);

                    currentSelection.Clear();
                }
                else
                {
                    pnlPickUp.Children.Remove(chosenPiece);
                    cnvPuzzle.Children.Add(shadowPieces[chosenPiece.Index]);
                    chosenPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[chosenPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    chosenPiece.BitmapEffect = shadowEffect;
                    chosenPiece.RenderTransform = stZoomed;
                    shadowPieces[chosenPiece.Index].RenderTransform = stZoomed;
                    cnvPuzzle.Children.Add(chosenPiece);
                    chosenPiece.Visibility = Visibility.Hidden;
                    shadowPieces[chosenPiece.Index].Visibility = Visibility.Hidden;
                    chosenPiece.IsSelected = true;
                    currentSelection.Add(chosenPiece);
                }
            }
        }

        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseUp();
        }

        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            
            if (currentSelection.Count > 0)
            {
                foreach (var currentPiece in currentSelection)
                {
                    currentPiece.Visibility = Visibility.Visible;
                    if (shadowPieces.Count > currentPiece.Index)
                        shadowPieces[currentPiece.Index].Visibility = Visibility.Visible;
                }
            }
        }

        void cnvPuzzle_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        void cnvPuzzle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            initialRectangleX = Mouse.GetPosition((IInputElement)sender).X;
            initialRectangleY = Mouse.GetPosition((IInputElement)sender).Y;
            SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleY);
        }

        void cnvPuzzle_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoving();
        }

        private void MouseMoving()
        {
            var newX =  Mouse.GetPosition((IInputElement)cnvPuzzle).X - 20;
            var newY = Mouse.GetPosition((IInputElement)cnvPuzzle).Y - 20;

            int cellX = (int)((newX) / 100);
            int cellY = (int)((newY) / 100);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                SetSelectionRectangle(initialRectangleX, initialRectangleY, newX, newY);
            }
            else
            {
                if (currentSelection.Count > 0)
                {
                    var firstPiece = currentSelection[0];

                    //This can move around more than one piece at the same time
                    foreach (var currentPiece in currentSelection)
                    {
                        var relativeCellX = currentPiece.X - firstPiece.X;
                        var relativeCellY = currentPiece.Y - firstPiece.Y;

                        double rotatedCellX = relativeCellX;
                        double rotatedCellY = relativeCellY;

                        currentPiece.SetValue(Canvas.LeftProperty, newX - 50 + rotatedCellX * 100);
                        currentPiece.SetValue(Canvas.TopProperty, newY - 50 + rotatedCellY * 100);

                        shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, newX - 50 + rotatedCellX * 100);
                        shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, newY - 50 + rotatedCellY * 100);
                    }
                }
            }
        }

        void cnvPuzzle_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionRectangle(-1, -1, -1, -1);
            if (currentSelection.Count() > 0)
            {
                int count = currentSelection.Count();
                for (var i = count - 1; i >= 0; i--)
                {
                    var p = currentSelection[i];
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 20, Y = 20 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = System.Windows.Visibility.Visible;
                    pnlPickUp.Children.Add(p);
                }
                currentSelection.Clear();
            }
            MouseUp();
        }

        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Hidden;
            scvImage.Visibility = Visibility.Visible;
            currentViewMode = ViewMode.Picture;
            btnShowImage.Visibility = System.Windows.Visibility.Collapsed;
            btnReset.Visibility = System.Windows.Visibility.Visible;
            btnShowPuzzle.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnShowPuzzle_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;
            currentViewMode = ViewMode.Puzzle;
            btnShowImage.Visibility = System.Windows.Visibility.Visible;
            btnReset.Visibility = System.Windows.Visibility.Collapsed;
            btnShowPuzzle.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }

        #endregion events

        #region events_mainpages
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();

        }

        private void img1_Click(object sender, RoutedEventArgs e)
        {
            if ( beg_bool )
            {
                defLink = "Images/beg1.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter1.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv1.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img2_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg2.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter2.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv2.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img3_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg3.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter3.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv3.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img4_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg4.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter4.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv4.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img5_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg5.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter5.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv6.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img6_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg6.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter6.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv6.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img7_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg7.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter7.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv7.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img8_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg8.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter8.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv8.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img9_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg9.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter9.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv9.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void img10_Click(object sender, RoutedEventArgs e)
        {
            if (beg_bool)
            {
                defLink = "Images/beg10.jpg";
            }
            else if (inter_bool)
            {
                defLink = "Images/inter10.jpg";
            }
            else if (adv_bool)
            {
                defLink = "Images/adv10.jpg";
            }


            try
            {
                DestroyReferences();
                srcFileName = defLink;
                using (Stream streamSource = LoadImage(srcFileName))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            Home1.IsEnabled = true;
            Home1.Visibility = System.Windows.Visibility.Visible;
            start_Timer();
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            if (kinectRegion2.IsEnabled)
            {
                ClockTextBlock.Visibility = System.Windows.Visibility.Hidden;
                sw.Reset();
                sw.Stop();
                kinectRegion1.IsEnabled = false;
                kinectRegion1.Visibility = System.Windows.Visibility.Hidden;
                kinectRegion2.IsEnabled = false;
                kinectRegion2.Visibility = System.Windows.Visibility.Hidden;
                kinectRegion.IsEnabled = true;
                kinectRegion.Visibility = System.Windows.Visibility.Visible;
            }
            else if (kinectRegion.IsEnabled)
            {
                ClockTextBlock.Visibility = System.Windows.Visibility.Hidden;
                sw.Reset();
                sw.Stop();
                kinectRegion.IsEnabled = false;
                kinectRegion.Visibility = System.Windows.Visibility.Hidden;
                kinectRegion2.IsEnabled = false;
                kinectRegion2.Visibility = System.Windows.Visibility.Hidden;
                kinectRegion1.IsEnabled = true;
                kinectRegion1.Visibility = System.Windows.Visibility.Visible;
                beg_bool = false;
                inter_bool = false;
                adv_bool = false;
                Home1.Visibility = System.Windows.Visibility.Hidden;
                Home1.IsEnabled = false;
                Home.Visibility = System.Windows.Visibility.Hidden;
                Home.IsEnabled = false;
            } 
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DestroyReferences();
                //srcFileName = "Images/p11.jpg";
                using (Stream streamSource = LoadImage(imgSrc))
                {
                    CreatePuzzle(streamSource);
                }
                btnShowImage.IsEnabled = true;
                ClockTextBlock.Visibility = System.Windows.Visibility.Visible;
                sw.Reset();
                sw.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
            kinectRegion.Visibility = System.Windows.Visibility.Hidden;
            kinectRegion.IsEnabled = false;
            kinectRegion2.Visibility = System.Windows.Visibility.Visible;
            kinectRegion2.IsEnabled = true;
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;
            currentViewMode = ViewMode.Puzzle;
            btnShowImage.Visibility = System.Windows.Visibility.Visible;
            btnShowPuzzle.Visibility = System.Windows.Visibility.Collapsed;

        }
        #endregion events_mainpages
    }

    public enum ViewMode
    {
        Picture,
        Puzzle
    }

}
