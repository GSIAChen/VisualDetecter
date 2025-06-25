using HalconDotNet;
using MyWPF1.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.Primitives;

namespace MyWPF1
{
    /// <summary>
    /// AlgorithmWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmWindow : Window
    {
        private AlgorithmTopPage _topPage;
        private readonly ArrowViewModel _arrowVM;
        private readonly ImageViewModel _imageVM;

        public AlgorithmWindow()
        {
            InitializeComponent();
            //Loaded += AlgorithmWindow_Loaded;

            _arrowVM = new ArrowViewModel();
            _imageVM = new ImageViewModel(_arrowVM);

            this.DataContext = _arrowVM;
            var imgPage = new ImagePage();
            ImageFrame.Content = imgPage;

            // 当窗口真正完成首次渲染后，再初始化 VM
            this.ContentRendered += (s, e) =>
            {
                // Dispatcher 再次延后一个“空闲时机”，确保 HWindowControl 真正拿到尺寸
                Dispatcher.BeginInvoke(() =>
                {
                    // ① 初始化主图
                    string imagePath = @"C:\Users\huanxiangpeng\Desktop\001.png";
                    _imageVM.Initialize(imgPage.hWindowControl, imagePath);

                    // ② 初始化每个 CCDVM
                    foreach (var ccd in _arrowVM.CCDs)
                        ccd.Initialize(imgPage.hWindowControl, _imageVM._image);
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
            _arrowVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ArrowViewModel.SelectedCCD))
                    ShowTopPageForCurrentCCD();
            };
        }

        // 文本点击选择
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.Tag is SelectableItem item)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        private System.Windows.Point _dragStartPoint;

