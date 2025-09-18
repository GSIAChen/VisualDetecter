using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWPF1.Entity
{
    /// <summary>
    /// 几何参数类
    /// </summary>
    public class GeometryParams
    {
        private int _filterLevel;
        private int _points;
        private double _magnification;
        private string _unit;

        /// <summary>
        /// 滤波级别
        /// </summary>
        public int FilterLevel
        {
            get { return _filterLevel; }
            set { _filterLevel = value; }
        }

        /// <summary>
        /// 点位数量
        /// </summary>
        public int Points
        {
            get { return _points; }
            set { _points = value; }
        }

        /// <summary>
        /// 倍率
        /// </summary>
        public double Magnification
        {
            get { return _magnification; }
            set { _magnification = value; }
        }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit
        {
            get { return _unit; }
            set { _unit = value; }
        }
    }
}
