using System;
using System.Collections.Generic;
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
        public SettingWindow()
        {
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


