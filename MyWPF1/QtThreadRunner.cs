using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

public class QtThreadRunner : IDisposable
{
    // kernel32
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    static extern IntPtr LoadLibrary(string lpFileName);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeLibrary(IntPtr hModule);

    // Delegate 定义（假设调用约定为 cdecl）
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitQtDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownQtDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TestDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CreateProjectAgentDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DestroyProjectAgentDelegate(IntPtr agent);

    // 假设是 ANSI string -> use LPStr. 若 native 接受 wchar_t，请改为 UnmanagedType.LPWStr
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetHandByNameDelegate(IntPtr agent, [MarshalAs(UnmanagedType.LPStr)] string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int HandleValidDelegate(IntPtr handle);

    // fields
    private readonly string _dllPath;
    private Thread _thread;
    private BlockingCollection<Action> _queue = new BlockingCollection<Action>();
    private IntPtr _hModule = IntPtr.Zero;
    private InitQtDelegate _initQt;
    private ShutdownQtDelegate _shutdownQt;

    // other function delegates kept as fields so GC 不会回收
    private TestDelegate _test;
    private CreateProjectAgentDelegate _createAgent;
    private DestroyProjectAgentDelegate _destroyAgent;
    private GetHandByNameDelegate _getHandByName;
    private HandleValidDelegate _handleValid;

    private ManualResetEventSlim _ready = new ManualResetEventSlim(false);

    public QtThreadRunner(string dllPath)
    {
        _dllPath = dllPath ?? throw new ArgumentNullException(nameof(dllPath));
    }

    public void Start(ApartmentState apartment = ApartmentState.STA)
    {
        if (!File.Exists(_dllPath)) throw new FileNotFoundException(_dllPath);
        _thread = new Thread(ThreadMain);
        _thread.IsBackground = true;
        _thread.SetApartmentState(apartment);
        _thread.Start();
        if (!_ready.Wait(5000))
            throw new TimeoutException("Qt thread did not become ready.");
    }

    private void ThreadMain()
    {
        try
        {
            _hModule = LoadLibrary(_dllPath);
            if (_hModule == IntPtr.Zero)
            {
                int e = Marshal.GetLastWin32Error();
                Console.WriteLine($"LoadLibrary failed on Qt thread: {e} - {new System.ComponentModel.Win32Exception(e).Message}");
                return;
            }
            Trace.WriteLine($"DLL loaded on Qt thread: {_hModule}");

            // load init/shutdown
            IntPtr pInit = GetProcAddress(_hModule, "testEc3224l_InitQt");
            IntPtr pShutdown = GetProcAddress(_hModule, "testEc3224l_ShutdownQt");

            if (pInit == IntPtr.Zero || pShutdown == IntPtr.Zero)
            {
                Trace.WriteLine("Init/Shutdown not found in DLL exports.");
                return;
            }
            _initQt = Marshal.GetDelegateForFunctionPointer<InitQtDelegate>(pInit);
            _shutdownQt = Marshal.GetDelegateForFunctionPointer<ShutdownQtDelegate>(pShutdown);

            // load other exports (may throw if not found)
            _test = GetDelegate<TestDelegate>("testEc3224l_Test");
            _createAgent = GetDelegate<CreateProjectAgentDelegate>("testEc3224l_CreateProjectAgent");
            _destroyAgent = GetDelegate<DestroyProjectAgentDelegate>("testEc3224l_DestroyProjectAgent");
            _getHandByName = GetDelegate<GetHandByNameDelegate>("testEc3224l_GetHandByName");
            _handleValid = GetDelegate<HandleValidDelegate>("testEc3224l_HandleValid");

            // 初始化 Qt（在本 Qt 线程上）
            int r = _initQt();
            Trace.WriteLine("testEc3224l_InitQt() -> " + r);

            _ready.Set();

            // 处理队列直到 CompleteAdding
            foreach (var action in _queue.GetConsumingEnumerable())
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine("Exception in Qt-thread job: " + ex); }
            }

            // 退出前调用 shutdown
            _shutdownQt();
            Trace.WriteLine("testEc3224l_ShutdownQt() done.");
        }
        finally
        {
            if (_hModule != IntPtr.Zero)
            {
                FreeLibrary(_hModule);
                _hModule = IntPtr.Zero;
            }
        }
    }

    // helper: 获取 delegate 并保持引用
    private T GetDelegate<T>(string name) where T : Delegate
    {
        IntPtr p = GetProcAddress(_hModule, name);
        if (p == IntPtr.Zero)
            throw new EntryPointNotFoundException($"{name} not found in {_dllPath}; GetLastError={Marshal.GetLastWin32Error()}");
        var d = Marshal.GetDelegateForFunctionPointer<T>(p);
        return d;
    }

    // 在 Qt 线程上执行并返回值
    public T RunOnQtThread<T>(Func<T> func)
    {
        if (_thread == null || !_thread.IsAlive) throw new InvalidOperationException("Qt thread not running");
        var evt = new ManualResetEventSlim(false);
        Exception ex = null;
        T result = default;
        _queue.Add(() =>
        {
            try { result = func(); }
            catch (Exception e) { ex = e; }
            finally { evt.Set(); }
        });
        evt.Wait();
        if (ex != null) throw new AggregateException(ex);
        return result;
    }

    // 在 Qt 线程上执行 Action（无返回）
    public void RunOnQtThread(Action action)
    {
        RunOnQtThread<object>(() => { action(); return null; });
    }

    // 公共方法：这些会把调用派发到 Qt 线程并同步返回
    public int Test() => RunOnQtThread(() => _test());

    public IntPtr CreateAgent() => RunOnQtThread(() => _createAgent());

    public void DestroyAgent(IntPtr agent) => RunOnQtThread(() => { _destroyAgent(agent); return 0; });

    public IntPtr GetHandByName(IntPtr agent, string name) => RunOnQtThread(() => _getHandByName(agent, name));

    public int HandleValid(IntPtr handle) => RunOnQtThread(() => _handleValid(handle));

    // 停止并清理（会让线程退出循环，线程内会调用 Shutdown 并卸载 DLL）
    public void Stop()
    {
        if (_queue != null && !_queue.IsAddingCompleted)
        {
            _queue.CompleteAdding();
            _thread?.Join(3000);
        }
    }

    public void Dispose()
    {
        Stop();
        _queue?.Dispose();
    }
}
