using GxIAPINET;
using MCDLL_NET;
using MyWPF1.Entity;
using MyWPF1.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MyWPF1
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window, INotifyPropertyChanged
    {
        private ushort stationNum = 0;
        private ushort stationType = 2;
        private ushort connected = 0;
        private ushort disconnected = 1;
        private float _RotationSpeed;
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
                cameraParameter.CameraName = i+1+"号相机";
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
            if (CameraSelector.SelectedIndex >= 0 && CameraSelector.SelectedIndex < cameras.Count)
            {
                currentCamera = cameraParaList[CameraSelector.SelectedIndex];
                currentDevice = deviceList[CameraSelector.SelectedIndex];
                LoadCameraSettings(currentCamera);
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
                //设置水平bin模式
                cameraSDK.SetHorizontalMode(currentCamera.HorizontalMode, featureControl);
                //设置水平bin模式值
                cameraSDK.SetBinningHorizontalValue(currentCamera.HorizontalValue, featureControl);
                //设置垂直bin模式
                cameraSDK.SetVerticalMode(currentCamera.VerticalMode, featureControl);
                //设置垂直bin模式值
                cameraSDK.SetVerticalValue(currentCamera.VerticalModeValue, featureControl);
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
            if (deviceList.Count > 0)
            {
                for (int i = 0; i < deviceList.Count; i++)
                {
                    IGXDevice device = deviceList[i];
                    device.Close();
                }
            }
           
        }


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

       
    }
}


