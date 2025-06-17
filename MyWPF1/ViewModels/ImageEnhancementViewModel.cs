using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class ImageEnhancementViewModel : ToolBaseViewModel
    {
        public override HObject CurrentResultImage => _resultImage;
        private bool _enableGamma = true;
        public bool EnableGamma
        {
            get => _enableGamma;
            set { if (_enableGamma != value) { _enableGamma = value; OnPropertyChanged(); Apply(); } }
        }

        private double _gamma = 0.41;
        public double Gamma
        {
            get => _gamma;
            set { _gamma = value; OnPropertyChanged(); Apply(); }
        }

        private double _offset = 0.055;
        public double Offset
        {
            get => _offset;
            set { _offset = value; OnPropertyChanged(); Apply(); }
        }

        private double _threshold = 0.0031;
        public double Threshold
        {
            get => _threshold;
            set { _threshold = value; OnPropertyChanged(); Apply(); }
        }

        private bool _encode = true;
        public bool Encode
        {
            get => _encode;
            set { _encode = value; OnPropertyChanged(); Apply(); }
        }

        // —— Emphasize 参数 —— 
        private bool _enableEmphasize = true;
        public bool EnableEmphasize
        {
            get => _enableEmphasize;
            set { if (_enableEmphasize != value) { _enableEmphasize = value; OnPropertyChanged(); Apply(); } }
        }

        private double _maskSize = 7;
        public double MaskSize
        {
            get => _maskSize;
            set { if (_maskSize != value) { _maskSize = value; OnPropertyChanged(); Apply(); } }
        }

        private double _factor = 1.0;
        public double Factor
        {
            get => _factor;
            set { if (_factor != value) { _factor = value; OnPropertyChanged(); Apply(); } }
        }

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized())
                return;
            if (!EnableGamma && !EnableEmphasize)
            {
                _resultImage = _inputImage;
                _hWindowControl.HalconWindow.ClearWindow();
                _hWindowControl.HalconWindow.DispObj(_resultImage);
                OnPropertyChanged(nameof(CurrentResultImage));
                return;
            }

            // 1. 先 Gamma
            HObject img = _inputImage;
            if (EnableGamma)
            {
                HOperatorSet.GammaImage(
                    img, out HObject tmp,
                    new HTuple(Gamma),
                    new HTuple(Offset),
                    new HTuple(Threshold),
                    new HTuple(255),
                    new HTuple(Encode ? "true" : "false"));
                img = tmp;
            }

            // 2. 再 Emphasize
            if (EnableEmphasize)
            {
                HOperatorSet.Emphasize(
                    img, out HObject tmp2,
                    new HTuple(MaskSize), new HTuple(MaskSize), new HTuple(Factor));
                img = tmp2;
            }

            // 3. 输出结果
            _resultImage = img;
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
            OnPropertyChanged(nameof(CurrentResultImage));
        }
    }
}
