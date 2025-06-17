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

    public ObservableCollection<CCDViewModel> CCDs { get; } = new();
    private CCDViewModel _selectedCCD;
    public CCDViewModel SelectedCCD
    {
        get => _selectedCCD;
        set { _selectedCCD = value; OnPropertyChanged(); }
    }

    public ArrowViewModel()
    {
        PreprocessingItems.Add(new SelectableItem { Text = "二值化" });
        PreprocessingItems.Add(new SelectableItem { Text = "色彩变换" });
        PreprocessingItems.Add(new SelectableItem { Text = "图像降噪" });
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
        for (int i = 1; i <= 7; i++)
            CCDs.Add(new CCDViewModel($"CCD{i}"));
        SelectedCCD = CCDs.First(); // 默认选中CCD1
    }

    // INotifyPropertyChanged 实现
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public void SetOriginalImage(HObject originalImage, HWindowControl hWindowControl)
    {
        _hwindowControl = hWindowControl;
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
            toolInstance.InstanceId,
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

    public class ProjectConfig
    {
        // key 是 CCD 的名称，比如 "CCD1"，value 是这一路 CCD 上的工具流水线
        public Dictionary<string, PipelineConfig> CCDPipelines { get; set; } = new();
    }

    public class PipelineConfig
    {
        public List<ToolConfig> Tools { get; set; } = new();
    }
    public class ToolConfig
    {
        public string ToolKey { get; set; }
        public Dictionary<string, object> Params { get; set; }
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
        var project = new ProjectConfig();

        // 假设 ArrowVM 里有一个 ObservableCollection<CCDViewModel> CCDs
        foreach (var ccd in CCDs)
        {
            var pipeline = new PipelineConfig();
            // 对每个 CCD，遍历它的 SelectedItems/ToolInstances
            foreach (var item in ccd.SelectedItems)
            {
                var inst = ccd.ToolInstances.First(t => t.InstanceId == item.InstanceId);
                var vm = inst.ViewModel;

                var cfg = new ToolConfig
                {
                    ToolKey = inst.ToolKey,
                    Params = new Dictionary<string, object>()
                };

                foreach (var prop in vm.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    var val = prop.GetValue(vm);
                    if (val is string || val is ValueType)
                        cfg.Params[prop.Name] = val;
                }

                pipeline.Tools.Add(cfg);
            }

            project.CCDPipelines[ccd.CCDName] = pipeline;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(project, options);
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
        var project = JsonSerializer.Deserialize<ProjectConfig>(json);
        if (project == null) return;

        // 对每一路 CCD 分别加载
        foreach (var ccd in CCDs)
        {
            ccd.SelectedItems.Clear();
            ccd.ToolInstances.Clear();

            if (!project.CCDPipelines.TryGetValue(ccd.CCDName, out var pipeline))
                continue; // 这个 CCD 在配置里没有流水线，就跳过

            HObject prev = originalImage;
            foreach (var cfg in pipeline.Tools)
            {
                // 1) 造一个 SelectableItem
                var item = new SelectableItem
                {
                    Text = cfg.ToolKey,
                    ToolKey = cfg.ToolKey
                };
                ccd.SelectedItems.Add(item);

                // 2) 新建一个 ToolInstance
                ccd.AddToolInstance(item, () => AlgorithmWindow.CreateViewModelByKey(cfg.ToolKey));
                var inst = ccd.CurrentToolInstance;
                var vm = inst.ViewModel;

                // 3) 初始化并注入输入图
                vm.Initialize(hwin);
                vm.SetInputImage(prev);

                // 4) 还原参数
                foreach (var kv in cfg.Params)
                {
                    var prop = vm.GetType().GetProperty(kv.Key);
                    if (prop == null || !prop.CanWrite)
                        continue;

                    object raw = kv.Value!;
                    object value;

                    if (raw is JsonElement je)
                    {
                        // 直接用 JsonElement 反序列化到目标属性类型
                        value = je.Deserialize(prop.PropertyType)!;
                    }
                    else
                    {
                        // 其它基础类型，保持兼容
                        value = Convert.ChangeType(raw, prop.PropertyType)!;
                    }

                    // 最终设置属性
                    prop.SetValue(vm, value);
                }

                prev = inst.ViewModel.CurrentResultImage;
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
}

public class ImageSourceItem
{
    public Guid InstanceId { get; }
    public string Name { get; set; }
    public HObject Image { get; }

    public ImageSourceItem(Guid instanceId, string name, HObject image)
    {
        InstanceId = instanceId;
        Name = name;
        Image = image;
    }

    public override string ToString() => Name; // 让 ComboBox 自动显示 Name
}
