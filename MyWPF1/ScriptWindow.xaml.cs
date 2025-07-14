// ScriptWindow.xaml.cs
using HalconDotNet;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<AllStatsEventArgs> AllStatsReported;
        private readonly HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        private readonly Dictionary<int, ObjectState> _objectStates
            = new Dictionary<int, ObjectState>();
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
            // —— 1. WPF 绑定上下文 —— 
            DataContext = this;

            // —— 2. 初始化 HALCON 引擎 —— 
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");

            // —— 3. 初始化脚本列表 —— 
            Scripts = new ObservableCollection<string>[7];
            for (int i = 0; i < 7; i++)
                Scripts[i] = new ObservableCollection<string>();

            Trace.WriteLine("Opening TCP Server!");
            // —— 4. 启动 TCP 双工服务器 —— 
            _tcpServer = new TcpDuplexServer(Scripts, _objectStates, 8001);
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
    }

    public class ObjectState
    {
        // 初始机位数量，初始化为 null/未填
        static int camNo = 5;
        public bool?[] Results { get; } = new bool?[camNo];

        // 已收到结果的机位数
        public int CountCompleted { get; private set; } = 0;

        /// <summary>
        /// 设置某个机位的结果。返回 true 表示这是第7个（最后一个）填入，允许触发最终判断。
        /// </summary>
        public bool SetResult(int cameraIndex, bool isOk)
        {
            if (Results[cameraIndex] == null)
            {
                Results[cameraIndex] = isOk;
                CountCompleted++;
                return CountCompleted == camNo;
            }
            // 如果已经有过结果（重复回调）就忽略，不算第二次
            return false;
        }

        /// <summary>
        /// 当 7 个机位都填完后，判断最终结果：只要有一个 false，就算 NG。
        /// </summary>
        public bool GetFinalOk()
            => Results.Any(r => r == false) ? false : true;
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

