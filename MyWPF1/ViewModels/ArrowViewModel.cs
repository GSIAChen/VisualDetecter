using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using MyWPF1.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace MyWPF1.ViewModels;

public class ArrowViewModel : INotifyPropertyChanged
{
    public event Action<ToolInstance> ToolInstanceAdded;
    private HWindowControl _hwindowControl;
    public event Action OnToolPageShouldBeCleared;
    public ObservableCollection<ExpandableItem> EItems { get; } = [];
    public ObservableCollection<SelectableItem> PreprocessingItems { get; } = [];
    public ObservableCollection<SelectableItem> PositioningItems { get; } = [];
    public ObservableCollection<SelectableItem> MeasureItems { get; } = [];
    public ObservableCollection<SelectableItem> FittingItems { get; } = [];
    public ObservableCollection<SelectableItem> PredictItems { get; } = [];
    private ObservableCollection<SelectableItem> _selectedItems = [];
    public ObservableCollection<BinarySettingPage> OpenedPanels { get; } = [];
    public ObservableCollection<ImageSourceItem> ImageSources { get; } = new ObservableCollection<ImageSourceItem>();
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
    private SelectableItem _selectedItem;
    public SelectableItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
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
            OnPropertyChanged(nameof(ImageSources));
        };
    }

    // INotifyPropertyChanged 实现
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public void SetOriginalImage(HObject originalImage, HWindowControl hWindowControl)
    {
        _hwindowControl = hWindowControl;
        ImageSources.Add(new ImageSourceItem("原图", originalImage));
    }

    public void AddToolInstance(SelectableItem item, Func<ToolBaseViewModel> factory)
    {
        // 如果该 SelectableItem 已绑定 ToolInstance，则直接返回已有实例
        var existing = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (existing != null)
        {
            CurrentToolInstance = existing;
            return;
        }

        int count = ToolInstances.Count;
        var toolInstance = new ToolInstance
        {
            InstanceId = item.InstanceId,
            ToolKey = item.Text,
            DisplayName = $"{count + 1} {item.Text}",
            ViewModel = factory()
        };
        Debug.WriteLine("New Tool Name: " + toolInstance.ToolKey);
        ToolInstances.Add(toolInstance);
        CurrentToolInstance = toolInstance;
        ToolInstanceAdded?.Invoke(toolInstance);

        // 添加这次工具的输出
        toolInstance.ViewModel.Apply();
        ImageSources.Add(new ImageSourceItem(
            toolInstance.DisplayName,
            toolInstance.ViewModel.CurrentResultImage));
    }

    public void RemoveToolInstance(SelectableItem item)
    {
        if (item == null) return;

        var inst = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (inst != null)
        {
            ToolInstances.Remove(inst);
        }
        SelectedItems.Remove(item);

        // ✅ 只有删除的是当前工具时才清空视图
        if (SelectedItem?.InstanceId == item.InstanceId)
        {
            SelectedItem = null;
            CurrentToolInstance = null;

            // ❗ 通知主界面移除 TopPage
            OnToolPageShouldBeCleared?.Invoke();
        }
    }

    public ICommand SaveConfigCommand => new RelayCommand(SaveAllWithDialog);
    public ICommand LoadConfigCommand => new RelayCommand(() => LoadAllWithDialog(_hwindowControl, ImageSources[0].Image));

    public void SaveAllWithDialog()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存图像处理配置",
            Filter = "配置文件 (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "ToolConfig.json"
        };

        if (dlg.ShowDialog() == true)
        {
            SaveAll(dlg.FileName);

            System.Windows.MessageBox.Show("配置已保存到：" + dlg.FileName, "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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

    public void LoadAllWithDialog(HWindowControl hwin, HObject originalImage)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "加载图像处理配置",
            Filter = "配置文件 (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dlg.ShowDialog() == true)
        {
            LoadAll(dlg.FileName, hwin, originalImage);
            System.Windows.MessageBox.Show("配置加载成功：" + dlg.FileName, "加载成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void LoadAll(string path, HWindowControl hwin, HObject originalImage)
    {
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var pipeline = JsonSerializer.Deserialize<PipelineConfig>(json);
        if (pipeline == null || pipeline.Tools.Count == 0)
            return;  // nothing to load

        // 清空已存在的工具
        SelectedItems.Clear();
        ToolInstances.Clear();
        HObject prevImage = originalImage;

        // 依次重建
        foreach (var cfg in pipeline.Tools)
        {
            // 新增一个 SelectableItem 占位（Text 与 ToolKey 相同）
            var item = new SelectableItem { Text = cfg.ToolKey, ToolKey = cfg.ToolKey };
            SelectedItems.Add(item);

            // 创建工具实例并设置参数
            AddToolInstance(item, () => AlgorithmWindow.CreateViewModelByKey(cfg.ToolKey));

            CurrentToolInstance.ViewModel.Initialize(hwin);
            CurrentToolInstance.ViewModel.SetInputImage(prevImage);
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
                    if (prop == null || !prop.CanWrite)
                        continue;

                    object raw = kv.Value;
                    object converted = null;

                    // 如果是 JsonElement，需要从中提取具体类型
                    if (raw is JsonElement je)
                    {
                        if (prop.PropertyType == typeof(int))
                            converted = je.GetInt32();
                        else if (prop.PropertyType == typeof(double))
                            converted = je.GetDouble();
                        else if (prop.PropertyType == typeof(bool))
                            converted = je.GetBoolean();
                        else if (prop.PropertyType == typeof(string))
                            converted = je.GetString();
                        else
                            continue;  // 不支持的类型
                    }
                    else
                    {
                        converted = Convert.ChangeType(raw, prop.PropertyType);
                    }
                    // 最终设置属性
                    prop.SetValue(vm, converted);
                }
            }
            prevImage = CurrentToolInstance.ViewModel.CurrentResultImage;
            CurrentToolInstance = ToolInstances.First();
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
}
