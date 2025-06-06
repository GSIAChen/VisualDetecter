using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class ColorTransformViewModel : ToolBaseViewModel
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
            Debug.WriteLine("ColorTransformViewModel Initialized");
            _hWindowControl = hwin;
            _image = baseImage;
        }

        public override void Apply()
        {

        }
    }
}

