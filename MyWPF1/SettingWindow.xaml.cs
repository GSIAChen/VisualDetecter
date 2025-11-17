using GxIAPINET;
using MCDLL_NET;
using MyWPF1.Entity;
using MyWPF1.Service;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MyWPF1
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : System.Windows.Window
    {
        private ushort stationNum = 0;
        private ushort stationType = 2;
        private ushort connected = 0;
        private ushort disconnected = 1;
        private float _RotationSpeed;
        private string jsFilePath = "E://program/TestEc3224l/TestEc3224l.js"; // 需要替换为实际路径  

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
        private readonly object _deviceListLock = new();
        private readonly object _cameraParaListLock = new();

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

        private async void SettingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CameraSDK cameraSDK = new CameraSDK();
            // 如果 InitCamera 很快可以直接调用；若有可能阻塞，放到后台
            await Task.Run(() => cameraSDK.InitCamera());

            // 获取设备列表（后台线程）
            var cameraList = await Task.Run(() => cameraSDK.GetAvailableCameras(true));
            int maxParallel = Math.Min(Environment.ProcessorCount, Math.Max(1, cameraList.Count));
            using var semaphore = new SemaphoreSlim(maxParallel);
            var connectTasks = cameraList.Select(async (devInfo, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // 在后台线程连接相机并读取参数（不要在此处直接操作 UI）
                    var device = cameraSDK.ConnectCamera(devInfo);
                    var featureControl = device.GetRemoteFeatureControl();
                    var cameraParameter = new CameraParameters
                    {
                        CameraName = "camera" + (index + 1),
                        ExposureTime = cameraSDK.GetExposureTime(featureControl),
                        Gain = cameraSDK.GetGain(featureControl),
                        Rate = cameraSDK.GetRate(featureControl),
                        GammaMode = cameraSDK.GetGammaMode(featureControl),
                        RedChannel = cameraSDK.getRChannels("red", featureControl),
                        GreenChannel = cameraSDK.getRChannels("green", featureControl),
                        BlueChannel = cameraSDK.getRChannels("blue", featureControl),
                        HorizontalMode = cameraSDK.GetHorizontalMode(featureControl),
                        HorizontalValue = cameraSDK.GetBinningHorizontalValue(featureControl),
                        VerticalMode = cameraSDK.GetVerticalMode(featureControl),
                        VerticalValue = cameraSDK.GetVerticalValue(featureControl)
                    };
                    if (cameraParameter.GammaMode == "User")
                        cameraParameter.Gamma = cameraSDK.GetGamma(featureControl);

                    // 把 device 和参数 安全地加入到共享集合
                    lock (_deviceListLock) deviceList.Add(device);
                    lock (_cameraParaListLock) cameraParaList.Add(cameraParameter);
                }
                catch (Exception ex)
                {
                    // 记录但不中断其他相机连接
                    Trace.WriteLine($"Connect camera #{index + 1} failed: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();
            if (deviceList.Count > 0)
            {
                Trace.WriteLine($"Total connected cameras: {deviceList.Count}");
                for (int i = 0; i < deviceList.Count; i++)
                {
                    IGXDevice device = deviceList[i];
                    device.GetRemoteFeatureControl().GetEnumFeature("TriggerMode").SetValue("Off");
                }
            }
            // 初始化UI
            await Task.WhenAll(connectTasks);
            await InitializeCameraSelectorAsync();
            // 读取JS文件  
            if (File.Exists(jsFilePath))
            {
                string jsContent = File.ReadAllText(jsFilePath);
                var cameraPositions = ParseCameraPositions(jsContent);
                // 假设当前选中的是camera1  
                if (cameraPositions.TryGetValue(currentCamera.CameraName, out int position))
                {
                    CamPositionBox.Text = position.ToString();
                    currentCamera.CameraPosition = position;
                }
            }
            // 确保 UI 的延后工作（若有）也处理完
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            _isCapturing = false;
            await ApplyLoadedSettingsIfAny();
            await ApplySettingsAsync();
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

        // 将 InitializeCameraSelector 改成返回 Task 的版本，内部在 UI 线程执行
        private Task InitializeCameraSelectorAsync()
        {
            return Dispatcher.InvokeAsync(() =>
            {
                CameraSelector.Items.Clear();

                // 使用本地快照避免并发读取问题
                List<CameraParameters> snapshot;
                lock (_cameraParaListLock)
                {
                    snapshot = cameraParaList.ToList();
                }

                foreach (CameraParameters cameraParameters in snapshot)
                {
                    CameraSelector.Items.Add(cameraParameters.CameraName);
                }

                if (CameraSelector.Items.Count > 0)
                {
                    CameraSelector.SelectedIndex = 0;

                    // 从受保护的集合读取当前 camera/device
                    lock (_cameraParaListLock)
                    {
                        currentCamera = cameraParaList[CameraSelector.SelectedIndex];
                    }
                    lock (_deviceListLock)
                    {
                        currentDevice = deviceList[CameraSelector.SelectedIndex];
                    }

                    // LoadCameraSettings 本身操作 UI，保持在 UI 线程调用
                    LoadCameraSettings(currentCamera);
                }
            }, DispatcherPriority.Normal).Task;
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
            VerticalSlider.Value = camera.VerticalValue;
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
            if (currentDevice == null || _isCapturing==true)
            {
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
                Trace.WriteLine("Image capture started.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动图像采集失败: {ex.Message}");
                Trace.WriteLine($"StartImageCapture exception: {ex.Message}");
            }
        }

        public void OnFrameCallbackFun(object objPara, IFrameData objIFrameData)
        {
            try
            {
                if (0 == objIFrameData.GetStatus())
                {
                    // 转换图像为Bitmap
                    Bitmap bitmap = ConvertGxImageToBitmap(objIFrameData);
                    // 将Bitmap加入队列，不在此处进行UI操作
                    _bitmapQueue.Enqueue(bitmap);

                    // 如果队列处理未启动，则启动它
                    if (!_isProcessingQueue)
                    {
                        StartQueueProcessing();
                    }
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
                    }
                });
            }
        }

        // 停止图像采集
        private void StopImageCapture()
        {
            if (currentDevice == null || _isCapturing == false)
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
                Trace.WriteLine("Image capture stopped.");

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"StopImageCapture exception: {ex.Message}");
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
            PixelFormat pixelFormat = PixelFormat.Format8bppIndexed;
            GX_PIXEL_FORMAT_ENTRY gxPixelFormat = image.GetPixelFormat();


            // 根据枚举值判断像素格式
            switch (gxPixelFormat)
            {
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BGR8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_RGB8:
                    pixelFormat = PixelFormat.Format24bppRgb;
                    break;
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_BGRA8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_RGBA8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_ARGB8:
                case GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_ABGR8:
                    pixelFormat = PixelFormat.Format32bppArgb;
                    break;
                default:
                    // 默认为8位灰度格式
                    pixelFormat = PixelFormat.Format8bppIndexed;
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
            if (pixelFormat == PixelFormat.Format8bppIndexed)
            {
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
            }
            return bitmap;
        }

        // 相机选择变更事件
        private void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 停止当前采集
            if(_isCapturing==true) StopImageCapture();
            if (CameraSelector.SelectedIndex >= 0 && CameraSelector.SelectedIndex < deviceList.Count)
            {
                currentCamera = cameraParaList[CameraSelector.SelectedIndex];
                currentDevice = deviceList[CameraSelector.SelectedIndex];
                LoadCameraSettings(currentCamera);
                // 开始新相机的采集
                StartImageCapture();
            }
            // 读取JS文件  
            if (File.Exists(jsFilePath))
            {
                string jsContent = File.ReadAllText(jsFilePath);
                var cameraPositions = ParseCameraPositions(jsContent);
                // 假设当前选中的是camera1  
                if (cameraPositions.TryGetValue(currentCamera.CameraName, out int position))
                {
                    CamPositionBox.Text = position.ToString();
                    currentCamera.CameraPosition = position;
                }
            }
        }

        // 应用配置按钮点击事件
        private async void ApplySettings_Click(object sender, RoutedEventArgs e)
        {
            // 事件处理器：调用可等待的方法，并捕获异常以免抛到 UI 线程
            try
            {
                await ApplySettingsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApplySettingsAsync()
        {
            // 先从 UI 读取一次值，避免在后台线程直接访问控件
            double exposure = ExposureSlider.Value;
            int rate = (int)RateSlider.Value;
            double gamma = GammaSlider.Value;
            double gain = GainSlider.Value;
            double red = RedChannelSlider.Value;
            double green = GreenChannelSlider.Value;
            double blue = BlueChannelSlider.Value;
            string gammaMode = ((ComboBoxItem)GammaModeComboBox.SelectedItem)?.Content?.ToString() ?? "";
            string horizMode = ((ComboBoxItem)HorizontalBox.SelectedItem)?.Content?.ToString() ?? "";
            string vertMode = ((ComboBoxItem)VerticalBox.SelectedItem)?.Content?.ToString() ?? "";
            long horizVal = (long)HorizontalSlider.Value;
            long vertVal = (long)VerticalSlider.Value;

            // Update in-memory cameraParaList (quick, on UI thread, protected by lock)
            lock (_cameraParaListLock)
            {
                foreach (var cam in cameraParaList)
                {
                    cam.ExposureTime = exposure;
                    cam.Rate = rate;
                    cam.Gamma = gamma;
                    cam.Gain = gain;
                    cam.RedChannel = red;
                    cam.GreenChannel = green;
                    cam.BlueChannel = blue;
                    cam.GammaMode = gammaMode;
                    // horizontal/vertical may be deferred if capturing; still write to memory
                    cam.HorizontalMode = horizMode;
                    cam.HorizontalValue = horizVal;
                    cam.VerticalMode = vertMode;
                    cam.VerticalValue = vertVal;
                }
            }

            // Inform user we're applying settings (optional)
            var applyDialog = MessageBoxResult.None;
            try
            {
                // Run the hardware write in background to avoid blocking UI.
                var saveResult = await Task.Run(() =>
                {
                    var sdk = new CameraSDK();
                    var errors = new List<string>();

                    // iterate over cameras and devices in a safe index range
                    int count;
                    lock (_cameraParaListLock) count = cameraParaList.Count;
                    int deviceCount;
                    lock (_deviceListLock) deviceCount = deviceList.Count;
                    int n = Math.Min(count, deviceCount);

                    for (int i = 0; i < n; i++)
                    {
                        CameraParameters cam;
                        IGXDevice device;
                        lock (_cameraParaListLock) cam = cameraParaList[i];
                        lock (_deviceListLock) device = deviceList[i];

                        try
                        {
                            // get feature control for this device
                            var fc = device.GetRemoteFeatureControl();

                            // apply common parameters
                            sdk.SetExposureTime(cam.ExposureTime, fc);
                            sdk.SetRate((int)cam.Rate, fc);
                            sdk.SetGain(cam.Gain, fc);
                            sdk.SetGammaMode(cam.GammaMode, fc);
                            sdk.SetRGBChannels((int)cam.RedChannel, (int)cam.GreenChannel, (int)cam.BlueChannel, fc);
                            if (string.Equals(cam.GammaMode, "User", StringComparison.OrdinalIgnoreCase))
                            {
                                sdk.SetGamma(cam.Gamma, fc);
                            }

                            // binning / geometry: only apply if not capturing (to avoid disrupting stream)
                            if (!_isCapturing)
                            {
                                sdk.SetHorizontalMode(cam.HorizontalMode, fc);
                                sdk.SetBinningHorizontalValue(cam.HorizontalValue, fc);
                                sdk.SetVerticalMode(cam.VerticalMode, fc);
                                sdk.SetVerticalValue(cam.VerticalValue, fc);
                            }
                            else
                            {
                                // 记录跳过的设备，用于提示
                                errors.Add($"Camera {cam.CameraName}: binning deferred (capturing).");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 每台设备独立捕获异常，不影响其它设备
                            errors.Add($"Camera {i + 1} ({cam?.CameraName ?? "unknown"}): {ex.Message}");
                        }
                    }

                    return errors;
                });

                // 回到 UI 线程：保存配置到文件并显示结果
                SaveAllSettingsToJs();

                if (saveResult.Count == 0)
                {
                    MessageBox.Show("所有相机配置已成功应用并保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 汇总错误/警告，限制显示长度
                    string summary = string.Join(Environment.NewLine, saveResult);
                    if (summary.Length > 800)
                    {
                        summary = summary.Substring(0, 800) + Environment.NewLine + "...(truncated)";
                    }
                    MessageBox.Show("已应用设置，但存在以下警告/错误：" + Environment.NewLine + summary, "部分完成", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用配置时发生未处理异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCapturing_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCapturing) 
            {
                StartImageCapture();
            }
        }

        private void StopCapturing_Click(object sender, RoutedEventArgs e)
        {
            if (_isCapturing)
            {
                StopImageCapture();
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
                        device.GetRemoteFeatureControl().GetEnumFeature("TriggerMode").SetValue("On");
                        device.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("关闭相机失败");
                    }

                }
            }
            SaveAllSettingsToJs();
        }

        private void RunToHome_Click(object sender, RoutedEventArgs e)
        {
            CMCDLL_NET_Sorting.MCF_Set_Pulse_Mode_Net(0, 1, 0);
            CMCDLL_NET_Sorting.MCF_Set_EMG_Bit_Net(0, 1, 0);
            CMCDLL_NET_Sorting.MCF_JOG_Net(0, 5000, 2000, 0);
            CurrentPositionBox.Text = "0";
        }

        //点击移动
        private void RunToPos_Click(object sender, RoutedEventArgs e)
        {
            int target = int.Parse(CamPositionBox.Text);
            int current = int.Parse(CurrentPositionBox.Text);
            CMCDLL_NET_Sorting.MCF_Set_EMG_Bit_Net(0, 0, 0);
            CMCDLL_NET_Sorting.MCF_Set_Axis_Profile_Net(0, 0, 5000, 50000, 500000, 0, 0);
            CMCDLL_NET_Sorting.MCF_Uniaxial_Net(0, target-current, 1);
            CurrentPositionBox.Text = target.ToString();
        }

        private void Left_Click(object sender, RoutedEventArgs e)
        {
            string value = CurrentPositionBox.Text;
            if (value == null) return;
            CMCDLL_NET_Sorting.MCF_Set_EMG_Bit_Net(0, 0, 0);
            CMCDLL_NET_Sorting.MCF_Set_Pulse_Mode_Net(0, 1, 0);
            CMCDLL_NET_Sorting.MCF_Set_Axis_Profile_Net(0, 0, 5000, 50000, 500000, 0, 0);
            CMCDLL_NET_Sorting.MCF_Uniaxial_Net(0, -5, 1);
            int v = int.Parse(CurrentPositionBox.Text) - 5;
            CurrentPositionBox.Text = v.ToString();
        }

        private void Right_Click(object sender, RoutedEventArgs e)
        {
            string value = CurrentPositionBox.Text;
            if (value == null) return;
            CMCDLL_NET_Sorting.MCF_Set_EMG_Bit_Net(0, 0, 0);
            CMCDLL_NET_Sorting.MCF_Set_Pulse_Mode_Net(0, 1, 0);
            CMCDLL_NET_Sorting.MCF_Set_Axis_Profile_Net(0, 0, 5000, 50000, 500000, 0, 0);
            CMCDLL_NET_Sorting.MCF_Uniaxial_Net(0, 5, 1);
            int v = int.Parse(CurrentPositionBox.Text) + 5;
            CurrentPositionBox.Text = v.ToString();
        }

        //保存到js文件
        private void SavePos_Click(object sender, RoutedEventArgs e)
        {
            string value = CurrentPositionBox.Text;
            if (CurrentPositionBox.Text != null)
            {
                currentCamera.CameraPosition = int.Parse(value);
                // 方式1: 写入到共享的配置文件（推荐）
                SaveToConfigFile(currentCamera.CameraName, currentCamera.CameraPosition);
            }
            else
            {
                MessageBox.Show("输入点位不能为空");
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
                                lines[i] = $"    cameraCfg.position = {position};";
                            }
                            currentCameraString = null;
                        }
                    }
                }

                File.WriteAllLines(jsFilePath, lines);
                CurrentPositionBox.Text = position.ToString();
                currentCamera.CameraPosition = position;
                MessageBox.Show("保存" + position + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败: {ex.Message}");
            }
        }

        public class CameraSettingDto
        {
            public string CameraName { get; set; } = "";
            public double ExposureTime { get; set; }
            public double Rate { get; set; }
            public double Gain { get; set; }
            public string? GammaMode { get; set; }
            public double? Gamma { get; set; }
            public double RedChannel { get; set; }
            public double GreenChannel { get; set; }
            public double BlueChannel { get; set; }
            public string? HorizontalMode { get; set; }
            public long HorizontalValue { get; set; }
            public string? VerticalMode { get; set; }
            public long VerticalValue { get; set; }
            public int CameraPosition { get; set; }
            // 如果你愿意并且 GeometryParams 可序列化，可以在此添加：
            // public GeometryParams? Geometry { get; set; }
        }

        // -----------------------------------------------------
        // 文件路径辅助
        // -----------------------------------------------------
        private string GetCameraSettingJsPath()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "CameraSetting.js");
        }

        // -----------------------------------------------------
        // CameraParameters <-> DTO 转换
        // -----------------------------------------------------
        private CameraSettingDto ToDto(CameraParameters p)
        {
            return new CameraSettingDto
            {
                CameraName = p.CameraName,
                ExposureTime = p.ExposureTime,
                Rate = p.Rate,
                Gain = p.Gain,
                GammaMode = p.GammaMode,
                Gamma = p.Gamma,
                RedChannel = p.RedChannel,
                GreenChannel = p.GreenChannel,
                BlueChannel = p.BlueChannel,
                HorizontalMode = p.HorizontalMode,
                HorizontalValue = p.HorizontalValue,
                VerticalMode = p.VerticalMode,
                VerticalValue = p.VerticalValue,
                CameraPosition = p.CameraPosition
                // Geometry omitted by default
            };
        }

        private void FromDto(CameraParameters dst, CameraSettingDto src)
        {
            if (dst == null || src == null) return;
            dst.ExposureTime = src.ExposureTime;
            dst.Rate = src.Rate;
            dst.Gain = src.Gain;
            if (!string.IsNullOrEmpty(src.GammaMode)) dst.GammaMode = src.GammaMode;
            if (src.Gamma.HasValue) dst.Gamma = src.Gamma.Value;
            dst.RedChannel = src.RedChannel;
            dst.GreenChannel = src.GreenChannel;
            dst.BlueChannel = src.BlueChannel;
            if (!string.IsNullOrEmpty(src.HorizontalMode)) dst.HorizontalMode = src.HorizontalMode;
            dst.HorizontalValue = src.HorizontalValue;
            if (!string.IsNullOrEmpty(src.VerticalMode)) dst.VerticalMode = src.VerticalMode;
            dst.VerticalValue = src.VerticalValue;
            dst.CameraPosition = src.CameraPosition;
            // Geometry 未处理，如需恢复请自行填充
        }

        // -----------------------------------------------------
        // 读写 CameraSetting.js：保存全部 / 单个
        // -----------------------------------------------------
        private void SaveAllSettingsToJs()
        {
            string path = GetCameraSettingJsPath();

            // 快照 cameraParaList（避免并发）
            List<CameraParameters> snapshot;
            lock (_cameraParaListLock) snapshot = cameraParaList.ToList();

            var dict = snapshot.ToDictionary(
                p => p.CameraName,
                p => ToDto(p));

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(dict, options);
            string jsText = "var CameraSettings = " + json + ";";

            try
            {
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", overwrite: true);
                }
                File.WriteAllText(path, jsText, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存 CameraSetting.js 失败: {ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSingleCameraToJs(CameraParameters camera)
        {
            if (camera == null) return;
            string path = GetCameraSettingJsPath();

            var dict = LoadSettingsFromJs() ?? new Dictionary<string, CameraSettingDto>(StringComparer.OrdinalIgnoreCase);

            dict[camera.CameraName] = ToDto(camera);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            string json = JsonSerializer.Serialize(dict, options);
            string jsText = "var CameraSettings = " + json + ";";
            try
            {
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", overwrite: true);
                }
                File.WriteAllText(path, jsText, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存 CameraSetting.js 失败: {ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -----------------------------------------------------
        // 读取 CameraSetting.js 并解析到字典
        // -----------------------------------------------------
        private Dictionary<string, CameraSettingDto>? LoadSettingsFromJs()
        {
            string path = GetCameraSettingJsPath();
            if (!File.Exists(path)) return null;

            string text = File.ReadAllText(path, System.Text.Encoding.UTF8);

            // 找到最外层花括号部分
            int idx = text.IndexOf('{');
            int last = text.LastIndexOf('}');
            if (idx < 0 || last < idx) return null;

            string json = text.Substring(idx, last - idx + 1);

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = JsonSerializer.Deserialize<Dictionary<string, CameraSettingDto>>(json, options);
                return dict;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"解析 CameraSetting.js 失败: {ex.Message}");
                return null;
            }
        }

        // -----------------------------------------------------
        // 在窗口加载后调用：将文件中的配置应用到已连接相机并下发到硬件
        // -----------------------------------------------------
        private async Task ApplyLoadedSettingsIfAny()
        {
            var dict = LoadSettingsFromJs();
            if (dict == null || dict.Count == 0) return;

            // 遍历每个保存项，匹配已连接的 camera 并应用（异步下发到硬件）
            lock (_cameraParaListLock)
            {
                foreach (var kv in dict)
                {
                    string name = kv.Key;
                    var dto = kv.Value;
                    var cam = cameraParaList.FirstOrDefault(c => c.CameraName == name);
                    if (cam != null)
                    {
                        // 更新内存数据（UI 线程已经完成 LoadCameraSettings 等）
                        FromDto(cam, dto);

                        // 在后台把设置写入到相机硬件（避免阻塞 UI）
                        Task.Run(() =>
                        {
                            try
                            {
                                IGXDevice device = null;
                                lock (_deviceListLock)
                                {
                                    int idx = cameraParaList.IndexOf(cam);
                                    if (idx >= 0 && idx < deviceList.Count)
                                        device = deviceList[idx];
                                }
                                if (device != null)
                                {
                                    var fc = device.GetRemoteFeatureControl();
                                    var sdk = new CameraSDK();
                                    // 以下调用按你的 SDK 接口顺序进行映射
                                    sdk.SetExposureTime(cam.ExposureTime, fc);
                                    sdk.SetRate((int)cam.Rate, fc);
                                    sdk.SetGain(cam.Gain, fc);
                                    sdk.SetGammaMode(cam.GammaMode, fc);
                                    sdk.SetRGBChannels((int)cam.RedChannel, (int)cam.GreenChannel, (int)cam.BlueChannel, fc);
                                    if (cam.GammaMode == "User") sdk.SetGamma(cam.Gamma, fc);

                                    // binning 等较大改动，若当前正在采集则延后或短暂停采
                                    if (!_isCapturing)
                                    {
                                        sdk.SetHorizontalMode(cam.HorizontalMode, fc);
                                        sdk.SetBinningHorizontalValue(cam.HorizontalValue, fc);
                                        sdk.SetVerticalMode(cam.VerticalMode, fc);
                                        sdk.SetVerticalValue(cam.VerticalValue, fc);
                                    }
                                    else
                                    {
                                        // 如果正在采集，按你的策略：跳过或在下次停止时应用
                                        Trace.WriteLine($"正在采集，延迟应用 {cam.CameraName} 的 binning 设置。");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"将设置应用到相机 {dto.CameraName} 失败: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        Trace.WriteLine($"CameraSetting.js 包含 {name}，但当前没有已连接的同名相机。");
                    }
                }
            }

            // 如果当前 UI 已选择 camera，刷新 UI 显示
            Dispatcher.Invoke(() =>
            {
                if (currentCamera != null) LoadCameraSettings(currentCamera);
            });
        }

        // -----------------------------------------------------
        // 使用场景示例：在 Loaded 事件中调用
        // -----------------------------------------------------
        // 在 SettingWindow_Loaded 的末尾或合适位置加：
        // ApplyLoadedSettingsIfAny();

        // -----------------------------------------------------
        // 单个相机“保存”按钮事件演示
        // -----------------------------------------------------
        private void SaveCurrentCameraButton_Click(object sender, RoutedEventArgs e)
        {
            // 先把 UI 上的当前值应用到 currentCamera（你的 ApplySettings_Click 中已有逻辑可以复用）
            ApplySettings_Click(sender, e); // 先执行一次应用并同步 currentCamera 的值
            SaveSingleCameraToJs(currentCamera);
        }

        // -----------------------------------------------------
        // 全部相机保存示例（可以绑定到“保存全部”按钮）
        // -----------------------------------------------------
        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAllSettingsToJs();
        }
    }
}