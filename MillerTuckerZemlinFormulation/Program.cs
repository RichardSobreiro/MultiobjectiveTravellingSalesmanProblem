using ILOG.Concert;
using ILOG.CPLEX;
using System.Diagnostics;
using System.IO;

namespace MillerTuckerZemlinFormulation
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
                        IIntVar[] u = cplex.IntVarArray(data.n, 0, (data.n - 1));
                        cplex.Add(u);

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

                        for (int i = 0; i < data.n; i++)
                        {
                            for (int j = 0; j < data.n; j++)
                            {
                                if (i >= 1 && i != j && j < data.n)
                                {
                                    cplex.AddLe(
                                            cplex.Sum(cplex.Diff(u[i], u[j]),
                                                cplex.Prod(data.n , x[i][j])),
                                        (data.n - 1));
                                }
                            }
                        }
                        #endregion

                        cplex.SetParam(Cplex.DoubleParam.WorkMem, 6000.0);
                        cplex.SetParam(Cplex.DoubleParam.EpGap, 0.1);

                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        if (cplex.Solve())
                        {
                            stopWatch.Stop();
                            double[][] sol_x = new double[data.n][];
                            for (int i = 0; i < data.n; i++)
                                sol_x[i] = cplex.GetValues(x[i]);
                            double[] sol_u = new double[data.n];
                            sol_u = cplex.GetValues(u);

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
                                    if(sol_x[i][j] == 1)
                                    {
                                        fileRouteResult.WriteLine($"From city {i} to city {j} - u[i] = {sol_u[i]} and u[j] = {sol_u[j]}");
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
    }
}
