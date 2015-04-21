using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Win32;

namespace AffdexMe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Location of Affdex Data files
        /// </summary>
        private const String AFFDEX_DATA_PATH = "C:\\Program Files (x86)\\Affectiva\\Affdex SDK\\data";

        /// <summary>
        /// Location of AffdexSDK Licence file
        /// </summary>
        private const String AFFDEX_LICENSE_FILE = "C:\\Affectiva\\Build\\AffdexfaceWindows\\bin\\affdex.license";

        #region Member Variables and Enums

        enum AffdexFaceClassifiers : int
        {
            Smile = 0,
            BrowFurrow = 1,
            BrowRaise = 2,
            LipCornerDepressor = 3,
            Engagement = 4,
            Valence = 5
        };

        /// <summary>
        /// The minimum length of the Classifier Value textbox
        /// </summary>
        const int ClassiferValueDisplayLength = 90;

        /// <summary>
        /// So the Classifiers can be cached
        /// </summary>
        int[] mAffdexClassifierValues = new int[6];

        /// <summary>
        /// Once a face has been recognized, the number of captures that occur before the classifiers get zero displayed
        /// This helps prevent classifier numbers from flashing on the screen.
        /// </summary>
        int mCachedSkipFaceResultsCount;

        /// <summary>
        /// Once a face's feature points get displayed, the number of successive captures that occur without
        /// the points getting redrawn in the OnResults callback.
        /// </summary>
        int mFeaturePointsSkipCount;

        /// <summary>
        /// Used to delay the display of the Classifier panel until the 1st face is recognized
        /// </summary>
        bool mFirstFaceRecognized;

        private Affdex.CameraDetector mCameraDetector;

        private DateTime mStartTime;

        private float mCurrentTimeStamp;

        /// <summary>
        /// Scale factor based on ratio between current and original size
        /// </summary>
        private double mImageXScaleFactor;
        private double mImageYScaleFactor;

        private bool mShowFacePoints;

        #endregion

        #region Image and Results Arg Classes
        /// <summary>
        /// 
        /// </summary>
        class ImageCaptureDataUpdateArgs
        {
            public float ImageCaptureTimeStamp {get; set;}
            public Affdex.Frame Image { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        class ImageResultsDataUpdateArgs
        {
            public float ImageResultsTimeStamp { get; set; }
            public Affdex.Frame Image { get; set; }
            public Affdex.Face Face { get; set; }
        }

        #endregion

        #region Listener Implementation

        /// <summary>
        /// 
        /// </summary>
        class Listener : Affdex.ImageListener, Affdex.ProcessStatusListener
        {
            public event EventHandler<ImageCaptureDataUpdateArgs> ImageCaptureUpdate;
            public event EventHandler<ImageResultsDataUpdateArgs> ImageResultsUpdate;
            public event EventHandler<Affdex.AffdexException> ExceptionHandler;

	        public void onImageResults(Dictionary<int, Affdex.Face> faces, Affdex.Frame image)
            {
                // For now only single face is supported
                if ((faces.Count() >= 1))
                {
                    Affdex.Face face = faces[0];

                    if (ImageResultsUpdate != null)
                    {
                        ImageResultsDataUpdateArgs imageResultsData = new ImageResultsDataUpdateArgs()
                        {
                            Image = image,
                            ImageResultsTimeStamp = image.getTimestamp(),
                            Face = face
                        };
                        ImageResultsUpdate(this, imageResultsData);
                    }
                }
	        }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="affdexImage"></param>
	        public void onImageCapture(Affdex.Frame image) 
	        {
                if (ImageCaptureUpdate != null)
                {
                    ImageCaptureDataUpdateArgs imageCaptureData = new ImageCaptureDataUpdateArgs()
                    {
                        Image = image,
                        ImageCaptureTimeStamp = image.getTimestamp()
                    };
                    ImageCaptureUpdate(this, imageCaptureData);
                    
                }
            }

            public void onProcessingException(Affdex.AffdexException ex)
            {
                if (ExceptionHandler != null)
                {
                    ExceptionHandler(this, ex);
                }                
            }

            public void onProcessingFinished()
            {
            }
        };
         
        #endregion

        private void ShowExceptionAndShutDown(String exceptionMessage)
        {
            MessageBoxResult result = MessageBox.Show(exceptionMessage,
                                                        "AffdexMe Error",
                                                        MessageBoxButton.OK,
                                                        MessageBoxImage.Error);
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                StopCameraProcessing();
            }));
        }

        public MainWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCameraApp();

            // Enable/Disable buttons on start
            btnStartCamera.IsEnabled =
            btnResetCamera.IsEnabled =
            btnShowPoints.IsEnabled =
            btnStopCamera.IsEnabled =
            btnExit.IsEnabled = true;

            this.ContentRendered += MainWindow_ContentRendered;
        }

        /// <summary>
        /// Once the window las been loaded and the content rendered, the camera
        /// can be initialized and started. This sequence allows for the underlying controls
        /// and watermark logo to be displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            StartCameraProcessing();
        }

        /// <summary>
        /// 
        /// </summary>
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private BitmapSource ConstructImage(byte[] imageData, int width, int height)
        {
            try
            {
                if (imageData != null && imageData.Length > 0)
                {
                    var stride = (width * PixelFormats.Bgr24.BitsPerPixel + 7) / 8;
                    var imageSrc = BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgr24, null, imageData, stride);
                    return imageSrc;
                }
            }
            catch(Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }

            return null;
        }

        private void DisplayFeaturePoints(Affdex.Frame affdexImage, Affdex.Face affdexFace)
        {
            try
            {
                // Plot Face Points
                if ((mShowFacePoints) && (affdexFace != null))
                {
                    canvasFacePoints.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if ((mCameraDetector != null) && (mCameraDetector.isRunning()))
                        {
                            // Clear the previous points
                            canvasFacePoints.Children.Clear();
                            canvasFacePoints.Width = imgAffdexFaceDisplay.ActualWidth;
                            canvasFacePoints.Height = imgAffdexFaceDisplay.ActualHeight;

                            mImageXScaleFactor = imgAffdexFaceDisplay.ActualWidth / affdexImage.getWidth();
                            mImageYScaleFactor = imgAffdexFaceDisplay.ActualHeight / affdexImage.getHeight();

                            SolidColorBrush pointBrush = new SolidColorBrush(Colors.Cornsilk);
                            var featurePoints = affdexFace.getFeaturePoints();
                            foreach (var point in featurePoints)
                            {
                                Ellipse ellipse = new Ellipse()
                                {
                                    Width = 4,
                                    Height = 4,
                                    Fill = pointBrush
                                };

                                canvasFacePoints.Children.Add(ellipse);
                                Canvas.SetLeft(ellipse, point.x * mImageXScaleFactor);
                                Canvas.SetTop(ellipse, point.y * mImageYScaleFactor);
                            }

                            // Draw Face Bounding Rectangle
                            var xMax = featurePoints.Max(r => r.x);
                            var xMin = featurePoints.Min(r => r.x);
                            var yMax = featurePoints.Max(r => r.y);
                            var yMin = featurePoints.Min(r => r.y);

                            // Adjust the x/y min to accomodate all points
                            xMin -= 2;
                            yMin -= 2;

                            // Increase the width/height to accomodate the entire max pixel position
                            // EllipseWidth + N to make sure max points in the box
                            double width = (xMax - xMin + 6) * mImageXScaleFactor;
                            double height = (yMax - yMin + 6) * mImageYScaleFactor;

                            SolidColorBrush boundingBrush = new SolidColorBrush(Colors.Bisque);
                            Rectangle boundingBox = new Rectangle()
                            {
                                Width = width,
                                Height = height,
                                Stroke = boundingBrush,
                                StrokeThickness = 1,
                            };

                            canvasFacePoints.Children.Add(boundingBox);
                            Canvas.SetLeft(boundingBox, xMin * mImageXScaleFactor);
                            Canvas.SetTop(boundingBox, yMin * mImageYScaleFactor);

                            mFeaturePointsSkipCount = 0;

                            affdexFace.Dispose();
                            affdexImage.Dispose();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }


        /// <summary>
        /// Since the panel is getting updated from a separate callback thread, access to controls must be
        /// made through BeginInvoke()
        /// </summary>
        /// <param name="face"></param>
        private void UpdateClassifierPanel(Affdex.Face face = null)
        {
            try
            {
                bool displayClassifiers = (imgAffdexFaceDisplay.Visibility == Visibility.Hidden)? false : true;

                if (mCameraDetector.isRunning() == true)
                {
                    // A Face was found - this comes from ImageResults CallBack
                    if (face != null)
                    {
                        // Convert classifier value to Integer (percentage) for display purposes
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Smile] = Convert.ToInt32(Math.Round(face.getSmileScore(), MidpointRounding.AwayFromZero));
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.BrowFurrow] = Convert.ToInt32(Math.Round(face.getBrowFurrowScore(), MidpointRounding.AwayFromZero));
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.BrowRaise] = Convert.ToInt32(Math.Round(face.getBrowRaiseScore(), MidpointRounding.AwayFromZero));
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.LipCornerDepressor] = Convert.ToInt32(Math.Round(face.getLipCornerDepressorScore(), MidpointRounding.AwayFromZero));
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Engagement] = Convert.ToInt32(Math.Round(face.getEngagementScore(), MidpointRounding.AwayFromZero));
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Valence] = Convert.ToInt32(Math.Round(face.getValenceScore(), MidpointRounding.AwayFromZero));

                        // Reset the cache count
                        mCachedSkipFaceResultsCount = 0;
                        mFirstFaceRecognized =
                        displayClassifiers = true;
                    }
                    else if (mFirstFaceRecognized == false)
                    {
                        displayClassifiers = false;
                    }
                    else if (++mCachedSkipFaceResultsCount > 10)
                    {
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Smile] =
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.BrowFurrow] =
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.BrowRaise] =
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.LipCornerDepressor] =
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Engagement] =
                        mAffdexClassifierValues[(int)AffdexFaceClassifiers.Valence] = 0;

                        // If we haven't seen a face in the past 30 frames (roughly 30/15fps seconds), don't display the classifiers
                        if (mCachedSkipFaceResultsCount >= 30)
                        {
                            displayClassifiers = false;
                        }
                    }

                    // Only display the classifiers and FacePoints if we've had a re
                    if (displayClassifiers)
                    {
                        // Update the Classifier Display
                        UpdateClassifier(txtSmileClassifierName, txtSmileClassifierValue, txtSmileClassifierValueBackground, AffdexFaceClassifiers.Smile);
                        UpdateClassifier(txtFrownClassifierName, txtFrownClassifierValue, txtFrownClassifierValueBackground, AffdexFaceClassifiers.LipCornerDepressor);
                        UpdateClassifier(txtBrowRaiseClassifierName, txtBrowRaiseClassifierValue, txtBrowRaiseClassifierValueBackground, AffdexFaceClassifiers.BrowRaise);
                        UpdateClassifier(txtValenceClassifierName, txtValenceClassifierValue, txtValenceClassifierValueBackground, AffdexFaceClassifiers.Valence);
                        UpdateClassifier(txtBrowLowerClassifierName, txtBrowLowerClassifierValue, txtBrowLowerClassifierValueBackground, AffdexFaceClassifiers.BrowFurrow);
                        UpdateClassifier(txtEngagementClassifierName, txtEngagementClassifierValue, txtEngagementClassifierValueBackground, AffdexFaceClassifiers.Engagement);
                    }

                    // Update the Image control from the UI thread
                    stackPanelClassifiers.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if ((mCameraDetector != null) && (mCameraDetector.isRunning()))
                        {
                            if (imgAffdexFaceDisplay.Visibility == Visibility.Hidden)
                            {
                                imgAffdexFaceDisplay.Visibility =
                                stackPanelClassifiersBackground.Visibility = 
                                stackPanelLogoBackground.Visibility = Visibility.Visible;
                            }
                            stackPanelClassifiers.Visibility = (displayClassifiers)?Visibility.Visible : Visibility.Hidden;
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void UpdateClassifier(TextBlock txtClassifier, TextBlock txtClassifierValue, 
                                      TextBlock txtClassifierValueBackground, AffdexFaceClassifiers classifierIndex)
        {
            try
            {
                int classifierValue = mAffdexClassifierValues[(int)classifierIndex];

                // Calculate the width
                double width = ClassiferValueDisplayLength * Math.Abs(classifierValue) / 100;

                var backgroundColor = Colors.Transparent;

                if (classifierValue > 0)
                {
                    backgroundColor = Colors.LimeGreen;
                }
                else if (classifierValue < 0)
                {
                    backgroundColor = Colors.Red;
                }

                txtClassifierValue.Dispatcher.BeginInvoke((Action)(() =>
                {
                    txtClassifierValueBackground.Background = new SolidColorBrush(backgroundColor);
                    txtClassifierValueBackground.Width = width;
                    txtClassifierValue.Text = String.Format("{0}%", classifierValue);
                }));

            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }        
    
        private void DisplayImageToOffscreenCanvas(Affdex.Frame image)
        {
            // Update the Image control from the UI thread
            imgAffdexFaceDisplay.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    DateTime functionEnter = DateTime.Now;
                    mCurrentTimeStamp = image.getTimestamp();

                    // Update the Image control from the UI thread
                    //imgAffdexFaceDisplay.Source = rtb;
                    imgAffdexFaceDisplay.Source = ConstructImage(image.getBGRByteArray(), image.getWidth(), image.getHeight());
                    var elapseTime = (DateTime.Now - functionEnter).Milliseconds;

                    // Allow N successive OnCapture callbacks before the FacePoint drawing canvas gets cleared.
                    if (++mFeaturePointsSkipCount > 4)
                    {
                        canvasFacePoints.Children.Clear();
                        mFeaturePointsSkipCount = 0;
                    }

                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                    ShowExceptionAndShutDown(message);
                }
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitializeCameraApp()
        {
            try
            {
                mCameraDetector = null;

                // Initialize Button Click Handlers
                btnStartCamera.Click += btnStartCamera_Click;
                btnStopCamera.Click += btnStopCamera_Click;
                btnShowPoints.Click += btnShowPoints_Click;
                btnResetCamera.Click += btnResetCamera_Click;
                btnExit.Click += btnExit_Click;

                // Disable Stop/Reset buttons
                btnResetCamera.IsEnabled =
                btnStopCamera.IsEnabled = false;

                mFeaturePointsSkipCount =
                mCachedSkipFaceResultsCount = 0;

                // Initially hide Classifier Panels
                stackPanelLogoBackground.Visibility =
                stackPanelClassifiersBackground.Visibility =
                stackPanelClassifiers.Visibility = Visibility.Hidden;

                // Face Points are off by default
                mShowFacePoints = false;

                // Show the logo
                imgAffdexLogoDisplay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnShowPoints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Style style;
                String buttonText = String.Empty;

                mShowFacePoints = !mShowFacePoints;
                if (mShowFacePoints)
                {
                    style = this.FindResource("PointsOnButtonStyle") as Style;
                    buttonText = "Hide Points";
                }
                else
                {
                    style = this.FindResource("CustomButtonStyle") as Style;
                    buttonText = "Show Points";
                }

                btnShowPoints.Style = style;
                btnShowPoints.Content = buttonText;
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void btnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            ResetCameraProcessing();
        }

        void btnStartCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartCameraProcessing();
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        void btnStopCamera_Click(object sender, RoutedEventArgs e)
        {
            StopCameraProcessing();
            ResetDisplayArea();
        }

        void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ClearClassifiersAndPointsDisplay()
        {
            // Hide AffdexFace Image
            imgAffdexFaceDisplay.Visibility =
            stackPanelLogoBackground.Visibility =
            stackPanelClassifiersBackground.Visibility = Visibility.Hidden;

            // Hide the Classifier Panel
            stackPanelClassifiers.Visibility = Visibility.Hidden;

            // Clear any Face Points
            canvasFacePoints.Children.Clear();
        }

        private void ResetDisplayArea()
        {
            try
            {
                ClearClassifiersAndPointsDisplay();

                // Show the logo
                imgAffdexLogoDisplay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void StartCameraProcessing()
        {
            try
            {
                // Instantiate CameraDetector using default camera ID
                mCameraDetector = new Affdex.CameraDetector();
                mCameraDetector.setClassifierPath(AFFDEX_DATA_PATH);

                // Set the Classifiers that we are interested in tracking
                mCameraDetector.setDetectSmile(true);
                mCameraDetector.setDetectBrowFurrow(true);
                mCameraDetector.setDetectBrowRaise(true);
                mCameraDetector.setDetectLipCornerDepressor(true);
                mCameraDetector.setDetectEngagement(true);
                mCameraDetector.setDetectValence(true);

                // Initialize Classifier cache
                for (int index = 0; index < mAffdexClassifierValues.Count(); index++)
                {
                    mAffdexClassifierValues[index] = 0;
                }

                mCachedSkipFaceResultsCount = 0;

                Listener listener = new Listener();
                listener.ImageCaptureUpdate += imageListener_ImageCaptureUpdate;
                listener.ImageResultsUpdate += imageListener_ImageResultsUpdate;
                listener.ExceptionHandler += imageListener_ExceptionHandler;

                mCameraDetector.setImageListener(listener);
                mCameraDetector.setProcessStatusListener(listener);

                btnStartCamera.IsEnabled = false;
                btnResetCamera.IsEnabled =
                btnShowPoints.IsEnabled =
                btnStopCamera.IsEnabled =
                btnExit.IsEnabled = true;

                // Set the License Path
                mCameraDetector.setLicensePath(AFFDEX_LICENSE_FILE);

                mStartTime = DateTime.Now;
                mCameraDetector.start();

                // Delay loading the Classifier panel until 1st face
                mFirstFaceRecognized = false;

                // Hide the logo
                imgAffdexLogoDisplay.Visibility = Visibility.Hidden;
            }
            catch(Affdex.AffdexException ex)
            {
                if (!String.IsNullOrEmpty(ex.Message))
                {
                    // If this is a camera failure, then reset the application to allow the user to turn on/enable camera
                    if (ex.Message.Equals("Unable to open webcam."))
                    {
                        MessageBoxResult result = MessageBox.Show(ex.Message,
                                                                "AffdexMe Error",
                                                                MessageBoxButton.OK,
                                                                MessageBoxImage.Error);
                        StopCameraProcessing();
                        return;
                    }
                }

                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
            catch(Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        void imageListener_ExceptionHandler(object sender, Affdex.AffdexException ex)
        {
            String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
            ShowExceptionAndShutDown(message);
        }

        void imageListener_ImageResultsUpdate(object sender, MainWindow.ImageResultsDataUpdateArgs e)
        {
            UpdateClassifierPanel(e.Face);
            DisplayFeaturePoints(e.Image, e.Face);
        }

        void imageListener_ImageCaptureUpdate(object sender, MainWindow.ImageCaptureDataUpdateArgs e)
        {
            UpdateClassifierPanel();
            DisplayImageToOffscreenCanvas(e.Image);
        }           

        private void ResetCameraProcessing()
        {
            try
            {
                mCameraDetector.reset();
            }
            catch(Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void StopCameraProcessing()
        {
            try
            {
                if ((mCameraDetector != null) && (mCameraDetector.isRunning()))
                {
                    mCameraDetector.stop();
                    mCameraDetector.Dispose();
                    mCameraDetector = null;
                }

                // Enable/Disable buttons on start
                btnStartCamera.IsEnabled = true;
                btnResetCamera.IsEnabled =
                btnStopCamera.IsEnabled = false;

            }
            catch(Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCameraProcessing();
            Application.Current.Shutdown();
        }

    }
}
