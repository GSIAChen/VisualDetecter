using HalconDotNet;
using OpenCvSharp;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
//using OpenCvSharp.Extensions;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MyWPF1
{
    public class TcpDuplexServer
    {
        // 通信速率统计字段
        private int Port;
        private const byte FrameHead = 0xFF;
        private readonly ActionBlock<(byte[] Buf, int Len)> _frameProcessor;
        private int _cameraCount;
        public int CameraCount
        {
            get => _cameraCount;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException();
                _cameraCount = value;
            }
        }
        private bool _saveNG;
        public bool SaveNG
        {
            get => _saveNG;
            set
            {
                _saveNG = value;
            }
        }

        // 这里只保存单个客户端连接；如果要多个并行，可以用 ConcurrentDictionary<int, TcpClient> 等
        private TcpClient _client;
        private NetworkStream _stream;
        private TcpListener _listener;
        private readonly int _parallelism = Environment.ProcessorCount;
        private ObservableCollection<string>[] scripts;
        private readonly ConcurrentDictionary<string, Task> _prewarmTasks = new(StringComparer.OrdinalIgnoreCase);
        private ConcurrentDictionary<int, ObjectState> objectStates;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private readonly DispatcherTimer _reportTimer;
        private HDevEngine _engine;
        ConcurrentDictionary<string, ThreadLocal<ScriptRunner>> _runners = new();
        public event EventHandler<AllStatsEventArgs>? AllStatsReported;
        private readonly CameraStat[] _stats = Enumerable
                                       .Range(1, MainWindow.CameraCount + 1)
                                       .Select(_ => new CameraStat(_))
                                       .ToArray();
        // 线程引擎池（初始化）
        private readonly ThreadLocal<HDevEngine> _threadEngine = new(() =>
        {
            var engine = new HDevEngine();
            engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            return engine;
        });
        private byte[] _buffer = ArrayPool<byte>.Shared.Rent(10 * 1024 * 1024); // 10 MB 循环缓冲区
        private int _head = 0, _tail = 0;
        private int width = 1440, height = 1080, channels = 3;
        private const int ReadBufferSize = 8 * 1024 * 1024;
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private readonly int _imgLen;
        private int _searchStart = 0;
        private int _frameCount;
        private System.Timers.Timer _fpsTimer;

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
            int initialCameraCount,
            bool saveNG
        )
        {
            this.scripts = scripts;
            this.Port = port;
            this.CameraCount = initialCameraCount;
            this.SaveNG = saveNG;
            this.objectStates = new ConcurrentDictionary<int, ObjectState>();
            _imgLen = width * height * channels;
            //InitMonitor();
            // 设置Halcon引擎
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");
            //HTuple devs;
            //HTuple handle;
            //HOperatorSet.QueryAvailableComputeDevices(out devs);
            //HOperatorSet.OpenComputeDevice(devs, out handle);
            //HOperatorSet.ActivateComputeDevice(handle);
            //HTuple useInfo;
            //HOperatorSet.GetSystem("parallelize_operators", out useInfo);
            //Trace.WriteLine($"AOP on: {useInfo}");
            //HTuple cudaInfo;
            //HOperatorSet.GetSystem("cuda_devices", out cudaInfo);
            //Trace.WriteLine($"Cuda devices: {cudaInfo}");
            //HOperatorSet.SetSystem("parallelize_operators", "true");
            //HOperatorSet.SetSystem("thread_num", Environment.ProcessorCount);
            //Trace.WriteLine("CPU num = " + Environment.ProcessorCount);

            // 设置 ActionBlock 的并行度和缓冲区
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _parallelism,
                EnsureOrdered = false
            };
            _frameProcessor = new ActionBlock<(byte[] Buf, int Len)>(t =>
            {
                ProcessFrame(t.Buf, t.Len);
            }, options);

            //_frameProcessor = new ActionBlock<byte[]>(frame =>
            //{
            //    ProcessFrame(frame);
            //}, options);
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
            _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            _listener.Start();
            var Ctrl = new ProcessStartInfo
            {
                FileName = "TestEc3224l.exe",
                WorkingDirectory = @"D:/program/TestEc3224l",
                UseShellExecute = true
            };
            Process.Start(Ctrl);
            while (true)
            {
                _client = await _listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                _ = HandleClientAsync(_client, _stream);
            }
        }

        private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        {
            var tmp = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            try
            {
                while (client.Connected)
                {
                    int n = await stream.ReadAsync(tmp, 0, ReadBufferSize);
                    if (n == 0) break;  // 客户端断开

                    // 1) 确保 _buffer 有足够空间：若不够则先搬移已用数据到头部或扩大（这里搬移）
                    int freeTail = _buffer.Length - _tail;
                    if (n > freeTail)
                    {
                        // 尝试搬家（把 [_head.._tail) 移到 0 开始）
                        int rem = _tail - _head;
                        if (rem > 0)
                        {
                            Buffer.BlockCopy(_buffer, _head, _buffer, 0, rem);
                        }
                        _head = 0;
                        _tail = rem;
                        freeTail = _buffer.Length - _tail;

                        // 如果仍然不够（单帧太大），最好抛错或扩容；这里简单扩容为双倍直到足够
                        if (n > freeTail)
                        {
                            int newSize = _buffer.Length;
                            while (n > (newSize - _tail)) newSize *= 2;
                            var newBuf = ArrayPool<byte>.Shared.Rent(newSize);
                            Buffer.BlockCopy(_buffer, 0, newBuf, 0, _tail);
                            ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
                            _buffer = newBuf; // 需要将 _buffer 改为非 readonly 字段，或实现其他扩容策略
                            freeTail = _buffer.Length - _tail;
                        }
                    }

                    // 2) 把新数据拷贝到 _buffer[_tail..]
                    Buffer.BlockCopy(tmp, 0, _buffer, _tail, n);
                    _tail += n;

                    // 3) 拆帧
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
                        if (_tail - _head < 5) break; // 还差长度字段

                        // 读取 payload 长度（小端）
                        int payloadLen = BitConverter.ToInt32(_buffer, _head + 1);
                        int frameLen = 1 + 4 + payloadLen;
                        if (payloadLen < 0 || _tail - _head < frameLen)
                            break; // 不够完整

                        // 租一个数组拷贝出这一帧的有效载荷
                        var frameBuf = _pool.Rent(payloadLen);
                        Buffer.BlockCopy(_buffer, _head + 5, frameBuf, 0, payloadLen);

                        // 推进 _head
                        _head += frameLen;

                        // 心跳 frame == "00"
                        if (payloadLen == 1 && frameBuf[0] == 0x00)
                        {
                            _pool.Return(frameBuf, clearArray: false);
                            continue;
                        }

                        if (payloadLen == 1 && frameBuf[0] == 0x01)
                        {
                            MessageBox.Show("设备初始化成功，可以运行！");
                            _pool.Return(frameBuf, clearArray: false);
                            continue;
                        }

                        // 把 (buf,len) 提交给处理器（注意使用 SendAsync 可以等待队列可用；这里使用 Post 非阻塞）
                        if (!_frameProcessor.Post((frameBuf, payloadLen)))
                        {
                            // 队列可能已完成或其他问题：退回数组并尝试 SendAsync（阻塞等待）
                            _pool.Return(frameBuf, clearArray: false);
                            await _frameProcessor.SendAsync((frameBuf, payloadLen));
                        }
                    }
                }
            }
            catch (IOException) { /* … */ }
            finally
            {
                ArrayPool<byte>.Shared.Return(tmp, clearArray: false);
                client.Close();
            }
        }

        private void ProcessFrame(byte[] buf, int payloadLen)
        {
            // payloadLen 是实际有效字节长度
            //var sw = Stopwatch.StartNew();

            try
            {
                using var ms = new MemoryStream(buf, 0, payloadLen, writable: false, publiclyVisible: true);
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
                // 保证 imgLen 不会超出 payloadLen
                if (imgLen < 0 || imgLen > payloadLen - (int)ms.Position)
                {
                    Trace.WriteLine("[ProcessFrame] imgLen invalid or larger than remaining payload");
                    return;
                }

                byte[] imgBuf = br.ReadBytes(imgLen);
                var swTotal = Stopwatch.StartNew();
                var swStep = Stopwatch.StartNew();
                // Pin & GenImageInterleaved 用 try/finally
                GCHandle handle = default;
                HObject imgObj = null;
                HImage rgbImage = null;
                try
                {
                    handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();

                    HOperatorSet.GenImageInterleaved(
                        out imgObj,
                        ptr,
                        "rgb", width, height,
                        (int)bpl, "byte",
                        width, height,
                        0, 0, 8, 0);

                    rgbImage = new HImage(imgObj);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("[ProcessFrame] Halcon GenImageInterleaved failed: " + ex);
                    // 出错尽早返回（确保释放）
                    if (imgObj != null) imgObj.Dispose();
                    if (rgbImage != null) rgbImage.Dispose();
                    return;
                }
                finally
                {
                    if (handle.IsAllocated) handle.Free();
                }
                swStep.Stop();
                Trace.WriteLine($"[TIMING] GenImageInterleaved: {swStep.ElapsedMilliseconds}ms");
                // 处理脚本（示例：thread-local runner）
                bool allOk = true;
                HTuple type = new HTuple();
                // 你这里可能需要一个 bounds check： cameraNo 必须是合法范围
                if (cameraNo >= 64 && cameraNo <= 69) cameraNo = (byte)(cameraNo - 58); // 64->6 ... 69->11 (你原来是多处映射)
                if (cameraNo >= scripts.Length)
                {
                    Trace.WriteLine($"[ProcessFrame] cameraNo {cameraNo} out of scripts range");
                    rgbImage.Dispose();
                    return;
                }

                foreach (var script in scripts[cameraNo])
                {
                    var swScript = Stopwatch.StartNew();
                    bool isOk;
                    try
                    {
                        var runner = GetRunner(script);
                        (isOk, type) = runner.Run(rgbImage);
                        swScript.Stop();
                        Trace.WriteLine($"[TIMING] Script {Path.GetFileName(script)}: {swScript.ElapsedMilliseconds}ms");
                        if (!isOk) { allOk = false; break; }
                    }
                    catch (Exception ex)
                    {
                        swScript.Stop();
                        Trace.WriteLine($"[TIMING] Script {Path.GetFileName(script)} failed after {swScript.ElapsedMilliseconds}ms: {ex.Message}");
                        allOk = false;
                        break;
                    }
                }

                // 可选保存 NG 图
                if (SaveNG && !allOk)
                {
                    string rtype = type;
                    try
                    {
                        HalconConverter.SaveImageWithDotNet(rgbImage, $@"D:\images\camera{cameraNo + 1}\{objectId}_{rtype}.bmp");
                    }
                    catch (Exception ex) { Trace.WriteLine("[SaveNG] " + ex); }
                }

                // 更新统计、回调 UI（短）等
                int idx = cameraNo;
                if (idx >= 0 && idx < _stats.Length)
                {
                    if (allOk) _stats[idx].OkCount++;
                    else _stats[idx].NgCount++;
                }

                if (cameraNo < 6 && !allOk)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo + 1, objectId, rgbImage, type));
                    }), DispatcherPriority.Background);
                }
                else
                {
                    rgbImage.Dispose();
                }

                // 更新 objectStates
                var state = objectStates.GetOrAdd(objectId, _ => new ObjectState());
                bool isLast = state.SetResult(cameraNo, allOk, CameraCount);
                Trace.WriteLine($"Obj {objectId} got cam {cameraNo} => {allOk} (count={state.Count})");
                if (isLast)
                {
                    bool finalOk = state.GetFinalOk();
                    int totIdx = _stats.Length - 1;
                    if (finalOk) _stats[totIdx].OkCount++;
                    else _stats[totIdx].NgCount++;
                    SendResult(objectId, finalOk);
                    objectStates.TryRemove(objectId, out _);
                }
                swTotal.Stop();
                Trace.WriteLine($"[TIMING] Total ProcessFrame: {swTotal.ElapsedMilliseconds}ms");
            }
            finally
            {
                // 一定要把 frame array 归还给池（无论成功或异常）
                _pool.Return(buf, clearArray: false);
            }
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

        /// <summary>
        /// 异步预热单个脚本（JIT / 初始化） —— 如果同一路径已在预热则复用该 Task。
        /// 在内部使用 ThreadPool（Task.Run），不会阻塞调用线程。
        /// </summary>
        public Task PrewarmScriptAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return Task.CompletedTask;

            return _prewarmTasks.GetOrAdd(path, p => Task.Run(() =>
            {
                try
                {
                    // 小灰度占位图，触发 script 的首次初始化/JIT
                    HObject tmpObj;
                    HOperatorSet.GenImageConst(out tmpObj, "byte", 4, 4);
                    using var tmpImg = new HImage(tmpObj);

                    // 使用线程本地 Runner（你的 GetRunner 已是 ThreadLocal 的实现）
                    var runner = GetRunner(p);

                    // Run 可能会抛异常，捕获并记录
                    try
                    {
                        runner.Run(tmpImg);
                        Trace.WriteLine($"[Prewarm] {Path.GetFileName(p)} prewarmed.");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[Prewarm] {p} run failed: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[Prewarm] Unexpected error for {path}: {ex}");
                }
                finally
                {
                    // 预热完成之后把 entry 移除，允许下一次重新预热（如果需要）
                    _prewarmTasks.TryRemove(path, out _);
                }
            }));
        }

        public async Task PrewarmAllScriptsAsync(int workers = 0, string? sampleImagePath = null)
        {
            try
            {
                var all = scripts.Where(s => s != null).SelectMany(s => s).Distinct().ToList();
                if (all.Count == 0) return;

                Trace.WriteLine($"[Prewarm] Prewarming {all.Count} scripts on workers...");

                if (workers <= 0) workers = Math.Max(1, Environment.ProcessorCount);

                // 1) 尝试加载真实样本图（如果给定了路径）
                HImage sampleImage = null!;
                bool usingRealImage = false;
                if (!string.IsNullOrEmpty(sampleImagePath) && File.Exists(sampleImagePath))
                {
                    try
                    {
                        // 使用 Halcon 读取图片（注意：ReadImage 的第二参可以是 string）
                        HObject tmpObj;
                        HOperatorSet.ReadImage(out tmpObj, sampleImagePath);
                        sampleImage = new HImage(tmpObj);
                        usingRealImage = true;
                        Trace.WriteLine($"[Prewarm] Using real sample image: {sampleImagePath}");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[Prewarm] Failed to read sample image '{sampleImagePath}': {ex.Message}. Falling back to tiny image.");
                        sampleImage = null!;
                        usingRealImage = false;
                    }
                }

                // 2) 若没成功读取真实图，建立 4x4 占位图
                if (!usingRealImage)
                {
                    HObject tmpObj;
                    HOperatorSet.GenImageConst(out tmpObj, "byte", 4, 4);
                    sampleImage = new HImage(tmpObj);
                    Trace.WriteLine("[Prewarm] Using tiny 4x4 image for prewarm.");
                }

                // 3) 在每个 worker 线程上触发 thread-local 引擎 & runner 的初始化并 run 脚本一次
                var tasks = Enumerable.Range(0, workers).Select(workerIndex => Task.Run(() =>
                {
                    try
                    {
                        // 访问 thread-local engine，确保在当前线程上创建 engine（Side-effect: engine 初始化）
                        var eng = _threadEngine?.Value ?? _engine;

                        // 遍历所有脚本并在当前线程上调用 runner.Run(sampleImage)
                        foreach (var path in all)
                        {
                            try
                            {
                                // GetRunner 返回当前线程的 ScriptRunner（通过 ThreadLocal wrapper）
                                var runner = GetRunner(path);
                                // Run once to trigger initialization/JIT; ignore return value
                                runner.Run(sampleImage);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"[Prewarm] script {Path.GetFileName(path)} on worker {Thread.CurrentThread.ManagedThreadId} failed: {ex.Message}");
                            }
                        }

                        Trace.WriteLine($"[Prewarm] done on worker thread {Thread.CurrentThread.ManagedThreadId}");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[Prewarm] worker exception: {ex}");
                    }
                })).ToArray();

                await Task.WhenAll(tasks);

                // 4) 释放 sampleImage
                sampleImage.Dispose();

                Trace.WriteLine("[Prewarm] all workers finished prewarming.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[Prewarm] Exception: " + ex);
            }
        }

        public Task StopAsync()
        {
            try
            {
                _listener?.Stop();
            }
            catch { }

            return Task.CompletedTask;
        }

        class ScriptRunner
        {
            private readonly string _path;
            private readonly string _procName;
            public HDevProcedure Procedure { get; }
            public HDevProcedureCall Call { get; private set; }


            public ScriptRunner(string path, string procName = "Defect")
            {
                _path = path;
                _procName = procName;
                var program = new HDevProgram(path);
                Procedure = new HDevProcedure(program, procName);
                Call = new HDevProcedureCall(Procedure);
            }

            // 每次调用重新创建调用对象（稳健）
            public (bool IsOk, HTuple Type) Run(HImage input)
            {
                // engine 传入调用线程对应的 engine（例如 threadLocal.Value）
                try
                {
                    using var call = new HDevProcedureCall(Procedure);
                    Call.SetInputIconicParamObject("Image", input);
                    Call.Execute();
                    using var result = Call.GetOutputCtrlParamTuple("IsOk");
                    using var type = Call.GetOutputCtrlParamTuple("Type");
                    bool ok = result.I == 1;
                    return (ok, type);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[ScriptRunner] Run failed for {_path}: {ex.Message}");
                    return (false, new HTuple());
                }
            }
        }

        public class HalconConverter
        {
            /// <summary>
            /// 将Halcon的HImage对象转换为.NET的Bitmap对象。
            /// 支持灰度图和24位彩色图。
            /// </summary>
            /// <param name="ho_Image">输入的HImage对象</param>
            /// <returns>转换后的Bitmap对象</returns>
            public static Bitmap HImageToBitmap(HImage ho_Image)
            {
                if (ho_Image == null || !ho_Image.IsInitialized())
                {
                    throw new ArgumentNullException("Halcon image is null or not initialized.");
                }

                HOperatorSet.GetImageSize(ho_Image, out HTuple width, out HTuple height);
                HOperatorSet.GetImageType(ho_Image, out HTuple type);

                int imageWidth = width.I;
                int imageHeight = height.I;
                Bitmap bmp = null;

                HOperatorSet.GetImagePointer3(ho_Image, out HTuple pointerR, out HTuple pointerG, out HTuple pointerB, out _, out _, out _);
                IntPtr ptrR = new IntPtr(pointerR.L);
                IntPtr ptrG = new IntPtr(pointerG.L);
                IntPtr ptrB = new IntPtr(pointerB.L);

                bmp = new Bitmap(imageWidth, imageHeight, PixelFormat.Format24bppRgb);

                // 锁定Bitmap的内存区域以便快速写入
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

                unsafe
                {
                    byte* p = (byte*)bmpData.Scan0;
                    byte* r = (byte*)ptrR;
                    byte* g = (byte*)ptrG;
                    byte* b = (byte*)ptrB;

                    for (int i = 0; i < imageWidth * imageHeight; i++)
                    {
                        // Bitmap内存布局通常是 B, G, R
                        p[i * 3] = *b++;
                        p[i * 3 + 1] = *g++;
                        p[i * 3 + 2] = *r++;
                    }
                }

                bmp.UnlockBits(bmpData);

                // 由于Bitmap是基于Halcon的内存指针创建的，我们需要克隆一份，
                // 以免在Halcon对象被释放后Bitmap失效。
                Bitmap finalBmp = (Bitmap)bmp.Clone();
                bmp.Dispose();

                return finalBmp;
            }

            public static void SaveImageWithDotNet(HImage ho_Image, string filePath)
            {
                Bitmap bitmapToSave = null;
                try
                {
                    string directoryPath = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    bitmapToSave = HImageToBitmap(ho_Image);
                    // 可以选择任意格式保存
                    bitmapToSave.Save(filePath, ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving image: {ex.Message}");
                }
                finally
                {
                    bitmapToSave?.Dispose();
                }
            }
        }
    }
}
