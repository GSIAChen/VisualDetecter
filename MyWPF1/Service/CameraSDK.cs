using GxIAPINET;
using MyWPF1.Entity;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MyWPF1.Service
{
    /// <summary>
    /// 相机SDK模拟实现
    /// </summary>
    public class CameraSDK 
    {


    public List<IGXDeviceInfo> GetAvailableCameras(bool sortBySnNumeric = true)
    {
        List<IGXDeviceInfo> lstDevInfo = new List<IGXDeviceInfo>();
        IGXFactory.GetInstance().UpdateAllDeviceListEx((ulong)GX_TL_TYPE_LIST.GX_TL_TYPE_GEV, 1000, lstDevInfo);

        if (lstDevInfo.Count < 1)
        {
            System.Windows.MessageBox.Show("枚举相机失败");
            throw new CGalaxyException((int)GX_STATUS_LIST.GX_STATUS_ERROR, "Gige device less than 1!");
        }

        if (sortBySnNumeric)
        {
            // 尝试按 SN 尾部数字排序，若无法解析则回退到字符串比较
            var rx = new Regex(@"(\d+)$", RegexOptions.Compiled);
            lstDevInfo.Sort((a, b) =>
            {
                string snA = a.GetSN() ?? string.Empty;
                string snB = b.GetSN() ?? string.Empty;

                var mA = rx.Match(snA);
                var mB = rx.Match(snB);

                if (mA.Success && mB.Success)
                {
                    // 解析为 long 以防整数较大
                    if (long.TryParse(mA.Groups[1].Value, out long nA) &&
                        long.TryParse(mB.Groups[1].Value, out long nB))
                    {
                        int cmp = nA.CompareTo(nB);
                        if (cmp != 0) return cmp;
                        // 如果数字相等（极少见），再按完整 SN 比较以稳定排序
                        return string.Compare(snA, snB, StringComparison.Ordinal);
                    }
                }

                // 任一方不能解析数字 -> 回退到字典序（稳定）
                return string.Compare(snA, snB, StringComparison.Ordinal);
            });
        }
        else
        {
            // 纯字符串排序（按字典序）
            lstDevInfo.Sort((a, b) => string.Compare(a.GetSN() ?? string.Empty, b.GetSN() ?? string.Empty, StringComparison.Ordinal));
        }

        // 输出日志（按排序后的顺序）
        Trace.WriteLine("已发现相机（按SN排序）:");
        foreach (var device in lstDevInfo)
        {
            Trace.WriteLine($"发现相机: SN={device.GetSN()}, Model={device.GetDeviceID()}, IP={device.GetIP()}");
        }

        return lstDevInfo;
    }

    /// <summary>
    /// 枚举网络相机设备
    /// </summary>
    public List<IGXDeviceInfo> GetAvailableCameras()
        {
            List<IGXDeviceInfo> lstDevInfo = new List<IGXDeviceInfo>();
            IGXFactory.GetInstance().UpdateAllDeviceListEx((ulong)GX_TL_TYPE_LIST.GX_TL_TYPE_GEV, 1000, lstDevInfo);
            foreach(var device in lstDevInfo)
            {
                Trace.WriteLine($"发现相机: SN={device.GetSN()}, Model={device.GetDeviceID()}, IP={device.GetIP()}");
            }
            if (lstDevInfo.Count < 1)
            {
                System.Windows.MessageBox.Show("枚举相机失败");
                throw new CGalaxyException((int)GX_STATUS_LIST.GX_STATUS_ERROR, "Gige device less than 1!");
            }
            return lstDevInfo;
        }

        /// <summary>
        /// 初始化相机
        /// </summary>
        public void InitCamera()
        {
            try
            {
                IGXFactory.GetInstance().Init();
            }catch(Exception e)
            {
                System.Windows.MessageBox.Show("初始化相机失败");
            }
        }


        /// <summary>
        /// 根据相机的sn,连接指定相机
        /// </summary>
        public IGXDevice ConnectCamera(IGXDeviceInfo info)
        {
            try
            {
                string sn = info.GetSN();
                // 通过SN打开所有枚举到的网络相机设备
                IGXDevice objDevPtr = IGXFactory.GetInstance().OpenDeviceBySN(info.GetSN(), GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);
                return objDevPtr;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("打开相机失败" + info.GetSN());
                return null;

            }
        }

        /// <summary>
        /// 断开当前相机连接
        /// </summary>
        public void DisconnectCamera()
        {
            Console.WriteLine("断开相机连接");
        }

      
        /// <summary>
        /// 设置曝光时间
        /// </summary>
        public void SetExposureTime(double exposureTime, IGXFeatureControl iGXFeatureControl)
        {
            try
            {

                iGXFeatureControl.GetFloatFeature("ExposureTime").SetValue(exposureTime);
            }
            catch (Exception ex)
            {
                throw new Exception("设置曝光时间失败"+ex.Message);
            }
        }

        /// <summary>
        /// 获取曝光时间
        /// </summary>
        public double GetExposureTime(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                double exposureTime = iGXFeatureControl.GetFloatFeature("ExposureTime").GetValue();
                return exposureTime;
            }
            catch(Exception ex)
            {
                throw new Exception("获取曝光时间失败" + ex.Message);

            }

        }

        /// <summary>
        /// 设置帧率
        /// </summary>
        public void SetRate(double rate,IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetFloatFeature("AcquisitionFrameRate").SetValue(rate);
            }
            catch(Exception ex)
            {
                throw new Exception("设置帧率失败" + ex.Message);

            }
        }

        /// <summary>
        /// 获取帧率
        /// </summary>
        public double GetRate(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                double rate = iGXFeatureControl.GetFloatFeature("AcquisitionFrameRate").GetValue();
                return rate;
            }
            catch(Exception ex)
            {
                throw new Exception("获取帧率失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置增益
        /// </summary>
        public void SetGain(double gain, IGXFeatureControl iGXFeatureControl)
        {
            try
            {

                iGXFeatureControl.GetEnumFeature("GainSelector").SetValue("AnalogAll");
                iGXFeatureControl.GetFloatFeature("Gain").SetValue(gain);
            }
            catch (Exception ex)
            {
                throw new Exception("设置增益失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取增益
        /// </summary>
        public double GetGain(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                double gain = iGXFeatureControl.GetFloatFeature("Gain").GetValue();
                return gain;
            }
            catch (Exception ex)
            {
                throw new Exception("获取增益失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置伽马值
        /// </summary>
        public void SetGamma(double gamma, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetFloatFeature("Gamma").SetValue(gamma);
            }
            catch (Exception ex)
            {
                throw new Exception("设置伽马值失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取伽马值
        /// </summary>
        public double GetGamma(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                double  gamma= iGXFeatureControl.GetFloatFeature("Gamma").GetValue();
                return gamma;
            }
            catch (Exception ex)
            {
                throw new Exception("获取伽马值失败" + ex.Message);
            }
        }


        /// <summary>
        /// 设置伽马模式
        /// </summary>
        public void SetGammaMode(string mode, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("GammaMode").SetValue(mode);
            }
            catch(Exception ex)
            {
                throw new Exception("设置伽马模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取伽马模式
        /// </summary>
        public string GetGammaMode(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                string gamma = iGXFeatureControl.GetEnumFeature("GammaMode").GetValue();
                return gamma;
            }
            catch(Exception ex)
            {
                throw new Exception("获取伽马模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置RGB通道值
        /// </summary>
        public void SetRGBChannels(double red, double green, double blue, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("BalanceRatioSelector").SetValue("Red");
                iGXFeatureControl.GetFloatFeature("BalanceRatio").SetValue(red);
                iGXFeatureControl.GetEnumFeature("BalanceRatioSelector").SetValue("green");
                iGXFeatureControl.GetFloatFeature("BalanceRatio").SetValue(green);
                iGXFeatureControl.GetEnumFeature("BalanceRatioSelector").SetValue("blue");
                iGXFeatureControl.GetFloatFeature("BalanceRatio").SetValue(blue);
            }
            catch(Exception ex)
            {
                throw new Exception("设置RGB通道值失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取R,G,B通道值
        /// </summary>
        public double getRChannels(string rgbType,IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("BalanceRatioSelector").SetValue(rgbType);
                double d = iGXFeatureControl.GetFloatFeature("BalanceRatio").GetValue();
                return d;
            }
            catch(Exception ex)
            {
                throw new Exception("获取RGB通道值失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置水平模式
        /// </summary>
        public void SetHorizontalMode(string mode, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("BinningHorizontalMode").SetValue(mode);

            }
            catch (Exception ex)
            {
                throw new Exception("设置水平模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取水平模式
        /// </summary>
        public string GetHorizontalMode(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                string s = iGXFeatureControl.GetEnumFeature("BinningHorizontalMode").GetValue();
                return s;
            }
            catch (Exception ex)
            {
                throw new Exception("获取水平模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置垂直模式
        /// </summary>
        public void SetVerticalMode(string mode, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("BinningVerticalMode").SetValue(mode);

            }
            catch (Exception ex)
            {
                throw new Exception("设置垂直模式失败" + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取水平模式
        /// </summary>
        public string GetVerticalMode(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                string s = iGXFeatureControl.GetEnumFeature("BinningVerticalMode").GetValue();
                return s;
            }
            catch (Exception ex)
            {
                throw new Exception("获取垂直模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置水平bin值
        /// </summary>
        public void SetBinningHorizontalValue(long binHorizontal, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetIntFeature("BinningHorizontal").SetValue(binHorizontal);
            }
            catch (Exception ex)
            {
                throw new Exception("设置水平bin值失败" + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取水平bin值
        /// </summary>
        public long GetBinningHorizontalValue(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                Int64 n = (Int64)iGXFeatureControl.GetIntFeature("BinningHorizontal").GetValue();
                return n;
            }
            catch (Exception ex)
            {
                throw new Exception("获取水平bin值失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置垂直bin值
        /// </summary>
        public void SetVerticalValue(long verValue, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetIntFeature("BinningVertical").SetValue(verValue);
            }
            catch (Exception ex)
            {
                throw new Exception("设置垂直bin值失败" + ex.Message);
            }
        }

        /// <summary>
        /// 获取垂直bin值
        /// </summary>
        public long GetVerticalValue(IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                Int64 n = (Int64)iGXFeatureControl.GetIntFeature("BinningVertical").GetValue();
                return n;
            }
            catch (Exception ex)
            {
                throw new Exception("获取垂直bin值失败" + ex.Message);
            }
        }

        public void SetTriggerMode(string mode, IGXFeatureControl iGXFeatureControl)
        {
            try
            {
                iGXFeatureControl.GetEnumFeature("TriggerMode").SetValue(mode);
            }
            catch (Exception ex)
            {
                throw new Exception("设置触发模式失败" + ex.Message);
            }
        }

        /// <summary>
        /// 设置几何参数
        /// </summary>
        public void SetGeometryParameters(GeometryParams geometry)
        {
            Console.WriteLine($"设置几何参数: Filter={geometry.FilterLevel}, Points={geometry.Points}, Magnification={geometry.Magnification}, Unit={geometry.Unit}");
        }

        /// <summary>
        /// 开始实时预览
        /// </summary>
        public void StartPreview()
        {
            Console.WriteLine("开始实时预览");
        }

        /// <summary>
        /// 停止实时预览
        /// </summary>
        public void StopPreview()
        {
            Console.WriteLine("停止实时预览");
        }

        /// <summary>
        /// 单帧捕获
        /// </summary>
        public void CaptureSingleFrame()
        {
            Console.WriteLine("单帧捕获");
        }

        /// <summary>
        /// 保存当前配置
        /// </summary>
        public void SaveConfiguration(string configName)
        {
            Console.WriteLine($"保存配置: {configName}");
        }

        /// <summary>
        /// 加载指定配置
        /// </summary>
        public void LoadConfiguration(string configName)
        {
            Console.WriteLine($"加载配置: {configName}");
        }
    }
}
