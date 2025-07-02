using HalconDotNet;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace MyWPF1
{
    public class TcpDuplexServer
    {
        private int Port;
        private const byte FrameHead = 0xFF;
        public event EventHandler<CameraResultEventArgs> CameraResultReported;

        // 这里只保存单个客户端连接；如果要多个并行，可以用 ConcurrentDictionary<int, TcpClient> 等
        private TcpClient _client;
        private NetworkStream _stream;
        private HDevEngine engine;
        private ObservableCollection<string>[] scripts;
        private Dictionary<int, ObjectState> objectStates;
        public event EventHandler<ImageReceivedEventArgs> ImageReceived;

        // Constructor parameter is named "port"
        public TcpDuplexServer(
            HDevEngine engine,
            ObservableCollection<string>[] scripts,
            Dictionary<int, ObjectState> objectStates,
            int port
        )
        {
            this.engine = engine;
            this.scripts = scripts;
            this.objectStates = objectStates;
            this.Port = port;
        }

        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            listener.Start();
            Console.WriteLine($"[TCP] Listening on port {Port}");

            while (true)
            {
                _client = await listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                Console.WriteLine("[TCP] Client connected");
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

                    recvBuffer.AddRange(tmp.Take(n));
                    // 拆帧
                    while (TryExtractFrame(ref recvBuffer, out var frame))
                    {
                        if (BitConverter.ToString(frame) == "00")
                        {
                            continue;
                        }
                        HandleImagePayload(frame);
                    }
                }
            }
            catch (IOException) { /* 连接出问题 */ }
            finally
            {
                Console.WriteLine("[TCP] Client disconnected");
                client.Close();
            }
        }

        private void HandleImagePayload(byte[] buf)
        {
            using var ms = new MemoryStream(buf);
            using var br = new BinaryReader(ms);

            byte frameHead = br.ReadByte();
            byte cameraNo = br.ReadByte();
            int objectId = br.ReadInt32();
            ushort width = br.ReadUInt16();
            ushort height = br.ReadUInt16();
            byte channels = br.ReadByte();
            ushort bpl = br.ReadUInt16();

            int imgLen = bpl * height;
            byte[] imgBuf = br.ReadBytes(imgLen);

            HImage image;

            if (channels == 1)
            {
                // Pin the managed array so its address is stable
                var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    // Create a gray image from the raw bytes
                    image = new HImage("byte", width, height, ptr);
                }
                finally
                {
                    handle.Free();
                }
            }
            else
            {
                // For interleaved RGB you still need a pointer:
                Debug.WriteLine($"[TCP] Received RGB image: camNo:{cameraNo}, objID:{objectId}, {width}x{height}, Channels={channels}, bpl={bpl}");
                var handle = GCHandle.Alloc(imgBuf, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    // HALCON's GenImageInterleaved has an overload that takes IntPtr
                    image = new HImage();
                    image.GenImageInterleaved(
                        ptr,       // pointer to R,G,BRGB... bytes
                        "rgb",     // channel order
                        width, height,
                        -1,        // plugin
                        "byte",    // pixel type
                        width, height,
                        0, 0, -1, 0
                    );
                }
                finally
                {
                    handle.Free();
                }
            }

            // ... now dispatch to UI thread as before:
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ImageReceived?.Invoke(this, new ImageReceivedEventArgs(cameraNo, objectId, image));
                ProcessScripts(cameraNo-1, objectId, image);
            });
        }

        private void ProcessScripts(int cameraIndex, int objectId, HImage image)
        {
            bool allOk = true;

            foreach (var script in scripts[cameraIndex])
            {
                try
                {
                    // 设置搜索路径 & 加载脚本
                    Debug.WriteLine("Running Scripts");
                    engine.SetProcedurePath(Path.GetDirectoryName(script));
                    var program = new HDevProgram(script);
                    var procedure = new HDevProcedure(program, "Defect");
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

                    Debug.WriteLine($"[Halcon] Camera {cameraIndex+1} result: {isOk}");
                    CameraResultReported?.Invoke(this, new CameraResultEventArgs
                    {
                        CameraIndex = cameraIndex,
                        IsOk = isOk
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Halcon Error] {ex.Message}");
                    allOk = false;
                }
            }
            SendResult(objectId, allOk);
            // 获取或创建该物件的状态
            if (!objectStates.TryGetValue(objectId, out var state))
            {
                state = new ObjectState();
                objectStates[objectId] = state;
            }

            // 填入本机位结果；如果返回 true，说明现在刚好收齐 7 个机位
            bool isLast = state.SetResult(cameraIndex, allOk);

            if (isLast)
            {
                // 累积完毕，计算最终结果
                bool finalOk = state.GetFinalOk();

                // 1) 通知主界面
                CameraResultReported?.Invoke(this, new CameraResultEventArgs
                {
                    CameraIndex = 8,
                    IsOk = finalOk
                });

                // 2) 同步发回 C++：只带 objectId 和 finalOk
                SendResult(objectId, finalOk);

                // 3) 清理
                objectStates.Remove(objectId);
            }
            Debug.WriteLine(
              $"[Done] Cam={cameraIndex}, Obj={objectId} => {(allOk ? "OK" : "NG")}"
            );
        }

        private void SendResult(int objectId, bool isOk)
        {
            if (_stream == null || !_client.Connected)
                return;

            using var bw = new BinaryWriter(_stream, Encoding.Default, leaveOpen: true);
            Debug.WriteLine("Sending Result");
            const byte FrameHead = 0xFF;
            const int PayloadLen = 0x06;    // 1(type) + 4(objectId) + 1(result)
            const byte ResultType = 0x01; // our “result” frame

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
    }
}
