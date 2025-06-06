using MyWPF1.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UserControl = System.Windows.Controls.UserControl;

namespace MyWPF1
{
    /// <summary>
    /// AlgorithmTopPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlgorithmTopPage : UserControl
    {
        public ObservableCollection<object> ComboItems { get; }
        public SelectableItem CurrentTool { get; }
        private SelectableItem _selectedItem;
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

        public AlgorithmTopPage(object item, ArrowViewModel parentViewModel)
        {
            InitializeComponent();
            ComboItems = ["原图"];

            // 获取当前工具在父集合中的真实索引
            SelectableItem selectedItem = (SelectableItem)item;
            SelectedItem = selectedItem;
            int currentIndex = parentViewModel.SelectedItems.IndexOf(selectedItem);

            // 筛选前置项并添加序号
            foreach (var tool in parentViewModel.SelectedItems)
            {
                if (tool.Index <= currentIndex) ComboItems.Add(tool);
            }
            // 设置数据上下文
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
}
