using HalconDotNet;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class ImageDenoiseViewModel : ToolBaseViewModel
    {
        // —— 高斯滤波参数 —— 
        private bool _enableGauss;
        public bool EnableGauss
        {
            get => _enableGauss;
            set
            {
                if (_enableGauss == value) return;
                _enableGauss = value;
                if (value) { EnableMedian = EnableBilateral = false; }
                OnPropertyChanged();
                Apply();
            }
        }

        private int _gaussSize = 5;
        /// <summary>滤波核大小，默认为 5，可在 XAML 里调整上下限</summary>
        public int GaussSize
        {
            get => _gaussSize;
            set
            {
                if (_gaussSize == value) return;
                _gaussSize = value;
                OnPropertyChanged();
                if (EnableGauss) Apply();
            }
        }

        // —— 中值滤波参数 —— 
        private bool _enableMedian = true;
        public bool EnableMedian
        {
            get => _enableMedian;
            set
            {
                if (_enableMedian == value) return;
                _enableMedian = value;
                if (value) { EnableGauss = EnableBilateral = false; }
                OnPropertyChanged();
                Apply();
            }
        }

        public ObservableCollection<string> MaskOptions { get; } =
        [
            "circle","square"
        ];

        public ObservableCollection<MarginOption> MarginOptions { get; } =
        new ObservableCollection<MarginOption>
        {
                new MarginOption("mirrored",    new HTuple("mirrored")),
                new MarginOption("cyclic",      new HTuple("cyclic")),
                new MarginOption("continued",   new HTuple("continued")),
                new MarginOption("0",           new HTuple(0)),
                new MarginOption("30",          new HTuple(30)),
                new MarginOption("60",          new HTuple(60)),
                new MarginOption("90",          new HTuple(90)),
                new MarginOption("120",         new HTuple(120)),
                new MarginOption("150",         new HTuple(150)),
                new MarginOption("180",         new HTuple(180)),
                new MarginOption("210",         new HTuple(210)),
                new MarginOption("240",         new HTuple(240)),
                new MarginOption("255",         new HTuple(255)),
        };

        private string _medianMaskType = "circle";
        public string MedianMaskType
        {
            get => _medianMaskType;
            set
            {
                if (_medianMaskType == value) return;
                _medianMaskType = value;
                OnPropertyChanged(nameof(MedianMaskType));
                if (EnableMedian) Apply();
            }
        }

        private int _medianRadius = 1;
        public int MedianRadius
        {
            get => _medianRadius;
            set
            {
                if (_medianRadius == value) return;
                _medianRadius = value;
                OnPropertyChanged();
                if (EnableMedian) Apply();
            }
        }

        private MarginOption _selectedMargin = new MarginOption("mirrored", new HTuple("mirrored"));
        public MarginOption SelectedMargin
        {
            get => _selectedMargin;
            set
            {
                if (_selectedMargin == value) return;
                _selectedMargin = value;
                OnPropertyChanged();
                if (EnableMedian) Apply();
            }
        }

        // —— 双边滤波参数 —— 
        private bool _enableBilateral;
        public bool EnableBilateral
        {
            get => _enableBilateral;
            set
            {
                if (_enableBilateral == value) return;
                _enableBilateral = value;
                if (value) { EnableGauss = EnableMedian = false; }
                OnPropertyChanged();
                Apply();
            }
        }

        private double _sigmaSpatial = 3.0;
        public double SigmaSpatial
        {
            get => _sigmaSpatial;
            set
            {
                _sigmaSpatial = value;
                OnPropertyChanged();
                if (EnableBilateral) Apply();
            }
        }

        private double _sigmaRange = 20.0;
        public double SigmaRange
        {
            get => _sigmaRange;
            set
            {
                _sigmaRange = value;
                OnPropertyChanged();
                if (EnableBilateral) Apply();
            }
        }

        // 绑定在 UI 上的选项列表（可选）
        public ObservableCollection<string> MaskTypes { get; } =
            new ObservableCollection<string>(new[] { "circle", "rect1", "rect2" });

        public override HObject CurrentResultImage => _resultImage;

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized()) return;
            HObject tmp = null;
       
            if (EnableGauss)
            {
                HOperatorSet.GaussFilter(_inputImage, out tmp, new HTuple(GaussSize));
            }
            else if (EnableMedian)
            {
                HOperatorSet.MedianImage(_inputImage, out tmp,
                    new HTuple(MedianMaskType),
                    new HTuple(MedianRadius),
                    SelectedMargin.Value);
            }
            else if (EnableBilateral)
            {
                // 双边滤波中 joint image 一般使用原图自身
                HOperatorSet.BilateralFilter(_inputImage, _inputImage, out tmp,
                    new HTuple(SigmaSpatial),
                    new HTuple(SigmaRange),
                    new HTuple(), // genParamName 默认空
                    new HTuple());
            }
            else
            {
                // 没选任何则直接 passthrough
                tmp = _inputImage;
            }

            // 显示
            _resultImage = tmp;
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
        }
    }

    public class MarginOption
    {
        public string Display { get; }
        public HTuple Value { get; }

        public MarginOption(string display, HTuple value)
        {
            Display = display;
            Value = value;
        }

        public override string ToString() => Display;
    }
}
