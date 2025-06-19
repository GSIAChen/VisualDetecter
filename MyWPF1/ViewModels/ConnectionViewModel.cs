using HalconDotNet;
using System.Diagnostics;

namespace MyWPF1.ViewModels
{
    public class ConnectionViewModel : ToolBaseViewModel
    {
        public override HObject CurrentResultImage => _resultImage;

        public override void Apply()
        {
            if (_inputImage == null || !_inputImage.IsInitialized())
                return;
            HObject ConnectedRegions;
            HOperatorSet.Connection(_inputImage, out ConnectedRegions);
            HOperatorSet.CountObj(ConnectedRegions, out HTuple numRegions);
            for (int i=0; numRegions; i++)
            {
                HOperatorSet.SelectObj(ConnectedRegions, out HObject region, i + 1);
                HOperatorSet.AreaCenter(region, out HTuple area, out HTuple row, out HTuple column);
                Debug.WriteLine(area);
                if (area > 100) // 只保留面积大于100的区域
                {
                    //HOperatorSet.SetColor(_hWindowControl, "green");
                    //HOperatorSet.DispObj(region, _hWindowControl);
                    //HOperatorSet.DispText(_hWindowControl, $"Region {i + 1}: Area = {area}, Center = ({row}, {column})", "window", row, column, "black", "box", "true");
                }
            }
        }
    }
}
