// ScriptWindow.xaml.cs
using HalconDotNet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window, INotifyPropertyChanged
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        private readonly Dictionary<int, ObjectState> _objectStates
            = new Dictionary<int, ObjectState>();
        private readonly TcpDuplexServer _tcpServer;

        // 新增属性用于前端绑定
        private string[] _executionStatus = new string[7];
        private string[] _detectionResults = new string[7];
        private string _connectionStatus = "未连接";
        private bool _isConnected = false;
        private string _overallStatus = "就绪";
        private int _processedObjects = 0;

        public string[] ExecutionStatus
        {
            get => _executionStatus;
            set
            {
                _executionStatus = value;
                OnPropertyChanged();
            }
        }

        public string[] DetectionResults
        {
            get => _detectionResults;
            set
            {
                _detectionResults = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public string OverallStatus
        {
            get => _overallStatus;
            set
            {
                _overallStatus = value;
                OnPropertyChanged();
            }
        }

        public int ProcessedObjects
        {
            get => _processedObjects;
            set
            {
                _processedObjects = value;
                OnPropertyChanged();
            }
        }

        public ScriptWindow()
        {
            InitializeComponent();

            // —— 1. WPF 绑定上下文 —— 
            DataContext = this;

            // —— 2. 初始化 HALCON 引擎 —— 
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");

            // —— 3. 初始化脚本列表和状态 —— 
            Scripts = new ObservableCollection<string>[7];
            for (int i = 0; i < 7; i++)
            {
                Scripts[i] = new ObservableCollection<string>();
                _executionStatus[i] = "就绪";
                _detectionResults[i] = "等待";
            }

            Debug.WriteLine("Opening TCP Server!");
            // —— 4. 启动 TCP 双工服务器 —— 
            _tcpServer = new TcpDuplexServer(_engine, Scripts, _objectStates, 8001);
            _tcpServer.CameraResultReported += OnCameraResultReported;
            _tcpServer.ImageReceived += OnImageReceived;
            _ = _tcpServer.StartAsync(); // 异步启动，不阻塞 UI

            // 更新连接状态
            UpdateConnectionStatus(true);
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "已连接" : "未连接";
        }

        private void OnImageReceived(object sender, ImageReceivedEventArgs e)
        {
            // 更新执行状态
            ExecutionStatus[e.CameraIndex] = "处理中...";
            OnPropertyChanged(nameof(ExecutionStatus));
        }

        private void OnCameraResultReported(object sender, CameraResultEventArgs e)
        {
            if (e.CameraIndex == 8) // 总体结果
            {
                OverallStatus = e.IsOk ? "检测通过" : "检测失败";
                ProcessedObjects++;
            }
            else // 单个相机结果
            {
                DetectionResults[e.CameraIndex] = e.IsOk ? "OK" : "NG";
                ExecutionStatus[e.CameraIndex] = "完成";
                OnPropertyChanged(nameof(DetectionResults));
                OnPropertyChanged(nameof(ExecutionStatus));
            }
        }

        // 下面是"加载脚本"按钮的逻辑，保持不变：
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
                ExecutionStatus[index] = "已加载";
                OnPropertyChanged(nameof(ExecutionStatus));
            }
        }

        // 新增删除脚本功能
        private void DeleteScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string scriptPath)
            {
                // 找到对应的相机索引
                for (int i = 0; i < Scripts.Length; i++)
                {
                    if (Scripts[i].Contains(scriptPath))
                    {
                        Scripts[i].Remove(scriptPath);
                        if (Scripts[i].Count == 0)
                        {
                            ExecutionStatus[i] = "就绪";
                            OnPropertyChanged(nameof(ExecutionStatus));
                        }
                        break;
                    }
                }
            }
        }

        private void RunScripts_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;
            int index = Convert.ToInt32(fe.Tag);

            if (Scripts[index].Count == 0)
            {
                System.Windows.MessageBox.Show("请先加载脚本！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 更新执行状态
            ExecutionStatus[index] = "等待图像...";
            DetectionResults[index] = "等待";
            OnPropertyChanged(nameof(ExecutionStatus));
            OnPropertyChanged(nameof(DetectionResults));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            // 清理资源
            if (_tcpServer != null)
            {
                _tcpServer.CameraResultReported -= OnCameraResultReported;
                _tcpServer.ImageReceived -= OnImageReceived;
            }
            base.OnClosed(e);
        }
    }

    public class CameraResultEventArgs : EventArgs
    {
        public int CameraIndex { get; set; }
        public bool IsOk { get; set; }
    }

    public class ObjectState
    {
        // 7 个机位的检测结果，初始化为 null/未填
        public bool?[] Results { get; } = new bool?[7];

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
                return CountCompleted == 7;
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

}

