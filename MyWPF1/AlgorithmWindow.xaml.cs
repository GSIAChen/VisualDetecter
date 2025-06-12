using HalconDotNet;
using MyWPF1.ViewModels;
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
        static ArrowViewModel arrowVM = new ArrowViewModel();
        static ImageViewModel imageVM = new ImageViewModel(arrowVM);
        ImagePage imgPage = new ImagePage { DataContext = imageVM };


        public AlgorithmWindow()
        {
            InitializeComponent();
            ImageFrame.Content = imgPage;
            this.DataContext = arrowVM;
            arrowVM.OnToolPageShouldBeCleared += () =>
            {
                _topPage = null;
                AlgorithmTopContainer.Content = null;
                TopFrame.Content = null;
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
            var vm = (ArrowViewModel)DataContext;
            var listBox = (System.Windows.Controls.ListBox)sender;

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
                    int oldIdx = vm.SelectedItems.IndexOf(dragged);
                    int newIdx = vm.SelectedItems.IndexOf(target);
                    vm.SelectedItems.Move(oldIdx, newIdx);
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

                vm.SelectedItems.Add(copy);
                // 创建并绑定对应工具 ViewModel
                vm.AddToolInstance(copy, () => CreateViewModelByKey(copy.Text));
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
            if (TargetListBox.SelectedItem is not SelectableItem selectedTool) return;

            var vm = (ArrowViewModel)DataContext;
            vm.SelectedItem = selectedTool;
            // 查找或添加 ToolInstance
            vm.AddToolInstance(selectedTool, () => CreateViewModelByKey(selectedTool.ToolKey));
            var toolInstance = vm.CurrentToolInstance;

            // 2. 选择输入图：上一步工具输出或原图
            HObject input;
            int idx = vm.ToolInstances.IndexOf(toolInstance);
            if (idx > 0)
            {
                input = vm.ToolInstances[idx - 1].ViewModel.CurrentResultImage;
            }
            else
            {
                input = vm.ImageSources[0].Image;  // 原图
            }

            // 3. 初始化并真正设置输入
            toolInstance.ViewModel.Initialize(imageVM._hWindowControl);
            toolInstance.ViewModel.SetInputImage(input);

            // 用单例 TopPage
            if (_topPage == null)
            {
                _topPage = new AlgorithmTopPage(arrowVM);
                AlgorithmTopContainer.Content = _topPage;
                TopFrame.Content = _topPage;
            }
            _topPage.SelectedItem = selectedTool;
            // **关键：手动刷新图源下拉列表**，传入当前选中的 SelectableItem
            _topPage.RefreshFor(selectedTool);

            // 加载设置面板
            if (toolInstance.SettingsPage == null)
            {
                toolInstance.SettingsPage = toolInstance.ToolKey switch
                {
                    "二值化" => new BinarySettingPage(),
                    "色彩变换" => new ColorTransformPage(),
                    "图像增强" => new ImageEnhancementPage(),
                    "边缘提取" => new EdgeExtractionPage(),
                    "面积检测" => new AreaDetectionPage(),
                    "线拟合" => new LineFittingPage(),
                    _ => new DefaultSettings()
                };
                toolInstance.SettingsPage.DataContext = toolInstance.ViewModel;
            }

            SettingsContainer.Content = toolInstance.SettingsPage;
            toolInstance.ViewModel.Apply();
        }

        public static ToolBaseViewModel CreateViewModelByKey(string toolKey)
        {
            return toolKey switch
            {
                "二值化" => new BinaryViewModel(),
                "色彩变换" => new ColorTransformViewModel(),
                "图像增强" => new ImageEnhancementViewModel(),
                "边缘提取" => new EdgeExtractionViewModel(),
                "面积检测" => new AreaDetectionViewModel(),
                "线拟合" => new LineFittingViewModel(),

                _ => throw new NotImplementedException(),
            };
        }

        public void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            int index = 0;
            string text = "";
            foreach (SelectableItem item in arrowVM.SelectedItems)
            {
                if (item.IsSelected)
                {
                    index = arrowVM.SelectedItems.IndexOf(item) + 1;
                    text = item.Text;
                    break;
                }
            }
            string imagePath = $"images/{index} {text}.png";
            arrowVM.CurrentToolInstance.ViewModel.SaveResultImage(imagePath);
        }

        private void OnRemoveSelectedItem(object sender, RoutedEventArgs e)
        {
            var selected = (SelectableItem)TargetListBox.SelectedItem;
            if (selected != null)
            {
                arrowVM.RemoveToolInstance(selected);
            }
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