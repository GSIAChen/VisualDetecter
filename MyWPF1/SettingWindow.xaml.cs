using MCDLL_NET;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

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

        public SettingWindow()
        {
            CMCDLL_NET_Sorting.MCF_Sorting_Init_Net();
            CMCDLL_NET_Sorting.MCF_Open_Net(1, ref stationNum, ref stationType);
            CMCDLL_NET_Sorting.MCF_Set_Servo_Enable_Net(0, connected, 0);
            CMCDLL_NET_Sorting.MCF_Set_Output_Bit_Net(0, connected, 0);
            InitializeComponent();
            this.DataContext = this;
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


