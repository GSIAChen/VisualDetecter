using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using System.Windows.Controls;

namespace MyWPF1
{
    public partial class ImagePage : System.Windows.Controls.UserControl
    {
        // 对外暴露事件，AlgorithmWindow 订阅它来打开对话框并加载图片
        public event EventHandler? DoubleClicked;

        private MyMouseControl? _hostControl;

        public ImagePage()
        {
            InitializeComponent();
        }

        // 提供一个方法让外部安装 (WinForms) child 控件
        public void AttachWinFormsChild(System.Windows.Forms.Control child)
        {
            // 如果之前有 child，先移除/Dispose
            if (winFormsHost.Child != null)
            {
                try { winFormsHost.Child.Dispose(); } catch { }
                winFormsHost.Child = null;
            }

            winFormsHost.Child = child;
            // 记住引用（如果是 MyMouseControl）
            _hostControl = child as MyMouseControl;

            // 确保 WinForms control 已创建 handle（避免初始化时 halcon 报 NULL handle）
            try
            {
                // CreateControl/Handle 会强制创建底层 Win32 handle（在需要时）
                if (!_hostControl.IsHandleCreated)
                    _hostControl.CreateControl();
                var h = _hostControl.Handle; // 触发创建
            }
            catch { /* 忽略创建失败，这里不应抛出 */ }
        }

        // 专门安装 MyMouseControl，并把其事件转成 ImagePage.DoubleClicked（WPF）
        public void AttachMyMouseControl(MyMouseControl myMouse)
        {
            AttachWinFormsChild(myMouse);

            // 解除旧订阅，避免多次重复订阅（可选）
            myMouse.WinFormsDoubleClicked -= MyMouse_WinFormsDoubleClicked;
            myMouse.WinFormsDoubleClicked += MyMouse_WinFormsDoubleClicked;
        }

        private void MyMouse_WinFormsDoubleClicked(object? sender, Point e)
        {
            // 把 WinForms 事件转到 WPF 事件（在 UI 线程调用）
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                DoubleClicked?.Invoke(this, EventArgs.Empty);
            }));
        }

        // 对外暴露当前的 Halcon 控件（作为 HWindowControl 使用）
        // 注意：调用方在使用前应检查返回值非 null
        public HalconDotNet.HWindowControl? HostHWindowControl => _hostControl;
    }
}
