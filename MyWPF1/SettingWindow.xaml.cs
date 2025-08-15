using MCDLL_NET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyWPF1
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        private ushort stationNum = 0;
        private ushort stationType = 0;

        public SettingWindow()
        {
            CMCDLL_NET_Sorting.MCF_Sorting_Init_Net();
            CMCDLL_NET_Sorting.MCF_Open_Net(2, ref stationNum, ref stationType);
            InitializeComponent();
            DataContext = new MyViewModel();
        }
    }
    public class MyViewModel : INotifyPropertyChanged
    {
        private string _RotationSpeed = "10"; // 初始化默认值
        private string _CamTriggerTime = "10"; // 初始化默认值

        public string RotationSpeed
        {
            get => _RotationSpeed;
            set
            {
                _RotationSpeed = value;
                OnPropertyChanged();
            }
        }
        public string CamTriggerTime
        {
            get => _CamTriggerTime;
            set
            {
                _CamTriggerTime = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


