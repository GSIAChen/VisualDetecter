using HalconDotNet;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public abstract class ToolBaseViewModel : INotifyPropertyChanged
{
    public virtual HObject CurrentResultImage => _resultImage;
    public abstract void Apply();
    protected HObject _inputImage;
    public HObject _resultImage;
    public HWindowControl _hWindowControl;
    private int _selectedSourceIndex = -1;
    public int SelectedSourceIndex
    {
        get => _selectedSourceIndex;
        set
        {
            Debug.WriteLine($"SelectedSourceIndex changed to: {value}");
            _selectedSourceIndex = value;
            OnPropertyChanged();
        }
    }

    public virtual void Initialize(HWindowControl hwin)
    {
        _hWindowControl = hwin;
    }

    public virtual void SetInputImage(HObject image)
    {
        if (_inputImage == image) return;
        _inputImage = image;
    }

    public virtual void SaveResultImage(string path)
    {
        if (CurrentResultImage != null && CurrentResultImage.IsInitialized())
        {
            Debug.WriteLine("Saving Result Image to: " + path);
            HOperatorSet.WriteImage(CurrentResultImage, "png", 0, path);
        }
    }

    public virtual HObject LoadInputImage(string path)
    {
        if (System.IO.File.Exists(path))
        {
            HObject img;
            HOperatorSet.ReadImage(out img, path);
            return img;
        }
        return null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ToolInstance
{
    public Guid InstanceId { get; set; }
    public string ToolKey { get; set; }
    public string DisplayName { get; set; }
    public ToolBaseViewModel ViewModel { get; set; }
    public System.Windows.Controls.UserControl SettingsPage { get; set; }
}