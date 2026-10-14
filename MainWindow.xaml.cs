using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Resizer.CoreUtils;
using System.ComponentModel;

namespace Resizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ResizerTool resizer = new ResizerTool();   
        public Bitmap currentMainImage;
        public Bitmap resultImage;



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
            MainImageContent.Source = ToBitmapImage(currentMainImage);
        }
        public void ChangeResultImage()
        {
            ResultImageContent.Source = ToBitmapImage(resultImage);
        }

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                currentMainImage = new Bitmap("Images/Placeholder/ImagePlaceholder.png");
                resultImage = new Bitmap("Images/Placeholder/ImagePlaceholder.png");
                ChangeMainImage();
                ChangeResultImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            resizer.worker_Complete += OnWorkerComplete;
        }

        private void OnWorkerComplete(Bitmap result)
        {
            
            resultImage = new Bitmap(result);
            result.Dispose();
            ChangeResultImage();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dial = new OpenFileDialog();


            try
            {
                dial.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif|All Files(*.*)|*.*";
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
            finally
            {
                dial = null;
            }
        }

        private void ScaleStart_Click(object sender, RoutedEventArgs e)
        {
            resizer.ResizeImage(currentMainImage, new OpenCvSharp.Size(300, 300));

        }
    }
}