using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AmazonScrape
{
    /// <summary>
    /// Interaction logic for TextBoxPlus.xaml
    /// </summary>
    [global::System.ComponentModel.TypeConverter(typeof(DoubleRangeConverter))]
    public partial class TextBoxPlus : UserControl, IValidatable
    {
        public TextBoxPlus()
        {
            Focusable = true;
            InitializeComponent();

            LostFocus += TextBoxPlus_LostFocus;
            PreviewGotKeyboardFocus += TextBoxPlusX_PreviewGotKeyboardFocus;
        }

        void TextBoxPlusX_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }

        public string Title { get { return _title; } set { _title = value; } }
        public bool ContainsDoubleValue { get { return HasDoubleValue(); } }
        private string _title = ""; // What to call the control if validation fails.

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register
        (
            "Text",
            typeof(string),
            typeof(TextBoxPlus),
            new PropertyMetadata(string.Empty)
        );

        public string Text
        {
            get
            {
                // Force the textbox to grab the current value
                // This prevents stale data from being returned
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
                be.UpdateSource();
                return (string)GetValue(TextProperty);
            }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty RequiresValueProperty = DependencyProperty.Register
        (
            "RequiresValue",
            typeof(bool),
            typeof(TextBoxPlus),
            new PropertyMetadata(false)
        );

        public bool RequiresValue
        {
            get { return (bool)GetValue(RequiresValueProperty); }
            set { SetValue(RequiresValueProperty, value); }
        }

        public static readonly DependencyProperty IsNumericOnlyProperty = DependencyProperty.Register
        (
            "NumericOnly",
            typeof(bool),
            typeof(TextBoxPlus),
            new PropertyMetadata(false)
        );

        public bool IsNumericOnly
        {
            get { return (bool)GetValue(IsNumericOnlyProperty); }
            set { SetValue(IsNumericOnlyProperty, value); }
        }

        public static readonly DependencyProperty NumericRangeProperty = DependencyProperty.Register
        (
            "NumericRange",
            typeof(DoubleRange),
            typeof(TextBoxPlus),
            new PropertyMetadata(null)
        );

        public DoubleRange NumericRange
        {
            get { return (DoubleRange)GetValue(NumericRangeProperty); }
            set { IsNumericOnly = true; SetValue(NumericRangeProperty, value); }
        }

        // Specify a numeric value range that the text must fall within
        // Otherwise, cause a validation error in Validate()
        //private DoubleRange _numericRange;
        //private string _name;
        protected bool HasDoubleValue()
        {
            double result;
            if (Double.TryParse(textBox.Text, out result))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Type converter for range values.
        /// Range should be in the form of:  "X,Y" where X is the numeric low value and Y is the numeric high value
        /// If one of the range values is blank, it means that there is no boundary on that end of the range.
        /// For example, "X," would mean X is the low value, and the top end of the range is the max value for a double.
        /// Note: A comma is required even if no values are being supplied.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DoubleRange Parse(string data)
        {
            return DoubleRange.Parse(data);
        }

        void TextBoxPlusX_OnGotFocus(object sender, EventArgs e)
        {
            textBox.SelectAll();
        }

        void TextBoxPlus_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        { this.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)); }

        public Result<T> Validate<T>()
        {
            Result<T> result = new Result<T>();

            // Error if blank and a value is required
            if (RequiresValue && textBox.Text.Length == 0)
            {
                result.ErrorMessage = _title + " cannot be blank.";
                return result;
            }

            if (IsNumericOnly)
            {
                double value;
                try
                {
                    value = Convert.ToDouble(textBox.Text);
                }
                catch
                {
                    result.ErrorMessage = _title + " is not numeric or exceeds the allowable numeric range.";
                    return result;
                }

                DoubleRange range = NumericRange;
                if (range != null)
                {
                    if (!range.Contains(value))
                    {
                        result.ErrorMessage = _title + " must be " + range.ToString();
                    }
                }
            }
            return result;
        } // validate

    }
}
