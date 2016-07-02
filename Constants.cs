
namespace AmazonScrape
{
    public static class Constants
    {
        // -------------------------------------------------------------------
        // Default search parameters
        // -------------------------------------------------------------------
        public const string DEFAULT_SEARCH_TEXT = @"";
        public const string DEFAULT_RESULT_COUNT = @"20";
        public const string DEFAULT_MIN_REVIEWS = @"0";
        public const bool DEFAULT_MATCH_ALL_TERMS = false;
        public const string DEFAULT_LOW_PRICERANGE = @"";
        public const string DEFAULT_HIGH_PRICERANGE = @"";
        // todo: add review distribution defaults



        // -------------------------------------------------------------------
        // Major formatting settings 
        // ( other settings are found in .\UI\Resources\ControlStyles.xaml )
        // -------------------------------------------------------------------
        public const double DEFAULT_LARGE_TEXT_SIZE = 16.0;
        public const double DEFAULT_MEDIUM_TEXT_SIZE = 12.0;
        public const double DEFAULT_SMALL_TEXT_SIZE = 10.0;
        public const double DEFAULT_BUTTON_FONT_SIZE = 12.0;



        // -------------------------------------------------------------------
        // URLS and parameters (used by Scraper/Parser)
        // -------------------------------------------------------------------
        public const string BASE_URL = @"www.amazon.com";
        public const string SEARCH_URL = 
            @"http://www.amazon.com/s/field-keywords=";
        public const string SEARCH_URL_PAGE_PARAM = @"&page=";        
        public const string REVIEW_HISTOGRAM_URL = 
            @"http://www.amazon.com/gp/customer-reviews/common/du/displayHistoPopAjax.html?&ASIN=";



        // -------------------------------------------------------------------
        // Program operation settings
        // -------------------------------------------------------------------

        // The number of async pageload threads to run (set to 2 or more)
        public const int MAX_THREADS = 3;

        // Ensures prime eligibility accuracy by loading an additional
        // page for each result (slows application execution significantly)
        // Under normal circumstances, this should remain "false".
        public const bool USE_STRICT_PRIME_ELIGIBILITY = false;
    }
}
