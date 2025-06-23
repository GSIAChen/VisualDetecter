using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using HalconDotNet;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MyWPF1
{
    /// <summary>
    /// Interaction logic for ScriptWindow.xaml
    /// </summary>
    public partial class ScriptWindow : Window
    {
        public event EventHandler<CameraResultEventArgs> CameraResultReported;
        private HDevEngine _engine;
        public ObservableCollection<string>[] Scripts { get; }
        private const string DefaultImagePath = "F:\\钽电容\\新缺陷例图\\新\\Image_20250612163038577.bmp"; // 默认测试图像路径

        public ScriptWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化 HDevelop 引擎
            _engine = new HDevEngine();
            _engine.SetEngineAttribute("execute_procedures_jit_compiled", "true");

            // 初始化脚本列表
            Scripts = new ObservableCollection<string>[7];
            for (int i = 0; i < 7; i++)
                Scripts[i] = new ObservableCollection<string>();
        }

        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;
            int index = Convert.ToInt32(fe.Tag);

            var dlg = new OpenFileDialog
            {
                Filter = "HDevelop 脚本 (*.hdev)|*.hdev",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                Scripts[index].Add(dlg.FileName);
            }
        }

        private void RunScripts_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;
            int index = Convert.ToInt32(fe.Tag);
            bool allOk = true;

            // 加载默认图像
            HObject image;
            HOperatorSet.ReadImage(out image, DefaultImagePath);

            foreach (var script in Scripts[index])
            {
                try
                {
                    _engine.SetProcedurePath(Path.GetDirectoryName(script));
                    var program = new HDevProgram(script);

                    // 创建 HDevProcedure，指定脚本主过程名称（替换为你的过程名）
                    var procedure = new HDevProcedure(program, "LargeDefect1");
                    var procCall = new HDevProcedureCall(procedure);

                    // 设置图像输入到过程的 Image 参数
                    procCall.SetInputIconicParamObject("ImagePath", image);

                    // 执行过程
                    procCall.Execute();

                    // 获取过程输出参数 Result
                    HObject resultImage = procCall.GetOutputIconicParamObject("DefectRegions");
                    HTuple result = procCall.GetOutputCtrlParamTuple("IsOk");
                    bool ok = result.I == 1;
                    if (!ok)
                    {
                        allOk = false;
                    }
                }
                catch (Exception ex)
                {
                    allOk = false;
                }
                CameraResultReported?.Invoke(this, new CameraResultEventArgs
                {
                    CameraIndex = index,
                    IsOk = allOk
                });
                Debug.WriteLine($"Script {script} executed. Result: {(allOk ? "OK" : "NG")}");
            }
        }
    }

    public class CameraResultEventArgs : EventArgs
    {
        public int CameraIndex { get; set; }
        public bool IsOk { get; set; }
    }
}
