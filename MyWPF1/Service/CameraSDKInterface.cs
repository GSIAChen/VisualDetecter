using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GxIAPINET;
using MyWPF1.Entity;

namespace MyWPF1.Service
{
    /// <summary>
    /// 相机SDK接口定义
    /// </summary>
    public interface ICameraSDK
    {
        /// <summary>
        /// 获取可用相机列表
        /// </summary>
        /// <returns>相机信息列表</returns>
        List<IGXDeviceInfo> GetAvailableCameras();

        /// <summary>
        /// 初始化相机
        /// </summary>
        /// <returns>连接是否成功</returns>
        void InitCamera();

        /// <summary>
        /// 连接指定相机
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns>连接是否成功</returns>
        bool ConnectCamera(IGXDeviceInfo device);

        /// <summary>
        /// 断开当前相机连接
        /// </summary>
        void DisconnectCamera();

        /// <summary>
        /// 获取当前相机参数
        /// </summary>
        /// <returns>相机参数对象</returns>
        CameraParameters GetCurrentParameters();

        /// <summary>
        /// 设置曝光时间
        /// </summary>
        /// <param name="exposureTime">曝光时间（毫秒）</param>
        void SetExposureTime(double exposureTime);

        /// <summary>
        /// 获取曝光时间
        /// </summary>
        void GetExposureTime();

        /// <summary>
        /// 设置帧率
        /// </summary>
        /// <param name="rate">曝光时间（毫秒）</param>
        void SetRate(double rate);

        /// <summary>
        /// 获取帧率
        /// </summary>
        void GetRate();

        /// <summary>
        /// 设置增益
        /// </summary>
        void SetGain(double gain);

        /// <summary>
        /// 获取增益
        /// </summary>
        void GetGain();

        /// <summary>
        /// 设置伽马值
        /// </summary>
        /// <param name="gamma">伽马值</param>
        void SetGamma(double gamma);

        /// <summary>
        /// 获取伽马值
        /// </summary>
        void GetGamma();

        /// <summary>
        /// 设置伽马模式
        /// </summary>
        /// <param name="mode">伽马模式</param>
        void SetGammaMode(string mode);

        /// <summary>
        /// 设置伽马模式
        /// </summary>
        void GetGammaMode();

        /// <summary>
        /// 设置RGB通道值
        /// </summary>
        /// <param name="red">红色通道值（0-255）</param>
        /// <param name="green">绿色通道值（0-255）</param>
        /// <param name="blue">蓝色通道值（0-255）</param>
        void SetRGBChannels(int red, int green, int blue);

        /// <summary>
        /// 设置几何参数
        /// </summary>
        /// <param name="geometry">几何参数对象</param>
        void SetGeometryParameters(GeometryParams geometry);

        /// <summary>
        /// 开始实时预览
        /// </summary>
        void StartPreview();

        /// <summary>
        /// 停止实时预览
        /// </summary>
        void StopPreview();

        /// <summary>
        /// 单帧捕获
        /// </summary>
        void CaptureSingleFrame();

        /// <summary>
        /// 保存当前配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        void SaveConfiguration(string configName);

        /// <summary>
        /// 加载指定配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        void LoadConfiguration(string configName);
    }
}
