using HalconDotNet;
using System.Windows;
using System.Windows.Forms.Integration;

namespace MyWPF1
{
    /// <summary>
    /// ImagePage.xaml 的交互逻辑
    /// </summary>
    public partial class ImagePage : System.Windows.Controls.UserControl
    {
        public ImagePage()
        {
            InitializeComponent();
            Loaded += ImagePage_Loaded;
        }

        private void ImagePage_Loaded(object sender, RoutedEventArgs e)
        {
            // 使用Dispatcher确保在UI渲染完成后执行初始化
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 确保控件已经完成布局和渲染
                if (hWindowControl != null && DataContext is ImageViewModel vm)
                {
                    // 使用有效的图像路径 - 这里应该是实际图像路径
                    //string imagePath = @"C:\Users\gsia\AppData\Local\Programs\MVTec\HALCON-24.11-Progress-Steady\doc_en_US\html\manuals\surface_based_matching\images\bin_threshold_6.png"
                    string imagePath = @"fabrik";
                    // 初始化视图模型
                    vm.Initialize(hWindowControl, imagePath);
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
    }
}