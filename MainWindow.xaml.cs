using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AmazonScrape
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {               
        private BackgroundWorker _scrapeWorker; // Performs async work
                private List<IValidatable> requireValidation; // list of controls that require validation

        public MainWindow()
        {
            // Catch any vague XAML exceptions
            try { InitializeComponent(); }
            catch
            {
                MessageBox.Show("XAML initialization error.");
            }

            this.Icon = ResourceLoader.GetProgramIconBitmap();
            this.Title = "Amazon Product Scraper";
            this.WindowState = System.Windows.WindowState.Maximized;

            // Create an async worker that will scrape Amazon and update our progress
            _scrapeWorker = new BackgroundWorker();
            _scrapeWorker.WorkerReportsProgress = true;
            _scrapeWorker.WorkerSupportsCancellation = true;
            _scrapeWorker.DoWork += ScraperDoWork;
            _scrapeWorker.ProgressChanged += ScraperProgressChanged;
            _scrapeWorker.RunWorkerCompleted += ScrapeComplete;

            // Specify the controls requiring validation
            // (validation properties are set in XAML)
            requireValidation = new List<IValidatable>();            
            requireValidation.Add(txtSearch);
            requireValidation.Add(txtNumberOfResults);
            requireValidation.Add(txtMinNumberOfReviews);
            requireValidation.Add(PriceRange);

            Style smallTextStyle = ResourceLoader.GetControlStyle("DataGridSmallTextStyle");
            Style mediumTextStyle = ResourceLoader.GetControlStyle("DataGridMediumTextStyle");
            Style largeTextStyle = ResourceLoader.GetControlStyle("DataGridLargeTextStyle");
            
            // Specify the result grid format
            ResultGrid.ColumnHeaderHeight = 40;
            ResultGrid.MouseDoubleClick += dataGrid_MouseDoubleClick;
            ResultGrid.PreviewMouseLeftButtonDown += dataGrid_PreviewMouseLeftButtonDown;
            ResultGrid.AddImageColumn("ProductImage", "Product", 5, BindingMode.TwoWay);
            ResultGrid.AddImageColumn("PrimeLogoImage", "Prime", 4, BindingMode.OneWay);
            ResultGrid.AddTextColumn("Name", "Product Name", 13, mediumTextStyle);
            ResultGrid.AddTextColumn("LowPrice", "Low Price", 5, largeTextStyle);
            ResultGrid.AddTextColumn("HighPrice", "High Price", 5, largeTextStyle);
            ResultGrid.AddTextColumn("Rating", "Rating", 3, largeTextStyle);
            ResultGrid.AddTextColumn("ReviewCount", "Reviews", 5, largeTextStyle);
            ResultGrid.AddTextColumn("ReviewDistribution", "Distribution", 5,smallTextStyle);
            ResultGrid.AddButtonColumn("Open", 3, new RoutedEventHandler(OpenInBrowser_Click));

            // Set focus to the search control once the window is loaded
            this.Loaded += MainWindow_Loaded;

        }

        
        void SearchControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }
                   
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {            
            txtSearch.Focus();
        }

        /// <summary>
        /// If the user clicks in a data grid area that is not a result, remove the
        /// grid selection.
        /// </summary>
        /// This is to prevent the user from being able to double click
        /// an empty part of the data grid to open a selected item in a browser tab.
        /// The intended behavior is to only open an item in a browser if the user
        /// double-clicks directly on a data grid result.
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement element = e.MouseDevice.DirectlyOver;
            if (element != null && element is FrameworkElement)
            {
                // If the element selected is of type scroll viewer, it means that the
                // user is not clicking on a data grid result. In that case, remove
                // the current selection
                if (element.GetType() == typeof(System.Windows.Controls.ScrollViewer))
                { ResultGrid.SelectedIndex = -1; }
            }
        }

        /// <summary>
        /// Double clicking a result item will attempt to open the item's URL
        /// in whatever program they have associated with URLs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ResultGrid.SelectedItem == null) return;
            AmazonItem item = ResultGrid.SelectedItem as AmazonItem;
            if (item.URL == null)
            {
                MessageBox.Show("The item's URL cannot be parsed.");
                return;
            }
            OpenWebpage(item.URL.ToString());
        }

        /// <summary>
        /// Attempts to open the user's default web browser and direct
        /// it to the supplied url.
        /// </summary>
        /// <param name="url"></param>
        private void OpenWebpage(string url)
        {
            // TODO: reported bug that this doesn't work on a machine that
            //    does not have framework 4.5 installed. Get more details.
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception)
            {
                MessageBox.Show("Error while trying to open browser for the requested product.");
            }
        }

        /// <summary>
        /// Obtains the selected result item's URL and attempts to open
        /// the user's default web browser to that page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            AmazonItem item = ((FrameworkElement)sender).DataContext as AmazonItem;

            if (item.URL == null)
            {
                //MessageBox.Show("The item's URL cannot be parsed.");
                return;
            }
            OpenWebpage(item.URL.ToString());            
        }

        /// <summary>
        /// Cancels an ongoing search.
        /// </summary>
        /// Because the search is being processed by a BackgroundWorker thread,
        /// the cancellation will not be instantaneous.
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBox.Text += Environment.NewLine + Environment.NewLine;
            StatusTextBox.Text += "Canceling search! Please be patient.";
            CancelScrapeWorker();
        }

        void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Search if the user presses the enter key on the search box
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }

        /// <summary>
        /// Loop through each control marked as validatable and check its
        /// validation status. If a single control is not valid (Status.Error),
        /// validation stops and the ValidationResult is immediately returned.
        /// </summary>
        /// <returns>ValidationResult</returns>
        private Result<bool> ValidateControls()
        {
            Result<bool> result = new Result<bool>();

            foreach (IValidatable validatable in requireValidation)
            {
                result = validatable.Validate<bool>();
                if (result.ErrorMessage.Length > 0) return result;
            }
            return result;
        }

        /// <summary>
        /// Obtains the current control values and returns a SearchCriteria object.
        /// Note: does not validate the controls; perform validation before calling.
        /// </summary>
        /// <returns></returns>
        private SearchCriteria GetSearchCriteria()
        {
            DoubleRange priceRange = PriceRange.GetValues();
            //ForceTextUpdate(
            double minReviewCount = Convert.ToDouble(txtMinNumberOfReviews.Text);

            //ForceTextUpdate(
            return new SearchCriteria(txtSearch.Text,
                Convert.ToDouble(txtNumberOfResults.Text),
                priceRange,
                ScoreDistribution.Distribution,
                minReviewCount,
                (bool)chkMatchAll.IsChecked);
        }

        /// <summary>
        /// Validates controls, obtains the control values, and begins the 
        /// asynchronous scraping / parsing / result handling process.
        /// </summary>
        private void Search()
        {
            // Validate the search controls.
            //ValidationResult result = ValidateControls();
            Result<bool> result = ValidateControls();

            if (result.ErrorMessage.Length > 0)
            {
                MessageBox.Show(result.ErrorMessage);
                return;
            }

            SearchCriteria criteria = GetSearchCriteria();

            // Replace the search controls with the progress control(s)
            ShowSearchProgressGrid();

            StatusTextBox.Text = "Loading search results; please be patient.";
            Progress.Value = 0;
            ResultGrid.Items.Clear();
            
            // Begin work
            _scrapeWorker.RunWorkerAsync(criteria);
        }

        /// <summary>
        /// Replaces the search controls with "search progress" controls when a user searches
        /// A cancel button and a text area that displays which items were excluded from the results.
        /// </summary>
        private void ShowSearchProgressGrid()
        {
            SearchLayoutGrid.Visibility = System.Windows.Visibility.Hidden;
            SearchProgressGrid.Visibility = System.Windows.Visibility.Visible;

            DataLayoutGrid.ColumnDefinitions[0].Width = new GridLength(84, GridUnitType.Star);
            DataLayoutGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            DataLayoutGrid.ColumnDefinitions[2].Width = new GridLength(15, GridUnitType.Star);
            
            // The initial page load can take a few moments, so make the
            // progress bar animate to show that work is being done.           
            SolidColorBrush bg = new SolidColorBrush(Colors.Black);
            Progress.Background = bg;
            
            ColorAnimation animation = new ColorAnimation()
            {
                From = Colors.Black,
                To = Colors.Gray,
                Duration = TimeSpan.FromMilliseconds(750),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true,
            };

            bg.BeginAnimation(SolidColorBrush.ColorProperty, animation);

            CancelButton.Focus();
            
        }

        /// <summary>
        /// Makes the search criteria controls visible again after a search is completed (or canceled).
        /// </summary>
        private void ShowSearchCriteriaGrid()
        {
            DataLayoutGrid.ColumnDefinitions[0].Width = new GridLength(70, GridUnitType.Star);
            DataLayoutGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            DataLayoutGrid.ColumnDefinitions[2].Width = new GridLength(30, GridUnitType.Star);

            SearchProgressGrid.Visibility = System.Windows.Visibility.Hidden;
            SearchLayoutGrid.Visibility = System.Windows.Visibility.Visible;

            // Return focus to the search textbox
            txtSearch.Focus();

        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        { Search(); }


        /// <summary>
        /// Called when the user cancels a search. May take a few
        /// seconds while it waits for the async thread to be ready.
        /// </summary>
        private void CancelScrapeWorker()
        {            
            _scrapeWorker.CancelAsync();

            // Wait until the cancel is complete
            while (_scrapeWorker.CancellationPending)
            {
                Scraper.DoEvents();
            }

            // Reset progress bar
            Progress.Value = 0;
        }


        void ScraperDoWork (object sender, DoWorkEventArgs e)
        {
            var searchCriteria = e.Argument as SearchCriteria;
            
            // Coordinates the actual scraping/parsing/validation/results:
            SearchManager searchManager = new SearchManager(searchCriteria);

            Result<AmazonItem> result = new Result<AmazonItem>();

            while (searchManager.Working)
            {
                // If the user has pressed the Cancel button, 
                // stop the search manager
                if (_scrapeWorker.CancellationPending)
                { searchManager.Working = false; }

                result = searchManager.ProcessNextItem();
                
                // Update the status textbox with the result message
                AppendStatusMessage(result.StatusMessage);

                // If a new result is found (it passed validation), add it to the grid
                if (result.HasReturnValue)
                {
                    AddResultToGrid(result.Value);

                    // Update our progress bar
                    _scrapeWorker.ReportProgress(searchManager.GetPercentComplete());
                }
            }
        }

        /// <summary>
        /// Displays a work progress message in the status textbox.
        /// Right now this only displays items that have failed validation. Mostly
        /// this is there to show that the application is working and isn't stalled out.
        /// </summary>
        /// <param name="message"></param>
        void AppendStatusMessage(string message)
        {
            if (message == null || message == "") return;

            // Add results to the data grid as soon as they are available
            // Must do this via dispatch since the objects belong to the
            // background worker thread
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                try
                {
                    StatusTextBox.Text += Environment.NewLine + Environment.NewLine;
                    StatusTextBox.Text += message;
                    StatusTextBox.Focus();
                    StatusTextBox.CaretIndex = StatusTextBox.Text.Length;
                    StatusTextBox.ScrollToEnd();
                }
                catch
                {
                    // TODO: How to better report this? As it stands now, every error causes its own
                    // messagebox, which is chaos if something goes wrong.
                    //MessageBox.Show("An error has occurred while trying to write to the status messsage area");
                }
            }));

        }

        /// <summary>
        /// Takes a validated result and adds it to the datagrid
        /// </summary>
        /// <param name="result"></param>
        void AddResultToGrid(AmazonItem result)
        {
            if (result == null) return;
            // Add results to the data grid as soon as they are available
            // Must do this via dispatch since the objects belong to the
            // background worker thread
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                try
                { ResultGrid.Items.Add(result); }
                catch
                {
                    //MessageBox.Show("An error has occurred while trying to populate the data grid");
                    // Supply some kind of error message, but not using a messagebox (each item opens a new modal dialog)
                }
            }));
        }

        /// <summary>
        /// Returns the state of the application to "search mode" after a search is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ScrapeComplete(object sender, RunWorkerCompletedEventArgs e)        
        {
            TaskbarItemInfo.ProgressValue = 1.0;

            // If the work manager is no longer working, return
            // control to the user
            Progress.Value = 0;
            
            SolidColorBrush bg = new SolidColorBrush(Colors.Black);
            Progress.Background = bg;
            
            // Show the search controls again
            ShowSearchCriteriaGrid();

        }

        /// <summary>
        /// Updates the progressbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScraperProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > 0)
                TaskbarItemInfo.ProgressValue = e.ProgressPercentage/100.0;
            // Update the progress bar
            Progress.Value = e.ProgressPercentage;
        }

    } // class
} // namespace
