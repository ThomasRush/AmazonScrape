using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    public static class ItemValidator
    {
        public static bool ValidateItemName(SearchCriteria criteria, string itemName)
        {
            if (criteria.MatchAllSearchTerms)
            {
                if (itemName == null) { return false; }

                // Make sure every search term is present in the item name
                string[] terms = criteria.SearchText.ToLower().Split(' ');
                for (int i = 0; i < terms.Count(); i++)
                {
                    if (!itemName.ToLower().Contains(terms[i]))
                    {
                        return false;
                    }
                }                
            }

            return true;
        }

        public static bool ValidateReviewCount(SearchCriteria criteria, int count)
        {
            if (count < criteria.MinNumberReviews)
            {
                return false;
            }

            return true;
        }

        public static bool ValidatePriceRange(SearchCriteria criteria, DoubleRange priceRange)
        {
            // If the user specified a price range, but the item has no price information, fail
            if (criteria.PriceRange.HasRangeSpecified && !priceRange.HasRangeSpecified) { return false; }

            if (criteria.PriceRange != null && criteria.PriceRange.HasRangeSpecified)
            {
                if (priceRange == null) return false;
                if (!criteria.PriceRange.Overlaps(priceRange)) return false;
            }

            return true;
        }

        public static bool ValidateReviewDistribution(SearchCriteria criteria, ScoreDistribution scoreDistribution)
        {
            
            // Test each of the review percentage criteria            
            if (scoreDistribution != null)
            {
                if (!criteria.ScoreDistribution.OneStar.Contains(scoreDistribution.OneStar))
                { return false; }

                if (!criteria.ScoreDistribution.TwoStar.Contains(scoreDistribution.TwoStar))
                { return false; }

                if (!criteria.ScoreDistribution.ThreeStar.Contains(scoreDistribution.ThreeStar))
                { return false; }

                if (!criteria.ScoreDistribution.FourStar.Contains(scoreDistribution.FourStar))
                { return false; }

                if (!criteria.ScoreDistribution.FiveStar.Contains(scoreDistribution.FiveStar))
                { return false; }
            }

            return true;
        }
    }
}
