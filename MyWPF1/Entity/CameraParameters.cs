using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyWPF1.Entity
{
    /// <summary>
    /// 相机参数类
    /// </summary>
    public class CameraParameters
    {
        private string _cameraName;
        private double _exposureTime;
        private double _gamma;
        private string _gammaMode;
        private double _redChannel;
        private double _greenChannel;
        private double _blueChannel;
        private double _rate;
        private double _gain;
        private string _horizontalMode;
        private long _horizontalValue;
        private string _verticalMode;
        private long _verticalModeValue;
        private GeometryParams _geometry;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName
        {
            get { return _cameraName; }
            set { _cameraName = value; }
        }

        /// <summary>
        /// 曝光时间（毫秒）
        /// </summary>
        public double ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; }
        }

        /// <summary>
        /// 帧率
        /// </summary>
        public double Rate
        {
            get { return _rate; }
            set { _rate = value; }
        }

        /// <summary>
        /// 增益
        /// </summary>
        public double Gain
        {
            get { return _gain; }
            set { _gain = value; }
        }

        /// <summary>
        /// 伽马值
        /// </summary>
        public double Gamma
        {
            get { return _gamma; }
            set { _gamma = value; }
        }

        /// <summary>
        /// 伽马模式
        /// </summary>
        public string GammaMode
        {
            get { return _gammaMode; }
            set { _gammaMode = value; }
        }

        /// <summary>
        /// 红色通道值（0-255）
        /// </summary>
        public double RedChannel
        {
            get { return _redChannel; }
            set { _redChannel = value; }
        }

        /// <summary>
        /// 绿色通道值（0-255）
        /// </summary>
        public double GreenChannel
        {
            get { return _greenChannel; }
            set { _greenChannel = value; }
        }

        /// <summary>
        /// 蓝色通道值（0-255）
        /// </summary>
        public double BlueChannel
        {
            get { return _blueChannel; }
            set { _blueChannel = value; }
        }

        /// <summary>
        /// 几何参数
        /// </summary>
        public GeometryParams Geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        /// <summary>
        /// 水平模式
        /// </summary>
        public string HorizontalMode
        {
            get { return _horizontalMode; }
            set { _horizontalMode = value; }
        }

        /// <summary>
        /// 水平模式值
        /// </summary>
        public long HorizontalValue
        {
            get { return _horizontalValue; }
            set { _horizontalValue = value; }
        }

        /// <summary>
        /// 垂直模式
        /// </summary>
        public string VerticalMode
        {
            get { return _verticalMode; }
            set { _verticalMode = value; }
        }

        /// <summary>
        /// 垂直模式值
        /// </summary>
        public long VerticalModeValue
        {
            get { return _verticalModeValue; }
            set { _verticalModeValue = value; }
        }
    }
}
