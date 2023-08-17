using ILOG.CPLEX;
using ILOG.OPL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOEM
{
    public class CplexSolver
    {
        /// <summary>
        /// 使用CPLEX求解当前构造下的非支配解
        /// </summary>
        /// <param name="batchesSize">批次数量</param>
        /// <param name="n">构造序号</param>
        /// <param name="solution">构造</param>
        /// <param name="processTime">批次m在赛汝j上的加工时间</param>
        /// <param name="b">总劳动时间的上界</param>
        /// <param name="duedate">交货期</param>
        /// <returns>当前构造下P最优解</returns>
        public List<Solution> ProduceParetoSolutions(int batchesSize, int n, Solution solution, string processTime, string workersInSeru, double b, double minError, double RatioBetweenObjFun)
        {
            string str = n.ToString();
            List<Solution> result = new List<Solution>();
            double currentObjVal = 0;
            while (true)
            {
                Solution currentSolution = new Solution
                {
                    TotalThroughPutTime = 0,
                    TotalLaborTime = 0
                };
                double currentMakespan = 0;
                int status = 127;
                try
                {
                    OplFactory.DebugMode = true;
                    OplFactory oplF = new OplFactory();
                    OplErrorHandler errHandler = oplF.CreateOplErrorHandler(Console.Out);
                    OplModelSource modelSource = oplF.CreateOplModelSourceFromString(CplexSolver.GetModelText(solution, processTime, workersInSeru, batchesSize, b, minError, RatioBetweenObjFun), str);
                    OplSettings settings = oplF.CreateOplSettings(errHandler);
                    OplModelDefinition def = oplF.CreateOplModelDefinition(modelSource, settings);
                    Cplex cplex = oplF.CreateCplex();
                    OplModel opl = oplF.CreateOplModel(def, cplex);
                    opl.Generate();
                    if (cplex.Solve())
                    {
                        //------先存储每次调用的目标函数值 没有存储对应解------
                        currentMakespan = opl.Cplex.ObjValue;
                        if (Math.Abs(opl.Cplex.ObjValue - currentObjVal) <= minError && b < 10000)
                        {
                            b = b - minError;
                            oplF.End();
                            continue;
                        }
                        else
                        {
                            currentObjVal = opl.Cplex.ObjValue;
                        }
                        Console.Out.WriteLine("OBJECTIVE: " + opl.Cplex.ObjValue);
                        //opl.PostProcess();
                        status = 0;
                        //opl.PrintSolution(Console.Out);
                        //File.CreateText(str + ".txt");
                        opl.saveResultsInText(str + ".txt");
                        try
                        {
                            currentSolution.Serus = solution.Serus;
                            StreamReader sr = new StreamReader(str + ".txt", Encoding.Default);
                            //用于读取数据的行
                            String line;
                            string totallaborhours;
                            string makespan;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.Contains("totalLaborHours"))
                                {
                                    totallaborhours = line;
                                    string[] a1 = totallaborhours.Split(' ');
                                    string[] a2 = a1[2].Split(';');
                                    double.TryParse(a2[0], out currentSolution.TotalLaborTime);
                                }
                                if (line.Contains("makespan"))
                                {
                                    makespan = line;
                                    string[] a1 = makespan.Split(' ');
                                    string[] a2 = a1[2].Split(';');
                                    double.TryParse(a2[0], out currentSolution.TotalThroughPutTime);
                                }
                            }
                            sr.Close();
                        }
                        catch (System.Exception)
                        {
                            throw;
                        }
                        result.Add(currentSolution);
                        b = currentSolution.TotalLaborTime;
                        if (b == 0)
                        {
                            break;
                        }
                        //清空.txt方便下次使用
                        System.IO.File.WriteAllText(str + ".txt", string.Empty);
                        //cplex.WriteOrder("a.txt");
                        //------注解------
                        //cplex.WriteAnnotations("a.txt");
                        //------当前模型所有参数------
                        cplex.WriteSolutions("a.txt");
                        //------Cplex当前版本------
                        //cplex.WriteParam("a.txt");
                    }
                    else
                    {
                        Console.Out.WriteLine("No solution!");
                        b = 10000000;
                        status = 1;
                        oplF.End();
                        break;
                    }
                    oplF.End();
                }
                catch (ILOG.OPL.OplException ex)
                {
                    Console.WriteLine(ex.Message);
                    status = 2;
                }
                catch (ILOG.Concert.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    status = 3;
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    status = 4;
                }
                Environment.ExitCode = status;
                //Console.WriteLine("--Press <Enter> to exit--");
                //Console.ReadLine();
            }
            return result;
        }
        /// <summary>
        /// 求解TLH单目标问题
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="processTime"></param>
        /// <param name="batchesSize"></param>
        /// <returns></returns>
        public double ObtainIdealTLH(Solution solution, string processTime, string workersInSeru, int batchesSize)
        {
            double i = 0;
            int status = 127;
            try
            {
                OplFactory.DebugMode = true;
                OplFactory oplF = new OplFactory();
                OplErrorHandler errHandler = oplF.CreateOplErrorHandler(Console.Out);
                OplModelSource modelSource = oplF.CreateOplModelSourceFromString(GetModelOfTLH(solution, processTime, workersInSeru, batchesSize), "TLH");
                OplSettings settings = oplF.CreateOplSettings(errHandler);
                OplModelDefinition def = oplF.CreateOplModelDefinition(modelSource, settings);
                Cplex cplex = oplF.CreateCplex();
                OplModel opl = oplF.CreateOplModel(def, cplex);
                opl.Generate();
                if (cplex.Solve())
                {
                    //先存储每次调用的目标函数值 没有存储对应解
                    i = opl.Cplex.ObjValue;
                    Console.Out.WriteLine("OBJECTIVE: " + opl.Cplex.ObjValue);
                    //opl.PostProcess();
                    //opl.PrintSolution(Console.Out);
                    status = 0;
                }
                else
                {
                    Console.Out.WriteLine("No solution!");
                    status = 1;
                }

                oplF.End();
            }
            catch (ILOG.OPL.OplException ex)
            {
                Console.WriteLine(ex.Message);
                status = 2;
            }
            catch (ILOG.Concert.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 3;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 4;
            }

            Environment.ExitCode = status;
            return i;
        }
        /// <summary>
        /// 求解Makspan单目标问题
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="processTime"></param>
        /// <param name="batchesSize"></param>
        /// <returns></returns>
        public double ObtainIdealMakespan(Solution solution, string processTime, int batchesSize)
        {
            double i = 0;
            int status = 127;
            try
            {
                OplFactory.DebugMode = true;
                OplFactory oplF = new OplFactory();
                OplErrorHandler errHandler = oplF.CreateOplErrorHandler(Console.Out);
                OplModelSource modelSource = oplF.CreateOplModelSourceFromString(GetModelOfMakespan(solution, processTime, batchesSize), " Makespan");
                OplSettings settings = oplF.CreateOplSettings(errHandler);
                OplModelDefinition def = oplF.CreateOplModelDefinition(modelSource, settings);
                Cplex cplex = oplF.CreateCplex();
                OplModel opl = oplF.CreateOplModel(def, cplex);
                opl.Generate();
                if (cplex.Solve())
                {
                    //先存储每次调用的目标函数值 没有存储对应解
                    i = opl.Cplex.ObjValue;
                    Console.Out.WriteLine("OBJECTIVE: " + opl.Cplex.ObjValue);
                    //opl.PostProcess();
                    //opl.PrintSolution(Console.Out);
                    status = 0;
                }
                else
                {
                    Console.Out.WriteLine("No solution!");
                    status = 1;
                }

                oplF.End();
            }
            catch (ILOG.OPL.OplException ex)
            {
                Console.WriteLine(ex.Message);
                status = 2;
            }
            catch (ILOG.Concert.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 3;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 4;
            }

            Environment.ExitCode = status;
            return i;
        }
        /// <summary>
        /// 传参数，形成模型文本
        /// </summary>
        /// <param name="solution">含有构造信息的和调度信息的解</param>
        /// <param name="processTime">加工时间</param>
        /// <param name="batchesSize">批次数</param>
        /// <returns> 模型文本</returns>
        public static String GetModelText(Solution solution, string processTime, string workersInSeru, int batchesSize, double b0, double minError, double RatioBetweenObjFun)
        {
            int.TryParse(Math.Log10(1 / minError).ToString(), out int posi);
            double b1 = Math.Round(b0, posi);
            if (b1 > b0)
            {
                b1 = b1 - minError;
            }
            string b = (b1 - minError).ToString();
            string Ratio = RatioBetweenObjFun.ToString();

            String model = "";
            //自动修改机器数
            model += "int nMachines =" + solution.Serus.Count + ";";
            //自动修改批次数
            model += "int nBatches =" + batchesSize + ";";
            model += "range Batches =1..nBatches;";
            model += "range Machines =1..nMachines;";

            //自动修改当前数据
            String stringProcessTime = "float processTime[Machines][Batches]=" + processTime + ";";
            model += stringProcessTime;
            String stringWorkersInSeru = "float workersInSeru[Machines]=" + workersInSeru + ";";
            model += stringWorkersInSeru;

            model += "dvar float+ startTime[Machines][Batches];";
            model += "dvar boolean allocation[Batches][Machines][Batches];";
            model += "dvar float+ makespan;";
            model += "dvar float+  totalLaborHours;";

            model += "minimize makespan + " + Ratio + " *totalLaborHours;";
            //model += "minimize totalLaborHours + " + Ratio + " *makespan;";
            model += "subject to";
            model += "{";
            //一个批次指派给唯一一个seru
            model += "  forall (i in Batches)";
            model += "    sum(m in Machines,o in Batches) allocation[i][m][o]==1;";
            //每个seru一个加工最多一个产品
            model += "  forall (m in Machines)";
            model += "    forall (o in Batches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=1;";
            //有序生产
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=sum (i in Batches) allocation[i][m][o-1];";
            //makespan计算公式
            model += "  forall (m in Machines)";
            model += "    makespan >= startTime[m][nBatches]+sum(i in Batches)allocation[i][m][nBatches]* processTime[m][i];";
            //总劳动时间计算公式
            model += "  totalLaborHours == " +
                "sum (m in Machines)" +
                " (sum (i in Batches)" +
                "   (sum (o in Batches) " +
                "      allocation[i][m][o]*processTime[m][i]*workersInSeru[m]" +
                "    )" +
                " );";
            //总劳动时间计算
            //model += " makespan <=" + b + ";";
            model += " totalLaborHours  <= " + b + ";";
            //初始时间均为0
            model += "  forall (m in Machines)";
            model += "    startTime[m][1]==0;";
            //可能出现等待
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      startTime[m][o]>=startTime[m][o-1]+sum(i in Batches) allocation[i][m][o-1]*processTime[m][i];";
            model += "}";
            return model;
        }
        /// <summary>
        /// 形成最小化总劳动时间的单目标问题Cplex模型
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="processTime"></param>
        /// <param name="batchesSize"></param>
        /// <returns></returns>
        public static String GetModelOfTLH(Solution solution, string processTime, string workersInSeru, int batchesSize)
        {
            String model = "";
            //自动修改机器数
            String stringMachines = "int nMachines =" + solution.Serus.Count + ";";
            model += stringMachines;

            //自动修改批次数
            String stringBatches = "int nBatches =" + batchesSize + ";";
            model += stringBatches;
            model += "range Machines =1..nMachines;";
            model += "range Batches =1..nBatches;";

            //自动修改processTime
            String stringProcessTime = "float processTime[Machines][Batches]=" + processTime + ";";
            model += stringProcessTime;
            String stringWorkersInSeru = "float workersInSeru[Machines]=" + workersInSeru + ";";
            model += stringWorkersInSeru;

            model += "dvar float+ startTime[Machines][Batches];";
            model += "dvar boolean allocation[Batches][Machines][Batches];";
            model += "dvar float+ totalLaborHours;";

            model += "minimize  totalLaborHours;";
            model += "subject to";
            model += "{";
            //一个批次指派给唯一一个seru
            model += "  forall (i in Batches)";
            model += "    sum(m in Machines,o in Batches) allocation[i][m][o]==1;";
            //每个seru一个加工最多一个产品
            model += "  forall (m in Machines)";
            model += "    forall (o in Batches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=1;";
            //有序生产
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=sum (i in Batches) allocation[i][m][o-1];";
            //总劳动时间的计算公式
            model += "  totalLaborHours == " +
                "sum (m in Machines)" +
                " (sum (i in Batches)" +
                "   (sum (o in Batches) " +
                "      allocation[i][m][o]*processTime[m][i]*workersInSeru[m]" +
                "    )" +
                " );";
            //初始时间均为0
            model += "  forall (m in Machines)";
            model += "    startTime[m][1]==0;";
            //可能出现等待
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      startTime[m][o]>=startTime[m][o-1]+sum(i in Batches) allocation[i][m][o-1]*processTime[m][i];";
            model += "}";
            return model;
        }
        /// <summary>
        /// 形成最小化最大完工时间的单目标问题Cplex模型
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="processTime"></param>
        /// <param name="batchesSize"></param>
        /// <returns></returns>
        public static String GetModelOfMakespan(Solution solution, string processTime, int batchesSize)
        {
            String model = "";
            //自动修改机器数
            String stringMachines = "int nMachines =" + solution.Serus.Count + ";";
            model += stringMachines;

            //自动修改批次数
            String stringBatches = "int nBatches =" + batchesSize + ";";
            model += stringBatches;
            model += "range Machines =1..nMachines;";
            model += "range Batches =1..nBatches;";

            //自动修改processTime
            String stringProcessTime = "float processTime[Machines][Batches]=" + processTime + ";";
            model += stringProcessTime;

            model += "dvar float+ startTime[Machines][Batches];";
            model += "dvar boolean allocation[Batches][Machines][Batches];";
            model += "dvar float+ makespan;";

            model += "minimize makespan;";
            model += "subject to";
            model += "{";
            model += "  forall (i in Batches)";
            model += "    sum(m in Machines,o in Batches) allocation[i][m][o]==1;";
            model += "  forall (m in Machines)";
            model += "    forall (o in Batches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=1;";
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      sum (i in Batches) allocation[i][m][o]<=sum (i in Batches) allocation[i][m][o-1];";
            model += "  forall (m in Machines)";
            model += "    makespan >=startTime[m][nBatches]+sum(i in Batches)allocation[i][m][nBatches]*processTime[m][i];";

            model += "  forall (m in Machines)";
            model += "    startTime[m][1]==0;";
            model += "  forall (m in Machines)";
            model += "    forall (o in 2..nBatches)";
            model += "      startTime[m][o]>=startTime[m][o-1]+sum(i in Batches) allocation[i][m][o-1]*processTime[m][i];";
            model += "}";
            return model;
        }
    }
}
