using HalconDotNet;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWPF1.ViewModels
{
    public class ColorTransformViewModel : ToolBaseViewModel
    {
        private bool _red = true, _green = true, _blue = true;
        private bool _hue, _saturation, _value;
        public override HObject CurrentResultImage => _resultImage;

        public bool RedChannel
        {
            get => _red;
            set
            {
                if (_red == value) return;
                _red = value;
                if (value) ClearHSV();
                OnPropertyChanged();
                Apply();
            }
        }

        public bool GreenChannel
        {
            get => _green;
            set
            {
                if (_green == value) return;
                _green = value;
                if (value) ClearHSV();
                OnPropertyChanged();
                Apply();
            }
        }

        public bool BlueChannel
        {
            get => _blue;
            set
            {
                if (_blue == value) return;
                _blue = value;
                if (value) ClearHSV();
                OnPropertyChanged();
                Apply();
            }
        }

        public bool HueChannel
        {
            get => _hue;
            set
            {
                if (_hue == value) return;
                _hue = value;
                if (value) ClearRGB();
                OnPropertyChanged();
                Apply();
            }
        }

        public bool SaturationChannel
        {
            get => _saturation;
            set
            {
                if (_saturation == value) return;
                _saturation = value;
                if (value) ClearRGB();
                OnPropertyChanged();
                Apply();
            }
        }

        public bool ValueChannel
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                if (value) ClearRGB();
                OnPropertyChanged();
                Apply();
            }
        }

        private void ClearRGB()
        {
            _red = _green = _blue = false;
            OnPropertyChanged(nameof(RedChannel));
            OnPropertyChanged(nameof(GreenChannel));
            OnPropertyChanged(nameof(BlueChannel));
        }

        private void ClearHSV()
        {
            _hue = _saturation = _value = false;
            OnPropertyChanged(nameof(HueChannel));
            OnPropertyChanged(nameof(SaturationChannel));
            OnPropertyChanged(nameof(ValueChannel));
        }

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized())
                return;

            HOperatorSet.CountChannels(_inputImage, out HTuple channels);
            bool isGray = (channels.I == 1);
            if (isGray)
            {
                HOperatorSet.Compose3(_inputImage,
                                      _inputImage,
                                      _inputImage,
                                      out HObject rgbImage);
                _inputImage = rgbImage;
            }

            // RGB 分量
            if (RedChannel || GreenChannel || BlueChannel)
            {
                // 分离后得到单通道图像 r、g、b
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);

                // 获取 r 的尺寸
                HOperatorSet.GetImageSize(r, out HTuple width, out HTuple height);

                // 生成一张同样大小的全零图像
                HOperatorSet.GenImageConst(out HObject zero, "byte", width, height);

                // 根据用户选择决定 rr, gg, bb
                HObject rr = RedChannel ? r : zero;
                HObject gg = GreenChannel ? g : zero;
                HObject bb = BlueChannel ? b : zero;

                // 合成
                HOperatorSet.Compose3(rr, gg, bb, out _resultImage);

                // 清理临时对象
                r.Dispose(); g.Dispose(); b.Dispose(); zero.Dispose();
            }
            // HSV 分量
            else if (HueChannel || SaturationChannel || ValueChannel)
            {
                // 转换到 HSV
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);
                HOperatorSet.TransFromRgb(r, g, b, out HObject h, out HObject s, out HObject v, "hsv");

                // 仅显示灰度图（单通道）
                // 如果多选，可以简单叠加或只选一
                if (HueChannel)
                    _resultImage = h;
                else if (SaturationChannel)
                    _resultImage = s;
                else // ValueChannel
                    _resultImage = v;

                // 其余对象若未用，释放
                if (!HueChannel) h.Dispose();
                if (!SaturationChannel) s.Dispose();
                if (!ValueChannel) v.Dispose();
            }
            else
            {
                // 如果都没选，就把原图显示出来
                _resultImage = _inputImage;
            }

            // 显示
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
            OnPropertyChanged(nameof(CurrentResultImage));
        }
    }
}
