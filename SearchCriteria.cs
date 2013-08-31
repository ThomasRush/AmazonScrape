using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    /// <summary>
    /// Used to pass around the user's search criteria (convenience class)
    /// </summary>
    public class SearchCriteria
    {        
        public string SearchText { get { return _searchText; } }
        public double NumberOfResults { get { return _numberOfResults; } }
        public double MinNumberReviews { get { return _minNumberReviews; } }
        public bool StrictPrimeEligibility { get { return _strictPrimeEligibility; } }
        public bool MatchAllSearchTerms { get { return _matchAllSearchTerms; } }
        public DoubleRange PriceRange { get { return _priceRange; } }
        public ScoreDistribution ScoreDistribution { get { return _distribution; } }

        private string _searchText;
        private double _numberOfResults;
        private double _minNumberReviews;
        private bool _matchAllSearchTerms;
        private ScoreDistribution _distribution;
        DoubleRange _priceRange;

        // Amazon items that "qualify for super-saver 2 day shipping" are
        // almost always Prime Eligible as well. If strictPrimeEligibility is set to false,
        // we use this criteria for determining prime eligibility.
        // Otherwise, the code goes to each product page and checks for Prime Eligibility.
        // While the second method is guaranteed to work, it results in an entire pageload
        // for each record, which doubles load times.
        private bool _strictPrimeEligibility;

        public SearchCriteria(string searchText,
            double numberOfResults,
            DoubleRange priceRange,
            ScoreDistribution distribution,
            double minNumberReviews = 0,
            bool matchAllSearchTerms = false,
            bool strictPrimeEligibility = false
            )
        {
            _searchText = searchText;
            _numberOfResults = numberOfResults;
            _matchAllSearchTerms = matchAllSearchTerms;
            _minNumberReviews = minNumberReviews;
            _strictPrimeEligibility = strictPrimeEligibility;
            _distribution = distribution;
            _priceRange = priceRange;
        }
    }
}
