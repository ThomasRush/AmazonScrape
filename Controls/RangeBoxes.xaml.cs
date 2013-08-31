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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [global::System.ComponentModel.TypeConverter(typeof(DoubleRangeConverter))]
    public partial class RangeBoxes : UserControl, IValidatable
    {
        protected string _name;
        //protected TextBoxPlus _lowText;
        //protected TextBoxPlus _highText;

        protected DoubleRange _range = new DoubleRange(); // The allowable range for this control

        public RangeBoxes()
        {
            InitializeComponent();
        }

        
        public DoubleRange GetValues()
        {
            DoubleRange values = new DoubleRange();

            if (TextLow.ContainsDoubleValue)
            { values.Low = Convert.ToDouble(TextLow.Text); }

            if (TextHigh.ContainsDoubleValue)
            { values.High = Convert.ToDouble(TextHigh.Text); }
            
            return values;
        }

        public Result<T> Validate<T>()
        {
            double low = _range.Low;
            double high = _range.High;

            Result<T> result = new Result<T>();

            // Try to cast the contents of the low value to double
            if (TextLow.Text.Length > 0)
            {
                try
                {
                    low = Convert.ToDouble(TextLow.Text);
                }
                catch (Exception)
                {
                    string message = _name + " low value must be numeric.";
                    result.ErrorMessage = message;
                    return result;
                    //return new ValidationResult(ValidationResult.Status.Error, message);
                }
            }

            // Try to cast the contents of the high value to double
            if (TextHigh.Text.Length > 0)
            {
                try
                { high = Convert.ToDouble(TextHigh.Text); }
                catch (Exception)
                {
                    string message = _name + " high value must be numeric.";
                    result.ErrorMessage = message;
                    return result;
                    //return new ValidationResult(ValidationResult.Status.Error, message);
                }
            }


            if (!_range.Contains(low) || !_range.Contains(high))
            {
                string message = "Specified " + _name + " values are out of allowable range.";
                result.ErrorMessage = message;
                return result;
                //return new ValidationResult(ValidationResult.Status.Error, message);
            }

            if (low > high)
            {
                string message = _name + " low value is greater than high value.";
                result.ErrorMessage = message;
                return result;
                //return new ValidationResult(ValidationResult.Status.Error, message);
            }

            return result;
            //return new ValidationResult(ValidationResult.Status.Valid, "");

        }
    }
}
