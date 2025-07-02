using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace MyWPF1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "DispatcherUnhandledException");
            e.Handled = true;  // or false if you want the app to still crash
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");
        }

        private void LogException(Exception ex, string source)
        {
            try
            {
                // 1. 获取可执行文件所在目录
                var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                             ?? AppDomain.CurrentDomain.BaseDirectory;

                // 2. 构造 log 子目录
                var logDir = Path.Combine(exeDir, "log");
                Directory.CreateDirectory(logDir);

                // 3. 日志文件名按照日期滚动
                var logFile = Path.Combine(logDir,
                    $"crash_{DateTime.Now:yyyyMMdd}.log");

                // 4. 追加写入
                var text = $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex}\n";
                File.AppendAllText(logFile, text);
            }
            catch
            {
                // 记录日志本身失败也不用抛
            }
        }
    }

}
