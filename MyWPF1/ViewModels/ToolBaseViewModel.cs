using HalconDotNet;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public abstract class ToolBaseViewModel : INotifyPropertyChanged
{
    public abstract HObject CurrentResultImage { get; }  // 工具执行完毕后的图像结果
    public abstract void Apply();
    protected HObject _inputImage;
    public HObject _resultImage;
    protected HWindowControl _hWindowControl;

    public virtual void Initialize(HWindowControl hwin)
    {
        Debug.WriteLine("A New ViewModel Initialize");
        _hWindowControl = hwin;
    }

    public virtual void SetInputImage(HObject image)
    {
        _inputImage = image;
        Apply();
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