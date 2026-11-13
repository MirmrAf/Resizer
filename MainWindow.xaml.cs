using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using Resizer.CoreUtils;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


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
        public ResizerTool.SizeMode _sizeMode = ResizerTool.SizeMode.PerPixel;
        public ResizerTool.ResizeSettings resizeSettings;


        private void ChangeMainImage()
        {
            Bitmap img;
            if (currentMainImage == null)
            {
                img = new(placeholder);
                currentMainImage = img;
            }
            else img = currentMainImage;
            MainImageContent.Source = ConverterHelper.ToBitmapImage(img);
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
                    ResultImageContent.Source = ConverterHelper.ToBitmapImage(img);
                    ResultImage.Reset();
                }));

                resultImageSize = $"{img.Size.Width}x{img.Size.Height}";
                resultSizeText.Dispatcher.Invoke(new Action(() => resultSizeText.Text = resultImageSize));
            }
            else
            {
                img = resultImage;

                Dispatcher.Invoke(new Action(() => {
                    ResultImageContent.Source = ConverterHelper.ToBitmapImage(img);
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

            //MainImage.PreviewKeyDown += KeyDownPicture1;
            //MainImageContent.GotKeyboardFocus += (s,e) => { Debug.WriteLine($"new {e.NewFocus}, old {e.OldFocus}"); };
            MainImage.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, PasteImage));
            resizer.worker_Complete += OnWorkerComplete;
            InterpolationMethodSelect.Items.Clear();
            InterpolationMethodSelect.ItemsSource = Enum.GetValues<ResizerTool.ResizeInterpolation>();
            InterpolationMethodSelect.SelectedIndex = 1;

            //for (int i = 0; i<Enum.GetNames(typeof(InterpolationFlags)).Length; i++)
            //{

            //}

            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(100);
            //        Dispatcher.Invoke(new Action(() =>
            //        {
            //            Debug.WriteLine(Keyboard.FocusedElement);

            //        }));

            //    }
            //});
        }

        private void MainImage_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //foreach (string file in files) Debug.WriteLine(file);
            string file = files.Last();
            Debug.WriteLine(file);

            if (file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg") || 
                file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith(".tif") || 
                file.ToLower().EndsWith(".ico") || file.ToLower().EndsWith(".jfif")) //*.jpg;*.jpeg;*.png;*.tif;*.ico
            {
                currentMainImage = new Bitmap(file);
                ChangeMainImage();
            }
        }

        private void MainImage_DragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("DragEnter!");
            e.Effects = DragDropEffects.Copy;
        }


        private void PasteImage(object sender, ExecutedRoutedEventArgs e)
        {


            BitmapSource img = Clipboard.GetImage();
            if (img != null)
            {
                currentMainImage = new Bitmap(ConverterHelper.BitmapFromSource(img));
                ChangeMainImage();
            }

        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) 
            {
                ScaleStartFunc();
            }
        }

        private void OnWorkerComplete(Bitmap result)
        {
            resultImage = new Bitmap(result);
            result.Dispose();
            ChangeResultImage();
            MessageBox.Show("ResizeCompleted!", "Success", MessageBoxButton.OK);
        }

        private void ScaleStartFunc()
        {
            MessageBoxResult msg = MessageBox.Show("Are You sure to start?", "Sure?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (msg == MessageBoxResult.Yes)
            {
                resizer.ResizeImage(currentMainImage, resizeSettings);
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dial;


            try
            {
                dial = new OpenFileDialog();
                dial.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.tif;*.ico;*.jfif|All Files(*.*)|*.*";
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
            ScaleStartFunc();
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

        private void PercentSwitch_Click(object sender, RoutedEventArgs e)
        {
            switch (_sizeMode)
            {
                case ResizerTool.SizeMode.PerPixel:
                    PercentSwitch.Content = "px";
                    PercentSwitch.ToolTip = "In percent mode now";
                    _sizeMode = ResizerTool.SizeMode.PerPercent;
                    break;
                case ResizerTool.SizeMode.PerPercent:
                    PercentSwitch.Content = "%";
                    PercentSwitch.ToolTip = "In pixel mode now";
                    _sizeMode = ResizerTool.SizeMode.PerPixel;
                    break;
            }
            resizeSettings.SM = _sizeMode;
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