using GxIAPINET;
using HandyControl.Controls;
using MCDLL_NET;
using MyWPF1.Entity;
using MyWPF1.Service;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Web.Services.Description;
using System.Collections.Concurrent;

namespace MyWPF1
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private ushort stationNum = 0;
        private ushort stationType = 2;
        private ushort connected = 0;
        private ushort disconnected = 1;
        private float _RotationSpeed;
        private string jsFilePath = "D:\\TestEc3224l.js"; // 需要替换为实际路径  

        public float RotationSpeed
        {
            get => _RotationSpeed;
            set
            {
                _RotationSpeed = value;
                OnPropertyChanged();
            }
        }
        private float _CamTriggerTime;
        public float CamTriggerTime
        {
            get => _CamTriggerTime;
            set
            {
                _CamTriggerTime = value;
                OnPropertyChanged();
            }
        }
        private int _CCD1Pos;
        public int CCD1Pos
        {
            get => _CCD1Pos;
            set
            {
                _CCD1Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD2Pos;
        public int CCD2Pos
        {
            get => _CCD2Pos;
            set
            {
                _CCD2Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD3Pos;
        public int CCD3Pos
        {
            get => _CCD3Pos;
            set
            {
                _CCD3Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD4Pos;
        public int CCD4Pos
        {
            get => _CCD4Pos;
            set
            {
                _CCD4Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD5Pos;
        public int CCD5Pos
        {
            get => _CCD5Pos;
            set
            {
                _CCD5Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD6Pos;
        public int CCD6Pos
        {
            get => _CCD6Pos;
            set
            {
                _CCD6Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD7Pos;
        public int CCD7Pos
        {
            get => _CCD7Pos;
            set
            {
                _CCD7Pos = value;
                OnPropertyChanged();
            }
        }
        private int _CCD8Pos;
        public int CCD8Pos
        {
            get => _CCD8Pos;
            set
            {
                _CCD8Pos = value;
                OnPropertyChanged();
            }
        }

        // 枚举的相机集合
        private List<IGXDeviceInfo> cameras = new List<IGXDeviceInfo>();
        //相机参数集合
        private List<CameraParameters> cameraParaList = new List<CameraParameters>();
        //相机设备集合
        private List<IGXDevice> deviceList = new List<IGXDevice>();
        // 当前选中的相机
        private CameraParameters currentCamera;
        //当前设备
        private IGXDevice currentDevice;
        // 
        private bool _isCapturing = false;
        private IGXStream _currentStream = null;
        private BitmapImage _currentBitmapImage = null;
        private object _imageLock = new object();
        // 内存复制函数
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        private ConcurrentQueue<Bitmap> _bitmapQueue = new ConcurrentQueue<Bitmap>();
        private bool _isProcessingQueue = false;
        private System.Threading.Timer _uiUpdateTimer;

        public SettingWindow()
        {
            CMCDLL_NET_Sorting.MCF_Sorting_Init_Net();
            CMCDLL_NET_Sorting.MCF_Open_Net(1, ref stationNum, ref stationType);
            CMCDLL_NET_Sorting.MCF_Set_Servo_Enable_Net(0, connected, 0);
            CMCDLL_NET_Sorting.MCF_Set_Output_Bit_Net(0, connected, 0);
            InitializeComponent();
            this.DataContext = this;
            Loaded += SettingWindow_Loaded;
            this.Closed += SettingWindow_Closed;
        }

        private void SettingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            

            CameraSDK cameraSDK=new CameraSDK();
            cameraSDK.InitCamera();
            List<IGXDeviceInfo> cameraList=cameraSDK.GetAvailableCameras();
            // 程序启动时连接所有相机
            for (int i = 0; i < cameraList.Count; i++)
            {
                IGXDeviceInfo iGXDeviceInfo=cameraList[i];
                IGXDevice device=cameraSDK.ConnectCamera(cameraList[i]);
                IGXFeatureControl featureControrl=device.GetRemoteFeatureControl();
                deviceList.Add(device);
                
                CameraParameters cameraParameter=new CameraParameters();
                cameraParameter.CameraName = "camera"+(i+1);
                //获取曝光时间
                cameraParameter.ExposureTime= cameraSDK.GetExposureTime(featureControrl);
                //获取增益
                cameraParameter.Gain = cameraSDK.GetGain(featureControrl);
                //获取帧率
                cameraParameter.Rate = cameraSDK.GetRate(featureControrl);
                //获取伽马模式
                cameraParameter.GammaMode = cameraSDK.GetGammaMode(featureControrl);
                //获取R的值
                cameraParameter.RedChannel = cameraSDK.getRChannels("red", featureControrl);
                //获取G的值
                cameraParameter.GreenChannel = cameraSDK.getRChannels("green", featureControrl);
                //获取B的值
                cameraParameter.BlueChannel = cameraSDK.getRChannels("blue", featureControrl);
                //获取水平bin模式
                cameraParameter.HorizontalMode = cameraSDK.GetHorizontalMode(featureControrl);
                //获取水平bin模式的值
                cameraParameter.HorizontalValue = cameraSDK.GetBinningHorizontalValue(featureControrl);
                //获取垂直bin模式
                cameraParameter.VerticalMode = cameraSDK.GetVerticalMode(featureControrl);
                //获取垂直bin模式的值
                cameraParameter.VerticalModeValue = cameraSDK.GetVerticalValue(featureControrl);
                if (cameraParameter.GammaMode.Equals("User"))
                {
                    //获取伽马值
                    cameraParameter.Gamma = cameraSDK.GetGamma(featureControrl);
                }

                cameraParaList.Add(cameraParameter);
            }

            // 初始化UI
            InitializeCameraSelector();
            // 读取JS文件  
            if (File.Exists(jsFilePath))
            {
                string jsContent = File.ReadAllText(jsFilePath);
                var cameraPositions = ParseCameraPositions(jsContent);
                // 假设当前选中的是camera1  
                if (cameraPositions.TryGetValue(currentCamera.CameraName, out int position))
                {
                    CurrentPositionBox.Text = position.ToString();
                    currentCamera.CameraPosition = position;
                }
            }
            // 启动图像采集
            StartImageCapture();
        }

        private Dictionary<string, int> ParseCameraPositions(string jsContent)
        {
            var cameraPositions = new Dictionary<string, int>();
            var lines = jsContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string currentCamera = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("cameraCfg.name"))
                {
                    // 提取name的值，例如"camera1"  
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(line, @"cameraCfg\.name\s*=\s*""([^""]+)""");
                    if (nameMatch.Success)
                    {
                        currentCamera = nameMatch.Groups[1].Value;
                    }
                }
                else if (line.StartsWith("cameraCfg.position") && currentCamera != null)
                {
                    var positionMatch = System.Text.RegularExpressions.Regex.Match(line, @"cameraCfg\.position\s*=\s*(\d+)");
                    if (positionMatch.Success)
                    {
                        if (int.TryParse(positionMatch.Groups[1].Value, out int position))
                        {
                            cameraPositions[currentCamera] = position;
                        }
                        currentCamera = null; // 重置，等待下一个name行  
                    }
                }
            }

            return cameraPositions;
        }

        // 开始图像采集
        private void StartImageCapture()
        {
            if (_isCapturing || currentDevice == null)
            {
                Debug.WriteLine("StartImageCapture: already capturing or currentDevice is null.");
                return;
            }

                try
                {
                // 创建流对象
                _currentStream = currentDevice.OpenStream(0);
                if (_currentStream == null)
                {
                    System.Windows.MessageBox.Show("无法打开相机流");
                    return;
                }
                IGXFeatureControl objIGXFeatureControl = currentDevice.GetRemoteFeatureControl();

                // 设置采集模式为连续采集
                objIGXFeatureControl.GetEnumFeature("AcquisitionMode").SetValue("Continuous");

                //以提高网络相机的采集性能,设置方法参考以下代码（目前只有千兆网系列相机支持设置最优包长）
                GX_DEVICE_CLASS_LIST objDeviceClass =
                currentDevice.GetDeviceInfo().GetDeviceClass();
                if (GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV == objDeviceClass)
                {
                    //判断设备是否支持流通道数据包功能
                    if (true == objIGXFeatureControl.IsImplemented("GevSCPSPacketSize"))
                    {
                        //获取当前网络环境的最优包长值
                        UInt32 ui32PacketSize = _currentStream.GetOptimalPacketSize();
                        //将最优包长值设置为当前设备的流通道包长值
                        objIGXFeatureControl.GetIntFeature(
                         "GevSCPSPacketSize").SetValue(ui32PacketSize);
                    }
                }
                //注册采集回调函数，注意第一个参数是用户私有参数，用户可以传入任何 object 对象，也可以是 null
                //用户私有参数在回调函数内部还原使用，如果不使用私有参数，可以传入 null
                _currentStream.RegisterCaptureCallback(_currentStream, OnFrameCallbackFun);
                //开启流通道采集
                _currentStream.StartGrab();
                //给设备发送开采命令
                objIGXFeatureControl.GetCommandFeature("AcquisitionStart").Execute();

                // 启动定时器更新UI
                _uiUpdateTimer = new System.Threading.Timer(ProcessImageQueue, null, 0, 33);

                _isCapturing = true;
                Debug.WriteLine("Image capture started.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动图像采集失败: {ex.Message}");
                Debug.WriteLine($"StartImageCapture exception: {ex.Message}");
            }
        }

        public void OnFrameCallbackFun(object objPara, IFrameData objIFrameData)
        {
            try
            {
                Debug.WriteLine("OnFrameCallbackFun called.");
                if (0 == objIFrameData.GetStatus())
                {
                    Debug.WriteLine("Frame status is good.");
                    // 转换图像为Bitmap
                    Bitmap bitmap = ConvertGxImageToBitmap(objIFrameData);
                    Debug.WriteLine($"Bitmap created: {bitmap.Width}x{bitmap.Height}");
                    // 将Bitmap加入队列，不在此处进行UI操作
                    _bitmapQueue.Enqueue(bitmap);

                    // 如果队列处理未启动，则启动它
                    if (!_isProcessingQueue)
                    {
                        StartQueueProcessing();
                    }
                    // 使用Dispatcher更新UI
                    /*  System.Windows.Application.Current.Dispatcher.Invoke(() =>
                      {
                          lock (_imageLock)
                          {
                              using (MemoryStream memory = new MemoryStream())
                              {
                                  bitmap.Save(memory, ImageFormat.Bmp);
                                  memory.Position = 0;
                                  _currentBitmapImage = new BitmapImage();
                                  _currentBitmapImage.BeginInit();
                                  _currentBitmapImage.StreamSource = memory;
                                  _currentBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                  _currentBitmapImage.EndInit();
                                  _currentBitmapImage.Freeze();
                                  SPreviewImage.Source = _currentBitmapImage;
                                  Debug.WriteLine("Image updated on UI.");
                              }
                          }
                      });*/
                }
                else
                {
                    Debug.WriteLine($"Frame status is not good: {objIFrameData.GetStatus()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图像处理错误: {ex.Message}");
            }
        }

        // 启动队列处理
        private void StartQueueProcessing()
        {
            _isProcessingQueue = true;

            // 使用后台线程处理队列
            Task.Run(() =>
            {
                while (_bitmapQueue.TryDequeue(out Bitmap bitmap))
                {
                    try
                    {
                        // 处理图像并更新UI
                        ProcessAndDisplayImage(bitmap);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"队列处理错误: {ex.Message}");
                    }
                    finally
                    {
                        bitmap?.Dispose();
                    }

                    // 短暂暂停以避免过度占用CPU
                    System.Threading.Thread.Sleep(1);
                }

                _isProcessingQueue = false;
            });
        }

        // 定时器回调方法 - 处理图像队列
        private void ProcessImageQueue(object state)
        {
            if (_bitmapQueue.IsEmpty) return;

            // 使用Dispatcher更新UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_bitmapQueue.TryDequeue(out Bitmap bitmap))
                {
                    try
                    {
                        lock (_imageLock)
                        {
                            using (MemoryStream memory = new MemoryStream())
                            {
                                bitmap.Save(memory, ImageFormat.Bmp);
                                memory.Position = 0;
                                _currentBitmapImage = new BitmapImage();
                                _currentBitmapImage.BeginInit();
                                _currentBitmapImage.StreamSource = memory;
                                _currentBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                _currentBitmapImage.EndInit();
                                _currentBitmapImage.Freeze();
                                SPreviewImage.Source = _currentBitmapImage;
                                Debug.WriteLine("Image updated on UI.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UI更新错误: {ex.Message}");
                    }
                    finally
                    {
                        bitmap?.Dispose();
                    }
                }
            });
        }

        // 替代方案：直接处理并显示图像
        private void ProcessAndDisplayImage(Bitmap bitmap)
        {
            // 在后台线程处理图像转换
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                // 在UI线程更新图像
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    lock (_imageLock)
                    {
                        _currentBitmapImage = new BitmapImage();
                        _currentBitmapImage.BeginInit();
                        _currentBitmapImage.StreamSource = memory;
                        _currentBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        _currentBitmapImage.EndInit();
                        _currentBitmapImage.Freeze();
                        SPreviewImage.Source = _currentBitmapImage;
                        Debug.WriteLine("Image updated on UI.");
                    }
                });
            }
        }

        // 停止图像采集
        private void StopImageCapture()
        {
            if (currentDevice == null)
            {
                return;
            }

            try
            {
                _uiUpdateTimer?.Dispose();
                _uiUpdateTimer = null;
                _currentBitmapImage = null;
                if (currentDevice != null)
                {
                    currentDevice.GetRemoteFeatureControl().GetCommandFeature("AcquisitionStop").Execute();
                }
                _currentStream.StopGrab();
                _currentStream.UnregisterCaptureCallback();
                _currentStream.Close();
                _currentStream = null;
                _isCapturing = false;
                Debug.WriteLine("Image capture stopped.");

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"停止图像采集失败: {ex.Message}");
                Debug.WriteLine($"StopImageCapture exception: {ex.Message}");

            }
        }

        // 更新UI图像显示
        private void UpdateImageUI(object state)
        {
            if (_currentBitmapImage != null)
            {
                // 使用Dispatcher确保UI更新在UI线程上执行
                Dispatcher.Invoke(() =>
                {
                    lock (_imageLock)
                    {
                        SPreviewImage.Source = _currentBitmapImage;
                    }
                });
            }
        }

        // 转换大恒图像到Bitmap
        private Bitmap ConvertGxImageToBitmap(IFrameData image)
        {
            // 获取图像信息
            int width = (int)image.GetWidth();
            int height = (int)image.GetHeight();
            int payloadSize = (int)image.GetPayloadSize();

            // 获取图像缓冲区
            IntPtr buffer = image.GetBuffer();

            // 根据像素格式创建Bitmap
            System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            GX_PIXEL_FORMAT_ENTRY gxPixelFormat = image.GetPixelFormat();


            // 根据枚举值判断像素格式
            switch (gxPixelFormat)
            {
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BGR8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_RGB8:
                    pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                    break;
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BGRA8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_RGBA8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_ARGB8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_ABGR8:
                    pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                    break;
                default:
                    // 默认为8位灰度格式
                    pixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
                    break;
            }

            // 创建Bitmap
            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            // 锁定Bitmap数据
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                pixelFormat);

            try
            {
                // 计算实际需要复制的数据大小
                int copySize = Math.Min(payloadSize, bitmapData.Stride * height);
                CopyMemory(bitmapData.Scan0, buffer, (uint)copySize);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            // 如果是8位灰度图像，设置灰度调色板
            if (pixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
            }
            return bitmap;
        }

        // 初始化相机选择下拉框
        private void InitializeCameraSelector()
        {
            CameraSelector.Items.Clear();

            foreach (CameraParameters cameraParameters in cameraParaList)
            {
                CameraSelector.Items.Add(cameraParameters.CameraName);
            }

            if (CameraSelector.Items.Count > 0)
            {
                CameraSelector.SelectedIndex = 0;
                currentCamera = cameraParaList[CameraSelector.SelectedIndex];
                currentDevice = deviceList[CameraSelector.SelectedIndex];
                LoadCameraSettings(currentCamera);
            }
            
        }

        // 相机选择变更事件
        private void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 停止当前采集
            StopImageCapture();
            if (CameraSelector.SelectedIndex >= 0 && CameraSelector.SelectedIndex < deviceList.Count)
            {
                currentCamera = cameraParaList[CameraSelector.SelectedIndex];
                currentDevice = deviceList[CameraSelector.SelectedIndex];
                LoadCameraSettings(currentCamera);
                // 开始新相机的采集
                StartImageCapture();
            }
        }

        // 加载相机设置到UI
        private void LoadCameraSettings(CameraParameters camera)
        {
            // 基础参数
            ExposureSlider.Value = camera.ExposureTime;
            RateSlider.Value = camera.Rate;
            GammaSlider.Value = camera.Gamma;
            GainSlider.Value = camera.Gain;
            RedChannelSlider.Value = camera.RedChannel;
            GreenChannelSlider.Value = camera.GreenChannel;
            BlueChannelSlider.Value = camera.BlueChannel;
            HorizontalSlider.Value = camera.HorizontalValue;
            VerticalSlider.Value = camera.VerticalModeValue;
            // 设置伽马模式
            foreach (ComboBoxItem item in GammaModeComboBox.Items)
            {
                if (item.Content.ToString() == camera.GammaMode)
                {
                    GammaModeComboBox.SelectedItem = item;
                    break;
                }
            }
            //设置水平bin模式
            foreach (ComboBoxItem item in HorizontalBox.Items)
            {
                if (item.Content.ToString() == camera.HorizontalMode)
                {
                    HorizontalBox.SelectedItem = item;
                    break;
                }
            }
            //设置垂直bin模式
            foreach (ComboBoxItem item in VerticalBox.Items)
            {
                if (item.Content.ToString() == camera.GammaMode)
                {
                    VerticalBox.SelectedItem = item;
                    break;
                }
            }
        }

        // 应用配置按钮点击事件
        private void ApplySettings_Click(object sender, RoutedEventArgs e)
        {
            CameraSDK cameraSDK = new CameraSDK();
            if (currentCamera == null) return;

            try
            {
                // 更新相机对象的值
                currentCamera.ExposureTime = ExposureSlider.Value;
                currentCamera.Rate = (int)RateSlider.Value;
                currentCamera.Gamma = GammaSlider.Value;
                currentCamera.Gain = GainSlider.Value;
                currentCamera.RedChannel = RedChannelSlider.Value;
                currentCamera.GreenChannel = GreenChannelSlider.Value;
                currentCamera.BlueChannel = BlueChannelSlider.Value;
                currentCamera.GammaMode = ((ComboBoxItem)GammaModeComboBox.SelectedItem)?.Content.ToString();
                currentCamera.HorizontalMode = ((ComboBoxItem)HorizontalBox.SelectedItem)?.Content.ToString();
                currentCamera.VerticalMode = ((ComboBoxItem)VerticalBox.SelectedItem)?.Content.ToString();
                currentCamera.HorizontalValue = (long)HorizontalSlider.Value;
                currentCamera.VerticalModeValue = (long)VerticalSlider.Value;
                IGXFeatureControl featureControl =currentDevice.GetRemoteFeatureControl();
                // 这里应该调用相机SDK应用设置
                //设置曝光时间的值
                cameraSDK.SetExposureTime(currentCamera.ExposureTime, featureControl);
                //设置帧率的值
                cameraSDK.SetRate(currentCamera.Rate, featureControl);
                //设置增益
                cameraSDK.SetGain(currentCamera.Gain, featureControl);
                //设置伽马方式
                cameraSDK.SetGammaMode(currentCamera.GammaMode, featureControl);
                //设置RGB
                cameraSDK.SetRGBChannels(currentCamera.RedChannel, currentCamera.GreenChannel, currentCamera.BlueChannel, featureControl);
                //设置伽马值
                if (currentCamera.GammaMode.Equals("User"))
                {
                    cameraSDK.SetGamma(currentCamera.Gamma, featureControl);
                }
                if (_isCapturing = false)
                {
                    //设置水平bin模式
                    cameraSDK.SetHorizontalMode(currentCamera.HorizontalMode, featureControl);
                    //设置水平bin模式值
                    cameraSDK.SetBinningHorizontalValue(currentCamera.HorizontalValue, featureControl);
                    //设置垂直bin模式
                    cameraSDK.SetVerticalMode(currentCamera.VerticalMode, featureControl);
                    //设置垂直bin模式值
                    cameraSDK.SetVerticalValue(currentCamera.VerticalModeValue, featureControl);
                }
        
                System.Windows.MessageBox.Show("配置已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"应用配置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 窗口已经完全关闭后
        private void SettingWindow_Closed(object sender, EventArgs e)
        {
            // 停止图像采集
            StopImageCapture();
            if (deviceList.Count > 0)
            {
                for (int i = 0; i < deviceList.Count; i++)
                {
                    try
                    {
                        IGXDevice device = deviceList[i];
                        device.Close();
                        IGXFactory.GetInstance().Uninit();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("关闭相机失败");
                    }

                }
            }
        }

        //点击移动
        private void RunToPos_Click(object sender, RoutedEventArgs e)
        {
            string value = Cam1PositionBox.Text;
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, int.Parse(value) - currentCamera.CameraPosition, 0);
        }

        //保存到js文件
        private void SavePos_Click(object sender, RoutedEventArgs e)
        {
            string value = Cam1PositionBox.Text;
            if (Cam1PositionBox.Text != null)
            {
                currentCamera.CameraPosition = int.Parse(value);
                // 方式1: 写入到共享的配置文件（推荐）
                SaveToConfigFile(currentCamera.CameraName, currentCamera.CameraPosition);
            }
            else
            {
                System.Windows.MessageBox.Show("输入点位不能为空");

            }

        }

        private void SaveToConfigFile(string cameraName, int position)
        {
            try
            {
                var lines = File.ReadAllLines(jsFilePath);
                bool foundBlowCfg = false;
                string currentCameraString = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Contains("var blowCfg = new Object;"))
                    {
                        foundBlowCfg = true;
                    }

                    if (!foundBlowCfg)
                    {
                        // 检查是否是相机配置的开始
                        if (line.Contains("cameraCfg.name ="))
                        {
                            // 提取相机名称，例如：cameraCfg.name = "camera1";
                            var match = Regex.Match(line, @"cameraCfg\.name\s*=\s*""([^""]+)""");
                            if (match.Success)
                            {
                                currentCameraString = match.Groups[1].Value;
                            }
                        }

                        // 如果当前有正在处理的相机，并且行中包含cameraCfg.position，则进行替换
                        if (currentCameraString != null && line.Contains("cameraCfg.position ="))
                        {
                            // 如果当前相机名称匹配，则替换这一行的数字部分
                            if (currentCameraString == cameraName)
                            {
                                lines[i] = "cameraCfg.position ="+position+";";
                            }
                            currentCameraString = null;
                        }
                    }
                }

                File.WriteAllLines(jsFilePath, lines);
                CurrentPositionBox.Text = position.ToString();
                currentCamera.CameraPosition = position;
                System.Windows.MessageBox.Show("保存"+position+"成功");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"更新失败: {ex.Message}");
            }
        }

        // 在每个点位变更后强制更新图像UpdateImageUI(null)

        private void ToCCD1_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD1Pos, 0);
        }

        private void ToCCD2_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD2Pos, 0);
        }

        private void ToCCD3_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD3Pos, 0);
        }

        private void ToCCD4_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD4Pos, 0);
        }

        private void ToCCD5_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD5Pos, 0);
        }

        private void ToCCD6_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD6Pos, 0);
        }

        private void ToCCD7_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD7Pos, 0);
        }

        private void ToCCD8_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, _CCD8Pos, 0);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Left_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, 5, 0);

        }

        private void Right_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Position_Net(0, -5, 0);
        }

        
    }
}


