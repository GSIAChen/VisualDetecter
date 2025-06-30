// ScriptWindow.xaml.cs
using HalconDotNet;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        private readonly HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        private readonly Dictionary<int, ObjectState> _objectStates
            = new Dictionary<int, ObjectState>();
        private readonly TcpDuplexServer _tcpServer;


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

            Debug.WriteLine("Opening TCP Server!");
            // —— 4. 启动 TCP 双工服务器 —— 
            _tcpServer = new TcpDuplexServer(_engine, Scripts, _objectStates, 8001);
            _ = _tcpServer.StartAsync(); // 异步启动，不阻塞 UI

            // （可选）如果你还想在界面上显示一个“正在等待图像”的提示，可以在这里写
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
                Scripts[index].Add(dlg.FileName);
        }

        private void RunScripts_Click(object sender, RoutedEventArgs e)
        {
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

