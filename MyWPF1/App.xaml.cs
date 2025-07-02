using System.IO;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace MyWPF1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _logFile;

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
    }
}
