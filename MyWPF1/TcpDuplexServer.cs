using HalconDotNet;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Web.Services.Description;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace MyWPF1
{
    public class TcpDuplexServer
    {
        static SieveDll.CaptureCallbackFunc _callback = OnCapture;
        // 通信速率统计字段
        private int Port;
        private const byte FrameHead = 0xFF;
        private readonly ActionBlock<byte[]> _frameProcessor;
        private int _cameraCount;
        public int CameraCount
        {
            get => _cameraCount;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException();
                _cameraCount = value;
                // （可选）当相机数量变动时，可以把所有半成品状态清空
                objectStates.Clear();
            }
        }

        // 这里只保存单个客户端连接；如果要多个并行，可以用 ConcurrentDictionary<int, TcpClient> 等
        private TcpClient _client;
        private NetworkStream _stream;
        private ObservableCollection<string>[] scripts;
        private ConcurrentDictionary<int, ObjectState> objectStates;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private readonly DispatcherTimer _reportTimer;
        private HDevEngine _engine;
        ConcurrentDictionary<string, ThreadLocal<ScriptRunner>> _runners = new();
        public event EventHandler<AllStatsEventArgs>? AllStatsReported;
        private readonly CameraStat[] _stats = Enumerable
                                       .Range(1, MainWindow.CameraCount+1)
                                       .Select(_ => new CameraStat(_))
                                       .ToArray();
        // 线程引擎池（初始化）
        private readonly ThreadLocal<HDevEngine> _threadEngine = new(() =>
        {
            var engine = new HDevEngine();
            engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            return engine;
        });
        private readonly byte[] _buffer = new byte[8 * 1024 * 1024]; // 8 MB 循环缓冲区
        private int _head = 0, _tail = 0;
        private int width = 1280, height = 1024, channels = 3;
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private readonly int _imgLen;
        private int _searchStart = 0;
        private int _frameCount;
        private System.Timers.Timer _fpsTimer;
        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern IntPtr LoadLibrary(string lpFileName);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern bool SetDllDirectory(string lpPathName);

        //public static void TryLoadDll()
        //{
        //    var handle = LoadLibrary(@"F:\tandianrong\TestEc3224l\TestEc3224l.dll");
        //    if (handle == IntPtr.Zero)
        //    {
        //        int error = Marshal.GetLastWin32Error();
        //        Trace.WriteLine("LoadLibrary failed: " + error);
        //    }
        //    else
        //    {
        //        Trace.WriteLine("DLL Loaded Successfully");
        //    }
        //}

        public void InitMonitor()
        {
            _fpsTimer = new System.Timers.Timer(1000);
            _fpsTimer.Elapsed += (s, e) =>
            {
                int count = Interlocked.Exchange(ref _frameCount, 0);
                Trace.WriteLine($"[PERF] FPS: {count}");
            };
            _fpsTimer.Start();
        }

        private ScriptRunner GetRunner(string path)
        {
            return _runners.GetOrAdd(path, p => new ThreadLocal<ScriptRunner>(() =>
                new ScriptRunner(p))).Value;
        }

        // Constructor parameter is named "port"
        public TcpDuplexServer(
            ObservableCollection<string>[] scripts,
            int port,
            int initialCameraCount
        )
        {
            this.scripts = scripts;
            this.Port = port;
            this.CameraCount = initialCameraCount;
            this.objectStates = new ConcurrentDictionary<int, ObjectState>();
            _imgLen = width * height * channels;
            //InitMonitor();
            //Console.WriteLine(Environment.GetEnvironmentVariable("Path"));

            // 设置搜索路径为 DLL 根目录
            //var h = SieveDll.testEc3224l_CreateProjectAgent("F:/tandianrong/TestEc3224l/TestEc3224l.js");

            //if (!SieveDll.testEc3224l_HandleValid(h))
            //{
            //    Console.WriteLine("Handle invalid");
            //    return;
            //}

            //SieveDll.testEc3224l_RegisterCaptrueCallBack(h, _callback, IntPtr.Zero);

            //Console.ReadKey();

            //SieveDll.testEc3224l_UnRegisterCaptrueCallBack(h);
            //SieveDll.testEc3224l_DestroyProjectAgent(h);

            // 设置Halcon引擎
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            HTuple devs;
            HTuple handle;
            HOperatorSet.QueryAvailableComputeDevices(out devs);
            HOperatorSet.OpenComputeDevice(devs, out handle);
            HOperatorSet.ActivateComputeDevice(handle);
            HTuple useInfo;
            HOperatorSet.GetSystem("parallelize_operators", out useInfo);
            Trace.WriteLine($"AOP on: {useInfo}");
            HTuple cudaInfo;
            HOperatorSet.GetSystem("cuda_devices", out cudaInfo);
            Trace.WriteLine($"Cuda devices: {cudaInfo}");
            HOperatorSet.SetSystem("parallelize_operators", "true");
            HOperatorSet.SetSystem("thread_num", Environment.ProcessorCount);
            Trace.WriteLine("CPU num = " + Environment.ProcessorCount);

            // 设置 ActionBlock 的并行度和缓冲区
            var parallelism = Environment.ProcessorCount;
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelism,
                EnsureOrdered = false
            };
            _frameProcessor = new ActionBlock<byte[]>(frame =>
            {
                ProcessFrame(frame);
            }, options);

            // 定时上报统计信息
            _reportTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(5),
            DispatcherPriority.Normal,
            (s, e) => RaiseAllStats(),
            Application.Current.Dispatcher);
            _reportTimer.Start();
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            listener.Start();
            var Ctrl = new ProcessStartInfo
            {
                FileName = "2控制软件",
                WorkingDirectory = @"F:/desktop",
                UseShellExecute = true
            };
            Process.Start(Ctrl);
            while (true)
            {
                _client = await listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                _ = HandleClientAsync(_client, _stream);
            }
        }

        private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        {
            var tmp = new byte[4 * 1024 * 1024];
            try
            {
                while (client.Connected)
                {
                    int n = await stream.ReadAsync(tmp);
                    if (n == 0) break;  // 客户端断开

                    // 1) 把新数据拷贝到 _buffer[_tail..]
                    Buffer.BlockCopy(tmp, 0, _buffer, _tail, n);
                    _tail += n;

                    // 2) 拆帧
                    while (true)
                    {
                        int available = _tail - _head;
                        if (available < 5) break; // 至少要能读到头+长度

                        // 在 _buffer[_head.._tail] 中找 0xFF
                        int idx = Array.IndexOf(_buffer, (byte)FrameHead, _head, available);
                        if (idx < 0)
                        {
                            // 整段丢弃
                            _head = _tail = 0;
                            break;
                        }
                        // 丢弃头前噪声
                        if (idx > _head) _head = idx;
                        _searchStart = _head + 1;
                        if (_tail - _head < 5) break; // 还差长度字段

                        // 读取 payload 长度（小端）
                        int payloadLen = BitConverter.ToInt32(_buffer, _head + 1);
                        int frameLen = 1 + 4 + payloadLen;
                        if (payloadLen < 0 || _tail - _head < frameLen)
                            break; // 不够完整

                        // 拷贝出这一帧的有效载荷
                        byte[] frame = _pool.Rent(payloadLen);
                        Buffer.BlockCopy(_buffer, _head + 5, frame, 0, payloadLen);

                        // 推进 _head
                        _head += frameLen;
                        _searchStart = _head;
                        // 心跳 frame == "00"
                        if (payloadLen == 1 && frame[0] == 0x00)
                            continue;

                        // 处理图像帧
                        //await _frameProcessor.SendAsync(frame);
                        //Trace.WriteLine($"[FRAME_QUEUE] Pending: {_frameProcessor.InputCount}");
                        _frameProcessor.Post(frame);
                    }

                    // 3) 如果剩余区间很小，就搬家一次
                    if (_head > 0 && _tail - _head < _buffer.Length / 2)
                    {
                        int rem = _tail - _head;
                        Buffer.BlockCopy(_buffer, _head, _buffer, 0, rem);
                        _head = 0;
                        _tail = rem;
                    }
                }
            }
            catch (IOException) { /* … */ }
            finally
            {
                client.Close();
            }
        }

        private void ProcessFrame(byte[] buf)
        {
            // 1. 解包
            //Interlocked.Increment(ref _frameCount);
            using var ms = new MemoryStream(buf);
            using var br = new BinaryReader(ms);
            br.ReadByte();               // frameHead
            byte cameraNo = br.ReadByte();
            int objectId = br.ReadInt32();
            ushort width = br.ReadUInt16();
            ushort height = br.ReadUInt16();
            br.ReadByte();               // channels
            ushort bpl = br.ReadUInt16();
            br.ReadInt32();              // length
            int imgLen = bpl * height;
            byte[] imgBuf = br.ReadBytes(imgLen);

            // 2. GenImageInterleaved → HImage rgbImage
            var sw = Stopwatch.StartNew();
            var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
            HOperatorSet.GenImageInterleaved(
            out HObject imgObj,
            handle.AddrOfPinnedObject(),
            "rgb", width, height,
            bpl, "byte",
            width, height,
            0, 0, 8, 0
            );
            var rgbImage = new HImage(imgObj);
            imgObj.Dispose();
            var engine = _threadEngine.Value;
            bool allOk = true;
            switch (cameraNo)
            {
                case 64:
                    cameraNo = 6;
                    break;
                case 65:
                    cameraNo = 7;
                    break;
                case 66:
                    cameraNo = 8;
                    break;
                case 67:
                    cameraNo = 9;
                    break;
                case 68:
                    cameraNo = 10;
                    break;
                case 69:
                    cameraNo = 11;
                    break;
                default:
                    break;
            }
            foreach (var script in scripts[cameraNo])
            {
                try
                {
                    var runner = GetRunner(script);
                    bool IsOk = runner.Run(rgbImage);
                    if (!IsOk) { allOk = false; break; }
                }
                catch { allOk = false; }
            }
            handle.Free();
            int idx = cameraNo;
            if (allOk) _stats[idx].OkCount++;
            else _stats[idx].NgCount++;
            if (cameraNo < 6)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo + 1, objectId, rgbImage));
                }), DispatcherPriority.Background);
            }
            else { rgbImage.Dispose(); }
            //更新 objectStates 并在最后一个相机时发 final result
            var state = objectStates.GetOrAdd(objectId, _ => new ObjectState());
            bool isLast = state.SetResult(cameraNo, allOk, CameraCount);
            Trace.WriteLine("" + objectId + " " + cameraNo + " " + allOk);
            if (isLast)
            {
                bool finalOk = state.GetFinalOk();
                idx = MainWindow.CameraCount;
                if (finalOk) _stats[idx].OkCount++;
                else _stats[idx].NgCount++;
                SendResult(objectId, finalOk);
                objectStates.TryRemove(objectId, out _);
            }
            _pool.Return(buf, clearArray: false);
            sw.Stop();
            Trace.WriteLine($"Processed in {sw.ElapsedMilliseconds}ms");
        }

        private void SendResult(int objectId, bool isOk)
        {
            if (_stream == null || !_client.Connected)
                return;

            Trace.WriteLine($"Sending result for object {objectId}: {(isOk ? "OK" : "NG")}");
            using var bw = new BinaryWriter(_stream, Encoding.Default, leaveOpen: true);
            const int PayloadLen = 0x06;    // 1(type) + 4(objectId) + 1(result)
            const byte ResultType = 0x01;   // our “result” frame

            // 1) header
            bw.Write(FrameHead);

            // 2) payload length (fixed)
            bw.Write(PayloadLen);

            // 3) payload
            bw.Write(ResultType);
            bw.Write(objectId);                   // 4 bytes, little‑endian
            bw.Write((byte)(isOk ? 1 : 0));       // 1 byte

            bw.Flush();
        }

        public void SendStartSignal() => SendControlSignal(0x03);
        public void SendStopSignal() => SendControlSignal(0x04);
        public void SendClearSignal() => SendControlSignal(0x05);
        private void SendControlSignal(byte code)
        {
            if (_stream == null || !_client?.Connected == true)
            {
                return;
            }
            // build frame in one go
            // head(1) + length(4, little‑endian) + payload(1)
            var buf = new byte[6] {
            FrameHead,
            1, 0, 0, 0,    // payload length = 1
            code           // your actual command byte
        };
            using var bw = new BinaryWriter(_stream, Encoding.Default, leaveOpen: true);
            bw.Write(buf, 0, buf.Length);
            bw.Flush();
        }

        private void RaiseAllStats()
        {
            // Shallow copy of the *list*, not the items
            var snapshot = _stats.ToArray();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                AllStatsReported?.Invoke(this,
                    new AllStatsEventArgs(snapshot));
            }), DispatcherPriority.Render);
        }

        static void OnCapture(IntPtr pData, ref SieveDll.SieveCaptureEx captureInfo, IntPtr userData)
        {
            var img = Marshal.PtrToStructure<SieveDll.GzsiaImage>(captureInfo.image);
            int len = img.height * img.bytePerLine;
            var buf = new byte[len];
            Marshal.Copy(pData, buf, 0, len);
            Console.WriteLine($"Got image: {img.width}x{img.height} from camera {captureInfo.camerId}");
        }
    }
    class ScriptRunner
    {
        public HDevProcedure Procedure { get; }
        public HDevProcedureCall Call { get; private set; }

        public ScriptRunner(string path)
        {
            var program = new HDevProgram(path);
            Procedure = new HDevProcedure(program, "Defect");
            Call = new HDevProcedureCall(Procedure);
        }

        public bool Run(HImage input)
        {
            Call.SetInputIconicParamObject("Image", input);
            Call.Execute();
            return Call.GetOutputCtrlParamTuple("IsOk");
        }
    }
}
