using HalconDotNet;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class BinaryViewModel : ToolBaseViewModel
    {
        private HObject _region;
        public override HObject CurrentResultImage => _resultImage;
        public ObservableCollection<string> ColorOptions { get; } =
        [
            "dark","light"
        ];
        private int _globalThreshold = 128;
        public int GlobalThreshold
        {
            get => _globalThreshold;
            set
            {
                if (_globalThreshold == value) return;
                _globalThreshold = value;
                OnPropertyChanged(nameof(GlobalThreshold));
                Apply();
            }
        }

        private bool _enableGlobalThreshold = false;
        public bool EnableGlobalThreshold
        {
            get => _enableGlobalThreshold;
            set
            {
                if (_enableGlobalThreshold == value) return;
                _enableGlobalThreshold = value;

                // 如果选择了全局，则取消局部
                if (value) EnableLocalThreshold = false;

                OnPropertyChanged(nameof(EnableGlobalThreshold));
                Apply();
            }
        }

        private string _lightdark = "dark";
        public string LightDark
        {
            get => _lightdark;
            set
            {
                if (_lightdark == value) return;
                _lightdark = value;
                OnPropertyChanged(nameof(LightDark));
                Apply();
            }
        }

        private int _maskSize = 15;
        public int MaskSize
        {
            get => _maskSize;
            set
            {
                if (_maskSize == value) return;
                _maskSize = value;
                OnPropertyChanged(nameof(MaskSize));
                Apply();
            }
        }

        private double _scale = 0.2;
        public double Scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;
                _scale = value;
                OnPropertyChanged(nameof(Scale));
                Apply();
            }
        }

        private bool _enableLocalThreshold = true;
        public bool EnableLocalThreshold
        {
            get => _enableLocalThreshold;
            set
            {
                if (_enableLocalThreshold == value) return;
                _enableLocalThreshold = value;

                // 如果选择了局部，则取消全局
                if (value) EnableGlobalThreshold = false;

                OnPropertyChanged(nameof(EnableLocalThreshold));
                Apply();
            }
        }

        public override void Apply()
        {
            // 全局阈值分割
            if (_inputImage == null || !_inputImage.IsInitialized()) return;

            Debug.WriteLine($"Global Threshold: {_globalThreshold}, Light/Dark: {LightDark}");
            HOperatorSet.CountChannels(_inputImage, out HTuple channels);
            bool isGray = (channels.I == 1);
            if (!isGray)
            {
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);
                HOperatorSet.Rgb3ToGray(r, g, b, out HObject grayImage);
                _inputImage = grayImage;
                r.Dispose(); g.Dispose(); b.Dispose();
            }
            if (EnableGlobalThreshold)
            {
                HOperatorSet.Threshold(_inputImage, out _region, _globalThreshold, 255);
                HOperatorSet.PaintRegion(_region,
                             _inputImage,
                             out _resultImage,
                             255,
                             "fill");

                OnPropertyChanged(nameof(EnableGlobalThreshold));
            }
            if (EnableLocalThreshold)
            {
                HTuple min, max, range;
                HOperatorSet.GetImageSize(_inputImage, out HTuple width, out HTuple height);
                HOperatorSet.GenRectangle1(out _region, 0, 0, height - 1, width - 1);
                HOperatorSet.MinMaxGray(_region, _inputImage, new HTuple(0), out min, out max, out range);
                HOperatorSet.LocalThreshold(_inputImage, out HObject localRegion, new HTuple("adapted_std_deviation"),
                    new HTuple(LightDark),
                    new HTuple(new string[] { "mask_size", "scale", "range" }),
                    new HTuple(new HTuple[] { new(MaskSize), new(Scale), range}));
                HOperatorSet.PaintRegion(localRegion,
                             _inputImage,
                             out _resultImage,
                             255,
                             "fill");
                OnPropertyChanged(nameof(EnableLocalThreshold));
            }
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
        }
    }
}

