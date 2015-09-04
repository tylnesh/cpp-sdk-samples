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
using System.Collections.Specialized;

using Microsoft.Win32;
using System.Reflection;

namespace AffdexMe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, Affdex.ImageListener, Affdex.ProcessStatusListener
    {

        #region Member Variables and Enums

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

        private StringCollection mEnabledClassifiers;

        private DateTime mStartTime;
        private float mCurrentTimeStamp;

        /// <summary>
        /// Scale factor based on ratio between current and original size
        /// </summary>
        private double mImageXScaleFactor;
        private double mImageYScaleFactor;

        private bool mShowFacePoints;
        private bool mShowMeasurements;

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

	    public void onImageResults(Dictionary<int, Affdex.Face> faces, Affdex.Frame image)
        {
            // For now only single face is supported
            if ((faces.Count() >= 1))
            {
                Affdex.Face face = faces[0];

                UpdateClassifierPanel(face);
                DisplayFeaturePoints(image, face);
                DisplayMeasurements(face);   
            }
	    }

	    public void onImageCapture(Affdex.Frame image) 
	    {
            UpdateClassifierPanel();
            DisplayImageToOffscreenCanvas(image);
        }

        public void onProcessingException(Affdex.AffdexException ex)
        {
            String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
            ShowExceptionAndShutDown(message);            
        }

        public void onProcessingFinished()
        {
        }
         
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

        private String GetClassifierDataFolder()
        {
            String affdexClassifierDir = Environment.GetEnvironmentVariable("AFFDEX_DATA_DIR");
            if (String.IsNullOrEmpty(affdexClassifierDir))
            {
                ShowExceptionAndShutDown("AFFDEX_DATA_DIR environment variable (Classifier Data Directory) is not set");
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(affdexClassifierDir);
            if (!directoryInfo.Exists)
            {
                ShowExceptionAndShutDown("AFFDEX_DATA_DIR (Classifier Data Directory) is set to an invalid folder location");
            }

            return affdexClassifierDir;
        }

        private String GetAffdexLicense()
        {
            String licensePath = String.Empty;
            licensePath = Environment.GetEnvironmentVariable("AFFDEX_LICENSE_DIR");
            if (String.IsNullOrEmpty(licensePath))
            {
                ShowExceptionAndShutDown("AFFDEX_LICENSE_DIR environment variable (Affdex License Folder) is not set");
            }

            return licensePath;
        }

        public MainWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCameraApp();

            mEnabledClassifiers = AffdexMe.Settings.Default.Classifiers;
            // Enable/Disable buttons on start
            btnStartCamera.IsEnabled =
            btnResetCamera.IsEnabled =
            btnShowPoints.IsEnabled =
            btnStopCamera.IsEnabled =
            btnExit.IsEnabled = true;

            if (AffdexMe.Settings.Default.ShowPoints)
            {
                btnShowPoints_Click(null, null);
            }

            if (AffdexMe.Settings.Default.ShowMeasurements)
            {
                btnShowMeasurements_Click(null, null);
            }

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

        private void DisplayMeasurements(Affdex.Face affdexFace)
        {
            //Update measurements
           try
           {
                var result = this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (mShowMeasurements && (affdexFace != null))
                        {
                            interocularDistanceDisplay.Text = String.Format("Interocular Distance: {0}", affdexFace.Measurements.InterocularDistance);
                            pitchDisplay.Text = String.Format("Pitch Angle: {0}", affdexFace.Measurements.Orientation.Pitch);
                            yawDisplay.Text = String.Format("Yaw Angle: {0}", affdexFace.Measurements.Orientation.Yaw);
                            rollDisplay.Text = String.Format("Roll Angle: {0}", affdexFace.Measurements.Orientation.Roll);
                        }
                    }));
            }
            catch(Exception ex)
            {
                String message = String.IsNullOrEmpty(ex.Message) ? "AffdexMe error encountered." : ex.Message;
                ShowExceptionAndShutDown(message);
            }
        }

        private void DisplayFeaturePoints(Affdex.Frame affdexImage, Affdex.Face affdexFace)
        {
            try
            {
                // Plot Face Points
                if ((mShowFacePoints) && (affdexFace != null))
                {
                    var result = this.Dispatcher.BeginInvoke((Action)(() =>
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
                            var featurePoints = affdexFace.FeaturePoints;
                            foreach (var point in featurePoints)
                            {
                                Ellipse ellipse = new Ellipse()
                                {
                                    Width = 4,
                                    Height = 4,
                                    Fill = pointBrush
                                };

                                canvasFacePoints.Children.Add(ellipse);
                                Canvas.SetLeft(ellipse, point.X * mImageXScaleFactor);
                                Canvas.SetTop(ellipse, point.Y * mImageYScaleFactor);
                            }

                            // Draw Face Bounding Rectangle
                            var xMax = featurePoints.Max(r => r.X);
                            var xMin = featurePoints.Min(r => r.X);
                            var yMax = featurePoints.Max(r => r.Y);
                            var yMin = featurePoints.Min(r => r.Y);

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
                        }
                    }));
                }
            }
            catch(Exception ex)
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
                        int index = 0;
                        foreach (String metric in mEnabledClassifiers)
                        {
                            PropertyInfo info;
                            float value = -1;
                            if ((info = face.Expressions.GetType().GetProperty(NameMappings(metric))) != null) value = (float)info.GetValue(face.Expressions, null);
                            else if ((info = face.Emotions.GetType().GetProperty(NameMappings(metric))) != null) value = (float)info.GetValue(face.Emotions, null);
                           
                            // Convert classifier value to Integer (percentage) for display purposes
                            mAffdexClassifierValues[index] = Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));
                            index++;

                        }

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
                        for (int r = 0; r < mAffdexClassifierValues.Count(); r++) mAffdexClassifierValues[r] = 0;

                        // If we haven't seen a face in the past 30 frames (roughly 30/15fps seconds), don't display the classifiers
                        if (mCachedSkipFaceResultsCount >= 30)
                        {
                            displayClassifiers = false;
                        }
                    }

                    var result = this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        // Only display the classifiers and FacePoints if we've had a re
                        if (displayClassifiers)
                        {
                            int r = 0;
                            foreach (String classifier in mEnabledClassifiers)
                            {
                                String stackPanelName = String.Format("stackPanel{0}", r);
                                TextBlock ClassifierName = (TextBlock) gridClassifierDisplay.FindName(String.Format("{0}Name", stackPanelName));
                                TextBlock ClassifierValueBackgroud = (TextBlock)gridClassifierDisplay.FindName(String.Format("{0}ValueBackgroud", stackPanelName));
                                TextBlock ClassifierValue = (TextBlock)gridClassifierDisplay.FindName(String.Format("{0}Value", stackPanelName));
                                // Update the Classifier Display
                                UpdateClassifier(ClassifierName, ClassifierValue, ClassifierValueBackgroud, classifier, r);
                                r++;

                            }
                        }

                        // Update the Image control from the UI thread
                        if ((mCameraDetector != null) && (mCameraDetector.isRunning()))
                        {
                            if (imgAffdexFaceDisplay.Visibility == Visibility.Hidden)
                            {
                                imgAffdexFaceDisplay.Visibility =
                                stackPanelClassifiersBackground.Visibility = 
                                stackPanelLogoBackground.Visibility = Visibility.Visible;
                            }
                            stackPanelClassifiers.Visibility = (displayClassifiers)?Visibility.Visible : Visibility.Hidden;
                            interocularDistanceDisplay.Visibility = (displayClassifiers && mShowMeasurements) ? Visibility.Visible : Visibility.Hidden;
                            pitchDisplay.Visibility = (displayClassifiers && mShowMeasurements) ? Visibility.Visible : Visibility.Hidden;
                            yawDisplay.Visibility = (displayClassifiers && mShowMeasurements) ? Visibility.Visible : Visibility.Hidden;
                            rollDisplay.Visibility = (displayClassifiers && mShowMeasurements) ? Visibility.Visible : Visibility.Hidden;
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

        private String NameMappings(String classifierName)
        {
            if (classifierName == "Frown")
            {
                return "LipCornerDepressor";
            }
            return classifierName;
        }

        private void UpdateClassifier(TextBlock txtClassifier, TextBlock txtClassifierValue, 
                                      TextBlock txtClassifierValueBackground, String classifierName, int classifierIndex)
        {
            try
            {
                UpperCaseConverter conv = new UpperCaseConverter();
                txtClassifier.Text = (String)conv.Convert(classifierName, null, null, null);
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

                txtClassifierValueBackground.Background = new SolidColorBrush(backgroundColor);
                txtClassifierValueBackground.Width = width;
                txtClassifierValue.Text = String.Format("{0}%", classifierValue);
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
            var result = this.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    mCurrentTimeStamp = image.getTimestamp();

                    // Update the Image control from the UI thread
                    //imgAffdexFaceDisplay.Source = rtb;
                    imgAffdexFaceDisplay.Source = ConstructImage(image.getBGRByteArray(), image.getWidth(), image.getHeight());

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
                btnShowMeasurements.Click += btnShowMeasurements_Click;
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
                mShowMeasurements = false;

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

        void btnShowMeasurements_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Style style;
                String buttonText = String.Empty;

                mShowMeasurements = !mShowMeasurements;
                if (mShowMeasurements)
                {
                    style = this.FindResource("PointsOnButtonStyle") as Style;
                    buttonText = "Hide Measurements";
                    interocularDistanceDisplay.Visibility = Visibility.Visible;
                    pitchDisplay.Visibility = Visibility.Visible;
                    yawDisplay.Visibility = Visibility.Visible;
                    rollDisplay.Visibility = Visibility.Visible;
                }
                else
                {
                    style = this.FindResource("CustomButtonStyle") as Style;
                    buttonText = "Show Measurements";
                    interocularDistanceDisplay.Visibility = Visibility.Hidden;
                    pitchDisplay.Visibility = Visibility.Hidden;
                    yawDisplay.Visibility = Visibility.Hidden;
                    rollDisplay.Visibility = Visibility.Hidden;
                }

                btnShowMeasurements.Style = style;
                btnShowMeasurements.Content = buttonText;

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
            SaveSettings();
            Application.Current.Shutdown();
        }

        void SaveSettings()
        {
            AffdexMe.Settings.Default.ShowPoints = mShowFacePoints;
            AffdexMe.Settings.Default.ShowMeasurements = mShowMeasurements;
            AffdexMe.Settings.Default.Classifiers = mEnabledClassifiers;
            AffdexMe.Settings.Default.Save();
        }

        private void ClearClassifiersAndPointsDisplay()
        {
            // Hide AffdexFace Image
            imgAffdexFaceDisplay.Visibility =
            stackPanelLogoBackground.Visibility =
            stackPanelClassifiersBackground.Visibility = Visibility.Hidden;

            //Clean measurements
            interocularDistanceDisplay.Text = String.Format("Interocular Distance: {0}", 0);
            pitchDisplay.Text = String.Format("Pitch Angle: {0}", 0);
            yawDisplay.Text = String.Format("Yaw Angle: {0}", 0);
            rollDisplay.Text = String.Format("Roll Angle: {0}", 0);

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

        private void TurnOnClassifiers()
        {
            mCameraDetector.setDetectAllEmotions(false);
            mCameraDetector.setDetectAllExpressions(false);
            foreach (String metric in mEnabledClassifiers)
            {
                MethodInfo setMethodInfo = mCameraDetector.GetType().GetMethod(String.Format("setDetect{0}", NameMappings(metric)));
                setMethodInfo.Invoke(mCameraDetector, new object[] { true });
            }
        }

        private void StartCameraProcessing()
        {
            try
            {
                btnStartCamera.IsEnabled = false;
                btnResetCamera.IsEnabled =
                btnShowPoints.IsEnabled =
                btnStopCamera.IsEnabled =
                btnExit.IsEnabled = true;

                // Instantiate CameraDetector using default camera ID
                mCameraDetector = new Affdex.CameraDetector();
                mCameraDetector.setClassifierPath(GetClassifierDataFolder());

                // Set the Classifiers that we are interested in tracking
                TurnOnClassifiers();

                // Initialize Classifier cache
                for (int index = 0; index < mAffdexClassifierValues.Count(); index++)
                {
                    mAffdexClassifierValues[index] = 0;
                }

                mCachedSkipFaceResultsCount = 0;
                mCameraDetector.setImageListener(this);
                mCameraDetector.setProcessStatusListener(this);



                // Set the License Path
                mCameraDetector.setLicensePath(GetAffdexLicense());

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
            SaveSettings();
            Application.Current.Shutdown();
        }

        private void btnChooseWin_Click(object sender, RoutedEventArgs e)
        {
            Boolean wasRunning = false;
            if ((mCameraDetector != null) && (mCameraDetector.isRunning()))
            {
                StopCameraProcessing();
                ResetDisplayArea();
                wasRunning = true;
            }
            
            MetricSelectionUI w = new MetricSelectionUI(mEnabledClassifiers);
            w.ShowDialog();
            mEnabledClassifiers = w.Classifiers;
            if (wasRunning)
            {
                StartCameraProcessing();
            }
        }

    }
}
