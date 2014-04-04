using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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
        private SearchManager _searchManager; // Oversees product searches
        private List<IValidatable> requireValidation; // list of controls that require validation

        public MainWindow()
        {
            // Catch any vague XAML exceptions
            try { InitializeComponent(); }
            catch
            {
                MessageBox.Show("XAML initialization error.");
            }

            Icon = ResourceLoader.GetProgramIconBitmap();
            Title = "AmazonScrape";
            
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Title += " " + version;
            WindowState = System.Windows.WindowState.Maximized;

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
            ResultGrid.AddImageColumn("PrimeLogoImage", "Prime", 4, BindingMode.OneWay,"IsPrimeEligible");
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
            // If the user is clicking the scrollbar (the arrow
            // or the "thumb"), don't attempt to open an item in the browser
            IInputElement element = e.MouseDevice.DirectlyOver;
            if (element != null && element is FrameworkElement)
            {
                var elementType = element.GetType();

                if (elementType == typeof(System.Windows.Controls.Primitives.RepeatButton) ||
                    elementType == typeof(System.Windows.Controls.Primitives.Thumb))
                { return; }
            }

            if (ResultGrid.SelectedItem == null) return;
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
                MessageBox.Show("The item's URL cannot be parsed.");
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
            CancelSearch();
        }

        void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Search if the user presses the enter key on the search box
            if (e.Key == Key.Enter) Search();
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

            double minReviewCount = Convert.ToDouble(txtMinNumberOfReviews.Text);

            return new SearchCriteria(txtSearch.Text,
                Convert.ToDouble(txtNumberOfResults.Text),
                priceRange,
                ScoreDistribution.Distribution,
                minReviewCount,
                (bool)chkMatchAll.IsChecked,
                Constants.USE_STRICT_PRIME_ELIGIBILITY);
        }

        /// <summary>
        /// Validates controls, obtains the control values, and begins the 
        /// asynchronous scraping / parsing / result handling process.
        /// </summary>
        private void Search()
        {
            // Validate the search controls.
            Result<bool> result = ValidateControls();

            if (result.ErrorMessage.Length > 0)
            {
                MessageBox.Show(result.ErrorMessage);
                return;
            }

            SearchCriteria searchCriteria = GetSearchCriteria();

            // Replace the search controls with the progress control(s)
            ShowSearchProgressGrid();

            StatusTextBox.Text = "Loading results. Please be patient.";
            Progress.Value = 0;
            ResultGrid.Items.Clear();

            // Stop any previous search if it's still working
            // (in practice this should not be necessary)
            if (_searchManager != null && _searchManager.IsBusy)
            {
                string msg = "Please wait one moment before searching. ";
                MessageBox.Show(msg);
                _searchManager.CancelAsync();
            }
            
            // Coordinates the actual scraping/parsing/validation/results:            
            _searchManager = new SearchManager(searchCriteria,
                Constants.MAX_THREADS);
            _searchManager.ProgressChanged += ScraperProgressChanged;
            _searchManager.RunWorkerCompleted += ScrapeComplete;
            _searchManager.RunWorkerAsync();       
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
        private void CancelSearch()
        {
            if (_searchManager != null)
            {
                _searchManager.CancelAsync();
            }

            // Reset progress bar
            Progress.Value = 0;
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
            StatusTextBox.Text += Environment.NewLine + Environment.NewLine;
            StatusTextBox.Text += message;
            StatusTextBox.Focus();
            StatusTextBox.CaretIndex = StatusTextBox.Text.Length;
            StatusTextBox.ScrollToEnd();

        }

        /// <summary>
        /// Takes a validated result and adds it to the datagrid
        /// </summary>
        /// <param name="result"></param>
        void AddResultToGrid(AmazonItem result)
        {
            if (result == null) return;
            // Add results to the data grid as soon as they are available
                try
                { ResultGrid.Items.Add(result); }
                catch
                {
                    string msg = "Error adding item to the result grid: " +
                        result.ToString();
                    Debug.WriteLine(msg);
                }
        }

        /// <summary>
        /// Returns the state of the application to "search mode" after a search is complete.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ScrapeComplete(object sender, RunWorkerCompletedEventArgs e)        
        {
            _searchManager.CancelAsync();
            _searchManager.Dispose();

            TaskbarItemInfo.ProgressValue = 1.0;
            Progress.Value = 0;

            SolidColorBrush bg = new SolidColorBrush(Colors.Black);
            Progress.Background = bg;

            // Show the search controls again
            ShowSearchCriteriaGrid();

        }

        /// <summary>
        /// Called whenever a result comes back. Updates the progress bars.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScraperProgressChanged(object sender, ProgressChangedEventArgs args)
        {
    
            if (args == null || args.UserState == null ||
                args.UserState.GetType() != typeof(Result<AmazonItem>)) return;

            Result<AmazonItem> result = (Result<AmazonItem>)args.UserState;

            // Update the status textbox with the result message
            AppendStatusMessage(result.StatusMessage);

            // If a new result is found (it passed validation), add it to the grid
            if (result.HasReturnValue) AddResultToGrid(result.Value);

            int intPercent = args.ProgressPercentage;
            double doublePercent = intPercent / 100.0;
            
            // Update progress controls
            if (intPercent > 0)
            {
                    // Progress bars
                    TaskbarItemInfo.ProgressValue = doublePercent;                    
                    Progress.Value = intPercent;
            }
        }


    }
}