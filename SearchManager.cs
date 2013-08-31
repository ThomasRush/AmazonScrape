using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Manages the workflow of the scraper. Returns WorkProgress object to report status and successful results.
    /// </summary>
    class SearchManager
    {
  
        public bool Working { get; set;} // Is the WorkManager finished?
        private SearchCriteria _searchCriteria; // User-specified search criteria
        private Page _resultPage; // Contains the state of a single page of Amazon product results
        private int _validResultCount; // The number of results matching the user's criteria (which ultimately populates the grid)

        private List<AmazonItem> _results; // the actual results returned to the grid

        public SearchManager(SearchCriteria searchCriteria)
        {
            _results = new List<AmazonItem>();
            _searchCriteria = searchCriteria;

            Working = true;
            _resultPage = new Page(searchCriteria.SearchText, Scraper.LoadSearchPage);
        }

        public bool IsFirstPage { get { return _resultPage.IsFirstPage; } }

        /// <summary>
        /// Processes the next result.
        /// Loads a new page if necessary.
        /// </summary>
        public Result<AmazonItem> ProcessNextItem()
        {
            // Holds returned results and any status/error messages
            Result<AmazonItem> result = new Result<AmazonItem>();

            // Get the next item's html ( loads a new page if necessary )
            string itemHtml = _resultPage.GetNextItemHtml();

            // If no results
            if (itemHtml.Length == 0)
            {
                // TODO: This should be done from the main window, not here (separate UI from logic)
                if (_resultPage.IsFirstPage) { MessageBox.Show("No results for the provided search term(s)."); }

                // If we're out of results, stop working and return progress
                Working = false;
                return result;
            }

            // Parse each item's html and exit early if validation fails on any item.
            string name = Parser.GetProductName(itemHtml);
            if (name == null || name.Length == 0) {
                
                // Do not report a "missing product name" status message here. 
                // Sometimes Amazon injects blurbs or information 
                // sections in lieu of results. It's misleading to say 
                // a product name could not be found, since
                // there is no product name in that case.
                //result.StatusMessage = "Could not obtain product name.";
                return result;
            }

            if (!ItemValidator.ValidateItemName(_searchCriteria,name)) 
            {
                result.StatusMessage = name + " doesn't contain all search criteria.";
                return result;
            }

            int reviewCount = Parser.GetReviewCount(itemHtml);
            if (!ItemValidator.ValidateReviewCount(_searchCriteria, reviewCount))             
            {
                string message = name + " ";

                if (reviewCount == 0) { message += "doesn't have any reviews."; }
                else {
                    message += "only has " + reviewCount.ToString() + " reviews.";
                }
                result.StatusMessage = message;
                return result;
            }

            DoubleRange priceRange = Parser.GetPriceRange(itemHtml);
            // If there's only one value being set, that should be the entire "range"

            // Note: Really we're using DoubleRange for two different purposes:
            // 1) Is the expected usage: for specifying a definite range of values, where
            //    unsupplied boundaries means "the min & max values supported by the data type"
            // 2) The not-so-expected usage: As a container for potentially two values,
            //    where the absence of one doesn't mean "anything is fine", but instead
            //    "limit the range to the supplied value only"

            if (!ItemValidator.ValidatePriceRange(_searchCriteria, priceRange)) 
            {
                result.StatusMessage = name + " doesn't fit in your price range.";
                return result;
            }

            ScoreDistribution scoreDistribution = Parser.GetScoreDistribution(itemHtml);
            if (!ItemValidator.ValidateReviewDistribution(_searchCriteria, scoreDistribution))             
            {
                result.StatusMessage = name + " doesn't fall within your review distribution.";
                return result;
            }

            // Grab the item's URL so the user can go directly to the product page
            Uri url = Parser.GetURL(itemHtml);
            
            double rating = Parser.GetRating(itemHtml);
            // Note: Right now there's no UI capability of validating average rating

            // TODO: implement a "prime-only" checkbox in the UI
            bool primeEligibility;
            if (_searchCriteria.StrictPrimeEligibility)
            {
                primeEligibility = Parser.GetStrictPrimeEligibility(url);
            }
            else
            {
                primeEligibility = Parser.GetFuzzyPrimeEligibility(itemHtml);
            }

            // Leave the image load for last since it takes longer,
            // and if the item doesn't pass validation, we don't waste time loading it
            BitmapImage image = Parser.GetImageThumbnail(itemHtml);

            // If we're here, the item passed all validateion
            _validResultCount += 1;

            // We have everything we need, build the AmazonItem to be
            // returned to the results grid
            AmazonItem resultItem = new AmazonItem(name, 
                                                   reviewCount, 
                                                   priceRange, 
                                                   scoreDistribution, 
                                                   url, 
                                                   rating, 
                                                   primeEligibility, 
                                                   image);

            // Add it to the total results for this search
            _results.Add(resultItem);

            // Add it to the work result (what gets passed back to the UI)
            result.Value = resultItem;

            if (GetPercentComplete() == 100) { Working = false; }

            return result;

        }

        public int GetResultCount()
        {
            return _results.Count;
        }

        /// <summary>
        /// Returns integer percentage of overall progress, based on the number
        /// of retrieved and validated results.
        /// </summary>
        /// <returns></returns>
        public int GetPercentComplete()
        {
            int percent = 0;
            try
            {
                decimal resultsCount = (decimal)_validResultCount;
                decimal totalResults = (decimal)_searchCriteria.NumberOfResults;
                percent = (int)((resultsCount / totalResults)*100);
            } catch // TODO: conversion / divide by zero exceptions
            { }
            
            return percent;
        }

    }
}
