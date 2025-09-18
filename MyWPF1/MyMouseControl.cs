using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Windows.Forms;
using HalconDotNet;

namespace MyWPF1
{


    public class MyMouseControl : HWindowControl
    {
        public event EventHandler<Point>? WinFormsDoubleClicked;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == WM_LBUTTONDBLCLK)
                {
                    var p = GetPointFromLParam(m.LParam);
                    WinFormsDoubleClicked?.Invoke(this, new Point(p.X, p.Y));
                    return; // 阻止基类继续处理（避免 Halcon 的内部回调）
                }

                // 若你希望屏蔽其它消息也可以 return，这里示例完全屏蔽 move/down/up：
                if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // 保险起见：不要让拦截抛异常干扰消息循环
                System.Diagnostics.Trace.WriteLine("[NoMouseHWindowControl] WndProc error: " + ex);
            }

            base.WndProc(ref m);
        }

        private static Point GetPointFromLParam(IntPtr lParam)
        {
            // lParam 低16位 X，高16位 Y（签名位考虑）
            int lp = lParam.ToInt32();
            int x = (short)(lp & 0xFFFF);
            int y = (short)((lp >> 16) & 0xFFFF);
            return new Point(x, y);
        }
    }

}
