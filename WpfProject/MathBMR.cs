using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace WpfProject
{
    public class MathBMR
    {
        public double Mean(double[] values) 
            => (values.Sum() / values.Length);

        public double Variance(double[] values)
        {
            double mean = Mean(values);
            double variance = values.Select(x => (x - mean) * (x - mean)).Sum() / values.Length;

            return variance;
        }

        public double stdDeviation(double[] values)
            => System.Math.Sqrt(Variance(values));
    }
}

