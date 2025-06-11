using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using MyWPF1.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UserControl = System.Windows.Controls.UserControl;

namespace MyWPF1
{
    /// <summary>
    /// AlgorithmTopPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmTopPage : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<ImageSourceItem> ComboItems { get; } = new ObservableCollection<ImageSourceItem>();
        public SelectableItem CurrentTool { get; }
        private SelectableItem _selectedItem;
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
                // 切换图源：把选中的 HObject 传给当前工具
                if (_selectedSource?.Image != null && ViewModel.CurrentToolInstance != null)
                {
                    ViewModel.CurrentToolInstance.ViewModel.SetInputImage(_selectedSource.Image);
                }
            }
        }

        public ICommand SaveCommand => new RelayCommand(SaveImage);

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
        
        public SelectableItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedItem.Text)); // 触发关联属性更新
            }
        }

        public AlgorithmTopPage(SelectableItem item, ArrowViewModel parentViewModel, ImageViewModel imageVM)
        {
            InitializeComponent();

            ViewModel = parentViewModel;
            ImageVM = imageVM;

            // 1. “原图” 
            ComboItems.Add(new ImageSourceItem("原图", ImageVM._image));

            // 2. 已执行的工具：将它们的输出图像加入列表
            var idx = parentViewModel.SelectedItems.IndexOf(item);
            foreach (var tool in parentViewModel.SelectedItems.Take(idx))
            {
                var inst = parentViewModel.ToolInstances
                            .FirstOrDefault(ti => ti.InstanceId == tool.InstanceId);
                if (inst != null)
                    ComboItems.Add(new ImageSourceItem(
                    tool.DisplayText,
                    inst.ViewModel.CurrentResultImage));
            }

            // 3. 默认选中最后一项（即“原图”或最新工具）
            SelectedSource = ComboItems.Last();

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public AlgorithmTopPage()
        {
            InitializeComponent();
        }
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
