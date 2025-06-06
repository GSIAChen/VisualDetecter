using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class BinaryViewModel : ToolBaseViewModel
    {
        private int _threshold = 128;
        private HObject _image;
        private HObject _region;
        private HWindowControl _hWindowControl;
        public int Threshold
        {
            get => _threshold;
            set
            {
                if (_threshold == value) return;
                _threshold = value;
                OnPropertyChanged(nameof(Threshold));
                Apply();
            }
        }

        public override HObject CurrentResultImage => throw new NotImplementedException();

        public override void Initialize(HWindowControl hwin, HObject baseImage)
        {
            Debug.WriteLine("BinaryViewModel Initialized");
            _hWindowControl = hwin;
            _image = baseImage;
        }
    
        public override void Apply()
        {
            // 全局阈值分割
            Debug.WriteLine($"Applying Binary Tool with Threshold: {_threshold}");
            HOperatorSet.Threshold(_image, out _region, _threshold, 255);
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_image);
            _hWindowControl.HalconWindow.SetColor("green");
            _hWindowControl.HalconWindow.DispObj(_region);
        }
    }
}

