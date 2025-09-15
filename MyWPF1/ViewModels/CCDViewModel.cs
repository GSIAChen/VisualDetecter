using HalconDotNet;
using MyWPF1;
using MyWPF1.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class CCDViewModel : INotifyPropertyChanged
{
    public string CCDName { get; }
    private static readonly Guid OriginalSourceId = Guid.Empty;
    public ImageSourceItem OriginalSource { get; private set; }
    private HWindowControl _hwin;
    private HObject _currentImage;  // 原图或上一个工具的输出
    public AlgorithmTopPage TopPage { get; set; }

    // —— 一套 CCD 专属的状态 ——  
    public ObservableCollection<SelectableItem> SelectedItems { get; } = new();
    public ObservableCollection<ToolInstance> ToolInstances { get; private set; } = new();
    public ObservableCollection<ImageSourceItem> ImageSources { get; private set; } = new();
    private ToolInstance _currentToolInstance;
    public ToolInstance CurrentToolInstance
    {
        get => _currentToolInstance;
        set { _currentToolInstance = value; OnPropertyChanged(); }
    }
    private SelectableItem _selectedItem;
    public SelectableItem SelectedItem
    {
        get => _selectedItem;
        set { _selectedItem = value; OnPropertyChanged(); }
    }

    public CCDViewModel(string name)
    {
        CCDName = name;
        SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
        SelectedItems.CollectionChanged += (_, __) =>
        {
            // 1) 给 SelectableItem 重新打顺序
            for (int i = 0; i < SelectedItems.Count; i++)
                SelectedItems[i].Index = i + 1;

            // 2) 给同序号的 ToolInstance 重新 DisplayName
            for (int i = 0; i < SelectedItems.Count; i++)
            {
                var sel = SelectedItems[i];
                var inst = ToolInstances.FirstOrDefault(t => t.InstanceId == sel.InstanceId);
                if (inst != null)
                {
                    inst.DisplayName = $"{i + 1} {inst.ToolKey}";
                }

                // 3) 同步它对应的 ImageSourceItem.Name
                var src = ImageSources.FirstOrDefault(s => s.InstanceId == sel.InstanceId);
                if (src != null)
                    src.Name = $"{i + 1} {inst.ToolKey}";
            }

            // 4) 通知 UI 刷新
            OnPropertyChanged(nameof(ToolInstances));
            OnPropertyChanged(nameof(ImageSources));
        };
    }

    private void SelectedItems_CollectionChanged(object s, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move)
        {
            var ids = SelectedItems.Select(si => si.InstanceId).ToList();
            ToolInstances = new ObservableCollection<ToolInstance>(
                ids.Select(id => ToolInstances.First(t => t.InstanceId == id)));
            ImageSources = new ObservableCollection<ImageSourceItem>(
                ids.Select(id => ImageSources.First(src => src.InstanceId == id)));
            OnPropertyChanged(nameof(ToolInstances));
            OnPropertyChanged(nameof(ImageSources));
        }
        // 你已经在 Add/Remove 方法里同步 add/remove 了
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public void Initialize(HWindowControl hwin, HObject orig)
    {
        _hwin = hwin;
        _currentImage = orig;
        OriginalSource = new ImageSourceItem(Guid.Empty, "原图", orig);
        ImageSources.Clear();
    }

    // … 从原来 ArrowVM 剥离过来的方法 …
    public void AddToolInstance(SelectableItem item, Func<ToolBaseViewModel> factory)
    {
        var exist = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (exist != null)
        {
            CurrentToolInstance = exist;
            return;
        }
        int idx = ToolInstances.Count;
        var inst = new ToolInstance
        {
            InstanceId = item.InstanceId,
            ToolKey = item.Text,
            DisplayName = $"{idx + 1} {item.Text}",
            ViewModel = factory()
        };
        inst.ViewModel.Initialize(_hwin);
        inst.ViewModel.SetInputImage(_currentImage);
        ToolInstances.Add(inst);
        CurrentToolInstance = inst;
        _currentImage = inst.ViewModel.CurrentResultImage;
        ImageSources.Add(new ImageSourceItem(inst.InstanceId, inst.DisplayName, _currentImage));
    }

    public void RemoveToolInstance(SelectableItem item)
    {
        var inst = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (inst != null) ToolInstances.Remove(inst);
        SelectedItems.Remove(item);
        var src = ImageSources.FirstOrDefault(i => i.InstanceId == item.InstanceId);
        if (src != null) ImageSources.Remove(src);
        if (SelectedItem?.InstanceId == item.InstanceId)
        {
            SelectedItem = null;
            CurrentToolInstance = null;
        }
    }
}