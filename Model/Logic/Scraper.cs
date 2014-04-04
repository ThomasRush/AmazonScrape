using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Controls;
using System.Web;

namespace AmazonScrape
{
    /// <summary>
    /// Performs page and image loads and provides methods to
    /// encode/decode strings.
    /// </summary>
    public static class Scraper
    {
        /// <summary>
        /// Encodes the supplied string for use as a URL
        /// </summary>
        /// <param name="URL">string to encode</param>
        /// <returns>URL-encoded string</returns>
        public static string EncodeURL(string URL)
        {
            try
            {
                return HttpUtility.UrlEncode(URL).Replace("+","%20");
            }
            catch
            {
                string msg = "Unable to encode URL: " + URL;
                throw new ArgumentException(msg);
            }
            
        }

        /// <summary>
        /// Decodes the HTML-encoded sections of the supplied string
        /// </summary>
        /// <param name="html">HTML-encoded string</param>
        /// <returns>decoded string</returns>
        public static string DecodeHTML(string html)
        {
            string result = "";
            try
            {
                result = HttpUtility.HtmlDecode(html);
            }
            catch
            {
                string msg = "Unable to decode HTML: " + html;
                throw new ArgumentException(msg);
            }

            return result;
        }

        /// <summary>
        /// Given a product's unique Amazon ID, loads the review distribution histogram.
        /// Much faster than an entire pageload for detailed review info.
        /// </summary>
        /// <param name="asin"></param>
        /// <returns></returns>
        public static string LoadReviewHistogram(string asin)
        {
            Uri reviewHistogramPopupURL = new Uri(Constants.REVIEW_HISTOGRAM_URL + asin);

            return Scraper.CreateHttpRequest(reviewHistogramPopupURL);
        }

        /// <summary>
        /// Loads the result page based on the criteria and the supplied page index, determines the number of valid
        /// results on the page, and returns a list of strings representing the markup for each result on the page.
        /// Those results can be used by the other Scraper methods to obtain specific pieces of data (e.g. product name).
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public static string LoadSearchPage(int pageIndex, string searchTerms)
        {
            if (searchTerms == null) return "";

            // Encode characters that are not URL-friendly
            // example: "C#" should become "C%23"
            searchTerms = EncodeURL(searchTerms);

            string URL = Constants.SEARCH_URL + searchTerms + Constants.SEARCH_URL_PAGE_PARAM + pageIndex.ToString();

            return CreateHttpRequest(new Uri(URL));
        }

        public static BitmapImage DownloadWebImage(Uri url)
        {
            // TODO: this method needs to gracefully handle unresolvable URLs and connection time-outs
            BitmapImage bitmap = new BitmapImage(url, 
                new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable));
            
            // To make the BitmapImages thread-safe for the BackgroundWorker, they need to
            // be frozen (bitmap.Freeze()), but they shouldn't be frozen until they're done downloading.
            // We have to force the UI thread to wait until the image is downloaded so we can freeze it.
            // Otherwise, we get images in the grid that are partially-downloaded.

            // TODO: this is poor design, but after much searching, there may not be a better solution.
            //  according to MSDN, DispatcherFrames can be implemented for
            //  "Short running, very specific frames that exit when an important criteria is met."
            while (bitmap.IsDownloading) { DoEvents(); };
            bitmap.Freeze(); // Bitmap is now thread-safe and can be operated on by the backgroundworker
            return bitmap;
        }

        /// <summary>
        /// Used to force the background worker to wait for a condition before proceeding.
        /// </summary>
        public static void DoEvents()
        {
            try
            {
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback((f) =>
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
                Dispatcher.PushFrame(frame);
            }
            catch (OutOfMemoryException)
            {
                // TODO: If this Exception is ever hit, we want to handle it by
                // cancelling the search. See if there is a clean way of doing that.
                MessageBox.Show("Out of memory");
            }
        }
        
        /// <summary>
        /// Given a URL, loads and returns a string representing a web page's markup
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        public static String CreateHttpRequest(Uri URL)
        {
            WebRequest request = HttpWebRequest.Create(URL);
            request.Method = "GET";

            String html = "";
            try
            {
                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    html = reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load page " + URL.ToString() + ". Check your internet connection.");
            }
            return html;
        }

    } // Class
} // Namespace