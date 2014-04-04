using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    /// <summary>
    /// Represents the percentage range of values of each Amazon review star category (convenience class)
    /// </summary>
    public class ScoreDistribution
    {
        public DoubleRange OneStar { get { return _oneStar; } }
        public DoubleRange TwoStar { get { return _twoStar; } }
        public DoubleRange ThreeStar { get { return _threeStar; } }
        public DoubleRange FourStar { get { return _fourStar; } }
        public DoubleRange FiveStar { get { return _fiveStar; } }

        private DoubleRange _oneStar;
        private DoubleRange _twoStar;
        private DoubleRange _threeStar;
        private DoubleRange _fourStar;
        private DoubleRange _fiveStar;

        public ScoreDistribution(DoubleRange oneStar,
            DoubleRange twoStar,
            DoubleRange threeStar,
            DoubleRange fourStar,
            DoubleRange fiveStar)
        {
            _oneStar = oneStar;
            _twoStar = twoStar;
            _threeStar = threeStar;
            _fourStar = fourStar;
            _fiveStar = fiveStar;
        }

        public ScoreDistribution(double[] scores)
        {
            // Ensure that there are five categories being supplied.
            if (scores.Length != 5)
            {
                throw new ArgumentException("Score distribution must have five scores.");
            }

            _oneStar = new DoubleRange(scores[0],scores[0]);
            _twoStar = new DoubleRange(scores[1], scores[1]);
            _threeStar = new DoubleRange(scores[2], scores[2]);
            _fourStar = new DoubleRange(scores[3], scores[3]);
            _fiveStar = new DoubleRange(scores[4], scores[4]);

        }

        public override string ToString()
        {
            string results = string.Format("Five star: {0} %", _fiveStar.Low) + Environment.NewLine +
                string.Format("Four star: {0} %", _fourStar.Low) + Environment.NewLine +
                string.Format("Three star: {0} %", _threeStar.Low) + Environment.NewLine +
                string.Format("Two star: {0} %", _twoStar.Low) + Environment.NewLine +
                string.Format("One star: {0} %", _oneStar.Low) + Environment.NewLine;

            return results;
        }
    }
}
