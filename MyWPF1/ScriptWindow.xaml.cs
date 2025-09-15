// ScriptWindow.xaml.cs
using HalconDotNet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window, INotifyPropertyChanged
    {
        private int _cameraCount = 6;
        public int CameraCount
        {
            get => _cameraCount;
            set
            {
                if (_cameraCount != value)
                {
                    _cameraCount = value;
                    OnPropertyChanged();

                    // ** 把变化通知给 TcpDuplexServer **
                    _tcpServer.CameraCount = value;
                }
            }
        }
        private bool _saveNG = false;
        public bool SaveNGImage
        {
            get => _saveNG;
            set
            {
                if (_saveNG != value)
                {
                    _saveNG = value;
                    OnPropertyChanged();

                    // ** 把变化通知给 TcpDuplexServer **
                    _tcpServer.SaveNG = value;
                }
            }
        }
        public static ObservableCollection<int> CamNoOptions { get; } =
            new ObservableCollection<int> { 6, 10, 12 };
        private readonly HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        public TcpDuplexServer _tcpServer;
        public ICommand DeleteScriptCommand { get; }

        public static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (current is T typed)
                    return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        public ScriptWindow()
        {
            InitializeComponent();
            DataContext = this;
            Scripts = new ObservableCollection<string>[MainWindow.CameraCount];
            for (int i = 0; i < MainWindow.CameraCount; i++)
                Scripts[i] = new ObservableCollection<string>();

            Trace.WriteLine("Opening TCP Server!");
            _tcpServer = new TcpDuplexServer(Scripts, 8001, _cameraCount, _saveNG);
            //_ = StartListeningBackground();
            _ = _tcpServer.StartAsync();
        }

        // 单独的后台方法：预热 + 启动服务器，并捕获异常
        private async Task StartListeningBackground()
        {
            try
            {
                // 如果你希望在构造完成后立即预热并启动（脚本已准备好），则直接调用：
                await _tcpServer.PrewarmAllScriptsAsync().ConfigureAwait(false);

                // StartAsync 是长期运行的异步方法，await 它将让这个 Task 表示服务器的生命周期
                await _tcpServer.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 任何异常都在此记录，不会被吞掉
                Trace.WriteLine($"[ScriptWindow] StartListeningBackground failed: {ex}");
                // 你也可以在UI上通知用户：
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
        }

        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;
            int index = Convert.ToInt32(fe.Tag);

            var dlg = new OpenFileDialog
            {
                Filter = "HDevelop 脚本 (*.hdev)|*.hdev",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                Scripts[index].Add(path);

                // fire-and-forget 地预热该脚本（不要阻塞 UI）
                try
                {
                    _ = _tcpServer?.PrewarmScriptAsync(path);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("[LoadScript_Click] Prewarm call failed: " + ex);
                }
            }
        }

        //private void LoadScript_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!(sender is FrameworkElement fe)) return;
        //    int index = Convert.ToInt32(fe.Tag);

        //    var dlg = new OpenFileDialog
        //    {
        //        Filter = "HDevelop 脚本 (*.hdev)|*.hdev",
        //        Multiselect = false
        //    };
        //    if (dlg.ShowDialog() == true)
        //    { 
        //        Scripts[index].Add(dlg.FileName);
        //    }
        //}

        private void DeleteScript_Click(object sender, RoutedEventArgs e)
        {
            // 1) Which script path?
            if (sender is not MenuItem mi || mi.CommandParameter is not string scriptPath)
                return;

            // 2) Which TextBlock was right‑clicked?
            if (mi.Parent is not ContextMenu cm ||
                cm.PlacementTarget is not DependencyObject placed)
                return;

            // 3) Find the ListBoxItem container
            var lbi = FindAncestor<ListBoxItem>(placed);
            if (lbi == null)
                return;

            // 4) Which ListBox contains this item?
            if (ItemsControl.ItemsControlFromItemContainer(lbi) is not System.Windows.Controls.ListBox listBox)
                return;

            // 5) Get the camera index from the Tag
            if (listBox.Tag is not string tagString || !int.TryParse(tagString, out var camIndex))
                return;

            // 6) Remove from exactly that Scripts[camIndex]
            var scripts = Scripts[camIndex];
            if (scripts.Contains(scriptPath))
                scripts.Remove(scriptPath);
        }

        /// 把 Scripts 保存到一个 JSON 文件里
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            // 1. 弹出文件保存对话框
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "脚本配置 (*.json)|*.json",
                FileName = "scripts_config.json"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // 2. 把 Scripts 转成 List<List<string>>
                var data = Scripts
                    .Select(obs => obs.ToList())
                    .ToList();

                // 3. 序列化并写文件
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(dlg.FileName, json, Encoding.UTF8);

                MessageBox.Show("保存成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 从 JSON 文件加载 Scripts 配置
        private async void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "脚本配置 (*.json)|*.json",
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<List<List<string>>>(json);

                if (data == null || data.Count != Scripts.Length)
                    throw new InvalidDataException("脚本配置格式不正确。");

                for (int i = 0; i < Scripts.Length; i++)
                {
                    Scripts[i].Clear();
                    foreach (var scriptPath in data[i])
                        Scripts[i].Add(scriptPath);
                }

                // 统一预热（等待完成）
                if (_tcpServer != null)
                {
                    // 可在 UI 上显示进度/禁用按钮等
                    await _tcpServer.PrewarmAllScriptsAsync().ConfigureAwait(false);
                }

                // 回到 UI 线程显示提示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("加载并预热成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    //public class ObjectState
    //{
    //    private readonly Dictionary<int, bool> _results = new Dictionary<int, bool>();
    //    private readonly object _locker = new();

    //    public bool SetResult(int cameraIndex, bool isOk, int requiredCount)
    //    {
    //        lock (_locker)
    //        {
    //            if (!_results.ContainsKey(cameraIndex))
    //            {
    //                _results[cameraIndex] = isOk;
    //                return _results.Count == requiredCount;
    //            }
    //            return false;
    //        }
    //    }

    //    public bool GetFinalOk()
    //    {
    //        lock (_locker)
    //        {
    //            // 只要有一个 false 就 NG
    //            return _results.Values.All(v => v);
    //        }
    //    }
    //}

    public class ObjectState
    {
        private readonly Dictionary<int, bool> _results = new Dictionary<int, bool>();
        private readonly object _locker = new();
        public DateTime FirstSeenUtc { get; private set; } = DateTime.MinValue;

        public int Count
        {
            get
            {
                lock (_locker) { return _results.Count; }
            }
        }

        public bool SetResult(int cameraIndex, bool isOk, int requiredCount)
        {
            lock (_locker)
            {
                if (FirstSeenUtc == DateTime.MinValue) FirstSeenUtc = DateTime.UtcNow;
                if (!_results.ContainsKey(cameraIndex))
                {
                    _results[cameraIndex] = isOk;
                    return _results.Count >= requiredCount;
                }
                return false;
            }
        }

        public bool GetFinalOk()
        {
            lock (_locker)
            {
                if (_results.Count == 0) return false; // 没有结果默认 NG
                return _results.Values.All(v => v);
            }
        }

        public bool IsStale(TimeSpan ttl)
        {
            if (FirstSeenUtc == DateTime.MinValue) return false;
            return (DateTime.UtcNow - FirstSeenUtc) > ttl;
        }

        public int[] SeenCameras
        {
            get { lock (_locker) { return _results.Keys.ToArray(); } }
        }
    }


    public class CameraResultEventArgs : EventArgs
    {
        public int CameraIndex { get; set; }
        public bool IsOk { get; set; }
    }

    public class ImageReceivedEventArgs : EventArgs
    {
        public int CameraIndex { get; }
        public HImage Image { get; }
        public int ObjectId { get; }

        public ImageReceivedEventArgs(int cameraIndex, int objectId, HImage image)
        {
            CameraIndex = cameraIndex;
            ObjectId = objectId;
            Image = image;
        }
    }

    public class AllStatsEventArgs : EventArgs
    {
        public IReadOnlyList<CameraStat> Stats { get; }

        public AllStatsEventArgs(IReadOnlyList<CameraStat> stats)
        {
            Stats = stats;
        }
    }
}

