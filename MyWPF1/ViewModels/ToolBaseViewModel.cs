using HalconDotNet;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class ToolBaseViewModel : INotifyPropertyChanged
{
    public abstract HObject CurrentResultImage { get; }  // 工具执行完毕后的图像结果

    public virtual void SaveResultImage(string path)
    {
        if (CurrentResultImage != null && CurrentResultImage.IsInitialized())
        {
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

    public abstract void Initialize(HWindowControl hwin, HObject baseImage);
    public abstract void Apply();
}