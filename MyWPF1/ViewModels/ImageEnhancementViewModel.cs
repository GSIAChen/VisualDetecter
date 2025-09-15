using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class ImageEnhancementViewModel : ToolBaseViewModel
    {
        public override HObject CurrentResultImage => _resultImage;

        private bool _enableScale = true;
        public bool EnableScale
        {
            get => _enableScale;
            set { if (_enableScale != value) { _enableScale = value; OnPropertyChanged(); Apply(); } }
        }

        private double _scaleMult = 0.01;
        public double ScaleMult
        {
            get => _scaleMult;
            set
            {
                if (_scaleMult == value) return;
                _scaleMult = value;
                OnPropertyChanged(nameof(ScaleMult));
                Apply();
            }
        }

        private double _scaleAdd = 0.0;
        public double ScaleAdd
        {
            get => _scaleAdd;
            set
            {
                if (_scaleAdd == value) return;
                _scaleAdd = value;
                OnPropertyChanged(nameof(ScaleAdd));
                Apply();
            }
        }

        private bool _enableGamma = false;
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
        private bool _enableEmphasize = false;
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

            HObject img = _inputImage;
            if (EnableScale)
            {
                HOperatorSet.ScaleImage(
                    _inputImage, out HObject tmp,
                    new HTuple(ScaleMult), new HTuple(ScaleAdd));
                img = tmp;
            }

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
