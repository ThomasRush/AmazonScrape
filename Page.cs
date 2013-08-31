using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AmazonScrape
{
    /// <summary>
    /// Represents a page of Amazon search results. Handles new pageloads, keeps track of the
    /// current result, and returns results back to the SearchManager.
    /// </summary>
    public class Page
    {
        public int PageNumber { get { return _pageNumber; } }
        public int ResultsOnPage { get { return _pageResultCount; } }
        public string Html { get { return _html; } }
        public bool IsFirstPage { get { return _pageNumber == 1; } }

        private string _html; // The actual page html
        private int _pageNumber; // Which search page index we're on
        private int _resultIndex; // Which result on the page we're working on
        private string _searchTerms; // Search terms specified by the user
        private int _pageResultCount; // The number of results on this page
        private List<string> _resultItemHtml = new List<string>(); // Holds each item's html
        
        // int: Page Number
        // string: Search Terms
        // returns string html of the page load result.
        private Func<int, string, string> _pageLoadMethod;


        public Page(string searchTerms, Func<int, string, string> pageLoadMethod)
        {
            _pageLoadMethod = pageLoadMethod;
            _searchTerms = searchTerms;
            _pageNumber = 0;
        }

        public void LoadPage()
        {
            _pageNumber += 1;
            _resultIndex = 0;
            _resultItemHtml = new List<string>();

            // This could be replaced with the TestResourceLoader call.
            _html = _pageLoadMethod(_pageNumber, _searchTerms);

            //_html = Scraper.LoadSearchPage(_pageNumber, _searchTerms);

            // This is returning zero results on the page at the end of a search:
            _pageResultCount = Parser.GetPageResultCount(_html);

            if (_pageResultCount > 0) _resultItemHtml = Parser.GetPageResultItemHtml(_html, _pageResultCount);
            
        }

        public bool RequiresNewPageLoad()
        {            
            return (_resultIndex > (_resultItemHtml.Count - 1));
        }

        public string GetNextItemHtml()
        {
            if (RequiresNewPageLoad()) LoadPage();

            if (_resultItemHtml.Count == 0) return "";

            // Ideally there would be one method for "do the next thing"
            // and you wouldn't need to check for a new page load
            // the important thing would be that it would take care of that
            // next step and then return progress.
            string resultHtml = "";
            try
            { resultHtml = _resultItemHtml[_resultIndex]; }
            catch (Exception)
            { throw; }

            _resultIndex += 1;

            return resultHtml;
        }

    }
}
