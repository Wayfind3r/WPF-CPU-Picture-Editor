using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using System.Diagnostics;

namespace Photo_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Top = 0;
            this.Left = 0;
        }

        private static List<Image> snapShotList;
        private static int snapShotIndex = -1;
        private static Stopwatch stopWatch = null;
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            snapShotList = new List<Image>();
        }

        /// <summary>
        /// Starts recording time, if we are not already recording
        /// </summary>
        private void StartRecordingTime()
        {
            if (stopWatch == null)
            {
                stopWatch = new Stopwatch();
                stopWatch.Start();
            }
        }

        /// <summary>
        /// Displays recorded time and resets the timer
        /// </summary>
        private void DisplayAndResetTime()
        {
            stopWatch.Stop();

            var elapsedTime = stopWatch.ElapsedMilliseconds;

            ProcessingTime.Text = "ExecutionTime: " + elapsedTime.ToString() + " ms";

            stopWatch = null;
        }

        /// <summary>
        /// Load image into LoadedImage frame and resize Main Window
        /// </summary>
        private void LoadImage(Image thisImage)
        {
            StartRecordingTime();

            if (Application.Current.MainWindow.Width > 400)
                Application.Current.MainWindow.Width = 400;
            if (Application.Current.MainWindow.Height > 650)
                Application.Current.MainWindow.Height = 650;
            double primaryScreenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double primaryScreenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            ImageGrid.Visibility = Visibility.Visible;
            if (Application.Current.MainWindow.Width + thisImage.Source.Width - 14 >= primaryScreenWidth)
                Application.Current.MainWindow.Width = primaryScreenWidth;
            else
            {
                Application.Current.MainWindow.Width += thisImage.Source.Width - 14;
            }
            if (Application.Current.MainWindow.Height + thisImage.Source.Height - 550 >= primaryScreenHeight)
                Application.Current.MainWindow.Height = primaryScreenHeight;
            else
            {
                if (thisImage.Source.Height > 550)
                {
                    Application.Current.MainWindow.Height += (thisImage.Source.Height - 550);
                }
            }
            LoadedImage.Source = thisImage.Source;

            NumberOfPixelsX.Text = "Pixels X:  " + (int)thisImage.Source.Width;
            ResizeWidthTextBox.Text = ((int)thisImage.Source.Width).ToString();
            NumberOfPixelsY.Text = "Pixels Y:  " + (int)thisImage.Source.Height;
            ResizeHeightTextBox.Text = ((int)thisImage.Source.Height).ToString();
            TotalPixels.Text = "Total Pixels:  " + (int)(thisImage.Source.Width * thisImage.Source.Height);
            ExactPixel.Text = "Pixel:";

            DisplayAndResetTime();
        }

        /// <summary>
        /// Undo and Redo Commands
        /// </summary>
        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool canUndo = (snapShotIndex > 0);
            e.CanExecute = canUndo;
        }

        private void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartRecordingTime();

            snapShotIndex--;
            LoadImage(snapShotList[snapShotIndex]);
        }
        private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var canRedo = (snapShotIndex != -1 && snapShotIndex < snapShotList.Count - 1);
            e.CanExecute = canRedo;
        }

        private void RedoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartRecordingTime();

            snapShotIndex++;
            LoadImage(snapShotList[snapShotIndex]);
        }

        private void ManageSnapShotList(Image newImage)
        {
            if (snapShotList.Count == 0)
            {
                snapShotIndex = 0;
            }
            else
            {
                if (snapShotIndex < snapShotList.Count - 1)
                {
                    snapShotList.RemoveRange(snapShotIndex + 1, snapShotList.Count - snapShotIndex - 1);
                    snapShotIndex = snapShotList.Count;
                }
                else
                {
                    snapShotIndex++;
                }
            }
            snapShotList.Add(newImage);
        }
        /// <summary>
        /// Resize Command, TextBoxes and Button
        /// </summary>
        private void ResizeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isFileOpen;
        }

        private void ResizeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CollapseDetailedFilterOptionsGrid();

            DetailedFilterOptionsGrid.Visibility = Visibility.Visible;
            FilterDetailsBorder.Visibility = Visibility.Visible;
            FilterDetailsTextBlock.Visibility = Visibility.Visible;
            FilterDetailsOptionsBorder.Visibility = Visibility.Visible;
            ImageFiltersBorder.Visibility = Visibility.Visible;

            FilterDetailsTextBlock.Text = "Resize Details";
            ResizeDetailsGrid.Visibility = Visibility.Visible;
        }
        private void ApplyResizeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ResizeImage((BitmapImage)LoadedImage.Source, int.Parse(ResizeWidthTextBox.Text), int.Parse(ResizeHeightTextBox.Text));
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }
        private void ResizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as TextBox;
            int thresholdValue = 0;
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(thisTextBox.Text);
            if (match.Success)
            {
                thresholdValue = int.Parse(match.Value);
            }
            thisTextBox.Text = thresholdValue.ToString();
            ThresholdSlider.Value = thresholdValue;
        }

        /// <summary>
        /// Collapse all Filter Menus
        /// </summary>
        private void CollapseDetailedFilterOptionsGrid()
        {
            foreach (var child in DetailedFilterOptionsGrid.Children)
            {
                UIElement thisChild = child as UIElement;
                thisChild.Visibility=Visibility.Collapsed;
            }
        }
        /// <summary>
        /// ExitCommand
        /// </summary>
        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Open file OpenCommand
        /// </summary>
        private bool isFileOpen = false;
        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartRecordingTime();

            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                Image loadedImage = new Image();
                loadedImage.Source = new BitmapImage(new Uri(op.FileName));
                ManageSnapShotList(loadedImage);
                LoadImage(loadedImage);
            }
            isFileOpen = true;
        }

        /// <summary>
        /// Save Command
        /// Save file -> .png .bmp .jpg
        /// </summary>
        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
                e.CanExecute = isFileOpen;
        }
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sav = new SaveFileDialog();
            sav.Title = "Save file";
            sav.Filter = "Images|*.png;*.bmp;*.jpg";
            ImageFormat format = ImageFormat.Png;
            if (sav.ShowDialog() == true)
            {
                var ext = Path.GetExtension(sav.FileName);
                switch (ext)
                {
                    case ".jpg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                }
                var b = ImageProcessing.ConvertBitmapImageToBitmap((BitmapImage) LoadedImage.Source);
                b.Save(sav.FileName, format);
            }
        }
        /// <summary>
        /// Get pixel RGB values and XY to UI
        /// </summary>
        private void LoadedImage_MouseDown_GetPixel(object sender, MouseButtonEventArgs e)
        {
            Image targetImage = sender as Image;
            Point pointOfImage = e.GetPosition(targetImage);

            double pointOfImageX = (targetImage.Source.Width/LoadedImage.ActualWidth)* pointOfImage.X;
            double pointOfImageY = (targetImage.Source.Height /LoadedImage.ActualHeight) * pointOfImage.Y;

            Bitmap b = ImageProcessing.ConvertBitmapImageToBitmap((BitmapImage)targetImage.Source);

            Color color = b.GetPixel((int)pointOfImageX, (int)pointOfImageY);
            ExactPixel.Text = String.Format("Pixel: {0}x{1}", (int)pointOfImageX, (int)pointOfImageY);
            PixelRGB.Text = String.Format("Pixel RGB: {0} {1} {2}", color.R,color.G,color.B);
        }

        /// <summary>
        /// Apply custom Kernel
        /// </summary>
        private void DFactorTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as TextBox;
            double number = 0;
            if (Double.TryParse(thisTextBox.Text.Trim(), out number))
            {
                if (number < 0.1) number = 0.1;
                if (number > 99) number = 99;
                thisTextBox.Text = number.ToString();
            }
            else
            {
                thisTextBox.Text = "1";
            }
        }
        private void DOffsetOrKernelTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as TextBox;
            int number = 0;
            if (int.TryParse(thisTextBox.Text.Trim(), out number))
            {
                if (number < -255) number = -255;
                if (number > 255) number = 255;
                thisTextBox.Text = number.ToString();
            }
            else
            {
                thisTextBox.Text = "0";
            }
        }

        private void ApplyCustomKernelButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            int[,] kernel = new int[,] { { int.Parse(KernelBox00.Text), int.Parse(KernelBox01.Text), int.Parse(KernelBox02.Text) },
                { int.Parse(KernelBox10.Text), int.Parse(KernelBox11.Text), int.Parse(KernelBox12.Text)},
                { int.Parse(KernelBox20.Text), int.Parse(KernelBox21.Text), int.Parse(KernelBox22.Text)} };
            BitmapImage newmap = ImageProcessing.ApplyFilterFromMatrix((BitmapImage)LoadedImage.Source,
                kernel, int.Parse(DOffsetTextBox.Text),
                float.Parse(DFactorTextBox.Text));
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        /// <summary>
        /// Show Thresholding options
        /// </summary>
        private void ThresholdingButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseDetailedFilterOptionsGrid();

            DetailedFilterOptionsGrid.Visibility = Visibility.Visible;
            FilterDetailsBorder.Visibility = Visibility.Visible;
            FilterDetailsTextBlock.Visibility = Visibility.Visible;
            FilterDetailsOptionsBorder.Visibility = Visibility.Visible;
            ImageFiltersBorder.Visibility = Visibility.Visible;

            FilterDetailsTextBlock.Text = "Thresholding Details";
            ThresholdDetailsGrid.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Interaction between Slider and Textbox
        /// </summary>
        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreshholdTextBox.Text = e.NewValue.ToString();
        }

        private void ThreshholdTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as TextBox;
            int thresholdValue = 0;
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(thisTextBox.Text);
            if (match.Success)
            {
                thresholdValue = int.Parse(match.Value);
            }
            thisTextBox.Text = thresholdValue.ToString();
            ThresholdSlider.Value = thresholdValue;
        }

        /// <summary>
        /// Apply thresholding on button click
        /// </summary>
        private void ApplyThresholdingButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            int threshold = (int)ThresholdSlider.Value;
            bool isColor = (bool)ThreshlodingColorToggleButton.IsChecked;
            BitmapImage newmap = ImageProcessing.ApplyThresholding((BitmapImage)LoadedImage.Source, threshold, isColor);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);  
            LoadImage(newIMG);
        }

        /// <summary>
        /// Show Gray Scale options
        /// </summary>
        private void GrayScaleButton_OnClick(object sender, RoutedEventArgs e)
        {
            CollapseDetailedFilterOptionsGrid();

            DetailedFilterOptionsGrid.Visibility = Visibility.Visible;
            FilterDetailsBorder.Visibility = Visibility.Visible;
            FilterDetailsTextBlock.Visibility = Visibility.Visible;
            FilterDetailsOptionsBorder.Visibility = Visibility.Visible;
            ImageFiltersBorder.Visibility = Visibility.Visible;

            FilterDetailsTextBlock.Text = "Gray Scale Details";
            
            GrayScaleDetailsGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Show Color Adjustment options
        /// </summary>
        private void ColorAdjustmentButton_OnClick(object sender, RoutedEventArgs e)
        {
            CollapseDetailedFilterOptionsGrid();

            DetailedFilterOptionsGrid.Visibility = Visibility.Visible;
            FilterDetailsBorder.Visibility = Visibility.Visible;
            FilterDetailsTextBlock.Visibility = Visibility.Visible;
            FilterDetailsOptionsBorder.Visibility = Visibility.Visible;
            ImageFiltersBorder.Visibility = Visibility.Visible;

            FilterDetailsTextBlock.Text = "Color Adjustment";

            ColorAdjustmentGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Interaction between Slider and Textbox
        /// </summary>
        private void GrayScaleBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            GrayScaleBrightnessTextBox.Text = e.NewValue.ToString();
        }

        private void GrayScaleBrightnessTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as TextBox;
            int thresholdValue = 0;
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(thisTextBox.Text);
            if (match.Success)
            {
                thresholdValue = int.Parse(match.Value);
            }
            thisTextBox.Text = thresholdValue.ToString();
            ThresholdSlider.Value = thresholdValue;
        }
        /// <summary>
        /// Apply gray Scale on button click
        /// </summary>
        private void ApplyGrayScaleButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            double brightness = GrayScaleBrightnessSlider.Value;
            BitmapImage newmap = ImageProcessing.ApplyGrayScale((BitmapImage)LoadedImage.Source, brightness);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }


        private void ApplySharpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplySharpen((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyGaussianBlurButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplyGaussianBlur((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyEdgeEnhanceButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplyEdgeEnhance((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyEdgeDetectButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplyEdgeDetect((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyEmbossButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplyEmboss((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyMeanRemovalButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.ApplyMeanRemoval((BitmapImage)LoadedImage.Source);
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }

        private void ApplyColorAdjustmentButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartRecordingTime();

            if (LoadedImage.Source == null) return;
            BitmapImage newmap = ImageProcessing.CustomColorSettings((BitmapImage) LoadedImage.Source,
                float.Parse(BrightnessSliderTextBlock.Text), float.Parse(ContrastSliderTextBlock.Text),
                float.Parse(GammaSliderTextBlock.Text));
            Image newIMG = new Image();
            newIMG.Source = newmap;
            ManageSnapShotList(newIMG);
            LoadImage(newIMG);
        }
        
    }
    
}
