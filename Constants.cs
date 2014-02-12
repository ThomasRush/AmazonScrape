
namespace AmazonScrape
{
    public static class Constants
    {
        public const bool DEBUG_MODE = true;

        // Default search parameters
        // -------------------------------------------------------------------
        public const string DEFAULT_SEARCH_TEXT = @"";
        public const int DEFAULT_RESULT_COUNT = 20;
        public const int DEFAULT_MIN_REVIEWS = 0;
        public const bool DEFAULT_MATCH_ALL_TERMS = false;
        public const bool USE_STRICT_PRIME_ELIGIBILITY = false;
        // "Fuzzy" Amazon Prime eligibility attempts to infer whether a product
        // is Prime eligible and is not always accurate. Strict Prime
        // eligibility requires another page load per item, which decreases
        // performance, but accurately reports an item's Prime status.

        // Major formatting settings 
        // ( other settings are found in .\Resources\ControlStyles.xaml )
        // -------------------------------------------------------------------
        public const double DEFAULT_LARGE_TEXT_SIZE = 16.0;
        public const double DEFAULT_MEDIUM_TEXT_SIZE = 12.0;
        public const double DEFAULT_SMALL_TEXT_SIZE = 10.0;
        public const double DEFAULT_BUTTON_FONT_SIZE = 12.0;

        // URLS and parameters (used by Scraper/Parser)
        // -------------------------------------------------------------------
        public const string BASE_URL = @"http://www.amazon.com";
        public const string SEARCH_URL = 
            @"http://www.amazon.com/s/field-keywords=";
        public const string SEARCH_URL_PAGE_PARAM = @"&page=";        
        public const string REVIEW_HISTOGRAM_URL = 
            @"http://www.amazon.com/gp/customer-reviews/common/du/displayHistoPopAjax.html?&ASIN=";

    }
}
