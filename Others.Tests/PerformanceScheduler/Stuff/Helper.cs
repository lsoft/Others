using System;
using System.Collections.Generic;
using System.Linq;

namespace Others.Tests.PerformanceScheduler.Stuff
{
    public static class Helper
    {
        public static bool NotInRange(
            this double value,
            double left,
            double right
            )
        {
            return
                !InRange(value, left, right);
        }

        public static bool InRange(
            this double value,
            double left,
            double right
            )
        {
            return
                left < value && value <= right;
        }

        public static double Mean(
            this IList<Point> array
            )
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Count == 0)
            {
                return
                    0.0;
            }

            var mean = array.Average(j => j.Diff);

            return
                mean;
        }

        public static double StandardDeviation(
            this IList<Point> array
            )
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Count == 0)
            {
                return
                    0.0;
            }

            var darray = array.ToList().ConvertAll(j => (double)j.Diff);

            double average = darray.Average();
            double sumOfSquaresOfDifferences = darray.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / darray.Count);

            return
                sd;
        }


    }
}