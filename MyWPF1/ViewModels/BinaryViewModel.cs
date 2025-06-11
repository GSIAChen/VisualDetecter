using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class BinaryViewModel : ToolBaseViewModel
    {
        private int _threshold = 128;
        private HObject _region;
        public override HObject CurrentResultImage => _resultImage;

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
    
        public override void Apply()
        {
            // 全局阈值分割
            HOperatorSet.CountChannels(_inputImage, out HTuple channels);
            bool isGray = (channels.I == 1);
            if (!isGray)
            {
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);
                HOperatorSet.Rgb3ToGray(r, g, b, out HObject grayImage);
                _inputImage = grayImage;
                r.Dispose(); g.Dispose(); b.Dispose();
            }
            HOperatorSet.Threshold(_inputImage, out _region, _threshold, 255);
            HOperatorSet.PaintRegion(_region,
                         _inputImage,
                         out _resultImage,
                         255,
                         "fill");
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
        }
    }
}

