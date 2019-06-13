using System;
using System.Linq;

namespace CrossCutting
{
    public static class CrossCutting
    {
        public static double[][] MinMaxScaleDoubleArrayArray(double[][] array, int m, int n)
        {
            double[][] result = AllocateArrayArray(m,n);

            double max = array.Cast<double>().Max();
            double min = array.Cast<double>().Min();
            double sum = array.Cast<double>().Sum();
            double mean = sum / (m * n);

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i][j] = (array[i][j] - mean) / (max - min);
                }
            }

            return result;
        }

        public static double[][] UnitVectorScaleDoubleArrayArray(double[][] array, int m, int n)
        {
            double[][] result = AllocateArrayArray(m, n);
            double[] euclideanNorms = new double[m];

            for (int i = 0; i < m; i++)
            {
                double linePowered2 = 0;
                for (int j = 0; j < n; j++)
                {
                    linePowered2 += array[i][j] * array[i][j];
                }
                euclideanNorms[i] = Math.Sqrt(linePowered2);
            }

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i][j] = array[i][j] / euclideanNorms[i];
                }
            }

            return result;
        }

        public static double[][] AllocateArrayArray(int m, int n)
        {
            double[][] result = new double[m][];
            for(int i = 0; i < m; i++)
            {
                result[i] = new double[n];
            }
            return result;
        }
    }
}