        // 记录鼠标按下位置
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        // 改用和 Drop 里一样的 Key："SelectableItem"
        private void ListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var listBox = (System.Windows.Controls.ListBox)sender;
                if (listBox.SelectedItem is SelectableItem item)
                {
                    var data = new System.Windows.DataObject("SelectableItem", item);
                    DragDrop.DoDragDrop(listBox, data, System.Windows.DragDropEffects.Copy);
                }
            }
        }

        // 阈值：离上下边缘多近就触发滚动
        private const double ScrollThreshold = 60;
        // 每次滚动的距离
        private const double ScrollOffset = 10;
        // 处理拖拽进入时的视觉效果
        private void TargetListBox_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 1. 先做原来的 Drop/Copy 效果判断
            if (e.Data.GetDataPresent("Reorder"))
                e.Effects = System.Windows.DragDropEffects.Move;
            else if (e.Data.GetDataPresent("SelectableItem"))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = (System.Windows.DragDropEffects)GetNone();

            // 2. 自动滚动逻辑
            // 鼠标相对于 ListBox 的坐标
            System.Windows.Point pos = e.GetPosition(TargetListBox);
            double height = TargetListBox.ActualHeight;

            // 靠上时，往上滚
            if (pos.Y < ScrollThreshold)
            {
                RightScrollViewer.ScrollToVerticalOffset(
                    Math.Max(0, RightScrollViewer.VerticalOffset - ScrollOffset));
            }
            // 靠下时，往下滚
            else if (pos.Y > height - ScrollThreshold)
            {
                RightScrollViewer.ScrollToVerticalOffset(
                    RightScrollViewer.VerticalOffset + ScrollOffset);
            }

            e.Handled = true;
        }

        private static object GetNone()
        {
            return System.Windows.DragDropEffects.None;
        }

        // 处理放置操作
        private void TargetListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var arrow = (ArrowViewModel)DataContext;
            var ccd = arrow.SelectedCCD;
            var selectedTool = ccd.SelectedItem;
            var listBox = (System.Windows.Controls.ListBox)sender;

            // 1) 如果之前已经有选中的工具，把它的结果保存到 ImageSources ---
            var previousInst = ccd.CurrentToolInstance;
            if (previousInst != null)
            {
                // 找到它在 ImageSources 中的位置
                var idxSrc = ccd.ImageSources
                    .Select((src, idx) => new { src, idx })
                    .FirstOrDefault(x => x.src.InstanceId == previousInst.InstanceId)?.idx;
                if (idxSrc.HasValue)
                {
                    // 用新的 HObject 替换
                    ccd.ImageSources[idxSrc.Value] = new ImageSourceItem(
                        previousInst.InstanceId,
                        previousInst.DisplayName,
                        previousInst.ViewModel.CurrentResultImage);
                }
            }

            // 1) 内部重排
            if (e.Data.GetDataPresent("Reorder"))
            {
                var dragged = (SelectableItem)e.Data.GetData("Reorder");
                // 找到鼠标下的目标项
                System.Windows.Point pos = e.GetPosition(listBox);
                var target = GetItemUnderMouse(listBox, pos);
                var targetContainer = GetItemContainerUnderMouse(listBox, pos);
                if (dragged != null && target != null && dragged != target)
                {
                    int oldIdx = ccd.SelectedItems.IndexOf(dragged);
                    int newIdx = ccd.SelectedItems.IndexOf(target);
                    ccd.SelectedItems.Move(oldIdx, newIdx);
                    ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(targetContainer);
                    itemsControl.Items.Refresh();
                }
            }
            // 2) 外部添加
            else if (e.Data.GetDataPresent("SelectableItem"))
            {
                var original = (SelectableItem)e.Data.GetData("SelectableItem");

                // 克隆 SelectableItem
                var copy = new SelectableItem
                {
                    Text = original.Text,
                    ToolKey = original.ToolKey,
                    InstanceId = Guid.NewGuid()  // 确保是新工具
                };

                ccd.SelectedItems.Add(copy);
                ccd.SelectedItem = copy;
                // 创建并绑定对应工具 ViewModel
                ccd.AddToolInstance(copy, () => CreateViewModelByKey(copy.Text));
            }
        }

        // 用于排序拖拽
        private System.Windows.Point _reorderDragStart;
        private SelectableItem _reorderDraggedItem;

        // 1) 鼠标按下：记录起点和被点击的项
        private void TargetListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _reorderDragStart = e.GetPosition(null);

            // 记录被按下的那一项
            var listBox = (System.Windows.Controls.ListBox)sender;
            System.Windows.Point p = e.GetPosition(listBox);
            _reorderDraggedItem = GetItemUnderMouse(listBox, p);
        }

        // 2) 鼠标移动：超过阈值后启动拖拽
        private void TargetListBox_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _reorderDraggedItem == null)
                return;

            System.Windows.Point curr = e.GetPosition(null);
            if (Math.Abs(curr.X - _reorderDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(curr.Y - _reorderDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // 启动拖拽，用 Move 效果
            if (_reorderDraggedItem != null)
            {
                var data = new System.Windows.DataObject("Reorder", _reorderDraggedItem);
                DragDrop.DoDragDrop((System.Windows.Controls.ListBox)sender, data, System.Windows.DragDropEffects.Move);
            }
        }

        // 3) 拖拽悬停：只接受同类型
        private void TargetListBox_DragOver_Reorder(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Reorder"))
                e.Effects = System.Windows.DragDropEffects.Move;
            else
                e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        // 修改后的容器获取方法
        private ListBoxItem GetItemContainerUnderMouse(System.Windows.Controls.ListBox listBox, System.Windows.Point point)
        {
            var element = listBox.InputHitTest(point) as DependencyObject;
            while (element != null && !(element is ListBoxItem))
                element = VisualTreeHelper.GetParent(element);

            return element as ListBoxItem;
        }

        // 辅助：根据鼠标位置，找到对应的 ListBoxItem 并返回它的 DataContext
        private SelectableItem GetItemUnderMouse(System.Windows.Controls.ListBox listBox, System.Windows.Point point)
        {
            // 先通过 HitTest 找到 ListBoxItem
            var element = listBox.InputHitTest(point) as DependencyObject;
            while (element != null && !(element is ListBoxItem))
                element = VisualTreeHelper.GetParent(element);

            return (element as ListBoxItem)?.DataContext as SelectableItem;
        }

        // 如果你把 Handler 写在 App.xaml.cs，确保 x:Class 指向 App
        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 找到最近的 ScrollViewer 祖先
            var listBox = (System.Windows.Controls.ListBox)sender;
            var scroll = FindAncestor<ScrollViewer>(listBox);
            if (scroll != null)
            {
                // 根据 Delta 上下滚
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta / 3.0);
                e.Handled = true;
            }
        }

        // 通用的向上递归查找某类型父级
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
                current = VisualTreeHelper.GetParent(current);
            return current as T;
        }

        private void TargetListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 1. 拿到你刚刚点中的那个 SelectableItem
            if (TargetListBox.SelectedItem is not SelectableItem selectedTool)
                return;

            // 2. 拿到 ArrowVM 与 当前 CCDVM
            var arrow = (ArrowViewModel)DataContext;
            var ccd = arrow.SelectedCCD;

            // 1) 如果之前已经有选中的工具，把它的结果保存到 ImageSources ---
            var previousInst = ccd.CurrentToolInstance;
            if (previousInst != null)
            {
                // 找到它在 ImageSources 中的位置
                var idxSrc = ccd.ImageSources
                    .Select((src, idx) => new { src, idx })
                    .FirstOrDefault(x => x.src.InstanceId == previousInst.InstanceId)?.idx;
                if (idxSrc.HasValue)
                {
                    // 用新的 HObject 替换
                    ccd.ImageSources[idxSrc.Value] = new ImageSourceItem(
                        previousInst.InstanceId,
                        previousInst.DisplayName,
                        previousInst.ViewModel.CurrentResultImage);
                }
            }

            // 3. 标记被选中
            ccd.SelectedItem = selectedTool;

            // 4. 调用 AddToolInstance —— 用 selectedTool，而不是以前的 vm.SelectedItem
            ccd.AddToolInstance(selectedTool,
                () => CreateViewModelByKey(selectedTool.Text));
            var inst = ccd.CurrentToolInstance;

            // 7. 弹出（或复用）AlgorithmTopPage，并让它刷新：
            if (ccd.TopPage == null)
            {
                ccd.TopPage = new AlgorithmTopPage(_arrowVM, _arrowVM.SelectedCCD);
                AlgorithmTopContainer.Content = ccd.TopPage;
                TopFrame.Content = ccd.TopPage;
            }
            ccd.TopPage.CurrentToolInstance = inst;
            ccd.TopPage.SelectedItem = selectedTool;
            ccd.TopPage.RefreshFor(selectedTool);

            // 8. 最后把 SettingsPage 塞进去
            if (inst.SettingsPage == null)
            {
                inst.SettingsPage = inst.ToolKey switch
                {
                    "二值化" => new BinarySettingPage(),
                    "色彩变换" => new ColorTransformPage(),
                    "图像降噪" => new ImageDenoisePage(),
                    "图像增强" => new ImageEnhancementPage(),
                    "边缘提取" => new EdgeExtractionPage(),
                    "连通域分离" => new ConnectionPage(),
                    "面积检测" => new AreaDetectionPage(),
                    "线拟合" => new LineFittingPage(),
                    _ => new DefaultSettings()
                };
                inst.SettingsPage.DataContext = inst.ViewModel;
            }
            SettingsContainer.Content = inst.SettingsPage;
            // 9. 最后再 `Apply()` 一次，确保界面更新
            inst.ViewModel.Apply();
        }

        public static ToolBaseViewModel CreateViewModelByKey(string toolKey)
        {
            return toolKey switch
            {
                "二值化" => new BinaryViewModel(),
                "色彩变换" => new ColorTransformViewModel(),
                "图像降噪" => new ImageDenoiseViewModel(),
                "图像增强" => new ImageEnhancementViewModel(),
                "边缘提取" => new EdgeExtractionViewModel(),
                "连通域分离" => new ConnectionViewModel(),
                "面积检测" => new AreaDetectionViewModel(),
                "线拟合" => new LineFittingViewModel(),

                _ => throw new NotImplementedException(),
            };
        }

        public void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            int index = 0;
            string text = "";
            foreach (SelectableItem item in _arrowVM.SelectedItems)
            {
                if (item.IsSelected)
                {
                    index = _arrowVM.SelectedItems.IndexOf(item) + 1;
                    text = item.Text;
                    break;
                }
            }
            string imagePath = $"images/{index} {text}.png";
            _arrowVM.CurrentToolInstance.ViewModel.SaveResultImage(imagePath);
        }

        private void OnRemoveSelectedItem(object sender, RoutedEventArgs e)
        {
            var selected = (SelectableItem)TargetListBox.SelectedItem;
            if (selected != null)
            {
                _arrowVM.RemoveToolInstance(selected);
            }
        }

        private void ShowTopPageForCurrentCCD()
        {
            var ccd = _arrowVM.SelectedCCD;
            if (ccd == null)
            {
                AlgorithmTopContainer.Content = null;
                SettingsContainer.Content = null;
                return;
            }

            if (ccd.TopPage == null)
            {
                // 第一次为这个 CCD 创建它自己的 TopPage
                ccd.TopPage = new AlgorithmTopPage(_arrowVM, ccd);
            }
            AlgorithmTopContainer.Content = ccd.TopPage;
            // 如果你也有 SettingsContainer，需要恢复它上次打开的 SettingsPage：
            // SettingsContainer.Content = ccd.TopPage.CurrentSettingsPage;
        }
    }

    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 直接转换AlternationIndex值（从0开始）
            return (int)value + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ComboBoxIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}