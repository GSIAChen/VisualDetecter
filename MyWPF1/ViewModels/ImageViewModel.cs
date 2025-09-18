using HalconDotNet;
using MyWPF1.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using static MyWPF1.ViewModels.SelectableItem;

public class ImageViewModel : INotifyPropertyChanged
{
    private readonly ArrowViewModel _arrowVM;
    public HObject _image;
    public HWindowControl _hWindowControl;
    public event Action<HWindowControl, HObject>? Initialized;
    private ToolInstance _currentToolInstance;
    public ToolInstance CurrentToolInstance
    {
        get => _currentToolInstance;
        set
        {
            if (_currentToolInstance == value) return;
            _currentToolInstance = value;
            OnPropertyChanged(nameof(CurrentToolInstance));
            CurrentTool = _currentToolInstance.ViewModel;
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
        _arrowVM = arrowVM;
        arrowVM.ToolInstanceAdded += OnToolInstanceAdded;
    }

    private void OnToolInstanceAdded(ToolInstance instance)
    {
        Trace.WriteLine($"ToolInstance added: {instance.ToolKey}");
        _currentToolInstance = instance;
        _currentTool = instance.ViewModel;
        if (_hWindowControl != null && _image != null)
        {
            _currentTool.Initialize(_hWindowControl);
        }
    }

    public void Initialize(HWindowControl hwin, HTuple imagePath)
    {
        _hWindowControl = hwin;
        HOperatorSet.ReadImage(out _image, imagePath);
        HOperatorSet.GetImageSize(_image, out HTuple width, out HTuple height);
        HTuple row = 0;
        HTuple col = 0;
        _hWindowControl.HalconWindow.SetPart(row, col, height - 1, width - 1);
        _hWindowControl.HalconWindow.DispObj(_image);
        Initialized?.Invoke(hwin, _image);
        Trace.WriteLine("ImageViewModel Initialized");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    internal HWindowControl InitializeAndGetWindowControl()
    {
        throw new NotImplementedException();
    }
}