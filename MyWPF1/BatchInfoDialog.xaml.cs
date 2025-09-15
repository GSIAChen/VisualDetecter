using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MyWPF1
{
    /// <summary>
    /// Interaction logic for BatchInfoDialog.xaml
    /// </summary>
    public partial class BatchInfoDialog : Window
    {
        // 对话框中填好的属性
        public string MaterialName { get; private set; } = "";
        public string BatchNumber { get; private set; } = "";
        public int BatchQuantity { get; private set; }

        public BatchInfoDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // 验证：物料名称和批次号不能为空，数量必须是整数
            if (string.IsNullOrWhiteSpace(MaterialNameTextBox.Text)
             || string.IsNullOrWhiteSpace(BatchNumberTextBox.Text))
            {
                MessageBox.Show("请填写物料名称和批次号。", "输入错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(BatchQuantityTextBox.Text, out var qty) || qty < 0)
            {
                MessageBox.Show("批次数量请输入非负整数。", "输入错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 赋值给属性
            MaterialName = MaterialNameTextBox.Text.Trim();
            BatchNumber = BatchNumberTextBox.Text.Trim();
            BatchQuantity = qty;

            DialogResult = true;  // 关闭对话框并返回 true
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // 关闭对话框并返回 false
        }
    }
}
