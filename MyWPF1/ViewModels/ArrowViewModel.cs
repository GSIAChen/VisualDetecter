using HalconDotNet;
using MyWPF1.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static MyWPF1.ViewModels.SelectableItem;
using System.Text.Json;
using System.Reflection;
using System.IO;

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
    public ObservableCollection<ToolInstance> ToolInstances { get; } = new ObservableCollection<ToolInstance>();
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
        PreprocessingItems.Add(new SelectableItem { Text = "图像增强" });
        PreprocessingItems.Add(new SelectableItem { Text = "边缘提取" });
        PositioningItems.Add(new SelectableItem { Text = "灰度匹配" });
        PositioningItems.Add(new SelectableItem { Text = "轮廓模板匹配" });
        MeasureItems.Add(new SelectableItem { Text = "尺寸检测-边到边" });
        MeasureItems.Add(new SelectableItem { Text = "边缘/梯度检测" });
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
        Debug.WriteLine("New Tool Name: " + toolInstance.ToolKey);
        ToolInstances.Add(toolInstance);
        CurrentToolInstance = toolInstance;
        ToolInstanceAdded?.Invoke(toolInstance);
    }

    public void SaveAll(string path)
    {
        var pipeline = new PipelineConfig();

        // 按 SelectedItems 的顺序遍历工具
        foreach (var item in SelectedItems)
        {
            var inst = ToolInstances.First(t => t.InstanceId == item.InstanceId);
            var vm = inst.ViewModel;

            var cfg = new ToolConfig
            {
                ToolKey = inst.ToolKey,
                Params = new Dictionary<string, object>()
            };

            // 用反射遍历 ViewModel 的 public 属性（你需要可调参数都公开为属性）
            foreach (var prop in vm.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                var val = prop.GetValue(vm);
                // 只保存基础类型
                if (val is string || val is ValueType)
                    cfg.Params[prop.Name] = val;
            }

            pipeline.Tools.Add(cfg);
        }

        // 序列化到 JSON，格式美观
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(pipeline, options);
        File.WriteAllText(path, json);
    }

    public void LoadAll(string path, HWindowControl hwin, HObject originalImage)
    {
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var pipeline = JsonSerializer.Deserialize<PipelineConfig>(json);

        // 清空已存在的工具
        SelectedItems.Clear();
        ToolInstances.Clear();

        // 依次重建
        foreach (var cfg in pipeline.Tools)
        {
            // 新增一个 SelectableItem 占位（Text 与 ToolKey 相同）
            var item = new SelectableItem { Text = cfg.ToolKey, ToolKey = cfg.ToolKey };
            SelectedItems.Add(item);

            // 创建工具实例并设置参数
            AddToolInstance(item, () =>
            {
                // 根据 ToolKey 生成正确 ViewModel
                return cfg.ToolKey switch
                {
                    "二值化" => new BinaryViewModel(),
                    "色彩变换" => new ColorTransformViewModel(),
                    "图像增强" => new ImageEnhancementViewModel(),
                    "边缘提取" => new EdgeExtractionViewModel(),
                    "面积检测" => new AreaDetectionViewModel(),
                    _ => throw new InvalidOperationException($"Unknown tool {cfg.ToolKey}")
                };
            });

            var inst = CurrentToolInstance;
            var vm = inst.ViewModel;

            // 给 VM 注入原图或前一个输出
            // 这里假设每个工具在 SetInputImage 里自己 Apply
            if (inst != null)
            {
                HObject input = originalImage;
                // 如果不是第一个，用前一个工具的输出
                if (pipeline.Tools.IndexOf(cfg) > 0)
                {
                    var prevInst = ToolInstances[pipeline.Tools.IndexOf(cfg) - 1];
                    input = prevInst.ViewModel.CurrentResultImage;
                }
                vm.SetInputImage(input);

                // 还原参数
                foreach (var kv in cfg.Params)
                {
                    var prop = vm.GetType().GetProperty(kv.Key);
                    if (prop != null && prop.CanWrite)
                    {
                        // 需要类型匹配，简单 Convert.ChangeType
                        var val = Convert.ChangeType(kv.Value, prop.PropertyType);
                        prop.SetValue(vm, val);
                    }
                }
            }
        }
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
    }
}
