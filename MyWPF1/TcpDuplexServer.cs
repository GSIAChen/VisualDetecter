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
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace MyWPF1
{
    public class TcpDuplexServer
    {
        // 通信速率统计字段
        private int Port;
        private const byte FrameHead = 0xFF;
        private readonly ActionBlock<byte[]> _frameProcessor;

        // 这里只保存单个客户端连接；如果要多个并行，可以用 ConcurrentDictionary<int, TcpClient> 等
        private TcpClient _client;
        private NetworkStream _stream;
        private ObservableCollection<string>[] scripts;
        private Dictionary<int, ObjectState> objectStates;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private readonly DispatcherTimer _reportTimer;
        private readonly ConcurrentDictionary<string, HDevProgram> _programCache = new();
        private HDevEngine _engine;
        private readonly HDevEngine[] _engines;
        public event EventHandler<AllStatsEventArgs>? AllStatsReported;
        private readonly CameraStat[] _stats = Enumerable
                                       .Range(1, 8)
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
            _imgLen = width * height * channels;
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
            Trace.WriteLine("CPU num = "+Environment.ProcessorCount);
            var parallelism = Environment.ProcessorCount;
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 24,
            };
            _frameProcessor = new ActionBlock<byte[]>(frame =>
            {
                ProcessFrame(frame);
            }, options);
            _reportTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(5),
            DispatcherPriority.Normal,
            (s, e) => RaiseAllStats(),
            Application.Current.Dispatcher);
            _reportTimer.Start();
        }

        private HDevProgram GetOrLoadProgram(string path)
        => _programCache.GetOrAdd(path, p => new HDevProgram(p));

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

        //private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        //{
        //    var recvBuffer = new List<byte>();
        //    var tmp = new byte[8192];

        //    try
        //    {
        //        while (client.Connected)
        //        {
        //            int n = await stream.ReadAsync(tmp, 0, tmp.Length);
        //            if (n == 0) break;  // 客户端断开
        //            recvBuffer.AddRange(tmp.Take(n));
        //            // 拆帧
        //            while (TryExtractFrame(ref recvBuffer, out var frame))
        //            {
        //                if (BitConverter.ToString(frame) == "00")
        //                {
        //                    continue;
        //                }
        //                HandleImagePayload(frame);
        //            }
        //        }
        //    }
        //    catch (IOException ex) { }
        //    finally
        //    {
        //        client.Close();
        //    }
        //}

        private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        {
            var tmp = new byte[8192];
            try
            {
                while (client.Connected)
                {
                    int n = await stream.ReadAsync(tmp, 0, tmp.Length);
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

                        if (_tail - _head < 5) break; // 还差长度字段

                        // 读取 payload 长度（小端）
                        int payloadLen = BitConverter.ToInt32(_buffer, _head + 1);
                        int frameLen = 1 + 4 + payloadLen;
                        if (payloadLen < 0 || _tail - _head < frameLen)
                            break; // 不够完整

                        // 拷贝出这一帧的有效载荷
                        byte[] frame = new byte[payloadLen];
                        Buffer.BlockCopy(_buffer, _head + 5, frame, 0, payloadLen);

                        // 推进 _head
                        _head += frameLen;

                        // 心跳 frame == "00"
                        if (payloadLen == 1 && frame[0] == 0x00)
                            continue;

                        // 处理图像帧
                        await _frameProcessor.SendAsync(frame);
                        //_frameProcessor.Post(frame);
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

        //private async Task HandleClientAsync(TcpClient client, NetworkStream stream)
        //{
        //    var recvBuffer = new List<byte>();
        //    var tmp = new byte[8192];

        //    while (client.Connected)
        //    {
        //        int n = await stream.ReadAsync(tmp, 0, tmp.Length);
        //        if (n == 0) break;
        //        recvBuffer.AddRange(tmp.Take(n));

        //        while (TryExtractFrame(ref recvBuffer, out var frame))
        //        {
        //            if (BitConverter.ToString(frame) == "00")
        //            {
        //                continue;
        //            }
        //            await _frameProcessor.SendAsync(frame);
        //        }
        //    }
        //    client.Close();
        //}

        private void ProcessFrame(byte[] buf)
        {
            // 1. 解包
            var sw = Stopwatch.StartNew();
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
            var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
            bool allOk = true;
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
            //var engine = _engines[cameraNo];
            //var engine = _threadEngine.Value;
            foreach (var script in scripts[cameraNo])
            {
                try
                {
                    var program = GetOrLoadProgram(script);
                    using var proc = new HDevProcedure(program, "Defect");
                    using var call = new HDevProcedureCall(proc);
                    call.SetInputIconicParamObject("Image", rgbImage);
                    call.Execute();
                    if (call.GetOutputCtrlParamTuple("IsOk").I != 1)
                        allOk = false;
                }
                catch { allOk = false; }
            }
            handle.Free();
            var idx = cameraNo;
            if (allOk) _stats[idx].OkCount++;
            else _stats[idx].NgCount++;
            //if (!allOk)
            //{
            //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo + 1, objectId, rgbImage));
            //    }), DispatcherPriority.Background);
            //}
            ////后续如果需要回调Ng图的缺陷位置时使用
            //else
            //{
            //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo + 1, objectId, rgbImage));
            //    }), DispatcherPriority.Background);
            //}
            //更新 objectStates 并在最后一个相机时发 final result
            if (!objectStates.TryGetValue(objectId, out var state))
            {
                state = new ObjectState();
                objectStates[objectId] = state;
            }

            bool isLast = state.SetResult(cameraNo, allOk);
            if (isLast)
            {
                bool finalOk = state.GetFinalOk();
                idx = 7;
                if (finalOk) _stats[idx].OkCount++;
                else _stats[idx].NgCount++;
                SendResult(objectId, finalOk);
                objectStates.Remove(objectId);
            }
            sw.Stop();
            Trace.WriteLine($"Processed in {sw.ElapsedMilliseconds}ms");
        }

        private void HandleImagePayload(byte[] buf)
        {
            using var ms = new MemoryStream(buf);
            using var br = new BinaryReader(ms);

            br.ReadByte();                      // 帧头
            byte cameraNo = br.ReadByte();
            int objectId = br.ReadInt32();
            br.ReadUInt16();     // 宽
            br.ReadUInt16();    // 高
            br.ReadByte();                      // 通道数
            ushort bpl = br.ReadUInt16();
            br.ReadInt32();                     // 图像总长

            byte[] imgBuf = _pool.Rent(_imgLen);
            int toRead = bpl * height;
            int read = br.Read(imgBuf, 0, toRead);
            Debug.Assert(read == toRead);
            var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
            // 1) Create the three single‐channel planes from the same buffer
            Trace.WriteLine($"[TCP] Received RGB image: camNo:{cameraNo}, objID:{objectId}, bpl={bpl}");
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
                HImage rgbImage = new(image);
                image.Dispose();
                _ = Task.Run(() => ProcessScripts(cameraNo, objectId, rgbImage));
            }
            finally
            {
                handle.Free();
                _pool.Return(imgBuf, clearArray: false);
            }
        }

        private void ProcessScripts(int cameraIndex, int objectId, HImage image)
        {
            var sw = Stopwatch.StartNew();
            //using var engine = _threadEngine.Value;
            bool allOk = true;
            foreach (var script in scripts[cameraIndex])
            {
                try
                {
                    var program = GetOrLoadProgram(script);
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
                    allOk = false;
                }
            }
            sw.Stop();
            var idx = cameraIndex;
            if (allOk) _stats[idx].OkCount++;
            else _stats[idx].NgCount++;
            //if (!allOk)
            //{
            //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraIndex + 1, objectId, image));
            //    }), DispatcherPriority.Background);
            //}
            ////后续如果需要回调Ng图的缺陷位置时使用
            //else
            //{
            //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraIndex + 1, objectId, image));
            //    }), DispatcherPriority.Background);
            //}
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
                idx = 7;
                if (finalOk) _stats[idx].OkCount++;
                else _stats[idx].NgCount++;
                SendResult(objectId, finalOk);
                objectStates.Remove(objectId);
            }
            Trace.WriteLine("Time cost for Scripts: " + sw.ElapsedMilliseconds);
        }

        private void SendResult(int objectId, bool isOk)
        {
            if (_stream == null || !_client.Connected)
                return;

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
    }
}
