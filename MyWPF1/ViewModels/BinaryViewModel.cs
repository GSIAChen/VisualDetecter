using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class BinaryViewModel : ToolBaseViewModel
    {
        private HObject _region;
        public override HObject CurrentResultImage => _resultImage;

        private int global_threshold = 128;
        public int GlobalThreshold
        {
            get => global_threshold;
            set
            {
                if (global_threshold == value) return;
                global_threshold = value;
                OnPropertyChanged(nameof(GlobalThreshold));
                Apply();
            }
        }
    
        public override void Apply()
        {
            // 全局阈值分割
            if (_inputImage == null || !_inputImage.IsInitialized()) return;

            HOperatorSet.CountChannels(_inputImage, out HTuple channels);
            bool isGray = (channels.I == 1);
            if (!isGray)
            {
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);
                HOperatorSet.Rgb3ToGray(r, g, b, out HObject grayImage);
                _inputImage = grayImage;
                r.Dispose(); g.Dispose(); b.Dispose();
            }
            HOperatorSet.Threshold(_inputImage, out _region, global_threshold, 255);
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

