// ScriptWindow.xaml.cs
using HalconDotNet;
using Microsoft.Win32;
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
using System.Windows.Media.Media3D;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window, INotifyPropertyChanged
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<AllStatsEventArgs> AllStatsReported;
        public static int CameraNo = 5;
        public static ObservableCollection<int> CamNoOptions { get; } =
            new ObservableCollection<int> { 5, 6, 10, 12 };
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

        public int getCameraNo() { return CameraNo; }

        public ScriptWindow()
        {
            InitializeComponent();
            // —— 1. WPF 绑定上下文 —— 
            DataContext = this;

            // —— 2. 初始化 HALCON 引擎 —— 
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");

            // —— 3. 初始化脚本列表 —— 
            Scripts = new ObservableCollection<string>[MainWindow.CameraCount];
            for (int i = 0; i < MainWindow.CameraCount; i++)
                Scripts[i] = new ObservableCollection<string>();

            Trace.WriteLine("Opening TCP Server!");
            // —— 4. 启动 TCP 双工服务器 —— 
            _tcpServer = new TcpDuplexServer(Scripts, 8001, CameraNo);
            // ** Wire server → window propagation **

            _ = _tcpServer.StartAsync(); // 异步启动，不阻塞 UI
        }

        // 下面是“加载脚本”按钮的逻辑，保持不变：
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
                Scripts[index].Add(dlg.FileName);
            }
        }

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
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "脚本配置 (*.json)|*.json",
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // 1. 读文件并反序列化
                var json = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<List<List<string>>>(json);

                if (data == null || data.Count != Scripts.Length)
                    throw new InvalidDataException("脚本配置格式不正确。");

                // 2. 清空现有列表并依序填回
                for (int i = 0; i < Scripts.Length; i++)
                {
                    Scripts[i].Clear();
                    foreach (var scriptPath in data[i])
                        Scripts[i].Add(scriptPath);
                }

                MessageBox.Show("加载成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class ObjectState
    {
        private readonly Dictionary<int, bool> _results = new Dictionary<int, bool>();
        private readonly object _locker = new();

        public bool SetResult(int cameraIndex, bool isOk, int requiredCount)
        {
            lock (_locker)
            {
                if (!_results.ContainsKey(cameraIndex))
                {
                    _results[cameraIndex] = isOk;
                    return _results.Count == requiredCount;
                }
                return false;
            }
        }

        public bool GetFinalOk()
        {
            lock (_locker)
            {
                // 只要有一个 false 就 NG
                return _results.Values.All(v => v);
            }
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

