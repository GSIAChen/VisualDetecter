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
            set => SetValue(ValueProperty, value);
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

        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            Value += Increment;
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            Value -= Increment;
        }
    }
}
