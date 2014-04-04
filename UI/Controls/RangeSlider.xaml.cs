using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace AmazonScrape
{
    /// <summary>
    /// Interaction logic for RangeSlider.xaml
    /// </summary>
    public partial class RangeSlider : UserControl
    {
        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register
        (
            "HeaderText",
            typeof(string),
            typeof(RangeSlider),
            new PropertyMetadata(string.Empty)
        );

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public RangeSlider()
        {
            InitializeComponent();
            
            LowText.textBox.Text = "0";
            HighText.textBox.Text = "100";
            
            // Set bindings for the sliders to change the values of the
            // text boxes
            Binding b = new Binding();
            b.Source = LowSlider;
            b.Path = new PropertyPath("Value", LowSlider.Value);
            b.Mode = BindingMode.TwoWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            LowText.textBox.SetBinding(TextBox.TextProperty, b);

            b = new Binding();
            b.Source = HighSlider;
            b.Path = new PropertyPath("Value", HighSlider.Value);
            b.Mode = BindingMode.TwoWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            HighText.textBox.SetBinding(TextBox.TextProperty, b);

            // Check to ensure that the low slider doesn't exceed the high slider
            LowSlider.ValueChanged += LowSlider_ValueChanged;
            HighSlider.ValueChanged += HighSlider_ValueChanged;
        }

        public double Low { get { return LowSlider.Value; } set { LowSlider.Value = value; } }
        public double High { get { return HighSlider.Value; } set { HighSlider.Value = value; } }

        // The range of possible values for the sliders
        protected DoubleRange range;

        public delegate void LowValueChangedEventHandler(RangeSlider slider, double oldValue, double newValue);
        public event LowValueChangedEventHandler LowValueChanged;

        void HighSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue < LowSlider.Value)
            {
                LowSlider.Value = e.NewValue;
            }
        }

        void LowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue > HighSlider.Value)
            {
                HighSlider.Value = e.NewValue;
            }

            if (this.LowValueChanged != null)
            {
                LowValueChanged(this, e.OldValue, e.NewValue);
            }
        }

        public DoubleRange GetRange()
        {
            return new DoubleRange(LowSlider.Value, HighSlider.Value);
        }

    } // class
} // namespace
