using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DantzigFulkersonJohnsonFormulation
{
    public class Program
    {
        internal class Data
        {
            internal int n;
            internal double[][] timeNormalized;
            internal double[][] distanceNormalized;
            internal double[][] time;
            internal double[][] distance;

            internal Data(string filename)
            {
                InputDataReader reader = new InputDataReader(filename);

                time = reader.ReadDoubleArrayArray();
                distance = reader.ReadDoubleArrayArray();
                n = time.Length;

                timeNormalized = CrossCutting.CrossCutting.UnitVectorScaleDoubleArrayArray(time, n, n);
                distanceNormalized = CrossCutting.CrossCutting.UnitVectorScaleDoubleArrayArray(distance, n, n);

            }
        }

        static void Main()
        {
            try
            {
                string resultFilename = "./ResultFiles/MTSP_MTZ.csv";
                string filename = "./DataFiles/Data.dat";
                Data data = new Data(filename);

                double timeFactor = 0;
                double distanceFactor = 1;
                double step = 0.1;
                int routeCounter = 0;

                File.WriteAllText(resultFilename, "");

                do
                {
                    using (Cplex cplex = new Cplex())
                    {
                        #region [Decision Variables]
                        IIntVar[][] x = new IIntVar[data.n][];
                        IIntVar Q = cplex.IntVar(1, 250, "Q");

                        for (int i = 0; i < data.n; i++)
                        {
                            x[i] = cplex.BoolVarArray(data.n);
                            cplex.Add(x[i]);
                        }
                        #endregion

                        #region [Objective Function]
                        INumExpr obj = cplex.NumExpr();
                        for (int i = 0; i < data.n; i++)
                        {
                            for (int j = 0; j < data.n; j++)
                            {
                                if (i != j)
                                {
                                    obj = cplex.Sum(obj,
                                        cplex.Prod(
                                            (
                                                (timeFactor * data.timeNormalized[i][j]) +
                                                (distanceFactor * data.distanceNormalized[i][j])
                                            ), x[i][j]));
                                }
                            }
                        }
                        cplex.AddMinimize(obj);
                        #endregion

                        #region [Restrictions]
                        for (int j = 0; j < data.n; j++)
                        {
                            ILinearNumExpr sumj = cplex.LinearNumExpr();
                            for (int i = 0; i < data.n; i++)
                            {
                                if (i != j)
                                {
                                    sumj.AddTerm(1, x[i][j]);
                                }
                            }
                            cplex.AddEq(sumj, 1);
                        }

                        for (int i = 0; i < data.n; i++)
                        {
                            ILinearNumExpr sumi = cplex.LinearNumExpr();
                            for (int j = 0; j < data.n; j++)
                            {
                                if (i != j)
                                {
                                    sumi.AddTerm(1, x[i][j]);
                                }
                            }
                            cplex.AddEq(sumi, 1);
                        }
                        #endregion

                        cplex.SetParam(Cplex.DoubleParam.WorkMem, 4000.0);
                        cplex.SetParam(Cplex.Param.MIP.Strategy.File, 2);
                        cplex.SetParam(Cplex.DoubleParam.EpGap, 0.1);
                        cplex.SetParam(Cplex.BooleanParam.MemoryEmphasis, true);
                        cplex.SetParam(Cplex.IntParam.VarSel, 4);

                        SOLVE:

                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        if (cplex.Solve())
                        {
                            stopWatch.Stop();
                            double[][] sol_x = new double[data.n][];
                            for (int i = 0; i < data.n; i++)
                                sol_x[i] = cplex.GetValues(x[i]);
                            //int shortestLenght = ShortestCicleInSelectedPaths(sol_x);
                            int[] tour = FindSubTour(sol_x);
                            if (tour.Length < data.n)
                            {
                                ILinearNumExpr sumx = cplex.LinearNumExpr();
                                for (int i = 0; i < tour.Length; i++)
                                {
                                    for (int j = 0; j < tour.Length; j++)
                                    {
                                        sumx.AddTerm(1, x[tour[i]][tour[j]]);
                                    }
                                }
                                cplex.AddLazyConstraint(cplex.AddLe(cplex.Diff(sumx, tour.Length), -1));
                                goto SOLVE;
                            }

                            double timeTotal = 0;
                            double distanceTotal = 0;
                            for (int i = 0; i < data.n; i++)
                            {
                                for (int j = 0; j < data.n; j++)
                                {
                                    timeTotal += data.time[i][j] * sol_x[i][j];
                                    distanceTotal += data.distance[i][j] * sol_x[i][j];
                                }
                            }

                            StreamWriter file = new StreamWriter(resultFilename, true);
                            file.WriteLine($"{timeFactor},{distanceFactor},{stopWatch.Elapsed.TotalSeconds},{cplex.ObjValue},{timeTotal},{distanceTotal}");
                            file.Close();

                            StreamWriter fileRouteResult = new StreamWriter($"./ResultFiles/Route-{routeCounter}.txt");
                            for (int i = 0; i < data.n; i++)
                            {
                                for (int j = 0; j < data.n; j++)
                                {
                                    if (sol_x[i][j] == 1)
                                    {
                                        fileRouteResult.WriteLine($"From city {i} to city {j}");
                                    }
                                }
                            }
                            fileRouteResult.Close();
                        }
                        cplex.End();
                    }

                    timeFactor += step;
                    distanceFactor -= step;
                    routeCounter++;
                } while (timeFactor <= 1);
            }
            catch (ILOG.Concert.Exception ex)
            {
                StreamWriter errorfile = new StreamWriter("./ErrorLog.txt");
                errorfile.WriteLine("Exception Kind: ILOG.Concert.Exception (Concert Error)");
                errorfile.WriteLine("Message: " + ex.Message);
                errorfile.WriteLine("StackTrace: " + ex.StackTrace);
                errorfile.Close();
            }
            catch (InputDataReader.InputDataReaderException ex)
            {
                StreamWriter errorfile = new StreamWriter("./ErrorLog.txt");
                errorfile.WriteLine("Exception Kind: InputDataReader.InputDataReaderException (Data Error)");
                errorfile.WriteLine("Message: " + ex.Message);
                errorfile.WriteLine("StackTrace: " + ex.StackTrace);
                errorfile.Close();
            }
            catch (System.IO.IOException ex)
            {
                StreamWriter errorfile = new StreamWriter("./ErrorLog.txt");
                errorfile.WriteLine("Exception Kind: System.IO.IOException (IO Error)");
                errorfile.WriteLine("Message: " + ex.Message);
                errorfile.WriteLine("StackTrace: " + ex.StackTrace);
                errorfile.Close();
            }
        }

        static bool HasTourElimination(Cplex cplex, Data data, IIntVar[][] x)
        {
            double[][] sol_x = new double[data.n][];
            for (int i = 0; i < data.n; i++)
                sol_x[i] = cplex.GetValues(x[i]);
            //int shortestLenght = ShortestCicleInSelectedPaths(sol_x);
            int[] tour = FindSubTour(sol_x);
            if (tour.Length < data.n)
            {
                ILinearNumExpr sumx = cplex.LinearNumExpr();
                for (int i = 0; i < tour.Length; i++)
                {
                    for (int j = i+1; j < tour.Length; j++)
                    {
                        sumx.AddTerm(1, x[tour[i]][tour[j]]);
                    }
                }
                cplex.AddLazyConstraint(cplex.AddLe(cplex.Diff(sumx, (tour.Length+ 1)), 0));
                return true;
            }
            else
            {
                return false;
            }
        }

        static int ShortestCicleInSelectedPaths(double[][] sol_x)
        {
            int n = sol_x.Length;
            bool[] visited = new bool[n];

            List<int> lenghts = new List<int>();
            int lenght = 0;

            int index = 0;
            int nextIndex = 0;
            while (index < n)
            {
                nextIndex = index;
                while (true)
                {
                    if (visited[nextIndex] == true)
                        break;

                    visited[nextIndex] = true;

                    for (int j = 0; j < n; j++)
                    {
                        if (sol_x[nextIndex][j] > 0.5)
                        {
                            nextIndex = j;
                            lenght++;
                            break;
                        }
                    }
                }
                if(lenght > 0)
                {
                    lenghts.Add(lenght);
                }
                lenght = 0;
                index++;
            }

            return lenghts.Min();
        }

        static int[] FindSubTour(double[][] sol)
        {
            int n = sol.GetLength(0);
            bool[] seen = new bool[n];
            int[] tour = new int[n];
            int bestind, bestlen;
            int i, node, len, start;

            for (i = 0; i < n; i++)
                seen[i] = false;

            start = 0;
            bestlen = n + 1;
            bestind = -1;
            node = 0;
            while (start < n)
            {
                for (node = 0; node < n; node++)
                    if (!seen[node])
                        break;
                if (node == n)
                    break;
                for (len = 0; len < n; len++)
                {
                    tour[start + len] = node;
                    seen[node] = true;
                    for (i = 0; i < n; i++)
                    {
                        if (sol[node][i] > 0.5 && !seen[i])
                        {
                            node = i;
                            break;
                        }
                    }
                    if (i == n)
                    {
                        len++;
                        if (len < bestlen)
                        {
                            bestlen = len;
                            bestind = start;
                        }
                        start += len;
                        break;
                    }
                }
            }

            for (i = 0; i < bestlen; i++)
                tour[i] = tour[bestind + i];
            System.Array.Resize(ref tour, bestlen);

            return tour;
        }
    }
}
