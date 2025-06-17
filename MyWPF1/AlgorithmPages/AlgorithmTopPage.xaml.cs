using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using MyWPF1.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using UserControl = System.Windows.Controls.UserControl;

namespace MyWPF1
{
    /// <summary>
    /// AlgorithmTopPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmTopPage : UserControl, INotifyPropertyChanged
    {
        private readonly CCDViewModel _ccd;    // 目前所使用的相机
        public ObservableCollection<ImageSourceItem> FilteredSources { get; } = new();
        public SelectableItem CurrentTool { get; }
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
        private ArrowViewModel ViewModel { get; set; }
        public ImageViewModel ImageVM { get; }
        private ImageSourceItem _selectedSource;
        public ImageSourceItem SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource == value) return;
                _selectedSource = value;
                OnPropertyChanged();

                var inst = ViewModel?.CurrentToolInstance;
                if (inst != null && _selectedSource?.Image != null)
                {
                    // ✅ 设置图像
                    inst.ViewModel.SetInputImage(_selectedSource.Image);

                    // ✅ 记录图源索引
                    var idx = FilteredSources.IndexOf(_selectedSource);
                    if (idx >= 0)
                        inst.ViewModel.SelectedSourceIndex = idx;
                }
            }
        }

        public AlgorithmTopPage(ArrowViewModel arrowVM, CCDViewModel ccd)
        {
            InitializeComponent();
            ViewModel = arrowVM;
            _ccd = ccd;                        // 保存当前 CCDVM
            DataContext = this;
        }

        public ICommand SaveCommand => new RelayCommand(SaveImage);
        public void RefreshFor(SelectableItem selectedTool)
        {
            _selectedItem = selectedTool;
            FilteredSources.Clear();

            // “原图”保持在第一张
            if (_ccd.ImageSources.Count > 0)
                FilteredSources.Add(_ccd.ImageSources[0]);

            // 工具执行顺序决定可选源
            int pos = _ccd.SelectedItems.IndexOf(_selectedItem);
            for (int i = 1; i <= pos && i < _ccd.ImageSources.Count; i++)
                FilteredSources.Add(_ccd.ImageSources[i]);

            // —— 关键：取 CCDVM.CurrentToolInstance ——  
            var toolInstance = _ccd.CurrentToolInstance;
            if (toolInstance != null && FilteredSources.Count > 0)
            {
                int savedIndex = toolInstance.ViewModel.SelectedSourceIndex;
                var targetSource = (savedIndex >= 0 && savedIndex < FilteredSources.Count)
                    ? FilteredSources[savedIndex]
                    : FilteredSources.Last();

                Debug.WriteLine($"Setting SelectedSource to: {targetSource.Name} (Index: {savedIndex})");

                SelectedSource = targetSource;
            }
        }

        private void SaveImage()
        {
            if (ViewModel?.CurrentToolInstance?.ViewModel is ToolBaseViewModel tool)
            {
                var selectedTool = ViewModel.SelectedItems.FirstOrDefault(i => i.InstanceId == ViewModel.CurrentToolInstance.InstanceId);
                if (selectedTool != null)
                {
                    string dir = "images";
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);

                    string fileName = $"{selectedTool.Index} {selectedTool.Text}";
                    string fullPath = System.IO.Path.Combine(dir, fileName);

                    tool.SaveResultImage(fullPath);
                }
            }
        }

        /**
        public void ClearDisplay()
        {
            this.DataContext = null;
            // 或者清空图像窗口内容：
            if (_hWindowControl != null)
                _hWindowControl.HalconWindow.ClearWindow();
        }
        **/
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public class ImageSourceItem
    {
        public string Name { get; }
        public HObject Image { get; }

        public ImageSourceItem(string name, HObject image)
        {
            Name = name;
            Image = image;
        }
        public override string ToString() => Name; // 让 ComboBox 自动显示 Name
    }
}
