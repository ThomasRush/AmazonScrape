using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Contains static methods to process Amazon html and return product information
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Finds and returns a list of signed/unsigned integers/doubles 
        /// parsed from the supplied string. Comma-formatted numbers are
        /// recognized.
        /// </summary>
        /// Only recognizes "correctly formatted" comma pattern:
        /// e.g. 1,234.123 or 12,345,678.123 but not 1,23,4.123
        /// Optional parameter parseCount allows the user to limit the number
        ///  of numbers returned.
        /// Note: limiting the amount of results does NOT improve performance;
        ///  it simply returns the firs N results found.
        /// <param name="text">The string to parse</param>
        /// <param name="parseCount">The number of double values 
        /// it will attempt to parse</param>
        /// <returns>List of Double values</returns>
        public static List<Double> ParseDoubleValues(string text,
            int parseCount = -1)
        {
            // Full pattern:
            // (((-?)(\d{1,3}(,\d{3})+)|(-?)(\d)+)(\.(\d)*)?)|((-)?\.(\d)+)

            List<Double> results = new List<Double>();
            if (text == null) { return results; }

            // Optional negative sign and one or more digits
            // Valid: "1234", "-1234", "0", "-0"
            string signedIntegerNoCommas = @"(-?)(\d)+";

            // Optional negative sign and digits grouped by commas
            // Valid: "1,234", "-1,234", "1,234,567"
            // INVALID: "12,34" <-- does not fit "normal" comma pattern
            string signedIntegerCommas = @"(-?)(\d{1,3}(,\d{3})+)";

            string or = @"|";

            // Optional decimal point and digits            
            // Valid: ".123", ".0", "", ".12345", "."
            string optionalUnsignedDecimalAndTrailingNumbers = @"(\.(\d)*)?";

            // Optional negative sign, decimal point and at least one digit
            // Valid: "-.12", ".123"
            // INVALID: "", ".", "-."
            string requiredSignedDecimalAndTrailingNumbers = @"((-)?\.(\d)+)";

            string pattern = @"";

            // Allow a signed integer with or without commas
            // and an optional decimal portion
            pattern += @"(" + signedIntegerCommas + or + signedIntegerNoCommas 
                + @")" + optionalUnsignedDecimalAndTrailingNumbers;

            // OR allow just a decimal portion (with or without sign)
            pattern = @"(" + pattern + @")" + or 
                + requiredSignedDecimalAndTrailingNumbers;

            List<string> matches = GetMultipleRegExMatches(text, pattern);

            int matchIndex = 0;
            foreach (string match in matches)
            {
                // If the user supplied a max number of
                // doubles to parse, check to make sure we don't exceed it
                if (parseCount > 0)
                {
                    if (matchIndex + 1 > parseCount) break;
                }

                try
                {
                    // Get rid of any commas before converting
                    results.Add(Convert.ToDouble(match.Replace(",", "")));
                }
                catch
                {
                    string msg = "Unable to convert {0} to a double";
                    Debug.WriteLine(string.Format(msg, match));
                }
                matchIndex += 1;
            }

            return results;
        }

        /// <summary>
        /// Returns the number of reviews for the product, given the
        /// review histogram html (not the product html)
        /// </summary>
        /// <param name="reviewHistogramHtml">html for the review histogram</param>
        /// <returns>integer number of product reviews</returns>
        public static int GetReviewCount(string reviewHistogramHtml)
        {
            string reviewCountPattern = @"(?<=See all ).*?(?= reviews)";

            string reviewCountMatch = GetSingleRegExMatch(reviewHistogramHtml,
                reviewCountPattern);

            if (reviewCountPattern.Length == 0) return 0;
            reviewCountMatch = reviewCountMatch.Replace(",", "");
            int reviewCount = 0;
            
            try
            {
                reviewCount = Convert.ToInt32(reviewCountMatch);
            }
            catch
            {
                string msg = "Unable to cast review count to an integer ";
                msg += "(probably because there were no reviews)";
                Debug.WriteLine(msg);                
            }
            
            return reviewCount;
        }

        /// <summary>
        /// Given the html of an Amazon search page result, returns
        /// the number of product results.
        /// </summary>
        /// <param name="pageHtml">html of entire search page</param>
        public static int GetPageResultCount(string pageHtml)
        {
            
            // Three possible formats for figuring out the 
            // number of results on the page:
            // -------------------------------------------------
            // Case 1: "Showing X Results" (one page)
            // Case 2: "Showing X - Y of Z Results" ( >1 page)
            // Case 3: "Your search "<search term here>" did 
            //          not match any products."
            
            // Grab the section after the resultCount id attribute
            // until the next id attribute
            string resultCountPattern = @"(?<=id=""resultCount"").*?(?= id=)";
            string match = GetSingleRegExMatch(pageHtml, resultCountPattern);

            int resultTotal = 0;

            if (match.Length == 0) return resultTotal;
            
            // Parse out the numeric values, 
            // limiting to two maximum (as in Case 2 above)
            List<Double> resultRange = ParseDoubleValues(match, 2);

            switch (resultRange.Count)
            {
                case 1:
                    try
                    { resultTotal = Convert.ToInt32(resultRange[0]); }
                    catch { }
                    break;
                case 2:
                    try
                    {
                        // ParseDoubleValues thinks the hyphen in the results
                        // denotes a negative number.
                        // e.g. "17-32 of 65,130" will return 17, -32
                        // Get the absolute values before subtracting.
                        resultTotal = Convert.ToInt32(
                            Math.Abs(resultRange[1]) - 
                            (Math.Abs(resultRange[0]) - 1));
                    }
                    catch { }
                    break;
            }
            
            // (Case 3 doesn't need to be handled, since resultTotal 
            //  will fall through and correctly remain 0)

            return resultTotal;
        }

        /// <summary>
        /// Returns a list of individual html product results from an html page
        /// </summary>
        /// <param name="pageHtml">The string containing a single page of Amazon search results</param>
        /// <param name="_resultCount">Specity the number of results the method will attempt to parse</param>
        /// <returns>A list of strings representing individual html item results</returns>
        public static List<string> GetPageResultItemHtml(string pageHtml, int resultCount)
        {
            // NOTE:
            // Amazon injects additional search results (commented out in 
            // javascript) at the bottom of each search page to cache results.
            // The parameter _resultCount is obtained from the top of the page 
            // so that only the results that are visible to the user are 
            // returned. This was mainly done to fix a bug where duplicate
            // records were being returned, but it's probably good practice
            // to only consider "visible" results in case Amazon changes its
            // caching strategy.

            List<string> results = new List<string>();
            TimeSpan timeOut = new TimeSpan(0, 0, 10);

            // Grab the text between each of the results
            string resultPattern = @"(?<=result_[0-9]?[0-9]).*?(?=result_[0-9]?[0-9])";
            List<string> matches = GetMultipleRegExMatches(pageHtml, resultPattern);

            if (matches.Count < resultCount) { return results; }

            for (int i = 0; i < resultCount ; i++)
            {
                results.Add(matches[i]);
            }
            
            return results;
        }

        /// <summary>
        /// Extracts the product's name from a single product's html
        /// </summary>
        /// <param name="itemHtml">Single product result html</param>
        /// <returns>Name of product</returns>
        public static string GetProductName(string itemHtml)
        {
            string productNamePattern = @"(?<=access-title.*?>).*?(?=</)";
            string match = GetSingleRegExMatch(itemHtml, productNamePattern);

            if (match.Length == 0)
            { return null; }

            string productName = Scraper.DecodeHTML(match);
            
            return productName;
        }

        /// <summary>
        /// Parses a DoubleRange object representing the "high" and "low"
        /// prices from the item's html.
        /// </summary>
        /// If there is only one price supplied, set the range to that single value.
        /// <param name="html">Single product result html</param>
        /// <returns>DoubleRange representing the product's pricerange</returns>
        public static DoubleRange GetPriceRange(string itemHtml)
        {
            // Dollarsign and Digits grouped by commas plus decimal
            // and change (change is required)
            string dollarCurrencyFormat = @"\$(\d{1,3}(,\d{3})*).(\d{2})";

            // Optional spaces and hyphen
            string spacesAndHyphen = @"\s+-\s+";

            // Grab the end of the preceeding tag, the dollar amount, and
            // optionally a hyphen and a high range amount before the
            // beginning bracket of the next tag
            string pricePattern = ">" + dollarCurrencyFormat + "(" + spacesAndHyphen + dollarCurrencyFormat + ")?" + "<";

            string match = GetSingleRegExMatch(itemHtml, pricePattern);

            // Need to remove the tag beginning and end:
            match = match.Trim(new char[] { '<', '>' });

            if (match.Length == 0)
            { return new DoubleRange(); }

            List<Double> prices = ParseDoubleValues(match, 2);
            DoubleRange priceRange = new DoubleRange();
            if (prices.Count > 0)
            {
                priceRange.Low = prices[0];
            }

            if (prices.Count > 1)
            {
                priceRange.High = prices[1];
            }

            if (!priceRange.HasHigh)
            {
                priceRange.High = priceRange.Low;
            }

            return priceRange;
        }

        /// <summary>
        /// Given a specific product result html, provides the review histogram
        /// html. Used for obtaining review count and review distribution.
        /// </summary>
        /// <param name="itemHtml"></param>
        /// <returns>string html of review histogram</returns>
        public static string GetReviewHistogramHtml(string itemHtml)
        {
            // To get the review information without loading an entire new page,
            // we will call the review histogram popup URL instead of the main URL
            // We need the ASIN of the product to make the call, which is in the same
            // DIV tag as the product result #:
            string productASINPattern = @"(?<=name="").*?(?="">)";

            string match = GetSingleRegExMatch(itemHtml, productASINPattern);
            
            // Occassionally Amazon adds attributes to the end of the tag, so
            // find the end of attribute containing the ASIN (should be the first
            // double quote we encounter).
            int endAttributeIndex = match.IndexOf('"');
            if (endAttributeIndex > 0)
            {
                // Truncate anything after
                match = match.Substring(0, endAttributeIndex);
            }
            
            if (match.Length == 0)
            { return null; }

            // With the product ASIN, make the httprequest to get the review popup data            
            return Scraper.LoadReviewHistogram(match);
        }

        /// <summary>
        /// Returns a product's review distribution (percentage of reviews in each category)
        /// </summary>
        /// Avoids a full page load by extrating the data from an Ajax popup.
        /// <param name="reviewHistogramHtml">Review histogram html</param>
        /// <returns>ScoreDistribution of reviews</returns>
        public static ScoreDistribution 
            GetScoreDistribution(string reviewHistogramHtml)
        {
            // Find each instance of review percentage. This regex includes more than we need, but we
            // wind up only grabbing the first five results, which are the ones we care about.
            string reviewDistributionPatterh = @"(?<=title="").*?(?=%)";

            List<string> matches = GetMultipleRegExMatches(reviewHistogramHtml,
                reviewDistributionPatterh);

            //MatchCollection reviewScoreMatches = Regex.Matches(reviewHistogramHtml,
            //    reviewDistributionPatterh, RegexOptions.Singleline);

            // If we can't find any more results, exit
            if (matches.Count == 0)
            { return null; }

            double[] reviews = new double[5];

            // Feed them into the array backwards so that
            // one star reviews are in the zero index
            for (int i = 0; i <5; i++)
            {
                // Reverse the order of the reviews so that index 0 is 1-star,
                // index 1 is 2-star, etc.
                try
                {
                    // The percentage is at the very end of each string
                    // Work backwards to build the value
                    var stack = new Stack<char>();

                    for (var strIndex = matches[i].Length - 1; strIndex >= 0; strIndex--)
                    {
                        if (!char.IsNumber(matches[i][strIndex]))
                        {
                            break;
                        }
                        stack.Push(matches[i][strIndex]);
                    }

                    matches[i] = new string(stack.ToArray());
                    
                    reviews[4 - i] = Convert.ToDouble(matches[i]);
                }
                catch (InvalidCastException)
                {
                    string msg = "Unable to cast review score match {0}";
                    Debug.WriteLine(string.Format(msg,i));
                    reviews[i] = -1;
                }
            }

            return new ScoreDistribution(reviews);
        }
        
        /// <summary>
        /// Attempts to match the supplied pattern to the input
        /// string. Obtains multiple matches and returns a
        /// list of string matches if successful and an empty
        /// list of strings if no matches found.
        /// </summary>
        /// <param name="inputString">String to search</param>
        /// <param name="regExPattern">RegEx pattern to search for</param>
        /// <returns>List of matches or empty list if no matches</returns>
        private static List<string> GetMultipleRegExMatches(
            string inputString,
            string regExPattern)
        {
            string msg;
            List<string> results = new List<string>();
            try
            {
                MatchCollection matches = Regex.Matches(inputString,
                    regExPattern,
                    RegexOptions.Singleline);
                if (matches.Count == 0) return results;
                
                IEnumerator e = matches.GetEnumerator();
                while (e.MoveNext())
                {
                    results.Add(((Match)e.Current).Value);
                }
            }
            catch (ArgumentException ex)
            {
                msg = regExPattern;
                Debug.WriteLine(ex.InnerException + 
                    " argument exception for pattern " + msg);
            }
            catch (RegexMatchTimeoutException ex)
            {
                msg = regExPattern;
                Debug.WriteLine(ex.InnerException + 
                    " timeout exception for pattern " + msg);
            }
            return results;
            
        }

        /// <summary>
        /// Attempts to match the supplied pattern to the input
        /// string. Only obtains a single match and returns the
        /// matching string if successful and an empty string if not.
        /// </summary>
        /// <param name="inputString">String to be searched</param>
        /// <param name="regExPattern">Pattern to be matched</param>
        /// <returns>String match or empty string if match not found</returns>
        private static string GetSingleRegExMatch(string inputString,
            string regExPattern)
        {
            string msg;
            string result = "";
            try
            {
                Match match = Regex.Match(inputString,
                    regExPattern,
                    RegexOptions.Singleline);
                if (match.Success)
                {
                    result = match.Value;
                }
            }
            catch (ArgumentException ex)
            {
                msg = regExPattern;
                Debug.WriteLine(ex.InnerException + 
                    " argument exception for pattern " + msg);
            }
            catch (RegexMatchTimeoutException ex)
            {
                msg = regExPattern;
                Debug.WriteLine(ex.InnerException + 
                    " timeout exception for pattern " + msg);
            }
            return result;
        }
        
        /// <summary>
        /// Returns a product's average review rating (double)
        /// </summary>
        /// <param name="reviewHistogramHtml">html of the review histogram</param>
        /// <returns>-1 if no rating, otherwise double rating value</returns>
        public static double GetRating(string reviewHistogramHtml)
        {
            string ratingPattern = @"[0-5].?[0-9]? out of 5 stars";

            string rating = GetSingleRegExMatch(reviewHistogramHtml,
                ratingPattern);

            double result = -1;

            if (rating.Length == 0) return result;
            
            try
            {
                // Two possible formats:
                // 1) Decimal value included, e.g. 4.5
                if (rating.Contains("."))
                {
                    
                    result = Convert.ToDouble(rating.Substring(0, 3));
                }
                else // 2) No decimal, e.g. 4
                {
                    result = Convert.ToDouble(rating.Substring(0, 1));
                }
                
            }
            catch (InvalidCastException)
            {
                Debug.WriteLine("Unable to cast product rating to a double.");
            }

            return result;
        }

        /// <summary>
        /// Using an item's html, determines Prime eligibility with passable accuracy.
        /// </summary>
        /// Bases Prime eligibility on "Super Saver Shipping", which is accurate most of the time.
        /// Done this way to avoid an extra page load. Notable speed increase.
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static bool GetFuzzyPrimeEligibility(string itemHtml)
        {
            string fuzzyPrimeEligibilityPattern = @".*?FREE.*?Shipping";

            string match = GetSingleRegExMatch(itemHtml, fuzzyPrimeEligibilityPattern);

            return (match.Length > 0);
        }

        /// <summary>
        /// Uses an additional page load to determine Prime eligibility
        /// with accuracy
        /// </summary>
        /// <param name="productURL"></param>
        /// <returns></returns>
        public static bool GetStrictPrimeEligibility(Uri productURL)
        {
            string html = Scraper.CreateHttpRequest(productURL);

            // Non-prime eligible results call this function with a "0" first
            // parameter; here we look specifically for "1", which 
            // denotes prime eligibility
            string primeEligiblePattern = @"bbopJS.initialize\(1,";

            string match = GetSingleRegExMatch(html, primeEligiblePattern);

            return (match.Length > 0);
        }

        /// <summary>
        /// Extracts a product's Amazon URL.
        /// </summary>
        /// Used when user clicks to access the product's Amazon listing.
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static Uri GetURL(string itemHtml)
        {
            bool usingSSL = false;
            string productURLPattern = @"(?<=http:).*?/dp/.*?(?=/)";
            string match = GetSingleRegExMatch(itemHtml, productURLPattern);

            // Check for SSL before calling this an error
            if (match.Length == 0)
            {
                usingSSL = true;
                productURLPattern = @"(?<=https:).*?/dp/.*?(?=/)";
                match = GetSingleRegExMatch(itemHtml, productURLPattern);
                if (match.Length == 0)
                {
                    return null;
                }
            }

            // NOTE: Amazon SOMETIMES supplies a relative URL
            // (I don't know what determines this)
            // If the base URL is not present, prepend it.
            if (!match.Contains(Constants.BASE_URL))
            {
                match = Constants.BASE_URL + match;
            }

            if (usingSSL) match = "https:" + match;
            else match = "http:" + match;

            if (Uri.IsWellFormedUriString(match, UriKind.Absolute))
            {
                return new Uri(match);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Parses out the URL to the product's image thumbnail (if one exists)
        // and then calls DownloadWebImage to return a BitmapImage
        /// </summary>
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static BitmapImage GetImageThumbnail(string itemHtml)
        {
            // TODO: does Amazon use a standardized image format?
            //   For now, allowing for multiple possible formats.
            string imageURLPattern = @"(http(s?):/)(/[^/]+)+\.(?:jpg|gif|png)";

            string match = GetSingleRegExMatch(itemHtml, imageURLPattern);

            if (match.Length == 0)
            { return null; }

            if (Uri.IsWellFormedUriString(match, UriKind.Absolute))
            {
                Uri imageURL = new Uri(match);
                return Scraper.DownloadWebImage(imageURL);
            }
            else
            {
                return null;
            }
        }


    }
}
