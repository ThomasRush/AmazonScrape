using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Loads image or control style resources
    /// </summary>
    public static class ResourceLoader
    {

        /// <summary>
        /// Load the Amazon Prime logo
        /// </summary>
        /// <returns></returns>
        public static BitmapImage GetPrimeLogoBitmap()
        {
            BitmapImage primeLogoBitmap = new BitmapImage();
            try
            {
                primeLogoBitmap = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/prime_logo.png", UriKind.RelativeOrAbsolute));
            }
            catch (Exception)
            { Console.WriteLine("Can't load prime logo resource."); }

            return primeLogoBitmap;
        }

        /// <summary>
        /// Load the program icon
        /// </summary>
        /// <returns></returns>
        public static BitmapImage GetProgramIconBitmap()
        {
            BitmapImage programIconBitmap = new BitmapImage();
            try
            {
                programIconBitmap = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/icon.ico", UriKind.RelativeOrAbsolute));
            }
            catch (Exception)
            { Console.WriteLine("Couldn't load program icon."); }

            return programIconBitmap;
        }

        /// <summary>
        /// Dynamically load a control style.
        /// </summary>
        /// <param name="styleName"></param>
        /// <returns></returns>
        public static Style GetControlStyle(string styleName)
        {
            if (!UriParser.IsKnownScheme("pack"))
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);

            ResourceDictionary dict = new ResourceDictionary();
            Uri uri = new Uri("/Resources/ControlStyles.xaml", UriKind.Relative);
            dict.Source = uri;
            Application.Current.Resources.MergedDictionaries.Add(dict);

            Style style;
            try
            {
                style = (Style)Application.Current.Resources[styleName];
            }
            catch
            {
                throw new ResourceReferenceKeyNotFoundException("Can't find the Style " + styleName, styleName);
            }

            return style;
        }


    } // class
} // namespace
