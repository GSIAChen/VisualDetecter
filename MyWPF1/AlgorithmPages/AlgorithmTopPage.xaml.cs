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
        private ToolInstance _currentToolInstance;
        public ToolInstance CurrentToolInstance { get => _currentToolInstance;
            set
            {
                if (_currentToolInstance == value) return;
                _currentToolInstance = value;
                OnPropertyChanged(nameof(CurrentToolInstance));
            }
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

                var inst = _ccd?.CurrentToolInstance;
                if (inst != null && _selectedSource?.Image != null)
                {
                    // ✅ 设置图像
                    Trace.WriteLine($"Setting input image to: {_selectedSource.Name}");
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

            // 1) **永远**在第 0 位加原图
            FilteredSources.Add(_ccd.OriginalSource);

            // 2) 找到当前工具在 SelectedItems 里的位置
            int pos = _ccd.SelectedItems.IndexOf(_selectedItem);

            // 3) 对应前面工具的输出，依次加入
            //    注意：ImageSources 中只存 toolOutputs，且 index 对齐 SelectedItems
            for (int i = 0; i < pos && i < _ccd.ImageSources.Count; i++)
                FilteredSources.Add(_ccd.ImageSources[i]);

            // 4) 还原上次用户手动选过的那张（或者默认最后一张）
            var inst = _ccd.CurrentToolInstance;
            if (inst != null && FilteredSources.Count > 0)
            {
                int saved = inst.ViewModel.SelectedSourceIndex;
                var pick = (saved >= 0 && saved < FilteredSources.Count)
                           ? FilteredSources[saved]
                           : FilteredSources.Last();
                SelectedSource = pick;
            }
            else if (FilteredSources.Count > 0)
            {
                SelectedSource = FilteredSources.Last();
            }

            OnPropertyChanged(nameof(SelectedSource));
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
