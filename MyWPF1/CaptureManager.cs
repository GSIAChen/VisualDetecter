//using System;
//using System.Runtime.InteropServices;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Threading;

//namespace MyWPF1
//{
//    // === 1. C# 映射结构体（与 C++ header 对应） ===
//    [StructLayout(LayoutKind.Sequential)]
//    public struct GzsiaImage
//    {
//        public short width;
//        public short height;
//        public short pixelType;
//        public short bytePerLine;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public struct SieveCaptureEx
//    {
//        public byte camerId;      // unsigned char
//        // padding will be handled by Marshal; next field is 4-byte aligned
//        public int detectId;
//        public IntPtr image;      // GzsiaImage*
//    }

//    // === 2. 回调委托（对应 __stdcall） ===
//    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
//    public delegate void CaptureCallbackFunc(IntPtr pData, IntPtr pCaptureInfo, IntPtr pUser);

//    // === 3. DllImport（注册/注销） ===
//    // 请确认 DLL 名、路径与导出名称一致；若 DLL 已通过 QtThreadRunner 加载，也可以通过 runner 获取委托并调用
//    internal static class NativeMethods
//    {
//        private const string DllName = "TestEc3224l.dll"; // 改成你的实际 dll 名或完整路径

//        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
//        public static extern bool testEc3224l_RegisterCaptrueCallBack(int hProjectAgent, CaptureCallbackFunc func, IntPtr pUser);

//        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
//        public static extern bool testEc3224l_UnRegisterCaptrueCallBack(int hProjectAgent);
//    }

//    // === 4. 管理类：注册、回调、UI 更新 ===
//    public class CaptureManager : IDisposable
//    {
//        private readonly Dispatcher _uiDispatcher;
//        private readonly System.Windows.Controls.Image _outputImage;

//        // 保持 delegate 不被 GC
//        private CaptureCallbackFunc _nativeCallback;
//        private GCHandle _gchUser; // optional: pinned handle for user state
//        private object _userState; // keep a reference

//        public CaptureManager(System.Windows.Controls.Image outputImage)
//        {
//            _outputImage = outputImage ?? throw new ArgumentNullException(nameof(outputImage));
//            _uiDispatcher = System.Windows.Application.Current.Dispatcher;
//        }

//        /// <summary>注册回调。agentHandle 为 CreateAgent() 返回的整数句柄。</summary>
//        public void Register(int agentHandle, object userState = null)
//        {
//            // 保存引用，防止被回收
//            _nativeCallback = new CaptureCallbackFunc(OnNativeCapture);
//            _userState = userState ?? new object();

//            // 把托管状态转为指针，传给 native（可在回调里读回）
//            _gchUser = GCHandle.Alloc(_userState);
//            IntPtr pUser = GCHandle.ToIntPtr(_gchUser);

//            bool ok = NativeMethods.testEc3224l_RegisterCaptrueCallBack(agentHandle, _nativeCallback, pUser);
//            if (!ok)
//            {
//                // 清理
//                _gchUser.Free();
//                _nativeCallback = null;
//                throw new InvalidOperationException("testEc3224l_RegisterCaptrueCallBack failed.");
//            }
//        }

//        /// <summary>注销回调并释放资源</summary>
//        public void Unregister(int agentHandle)
//        {
//            try
//            {
//                NativeMethods.testEc3224l_UnRegisterCaptrueCallBack(agentHandle);
//            }
//            finally
//            {
//                if (_gchUser.IsAllocated) _gchUser.Free();
//                _nativeCallback = null;
//                _userState = null;
//            }
//        }

//        // 回调实现：注意：此方法通常在 native/Qt 线程调用
//        private void OnNativeCapture(IntPtr pData, IntPtr pCaptureInfo, IntPtr pUser)
//        {
//            try
//            {
//                // 1) 解析 captureInfo 结构体
//                SieveCaptureEx capture = Marshal.PtrToStructure<SieveCaptureEx>(pCaptureInfo);
//                // 2) 解析内部的 GzsiaImage 结构体（指针可能为空）
//                GzsiaImage imgInfo = Marshal.PtrToStructure<GzsiaImage>(capture.image);

//                int width = imgInfo.width;
//                int height = imgInfo.height;
//                int pixelType = imgInfo.pixelType;
//                int stride = imgInfo.bytePerLine;

//                // Sanity checks
//                if (width <= 0 || height <= 0 || stride <= 0) return;

//                // 3) 计算数据长度并复制数据到托管数组
//                int byteCount = stride * height;
//                byte[] managed = new byte[byteCount];
//                // 注意：pData 指向 native 内存，立即复制
//                Marshal.Copy(pData, managed, 0, byteCount);

//                // 4) 根据 pixelType 创建 BitmapSource（你应确认 pixelType 的实际定义）
//                // 假设定义：0 = Gray8, 1 = Bgr24, 2 = Bgra32
//                BitmapSource bmp = null;
//                switch (pixelType)
//                {
//                    case 0: // Gray8
//                        bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, managed, stride);
//                        break;
//                    case 1: // Bgr24 (24bpp)
//                        // WPF PixelFormats.Bgr24 expects B,G,R ordering per pixel.
//                        bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, managed, stride);
//                        break;
//                    case 2: // Bgra32
//                        bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, managed, stride);
//                        break;
//                    default:
//                        // unknown format: try to log and skip
//                        Console.WriteLine($"Unknown pixelType {pixelType}");
//                        return;
//                }

//                // 5) 冻结并派发到 UI 线程
//                bmp.Freeze();
//                _uiDispatcher.BeginInvoke((Action)(() =>
//                {
//                    _outputImage.Source = bmp;
//                }));
//            }
//            catch (Exception ex)
//            {
//                // 记录或显示错误（避免在 native 回调中抛异常）
//                Console.WriteLine("OnNativeCapture Exception: " + ex);
//            }
//        }

//        public void Dispose()
//        {
//            // 不调用 Unregister 的话也尝试释放
//            if (_gchUser.IsAllocated) _gchUser.Free();
//            _nativeCallback = null;
//        }
//    }
//}