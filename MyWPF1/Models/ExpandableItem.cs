using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MyWPF1.Models  // 注意命名空间要与项目实际命名空间一致
{
    public class ExpandableItem : INotifyPropertyChanged
    {
        private bool _isExpanded;
        public required string Title { get; set; }
        public required object Content { get; set; }
        public ICommand ExpandAllCommand => new RelayCommand(() => IsExpanded = true);
        public ICommand CollapseAllCommand => new RelayCommand(() => IsExpanded = false);

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}