using Microsoft.Win32;
using NetSparkleUpdater


using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Resizer.CoreUtils;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit.Core;
using Xceed.Wpf.Toolkit.Primitives;

namespace Resizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        readonly private Bitmap placeholder = new("Images/Placeholder/ImagePlaceholder.png");
        public ResizerTool resizer = new ResizerTool();   
        public Bitmap currentMainImage;
        public Bitmap resultImage;

        public string mainImageSize = "";
        public string resultImageSize = "";

        public ResizerTool.ResizeSettings resizeSettings;


        private SparkleUpdater _sparkle;

        public BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void ChangeMainImage()
        {
            Bitmap img;
            if (currentMainImage == null)
            {
                img = new(placeholder);
                currentMainImage = img;
            }
            else img = currentMainImage;
            MainImageContent.Source = ToBitmapImage(img);
            MainImage.Reset();
            mainImageSize = $"{img.Size.Width}x{img.Size.Height}";
            inputSizeText.Text = mainImageSize;
            GC.Collect();
        }
        public void ChangeResultImage()
        {
            Bitmap img;
            if (resultImage == null)
            {
                img = new(placeholder);
                resultImage = img;

                Dispatcher.Invoke(new Action(() => {
                    ResultImageContent.Source = ToBitmapImage(img);
                    ResultImage.Reset();
                }));

                resultImageSize = $"{img.Size.Width}x{img.Size.Height}";
                resultSizeText.Dispatcher.Invoke(new Action(() => resultSizeText.Text = resultImageSize));
            }
            else
            {
                img = resultImage;

                Dispatcher.Invoke(new Action(() => {
                    ResultImageContent.Source = ToBitmapImage(img);
                    ResultImage.Reset();
                }));

                resultImageSize = $"{img.Size.Width}x{img.Size.Height}";
                resultSizeText.Dispatcher.Invoke(new Action(() => resultSizeText.Text = resultImageSize));

            }

            

        }

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                //currentMainImage = new Bitmap("Images/Placeholder/ImagePlaceholder.png");
                //resultImage = new Bitmap("Images/Placeholder/ImagePlaceholder.png");
                ChangeMainImage();
                ChangeResultImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            resizer.worker_Complete += OnWorkerComplete;
            InterpolationMethodSelect.Items.Clear();
            InterpolationMethodSelect.ItemsSource = Enum.GetValues<ResizerTool.ResizeInterpolation>();

            InterpolationMethodSelect.SelectedIndex = 1;
            //for (int i = 0; i<Enum.GetNames(typeof(InterpolationFlags)).Length; i++)
            //{

            //}


            // on your main thread...
            _sparkle = new SparkleUpdater(
                "https://mywebsite.com/appcast.xml", // link to your app cast file - change extension to .json if using json
                new Ed25519Checker(SecurityMode.Strict, // security mode -- use .Unsafe to ignore all signature checking (NOT recommended!!)
                           "Nam3y/KR3PCKZKX7+vLgWdFPeMd/yvkTM6Yl3uOV7Hc=") // your base 64 public key
            )
            {
                UIFactory = new NetSparkleUpdater.UI.WPF.UIFactory(this.Icon), // or null, or choose some other UI factory, or build your own IUIFactory implementation!
                RelaunchAfterUpdate = false, // set to true if needed
            };
            _sparkle.StartLoop(true); // will auto-check for updates


        }

        private void OnWorkerComplete(Bitmap result)
        {
            resultImage = new Bitmap(result);
            result.Dispose();
            ChangeResultImage();
            MessageBox.Show("ResizeCompleted!", "Success", MessageBoxButton.OK);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dial;


            try
            {
                dial = new OpenFileDialog();
                dial.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif;*.ico|All Files(*.*)|*.*";
                if (dial.ShowDialog() == true)
                {
                    string filepath = dial.FileName;
                    currentMainImage = new Bitmap(filepath);
                }
                ChangeMainImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка!",MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dial;


            try
            {
                dial = new SaveFileDialog();
                dial.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif|All Files(*.*)|*.*";
                if (dial.ShowDialog() == true)
                {
                    Cv2.ImWrite(dial.FileName, resultImage.ToMat());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void StraightSaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dial;


            try
            {
                dial = new SaveFileDialog();
                dial.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif|All Files(*.*)|*.*";
                if (dial.ShowDialog() == true)
                {
                    Cv2.ImWrite(dial.FileName, currentMainImage.ToMat());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScaleStart_Click(object sender, RoutedEventArgs e)
        {
            resizer.ResizeImage(currentMainImage, resizeSettings);

        }

        private void InterpolationMethodSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            resizeSettings.Method = (ResizerTool.ResizeInterpolation)InterpolationMethodSelect.SelectedItem;
        }

        private void SizeX_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            resizeSettings.ImgSize = new OpenCvSharp.Size((int)e.NewValue, resizeSettings.ImgSize.Height);
        }
        private void SizeY_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            resizeSettings.ImgSize = new OpenCvSharp.Size(resizeSettings.ImgSize.Width, (int)e.NewValue);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }
        private void ResetMainMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainImage.Reset();
            currentMainImage = null;
            ChangeMainImage();
        }
        private void ResetResMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ResultImage.Reset();
            resultImage = null;
            ChangeResultImage();
        }

        //private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    // Use SelectionStart property to find the caret position.
        //    // Insert the previewed text into the existing text in the textbox.
        //    var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

        //    int val;
        //    // If parsing is successful, set Handled to false
        //    e.Handled = !int.TryParse(fullText,
        //                                 NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
        //                                 CultureInfo.InvariantCulture,
        //                                 out val);
        //}
    }
}