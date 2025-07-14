using HalconDotNet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MyWPF1
{
    // MainWindow.xaml.cs
    public partial class MainWindow : Window
    {
        public string MaterialName { get; private set; }
        public string BatchNumber { get; private set; }
        public int BatchQuantity { get; private set; }
        private ScriptWindow _scriptWindow;

        // 你原来的 7 个机位 Stats
        public ObservableCollection<CameraStat> Stats { get; }
            = new ObservableCollection<CameraStat>(
                Enumerable.Range(0, 7).Select(i => new CameraStat(i + 1))
            );

        // 你原来的 TotalStat
        public CameraStat TotalStat { get; }
            = new CameraStat(0) {};

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        // 示例事件处理（需要时添加）
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 弹出我们准备好的对话框
            var dlg = new BatchInfoDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dlg.ShowDialog() == true)
            {
                // 用户点 OK，读取它暴露的属性
                MaterialName = dlg.MaterialName;
                BatchNumber = dlg.BatchNumber;
                BatchQuantity = dlg.BatchQuantity;

                _scriptWindow._tcpServer.SendStartSignal();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _scriptWindow._tcpServer.SendStopSignal();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scriptWindow == null)
            {
                _scriptWindow = new ScriptWindow();
                _scriptWindow._tcpServer.AllStatsReported += OnAllStatsReported;
                _scriptWindow._tcpServer.ImageReceived += OnImageReceived;
                _scriptWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _scriptWindow.ShowInTaskbar = false;
                _scriptWindow.Show();
            }
            else
            {
                _scriptWindow.Activate();
            }
        }

        private void AlgorithmButton_Click(object sender, RoutedEventArgs e)
        {
            var algorithmWindow = new AlgorithmWindow
            {
                Owner = this // 设置父窗口
            };
            algorithmWindow.Show();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingWindow
            {
                Owner = this // 设置父窗口
            };
            settingsWindow.Show();
        }

        private void LaunchLabelingTool_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 获取当前程序所在目录
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;

                // 2. 拼出目标程序的完整路径
                string labelerExe = Path.Combine(exeDir, "X-AnyLabeling-CPU.exe");

                if (!File.Exists(labelerExe))
                {
                    MessageBox.Show($"找不到：{labelerExe}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"启动失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        /**
        private void OnCameraResultReported(object? sender, CameraResultEventArgs e)
        {
            // Must marshal back onto UI thread
            Dispatcher.Invoke(() =>
            {
                Trace.WriteLine($"Updating result {e.CameraIndex} Result: {(e.IsOk ? "OK" : "NG")}");
                if (e.CameraIndex != 8) { 
                    var stat = Stats[e.CameraIndex];
                    if (e.IsOk) stat.OkCount++;
                    else stat.NgCount++;
                }
                else
                {
                    if (e.IsOk) TotalStat.OkCount++;
                    else TotalStat.NgCount++;
                }
            });
        }**/

        private void OnAllStatsReported(object sender, AllStatsEventArgs e)
        {
            // e.Stats 长度 8：0–6 = 相机 1–7，7 = 总计
            var arr = e.Stats;
            Dispatcher.Invoke(() =>
            {
                // 更新前 7 个
                for (int i = 0; i < 7; i++)
                {
                    Stats[i].OkCount = arr[i].OkCount;
                    Stats[i].NgCount = arr[i].NgCount;
                    Stats[i].ReCount = arr[i].ReCount;
                    // TotalCount、Accuracy 在 VM 里是只读自动计算的
                }
                // 更新总计（index 7）
                TotalStat.OkCount = arr[7].OkCount;
                TotalStat.NgCount = arr[7].NgCount;
                TotalStat.ReCount = arr[7].ReCount;
            });
        }

        private void OnImageReceived(object? sender, ImageReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Route to the corresponding HWindowControl
                HWindowControl target = e.CameraIndex switch
                {
                    1 => windowControl1,
                    2 => windowControl2,
                    3 => windowControl3,
                    4 => windowControl4,
                    5 => windowControl5,
                    6 => windowControl6,
                    _ => null
                };
                if (target == null || e == null) { return; }

                HOperatorSet.GetImageSize(e.Image, out HTuple imgWidth, out HTuple imgHeight);
                int w = imgWidth.I;
                int h = imgHeight.I;
                HOperatorSet.SetPart(
                    target.HalconWindow,
                    0,
                    0,
                    h - 1,
                    w - 1
                );
                target.HalconWindow.DispColor(e.Image);
            });
        }
    }

    public class NoMouseHWindowControl : HWindowControl
    {
        // 以下函数特意留空，避免 HWindowControl 的默认鼠标事件处理逻辑
        protected override void OnMouseMove(MouseEventArgs e)
        {
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
        }
    }

    public class CameraStat : INotifyPropertyChanged
    {
        public int CameraIndex { get; set; } // 0–7 对应 Camera1…Camera7，8 用于总计
        private int _okCount;
        public int OkCount
        {
            get => _okCount;
            set
            {
                if (_okCount == value) return;
                _okCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(Accuracy));
            }
        }

        private int _ngCount;
        public int NgCount
        {
            get => _ngCount;
            set
            {
                if (_ngCount == value) return;
                _ngCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(Accuracy));
            }
        }

        private int _reCount;
        public int ReCount
        {
            get => _reCount;
            set
            {
                if (_reCount == value) return;
                _reCount = value;
                OnPropertyChanged();
            }
        }
        public int TotalCount => OkCount + NgCount;
        public double Accuracy => TotalCount == 0 ? 0 : (double)OkCount / TotalCount;
        public String DisplayHeader { get; set; }

        public CameraStat(int cameraIndex)
        {
            CameraIndex = cameraIndex;
            DisplayHeader = cameraIndex switch
            {
                1 => "相机1统计",
                2 => "相机2统计",
                3 => "相机3统计",
                4 => "相机4统计",
                5 => "相机5统计",
                6 => "相机6统计",
                7 => "相机7统计",
                _ => $"Camera{cameraIndex}"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AllStatsEventArgs : EventArgs
    {
        // 索引 0–6 对应 Camera1…Camera7，索引 7 用作“总计”
        public CameraStat[] Stats { get; }
        public AllStatsEventArgs(CameraStat[] stats) => Stats = stats;
    }
}