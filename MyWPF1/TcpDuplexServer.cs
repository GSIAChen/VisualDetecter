using HalconDotNet;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Application = System.Windows.Application;
using NLog;
using System.Threading;

namespace MyWPF1
{
    public class TcpDuplexServer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // 通信速率统计字段
        private long _frameCount = 0;
        private long _byteCount = 0;
        private readonly object _statLock = new object();
        private readonly DispatcherTimer _commStatTimer;
        private int Port;
        private const byte FrameHead = 0xFF;

        // 这里只保存单个客户端连接；如果要多个并行，可以用 ConcurrentDictionary<int, TcpClient> 等
        private TcpClient _client;
        private NetworkStream _stream;
        private ObservableCollection<string>[] scripts;
        private Dictionary<int, ObjectState> objectStates;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private readonly ActionBlock<byte[]> _frameProcessor;
        private readonly DispatcherTimer _reportTimer;
        private readonly ConcurrentDictionary<string, HDevProgram> _programCache = new();
        private readonly HDevEngine _engine;
        public event EventHandler<AllStatsEventArgs>? AllStatsReported;
        private readonly CameraStat[] _stats = Enumerable
                                       .Range(1, 8)
                                       .Select(_ => new CameraStat(_))
                                       .ToArray();
        // 性能监控定时器
        private readonly DispatcherTimer _perfStatTimer;

