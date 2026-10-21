using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.XPhoto;


namespace Resizer.CoreUtils
{
    public class ResizerTool//: INotifyPropertyChanged
    {
        private BackgroundWorker _bgWorker;
        //private int _workerState;

        //public int WorkerState
        //{
        //    get { return _workerState; }
        //    set
        //    {
        //        _workerState = value;
        //        if (PropertyChanged != null)
        //            PropertyChanged(this, new PropertyChangedEventArgs("WorkerState"));
        //    }
        //}
        public delegate void WorkerComplete(Bitmap resultImage);
        public event WorkerComplete worker_Complete;

        public enum ResizeInterpolation
        {
            Nearest = (int)InterpolationFlags.Nearest,
            Linear = (int)InterpolationFlags.Linear,
            Cubic = (int)InterpolationFlags.Cubic,
            Area = (int)InterpolationFlags.Area,
            Lanczos4 = (int)InterpolationFlags.Lanczos4,
            LinearExact = (int)InterpolationFlags.LinearExact,

            // Собственный алгоритм, а не флаг OpenCV.
            ScaleAwareLanczos3 = 1000
        }

        public struct ResizeSettings
        {
            public OpenCvSharp.Size ImgSize = new OpenCvSharp.Size(100,100);
            public ResizeInterpolation Method = ResizeInterpolation.Linear;
            public double fx = 0;
            public double fy = 0;

            public ResizeSettings(OpenCvSharp.Size imgSize, ResizeInterpolation method, double _fx, double _fy)
            {
                ImgSize = imgSize;
                Method = method;
                fx = _fx; fy = _fy;
            }

        };


        public ResizerTool()
        {
            
            Debug.WriteLine("Resize tool initialized!");
        }


        public void ResizeImage(Bitmap mainImg, ResizeSettings Settings)
        {
            _bgWorker = new BackgroundWorker();
            Mat img = BitmapConverter.ToMat(mainImg);

            Mat res = new Mat();

            _bgWorker.DoWork += (s, e) =>
            {

                try
                {
                    if (Settings.Method == ResizeInterpolation.ScaleAwareLanczos3)
                    {
                        ScaleAwareLanczos3.Resize(img, Settings.ImgSize.Width, Settings.ImgSize.Height).CopyTo(res);
                    }
                    else
                    {
                        res = img.Resize(Settings.ImgSize, Settings.fx, Settings.fy, (InterpolationFlags)Settings.Method);
                    }
                }
                catch (OpenCVException ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}\n Интерполяция будет проведена в режиме Linear.", "Ошибка интреполяции!", MessageBoxButton.OK, MessageBoxImage.Error);
                    res = img.Resize(Settings.ImgSize, Settings.fx, Settings.fy, InterpolationFlags.Linear);
                }
                
                worker_Complete?.Invoke(res.ToBitmap());
                _bgWorker.Dispose();
            };
            _bgWorker.RunWorkerAsync();
        }

        //#region INotifyPropertyChanged Member

        //public event PropertyChangedEventHandler PropertyChanged;

