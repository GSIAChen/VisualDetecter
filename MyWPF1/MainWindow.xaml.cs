using HalconDotNet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MyWPF1
{
    // MainWindow.xaml.cs
    public partial class MainWindow : Window
    {
        public string MaterialName { get; private set; }
        public string BatchNumber { get; private set; }
        public int BatchQuantity { get; private set; }
        public static int CameraCount { get; } = 12;
        private ScriptWindow _scriptWindow;

        public ObservableCollection<CameraStat> Stats { get; } =
            new ObservableCollection<CameraStat>(
                Enumerable.Range(1, CameraCount).Select(i => new CameraStat(i))
            );

        // 还可以给一个总计项放在索引 11
        public CameraStat TotalStat { get; } = new CameraStat(13);

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        // 示例事件处理（需要时添加）
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 弹出我们准备好的对话框
            //var dlg = new BatchInfoDialog
            //{
            //    Owner = this,
            //    WindowStartupLocation = WindowStartupLocation.CenterOwner
            //};

            //if (dlg.ShowDialog() == true)
            //{
            //    // 用户点 OK，读取它暴露的属性
            //    MaterialName = dlg.MaterialName;
            //    BatchNumber = dlg.BatchNumber;
            //    BatchQuantity = dlg.BatchQuantity;

            _scriptWindow._tcpServer.SendStartSignal();
            //}
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
                //_scriptWindow._tcpServer.CameraResultReported += OnCameraResultReported;
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

        //private void OnCameraResultReported(object? sender, CameraResultEventArgs e)
        //{
        //    // Must marshal back onto UI thread
        //    Dispatcher.Invoke(() =>
        //    {
        //        Trace.WriteLine($"Updating result {e.CameraIndex} Result: {(e.IsOk ? "OK" : "NG")}");
        //        if (e.CameraIndex != 11) { 
        //            var stat = Stats[e.CameraIndex];
        //            if (e.IsOk) stat.OkCount++;
        //            else stat.NgCount++;
        //        }
        //        else
        //        {
        //            if (e.IsOk) TotalStat.OkCount++;
        //            else TotalStat.NgCount++;
        //        }
        //    });
        //}

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
                e.Image.Dispose();
            });
        }

        private void OnAllStatsReported(object sender, AllStatsEventArgs e)
        {
            var arr = e.Stats;  // now just a shallow array of your real CameraStat objects
            Dispatcher.BeginInvoke(() =>
            {
                for (int i = 0; i < CameraCount; i++)
                {
                    Stats[i].OkCount = arr[i].OkCount;
                    Stats[i].NgCount = arr[i].NgCount;
                    Stats[i].ReCount = arr[i].ReCount;
                }
                TotalStat.OkCount = arr[CameraCount].OkCount;
                TotalStat.NgCount = arr[CameraCount].NgCount;
                TotalStat.ReCount = arr[CameraCount].ReCount;
            }, DispatcherPriority.Render);
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