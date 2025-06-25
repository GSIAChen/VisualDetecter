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
        Debug.WriteLine($"ToolInstance added: {instance.ToolKey}");
        _currentToolInstance = instance;
        _currentTool = instance.ViewModel;
        if (_hWindowControl != null && _image != null)
        {
            _currentTool.Initialize(_hWindowControl);
        }
    }

    public void Initialize(HWindowControl hwin, HTuple imagePath)
    {
        try
        {
            Debug.WriteLine("ImageViewModel Initialized");
            _hWindowControl = hwin;
            Debug.WriteLine($"加载图片路径: {imagePath}, 存在: {System.IO.File.Exists(imagePath.ToString())}");
            if (!System.IO.File.Exists(imagePath.ToString()))
            {
                System.Windows.MessageBox.Show($"图片文件不存在: {imagePath}");
                return;
            }
            
            HOperatorSet.ReadImage(out _image, imagePath);
            HOperatorSet.GetImageSize(_image, out HTuple width, out HTuple height);
            HTuple row = 0;
            HTuple col = 0;
            
            // 直接调用Halcon操作，因为HWindowControl本身就是Halcon控件
            _hWindowControl.HalconWindow.SetPart(row, col, height - 1, width - 1);
            _hWindowControl.HalconWindow.DispObj(_image);
            
            Initialized?.Invoke(hwin, _image);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageViewModel.Initialize 异常: {ex.Message}");
            System.Windows.MessageBox.Show($"初始化图像失败: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    internal HWindowControl InitializeAndGetWindowControl()
    {
        throw new NotImplementedException();
    }
}