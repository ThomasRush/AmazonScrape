using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    [DebuggerDisplay("Range = {Low},{High}")]
    [global::System.ComponentModel.TypeConverter(typeof(DoubleRangeConverter))]
    public class DoubleRange: NumericRange<double>
    {
        public double Span { get { return High - Low; } }
        public bool HasLow { get { return _low > double.MinValue; } }
        public bool HasHigh { get { return _high < double.MaxValue; } }

        // Default constructor sets the range to the min/max
        // allowable for a double. 
        public DoubleRange()
        {
            _low = double.MinValue;
            _high = double.MaxValue;
        }

        public DoubleRange(double low, double high) : base(low, high) { }

        // TODO: allow the user to specify one boundary or the other?

        /// <summary>
        /// Returns true if the supplied double is contained within the range (inclusive)
        /// </summary>
        /// <param name="testDouble"></param>
        /// <returns></returns>
        public override bool Contains(double testDouble)
        {
            return (testDouble >= Low && testDouble <= High);
        }

        /// <summary>
        /// Returns true if the supplied NumericRange exists entirely within this range (inclusive)
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool Contains(NumericRange<double> range)
        {
            return (Low <= range.Low && High >= range.High);
        }

        /// <summary>
        /// Returns true if there is any overlap between the supplied NumericRange (inclusive)
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool Overlaps(NumericRange<Double> range)
        {
            if (Contains(range)) return true;

            if ((range.Low <= Low && range.High >= Low) ||
                (range.High >= High && range.Low <= High)) return true;

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

            DoubleRange result = new DoubleRange();
            if (string.IsNullOrEmpty(data)) return result;

            string[] boundaries = data.Split(',');

            if (boundaries.Count() != 2)
            {
                string msg = "Double Range requires values separated by a comma. Note: you can supply one boundary and leave the other blank, e.g. '0,' means a value zero or greater";
                throw new FormatException(msg);
            }

            double low = double.MinValue;
            double high = double.MaxValue;

            if (boundaries[0].Length > 0)
            {
                try
                {
                    low = Convert.ToDouble(boundaries[0]);
                    result.Low = low;
                }
                catch { throw new FormatException("Can't convert the low boundary in the TextBox control range property. ('" + boundaries[0] + "' supplied)"); }
            }

            if (boundaries[1].Length > 0)
            {
                try
                {
                    high = Convert.ToDouble(boundaries[1]);
                    result.High = high;
                }
                catch { throw new FormatException("Can't convert the high boundary in the TextBox control range property. ('" + boundaries[1] + "' supplied)"); }
            }

            return result;
        }

        public override string ToString()
        {
            string result = "";

            // Changed format here to support type converter

            if (HasLow)
            {
                result += Low.ToString();
            }

            result += ",";

            if (HasHigh)
            {
                result += High.ToString();
            }


            /*
            if (!HasLow && !HasHigh)
            {
                return "No range specified";
            }

            if (HasLow && HasHigh)
            {
                return "From " + Low.ToString() + " to " + High.ToString();
            }

            if (HasLow && !HasHigh)
            {
                return " greater than or equal to " + Low.ToString();
            }

            if (HasHigh && !HasLow)
            {
                return " less than or equal to " + High.ToString();
            }
            */

            return result;
        }
     
    } // class

} // namespace
