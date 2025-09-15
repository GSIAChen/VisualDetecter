using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWPF1.ViewModels
{
    public class LineFittingViewModel : ToolBaseViewModel
    {
        public override HObject CurrentResultImage => _resultImage;
        // 可选项
        public ObservableCollection<string> AlgorithmOptions { get; } = new()
        {
            "regression", "huber", "tukey", "gauss", "drop"
        };

        private double _clippingFactor = 2.0;
        public double ClippingFactor
        {
            get => _clippingFactor;
            set { _clippingFactor = value; OnPropertyChanged(); Apply(); }
        }

        private int _clippingEndPoints = 0;
        public int ClippingEndPoints
        {
            get => _clippingEndPoints;
            set { _clippingEndPoints = value; OnPropertyChanged(); Apply(); }
        }

        private int _iterations = 5;
        public int Iterations
        {
            get => _iterations;
            set { _iterations = value; OnPropertyChanged(); Apply(); }
        }

        private string _algorithm = "tukey";
        public string Algorithm
        {
            get => _algorithm;
            set { _algorithm = value; OnPropertyChanged(); Apply(); }
        }

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized()) return;

            HObject contours;
            if (_inputImage.GetObjClass().S == "image")
            {
                HOperatorSet.EdgesSubPix(_inputImage, out contours, "canny", 1.2, 20, 40);
            }
            else
            {
                contours = _inputImage;
            }

            HOperatorSet.FitLineContourXld(
                contours,
                _algorithm,
                -1,
                0,
                _iterations,
                _clippingFactor,
                out HTuple rowBegin, out HTuple colBegin,
                out HTuple rowEnd, out HTuple colEnd,
                out HTuple nr, out HTuple nc,
                out HTuple dist
            );

            HObject lines;
            HOperatorSet.GenContourPolygonXld(out lines, rowBegin.TupleConcat(rowEnd), colBegin.TupleConcat(colEnd));
            _resultImage = lines;

            _hWindowControl.HalconWindow.ClearWindow();
            _hWindowControl.HalconWindow.DispObj(_resultImage);
        }
    }
}
