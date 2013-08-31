using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    class IntRange : NumericRange<int>
    {
        public IntRange(int low, int high) : base(low, high) {}

        // The difference between the high and low values
        public int Span { get { return _high - _low; } }

        // Default constructor sets the range to the min/max
        // allowable for an integer.
        public IntRange()
        {
            this.Low = int.MinValue;
            this.High = int.MaxValue;
        }
        
        /// <summary>
        /// Returns true if the supplied integer falls within this range (inclusive).
        /// </summary>
        /// <param name="testInt"></param>
        /// <returns></returns>
        public override bool Contains(int testInt)
        {
            return (testInt >= Low && testInt <= High);
        }

        public override bool Contains(NumericRange<int> range)
        {
            return (Low <= range.Low && High >= range.High);
        }

        /// <summary>
        /// Returns true if this range value overlaps at any point with the supplied range.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool Overlaps(NumericRange<int> range)
        {
            return (Low <= range.High || High >= range.Low);
        }

    }
}
