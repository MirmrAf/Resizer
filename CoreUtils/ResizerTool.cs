using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.XPhoto;


namespace Resizer.CoreUtils
{
    public class ResizerTool: INotifyPropertyChanged
    {
        private WorkerStateWindow workerStateWindow;
        private BackgroundWorker _bgWorker = new BackgroundWorker();
        private int _workerState;

        public delegate void WorkerComplete(Bitmap resultImage);
        public event WorkerComplete worker_Complete;

        public int WorkerState
        {
            get { return _workerState; }
            set
            {
                _workerState = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("WorkerState"));
            }
        }

        public void MakeWorkerWindow(Binding workerBind)
        {
            if (workerStateWindow == null)
            {
                workerStateWindow = new WorkerStateWindow();
                workerStateWindow.GetProgressBar().SetBinding(ProgressBar.ValueProperty, new Binding("Progress")
                {
                    Source = WorkerState,
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });
            }
        }
        public void CloseWorkerWindow()
        {
            if (workerStateWindow != null)
            {
                workerStateWindow.Close();
                workerStateWindow.Closed += (s, e) => {
                    s = null;
                };
            }
            
        }

        public ResizerTool()
        {
            Debug.WriteLine("Resize tool initialized!");
        }


        public void ResizeImage(Bitmap mainImg, OpenCvSharp.Size size)
        {

            Mat img = BitmapConverter.ToMat(mainImg);

            Mat res = new Mat();

            _bgWorker.DoWork += (s, e) =>
            {
                res = img.Resize(size, 0, 0, InterpolationFlags.Linear);
                worker_Complete.Invoke(res.ToBitmap());
            };

            _bgWorker.RunWorkerAsync();
        }

        #region INotifyPropertyChanged Member

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
