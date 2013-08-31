using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace AmazonScrape
{
    /// <summary>
    /// Interaction logic for ScoreDistribution.xaml
    /// </summary>
    public partial class ScoreDistributionControl : UserControl
    {
        // The control prevents the user from setting the "low" ranges in such
        // a way that the total exceeds 100%. When a user tries to raise a value
        // that woudld cause the total to exceed 100%, it reduces the other control
        // values accordingly. This boolean prevents an event cascade when the other
        // control values are being set.
        bool resolvingPercentageError = false;
        private RangeSlider[] _sliders;

        public ScoreDistribution Distribution
        {
            get
            {
                return new ScoreDistribution(
                    OneStar.GetRange(),
                    TwoStar.GetRange(),
                    ThreeStar.GetRange(),
                    FourStar.GetRange(),
                    FiveStar.GetRange());
            }
        }

        public ScoreDistributionControl()
        {
            InitializeComponent();

            // Makes validating the values of the sliders easier
            _sliders = new RangeSlider[5];
            _sliders[0] = OneStar;
            _sliders[1] = TwoStar;
            _sliders[2] = ThreeStar;
            _sliders[3] = FourStar;
            _sliders[4] = FiveStar;
        }

        /// <summary>
        /// Returns the sum total of "low" range scores, given a RangeSlider
        /// that just had its value modified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newValue"></param>
        /// <returns>Sum of current "low" values</returns>
        double GetNewSumOfLowRange(RangeSlider sender, double newValue)
        {
            // Start with the supplied new value
            double total = newValue;

            for (int i = 0; i < 5; i++)
            {
                if (_sliders[i] != sender)
                {
                    total += _sliders[i].Low;
                }
            }
            return total;
        }

        /// <summary>
        /// Given a RangeSlider, provides a list of the other RangeSliders that have
        /// a positive "low" range value. Used when adjusting the total of the "low"
        /// range sliders to not exceed 100%.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        List<RangeSlider> GetOtherSlidersWithPositiveLowScore(RangeSlider sender)
        {
            List<RangeSlider> results = new List<RangeSlider>();

            // Loop through and get the sliders (besides the sender)
            // that have a positive "low" range value.
            for (int i = 0; i < 5; i++)
            {
                if (_sliders[i] != sender && _sliders[i].Low > 0)
                { results.Add(_sliders[i]); }
            }
            return results;
        }

        /// <summary>
        /// Prevents user from specifying a total minimum number of results that exceeds 100%
        /// by dynamically reducing the values of the other sliders.
        /// </summary>
        /// e.g. the user specifies 40% for the "low" range value of each of the five 
        /// star categories. The sum would be 200% and wouldn't make sense.
        /// <param name="sender"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void slider_LowValueChanged(RangeSlider sender, double oldValue, double newValue)
        {
            // If we're already dealing with with this problem, allow it to resolve.
            // This prevents a cascade of events as every change to a low score causes
            // this method to be called
            if (resolvingPercentageError) { return; }

            // Figure out whether the change puts the "low" sum over 100%
            double sum = GetNewSumOfLowRange(sender, newValue);

            if (sum <= 100) { return; }

            resolvingPercentageError = true;

            // Loop through the sliders, reducing each until
            // the total amount is less than or equal to 100
            while (sum > 100)
            {
                // We can't do anything with sliders with a zero "low" value, so get
                // a list of other sliders that have a non-zero "low" value
                List<RangeSlider> hasLowRangeValue = GetOtherSlidersWithPositiveLowScore(sender);

                foreach (RangeSlider rangeSlider in hasLowRangeValue)
                {
                    // how much more we have to distribute
                    double amountAboveOneHundred = sum - 100;

                    // The amount over 100 divided by the number of sliders that have
                    // a "low" value.
                    double shareOfAmountOver = Math.Ceiling(amountAboveOneHundred / hasLowRangeValue.Count);

                    // Try to subtract this amount from the current slider
                    double remainder = rangeSlider.Low - Math.Ceiling(amountAboveOneHundred / hasLowRangeValue.Count);

                    // Two possibilities:
                    // 1) The slider had enough "low" value to subtract its share of the overage
                    if (remainder > 0)
                    {
                        sum -= shareOfAmountOver;
                        rangeSlider.Low = remainder;
                    }
                    else // 2) We could only subtract a part of this slider's share before it became zero
                    {
                        // We've reduced this slider's "low" value to zero
                        rangeSlider.Low = 0;

                        // The negative remainder represents the amount that we couldn't subtract
                        // from this slider. Adjust sum to reflect the portion that we subtracted
                        sum -= (shareOfAmountOver - Math.Abs(remainder));

                    }

                } // for each rangeslider with a low value

            } // sum > 100

            // The problem has been successfully resolved.
            resolvingPercentageError = false;
        }

        /// <summary>
        /// Returns whether the control is validated
        /// Always returns true, as the control ensures valid values.
        /// </summary>
        /// <returns></returns>
        public Result<T> Validate<T>()
        {
            return new Result<T>();
        }


    }
}
