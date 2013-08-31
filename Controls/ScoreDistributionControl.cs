using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AmazonScrape
{
    class ScoreDistributionControl : GridPlus, IValidatable
    {
        /// <summary>
        /// Provides "low range" and "high range" sliders for each Amazon review
        /// category. Allows the user to supply the desired minimum and maximum 
        /// percentage values for the product results.
        /// 
        /// Examples: 
        ///  
        ///   If the user specifies a 5% high range value for one-star reviews,
        ///  no more than 5% of all reviews will be one-star reviews.
        /// 
        ///  If the user specifies a 50% low range value for five-star reviews,
        ///  no fewer than 50% of all reviews will be five-star reviews.
        /// </summary>
        public DoubleRange OneStarRange { get { return _sliders[0].GetRange(); } }
        public DoubleRange TwoStarRange { get { return _sliders[1].GetRange(); } }
        public DoubleRange ThreeStarRange { get { return _sliders[2].GetRange(); } }
        public DoubleRange FourStarRange { get { return _sliders[3].GetRange(); } }
        public DoubleRange FiveStarRange { get { return _sliders[4].GetRange(); } }
        public ScoreDistribution Distribution 
        { 
            get 
            {
                return new ScoreDistribution(OneStarRange,
                    TwoStarRange,
                    ThreeStarRange,
                    FourStarRange,
                    FiveStarRange);
            }
        }

        // One RangeSlider for each star-rating category
        private RangeSliderX[] _sliders = new RangeSliderX[5];

        // The control prevents the user from setting the "low" ranges in such
        // a way that the total exceeds 100%. When a user tries to raise a value
        // that woudld cause the total to exceed 100%, it reduces the other control
        // values accordingly. This boolean prevents an event cascade when the other
        // control values are being set.
        bool resolvingPercentageError = false;

        public ScoreDistributionControl()
        {
            // Five columns, one for each star category RangeSlider
            //ColumnDefinition col;
            for (int i = 0; i < 5; i++)
            {
                AddColumn(20, GridUnitType.Star);
            }

            // Two rows, one for the header and one for the controls
            AddRow(10, GridUnitType.Star);
            AddRow(90, GridUnitType.Star);

            // Control header
            TextBlock text = new TextBlock();
            text.Background = new SolidColorBrush(Colors.LightGray);
            ToolTip tip = new ToolTip();
            tip.Content = "Specify how the results should be distributed." + Environment.NewLine;
            tip.Content += "For instance, if you set the one-star 'high' slider to 5%, it means that" + Environment.NewLine;
            tip.Content += "the returned items will have no more than 5% one-star reviews.";
            text.ToolTip = tip;
            text.Foreground = new SolidColorBrush(Colors.Blue);            
            text.Text = "Result percentage ranges per star category";
            text.TextAlignment = TextAlignment.Center;

            AddContent(text, 0, 0, 5);

            // Five range sliders; one for each star category
            for (int i = 0; i < 5; i++)
            {
                RangeSliderX slider = new RangeSliderX();
                slider.HeaderText = (i+1).ToString() + " - Star";
                slider.High = 100;
                slider.Low = 0;
                slider.VerticalAlignment = VerticalAlignment.Stretch;
                slider.HorizontalAlignment = HorizontalAlignment.Stretch;
                _sliders[i] = slider;
                //_sliders[i] = new RangeSliderX((i+1).ToString() + " - Star", new DoubleRange(0, 100))
                //{
                //    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                //    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                //};
                _sliders[i].LowValueChanged += slider_LowValueChanged;

                AddContent(_sliders[i], 1, i, 1);
            }
        }

        /// <summary>
        /// Returns the sum total of "low" range scores, given a RangeSlider
        /// that just had its value modified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newValue"></param>
        /// <returns>Sum of current "low" values</returns>
        double GetNewSumOfLowRange(RangeSliderX sender,double newValue)
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
        List<RangeSliderX> GetOtherSlidersWithPositiveLowScore(RangeSliderX sender)
        {
            List<RangeSliderX> results = new List<RangeSliderX>();

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
        void slider_LowValueChanged(RangeSliderX sender, double oldValue, double newValue)
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
                List<RangeSliderX> hasLowRangeValue = GetOtherSlidersWithPositiveLowScore(sender);

                foreach (RangeSliderX rangeSlider in hasLowRangeValue)
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
        public ValidationResult Validate()
        {
            return new ValidationResult(ValidationResult.Status.Valid);
        }
    }
}
