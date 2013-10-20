using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Contains static methods to process Amazon html and return product information
    /// </summary>
    public static class Parser
    {

        /// <summary>
        /// Finds and returns a list of signed/unsigned integers/doubles parsed from the supplied string.
        /// </summary>
        /// It will accept numbers with and without the typical comma pattern :
        /// e.g. 12345678.123 or 12,345,678.123
        /// Optional parameter parseCount allows the user to limit the number of numbers returned.
        /// Note: limiting the amount of results does not improve performance; it simply returns
        ///      the firs N results found.
        /// <param name="text">The string to parse</param>
        /// <param name="parseCount">The number of double values it will attempt to parse</param>
        /// <returns>List of Double values</returns>
        public static List<Double> ParseDoubleValues(string text, int parseCount = -1)
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

            // Allow a signed integer with or without commas and an optional decimal portion
            pattern += @"(" + signedIntegerCommas + or + signedIntegerNoCommas + @")" + optionalUnsignedDecimalAndTrailingNumbers;

            // OR allow just a decimal portion (with or without sign)
            pattern = @"(" + pattern + @")" + or + requiredSignedDecimalAndTrailingNumbers;

            MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.Singleline);

            int matchIndex = 0;
            foreach (Match match in matches)
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
                    results.Add(Convert.ToDouble(match.Value.Replace(",", "")));
                }
                catch
                {
                    // TOOD: log errors here 
                }
                matchIndex += 1;
            } // for

            return results;
        }

        public static int GetReviewCount(string itemHtml)
        {

            // look for span tag with class="rvwCnt". There's an <a href.. where the text inside is the review count.
            string reviewCountPattern = @"(?<=class=""rvwCnt"">\(<a href=.*?"">).*?(?=</a>\)</span>)";

            Match reviewCountMatch = Regex.Match(itemHtml, reviewCountPattern, RegexOptions.Singleline);

            if (!reviewCountMatch.Success)
            { return 0; }

            string strReviewCount = reviewCountMatch.Value;
            strReviewCount = strReviewCount.Replace(",", "");

            int reviewCount = 0;

            try
            {
                reviewCount = Convert.ToInt32(strReviewCount);
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("Unable to cast review count to an integer.");
            }
            return reviewCount;
        }

        /// <summary>
        /// Returns the number of results displayed on the supplied page.
        /// </summary>
        /// <param name="page">ResultPage object</param>
        public static int GetPageResultCount(string pageHtml)
        {
            
            // Three possible formats for figuring out the number of results on the page:
            // ---------------------------------------------------------------------------            
            // Case 1: "Showing X Results" (one page)
            // Case 2: "Showing X - Y of Z Results" ( >1 page)
            // Case 3: "Your search "<search term here>" did not match any products."

            string resultCountPattern = @"(?<=id=""resultCount""><span>Showing ).*?(?= Results)";
            Match resultCountStartMatch = Regex.Match(pageHtml, resultCountPattern, RegexOptions.Singleline);
            int resultTotal = 0;

            if (!resultCountStartMatch.Success) return resultTotal;

            // Parse out the values, limiting to two maximum (as in Case 2 above)
            List<Double> resultRange = ParseDoubleValues(resultCountStartMatch.Value, 2);

            switch (resultRange.Count)
            {
                case 1:
                    try
                    { resultTotal = Convert.ToInt32(resultRange[0]); }
                    catch { }
                    break;
                case 2:
                    try
                    { resultTotal = Convert.ToInt32(resultRange[1] - (resultRange[0] - 1)); }
                    catch { }
                    break;
            }
            
            // (Case 3 doesn't need to be handled, since resultTotal will fall through and correctly remain 0)

            return resultTotal;
        }

        /// <summary>
        /// Returns a list of individual html product results from an html page
        /// </summary>
        /// <param name="pageHtml">The string containing a single page of Amazon search results</param>
        /// <param name="resultCount">Specity the number of results the method will attempt to parse</param>
        /// <returns>A list of strings representing individual html item results</returns>
        public static List<string> GetPageResultItemHtml(string pageHtml, int resultCount)
        {
            // NOTE:
            // Amazon injects additional search results (commented out in javascript) at the bottom of each search page
            // to cache results. The parameter resultCount is obtained from the top of the page so that only
            // the results that are visible to the user are returned. This was mainly done to fix a bug where duplicate
            // records were being returned.

            List<string> results = new List<string>();
            TimeSpan timeOut = new TimeSpan(0, 0, 10);

            // Grab the text between each of the results
            string resultPattern = @"(?<=result_[0-9]?[0-9]).*?(?=clear=)";

            //result_[0-9].*?clear=
            //string resultPattern = @"(?<=result_).*?(?=result_)";
            MatchCollection resultPatternMatches = Regex.Matches(pageHtml, resultPattern, RegexOptions.Singleline, timeOut);

            if (resultPatternMatches.Count < resultCount  ) {return results;}

            for (int i = 0; i < resultCount ; i++)
            {
                results.Add(resultPatternMatches[i].Value);
            }
            
            return results;
        }

        /// <summary>
        /// Extracts the product's name
        /// </summary>
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static string GetProductName(string itemHtml)
        {

            // TODO: remove html formatting characters

            // The product name is preceded by an h3 tag with a class of "newaps"
            string productNamePattern = @"(?<=newaps.*?<a href.*?<span.*?>).*?(?=</span>)";

            Match productNameMatch = Regex.Match(itemHtml, productNamePattern, RegexOptions.Singleline);

            if (!productNameMatch.Success)
            { return null; }

            string productName = Scraper.DecodeURL(productNameMatch.Value);
            
            return productName;
        }

        /// <summary>
        /// Parses a DoubleRange object representing the "high" and "low"
        /// prices from the item's html.
        /// </summary>
        /// If there is only one price supplied, set the range to that single value.
        /// <param name="html"></param>
        /// <returns>DoubleRange of the </returns>
        public static DoubleRange GetPriceRange(string itemHtml)
        {
            string pricePattern = @"(?<=li class=""newp"">.*?<a href.*?<span class=""bld lrg red"">).*?(?=</span>)";

            Match priceMatch = Regex.Match(itemHtml, pricePattern, RegexOptions.Singleline);

            if (!priceMatch.Success)
            { return new DoubleRange(); }

            List<Double> prices = ParseDoubleValues(priceMatch.Value, 2);
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
        /// Returns a product's review distribution (percentage of reviews in each category)
        /// </summary>
        /// Avoids a full page load by extrating the data from an Ajax popup.
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static ScoreDistribution GetScoreDistribution(string itemHtml)
        {
            // To get the review information without loading an entire new page,
            // we will call the review histogram popup URL instead of the main URL
            // We need the ASIN of the product to make the call, which is in the same
            // DIV tag as the product result #:
            string productASINPattern = @"(?<=name="").*?(?="">)";

            Match productASINMatch = Regex.Match(itemHtml, productASINPattern, RegexOptions.Singleline);

            if (!productASINMatch.Success)
            { return null; }

            // With the product ASIN, make the httprequest to get the review popup data            
            string htmlReview = Scraper.LoadReviewHistogram(productASINMatch.Value);

            // Find each instance of review percentage. This regex includes more than we need, but we
            // wind up only grabbing the first five results, which are the ones we care about.
            string reviewScorePattern = @"(?<=title="").*?(?=%)";

            MatchCollection reviewScoreMatches = Regex.Matches(htmlReview, reviewScorePattern, RegexOptions.Singleline);

            // If we can't find any more results, exit
            if (reviewScoreMatches.Count == 0)
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
                    reviews[4-i] = Convert.ToDouble(reviewScoreMatches[i].Value);
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("Unable to cast review score match " + i.ToString());
                    reviews[i] = -1;
                }
            }

            return new ScoreDistribution(reviews);
        }

        /// <summary>
        /// Returns a product's average review rating (double)
        /// </summary>
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static double GetRating(string itemHtml)
        {
            double rating = -1;
            // 5) Get review count / score: find span tag with class="asinReviewsSummary". <a alt="x.x out of 5 stars". 
            string ratingPattern = @"(?<=asinReviewsSummary"">.*?<a alt="").*?(?= out)";

            Match ratingMatch = Regex.Match(itemHtml, ratingPattern, RegexOptions.Singleline);

            if (!ratingMatch.Success)
            { return rating; }

            try
            {
                rating = Convert.ToDouble(ratingMatch.Value);
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("Unable to cast product rating to a double.");
            }

            return rating;
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
            string fuzzyPrimeEligibilityPattern = @"Eligible.*?for.*?FREE.*?Super.*?Saver.*?Shipping";

            Match fuzzyPrimeEligibilityMatch = Regex.Match(itemHtml, fuzzyPrimeEligibilityPattern, RegexOptions.Singleline);

            return fuzzyPrimeEligibilityMatch.Success;
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

            // Non-prime eligible results call this function with a "0" first parameter; here we look
            // specifically for "1", which denotes prime eligibility
            string primeEligiblePattern = @"bbopJS.initialize\(1,";

            Match primeEligibleMatch = Regex.Match(html, primeEligiblePattern, RegexOptions.Singleline);

            return primeEligibleMatch.Success;
        }

        /// <summary>
        /// Extracts a product's Amazon URL.
        /// </summary>
        /// Used when user clicks to access the product's Amazon listing.
        /// <param name="itemHtml"></param>
        /// <returns></returns>
        public static Uri GetURL(string itemHtml)
        {
            string productURLPattern = @"(?<=newaps.*?<a href="").*?(?="">)";

            Match productURLMatch = Regex.Match(itemHtml, productURLPattern, RegexOptions.Singleline);

            if (!productURLMatch.Success)
            { return null; }

            string result = "";

            // Amazon SOMETIMES supplies a relative URL (I don't know what determines this),
            // If the base URL is not present, prepend it.
            if (!productURLMatch.Value.Contains(Constants.BASE_URL))
            {
                result += Constants.BASE_URL;
            }

            // Concat the URL
            result += productURLMatch.Value;

            try
            {
                return new Uri(result);
            }
            catch
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
            string imageURLPattern = @"(?<=imageContainer.*?<img.*?src="").*?(?="".*?class=""productImage"")";

            Match imageURLPatternMatch = Regex.Match(itemHtml, imageURLPattern, RegexOptions.Singleline);

            if (!imageURLPatternMatch.Success)
            { return null; }

            try
            {
                Uri imageURL = new Uri(imageURLPatternMatch.Value);
                return Scraper.DownloadWebImage(imageURL);
            } catch
            {
                return null;
            }
            
        }

    } // end class
}
