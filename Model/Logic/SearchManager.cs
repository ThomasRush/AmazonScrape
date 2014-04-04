using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AmazonScrape
{
    /// <summary>
    /// Manages the logical workflow of the application.
    /// Creates and deploys PageManagers.
    /// Returns WorkProgress object to report status and successful results.
    /// </summary>
    public sealed class SearchManager: BackgroundWorker
    {
        // Total number of results considered in the search (used for testing)
        private int _resultCount = 0;

        // The number of results matching the user's criteria.
        // Used when calculating percent complete.
        private int _validResultCount = 0;

        private SearchCriteria _searchCriteria; // User-specified search criteria        
        private bool _working; // Is the main search thread still active?
        private readonly int _threadCount; // Number of PageManagers to spawn

        // Loads/parses/validates individual pages asyncrhonously
        // The "worker threads" of the application
        private PageManager[] _pageManagers;

        // Maintain the highest search page currently being scraped
        private int _pageNumber = 1;

        /// <summary>
        /// Create and start work on a new search. Spawns PageManagers.
        /// </summary>
        /// <param name="searchCriteria">User-supplied search criteria</param>
        /// <param name="threadCount">Number of pages to search asynchronously</param>
        public SearchManager(SearchCriteria searchCriteria, int threadCount)
        {
            if (threadCount < 2)
            {
                string msg = "Application misconfigured. ";
                msg += "Set the MAX_THREADS constant to a value > 1";
                throw new ArgumentException(msg);
            }

            _searchCriteria = searchCriteria;
            _threadCount = threadCount;
            DoWork += Work;
            
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Sets up PageManager threads and begin search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Work(object sender, DoWorkEventArgs e)
        {
            _working = true;

            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Search Manager: " + 
                    _searchCriteria.SearchText;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
        
            // Set up the page managers, each running on an async thread
            _pageManagers = new PageManager[_threadCount];
            
            for (int i = 0; i < _pageManagers.Count(); i++)
            {
                // Keep track of the highest page we've
                // attempted to scrape (page numbers are not zero-based)
                _pageNumber = i+1;

                // PageManagers internally fire off an async worker which each
                // call the ResultReturned / WorkerFinished event handlers.
                _pageManagers[i] = new PageManager(
                    _pageNumber,
                    _searchCriteria,
                    Scraper.LoadSearchPage, // inject method for testing here
                    ResultReturned,
                    WorkerFinished);
                _pageManagers[i].RunWorkerAsync();
            }

            while (_working)
            {
                // User can cancel a search through the UI.
                if (CancellationPending)
                {
                    HaltAllOngoingWork();                    
                }
            }
            
            stopwatch.Stop();
            string msg = "Search time : {0} ms" + Environment.NewLine;
            msg = string.Format(msg, stopwatch.ElapsedMilliseconds);
            Debug.WriteLine(msg);            
        }

        /// <summary>
        /// True if the search was cancelled, we've reached 100% of the
        /// desired number of results, or all of the threads are finished
        /// </summary>
        /// <returns></returns>
        private bool IsWorkFinished()
        {
            // If the main search has been cancelled by the user
            if (_working == false)
            {
                string msg = "SearchManager no longer set to 'Working'";
                Debug.WriteLine(msg);
                return true; 
            }

            // If all specified results have been returned
            if (GetPercentComplete() >= 100)
            {
                Debug.WriteLine("=== Percent Complete at or above 100 ===");
                return true; 
            }

            // If all worker threads are no longer working
            // (finished / no more results to search)
            int workingCount = _pageManagers.Where(i => i.WorkStatus ==
                PageManager.Status.Working).Count();
            if (workingCount == 0)
            {
                Debug.WriteLine("No working PageManagers (none with Working status)");
                OutputPageManagerStatus();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Called by the PageManagers whenever they have a result. Returns
        /// the result back to the UI. Also checks and stops work if finished.
        /// </summary>
        /// <param name="obj">sender object</param>
        /// <param name="args">Result<AmazonItem></param>
        public void ResultReturned(object obj, ProgressChangedEventArgs args)
        {
            
            string msg = "Number of results considered:{0}";
            msg = string.Format(msg, _resultCount);
            _resultCount += 1;
            Debug.WriteLine(msg);

            Result<AmazonItem> result;
            
            // Grab the result
            if (args.UserState.GetType() == typeof(Result<AmazonItem>))
            {            
                result = (Result<AmazonItem>)args.UserState;

                // If we're already done, stop all threads
                // still active and exit
                if (IsWorkFinished())
                {
                    HaltAllOngoingWork();
                    return;
                }

                ReportProgress(GetPercentComplete(), result);
                
                // If it was a result that fit our search critera, update
                // our counter (used by the GetPercentComplete method)
                if (result.HasReturnValue) _validResultCount += 1;

            }
        }

        /// <summary>
        /// Stops and disposes all PageManager threads and marks
        /// the main search as finished working
        /// </summary>
        private void HaltAllOngoingWork()
        {
            // End each worker thread
            foreach (PageManager page in _pageManagers)
            {
                page.CancelAsync();
                page.Dispose();
            }

           _working = false;

        }

        /// <summary>
        /// Called by PageManagers when their task is complete.
        /// If there is still work to be done, replaces the finished
        /// pageManager with a new one (creates a new thread)
        /// Exits if all work is complete.
        /// </summary>
        /// <param name="obj">sender object</param>
        /// <param name="args">Result<AmazonItem></param>
        public void WorkerFinished(object obj, RunWorkerCompletedEventArgs args)
        {
            // See if any of the workers are reporting that they're out
            // of results.
            bool outOfResults = _pageManagers.Any(i => i.WorkStatus ==
                PageManager.Status.NoResults);
            
            // If so, don't deploy another thread.
            if (outOfResults)
            {
                string msg = "PageManager reporting no more results;";
                msg += " not deploying another thread.";
                Debug.WriteLine(msg);
                return;
            }
            
            // Or if there are no threads that are marked "Working", 
            // we are done
            if (IsWorkFinished())
            {
                HaltAllOngoingWork();
                return;
            }
            
            if (args == null) return;
            if (args.Error != null)
            {
                Debug.WriteLine(args.Error.Message);
                return;
            }
            
            if (args.Result == null || 
                args.Result.GetType() != typeof(PageManager)) return;

            // If this PageManager is done but we haven't hit our
            // target number of results, we should spawn a new thread
            PageManager finished = (PageManager)args.Result;
                        
            // Get the index of the PageManager whose search page number
            // matches the one that just finished (we're going to replace
            // it with a new PageManager)
            int index =_pageManagers.ToList().FindIndex(i => 
                i.PageNumber == finished.PageNumber);               

            // Increment the variable that tracks the
            // highest page number we've searched so far
            // TODO: since page number is shared state, there is a
            //   slight chance that two PageManagers might hit this
            //   code 
            _pageNumber += 1;

            // Start searching a new page
            PageManager newPageManager = new PageManager(
                _pageNumber,
                _searchCriteria,
                Scraper.LoadSearchPage, // inject method for testing here
                ResultReturned,
                WorkerFinished);

            
            _pageManagers[index].Dispose(); // get rid of old one
            _pageManagers[index] = newPageManager;
            _pageManagers[index].RunWorkerAsync();

        }

        /// <summary>
        /// Returns integer percentage of overall progress, based on the number
        /// of retrieved and validated results.
        /// </summary>
        /// <returns></returns>
        public int GetPercentComplete()
        {
            // NumberOfResults is validated in the UI to be > 0
            // Catch DBZ error anyway (would result in end of search if error)
            int percent = 100;
            try
            {
                decimal resultsCount = (decimal)_validResultCount;
                decimal totalResults = (decimal)_searchCriteria.NumberOfResults;
                percent = (int)((resultsCount / totalResults)*100);
            } catch
            { }
            
            return percent;
        }

        /// <summary>
        /// Write PageManager WorkStatus to console for debugging
        /// </summary>
        private void OutputPageManagerStatus()
        {
            string msg = "";
            for (int i = 0; i < _pageManagers.Count(); i++)
            {
                msg = "Thread index : {0}" + Environment.NewLine;
                msg += "{1}" + Environment.NewLine + Environment.NewLine;
                msg = string.Format(msg, i, _pageManagers[i]);
                Debug.WriteLine(msg);
            }
        }

    }
}
