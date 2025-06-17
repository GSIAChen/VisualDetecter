using System.Printing;
using System.Windows;

namespace MyWPF1.Controls
{
    public partial class NumericUpDown : System.Windows.Controls.UserControl
    {
        public NumericUpDown()
        {
            InitializeComponent();
        }

        // 绑定的可调数值
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set
            {
                // 限制在最小最大值之间
                double clamped = Math.Max(Minimum, Math.Min(Maximum, value));
                SetValue(ValueProperty, clamped);
            }
        }

        // 每次微调增量
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(
                nameof(Increment),
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(1.0));

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        // 最小值
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(double.MinValue));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        // 最大值
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(double.MaxValue));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            Value = Math.Min(Value + Increment, Maximum);
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            Value = Math.Max(Value - Increment, Minimum);
        }

        private void InputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(InputTextBox.Text, out double inputValue))
            {
                double clamped = Math.Max(Minimum, Math.Min(Maximum, inputValue));
                Value = clamped;
                InputTextBox.Text = clamped.ToString(); // 显示合法值
            }
            else
            {
                // 输入不合法时，恢复显示当前 Value
                InputTextBox.Text = Value.ToString();
            }
        }
    }
}
