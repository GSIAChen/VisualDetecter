using System.IO;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;
using Application = System.Windows.Application;
using NLog;

namespace MyWPF1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _logFile;

        public App()
        {
            // 初始化NLog
            LogManager.LoadConfiguration("NLog.config");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1) Ensure our crash + debug log folder exists
            var exeDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            ) ?? AppDomain.CurrentDomain.BaseDirectory;
            var logDir = Path.Combine(exeDir, "log");
            Directory.CreateDirectory(logDir);

            // 2) Create a daily-log file, or a single one as you wish
            _logFile = Path.Combine(logDir,
                $"app_{DateTime.Now:yyyyMMdd}.log"
            );

            // 3) Install a listener that writes Trace to that file
            // Use Trace for both Debug and Trace listeners
            var tw = new TextWriterTraceListener(_logFile) { Name = "FileLogger" };
            Trace.Listeners.Add(tw);
            Trace.AutoFlush = true;

            Trace.WriteLine($"=== Application Starting @ {DateTime.Now:O} ===");

            // Hook your global exception logging too:
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"[UI Exception] {e.Exception}");
            // if you still want a crash log:
            LogException(e.Exception, "DispatcherUnhandledException");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.WriteLine($"[Domain Exception] {e.ExceptionObject}");
            LogException(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");
        }

        private void LogException(Exception ex, string source)
        {
            try
            {
                Trace.WriteLine($"[{source}] {ex}");
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // 在后台异步关闭进程（不阻塞 UI 退出）
            _ = Task.Run(async () =>
            {
                try
                {
                    var terminated = ProcessHelper.CloseProcessesByName("TestEc3224l", timeoutMs: 2000);
                    Trace.WriteLine($"(OnExit) Closed {terminated} processes");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("(OnExit) Close processes failed: " + ex);
                }
            });
        }
    }

    public static class ProcessHelper
    {
        /// <summary>
        /// 关闭所有名为 processName 的进程（不带 .exe），先尝试 CloseMainWindow（若有窗口），等待 timeoutMs 毫秒，若未退出再 Kill。
        /// 返回被成功终止或已不存在的进程数量。
        /// </summary>
        public static int CloseProcessesByName(string processName, int timeoutMs = 2000)
        {
            if (string.IsNullOrWhiteSpace(processName)) return 0;

            int count = 0;
            var procs = Process.GetProcessesByName(processName);
            foreach (var p in procs)
            {
                try
                {
                    // 如果进程已经退出则忽略
                    if (p.HasExited)
                    {
                        p.Dispose();
                        continue;
                    }

                    // 尝试优雅关闭（如果进程有主窗口）
                    bool triedCloseMain = false;
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        try
                        {
                            triedCloseMain = p.CloseMainWindow(); // 发送 WM_CLOSE
                        }
                        catch { triedCloseMain = false; }
                    }

                    if (triedCloseMain)
                    {
                        // 等待一段时间让其优雅退出
                        if (p.WaitForExit(timeoutMs))
                        {
                            count++;
                            p.Dispose();
                            continue;
                        }
                    }

                    // 如果没有主窗口或 CloseMainWindow 无效或超时，使用 Kill 强制结束
                    try
                    {
                        p.Kill(true); // .NET Core 3.0+ 可传 true 递归结束子进程
                        if (p.WaitForExit(timeoutMs))
                        {
                            count++;
                        }
                        else
                        {
                            // 最后再尝试标记为已结束（仍然计数）
                            count++;
                        }
                    }
                    catch (Exception exKill)
                    {
                        // 无法 Kill（可能权限或正在退出中），记录并继续
                        Debug.WriteLine($"Kill failed for {processName} (PID={p.Id}): {exKill.Message}");
                    }

                    p.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error closing process {processName}: {ex.Message}");
                }
            }

            return count;
        }
    }
}
