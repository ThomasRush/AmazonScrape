using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Manages the workload of one page of search results.
    /// Calls appropriate methods to load, parse,
    /// validate and return results to the SearchManager.
    /// </summary>
    /// Directly calls ItemValidator, Parser, Scraper
    public class PageManager : BackgroundWorker
    {
        
        public int PageNumber { get { return _pageNumber; } }
        public int ResultsOnPage { get { return _pageResultCount; } }
        public bool IsFirstPage { get { return _pageNumber == 1; } }
        
        // NoResults means that there were zero parsable results
        // Finished means that the results that were found are all returned
        // Error indicates a problem loading the page
        public enum Status { Working, NoResults, Finished, Error };

        public Status WorkStatus { get { return _status; } }
        private Status _status;
        
        private readonly int _pageNumber; // Which search page index we're on       
        private int _pageResultCount; // The number of results on this page

        // Holds the html for each individual product returned
        private List<string> _productHtmlSegments = new List<string>(); 
        
        // Pass in the method you'd like to use to get the html for this page
        // (allows easier test injection)
        //      int: Search page number to load
        //      string: Search terms to use
        //
        //      Returns: string html of the page load result.
        private readonly Func<int, string, string> _pageLoadMethod;
        private readonly SearchCriteria _searchCriteria;
        
        // The final list of results to be passed back
        private List<Result<AmazonItem>> _results =
            new List<Result<AmazonItem>>();
        
        /// <summary>
        /// Creates and dispatches a PageManager to load a search page,
        /// parse, extract, validate and return results using the 
        /// parameter callback EventHandlers
        /// </summary>
        /// <param name="pageNumber">Which search page number to load</param>
        /// <param name="criteria">The user's search criteria</param>
        /// <param name="pageLoadMethod">Supply the method to retrieve html</param>
        /// <param name="updateProgressMethod">Callback for progress updates</param>
        /// <param name="workerCompleteMethod">Callback when work complete</param>
        public PageManager(int pageNumber,
            SearchCriteria criteria,
            Func<int, string, string> pageLoadMethod, // see explanation above
            ProgressChangedEventHandler updateProgressMethod,
            RunWorkerCompletedEventHandler workerCompleteMethod)
        {
            
            if (pageNumber < 1)
            {
                string msg = "Supplied page number ({0}) was < 0!";
                msg = string.Format(msg,pageNumber);
                throw new ArgumentOutOfRangeException(msg);
            }

            if (pageLoadMethod == null)
            {
                string msg = "Provided a null method to obtain page HTML!";
                throw new InvalidOperationException(msg);
            }

            ProgressChanged += updateProgressMethod; // Callback
            RunWorkerCompleted += workerCompleteMethod;  // Callback
            DoWork += Work;
            
            _pageLoadMethod = pageLoadMethod;
            _searchCriteria = criteria;
            _pageNumber = pageNumber;

            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;

        }

        /// <summary>
        /// Loads, chops up, parses and validates one page worth of results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Work(object sender, DoWorkEventArgs e)
        {
            _status = Status.Working;
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Page " + _pageNumber.ToString() + " worker";

            // Set the RunWorkEventArgs so we can check its status on completion
            e.Result = this;
            
            // Will hold the page's html broken up by each individual product
            _productHtmlSegments = new List<string>();

            // Gets the entire page's html
            string pageHtml = _pageLoadMethod(_pageNumber,
                _searchCriteria.SearchText);
            
            // Get the number of results on this page
            _pageResultCount = Parser.GetPageResultCount(pageHtml);

            // If there are no results, set the status accordingly and exit
            if (_pageResultCount == 0)
            {
                _status = Status.NoResults;
                return;
            }
            else // There are results
            {
                // Break apart the page html by product
                // so they can be parsed individually
                _productHtmlSegments = Parser.GetPageResultItemHtml(pageHtml,
                    _pageResultCount);
            }

            List<Result<AmazonItem>> results = new List<Result<AmazonItem>>();
            
            // Parse and validate each result, adding to the result list
            foreach (string productHtml in _productHtmlSegments)
            {
                Result<AmazonItem> result = 
                    ParseAndValidateProductHtml(productHtml);

                // Don't worry about reporting the progress percentage here.
                // The SearchManager will look at the total results returned
                // and compare with the results requested and report that
                // percentage to the UI (passing in a dummy zero here)
                ReportProgress(0, result);
            }

            // The RunWorkerComplete method fires when method completes
            // This is used as a signal to the SearchManager that we
            // are clear to spawn another thread if necessary.
            _status = Status.Finished;
        }

        /// <summary>
        /// Parses and validates a single product's html, returning a 
        /// Result containing error messages or the valid AmazonItem
        /// </summary>
        /// <param name="html">Product html to parse</param>
        /// <returns>List of AmazonItem Results</returns>
        private Result<AmazonItem> ParseAndValidateProductHtml(string html)
        {     
            Result<AmazonItem> result = new Result<AmazonItem>();

            // Parse each item's html and exit early if validation fails on any item.
            string name = Parser.GetProductName(html);
            if (name == null || name.Length == 0)
            {
                // Do not report a "missing product name" status message here. 
                // Sometimes Amazon injects blurbs or information 
                // sections in lieu of results (book results, for example).
                // This should not trigger an error.
                return result;
            }

            if (!ItemValidator.ValidateItemName(_searchCriteria, name))
            {
                result.StatusMessage = name + " doesn't contain all search criteria.";
                return result;
            }

            // Scrape the review histogram to obtain the review distribution
            // and the review count (originally review count was being
            // obtained on the main page, but Amazon removes review
            // information from search results if they smell a bot).
            string reviewHistogramHtml = Parser.GetReviewHistogramHtml(html);
            if (reviewHistogramHtml == null || reviewHistogramHtml.Length == 0)
            {
                string msg = "Couldn't obtain review histogram data";
                result.ErrorMessage = msg;
            }

            ScoreDistribution scoreDistribution = 
                Parser.GetScoreDistribution(reviewHistogramHtml);
            if (!ItemValidator.ValidateReviewDistribution(_searchCriteria, scoreDistribution))
            {
                result.StatusMessage = name + " doesn't fall within your review distribution.";
                return result;
            }

            int reviewCount = Parser.GetReviewCount(reviewHistogramHtml);
            if (!ItemValidator.ValidateReviewCount(_searchCriteria, reviewCount))
            {
                string message = name + " ";

                if (reviewCount == 0) { message += "doesn't have any reviews."; }
                else
                {
                    message += "only has " + reviewCount.ToString() + " reviews.";
                }
                result.StatusMessage = message;
                return result;
            }
            
            DoubleRange priceRange = Parser.GetPriceRange(html);
            if (!ItemValidator.ValidatePriceRange(_searchCriteria, priceRange))
            {
                result.StatusMessage = name + " doesn't fit in your price range.";
                return result;
            }

            // Grab the item's URL so the user can go directly to the product page
            Uri url = Parser.GetURL(html);

            // Note: Right now there's no UI capability of validating average rating
            double rating = Parser.GetRating(reviewHistogramHtml);
            
            // TODO: implement a "prime-only" checkbox in the UI
            bool primeEligibility;
            if (_searchCriteria.StrictPrimeEligibility)
            {
                primeEligibility = Parser.GetStrictPrimeEligibility(url);
            }
            else
            {
                primeEligibility = Parser.GetFuzzyPrimeEligibility(html);
            }

            // Leave the image load for last since it takes longer and if the
            // item doesn't pass validation we don't waste time downloading
            BitmapImage image = Parser.GetImageThumbnail(html);

            // We have everything we need, build the AmazonItem to be returned
            result.Value = new AmazonItem(name,
                reviewCount,
                priceRange,
                scoreDistribution,
                url,
                rating,
                primeEligibility,
                image);
            
            return result;
        }

        /// <summary>
        /// Outputs useful information about the PageManager
        /// </summary>
        /// <returns>string description</returns>
        public override string ToString()
        {
            string msg = base.ToString() + Environment.NewLine;
            msg += "----------------------------------" + Environment.NewLine;
            msg += "Search Page Number: {0}" + Environment.NewLine;
            msg += "Worker Status: {1}" + Environment.NewLine;
            msg += "Results on Page: {2}" + Environment.NewLine;

            return string.Format(msg,
                _pageNumber,
                WorkStatus,
                _pageResultCount);

        }
    }
}
