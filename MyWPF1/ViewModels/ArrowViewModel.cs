using MyWPF1.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static MyWPF1.ViewModels.SelectableItem;

namespace MyWPF1.ViewModels;

public class ArrowViewModel : INotifyPropertyChanged
{
    public event Action<ToolInstance> ToolInstanceAdded;
    public ObservableCollection<ExpandableItem> EItems { get; } = [];
    public ObservableCollection<SelectableItem> PreprocessingItems { get; } = [];
    public ObservableCollection<SelectableItem> PositioningItems { get; } = [];
    public ObservableCollection<SelectableItem> MeasureItems { get; } = [];
    public ObservableCollection<SelectableItem> FittingItems { get; } = [];
    public ObservableCollection<SelectableItem> PredictItems { get; } = [];
    private ObservableCollection<SelectableItem> _selectedItems = [];
    public ObservableCollection<BinarySettingPage> OpenedPanels { get; } = [];
    public static ObservableCollection<ToolInstance> ToolInstances { get; } = [];
    private ToolInstance _currentToolInstance;
    public ToolInstance CurrentToolInstance
    {
        get => _currentToolInstance;
        set { _currentToolInstance = value; OnPropertyChanged(); }
    }
    public ObservableCollection<SelectableItem> SelectedItems
    {
        get => _selectedItems;
        set { _selectedItems = value; OnPropertyChanged(); }
    }

    public IEnumerable<object> ComboItems
    {
        get
        {
            // 先一个固定的“原图”字符串，然后再是用户拖入的那些 SelectableItem
            yield return "原图";
            foreach (var item in SelectedItems)
                yield return item;
        }
    }

    public ArrowViewModel()
    {
        PreprocessingItems.Add(new SelectableItem { Text = "二值化" });
        PreprocessingItems.Add(new SelectableItem { Text = "色彩变换" });
        PreprocessingItems.Add(new SelectableItem { Text = "图片增强" });
        PositioningItems.Add(new SelectableItem { Text = "灰度匹配" });
        PositioningItems.Add(new SelectableItem { Text = "轮廓模板匹配" });
        MeasureItems.Add(new SelectableItem { Text = "尺寸检测-边到边" });
        MeasureItems.Add(new SelectableItem { Text = "面积检测" });
        FittingItems.Add(new SelectableItem { Text = "线拟合" });
        FittingItems.Add(new SelectableItem { Text = "双直线交点" });
        FittingItems.Add(new SelectableItem { Text = "拟合测量" });
        PredictItems.Add(new SelectableItem { Text = "结果输出" });

        SelectedItems.CollectionChanged += (s, e) =>
        {
            foreach (SelectableItem item in e.NewItems?.OfType<SelectableItem>() ?? [])
                item.IsSelected = true;
            foreach (SelectableItem item in e.OldItems?.OfType<SelectableItem>() ?? [])
                item.IsSelected = false;
            for (int i = 0; i < SelectedItems.Count; i++)
                SelectedItems[i].Index = i + 1;
            OnPropertyChanged(nameof(ComboItems));
        };
    }

    // INotifyPropertyChanged 实现
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public void AddToolInstance(SelectableItem item, Func<ToolBaseViewModel> factory)
    {
        // 如果该 SelectableItem 已绑定 ToolInstance，则直接返回已有实例
        var existing = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (existing != null)
        {
            CurrentToolInstance = existing;
            return;
        }

        int count = ToolInstances.Count(x => x.ToolKey == item.ToolKey);
        var toolInstance = new ToolInstance
        {
            InstanceId = item.InstanceId,
            ToolKey = item.Text,
            DisplayName = $"{item.ToolKey} {count + 1}",
            ViewModel = factory()
        };
        Debug.WriteLine("New Tool Name: "+ toolInstance.ToolKey);
        ToolInstances.Add(toolInstance);
        CurrentToolInstance = toolInstance;
        ToolInstanceAdded?.Invoke(toolInstance);
    }
}

public class SelectableItem : INotifyPropertyChanged
{
    private bool _isSelected;
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    public required string Text { get; set; }
    // 用于显示的 “序号 + 文本”
    private int _index;
    // 用于唯一标识工具类型
    public string ToolKey { get; set; }
    public int Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
    }

    public string DisplayText => $"{Index} {Text}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
    public override string ToString() => DisplayText;

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ToolInstance
    {
        public Guid InstanceId { get; set; }
        public string ToolKey { get; set; }
        public string DisplayName { get; set; }
        public ToolBaseViewModel ViewModel { get; set; }
        public System.Windows.Controls.UserControl SettingsPage { get; set; }
        public System.Windows.Controls.UserControl TopPanelPage { get; set; }

        public static bool ContainsKey(int key)
        {
            if (key <= ArrowViewModel.ToolInstances.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
