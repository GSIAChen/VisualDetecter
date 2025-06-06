using HalconDotNet;
using MyWPF1.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using static MyWPF1.ViewModels.SelectableItem;

public class ImageViewModel : INotifyPropertyChanged
{
    private HObject _image;
    private HWindowControl _hWindowControl;
    private ToolInstance _currentToolInstance;
    public ToolInstance CurrentToolInstance
    {
        get => _currentToolInstance;
        set
        {
            if (_currentToolInstance == value) return;
            _currentToolInstance = value;
            OnPropertyChanged(nameof(CurrentToolInstance));
            CurrentTool = _currentToolInstance?.ViewModel;
            CurrentTool?.Apply();
        }
    }
    private ToolBaseViewModel _currentTool;
    public ToolBaseViewModel CurrentTool
    {
        get => _currentTool;
        set
        {
            if (_currentTool == value) return;
            _currentTool = value;
            OnPropertyChanged(nameof(CurrentTool));
        }
    }

    public ImageViewModel(ArrowViewModel arrowVM)
    {
        arrowVM.ToolInstanceAdded += OnToolInstanceAdded;
    }

    private void OnToolInstanceAdded(ToolInstance instance)
    {
        Debug.WriteLine($"ToolInstance added: {instance.ToolKey}");
        // 判断类型，初始化参数或应用处理
        if (instance.ViewModel is BinaryViewModel binaryVM)
        {
            _currentToolInstance = instance;
            _currentTool = binaryVM;
            if (_hWindowControl != null && _image != null)
            {
                binaryVM.Initialize(_hWindowControl, _image);
            }
        }
    }

    public void Initialize(HWindowControl hwin, string imagePath)
    {
        Debug.WriteLine("ImageViewModel Initialized");
        _hWindowControl = hwin;
        HOperatorSet.ReadImage(out _image, imagePath);
        HOperatorSet.GetImageSize(_image, out HTuple width, out HTuple height);
        HTuple row = 0;
        HTuple col = 0;
        _hWindowControl.HalconWindow.SetPart(row, col, height - 1, width - 1);
        _hWindowControl.HalconWindow.DispObj(_image);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}