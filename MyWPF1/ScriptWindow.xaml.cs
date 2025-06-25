// ScriptWindow.xaml.cs
using HalconDotNet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static System.Windows.Forms.AxHost;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    public partial class ScriptWindow : Window
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private readonly HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        private readonly Dictionary<int, ObjectState> _objectStates
        = new Dictionary<int, ObjectState>();

        // Named Pipe 名称
        private const string PipeName = "ImagePipe";
        // 回传给 C++ 的管道
        private const string ResultPipeName = "HalconResultPipe";

        public ScriptWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化脚本列表
            Scripts = new ObservableCollection<string>[7];
            for (int i = 0; i < 7; i++)
                Scripts[i] = new ObservableCollection<string>();

            // 初始化 Halcon 引擎
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");

            // 异步启动管道监听
            _ = Task.Run(() => ListenForImagesAsync());
        }

        private async Task ListenForImagesAsync()
        {
            while (true)
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                Debug.WriteLine("[Pipe] 等待连接...");
                await server.WaitForConnectionAsync();
                Debug.WriteLine("[Pipe] 客户端已连接");

                try
                {
                    using var reader = new BinaryReader(server);

                    // 1) 读头部元数据
                    int cameraIndex = reader.ReadInt32();
                    int objectId = reader.ReadInt32();
                    int width = reader.ReadInt32();
                    int height = reader.ReadInt32();
                    int channels = reader.ReadInt32();

                    // 2) 读图像字节
                    int byteCount = width * height * channels;
                    byte[] buffer = reader.ReadBytes(byteCount);

                    Debug.WriteLine(
                      $"[Pipe] 收到 图像：Cam={cameraIndex}, Obj={objectId}, {width}×{height}×{channels}"
                    );

                    // 3) 构造 HImage
                    HImage image;
                    if (channels == 1)
                        image = new HImage("byte", width, height, buffer[0]);
                    else
                    {
                        image = new HImage();
                        image.GenImageInterleaved(buffer[0], "rgb", width, height, -1,
                                                 "byte", width, height, 0, 0, -1, 0);
                    }

                    // 4) 在 UI 线程或调度队列中执行脚本
                    await Dispatcher.InvokeAsync(() =>
                        ProcessScripts(cameraIndex, objectId, image),
                        DispatcherPriority.Background
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Pipe Error] {ex.Message}");
                }
                finally
                {
                    if (server.IsConnected)
                        server.Disconnect();
                }
            }
        }

        private void ProcessScripts(int cameraIndex, int objectId, HImage image)
        {
            bool allOk = true;

            foreach (var script in Scripts[cameraIndex])
            {
                try
                {
                    // 设置搜索路径 & 加载脚本
                    _engine.SetProcedurePath(Path.GetDirectoryName(script));
                    var program = new HDevProgram(script);
                    var procedure = new HDevProcedure(program, "LargeDefect1");
                    var call = new HDevProcedureCall(procedure);

                    // 传入图像
                    call.SetInputIconicParamObject("Image", image);
                    // 如果过程需要用 objectId/cameraIndex 也可传入：
                    // call.SetInputCtrlParamTuple("CameraIndex", new HTuple(cameraIndex));
                    // call.SetInputCtrlParamTuple("ObjectId", new HTuple(objectId));

                    // 执行
                    call.Execute();

                    // 读取输出
                    HTuple isOk = call.GetOutputCtrlParamTuple("IsOk");
                    if (isOk.I != 1)
                        allOk = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Halcon Error] {ex.Message}");
                    allOk = false;
                }
            }

            if (!_objectStates.TryGetValue(objectId, out var state))
            {
                state = new ObjectState();
                _objectStates[objectId] = state;
            }

            if (!allOk)
                state.HasNg = true;

            // 只有当 cameraIndex==6（即 7号相机）时，才发结果
            if (cameraIndex == 6)
            {
                // 最终 OK 当且仅当整个流程都没出现过 NG
                bool finalOk = !state.HasNg;

                // 1) 回调父窗口统计
                CameraResultReported?.Invoke(this, new CameraResultEventArgs
                {
                    CameraIndex = cameraIndex,
                    IsOk = finalOk
                });

                // 2) 返送给 C++，只带 objectId 和结果
                SendResultToCpp(objectId, finalOk);

                // 3) 清理状态，以防内存泄露
                _objectStates.Remove(objectId);
            }

            Debug.WriteLine(
              $"[Done] Cam={cameraIndex}, Obj={objectId} => {(allOk ? "OK" : "NG")}"
            );
        }

        private void SendResultToCpp(int objectId, bool isOk)
        {
            try
            {
                using var client = new NamedPipeClientStream(
                    ".", ResultPipeName, PipeDirection.Out);

                client.Connect(2000);
                using var writer = new BinaryWriter(client);

                // 按约定，只发送 objectId(int32) 和 result(int32:1=OK,0=NG)
                writer.Write(objectId);
                writer.Write(isOk ? 1 : 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendResult Error] {ex.Message}");
            }
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

    class ObjectState
    {
        public bool HasNg { get; set; } = false;   // 是否已有 NG
    }
}
