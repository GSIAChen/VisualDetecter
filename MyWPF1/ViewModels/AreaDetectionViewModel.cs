using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWPF1.ViewModels
{
    public class AreaDetectionViewModel : ToolBaseViewModel
    {
        public override HObject CurrentResultImage => _resultImage;
        // —— ROI 参数 —— 
        private double _row1 = 30;
        public double Row1
        {
            get => _row1;
            set { if (_row1 != value) { _row1 = value; OnPropertyChanged(); Apply(); } }
        }
        private double _col1 = 20;
        public double Column1
        {
            get => _col1;
            set { if (_col1 != value) { _col1 = value; OnPropertyChanged(); Apply(); } }
        }
        private double _row2 = 100;
        public double Row2
        {
            get => _row2;
            set { if (_row2 != value) { _row2 = value; OnPropertyChanged(); Apply(); } }
        }
        private double _col2 = 200;
        public double Column2
        {
            get => _col2;
            set { if (_col2 != value) { _col2 = value; OnPropertyChanged(); Apply(); } }
        }

        // 二值化阈值参数
        private HObject _region;  // 用于存储二值化后的区域
        private int _threshold = 128;
        public int Threshold
        {
            get => _threshold;
            set
            {
                if (_threshold != value)
                {
                    _threshold = value;
                    OnPropertyChanged();
                    Apply();
                }
            }
        }

        // 结果数据：面积与中心坐标
        private double _area;
        public double Area
        {
            get => _area;
            private set { _area = value; OnPropertyChanged(); }
        }

        private double _centerRow;
        public double CenterRow
        {
            get => _centerRow;
            private set { _centerRow = value; OnPropertyChanged(); }
        }

        private double _centerCol;
        public double CenterCol
        {
            get => _centerCol;
            private set { _centerCol = value; OnPropertyChanged(); }
        }

        public override void SetInputImage(HObject image)
        {
            base.SetInputImage(image); // 存 _inputImage 并调用 Apply()

            // 在这里读取图像尺寸，初始化 ROI 为整图
            HOperatorSet.GetImageSize(_inputImage, out HTuple width, out HTuple height);
            Row1 = 0;
            Column1 = 0;
            Row2 = height.D - 1;
            Column2 = width.D - 1;
        }

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized())
                return;

            // 2. 确保输入图像是灰度图像，如果是彩色图像则转换为灰度图像
            HOperatorSet.CountChannels(_inputImage, out HTuple channels);
            bool isGray = (channels.I == 1);
            if (!isGray)
            {
                HOperatorSet.Decompose3(_inputImage, out HObject r, out HObject g, out HObject b);
                HOperatorSet.Rgb3ToGray(r, g, b, out HObject grayImage);
                _inputImage = grayImage;
                r.Dispose(); g.Dispose(); b.Dispose();
            }
            
            // 3. 在 ROI 上构造区域并缩小域
            HOperatorSet.GenRectangle1(
                out HObject rectangle,
                new HTuple(Row1),
                new HTuple(Column1),
                new HTuple(Row2),
                new HTuple(Column2));

            HOperatorSet.ReduceDomain(
                _inputImage,
                rectangle,
                out HObject domainImage);

            // 4. 二值化
            HOperatorSet.Threshold(domainImage, out HObject region, _threshold, 255);
            _region = region;

            // 5. 面积检测
            HOperatorSet.AreaCenter(region, out HTuple areaTuple, out HTuple rowTuple, out HTuple colTuple);
            Area = areaTuple.D;

            HOperatorSet.GenContourRegionXld(
            rectangle,
            out HObject rectContour,
            "border");

            HOperatorSet.PaintRegion(region,
                         _inputImage,
                         out _resultImage,
                         255,
                         "fill");
            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
            _hWindowControl.HalconWindow.SetColor("red");
            _hWindowControl.HalconWindow.SetDraw("margin");
            _hWindowControl.HalconWindow.DispObj(rectContour);

            // 清理
            region.Dispose(); rectangle.Dispose(); rectContour.Dispose();

            OnPropertyChanged(nameof(Area));
        }
    }
}
