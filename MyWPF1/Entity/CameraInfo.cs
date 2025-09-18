using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyWPF1.Entity
{
    /// <summary>
    /// 相机信息类
    /// </summary>
    public class CameraInfo
    {
        private string _id;
        private string _name;
        private string _model;
        private bool _isConnected;

        /// <summary>
        /// 相机唯一标识
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// 相机显示名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// 相机型号
        /// </summary>
        public string Model
        {
            get { return _model; }
            set { _model = value; }
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; }
        }
    }

}
