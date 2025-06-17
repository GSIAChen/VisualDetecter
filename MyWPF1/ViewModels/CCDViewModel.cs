using HalconDotNet;
using MyWPF1;
using MyWPF1.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class CCDViewModel : INotifyPropertyChanged
{
    public string CCDName { get; }
    private HWindowControl _hwin;
    private HObject _currentImage;  // 原图或上一个工具的输出

    // —— 一套 CCD 专属的状态 ——  
    public ObservableCollection<SelectableItem> SelectedItems { get; } = new();
    public ObservableCollection<ToolInstance> ToolInstances { get; } = new();
    public ObservableCollection<ImageSourceItem> ImageSources { get; } = new();
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
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public void Initialize(HWindowControl hwin, HObject orig)
    {
        _hwin = hwin;
        _currentImage = orig;
        ImageSources.Clear();
        ImageSources.Add(new ImageSourceItem("原图", orig));
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
        ImageSources.Add(new ImageSourceItem(inst.DisplayName, _currentImage));
    }

    public void RemoveToolInstance(SelectableItem item)
    {
        var inst = ToolInstances.FirstOrDefault(t => t.InstanceId == item.InstanceId);
        if (inst != null) ToolInstances.Remove(inst);
        SelectedItems.Remove(item);
        if (SelectedItem?.InstanceId == item.InstanceId)
        {
            SelectedItem = null;
            CurrentToolInstance = null;
        }
    }
}