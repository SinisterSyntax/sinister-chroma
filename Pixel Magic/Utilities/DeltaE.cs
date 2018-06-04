using Colourful;
using Colourful.Difference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixel_Magic.Utilities
{
    public static class DeltaE
    {

        public static CIEDE2000ColorDifference CIE2000 = new CIEDE2000ColorDifference();
        public static CIE94ColorDifference CIE1994 = new CIE94ColorDifference();
        public static CIE76ColorDifference CIE1976 = new CIE76ColorDifference();
        public static Colorspace _COLORSPACE = Colorspace.CIE2000;


        public enum Colorspace
        {
            CIE2000 = 1,
            CIE1994 = 2,
            CIE1976 = 3
        }

        public static double StdDev(this IEnumerable<int> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return (int)ret;
        }


        static public double Distance(LabColor x, LabColor y)
        {
            switch (_COLORSPACE)
            {
                case Colorspace.CIE2000:
                    return CIE2000.ComputeDifference(x, y);

                case Colorspace.CIE1994:
                    return CIE1994.ComputeDifference(x, y);

                case Colorspace.CIE1976:
                    return CIE1976.ComputeDifference(x, y);
                default:
                    return CIE1976.ComputeDifference(x, y);
            }
        }

        static public double DistanceCIE1976(LabColor x, LabColor y)
        {
            return CIE1976.ComputeDifference(x, y);
        }


    }
}
