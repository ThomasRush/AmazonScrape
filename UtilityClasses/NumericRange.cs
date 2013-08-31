using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace AmazonScrape
{
    // Represents a numeric range of values.
    // Require that the extending classes implement IComparable (numeric types, String, Char, Datetime)
    // This gets us some compile-time type-checking (DateTime, String & Char throw exceptions when object is instantiated)
    public abstract class NumericRange<T>  where T : IComparable<T>
    {
        // Allow user to specify range after construction, but ensure that the 
        // "low" value is less than the "high" value 
        // (otherwise throws ArgumentOutOfRangeException)
        public T Low
        {
            get
            { return _low;}
            set
            { CheckRange(value, _high); _low = value; HasRangeSpecified = true; }
        }

        public T High
        {
            get
            { return _high; }
            set
            { CheckRange(_low, value); _high = value; HasRangeSpecified = true; }
        }

        protected T _low;
        protected T _high;

        /// <summary>
        /// True if the user specifies a range of values; otherwise the range is set to the
        /// minimum/maximum of the inheriting numeric data type
        /// </summary>
        public bool HasRangeSpecified { get; protected set; }
        

        /// <summary>
        /// Default constructor
        /// </summary>
        public NumericRange()
        {
            CheckType();
            HasRangeSpecified = false;
        }

        /// <summary>
        /// Constructor allows user to specify the low and high bounds
        /// for the numeric range.
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public NumericRange(T low, T high)
        {
            CheckType();

            CheckRange(low, high);
            _low = low;
            _high = high;
            HasRangeSpecified = true;
        }

        /// <summary>
        /// Ensure that the 'low' value does not exceed the 'high' value
        /// </summary>
        /// Note: it is allowable that they are the same value.
        /// <param name="low"></param>
        /// <param name="high"></param>
        private void CheckRange(T low, T high)
        {
            // Low value can't be greater than high value.
            if (low.CompareTo(high) > 0)
            {
                string msg = "The 'low' value for the range cannot be greater than the 'high' value.";
                throw new ArgumentOutOfRangeException(msg);
            }
        }

        /// <summary>
        /// Ensure that the implementing class uses a numeric type
        /// ( implementing IComparable addresses other types at compile-time )
        /// </summary>
        private void CheckType()
        {
            Type t = typeof(T);

            // Restrict to numeric data only
            // According to MSDN,
            // "All numeric types (such as Int32 and Double) implement IComparable,
            /// as do String, Char, and DateTime."
            // Explicitly check for String, Char, and DateTime :
            if (t == typeof(string) ||
                t == typeof(DateTime) ||
                t == typeof(char))
            { throw new NotSupportedException("Only numeric values are supported."); }
        }

        public abstract bool Contains(T value); // Generic parameter is contained within the range (inclusive).
        public abstract bool Contains(NumericRange<T> testRange); // Range parameter is contained (inclusive)
        public abstract bool Overlaps(NumericRange<T> testRange); // Range parameter partially overlaps this range (inclusive)

    }
}