        //#endregion
    }
    
    public static unsafe class ScaleAwareLanczos3
    {
        private const double Radius = 2.0;

        private sealed class AxisWeights
        {
            public int[][] Indices { get; }
            public float[][] Weights { get; }

            public AxisWeights(int size)
            {
                Indices = new int[size][];
                Weights = new float[size][];
            }
        }

        // Синус-кардинальная функция sinc(x).
        private static double Sinc(double x)
        {
            if (Math.Abs(x) < 1e-12)
                return 1.0;

            double px = Math.PI * x;
            return Math.Sin(px) / px;
        }

        // Оконная функция Lanczos-3.
        private static double Kernel(double x)
        {
            x = Math.Abs(x);

            if (x >= Radius)
                return 0.0;

            return Sinc(x) * Sinc(x / Radius);
        }

        // Предварительный расчёт вкладов исходных пикселей
        // для каждой координаты результирующего изображения.
        private static AxisWeights BuildWeights(
            int sourceSize,
            int destinationSize)
        {
            var result = new AxisWeights(destinationSize);

            double scale = (double)sourceSize / destinationSize;
            double support = Radius * scale;

            for (int d = 0; d < destinationSize; d++)
            {
                // Центр результирующего пикселя в координатах исходника.
                double center = (d + 0.5) * scale - 0.5;

                int first = Math.Max(
                    0, (int)Math.Ceiling(center - support));

                int last = Math.Min(
                    sourceSize - 1,
                    (int)Math.Floor(center + support));

                int count = last - first + 1;

                var indices = new int[count];
                var weights = new float[count];

                double sum = 0.0;

                for (int k = 0; k < count; k++)
                {
                    int s = first + k;

                    // Масштабно-зависимое ядро:
                    // при уменьшении в N раз ширина ядра растёт в N раз.
                    double w = Kernel((center - s) / scale);

                    indices[k] = s;
                    weights[k] = (float)w;
                    sum += w;
                }

                // Страховка от вырожденной суммы весов.
                if (Math.Abs(sum) < 1e-12)
                {
                    int nearest = Math.Clamp(
                        (int)Math.Round(center), 0, sourceSize - 1);

                    result.Indices[d] = new[] { nearest };
                    result.Weights[d] = new[] { 1.0f };
                    continue;
                }

                // Нормализация особенно важна у границ изображения.
                for (int k = 0; k < count; k++)
                    weights[k] = (float)(weights[k] / sum);

                result.Indices[d] = indices;
                result.Weights[d] = weights;
            }

            return result;
        }

        private static byte ToByte(float value)
        {
            int rounded = (int)Math.Round(value);
            return (byte)Math.Clamp(rounded, 0, 255);
        }

        public static Mat Resize(Mat src, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(src);

            if (src.Empty())
                throw new ArgumentException("Исходное изображение пустое.");

            MatType type = src.Type();

            if (type != MatType.CV_8UC1 &&
                type != MatType.CV_8UC3 &&
                type != MatType.CV_8UC4)
            {
                throw new ArgumentException(
                    "Поддерживаются CV_8UC1, CV_8UC3 и CV_8UC4.");
            }

            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(width), "Размеры должны быть положительными.");

            if (width > src.Cols || height > src.Rows)
                throw new ArgumentException(
                    "Этот алгоритм предназначен для уменьшения изображения.");

            int channels = type.Channels;

            AxisWeights horizontal = BuildWeights(src.Cols, width);
            AxisWeights vertical = BuildWeights(src.Rows, height);

            // Промежуточное изображение после горизонтального прохода.
            // Float позволяет избежать промежуточного округления до byte.
            int tempLength = checked(src.Rows * width * channels);
            var temp = new float[tempLength];

            // Проход 1: горизонтальная фильтрация.
            int srcRows = src.Rows;
            for (int y = 0; y < srcRows; y++)
            {
                byte* srcRow = (byte*)src.Ptr(y);

                for (int x = 0; x < width; x++)
                {
                    int tempOffset = (y * width + x) * channels;

                    int[] indices = horizontal.Indices[x];
                    float[] weights = horizontal.Weights[x];

                    for (int k = 0; k < indices.Length; k++)
                    {
                        int srcOffset = indices[k] * channels;
                        float weight = weights[k];

                        float alpha = channels == 4
                            ? srcRow[srcOffset + 3] / 255.0f
                            : 1.0f;

                        for (int c = 0; c < channels; c++)
                        {
                            float sample = srcRow[srcOffset + c];

                            // Для BGRA фильтруем цвет в premultiplied
                            // alpha, чтобы прозрачные RGB не загрязняли цвет.
                            if (channels == 4 && c < 3)
                                sample *= alpha;

                            temp[tempOffset + c] += sample * weight;
                        }
                    }
                }
            }

            var dst = new Mat(height, width, type);

            // Проход 2: вертикальная фильтрация и финальное округление.
            for (int y = 0; y < height; y++)
            {
                byte* dstRow = (byte*)dst.Ptr(y);

                int[] indices = vertical.Indices[y];
                float[] weights = vertical.Weights[y];

                for (int x = 0; x < width; x++)
                {
                    int dstOffset = x * channels;
                    float alpha = 255.0f;

                    if (channels == 4)
                    {
                        float alphaSum = 0.0f;

                        for (int k = 0; k < indices.Length; k++)
                        {
                            int offset =
                                (indices[k] * width + x) * channels + 3;

                            alphaSum += temp[offset] * weights[k];
                        }

                        alpha = Math.Clamp(alphaSum, 0.0f, 255.0f);
                    }

                    for (int c = 0; c < channels; c++)
                    {
                        float sum;

                        if (channels == 4 && c == 3)
                        {
                            sum = alpha;
                        }
                        else
                        {
                            sum = 0.0f;

                            for (int k = 0; k < indices.Length; k++)
                            {
                                int offset =
                                    (indices[k] * width + x) * channels + c;

                                sum += temp[offset] * weights[k];
                            }

                            // Возвращаем обычные RGB-значения из
                            // premultiplied alpha.
                            if (channels == 4)
                            {
                                sum = alpha > 1e-6f
                                    ? sum * 255.0f / alpha
                                    : 0.0f;
                            }
                        }

                        dstRow[dstOffset + c] = ToByte(sum);
                    }
                }
            }

            return dst;
        }
    }


}
