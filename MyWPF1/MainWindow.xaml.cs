using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MyWPF1
{
    // MainWindow.xaml.cs
    public partial class MainWindow : Window
    {
        public ObservableCollection<CameraStat> Stats { get; } =
            new ObservableCollection<CameraStat>(
                Enumerable.Range(1, 7).Select(i => new CameraStat(i))
            );

        // 还可以给一个总计项放在索引 7
        public CameraStat TotalStat { get; } = new CameraStat(-1);

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        // 示例事件处理（需要时添加）
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 开始按钮逻辑
            System.Windows.MessageBox.Show("开始检测", "提示");
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止按钮逻辑
            System.Windows.MessageBox.Show("停止检测", "提示");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // 清料按钮逻辑 - 清空所有统计数据
            foreach (var stat in Stats)
            {
                stat.OkCount = 0;
                stat.NgCount = 0;
                stat.ReCount = 0;
            }
            TotalStat.OkCount = 0;
            TotalStat.NgCount = 0;
            TotalStat.ReCount = 0;
            System.Windows.MessageBox.Show("统计数据已清空", "提示");
        }

        private void ScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var scriptWindow = new ScriptWindow
            {
                Owner = this,
                // 订阅事件
            };
            scriptWindow.CameraResultReported += OnCameraResultReported;
            scriptWindow.ShowDialog();
        }

        private void AlgorithmButton_Click(object sender, RoutedEventArgs e)
        {
            var algorithmWindow = new AlgorithmWindow
            {
                Owner = this // 设置父窗口
            };
            algorithmWindow.ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingWindow
            {
                Owner = this // 设置父窗口
            };
            settingsWindow.ShowDialog();
        }

        private void LaunchLabelingTool_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 获取当前程序所在目录
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                // 2. 拼出目标程序的完整路径
                string labelerExe = System.IO.Path.Combine(exeDir, "X-AnyLabeling-CPU.exe");

                if (!File.Exists(labelerExe))
                {
                    System.Windows.MessageBox.Show($"找不到：{labelerExe}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. 启动外部程序
                var psi = new ProcessStartInfo
                {
                    FileName = labelerExe,
                    WorkingDirectory = exeDir,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止按钮逻辑
        }

        private void ProductStartButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止按钮逻辑
        }

        private void ProductStopButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止按钮逻辑
        }

        private void OnCameraResultReported(object sender, CameraResultEventArgs e)
        {
            var stat = Stats[e.CameraIndex];
            if (e.IsOk) stat.OkCount++; else stat.NgCount++;
            // 同时更新总计
            if (e.IsOk) TotalStat.OkCount++; else TotalStat.NgCount++;
        }
    }

    public class CameraStat : INotifyPropertyChanged
    {
        public int CameraIndex { get; }
        public string DisplayHeader => CameraIndex == -1 ? "全部机位统计" : $"相机{CameraIndex}统计";

        private int _okCount = 0;
        public int OkCount
        {
            get => _okCount;
            set 
            { 
                _okCount = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TotalCount)); 
                OnPropertyChanged(nameof(Accuracy)); 
            }
        }

        private int _ngCount = 0;
        public int NgCount
        {
            get => _ngCount;
            set 
            { 
                _ngCount = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TotalCount)); 
                OnPropertyChanged(nameof(Accuracy)); 
            }
        }

        private int _reCount = 0;
        public int ReCount
        {
            get => _reCount;
            set 
            { 
                _reCount = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TotalCount)); 
                OnPropertyChanged(nameof(Accuracy)); 
            }
        }

        // 通过 OkCount + NgCount 自动计算总数
        public int TotalCount => OkCount + NgCount;

        // 精度 = OK/(OK+NG)
        public double Accuracy => TotalCount == 0 ? 0 : (double)OkCount / TotalCount;

        public CameraStat(int index)
        {
            CameraIndex = index;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}