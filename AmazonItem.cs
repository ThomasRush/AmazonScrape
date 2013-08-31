using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AmazonScrape
{
    /// <summary>
    /// Represents an Amazon product
    /// Used to populate the data grid.
    /// </summary>
    public class AmazonItem
    {
        // Data grid doesn't display null values, so using
        // all nullable public fields
        public String Name { get { return _name; } }
        public ImageSource ProductImage { get { return _image; } set { ProductImage = value; } }
        public Uri URL { get { return _url; } }

        public Double? LowPrice 
        { 
            get 
            { 
                if (_priceRange.Low == double.MinValue) { return null; } 
                else { return _priceRange.Low; } 
            }
        }

        public Double? HighPrice
        {
            get
            {
                if (_priceRange.High == double.MaxValue || _priceRange.High == _priceRange.Low ) { return null; }
                else { return _priceRange.High; }
            }
        }

        public Double? Rating 
        { 
            get 
            {
                if (_rating == -1) return null;
                else return _rating;
            }
        }
        public Int32? ReviewCount { get { return _reviewCount; } }
        public ImageSource PrimeLogoImage
        {
            get
            {
                if (_primeEligible) return ResourceLoader.GetPrimeLogoBitmap();
                else return null;
            }
        }
        public String ReviewDistribution 
        { 
            get 
            {
                if (_reviewDistribution != null)
                {
                    return _reviewDistribution.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        String _name;
        int _reviewCount;
        DoubleRange _priceRange;
        ScoreDistribution _reviewDistribution;
        Uri _url;
        double _rating;
        bool _primeEligible;
        BitmapImage _image;

        public AmazonItem(string name,
                          int reviewCount,
                          DoubleRange priceRange,
                          ScoreDistribution reviewDistribution,
                          Uri url,
                          double rating,
                          bool primeEligible,
                          BitmapImage image)
        {
            _name = name;
            _reviewCount = reviewCount;
            _priceRange = priceRange;
            _reviewDistribution = reviewDistribution;
            _url = url;
            _rating = rating;
            _primeEligible = primeEligible;
            _image = image;
    
        }

    }
}