        // Constructor parameter is named "port"
        public TcpDuplexServer(
            ObservableCollection<string>[] scripts,
            Dictionary<int, ObjectState> objectStates,
            int port
        )
        {
            this.scripts = scripts;
            this.objectStates = objectStates;
            this.Port = port;
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            HTuple devs;
            HTuple handle;
            HOperatorSet.QueryAvailableComputeDevices(out devs);
            // 2) 打开第一个 OpenCL 设备
            //    （如果你想指定平台、设备，也可以从 devs 里挑更合适的 index）
            HOperatorSet.OpenComputeDevice(devs, out handle);
            HOperatorSet.ActivateComputeDevice(handle);
            // 3) 确保并行算子已开启（默认就是 true）
            HOperatorSet.SetSystem("parallelize_operators", "true");
            _reportTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(5),
            DispatcherPriority.Normal,
            (s, e) => RaiseAllStats(),
            Application.Current.Dispatcher);
            _reportTimer.Start();
            // 通信速率统计定时器
            _commStatTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (s, e) => LogCommStats(),
                Application.Current.Dispatcher);
            _commStatTimer.Start();
            // 性能监控定时器，每5秒输出一次内存和线程池状态
            _perfStatTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(5),
                DispatcherPriority.Normal,
                (s, e) => LogPerfStats(),
                Application.Current.Dispatcher);
            _perfStatTimer.Start();
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            listener.Start();
            logger.Info($"[硬件交互] TCP监听启动，端口: {Port}");
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
                logger.Info("[硬件交互] TCP客户端已连接");
                _ = HandleClientAsync(_client, _stream);
            }
        }

        private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        {
            var recvBuffer = new List<byte>();
            var tmp = new byte[8192];

            try
            {
                while (client.Connected)
                {
                    int n = await stream.ReadAsync(tmp, 0, tmp.Length);
                    if (n == 0) break;  // 客户端断开
                    logger.Info($"[硬件交互] 接收到原始数据: 字节数={n}，当前待处理帧队列长度: {recvBuffer.Count}");
                    recvBuffer.AddRange(tmp.Take(n));
                    // 拆帧
                    while (TryExtractFrame(ref recvBuffer, out var frame))
                    {
                        if (BitConverter.ToString(frame) == "00")
                        {
                            logger.Warn("[丢帧] 收到无效帧，已丢弃");
                            continue;
                        }
                        HandleImagePayload(frame);
                    }
                }
            }
            catch (IOException ex)
            {
                logger.Error(ex, "[硬件交互] TCP连接异常");
            }
            finally
            {
                logger.Info("[硬件交互] TCP客户端已断开");
                client.Close();
            }
        }

        private void HandleImagePayload(byte[] buf)
        {
            // 统计帧数和字节数
            lock (_statLock)
            {
                _frameCount++;
                _byteCount += buf.Length;
            }
            logger.Info($"收到新帧: 长度={buf.Length}");
            logger.Info($"[性能] 当前待处理帧队列长度: {buf.Length}");
            using var ms = new MemoryStream(buf);
            using var br = new BinaryReader(ms);

            byte frameHead = br.ReadByte();
            byte cameraNo = br.ReadByte();
            int objectId = br.ReadInt32();
            ushort width = br.ReadUInt16();
            ushort height = br.ReadUInt16();
            byte channels = br.ReadByte();
            ushort bpl = br.ReadUInt16();
            int length = br.ReadInt32();

            int imgLen = bpl * height;
            byte[] imgBuf = br.ReadBytes(imgLen);
            var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
            // 1) Create the three single‐channel planes from the same buffer
            Trace.WriteLine($"[TCP] Received RGB image: camNo:{cameraNo}, objID:{objectId}, {width}x{height}, Channels={channels}, bpl={bpl}");
            logger.Info($"[TCP] Received RGB image: camNo:{cameraNo}, objID:{objectId}, {width}x{height}, Channels={channels}, bpl={bpl}");
            HObject image;
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                // HALCON's GenImageInterleaved has an overload that takes IntPtr
                HOperatorSet.GenImageInterleaved(
                    out image,
                    ptr,       // pointer to R,G,B bytes
                    "rgb",     // channel order
                    width, height,
                    bpl,       // plugin
                    "byte",    // pixel type
                    width, height,
                    0, 0, 8, 0
                );
            }
            finally
            {
                handle.Free();
            }
            HImage rgbImage = new HImage(image);
            image.Dispose();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo + 1, objectId, rgbImage));
            //}), DispatcherPriority.Background);
            _ = Task.Run(() => ProcessScripts(cameraNo, objectId, rgbImage));
        }

        private HDevProgram GetOrLoadProgram(string path)
        => _programCache.GetOrAdd(path, p => new HDevProgram(p));

        private void ProcessScripts(int cameraIndex, int objectId, HImage image)
        {
            var sw = Stopwatch.StartNew();
            logger.Info($"开始处理: ObjectId={objectId}, Camera={cameraIndex}");
            // 线程池状态
            int workerThreads, completionPortThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            logger.Info($"[性能] 线程池可用工作线程: {workerThreads}, IO线程: {completionPortThreads}");
            bool allOk = true;
            foreach (var script in scripts[cameraIndex])
            {
                try
                {
                    _engine.SetProcedurePath(Path.GetDirectoryName(script));
                    using var program = GetOrLoadProgram(script);
                    using var procedure = new HDevProcedure(program, "Defect");
                    using var call = new HDevProcedureCall(procedure);

                    // 传入图像
                    call.SetInputIconicParamObject("Image", image);

                    // 执行
                    call.Execute();

                    // 读取输出
                    using HTuple isOk = call.GetOutputCtrlParamTuple("IsOk");

                    if (isOk.I != 1)
                    {
                        allOk = false;
                        /**
                        // 1) retrieve global batch info from MainWindow
                        if (System.Windows.Application.Current.MainWindow is not MainWindow main)
                            continue;   // or break, as you prefer

                        string material = main.MaterialName ?? "UnknownMaterial";
                        string batch = main.BatchNumber ?? "UnknownBatch";
                        int objId = objectId;  // from your parameters

                        // 2) date folder
                        string date = DateTime.Now.ToString("yyyy_MM_dd");
                        string baseDir = Path.Combine(@"D:\images", material, date, batch);

                        // 3) ensure it exists
                        Directory.CreateDirectory(baseDir);

                        // 4) script name without path or extension
                        string scriptName = Path.GetFileNameWithoutExtension(script);

                        // 5) build full file name
                        string filename = $"{objId}_{scriptName}.bmp";
                        string fullPath = Path.Combine(baseDir, filename);
                        Trace.WriteLine(fullPath);
                        // 6) write the image
                        try
                        {
                            HOperatorSet.WriteImage(image, "bmp", 0, fullPath);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"[ERROR] Save failed: {ex}");
                        }
                        **/
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[Halcon Error] {ex.Message}");
                    logger.Error(ex, $"处理异常: ObjectId={objectId}, Camera={cameraIndex}");
                    allOk = false;
                }
            }

            var idx = cameraIndex;
            if (allOk) _stats[idx].OkCount++;
            else _stats[idx].NgCount++;
            if (!allOk)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logger.Warn($"处理结果NG: ObjectId={objectId}, Camera={cameraIndex}");
                    ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraIndex + 1, objectId, image));
                }), DispatcherPriority.Render);
            }
            // 后续如果需要回调Ng图的缺陷位置时使用
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraIndex + 1, objectId, image));
                }), DispatcherPriority.Render);
            }
            //CameraResultReported?.Invoke(this, new CameraResultEventArgs
            //{
            //    CameraIndex = cameraIndex,
            //    IsOk = allOk
            //});
            // 更新 objectStates 并在最后一个相机时发 final result
            if (!objectStates.TryGetValue(objectId, out var state))
            {
                state = new ObjectState();
                objectStates[objectId] = state;
            }

            bool isLast = state.SetResult(cameraIndex, allOk);
            if (isLast)
            {
                bool finalOk = state.GetFinalOk();
                //CameraResultReported?.Invoke(this, new CameraResultEventArgs
                //{
                //    CameraIndex = 8,
                //    IsOk = finalOk
                //});
                idx = 7;
                if (finalOk) _stats[idx].OkCount++;
                else _stats[idx].NgCount++;
                SendResult(objectId, finalOk);
                objectStates.Remove(objectId);
            }
            image.Dispose();
            sw.Stop();
            logger.Info($"处理结束: ObjectId={objectId}, Camera={cameraIndex}, 耗时={sw.ElapsedMilliseconds}ms");
        }

        private void SendResult(int objectId, bool isOk)
        {
            if (_stream == null || !_client.Connected)
                return;

            using var bw = new BinaryWriter(_stream, Encoding.Default, leaveOpen: true);
            Trace.WriteLine("Sending Result for ObjectId:" + objectId + " the result is " + isOk);
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

        private bool TryExtractFrame(ref List<byte> recvBuffer, out byte[] frame)
        {
            frame = null;
            // 1) find header
            int idx = recvBuffer.IndexOf(FrameHead);
            if (idx < 0)
            {
                // no header at all, drop garbage
                recvBuffer.Clear();
                return false;
            }
            // discard anything before the header
            if (idx > 0)
                recvBuffer.RemoveRange(0, idx);

            // 2) need at least 5 bytes (1 header + 4 length)
            if (recvBuffer.Count < 5)
                return false;

            // 3) parse length (little‑endian)
            int payloadLen = BitConverter.ToInt32(recvBuffer.Skip(1).Take(4).ToArray(), 0);
            int fullLen = 1 + 4 + payloadLen;
            if (payloadLen < 0 || fullLen > recvBuffer.Count)
                return false;  // not enough yet, or bogus length

            // 4) we have a full frame!
            frame = recvBuffer.Skip(5).Take(payloadLen).ToArray();
            // 5) consume it from buffer
            recvBuffer.RemoveRange(0, fullLen);

            return true;
        }

        public void SendStartSignal() => SendControlSignal(0x03);
        public void SendStopSignal() => SendControlSignal(0x04);
        public void SendClearSignal() => SendControlSignal(0x05);
        private void SendControlSignal(byte code)
        {
            if (_stream == null || !_client?.Connected == true) { 
                logger.Warn("[硬件交互] 发送控制信号失败：未连接");
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
            logger.Info($"[硬件交互] 发送控制信号: code=0x{code:X2}");
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

        private void LogCommStats()
        {
            long frames, bytes;
            lock (_statLock)
            {
                frames = _frameCount;
                bytes = _byteCount;
                _frameCount = 0;
                _byteCount = 0;
            }
            logger.Info($"[实时通信速率] 1秒内收到帧数: {frames}，字节数: {bytes}，平均每帧: {(frames > 0 ? bytes / frames : 0)} 字节");
        }

        private void LogPerfStats()
        {
            long memory = GC.GetTotalMemory(false);
            int workerThreads, completionPortThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            logger.Info($"[性能] 当前进程内存占用: {memory / 1024 / 1024} MB，线程池可用工作线程: {workerThreads}, IO线程: {completionPortThreads}");
        }
    }
}
