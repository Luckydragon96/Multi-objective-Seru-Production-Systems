using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MOCCSNMS_NSGA2
{
    public partial class Form1 : Form
    {
        //-----工人数、批次数
        int numOfWorkers;
        int numOfBatches;
        //----赛汝生产的相关参数
        int maxNumOfMultipleTask = 10;                                                                                      //多能工最大值，Seru里tasks大于这个值加工时间就要延长。
        int numOfPopular = 60;                                                                                                      //种群大小
        int maxItera = 100;                                                                                                              //最大迭代次数
        double probabilityOfMutation = 1;
        double probabilityOfCrossover = 1; 
        double taskTime = 1.8;
        Random random = new Random();

        //----赛汝构造的主种群/赛汝调度的主种群
        List<SeruFormation> masterPopulation_SF = new List<SeruFormation>();
        List<SeruScheduling> masterPopulation_SS = new List<SeruScheduling>();

        //---赛汝构造的从种群/赛汝调度的从种群 
        List<SeruFormation> P_mCmax_SF = new List<SeruFormation>();
        List<SeruFormation> P_mTLH_SF = new List<SeruFormation>();
        List<SeruFormation> P_aveND_SF = new List<SeruFormation>();
        List<SeruScheduling> P_mCmax_SS = new List<SeruScheduling>();
        List<SeruScheduling> P_mTLH_SS = new List<SeruScheduling>();
        List<SeruScheduling> P_aveND_SS = new List<SeruScheduling>();


        //---协助进化的三个构造个体
        SeruFormation mCmax_SF = new SeruFormation();
        SeruFormation mTLH_SF = new SeruFormation();
        SeruFormation aveND_SF = new SeruFormation();

        //---协助进化的三个调度个体
        SeruScheduling mCmax_SS = new SeruScheduling();
        SeruScheduling mTLH_SS = new SeruScheduling();
        SeruScheduling aveND_SS = new SeruScheduling();

        //---批次与产品类型关系的数据/工人与产品类型的熟练程度的数据/多能工系数数据
        DataTable tableBatchToProductType = new DataTable();
        DataTable tableWorkerToProductType = new DataTable();
        DataTable tableWorkerToMultipleTask = new DataTable();

        //---用于锁定代码块
        private static readonly Object Lock = new Object();

        public Form1()
        {
            InitializeComponent();
            readData();
        }
        /// <summary>
        /// 程序入口1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //List<int> workers = new List<int> { 8, 10, 15, 20 };
            //List<int> batches = new List<int> { 10, 15, 20, 25, 30 };

            //List<int> workers = new List<int> { 4, 5,6 };
            //List<int> batches = new List<int> { 5, 6, 7, 8, 9 };

            List<int> workers = new List<int> { 5 };
            List<int> batches = new List<int> { 8 };

            int Row = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                numOfWorkers = workers[i];
                for (int j = 0; j < batches.Count; j++)
                {
                    Row++;
                    numOfBatches = batches[j];
                    MOCCMS_NSGA_II();
                }
            }
        }
        /// <summary>
        /// MOCCMS_NSGA_II
        /// </summary>
        public void MOCCMS_NSGA_II() 
        {
            string outPutName = @"D:\Result_MOCC\MOCCMS_NSGA_II_small\MOCCMS_NSGA2_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";
            P_mCmax_SF = new List<SeruFormation>();
            P_mTLH_SF = new List<SeruFormation>();
            P_aveND_SF = new List<SeruFormation>();
            P_mCmax_SS = new List<SeruScheduling>();
            P_mTLH_SS = new List<SeruScheduling>();
            P_aveND_SS = new List<SeruScheduling>();

            //------生成初始的赛汝构造的从种群和赛汝调度的从种群------
            for (int i = 0; i < numOfPopular; i++)
            {
                SeruFormation formation1 = new SeruFormation();
                formation1.formationCode = initialFormationCode();                                                                       //初始化构造编码
                formation1.serusSet = formation1.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_mCmax_SF.Add(formation1);

                SeruFormation formation2 = new SeruFormation();
                formation2.formationCode = initialFormationCode();                                                                       //初始化构造编码
                formation2.serusSet = formation2.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_mTLH_SF.Add(formation2);

                SeruFormation formation3 = new SeruFormation();
                formation3.formationCode = initialFormationCode();                                                                      //初始化构造编码
                formation3.serusSet = formation2.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_aveND_SF.Add(formation3);

                SeruScheduling schedule1 = new SeruScheduling();
                schedule1.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule1.BatchesAssignment = schedule1.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_mCmax_SS.Add(schedule1);

                SeruScheduling schedule2 = new SeruScheduling();
                schedule2.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule2.BatchesAssignment = schedule2.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_mTLH_SS.Add(schedule2);

                SeruScheduling schedule3 = new SeruScheduling();
                schedule3.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule3.BatchesAssignment = schedule3.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_aveND_SS.Add(schedule3);
            }

            //------初始化协同进化个体------
            mCmax_SF = P_mCmax_SF[0];
            mTLH_SF = P_mTLH_SF[0];
            aveND_SF = P_aveND_SF[0];

            mCmax_SS = P_mCmax_SS[0];
            mTLH_SS = P_mTLH_SS[0];
            aveND_SS = P_aveND_SS[0];

            int iterator = 0;
            //DateTime startTime = DateTime.Now;

            //------初始化赛汝构造或者赛汝调度的主种群------
            InitializeMasterPopulation_SS(mTLH_SF, P_mTLH_SS);
            InitializeMasterPopulation_SF(mTLH_SS, P_mTLH_SF);

            //------开始进化------
            DateTime time1 = DateTime.Now;
            for (int m = 0; m < maxItera; m++)
            {
                Console.WriteLine($"===================第{iterator + 1}次迭代================");
                Result result = ProcessCooperativeEvo(iterator, mCmax_SF, mTLH_SF, aveND_SF, mCmax_SS, mTLH_SS, aveND_SS, P_mCmax_SF, P_mTLH_SF, P_aveND_SF, P_mCmax_SS, P_mTLH_SS, P_aveND_SS);
                if (iterator % 2 == 0)//先进化构造种群
                {
                    P_mCmax_SF = result.P_mCmax_SF;
                    P_mTLH_SF = result.P_mTLH_SF;
                    P_aveND_SF = result.P_aveND_SF;
                    mCmax_SF = result.mCmax_SF;
                    mTLH_SF = result.mTLH_SF;
                    aveND_SF = result.aveND_SF;
                    masterPopulation_SF = result.masterPopulation_SF;
                    //Console.WriteLine($"SF------numofSolutions : {masterPopulation_SF.Count}");
                    //Console.WriteLine($"SF------TTPT is : {aveND_SF.throughPutTime}; TLH is {aveND_SF.labourTime}");
                }
                else
                {
                    P_mCmax_SS = result.P_mCmax_SS;
                    P_mTLH_SS = result.P_mTLH_SS;
                    P_aveND_SS = result.P_aveND_SS;
                    mCmax_SS = result.mCmax_SS;
                    mTLH_SS = result.mTLH_SS;
                    aveND_SS = result.aveND_SS;
                    masterPopulation_SS = result.masterPopulation_SS;
                    //Console.WriteLine($"SS------numofSolutions : {masterPopulation_SS.Count}");
                    //Console.WriteLine($"SS------TTPT is : {aveND_SS.throughPutTime}; TLH is {aveND_SS.labourTime}");
                }
                iterator++;
            }
            //------赛汝构造和赛汝调度组合------
            List<SeruFormation> masterPopulation_SFClone = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
            for (int i = 0; i < masterPopulation_SFClone.Count; i++)
            {
                SeruFormation currentFormation = masterPopulation_SFClone[i];
                for (int j = 0; j < masterPopulation_SS.Count; j++)
                {
                    SeruScheduling currentSchedule = DeepCopyByBin<SeruScheduling>(masterPopulation_SS[j]);
                    caculateFitness(currentFormation, currentSchedule);
                }
                masterPopulation_SF.Add(DeepCopyByBin<SeruFormation>(currentFormation));
            }
            masterPopulation_SF = produceNewParentSchesulingByDominatedSort_SF(masterPopulation_SF);
            masterPopulation_SF = getParetoOfPopulation_SF(masterPopulation_SF);
            List<SeruFormation> temp1 = new List<SeruFormation>();
            for (int i = 0; i < masterPopulation_SF.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruFormation tempSolution = DeepCopyByBin<SeruFormation>(masterPopulation_SF[i]);
                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(temp1, tempSolution))
                {
                    temp1.Add(tempSolution);
                }
            }
            masterPopulation_SF = DeepCopyByBin<List<SeruFormation>>(temp1);
            //------检查支配关系------
            List<SeruScheduling> tem = new List<SeruScheduling>();
            for (int i = 0; i < masterPopulation_SS.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < masterPopulation_SF.Count; j++)
                {
                    if (((masterPopulation_SS[i]).throughPutTime >= (masterPopulation_SF[j]).throughPutTime) && (masterPopulation_SS[i]).labourTime >= (masterPopulation_SF[j]).labourTime)
                        flag = false;
                }
                if (flag == true)
                    tem.Add(masterPopulation_SS[i]);
            }
            masterPopulation_SS = tem;
            List<SeruFormation> tem1 = new List<SeruFormation>();
            for (int i = 0; i < masterPopulation_SF.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < masterPopulation_SS.Count; j++)
                {
                    if (((masterPopulation_SF[i]).throughPutTime >= (masterPopulation_SS[j]).throughPutTime) && ((masterPopulation_SF[i]).labourTime >= (masterPopulation_SS[j]).labourTime))
                        flag = false;
                }
                if (flag == true)
                    tem1.Add(masterPopulation_SF[i]);
            }
            masterPopulation_SF = tem1;
            //------打印实验结果------
            Console.WriteLine("-----------------------------------------------------------------");
            for (int i = 0; i < masterPopulation_SF.Count; i++)
                Console.WriteLine((masterPopulation_SF[i]).throughPutTime + "   " + (masterPopulation_SF[i]).labourTime);
            for (int i = 0; i < masterPopulation_SS.Count; i++)
                Console.WriteLine((masterPopulation_SS[i]).throughPutTime + "   " + (masterPopulation_SS[i]).labourTime);
            Console.WriteLine("-------------------主程序运行结束--------------------------");
            int numOfSolutions = masterPopulation_SS.Count + masterPopulation_SF.Count;
            DateTime time2 = DateTime.Now;
            Console.WriteLine(time2 - time1);
            OuT ouT = new OuT();
            ouT.OutPut(masterPopulation_SF, masterPopulation_SS, numOfSolutions, outPutName, (time2 - time1).TotalSeconds);
            Console.WriteLine("-------------------数据输出结束--------------------------");
        }
        /// <summary>
        /// 执行并行协同进化算法
        /// </summary>
        /// <param name="iterator"></param>
        /// <param name="mCmax_SF"></param>
        /// <param name="mTLH_SF"></param>
        /// <param name="aveND_SF"></param>
        /// <param name="mCmax_SS"></param>
        /// <param name="mTLH_SS"></param>
        /// <param name="aveND_SS"></param>
        /// <returns></returns>
        public Result ProcessCooperativeEvo(int iterator, SeruFormation mCmax_SF, SeruFormation mTLH_SF, SeruFormation aveND_SF, SeruScheduling mCmax_SS, SeruScheduling mTLH_SS, SeruScheduling aveND_SS, List<SeruFormation> P_mCmax_SF, List<SeruFormation> P_mTLH_SF, List<SeruFormation> P_aveND_SF, List<SeruScheduling> P_mCmax_SS, List<SeruScheduling> P_mTLH_SS, List<SeruScheduling> P_aveND_SS ) 
        {
            Result result = new Result();
            if (iterator % 2 == 0)
            {
                SeruScheduling cloneMCmax_SS = DeepCopyByBin<SeruScheduling>(mCmax_SS);
                SeruScheduling cloneMTLH_SS = DeepCopyByBin<SeruScheduling>(mTLH_SS);
                SeruScheduling cloneAveND_SS = DeepCopyByBin<SeruScheduling>(aveND_SS);

                DateTime begin = DateTime.Now;

                //------赛汝构造的从种群并行进化产生子代种群与当前非支配解------
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func1 = getNextFormationGeneration;
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func2 = getNextFormationGeneration;
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func3 = getNextFormationGeneration;

                IAsyncResult asyncResult1 = func1.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMCmax_SS, P_mCmax_SF, "------Evolve P_mCmax_SF------", null, null);
                IAsyncResult asyncResult2 = func2.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMTLH_SS, P_mTLH_SF, "------Evolve P_mTLH_SF------", null, null);
                IAsyncResult asyncResult3 = func3.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneAveND_SS, P_aveND_SF, "------Evolve P_aveND_SF------", null, null);

                ResultsForMultiThreading result1 = func1.EndInvoke(asyncResult1);
                ResultsForMultiThreading result2 = func2.EndInvoke(asyncResult2);
                ResultsForMultiThreading result3 = func3.EndInvoke(asyncResult3);

                //ResultsForMultiThreading result1 = getNextFormationGeneration(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMCmax_SS, P_mCmax_SF, "------Evolve P_mCmax_SF------");
                //ResultsForMultiThreading result2 = getNextFormationGeneration(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMTLH_SS, P_mTLH_SF, "------Evolve P_mTLH_SF------");
                //ResultsForMultiThreading result3 = getNextFormationGeneration(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneAveND_SS, P_aveND_SF, "------Evolve P_aveND_SF------");


                //更新从种群
                result.P_mCmax_SF = result1.newFormations;
                result.P_mTLH_SF = result2.newFormations;
                result.P_aveND_SF = result3.newFormations;

                //------更新赛汝构造的主种群------
                List<SeruFormation> temp = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
                masterPopulation_SF = new List<SeruFormation>();
                temp.AddRange(result1.nonDominatedFormations);
                temp.AddRange(result2.nonDominatedFormations);
                temp.AddRange(result3.nonDominatedFormations);
                List<SeruFormation> temp1 = new List<SeruFormation>();
                for (int i = 0; i < temp.Count; i++)
                {
                    //新种群中的individual在父种群有，可以添加的情况
                    SeruFormation tempSolution = (SeruFormation)(temp[i]).Clone();
                    //新种群中的individual在父种群有，不可以添加的情况
                    if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(temp1, tempSolution))
                    {
                        temp1.Add(tempSolution);
                    }
                }
                //------非支配排序------
                for (int i = 0; i < temp1.Count; i++)
                {
                    SeruFormation iIndividual = temp1[i];
                    iIndividual.frontNumber = 0;                                                        //每次计算pareto解时，frontNumber重值为0
                    iIndividual.numOfDonimateIndividual = 0;                                  //每次计算pareto解时，重值为0
                    iIndividual.donimatedSet = new List<SeruFormation>();           //每次计算pareto解时，重值为空
                    iIndividual.distanceOfCrowd = 0;
                }
                for (int p = 0; p < temp1.Count; p++)
                {
                    SeruFormation pIndividual = temp1[p];
                    for (int q = 0; q < temp1.Count; q++)
                    {
                        SeruFormation qIndividual = temp1[q];
                        if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                        {
                            pIndividual.donimatedSet.Add(qIndividual);          //qIndividual加入到pIndividual的支配解中
                        }
                        else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                        {
                            pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                        }
                    }
                    if (pIndividual.numOfDonimateIndividual == 0)               //不被任何个体支配，加入到第1层front中。
                    {
                        pIndividual.frontNumber = 1;
                        masterPopulation_SF.Add(pIndividual);
                    }
                }
                //------更新协同进化个体------
                SeruFormation temSolution1 = new SeruFormation();
                SeruFormation temSolution2 = new SeruFormation();
                temSolution1.throughPutTime = double.MaxValue;
                temSolution2.labourTime = double.MaxValue;
                double temSumOfThroughPutTime = 0;
                double temSumOfLabourTime = 0;
                for (int i = 0; i < masterPopulation_SF.Count; i++)
                {
                    if ((masterPopulation_SF[i]).throughPutTime < temSolution1.throughPutTime)
                    {
                        temSolution1 = masterPopulation_SF[i];
                    }

                    if ((masterPopulation_SF[i]).labourTime < temSolution2.labourTime)
                    {
                        temSolution2 = masterPopulation_SF[i];
                    }

                    temSumOfThroughPutTime = temSumOfThroughPutTime + masterPopulation_SF[i].throughPutTime;
                    temSumOfLabourTime = temSumOfLabourTime + masterPopulation_SF[i].labourTime;
                }
                result.mCmax_SF = (SeruFormation)temSolution1.Clone();
                result.mTLH_SF = (SeruFormation)temSolution2.Clone();
                temSumOfThroughPutTime = temSumOfThroughPutTime / (masterPopulation_SF.Count);
                temSumOfLabourTime = temSumOfLabourTime / (masterPopulation_SF.Count);

                double temvalue = double.MaxValue;
                double currentValue = 0;

                for (int i = 0; i < masterPopulation_SF.Count; i++)
                {
                    currentValue = Math.Pow(((masterPopulation_SF[i]).throughPutTime - temSumOfThroughPutTime), 2) + Math.Pow(((masterPopulation_SF[i]).labourTime - temSumOfLabourTime), 2);
                    if (currentValue < temvalue)
                    {
                        result.aveND_SF = (SeruFormation)masterPopulation_SF[i].Clone();
                        temvalue = currentValue;
                    }
                }
                result.masterPopulation_SF = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
                DateTime end = DateTime.Now;
                TimeSpan t = end - begin;
                Console.WriteLine(t);
            }
            else 
            {
                SeruFormation cloneMCmax_SF = DeepCopyByBin<SeruFormation>(mCmax_SF);
                SeruFormation cloneMTLH_SF = DeepCopyByBin<SeruFormation>(mTLH_SF);
                SeruFormation cloneAveND_SF = DeepCopyByBin<SeruFormation>(aveND_SF);

                DateTime begin = DateTime.Now;

                //------赛汝调度的从种群并行进化产生子代种群与当前非支配解------
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func1 = getNextScheduleGeneration;
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func2 = getNextScheduleGeneration;
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func3 = getNextScheduleGeneration;

                IAsyncResult asyncResult1 = func1.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMCmax_SF, P_mCmax_SS, "------Evolve P_mCmax_SS------", null, null);
                IAsyncResult asyncResult2 = func2.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMTLH_SF, P_mTLH_SS, "------Evolve P_mTLH_SS------", null, null);
                IAsyncResult asyncResult3 = func3.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneAveND_SF, P_aveND_SS, "------Evolve P_aveND_SS------", null, null);

                ResultsForMultiThreading result1 = func1.EndInvoke(asyncResult1);
                ResultsForMultiThreading result2 = func2.EndInvoke(asyncResult2);
                ResultsForMultiThreading result3 = func3.EndInvoke(asyncResult3);

                //ResultsForMultiThreading result1 = getNextScheduleGeneration(masterPopulation_SS, mCmax_SF, P_mCmax_SS, "------Evolve P_mCmax_SS------");
                //ResultsForMultiThreading result2 = getNextScheduleGeneration(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), mTLH_SF, P_mTLH_SS, "------Evolve P_mTLH_SS------");
                //ResultsForMultiThreading result3 = getNextScheduleGeneration(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), aveND_SF, P_aveND_SS, "------Evolve P_aveND_SS------");

                //更新从种群
                result.P_mCmax_SS = DeepCopyByBin<List<SeruScheduling>>(result1.newSchedules);
                result.P_mTLH_SS = DeepCopyByBin<List<SeruScheduling>>(result2.newSchedules);
                result.P_aveND_SS = DeepCopyByBin<List<SeruScheduling>>(result3.newSchedules);

                //DateTime end = DateTime.Now;
                //TimeSpan t = end - begin;
                //Console.WriteLine(t);

                //------更新赛汝调度的主种群------
                List<SeruScheduling> temp = DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS);
                masterPopulation_SS = new List<SeruScheduling>();
                temp.AddRange(result1.nonDominatedSchedules);
                temp.AddRange(result2.nonDominatedSchedules);
                temp.AddRange(result3.nonDominatedSchedules);
                List<SeruScheduling> temp1 = new List<SeruScheduling>();
                for (int i = 0; i < temp.Count; i++)
                {
                    //新种群中的individual在父种群有，可以添加的情况
                    SeruScheduling tempSolution = (SeruScheduling)(temp[i]).Clone();
                    //新种群中的individual在父种群有，不可以添加的情况
                    if (!ifSolutionExistedInPopulationWithObjectiveValue(temp1, tempSolution))
                    {
                        temp1.Add(tempSolution);
                    }
                }
                //------非支配排序------
                for (int i = 0; i < temp1.Count; i++)
                {
                    SeruScheduling iIndividual = temp1[i];
                    iIndividual.frontNumber = 0;                                                        //每次计算pareto解时，frontNumber重值为0
                    iIndividual.numOfDonimateIndividual = 0;                                  //每次计算pareto解时，重值为0
                    iIndividual.donimatedSet = new List<SeruScheduling>();           //每次计算pareto解时，重值为空
                    iIndividual.distanceOfCrowd = 0;
                }
                for (int p = 0; p < temp1.Count; p++)
                {
                    SeruScheduling pIndividual = temp1[p];
                    for (int q = 0; q < temp1.Count; q++)
                    {
                        SeruScheduling qIndividual = temp1[q];
                        if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                        {
                            pIndividual.donimatedSet.Add(qIndividual);          //qIndividual加入到pIndividual的支配解中
                        }
                        else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                        {
                            pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                        }
                    }
                    if (pIndividual.numOfDonimateIndividual == 0)               //不被任何个体支配，加入到第1层front中。
                    {
                        pIndividual.frontNumber = 1;
                        masterPopulation_SS.Add(pIndividual);
                    }
                }

                //------更新协同进化个体------
                SeruScheduling temSolution1 = new SeruScheduling();
                SeruScheduling temSolution2 = new SeruScheduling();
                temSolution1.throughPutTime = double.MaxValue;
                temSolution2.labourTime = double.MaxValue;
                double temSumOfThroughPutTime = 0;
                double temSumOfLabourTime = 0;
                for (int i = 0; i < masterPopulation_SS.Count; i++)
                {
                    if ((masterPopulation_SS[i]).throughPutTime < temSolution1.throughPutTime)
                    {
                        temSolution1 = masterPopulation_SS[i];
                    }
                    if ((masterPopulation_SS[i]).labourTime < temSolution2.labourTime)
                    {
                        temSolution2 = masterPopulation_SS[i];
                    }
                    temSumOfThroughPutTime = temSumOfThroughPutTime + masterPopulation_SS[i].throughPutTime;
                    temSumOfLabourTime = temSumOfLabourTime + masterPopulation_SS[i].labourTime;
                }
                result.mCmax_SS = (SeruScheduling)temSolution1.Clone();
                result.mTLH_SS = (SeruScheduling)temSolution2.Clone();
                temSumOfThroughPutTime = temSumOfThroughPutTime / (masterPopulation_SS.Count);
                temSumOfLabourTime = temSumOfLabourTime / (masterPopulation_SS.Count);

                double temvalue = double.MaxValue;
                double currentValue = 0;

                for (int i = 0; i < masterPopulation_SS.Count; i++)
                {
                    currentValue = Math.Pow(((masterPopulation_SS[i]).throughPutTime - temSumOfThroughPutTime), 2) + Math.Pow(((masterPopulation_SS[i]).labourTime - temSumOfLabourTime), 2);
                    if (currentValue < temvalue)
                    {
                        result.aveND_SS = (SeruScheduling)masterPopulation_SS[i].Clone();
                        temvalue = currentValue;
                    }
                }
                result.masterPopulation_SS = DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS);
                DateTime end = DateTime.Now;
                TimeSpan t = end - begin;
                Console.WriteLine(t);
            }
            return result;
        }
        /// <summary>
        /// 通过NSGA2进化赛汝调度种群
        /// </summary>
        /// <param name="masterPopulation"></param>
        /// <param name="bestFormation"></param>
        /// <param name="parentSchedules"></param>
        /// <param name="name"></param>
        public ResultsForMultiThreading getNextScheduleGeneration(List<SeruScheduling> masterPopulation,  SeruFormation bestFormation, List<SeruScheduling> parentSchedules, String name)
        {
            Console.WriteLine(name);
            ResultsForMultiThreading resultsForMultiThreading = new ResultsForMultiThreading();
            List<SeruScheduling> newParents = new List<SeruScheduling>();
            List<SeruScheduling> cloneParents = DeepCopyByBin<List<SeruScheduling>>(parentSchedules);
            List<SeruScheduling> parents = DeepCopyByBin<List<SeruScheduling>>(parentSchedules);
            //----Step 1: 初始化当前解
            for (int p = 0; p < cloneParents.Count; p++)
            {
                SeruScheduling tempParent = cloneParents[p];
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                caculateFitness(bestFormation, tempParent);
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
            }
            //---Step 2: 精英策略：选择一对精英，一个来自父代种群，一个来自当前子代种群。
            while (parentSchedules.Count > 0)
            {
                SeruScheduling parent1 = new SeruScheduling();
                SeruScheduling parent2 = new SeruScheduling();
                //---从留下来的精英中无放回地挑选父母
                int number1 = random.Next(0, parentSchedules.Count);
                int number2 = random.Next(0, masterPopulation.Count);
                //---从父代种群和当前子代种群中选择个体
                parent1 = (SeruScheduling)(parentSchedules[number1].Clone());
                parent2 = (SeruScheduling)(masterPopulation[number2].Clone());

                //---Step 3: 交叉
                List<List<int>> OffspringCode = new List<List<int>>();
                if (random.NextDouble() < probabilityOfCrossover) 
                {
                    OffspringCode = CrossOver(parent1.scheduleCode, parent2.scheduleCode);
                    for (int i = 0; i < OffspringCode.Count; i++)
                    {
                        SeruScheduling tempOffSpring = new SeruScheduling();
                        tempOffSpring.scheduleCode = DeepCopyByBin<List<int>>(OffspringCode[i]);
                        tempOffSpring.BatchesAssignment = (tempOffSpring.produceSeruSchedule(numOfBatches));
                        SeruFormation clonebestFormation = DeepCopyByBin<SeruFormation>(bestFormation);
                        tempOffSpring.currentSeruSet = DeepCopyByBin<List<Seru>>(clonebestFormation.serusSet);
                        caculateFitness(clonebestFormation, tempOffSpring);
                        tempOffSpring.currentSeruSet = DeepCopyByBin<List<Seru>>(clonebestFormation.serusSet);
                        for (int k = 0; k < cloneParents.Count; k++)
                        {
                            if (((tempOffSpring.throughPutTime <= (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime < (cloneParents[k]).labourTime)) || ((tempOffSpring.throughPutTime < (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime <= (cloneParents[k]).labourTime)))
                            {
                                cloneParents.RemoveAt(k);
                                cloneParents.Add(tempOffSpring);
                                break;
                            }
                        }
                    }
                }
                parentSchedules.RemoveAll(x => ((x.throughPutTime == parent1.throughPutTime) && (x.labourTime == parent1.labourTime)));
            }
            //---Step 4: 变异
            mutation_SS(cloneParents, bestFormation);

            //---Step 5: 精英策略
            for (int i = 0; i < cloneParents.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruScheduling tempSolution = (SeruScheduling)(cloneParents[i]).Clone();

                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue(parents, tempSolution))
                {
                    parents.Add(tempSolution);
                }
            }
            //生成newParentSolutions，按照pareto front number and crowd distance 填充新种群
            for (int p = 0; p < parents.Count; p++)
            {
                SeruScheduling tempParent = parents[p];
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                caculateFitness(bestFormation, tempParent);
            }
            parents = produceNewParentSchesulingByDominatedSort_SS(parents);
            resultsForMultiThreading.newSchedules = DeepCopyByBin<List<SeruScheduling>>(parents);
            resultsForMultiThreading.nonDominatedSchedules = getParetoOfPopulation_SS(resultsForMultiThreading.newSchedules);
            Console.WriteLine($"------进化结束，当前线程为{Thread.CurrentThread.ManagedThreadId}------");
            return resultsForMultiThreading;
        }
        /// <summary>
        ///  通过NSGA2进化赛汝构造种群
        /// </summary>
        /// <param name="masterPopulation"></param>
        /// <param name="bestSchedule"></param>
        /// <param name="parentFormations"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public ResultsForMultiThreading getNextFormationGeneration(List<SeruFormation> masterPopulation, SeruScheduling bestSchedule, List<SeruFormation> parentFormations, String name)
        {
            Console.WriteLine(name);
            ResultsForMultiThreading resultsForMultiThreading = new ResultsForMultiThreading();
            List<SeruFormation> cloneParents = DeepCopyByBin<List<SeruFormation>>(parentFormations);
            List<SeruFormation > parents = DeepCopyByBin<List<SeruFormation >>(parentFormations);
            //----Step 1: 初始化当前解
            for (int p = 0; p < cloneParents.Count; p++)
            {
               SeruFormation tempParent = cloneParents[p];
                tempParent.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                caculateFitness(tempParent,bestSchedule);
            }
            //---Step 2: 精英策略：选择一对精英，一个来自父代种群，一个来自当前子代种群。
            while (parentFormations.Count > 0)
            {
                SeruFormation parent1 = new SeruFormation();
                SeruFormation parent2 = new SeruFormation();
                //---从留下来的精英中无放回地挑选父母
                int number1 = random.Next(0, parentFormations.Count);
                int number2 = random.Next(0, masterPopulation.Count);
                //---从父代种群和当前子代种群中选择个体
                parent1 = DeepCopyByBin<SeruFormation>(parentFormations[number1]);
                parent2 = DeepCopyByBin<SeruFormation>(masterPopulation[number2]);
                
                //---Step 3: 交叉
                List<List<int>> OffspringCode = new List<List<int>>();
                if (random.NextDouble() < probabilityOfCrossover)
                {
                    OffspringCode = CrossOver(parent1.formationCode, parent2.formationCode);
                    for (int i = 0; i < OffspringCode.Count; i++)
                    {
                        SeruFormation tempOffSpring = new SeruFormation();
                        tempOffSpring.formationCode = DeepCopyByBin<List<int>>(OffspringCode[i]);
                        tempOffSpring.serusSet = tempOffSpring.produceSerusSet(numOfWorkers);
                        tempOffSpring.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                        caculateFitness(tempOffSpring, bestSchedule);
                        for (int k = 0; k < cloneParents.Count; k++)
                        {
                            if (((tempOffSpring.throughPutTime <= (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime < (cloneParents[k]).labourTime)) || ((tempOffSpring.throughPutTime < (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime <= (cloneParents[k]).labourTime)))
                            {
                                cloneParents.RemoveAt(k);
                                cloneParents.Add(tempOffSpring);
                                break;
                            }
                        }
                    }
                }
                parentFormations.RemoveAll(x => ((x.throughPutTime == parent1.throughPutTime) && (x.labourTime == parent1.labourTime)));
            }
            //---Step 4: 变异
            mutation_SF(cloneParents, bestSchedule);
            //---Step5: 精英策略
            for (int i = 0; i < cloneParents.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruFormation tempSolution = (SeruFormation)(cloneParents[i]).Clone();

                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(parents, tempSolution))
                {
                    parents.Add(tempSolution);
                }
            }
            //生成newParentSolutions，按照pareto front number and crowd distance 填充新种群
            for (int p = 0; p < parents.Count; p++)
            {
                SeruFormation tempParent = parents[p];
                tempParent.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                caculateFitness(tempParent, bestSchedule);
            }
            parents = produceNewParentSchesulingByDominatedSort_SF(parents);
            resultsForMultiThreading.newFormations = DeepCopyByBin<List<SeruFormation>>(parents);
            resultsForMultiThreading.nonDominatedFormations = getParetoOfPopulation_SF(resultsForMultiThreading.newFormations);
            Console.WriteLine($"------进化结束，当前线程为{Thread.CurrentThread.ManagedThreadId}------");
            return resultsForMultiThreading;
        }
        /// <summary>
        /// 读取Excel数据
        /// </summary>
        public void readData()
        {
            string strConn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\实验数据4.xls;Extended Properties='Excel 8.0;HDR=yes;IMEX=1'";
            OleDbConnection conn = new OleDbConnection(strConn);
            OleDbDataAdapter myCommand1 = new OleDbDataAdapter("SELECT * FROM [批次与产品类型关系$]", strConn);
            OleDbDataAdapter myCommand2 = new OleDbDataAdapter("SELECT * FROM [工人与产品类型的熟练程度$]", strConn);
            OleDbDataAdapter myCommand3 = new OleDbDataAdapter("SELECT * FROM [多能工系数$]", strConn);
            try
            {
                myCommand1.Fill(tableBatchToProductType);
                myCommand2.Fill(tableWorkerToProductType);
                myCommand3.Fill(tableWorkerToMultipleTask);
            }
            catch (Exception ex)
            {
                throw new Exception("该Excel文件的工作表的名字不正确," + ex.Message);
            }
        }
        /// <summary>
        /// 计算Seru中批次的流通时间
        /// </summary>
        /// <param name="seru"></param>
        /// <param name="productTypeID"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        double caculateBatchFlowTimeInSeru(Seru seru, int productTypeID, int batchSize)
        {
            double flowTime = 0;
            double taskTimeInSeru = 0;
            //Seru中的所有工人，每个人加工该批次的一件产品的一道工序所用的时间的和
            for (int i = 0; i < seru.workersSet.Count; i++)
            {
                int workerID = Convert.ToInt16(seru.workersSet[i].ToString());
                double workerCoefficientOfMultipleTask = Convert.ToDouble(tableWorkerToMultipleTask.Rows[workerID - 1][1]);
                double c = 1;
                if ((numOfWorkers - maxNumOfMultipleTask) > 0)
                {
                    //多能工系数
                    c = c + workerCoefficientOfMultipleTask * (numOfWorkers - maxNumOfMultipleTask);
                }
                double workerToProductTypeCoefficient = Convert.ToDouble(tableWorkerToProductType.Rows[workerID - 1][productTypeID]);
                taskTimeInSeru = taskTimeInSeru + taskTime * c * workerToProductTypeCoefficient;
            }
            //Seru中加工一个工序的平均时间
            taskTimeInSeru = taskTimeInSeru / seru.workersSet.Count;
            flowTime = taskTimeInSeru * batchSize * numOfWorkers / seru.workersSet.Count;
            return flowTime;
        }
        /// <summary>
        /// 计算适应值
        /// </summary>
        /// <param name="formation"></param>
        /// <param name="schedule"></param>
        public void caculateFitness(SeruFormation formation, SeruScheduling schedule)
        {
            //计算totalThroughPutTime，为Seru中最长的
            //计算totalLabourTime，为Seru的总和
            List<List<int>> SeruSchedule = schedule.produceSeruSchedule(numOfBatches);
            //把批次分成组，如果组的个数小于Seru的个数，按组将批次分给不同的Seru
            if (SeruSchedule.Count < formation.serusSet.Count)
            {
                for (int i = 0; i < SeruSchedule.Count; i++)
                {
                    Seru serutmp = formation.serusSet[i];
                    serutmp.throughPutTime = 0;
                    serutmp.labourTime = 0;
                    serutmp.batchesSet = new List<int>();
                    List<int> temBatches = SeruSchedule[i];
                    for (int k = 0; k < temBatches.Count; k++)
                    {
                        int batchID = Convert.ToInt16(temBatches[k]);
                        int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][1]);
                        int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][2]);
                        serutmp.batchesSet.Add(batchID);
                        double flowTimeOfBatch = caculateBatchFlowTimeInSeru(serutmp, productTypeID, batchSize);
                        serutmp.throughPutTime = serutmp.throughPutTime + flowTimeOfBatch;
                        serutmp.labourTime = serutmp.labourTime + flowTimeOfBatch * serutmp.workersSet.Count;
                    }
                }
                //---初始化totalThroughPutTime/totalLabourTime
                formation.throughPutTime = formation.serusSet[0].throughPutTime;
                formation.labourTime = formation.serusSet[0].labourTime;
                //---计算totalThroughPutTime/totalLabourTime
                for (int i = 1; i < formation.serusSet.Count; i++)
                {
                    if ((formation.serusSet[i]).throughPutTime > formation.throughPutTime)
                    {
                        formation.throughPutTime = (formation.serusSet[i]).throughPutTime;
                    }
                    formation.labourTime = formation.labourTime + (formation.serusSet[i]).labourTime;
                }
                formation.throughPutTime = System.Math.Round(formation.throughPutTime, 3);
                formation.labourTime = System.Math.Round(formation.labourTime, 3);
                schedule.throughPutTime = formation.throughPutTime;
                schedule.labourTime = formation.labourTime;
            }
            //---否则就是堆的个数大于Seru的个数
            else
            {
                for (int i = 0; i < formation.serusSet.Count; i++)
                {
                    Seru serutmp = formation.serusSet[i];
                    serutmp.throughPutTime = 0;
                    serutmp.labourTime = 0;
                    serutmp.batchesSet = new List<int>();
                    List<int> temBatches = SeruSchedule[i];
                    for (int k = 0; k < temBatches.Count; k++)
                    {
                        int batchID = Convert.ToInt16(temBatches[k]);
                        int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][1]);
                        int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][2]);
                        serutmp.batchesSet.Add(batchID);
                        double flowTimeOfBatch = caculateBatchFlowTimeInSeru(serutmp, productTypeID, batchSize);
                        serutmp.throughPutTime = serutmp.throughPutTime + flowTimeOfBatch;
                        serutmp.labourTime = serutmp.labourTime + flowTimeOfBatch * serutmp.workersSet.Count;
                    }
                }
                for (int j = formation.serusSet.Count; j < SeruSchedule.Count; j++)
                {
                    Seru serutmp = formation.serusSet[j % formation.serusSet.Count];
                    List<int> temBatches = SeruSchedule[j];
                    for (int k = 0; k < temBatches.Count; k++)
                    {
                        int batchID = Convert.ToInt16(temBatches[k]);
                        int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][1]);
                        int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[batchID - 1][2]);
                        serutmp.batchesSet.Add(batchID);
                        double flowTimeOfBatch = caculateBatchFlowTimeInSeru(serutmp, productTypeID, batchSize);
                        serutmp.throughPutTime = serutmp.throughPutTime + flowTimeOfBatch;
                        serutmp.labourTime = serutmp.labourTime + flowTimeOfBatch * serutmp.workersSet.Count;
                    }
                }
                formation.throughPutTime = (formation.serusSet[0]).throughPutTime;
                formation.labourTime = (formation.serusSet[0]).labourTime;
                for (int i = 1; i < formation.serusSet.Count; i++)
                {
                    if ((formation.serusSet[i]).throughPutTime > formation.throughPutTime)
                    {
                        formation.throughPutTime = (formation.serusSet[i]).throughPutTime;
                    }
                    formation.labourTime = formation.labourTime + (formation.serusSet[i]).labourTime;
                }
                formation.throughPutTime = System.Math.Round(formation.throughPutTime, 3);
                formation.labourTime = System.Math.Round(formation.labourTime, 3);
                schedule.throughPutTime = formation.throughPutTime;
                schedule.labourTime = formation.labourTime;
            }
        }
        /// <summary>
        /// 生成Seru构造编码
        /// </summary>
        /// <returns></returns>
        public List<int> initialFormationCode()
        {
            double[] code1 = new double[2 * numOfWorkers - 1];
            double[] code2 = new double[2 * numOfWorkers - 1];
            for (int i = 0; i < 2 * numOfWorkers - 1; i++)
            {
                code1[i] = random.NextDouble();
                code2[i] = code1[i];
            }
            //--- 对数组code1排序
            Array.Sort(code1);
            List<int> formationCode = new List<int>();
            for (int i = 0; i < 2 * numOfWorkers - 1; i++)
            {
                formationCode.Add(Array.BinarySearch(code1, code2[i]) + 1);
            }
            return formationCode;
        }
        /// <summary>
        /// 生成Seru调度编码
        /// </summary>
        /// <returns></returns>
        public List<int> initialScheduleCode()
        {
            double[] code1 = new double[numOfBatches + numOfWorkers - 1];
            double[] code2 = new double[numOfBatches + numOfWorkers - 1];
            for (int i = 0; i < numOfBatches + numOfWorkers - 1; i++)
            {
                code1[i] = random.NextDouble();
                code2[i] = code1[i];
            }
            Array.Sort(code1);
            List<int> scheduleCode = new List<int>();
            for (int i = 0; i < numOfBatches + numOfWorkers - 1; i++)
            {
                scheduleCode.Add(Array.BinarySearch(code1, code2[i]) + 1);
            }
            return scheduleCode;
        }
        /// <summary>
        /// 交叉
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <returns></returns>
        public List<List<int>> CrossOver(List<int> code1, List<int> code2)
        {
            List<List<int>> OffSpringCode = new List<List<int>>();
            List<int> cloneCode1 = DeepCopyByBin<List<int>>(code1);
            List<int> cloneCode2 = DeepCopyByBin<List<int>>(code2);
        laberswap:
            int number1 = random.Next(0, code1.Count);
            int number2 = random.Next(0, code2.Count);
            if (number1 >= number2)
            {
                goto laberswap;
            }
            List<int> tempOffspring = new List<int>();
            tempOffspring = orderedCross(cloneCode1, cloneCode2, number1, number2);
            OffSpringCode.Add(tempOffspring);
            tempOffspring = new List<int>();
            tempOffspring = orderedCross(cloneCode2, cloneCode1, number1, number2);
            OffSpringCode.Add(tempOffspring);
            return OffSpringCode;
        }
        /// <summary>
        /// 顺序交叉操作
        /// </summary>
        /// <param name="code1"></param>
        /// <param name="code2"></param>
        /// <param name="number1"></param>
        /// <param name="number2"></param>
        /// <returns></returns>
        public List<int> orderedCross(List<int> code1, List<int> code2, int number1, int number2)
        {

            List<int> exchangePart = new List<int>();
            for (int k = number1 + 1; k < number2 + 1; k++)
            {
                exchangePart.Add(code2[k]);
            }
            List<int> MidCode1 = new List<int>();
            for (int i = number2 + 1; i < code1.Count; i++)
            {
                MidCode1.Add(code1[i]);
            }
            for (int i = 0; i < number2 + 1; i++)
            {
                MidCode1.Add(code1[i]);
            }
            for (int i = 0; i < exchangePart.Count; i++)
            {
                MidCode1.Remove(exchangePart[i]);
            }
            List<int> tempOffspring = new List<int>();
            for (int i = 0; i < number1 + 1; i++)
            {
                tempOffspring.Add(MidCode1[i]);
            }
            for (int i = 0; i < exchangePart.Count; i++)
            {
                tempOffspring.Add(exchangePart[i]);
            }
            for (int i = number1 + 1; i < MidCode1.Count; i++)
            {
                tempOffspring.Add(MidCode1[i]);
            }
            return tempOffspring;
        }
        /// <summary>
        /// 赛汝调度变异
        /// </summary>
        /// <param name="populations"></param>
        /// <param name="bestSchedule"></param>
        public void mutation_SS(List<SeruScheduling> populations, SeruFormation bestFormation)
        {
            for (int i = 0; i < populations.Count; i++)
            {
                //如果小于变异概率则变异，采用交换方式
                if (random.NextDouble() < probabilityOfMutation)
                {
                    SeruScheduling temp = (SeruScheduling)(populations[i]).Clone();
                    temp.scheduleCode = DeepCopyByBin<List<int>>(populations[i].scheduleCode);
                laberMutation:
                    int number1 = random.Next(0, temp.scheduleCode.Count);
                    int number2 = random.Next(0, temp.scheduleCode.Count);

                    //相等的话继续生成2个随机交换点
                    if (number1 == number2)
                    {
                        goto laberMutation;
                    }

                    //执行交换
                    int tempInt = (int)temp.scheduleCode[number1];
                    temp.scheduleCode[number1] = temp.scheduleCode[number2];
                    temp.scheduleCode[number2] = tempInt;
                    temp.BatchesAssignment = temp.produceSeruSchedule(numOfBatches);

                    caculateFitness(bestFormation,temp);                   //重新计算temp的适应值
                    populations[i] = DeepCopyByBin<SeruScheduling>(temp);
                    populations[i].currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                }
            }
        }
        /// <summary>
        /// 赛汝构造变异
        /// </summary>
        /// <param name="populations"></param>
        /// <param name="bestSchedule"></param>
        public void mutation_SF(List<SeruFormation> populations, SeruScheduling bestSchedule)
        {
            for (int i = 0; i < populations.Count; i++)
            {
                //如果小于变异概率则变异，采用交换方式
                if (random.NextDouble() < probabilityOfMutation)
                {
                    SeruFormation temp = (SeruFormation)(populations[i]).Clone();
                    temp.formationCode = DeepCopyByBin<List<int>>(populations[i].formationCode);
                laberMutation:
                    int number1 = random.Next(0, temp.formationCode.Count);
                    int number2 = random.Next(0, temp.formationCode.Count);

                    //相等的话继续生成2个随机交换点
                    if (number1 == number2)
                    {
                        goto laberMutation;
                    }

                    //执行交换
                    int tempInt = (int)temp.formationCode[number1];
                    temp.formationCode[number1] = temp.formationCode[number2];
                    temp.formationCode[number2] = tempInt;
                    temp.produceSerusSet(numOfWorkers);

                    caculateFitness(temp, bestSchedule);                   //重新计算temp的适应值
                    populations[i] = (SeruFormation)temp.Clone();
                    populations[i].formationCode =DeepCopyByBin<List<int>>(temp.formationCode);
                }
            }
        }
        /// <summary>
        /// 初始化赛汝调度主种群
        /// </summary>
        /// <param name="bestFormation"></param>
        /// <param name="parentSchedules"></param>
        public void InitializeMasterPopulation_SS(SeruFormation bestFormation, List<SeruScheduling> parentSchedules)
        {
            //初始化当前完整解
            List<SeruScheduling> cloneParents = DeepCopyByBin<List<SeruScheduling>>(parentSchedules);
            for (int p = 0; p < cloneParents.Count; p++)
            {
                SeruScheduling tempParent = cloneParents[p];
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                caculateFitness(bestFormation, tempParent);
            }
            cloneParents = produceNewParentSchesulingByDominatedSort_SS(cloneParents);
            masterPopulation_SS = getParetoOfPopulation_SS(cloneParents);
            if (masterPopulation_SS.Count==0) 
            {
                cloneParents.OrderBy(x=>x.throughPutTime);
                masterPopulation_SS.Add(cloneParents[0]);
            }
        }
        /// <summary>
        ///  对赛汝调度中的个体进行非支配排序与拥挤度排序
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruScheduling> produceNewParentSchesulingByDominatedSort_SS(List<SeruScheduling> population)
        {
            List<SeruScheduling> frontSet = new List<SeruScheduling>();                     //记录按pareto层排序的解集
            List<SeruScheduling> newParentSolutions = new List<SeruScheduling>();  //新种群

            //------初始化------
            for (int i = 0; i < population.Count; i++)
            {
                SeruScheduling iIndividual = population[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<SeruScheduling>();                     //每次计算pareto解时，重值为空
                iIndividual.distanceOfCrowd = 0;                                                           //每次计算pareto解时，distanceOfCrowd重置为空
            }

            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<SeruScheduling> firstFrontSet = new List<SeruScheduling>();       //第1层解集
            for (int p = 0; p < population.Count; p++)
            {
                SeruScheduling pIndividual = population[p];
                for (int q = 0; q < population.Count; q++)
                {
                    SeruScheduling qIndividual = population[q];
                    if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                    {
                        pIndividual.donimatedSet.Add(qIndividual);                               //qIndividual加入到pIndividual的支配解中
                    }
                    else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                    {
                        pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                    }
                }

                if (pIndividual.numOfDonimateIndividual == 0)                             //不被任何个体支配，加入到第1层front中。
                {
                    pIndividual.frontNumber = 1;
                    firstFrontSet.Add(pIndividual);
                }
            }


            //如果第1 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((firstFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(firstFrontSet);                                            //第1层加入到newParentSolutions
                //caculateCrowdDistance_SS(firstFrontSet);                                              //计算第1层crowdDistance
            }
            else
            {
                caculateCrowdDistance_SS(firstFrontSet);                                          //计算第1层crowdDistance
                firstFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                int countOfParent = newParentSolutions.Count;
                for (int i = 0; i < numOfPopular - countOfParent; i++)
                {
                    newParentSolutions.Add(firstFrontSet[i]);
                }
                return newParentSolutions;
            }

            //------生成第2层pareto解集------
            List<SeruScheduling> nextFrontSet = DeepCopyByBin<List<SeruScheduling>>(produceNextFrontNumber_SS(firstFrontSet));
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                                //第2层加入到newParentSolutions
                //caculateCrowdDistance_SS(nextFrontSet);                                   //计算第2层crowdDistance
            }
            else
            {
                caculateCrowdDistance_SS(nextFrontSet);                                    //计算第2层crowdDistance
                nextFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                int countOfParent = newParentSolutions.Count;
                for (int i = 0; i < numOfPopular - countOfParent; i++)
                {
                    newParentSolutions.Add(nextFrontSet[i]);
                }
                return newParentSolutions;
            }

            while (nextFrontSet.Count != 0)
            {
                nextFrontSet = produceNextFrontNumber_SS(nextFrontSet);                   //继续生成其它层pareto解集
                if (nextFrontSet.Count != 0)
                {
                    //如果下层front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
                    if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
                    {
                        newParentSolutions.AddRange(nextFrontSet);                                           //该层加入到newParentSolutions
                        //caculateCrowdDistance_SS(nextFrontSet);                                              //计算该层crowdDistance
                    }
                    else
                    {
                        caculateCrowdDistance_SS(nextFrontSet);                                                //计算该层crowdDistance
                        nextFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                        int countOfParent = newParentSolutions.Count;
                        for (int i = 0; i < numOfPopular - countOfParent; i++)
                        {
                            newParentSolutions.Add(nextFrontSet[i]);
                        }
                        return newParentSolutions;
                    }
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 生成下一层赛汝调度
        /// </summary>
        /// <param name="currentFrontSet"></param>
        /// <returns></returns>
        public List<SeruScheduling> produceNextFrontNumber_SS(List<SeruScheduling> currentFrontSet)
        {
            //produce the individuals in the next front number            
            List<SeruScheduling> nextFrontSet = new List<SeruScheduling>();                   //记录下1层front解
            for (int p = 0; p < currentFrontSet.Count; p++)
            {
                SeruScheduling pIndividual = currentFrontSet[p];
                for (int q = 0; q < pIndividual.donimatedSet.Count; q++)
                {
                    SeruScheduling qIndividual = pIndividual.donimatedSet[q];
                    qIndividual.numOfDonimateIndividual = qIndividual.numOfDonimateIndividual - 1;
                    if (qIndividual.numOfDonimateIndividual == 0)
                    {
                        qIndividual.frontNumber = pIndividual.frontNumber + 1;   //记录front层号
                        nextFrontSet.Add(qIndividual);         //加入到下1层front中
                    }
                }
            }
            //loop ends, if nextFrontSet is empty
            if (nextFrontSet.Count != 0)
            {
                return nextFrontSet;
            }
            return nextFrontSet;
        }
        /// <summary>
        /// 计算赛汝调度种群中个体的拥挤度
        /// </summary>
        /// <param name="currentFrontSet"></param>
        public void caculateCrowdDistance_SS(List<SeruScheduling> currentFrontSet)
        {
            // sort by TTPT
            currentFrontSet.OrderBy(x => x.throughPutTime);
            // set the distance of the first individual which has the minimum of TTPT as maxvalue
            currentFrontSet[0].distanceOfCrowd = double.MaxValue;
            // set the distance of the last individual which has the maximum of TTPT as maxvalue
            currentFrontSet[currentFrontSet.Count - 1].distanceOfCrowd = double.MaxValue;
            double minimumOfTTPT = (currentFrontSet[0]).throughPutTime;
            double maximumOfTTPT = (currentFrontSet[currentFrontSet.Count - 1]).throughPutTime;
            double distanceOfMaxToMinOfTTPT = maximumOfTTPT - minimumOfTTPT;

            for (int i = 1; i < currentFrontSet.Count - 1; i++)
            {
                //一个front中的解都相等，实则是一个
                if (distanceOfMaxToMinOfTTPT == 0)
                {
                    (currentFrontSet[i]).distanceOfCrowd = double.MaxValue;
                }
                else
                    (currentFrontSet[i]).distanceOfCrowd = ((currentFrontSet[i + 1]).throughPutTime - (currentFrontSet[i - 1]).throughPutTime) / distanceOfMaxToMinOfTTPT;
            }

            // sort by TLT. 
            currentFrontSet.OrderBy(x => x.labourTime);
            double minimumOfTLT = (currentFrontSet[0]).labourTime;
            double maximumOfTLT = (currentFrontSet[currentFrontSet.Count - 1]).labourTime;
            double distanceOfMaxToMinOfTLT = maximumOfTLT - minimumOfTLT;
            for (int i = 1; i < currentFrontSet.Count - 1; i++)
            {
                if (distanceOfMaxToMinOfTLT != 0)
                {
                    (currentFrontSet[i]).distanceOfCrowd = ((currentFrontSet[i]).distanceOfCrowd + ((currentFrontSet[i + 1]).labourTime - (currentFrontSet[i - 1]).labourTime) / distanceOfMaxToMinOfTLT) / 2;
                }
            }
        }
        /// <summary>
        /// 得到赛汝调度集合的pareto前沿
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruScheduling> getParetoOfPopulation_SS(List<SeruScheduling> population)
        {
            List<SeruScheduling> tempPopulation = new List<SeruScheduling>();
            for (int j = 0; j < population.Count; j++)
            {
                SeruScheduling currentSolution = population[j];
                if (currentSolution.frontNumber == 1)
                {
                    tempPopulation.Add((SeruScheduling)currentSolution.Clone());
                }
                else
                    break;
            }
            tempPopulation.OrderBy(x=>x.throughPutTime);
            return tempPopulation;
        }
        /// <summary>
        /// 初始化赛汝构造主种群
        /// </summary>
        /// <param name="bestFormation"></param>
        /// <param name="parentSchedules"></param>
        public void InitializeMasterPopulation_SF(SeruScheduling  bestSchedule, List<SeruFormation> parentFormations)
        {
            //初始化当前完整解
            List<SeruFormation> cloneParents = DeepCopyByBin<List<SeruFormation>>(parentFormations);
            for (int p = 0; p < cloneParents.Count; p++)
            {
                SeruFormation tempParent = cloneParents[p];
                tempParent.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                caculateFitness(tempParent, bestSchedule);
            }
            cloneParents = produceNewParentSchesulingByDominatedSort_SF(cloneParents);
            masterPopulation_SF = getParetoOfPopulation_SF(cloneParents);
            if (masterPopulation_SF.Count==0)
            {
                cloneParents.OrderBy(x=>x.throughPutTime);
                masterPopulation_SF.Add(cloneParents[0]);
            }
        }
        /// <summary>
        ///  对赛汝构造中的个体进行非支配排序与拥挤度排序
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> produceNewParentSchesulingByDominatedSort_SF(List<SeruFormation> population)
        {
            List<SeruFormation> frontSet = new List<SeruFormation>();                     //记录按pareto层排序的解集
            List<SeruFormation> newParentSolutions = new List<SeruFormation>();  //新种群

            //------初始化------
            for (int i = 0; i < population.Count; i++)
            {
                SeruFormation iIndividual = population[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<SeruFormation>();                      //每次计算pareto解时，重值为空
                iIndividual.distanceOfCrowd = 0;                                                           //每次计算pareto解时，distanceOfCrowd重置为空
            }

            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<SeruFormation> firstFrontSet = new List<SeruFormation>();        //第1层解集
            for (int p = 0; p < population.Count; p++)
            {
                SeruFormation pIndividual = population[p];
                for (int q = 0; q < population.Count; q++)
                {
                    SeruFormation qIndividual = population[q];
                    if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                    {
                        pIndividual.donimatedSet.Add(qIndividual);                               //qIndividual加入到pIndividual的支配解中
                    }
                    else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                    {
                        pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                    }
                }

                if (pIndividual.numOfDonimateIndividual == 0)                             //不被任何个体支配，加入到第1层front中。
                {
                    pIndividual.frontNumber = 1;
                    firstFrontSet.Add(pIndividual);
                }
            }

            //如果第1 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((firstFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(firstFrontSet);                              //第1层加入到newParentSolutions
                //caculateCrowdDistance_SF(firstFrontSet);                                 //计算第1层crowdDistance
            }
            else
            {
                caculateCrowdDistance_SF(firstFrontSet);                                   //计算第1层crowdDistance
                firstFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                int countOfParent = newParentSolutions.Count;
                for (int i = 0; i < numOfPopular - countOfParent; i++)
                {
                    newParentSolutions.Add(firstFrontSet[i]);
                }
                return newParentSolutions;
            }

            //------生成第2层pareto解集------
            List<SeruFormation> nextFrontSet = DeepCopyByBin<List<SeruFormation>>(produceNextFrontNumber_SF(firstFrontSet));
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                         //第2层加入到newParentSolutions
                //caculateCrowdDistance_SF(nextFrontSet);                            //计算第2层crowdDistance
            }
            else
            {
                caculateCrowdDistance_SF(nextFrontSet);                                    //计算第2层crowdDistance
                nextFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                int countOfParent = newParentSolutions.Count;
                for (int i = 0; i < numOfPopular - countOfParent; i++)
                {
                    newParentSolutions.Add(nextFrontSet[i]);
                }
                return newParentSolutions;
            }

            while (nextFrontSet.Count != 0)
            {
                nextFrontSet = DeepCopyByBin<List<SeruFormation>>(produceNextFrontNumber_SF(nextFrontSet));                   //继续生成其它层pareto解集
                if (nextFrontSet.Count != 0)
                {
                    //如果下层front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
                    if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
                    {
                        newParentSolutions.AddRange(nextFrontSet);                                     //该层加入到newParentSolutions
                        //caculateCrowdDistance_SF(nextFrontSet);                                         //计算该层crowdDistance
                    }
                    else
                    {
                        caculateCrowdDistance_SF(nextFrontSet);                                                //计算该层crowdDistance
                        nextFrontSet.OrderByDescending(x => x.distanceOfCrowd);
                        int countOfParent = newParentSolutions.Count;
                        for (int i = 0; i < numOfPopular - countOfParent; i++)
                        {
                            newParentSolutions.Add(nextFrontSet[i]);
                        }
                        return newParentSolutions;
                    }
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 生成下一层赛汝构造
        /// </summary>
        /// <param name="currentFrontSet"></param>
        /// <returns></returns>
        public List<SeruFormation> produceNextFrontNumber_SF(List<SeruFormation> currentFrontSet)
        {
            //produce the individuals in the next front number            
            List<SeruFormation> nextFrontSet = new List<SeruFormation>();                   //记录下1层front解
            for (int p = 0; p < currentFrontSet.Count; p++)
            {
                SeruFormation pIndividual = currentFrontSet[p];
                for (int q = 0; q < pIndividual.donimatedSet.Count; q++)
                {
                    SeruFormation qIndividual = pIndividual.donimatedSet[q];
                    qIndividual.numOfDonimateIndividual = qIndividual.numOfDonimateIndividual - 1;
                    if (qIndividual.numOfDonimateIndividual == 0)
                    {
                        qIndividual.frontNumber = pIndividual.frontNumber + 1;   //记录front层号
                        nextFrontSet.Add(qIndividual);         //加入到下1层front中
                    }
                }
            }
            //loop ends, if nextFrontSet is empty
            if (nextFrontSet.Count != 0)
            {
                return nextFrontSet;
            }
            return nextFrontSet;
        }
        /// <summary>
        /// 计算赛汝构造种群中个体的拥挤度
        /// </summary>
        /// <param name="currentFrontSet"></param>
        public void caculateCrowdDistance_SF(List<SeruFormation> currentFrontSet)
        {
            // sort by TTPT
            currentFrontSet.OrderBy(x => x.throughPutTime);
            // set the distance of the first individual which has the minimum of TTPT as maxvalue
            currentFrontSet[0].distanceOfCrowd = double.MaxValue;
            // set the distance of the last individual which has the maximum of TTPT as maxvalue
            currentFrontSet[currentFrontSet.Count - 1].distanceOfCrowd = double.MaxValue;
            double minimumOfTTPT = (currentFrontSet[0]).throughPutTime;
            double maximumOfTTPT = (currentFrontSet[currentFrontSet.Count - 1]).throughPutTime;
            double distanceOfMaxToMinOfTTPT = maximumOfTTPT - minimumOfTTPT;

            for (int i = 1; i < currentFrontSet.Count - 1; i++)
            {
                //一个front中的解都相等，实则是一个
                if (distanceOfMaxToMinOfTTPT == 0)
                {
                    (currentFrontSet[i]).distanceOfCrowd = double.MaxValue;
                }
                else
                    (currentFrontSet[i]).distanceOfCrowd = ((currentFrontSet[i + 1]).throughPutTime - (currentFrontSet[i - 1]).throughPutTime) / distanceOfMaxToMinOfTTPT;
            }

            // sort by TLT. 
            currentFrontSet.OrderBy(x => x.labourTime);
            double minimumOfTLT = (currentFrontSet[0]).labourTime;
            double maximumOfTLT = (currentFrontSet[currentFrontSet.Count - 1]).labourTime;
            double distanceOfMaxToMinOfTLT = maximumOfTLT - minimumOfTLT;
            for (int i = 1; i < currentFrontSet.Count - 1; i++)
            {
                if (distanceOfMaxToMinOfTLT != 0)
                {
                    (currentFrontSet[i]).distanceOfCrowd = ((currentFrontSet[i]).distanceOfCrowd + ((currentFrontSet[i + 1]).labourTime - (currentFrontSet[i - 1]).labourTime) / distanceOfMaxToMinOfTLT) / 2;
                }
            }
        }
        /// <summary>
        /// 得到赛汝构造集合的pareto前沿
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> getParetoOfPopulation_SF(List<SeruFormation> population)
        {
            List<SeruFormation> tempPopulation = new List<SeruFormation>();
            for (int j = 0; j < population.Count; j++)
            {
                SeruFormation currentSolution = population[j];
                if (currentSolution.frontNumber == 1)
                {
                    tempPopulation.Add((SeruFormation)currentSolution.Clone());
                }
                else
                    break;
            }
            tempPopulation.OrderBy(x => x.throughPutTime);

            return tempPopulation;
        }
        /// <summary>
        /// 邻域搜索
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public List<int> localSearch(List<int> code)
        {
        laberSwap:
            int number1 = random.Next(0, code.Count);
            int number2 = random.Next(0, code.Count);
            //相等的话继续生成2个随机交换点
            if (number1 == number2)
            {
                goto laberSwap;
            }
            //执行交换
            int tempInt = (int)code[number1];
            code[number1] = code[number2];
            code[number2] = tempInt;
            return code;
        }
        /// <summary>
        /// 如果individual目标值已在population返回true
        /// </summary>
        /// <param name="population"></param>
        /// <param name="individual"></param>
        /// <returns></returns>
        public bool ifSolutionExistedInPopulationWithObjectiveValue(List<SeruScheduling> population, SeruScheduling individual)
        {
            bool flag = false;
            for (int i = 0; i < population.Count; i++)
            {
                //将population的一个Individual的solutionCode与individual相比
                
                if ((individual.throughPutTime == (population[i]).throughPutTime) && (individual.labourTime == (population[i]).labourTime))
                {
                    flag = true;
                    break;
                }                
            }
            return flag;
        }
        /// <summary>
        /// 如果individual目标值已在population返回true
        /// </summary>
        /// <param name="population"></param>
        /// <param name="individual"></param>
        /// <returns></returns>
        public bool ifSolutionExistedInPopulationWithObjectiveValue_SF(List<SeruFormation> population,SeruFormation individual)
        {
            bool flag = false;
            for (int i = 0; i < population.Count; i++)
            {
                //将population的一个Individual的solutionCode与individual相比
                if ((individual.throughPutTime == population[i].throughPutTime) && (individual.labourTime == population[i].labourTime))
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }
        /// <summary>
        /// 深克隆
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T DeepCopyByBin<T>(T t)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, t);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }
        /// <summary>
        /// 程序入口2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            //List<int> workers = new List<int> { 8, 10, 15, 20 };
            //List<int> batches = new List<int> { 10, 15, 20, 25, 30 };

            //List<int> workers = new List<int> { 4, 5, 6 };
            //List<int> batches = new List<int> { 5, 6, 7, 8, 9 };

            List<int> workers = new List<int> { 6 };
            List<int> batches = new List<int> { 8 };

            int Row = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                numOfWorkers = workers[i];
                for (int j = 0; j < batches.Count; j++)
                {
                    Row++;
                    numOfBatches = batches[j];
                    MOCCMS_NSGA_III();
                }
            }
        }
        /// <summary>
        /// MOCCMS_NSGA_III
        /// </summary>
        public void MOCCMS_NSGA_III() 
        {
            string outPutName = @"D:\Result_MOCC\MOCCMS_NSGA_III_small\MOCCMS_NSGA3_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";
            //string outPutName = @"D:\Result_MOCC\Reference_Set\MOCCMS_NSGA3_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";

            P_mCmax_SF = new List<SeruFormation>();
            P_mTLH_SF = new List<SeruFormation>();
            P_aveND_SF = new List<SeruFormation>();
            P_mCmax_SS = new List<SeruScheduling>();
            P_mTLH_SS = new List<SeruScheduling>();
            P_aveND_SS = new List<SeruScheduling>();

            //------生成参考集------
            int M = 2;
            int H = 10;
            List<double[]> referencePointSet = GenerateReferencePoints(M, H);

            //------生成初始的赛汝构造的从种群和赛汝调度的从种群------
            for (int i = 0; i < numOfPopular; i++)
            {
                SeruFormation formation1 = new SeruFormation();
                formation1.formationCode = initialFormationCode();                                                                       //初始化构造编码
                formation1.serusSet = formation1.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_mCmax_SF.Add(formation1);

                SeruFormation formation2 = new SeruFormation();
                formation2.formationCode = initialFormationCode();                                                                       //初始化构造编码
                formation2.serusSet = formation2.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_mTLH_SF.Add(formation2);

                SeruFormation formation3 = new SeruFormation();
                formation3.formationCode = initialFormationCode();                                                                      //初始化构造编码
                formation3.serusSet = formation2.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                P_aveND_SF.Add(formation3);

                SeruScheduling schedule1 = new SeruScheduling();
                schedule1.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule1.BatchesAssignment = schedule1.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_mCmax_SS.Add(schedule1);

                SeruScheduling schedule2 = new SeruScheduling();
                schedule2.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule2.BatchesAssignment = schedule2.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_mTLH_SS.Add(schedule2);

                SeruScheduling schedule3 = new SeruScheduling();
                schedule3.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule3.BatchesAssignment = schedule3.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                P_aveND_SS.Add(schedule3);
            }

            //------初始化协同进化个体------
            mCmax_SF = P_mCmax_SF[0];
            mTLH_SF = P_mTLH_SF[0];
            aveND_SF = P_aveND_SF[0];

            mCmax_SS = P_mCmax_SS[0];
            mTLH_SS = P_mTLH_SS[0];
            aveND_SS = P_aveND_SS[0];

            int iterator = 0;
            //DateTime startTime = DateTime.Now;

            //------初始化赛汝构造或者赛汝调度的主种群------
            InitializeMasterPopulation_SS(mTLH_SF, P_mTLH_SS);
            InitializeMasterPopulation_SF(mTLH_SS, P_mTLH_SF);

            //------开始进化------
            DateTime time1 = DateTime.Now;
            for (int m = 0; m < maxItera; m++)
            {
                Console.WriteLine($"===================第{iterator + 1}次迭代================");
                Result result = ProcessCooperativeEvoPro(iterator, mCmax_SF, mTLH_SF, aveND_SF, mCmax_SS, mTLH_SS, aveND_SS, P_mCmax_SF, P_mTLH_SF, P_aveND_SF, P_mCmax_SS, P_mTLH_SS, P_aveND_SS);
                if (iterator % 2 == 0)//先进化构造种群
                {
                    P_mCmax_SF = result.P_mCmax_SF;
                    P_mTLH_SF = result.P_mTLH_SF;
                    P_aveND_SF = result.P_aveND_SF;
                    mCmax_SF = result.mCmax_SF;
                    mTLH_SF = result.mTLH_SF;
                    aveND_SF = result.aveND_SF;
                    masterPopulation_SF = result.masterPopulation_SF;
                    //Console.WriteLine($"SF------numofSolutions : {masterPopulation_SF.Count}");
                    //Console.WriteLine($"SF------TTPT is : {aveND_SF.throughPutTime}; TLH is {aveND_SF.labourTime}");
                }
                else
                {
                    P_mCmax_SS = result.P_mCmax_SS;
                    P_mTLH_SS = result.P_mTLH_SS;
                    P_aveND_SS = result.P_aveND_SS;
                    mCmax_SS = result.mCmax_SS;
                    mTLH_SS = result.mTLH_SS;
                    aveND_SS = result.aveND_SS;
                    masterPopulation_SS = result.masterPopulation_SS;
                    //Console.WriteLine($"SS------numofSolutions : {masterPopulation_SS.Count}");
                    //Console.WriteLine($"SS------TTPT is : {aveND_SS.throughPutTime}; TLH is {aveND_SS.labourTime}");
                }
                iterator++;
            }
            //------赛汝构造和赛汝调度组合------
            List<SeruFormation> masterPopulation_SFClone = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
            for (int i = 0; i < masterPopulation_SFClone.Count; i++)
            {
                SeruFormation currentFormation = masterPopulation_SFClone[i];
                for (int j = 0; j < masterPopulation_SS.Count; j++)
                {
                    SeruScheduling currentSchedule = DeepCopyByBin<SeruScheduling>(masterPopulation_SS[j]);
                    caculateFitness(currentFormation, currentSchedule);
                }
                masterPopulation_SF.Add(DeepCopyByBin<SeruFormation>(currentFormation));
            }
            masterPopulation_SF = produceNewParentSchesulingByDominatedSort_SF(masterPopulation_SF);
            masterPopulation_SF = getParetoOfPopulation_SF(masterPopulation_SF);
            List<SeruFormation> temp1 = new List<SeruFormation>();
            for (int i = 0; i < masterPopulation_SF.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruFormation tempSolution = DeepCopyByBin<SeruFormation>(masterPopulation_SF[i]);
                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(temp1, tempSolution))
                {
                    temp1.Add(tempSolution);
                }
            }
            masterPopulation_SF = DeepCopyByBin<List<SeruFormation>>(temp1);
            //------检查支配关系------
            List<SeruScheduling> tem = new List<SeruScheduling>();
            for (int i = 0; i < masterPopulation_SS.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < masterPopulation_SF.Count; j++)
                {
                    if (((masterPopulation_SS[i]).throughPutTime >= (masterPopulation_SF[j]).throughPutTime) && (masterPopulation_SS[i]).labourTime >= (masterPopulation_SF[j]).labourTime)
                        flag = false;
                }
                if (flag == true)
                    tem.Add(masterPopulation_SS[i]);
            }
            masterPopulation_SS = tem;
            List<SeruFormation> tem1 = new List<SeruFormation>();
            for (int i = 0; i < masterPopulation_SF.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < masterPopulation_SS.Count; j++)
                {
                    if (((masterPopulation_SF[i]).throughPutTime >= (masterPopulation_SS[j]).throughPutTime) && ((masterPopulation_SF[i]).labourTime >= (masterPopulation_SS[j]).labourTime))
                        flag = false;
                }
                if (flag == true)
                    tem1.Add(masterPopulation_SF[i]);
            }
            masterPopulation_SF = tem1;
            //------打印实验结果------
            Console.WriteLine("-----------------------------------------------------------------");
            for (int i = 0; i < masterPopulation_SF.Count; i++)
                Console.WriteLine((masterPopulation_SF[i]).throughPutTime + "   " + (masterPopulation_SF[i]).labourTime);
            for (int i = 0; i < masterPopulation_SS.Count; i++)
                Console.WriteLine((masterPopulation_SS[i]).throughPutTime + "   " + (masterPopulation_SS[i]).labourTime);
            Console.WriteLine("-------------------主程序运行结束--------------------------");
            int numOfSolutions = masterPopulation_SS.Count + masterPopulation_SF.Count;
            DateTime time2 = DateTime.Now;
            Console.WriteLine(time2 - time1);
            OuT ouT = new OuT();
            ouT.OutPut(masterPopulation_SF, masterPopulation_SS, numOfSolutions, outPutName, (time2 - time1).TotalSeconds);
            Console.WriteLine("-------------------数据输出结束--------------------------");
        }
        /// <summary>
        /// 产生（超）平面上的参考点
        /// </summary> 如果维数超过二维，需要通过Das and Dennis’s method获得参考点
        /// <param name="M"></param> 目标数
        /// <param name="H"></param> 目标值划分的数目
        /// <returns></returns>
        public List<double[]> GenerateReferencePoints(int M, int H)
        {
            List<double[]> referencePointSet = new List<double[]>();
            double[] valueOfTTPT = new double[H];
            double[] valueOfTLH = new double[H];
            for (int i = 0; i < H; i++)
            {
                valueOfTTPT[i] = (double)(i + 1) / (double)H;
                valueOfTLH[i] = (double)(i + 1) / (double)H;
            }
            for (int i = 0; i < H; i++)
            {
                for (int j = 0; j < H; j++)
                {
                    if (valueOfTTPT[i] + valueOfTLH[j] == 1)
                    {
                        double[] point = new double[2];
                        point[0] = valueOfTTPT[i];
                        point[1] = valueOfTLH[j];
                        referencePointSet.Add(point);
                    }
                }
            }
            return referencePointSet;
        }
        /// <summary>
        /// 执行改进版本的多目标协同进化
        /// </summary>
        /// <param name="iterator"></param>
        /// <param name="mCmax_SF"></param>
        /// <param name="mTLH_SF"></param>
        /// <param name="aveND_SF"></param>
        /// <param name="mCmax_SS"></param>
        /// <param name="mTLH_SS"></param>
        /// <param name="aveND_SS"></param>
        /// <param name="P_mCmax_SF"></param>
        /// <param name="P_mTLH_SF"></param>
        /// <param name="P_aveND_SF"></param>
        /// <param name="P_mCmax_SS"></param>
        /// <param name="P_mTLH_SS"></param>
        /// <param name="P_aveND_SS"></param>
        /// <returns></returns>
        public Result ProcessCooperativeEvoPro(int iterator, SeruFormation mCmax_SF, SeruFormation mTLH_SF, SeruFormation aveND_SF, SeruScheduling mCmax_SS, SeruScheduling mTLH_SS, SeruScheduling aveND_SS, List<SeruFormation> P_mCmax_SF, List<SeruFormation> P_mTLH_SF, List<SeruFormation> P_aveND_SF, List<SeruScheduling> P_mCmax_SS, List<SeruScheduling> P_mTLH_SS, List<SeruScheduling> P_aveND_SS)
        {
            Result result = new Result();
            if (iterator % 2 == 0)
            {
                SeruScheduling cloneMCmax_SS = DeepCopyByBin<SeruScheduling>(mCmax_SS);
                SeruScheduling cloneMTLH_SS = DeepCopyByBin<SeruScheduling>(mTLH_SS);
                SeruScheduling cloneAveND_SS = DeepCopyByBin<SeruScheduling>(aveND_SS);

                DateTime begin = DateTime.Now;

                //------赛汝构造的从种群并行进化产生子代种群与当前非支配解------
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func1 = getNextFormationGenerationByNSGA3;
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func2 = getNextFormationGenerationByNSGA3;
                Func<List<SeruFormation>, SeruScheduling, List<SeruFormation>, String, ResultsForMultiThreading> func3 = getNextFormationGenerationByNSGA3;

                IAsyncResult asyncResult1 = func1.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMCmax_SS, P_mCmax_SF, "------Evolve P_mCmax_SF------", null, null);
                IAsyncResult asyncResult2 = func2.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneMTLH_SS, P_mTLH_SF, "------Evolve P_mTLH_SF------", null, null);
                IAsyncResult asyncResult3 = func3.BeginInvoke(DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF), cloneAveND_SS, P_aveND_SF, "------Evolve P_aveND_SF------", null, null);

                ResultsForMultiThreading result1 = func1.EndInvoke(asyncResult1);
                ResultsForMultiThreading result2 = func2.EndInvoke(asyncResult2);
                ResultsForMultiThreading result3 = func3.EndInvoke(asyncResult3);

                //更新从种群
                result.P_mCmax_SF = result1.newFormations;
                result.P_mTLH_SF = result2.newFormations;
                result.P_aveND_SF = result3.newFormations;

                //------更新赛汝构造的主种群------
                List<SeruFormation> temp = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
                masterPopulation_SF = new List<SeruFormation>();
                temp.AddRange(result1.nonDominatedFormations);
                temp.AddRange(result2.nonDominatedFormations);
                temp.AddRange(result3.nonDominatedFormations);
                List<SeruFormation> temp1 = new List<SeruFormation>();
                for (int i = 0; i < temp.Count; i++)
                {
                    //新种群中的individual在父种群有，可以添加的情况
                    SeruFormation tempSolution = (SeruFormation)(temp[i]).Clone();
                    //新种群中的individual在父种群有，不可以添加的情况
                    if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(temp1, tempSolution))
                    {
                        temp1.Add(tempSolution);
                    }
                }
                //------非支配排序------
                for (int i = 0; i < temp1.Count; i++)
                {
                    SeruFormation iIndividual = temp1[i];
                    iIndividual.frontNumber = 0;                                                        //每次计算pareto解时，frontNumber重值为0
                    iIndividual.numOfDonimateIndividual = 0;                                  //每次计算pareto解时，重值为0
                    iIndividual.donimatedSet = new List<SeruFormation>();           //每次计算pareto解时，重值为空
                    iIndividual.distanceOfCrowd = 0;
                }
                for (int p = 0; p < temp1.Count; p++)
                {
                    SeruFormation pIndividual = temp1[p];
                    for (int q = 0; q < temp1.Count; q++)
                    {
                        SeruFormation qIndividual = temp1[q];
                        if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                        {
                            pIndividual.donimatedSet.Add(qIndividual);          //qIndividual加入到pIndividual的支配解中
                        }
                        else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                        {
                            pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                        }
                    }
                    if (pIndividual.numOfDonimateIndividual == 0)               //不被任何个体支配，加入到第1层front中。
                    {
                        pIndividual.frontNumber = 1;
                        masterPopulation_SF.Add(pIndividual);
                    }
                }

                //------更新协同进化个体------
                SeruFormation temSolution1 = new SeruFormation();
                SeruFormation temSolution2 = new SeruFormation();
                temSolution1.throughPutTime = double.MaxValue;
                temSolution2.labourTime = double.MaxValue;
                double temSumOfThroughPutTime = 0;
                double temSumOfLabourTime = 0;
                for (int i = 0; i < masterPopulation_SF.Count; i++)
                {
                    if ((masterPopulation_SF[i]).throughPutTime < temSolution1.throughPutTime)
                    {
                        temSolution1 = masterPopulation_SF[i];
                    }

                    if ((masterPopulation_SF[i]).labourTime < temSolution2.labourTime)
                    {
                        temSolution2 = masterPopulation_SF[i];
                    }

                    temSumOfThroughPutTime = temSumOfThroughPutTime + masterPopulation_SF[i].throughPutTime;
                    temSumOfLabourTime = temSumOfLabourTime + masterPopulation_SF[i].labourTime;
                }
                result.mCmax_SF = (SeruFormation)temSolution1.Clone();
                result.mTLH_SF = (SeruFormation)temSolution2.Clone();
                temSumOfThroughPutTime = temSumOfThroughPutTime / (masterPopulation_SF.Count);
                temSumOfLabourTime = temSumOfLabourTime / (masterPopulation_SF.Count);

                double temvalue = double.MaxValue;
                double currentValue = 0;

                for (int i = 0; i < masterPopulation_SF.Count; i++)
                {
                    currentValue = Math.Pow(((masterPopulation_SF[i]).throughPutTime - temSumOfThroughPutTime), 2) + Math.Pow(((masterPopulation_SF[i]).labourTime - temSumOfLabourTime), 2);
                    if (currentValue < temvalue)
                    {
                        result.aveND_SF = (SeruFormation)masterPopulation_SF[i].Clone();
                        temvalue = currentValue;
                    }
                }
                result.masterPopulation_SF = DeepCopyByBin<List<SeruFormation>>(masterPopulation_SF);
                DateTime end = DateTime.Now;
                TimeSpan t = end - begin;
                Console.WriteLine(t);
            }
            else
            {
                SeruFormation cloneMCmax_SF = DeepCopyByBin<SeruFormation>(mCmax_SF);
                SeruFormation cloneMTLH_SF = DeepCopyByBin<SeruFormation>(mTLH_SF);
                SeruFormation cloneAveND_SF = DeepCopyByBin<SeruFormation>(aveND_SF);

                DateTime begin = DateTime.Now;

                //------赛汝调度的从种群并行进化产生子代种群与当前非支配解------
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func1 = getNextScheduleGenerationByNSGA3;
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func2 = getNextScheduleGenerationByNSGA3;
                Func<List<SeruScheduling>, SeruFormation, List<SeruScheduling>, String, ResultsForMultiThreading> func3 = getNextScheduleGenerationByNSGA3;

                IAsyncResult asyncResult1 = func1.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMCmax_SF, P_mCmax_SS, "------Evolve P_mCmax_SS------", null, null);
                IAsyncResult asyncResult2 = func2.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMTLH_SF, P_mTLH_SS, "------Evolve P_mTLH_SS------", null, null);
                IAsyncResult asyncResult3 = func3.BeginInvoke(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneAveND_SF, P_aveND_SS, "------Evolve P_aveND_SS------", null, null);

                ResultsForMultiThreading result1 = func1.EndInvoke(asyncResult1);
                ResultsForMultiThreading result2 = func2.EndInvoke(asyncResult2);
                ResultsForMultiThreading result3 = func3.EndInvoke(asyncResult3);

                //ResultsForMultiThreading result1 = getNextScheduleGenerationByNSGA3(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMCmax_SF, P_mCmax_SS, "------Evolve P_mCmax_SS------");
                //ResultsForMultiThreading result2 = getNextScheduleGenerationByNSGA3(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneMTLH_SF, P_mTLH_SS, "------Evolve P_mTLH_SS------");
                //ResultsForMultiThreading result3 = getNextScheduleGenerationByNSGA3(DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS), cloneAveND_SF, P_aveND_SS, "------Evolve P_aveND_SS------");

                //更新从种群
                result.P_mCmax_SS = DeepCopyByBin<List<SeruScheduling>>(result1.newSchedules);
                result.P_mTLH_SS = DeepCopyByBin<List<SeruScheduling>>(result2.newSchedules);
                result.P_aveND_SS = DeepCopyByBin<List<SeruScheduling>>(result3.newSchedules);

                //DateTime end = DateTime.Now;
                //TimeSpan t = end - begin;
                //Console.WriteLine(t);

                //------更新赛汝调度的主种群------
                List<SeruScheduling> temp = DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS);
                masterPopulation_SS = new List<SeruScheduling>();
                temp.AddRange(result1.nonDominatedSchedules);
                temp.AddRange(result2.nonDominatedSchedules);
                temp.AddRange(result3.nonDominatedSchedules);
                List<SeruScheduling> temp1 = new List<SeruScheduling>();
                for (int i = 0; i < temp.Count; i++)
                {
                    //新种群中的individual在父种群有，可以添加的情况
                    SeruScheduling  tempSolution = (SeruScheduling)(temp[i]).Clone();
                    //新种群中的individual在父种群有，不可以添加的情况
                    if (!ifSolutionExistedInPopulationWithObjectiveValue(temp1, tempSolution))
                    {
                        temp1.Add(tempSolution);
                    }
                }
                //------非支配排序------
                for (int i = 0; i < temp1.Count; i++)
                {
                    SeruScheduling iIndividual = temp1[i];
                    iIndividual.frontNumber = 0;                                                        //每次计算pareto解时，frontNumber重值为0
                    iIndividual.numOfDonimateIndividual = 0;                                  //每次计算pareto解时，重值为0
                    iIndividual.donimatedSet = new List<SeruScheduling>();           //每次计算pareto解时，重值为空
                    iIndividual.distanceOfCrowd = 0;
                }
                for (int p = 0; p < temp1.Count; p++)
                {
                    SeruScheduling pIndividual = temp1[p];
                    for (int q = 0; q < temp1.Count; q++)
                    {
                        SeruScheduling qIndividual = temp1[q];
                        if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                        {
                            pIndividual.donimatedSet.Add(qIndividual);          //qIndividual加入到pIndividual的支配解中
                        }
                        else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                        {
                            pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                        }
                    }
                    if (pIndividual.numOfDonimateIndividual == 0)               //不被任何个体支配，加入到第1层front中。
                    {
                        pIndividual.frontNumber = 1;
                        masterPopulation_SS.Add(pIndividual);
                    }
                }
                //------更新协同进化个体------
                SeruScheduling temSolution1 = new SeruScheduling();
                SeruScheduling temSolution2 = new SeruScheduling();
                temSolution1.throughPutTime = double.MaxValue;
                temSolution2.labourTime = double.MaxValue;
                double temSumOfThroughPutTime = 0;
                double temSumOfLabourTime = 0;
                for (int i = 0; i < masterPopulation_SS.Count; i++)
                {
                    if ((masterPopulation_SS[i]).throughPutTime < temSolution1.throughPutTime)
                    {
                        temSolution1 = masterPopulation_SS[i];
                    }
                    if ((masterPopulation_SS[i]).labourTime < temSolution2.labourTime)
                    {
                        temSolution2 = masterPopulation_SS[i];
                    }
                    temSumOfThroughPutTime = temSumOfThroughPutTime + masterPopulation_SS[i].throughPutTime;
                    temSumOfLabourTime = temSumOfLabourTime + masterPopulation_SS[i].labourTime;
                }
                result.mCmax_SS = (SeruScheduling)temSolution1.Clone();
                result.mTLH_SS = (SeruScheduling)temSolution2.Clone();
                temSumOfThroughPutTime = temSumOfThroughPutTime / (masterPopulation_SS.Count);
                temSumOfLabourTime = temSumOfLabourTime / (masterPopulation_SS.Count);

                double temvalue = double.MaxValue;
                double currentValue = 0;

                for (int i = 0; i < masterPopulation_SS.Count; i++)
                {
                    currentValue = Math.Pow(((masterPopulation_SS[i]).throughPutTime - temSumOfThroughPutTime), 2) + Math.Pow(((masterPopulation_SS[i]).labourTime - temSumOfLabourTime), 2);
                    if (currentValue < temvalue)
                    {
                        result.aveND_SS = (SeruScheduling)masterPopulation_SS[i].Clone();
                        temvalue = currentValue;
                    }
                }
                result.masterPopulation_SS = DeepCopyByBin<List<SeruScheduling>>(masterPopulation_SS);
                DateTime end = DateTime.Now;
                TimeSpan t = end - begin;
                Console.WriteLine(t);
            }
            return result;
        }
        /// <summary>
        /// 通过NSGA3进化赛汝调度种群
        /// </summary>
        /// <param name="masterPopulation"></param>
        /// <param name="bestFormation"></param>
        /// <param name="parentSchedules"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public ResultsForMultiThreading getNextScheduleGenerationByNSGA3(List<SeruScheduling> masterPopulation, SeruFormation bestFormation, List<SeruScheduling> parentSchedules, String name)
        {
            Console.WriteLine(name);
            ResultsForMultiThreading resultsForMultiThreading = new ResultsForMultiThreading();
            List<SeruScheduling> cloneParents = DeepCopyByBin<List<SeruScheduling>>(parentSchedules);
            List<SeruScheduling> parents = DeepCopyByBin<List<SeruScheduling>>(parentSchedules);
            //----Step 1: 初始化当前解
            for (int p = 0; p < cloneParents.Count; p++)
            {
                SeruScheduling tempParent = cloneParents[p];
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                caculateFitness(bestFormation, tempParent);
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
            }
            //---Step 2: 精英策略：选择一对精英，一个来自父代种群，一个来自当前子代种群。
            while (parentSchedules.Count > 0)
            {
                SeruScheduling parent1 = new SeruScheduling();
                SeruScheduling parent2 = new SeruScheduling();
                //---从留下来的精英中无放回地挑选父母
                int number1 = random.Next(0, parentSchedules.Count);
                int number2 = random.Next(0, masterPopulation.Count);
                //---从父代种群和当前子代种群中选择个体
                parent1 = (SeruScheduling)(parentSchedules[number1].Clone());
                parent2 = (SeruScheduling)(masterPopulation[number2].Clone());
                //---Step 3: 交叉
                List<List<int>> OffspringCode = new List<List<int>>();
                if (random.NextDouble() < probabilityOfCrossover)
                {
                    OffspringCode = CrossOver(parent1.scheduleCode, parent2.scheduleCode);
                    for (int i = 0; i < OffspringCode.Count; i++)
                    {
                        SeruScheduling tempOffSpring = new SeruScheduling();
                        tempOffSpring.scheduleCode = DeepCopyByBin<List<int>>(OffspringCode[i]);
                        tempOffSpring.BatchesAssignment = (tempOffSpring.produceSeruSchedule(numOfBatches));
                        SeruFormation clonebestFormation = DeepCopyByBin<SeruFormation>(bestFormation);
                        tempOffSpring.currentSeruSet = DeepCopyByBin<List<Seru>>(clonebestFormation.serusSet);
                        caculateFitness(clonebestFormation, tempOffSpring);
                        tempOffSpring.currentSeruSet = DeepCopyByBin<List<Seru>>(clonebestFormation.serusSet);
                        for (int k = 0; k < cloneParents.Count; k++)
                        {
                            if (((tempOffSpring.throughPutTime <= (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime < (cloneParents[k]).labourTime)) || ((tempOffSpring.throughPutTime < (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime <= (cloneParents[k]).labourTime)))
                            {
                                cloneParents.RemoveAt(k);
                                cloneParents.Add(tempOffSpring);
                                break;
                            }
                        }
                    }
                }
                parentSchedules.RemoveAll(x => ((x.throughPutTime == parent1.throughPutTime) && (x.labourTime == parent1.labourTime)));
            }
            //---Step 4: 变异
            mutation_SS(cloneParents, bestFormation);

            //---Step 5: 精英策略
            for (int i = 0; i < cloneParents.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruScheduling tempSolution = (SeruScheduling)(cloneParents[i]).Clone();

                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue(parents, tempSolution))
                {
                    parents.Add(tempSolution);
                }
            }
            //生成newParentSolutions，按照pareto front number and crowd distance 填充新种群
            for (int p = 0; p < parents.Count; p++)
            {
                SeruScheduling tempParent = parents[p];
                tempParent.currentSeruSet = DeepCopyByBin<List<Seru>>(bestFormation.serusSet);
                caculateFitness(bestFormation, tempParent);
            }
            parents = produceNewParentSchesulingByReferencePoint_SS(parents);
            //parents = produceNewParentSchesulingByDominatedSort_SS(parents);
            resultsForMultiThreading.newSchedules = DeepCopyByBin<List<SeruScheduling>>(parents);
            resultsForMultiThreading.nonDominatedSchedules = getParetoOfPopulation_SS(resultsForMultiThreading.newSchedules);
            Console.WriteLine($"------进化结束，当前线程为{Thread.CurrentThread.ManagedThreadId}------");

            return resultsForMultiThreading;
        }
        /// <summary>
        /// 通过非支配排序和参考点获得下一代赛汝调度种群
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruScheduling> produceNewParentSchesulingByReferencePoint_SS(List<SeruScheduling> population)
        {
            List<SeruScheduling> frontSet = new List<SeruScheduling>();                     //记录按pareto层排序的解集
            List<SeruScheduling> newParentSolutions = new List<SeruScheduling>();  //新种群
            //------Step1：生成参考集------
            int M = 2;
            int H = 10;
            List<double[]> referencePointSet = GenerateReferencePoints(M, H);
            //------初始化------
            for (int i = 0; i < population.Count; i++)
            {
                SeruScheduling iIndividual = population[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<SeruScheduling>();                     //每次计算pareto解时，重值为空
                iIndividual.distanceOfCrowd = 0;                                                           //每次计算pareto解时，distanceOfCrowd重置为空
                iIndividual.pointInformation = new Point();
            }
            //------Step2：归一化------
            population = Normalization_SS(population);
            //------Step3：联系个体与参考点------
            population = Association_SS(referencePointSet, population);

            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<SeruScheduling> firstFrontSet = new List<SeruScheduling>();       //第1层解集
            for (int p = 0; p < population.Count; p++)
            {
                SeruScheduling pIndividual = population[p];
                for (int q = 0; q < population.Count; q++)
                {
                    SeruScheduling qIndividual = population[q];
                    if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                    {
                        pIndividual.donimatedSet.Add(qIndividual);                               //qIndividual加入到pIndividual的支配解中
                    }
                    else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                    {
                        pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                    }
                }

                if (pIndividual.numOfDonimateIndividual == 0)                             //不被任何个体支配，加入到第1层front中。
                {
                    pIndividual.frontNumber = 1;
                    firstFrontSet.Add(pIndividual);
                }
            }

            //如果第1 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((firstFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(firstFrontSet);                                            //第1层加入到newParentSolutions
            }
            else
            {
                int countOfParent = newParentSolutions.Count;
                int K = numOfPopular - countOfParent;
                newParentSolutions = FillNewParentSchedules(K, referencePointSet, newParentSolutions, firstFrontSet);
                return newParentSolutions;
            }

            //------生成第2层pareto解集------
            List<SeruScheduling> nextFrontSet = DeepCopyByBin<List<SeruScheduling>>(produceNextFrontNumber_SS(firstFrontSet));
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                                //第2层加入到newParentSolutions

            }
            else
            {
                int countOfParent = newParentSolutions.Count;
                int K = numOfPopular - countOfParent;
                newParentSolutions = FillNewParentSchedules(K, referencePointSet, newParentSolutions, nextFrontSet);
                return newParentSolutions;
            }

            while (nextFrontSet.Count != 0)
            {
                nextFrontSet = produceNextFrontNumber_SS(nextFrontSet);                   //继续生成其它层pareto解集
                if (nextFrontSet.Count != 0)
                {
                    //如果下层front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
                    if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
                    {
                        newParentSolutions.AddRange(nextFrontSet);                                           //该层加入到newParentSolutions
                    }
                    else
                    {
                        int countOfParent = newParentSolutions.Count;
                        int K = numOfPopular - countOfParent;
                        newParentSolutions = FillNewParentSchedules(K, referencePointSet, newParentSolutions, nextFrontSet);
                        return newParentSolutions;
                    }
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 通过Niche-Preservation操作填充下一代赛汝调度种群
        /// </summary>
        /// <param name="K"></param>
        /// <param name="referencePointSet"></param>
        /// <param name="newParentSolutions"></param>
        /// <param name="nextFrontSet"></param>
        /// <returns></returns>
        public List<SeruScheduling> FillNewParentSchedules(int K, List<double[]> referencePointSet, List<SeruScheduling> newParentSolutions, List<SeruScheduling> nextFrontSet) 
        {
            if (newParentSolutions.Count == 0) 
            {
                for (int i =0;i < K;i++) 
                {
                    newParentSolutions.Add(nextFrontSet[i]);
                }
                return newParentSolutions;
            }
            List<ReferencePoint> referencePoints = CalculateNichesOfReferencePoints(referencePointSet, newParentSolutions);
            for (int l = 0; l < referencePoints.Count; l++)
            {
                referencePoints[l] = FindSchedulesForSelection(referencePoints[l], nextFrontSet);
            }
            int k = 0;
            //------选择K个个体补充种群------
            while (k < K)
            {
                //------找出Niches最小的参考点------
                int minNiches = 100000;
                ReferencePoint minNichesForReference = new ReferencePoint();
                //minNichesForReference.schedulesForSelection = new List<SeruScheduling>();
                for (int j = 0; j < referencePoints.Count; j++)
                {
                    if (referencePoints[j].NichesOfSeruScheduling < minNiches)
                    {
                        minNiches = referencePoints[j].NichesOfSeruScheduling;
                        minNichesForReference = (ReferencePoint)referencePoints[j].Clone();
                    }
                }
                //------找到NearestReferencePoints中等于minNichesForReference的赛汝调度------
                if (minNichesForReference.schedulesForSelection == null) 
                {
                    minNichesForReference.schedulesForSelection = new List<SeruScheduling>();
                    k++;
                }
                if ((minNichesForReference.schedulesForSelection.Count == 0))
                {
                    //重新选择参考点
                    referencePoints.RemoveAll(x => (x.Coordinates[0] == minNichesForReference.Coordinates[0]) && (x.Coordinates[1] == minNichesForReference.Coordinates[1]));
                    continue;
                }
                else
                {
                    //------判断这个参考点的小生境是否为0------
                    if (minNichesForReference.NichesOfSeruScheduling == 0)
                    {
                        //选择距离该参考点最小的赛汝调度加入种群
                        //计算参考点到当前解的距离
                        double minValue = 100000000;
                        SeruScheduling minDiatanceSchedule = new SeruScheduling();
                        for (int s = 0; s < minNichesForReference.schedulesForSelection.Count; s++)
                        {
                            SeruScheduling temSchedule = minNichesForReference.schedulesForSelection[s];
                            double d = Math.Sqrt((temSchedule.pointInformation.NCoordinates[0] - minNichesForReference.Coordinates[0]) * (temSchedule.pointInformation.NCoordinates[0] - minNichesForReference.Coordinates[0]) + (temSchedule.pointInformation.NCoordinates[1] - minNichesForReference.Coordinates[1]) * (temSchedule.pointInformation.NCoordinates[1] - minNichesForReference.Coordinates[1]));
                            if (d < minValue)
                            {
                                minValue = d;
                                minDiatanceSchedule = (SeruScheduling)temSchedule.Clone();
                            }
                        }
                        newParentSolutions.Add(minDiatanceSchedule);
                        for (int j = 0;j < minNichesForReference.schedulesForSelection.Count;j++) 
                        {
                            if ((minNichesForReference.schedulesForSelection[j].pointInformation.Coordinates[0] == minDiatanceSchedule.pointInformation.Coordinates[0]) && (minNichesForReference.schedulesForSelection[j].pointInformation.Coordinates[1] == minDiatanceSchedule.pointInformation.Coordinates[1])) 
                            {
                                minNichesForReference.schedulesForSelection.RemoveAt(j);
                                break;
                            }
                        }
                        //minNichesForReference.schedulesForSelection.RemoveAll(x => (x.pointInformation.Coordinates[0] == minDiatanceSchedule.pointInformation.Coordinates[0]) && (x.pointInformation.Coordinates[1] == minDiatanceSchedule.pointInformation.Coordinates[1]));
                    }
                    else
                    {
                        //随机选择一个赛汝调度个体加入种群
                        newParentSolutions.Add(minNichesForReference.schedulesForSelection[0]);
                        minNichesForReference.schedulesForSelection.RemoveAt(0);
                    }
                    k++;
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 对赛汝调度种群个体的归一化
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruScheduling> Normalization_SS(List<SeruScheduling> population) 
        {
            List<SeruScheduling> clonePopulation = DeepCopyByBin<List<SeruScheduling>>(population);
            //------计算理想点------
            double[] idealPoint = new double[2];
            clonePopulation = clonePopulation.OrderBy(x => x.throughPutTime).ToList();
            idealPoint[0] = clonePopulation[0].throughPutTime;
            clonePopulation = clonePopulation.OrderBy(x => x.labourTime).ToList();
            idealPoint[1] = clonePopulation[0].labourTime;

            //------计算极值点------
            List<Point> extremePoints = new List<Point>();

            double[] w1 = new double[] { 1, 0.000001 };
            double[] w2 = new double[] { 0.000001, 1 };

            //计算每个个体距离X轴的距离和距离Y轴的距离
            for (int i = 0; i < clonePopulation.Count; i++)
            {
                Point point = new Point();
                point.ID = i;
                point.Coordinates = new double[] { clonePopulation[i].throughPutTime, clonePopulation[i].labourTime };
                point.TCoordinates = new double[] { clonePopulation[i].throughPutTime - idealPoint[0], clonePopulation[i].labourTime - idealPoint[1] };

                List<double> tem1 = new List<double>();
                for (int j = 0; j < 2; j++)
                {
                    tem1.Add((double)point.TCoordinates[j] / (double)w1[j]);
                }
                point.DistanceX = tem1.Max();

                List<double> tem2 = new List<double>();
                for (int j = 0; j < 2; j++)
                {
                    tem2.Add((double)point.TCoordinates[j] / (double)w2[j]);
                }
                point.DistanceY = tem2.Max();

                clonePopulation[i].pointInformation = (Point)point.Clone();
            }

            //获得极值点
            Point minDistanceX = new Point();
            Point minDistanceY = new Point();
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double a1 = double.MinValue;
            double a2 = double.MinValue;

            for (int i = 0; i < clonePopulation.Count; i++)
            {
                if (a1 < clonePopulation[i].pointInformation.Coordinates[0])
                {
                    a1 = clonePopulation[i].pointInformation.Coordinates[0];
                }
                if (a2 < clonePopulation[i].pointInformation.Coordinates[1])
                {
                    a2 = clonePopulation[i].pointInformation.Coordinates[1];
                }

                if (minX > clonePopulation[i].pointInformation.DistanceX)
                {
                    minX = clonePopulation[i].pointInformation.DistanceX;
                    minDistanceX = (Point)clonePopulation[i].pointInformation.Clone();
                }
                if (minY > clonePopulation[i].pointInformation.DistanceY)
                {
                    minY = clonePopulation[i].pointInformation.DistanceY;
                    minDistanceY = (Point)clonePopulation[i].pointInformation.Clone();
                }

            }
            extremePoints.Add(minDistanceX);
            extremePoints.Add(minDistanceY);

            //截距不存在，则设置为该目标上的最大值
            List<double> a = new List<double>() { a1, a2 };
            //------归一化------
            for (int s = 0; s < clonePopulation.Count; s++)
            {
                clonePopulation[s].pointInformation.NCoordinates = new double[2];
                double tem0 = a[0] - idealPoint[0];
                double tem1 = a[1] - idealPoint[1];
                clonePopulation[s].pointInformation.NCoordinates[0] = clonePopulation[s].pointInformation.TCoordinates[0] / tem0;
                clonePopulation[s].pointInformation.NCoordinates[1] = clonePopulation[s].pointInformation.TCoordinates[1] / tem1;
            }
            return clonePopulation;
        }
        /// <summary>
        /// 找到赛汝调度种群中每个个体距离最近的参考点和参考线
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruScheduling> Association_SS(List<double[]> referencePointSet, List<SeruScheduling> population) 
        {
            List<SeruScheduling> clonePopulation = DeepCopyByBin<List<SeruScheduling>>(population);
            //计算种群中每个个体距离参考点的距离以及距离最近的参考点（可能为0,1或者多个）
            for (int s = 0;s < clonePopulation.Count;s++) 
            {
                clonePopulation[s].pointInformation.DistanceToReferencePoints = new List<double>();
                clonePopulation[s].pointInformation.NearestReferencePoints = new List<double[]>();
                double minDistance = 1000000;
                for (int w = 0; w < referencePointSet.Count; w++) 
                {
                    double r = (clonePopulation[s].pointInformation.NCoordinates[0] * referencePointSet[w][0] + clonePopulation[s].pointInformation.NCoordinates[1] * referencePointSet[w][1]) / (referencePointSet[w][0] * referencePointSet[w][0] + referencePointSet[w][1] * referencePointSet[w][1]);
                    double d1 = clonePopulation[s].pointInformation.NCoordinates[0] - r * referencePointSet[w][0];
                    double d2 = clonePopulation[s].pointInformation.NCoordinates[1] - r * referencePointSet[w][1];
                    double d = Math.Sqrt(d1*d1+d2*d2);
                    if (minDistance >d)
                    {
                        minDistance =d;
                        clonePopulation[s].pointInformation.NearestReferencePoints = new List<double[]>();
                        clonePopulation[s].pointInformation.NearestReferencePoints.Add(referencePointSet[w]);
                    }
                    else if (d == minDistance)
                    {
                        clonePopulation[s].pointInformation.NearestReferencePoints.Add(referencePointSet[w]);
                    }

                    clonePopulation[s].pointInformation.DistanceToReferencePoints.Add(d);
                }
            }
            return clonePopulation;
        }
        /// <summary>
        /// 计算参考点的小生境
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        public List<ReferencePoint> CalculateNichesOfReferencePoints(List<double[]> referencePointSet,List<SeruScheduling> population) 
        {
            List<ReferencePoint> references = new List<ReferencePoint>();
            for (int i = 0;i < referencePointSet.Count;i++) 
            {
                ReferencePoint temPoint = new ReferencePoint();
                temPoint.ID = i;
                temPoint.Coordinates = referencePointSet[i];
                temPoint.NichesOfSeruScheduling = 0;
                //temPoint.schedules = new List<SeruScheduling>();
                //计算种群中和这个参考点相联系的个体数量，即为 Niche
                for (int s = 0; s < population.Count;s++) 
                {
                    SeruScheduling tempSchedules = (SeruScheduling)population[s].Clone();
                    for (int j =0;j < tempSchedules.pointInformation.NearestReferencePoints.Count;j++) 
                    {
                        if ((tempSchedules.pointInformation.NearestReferencePoints[j][0]== temPoint.Coordinates[0])&& (tempSchedules.pointInformation.NearestReferencePoints[j][1] == temPoint.Coordinates[1])) 
                        {
                            temPoint.NichesOfSeruScheduling = temPoint.NichesOfSeruScheduling + 1;
                           // temPoint.schedules.Add(tempSchedules);
                        }
                    }
                }
                //temPoint.NichesOfSeruScheduling = temPoint.schedules.Count;
                references.Add(temPoint);
            }
            return references;
        }
        /// <summary>
        /// 找到NearestReferencePoints中等于minNichesForReference的赛汝调度
        /// </summary>
        /// <param name="minNichesForReference"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public ReferencePoint FindSchedulesForSelection(ReferencePoint minNichesForReference,List<SeruScheduling> population) 
        {
            ReferencePoint temPoint = (ReferencePoint)minNichesForReference.Clone();
            temPoint.schedulesForSelection = new List<SeruScheduling>();
            for (int s = 0; s < population.Count; s++)
            {
                SeruScheduling tempSchedules = (SeruScheduling)population[s].Clone();
                for (int j = 0; j < tempSchedules.pointInformation.NearestReferencePoints.Count; j++)
                {
                    if ((tempSchedules.pointInformation.NearestReferencePoints[j][0] == temPoint.Coordinates[0]) && (tempSchedules.pointInformation.NearestReferencePoints[j][1] == temPoint.Coordinates[1]))
                    {
                        temPoint.schedulesForSelection.Add(tempSchedules);
                    }
                }
            }
            return temPoint;
        }
       /// <summary>
        /// 通过NSGA3进化赛汝构造种群
        /// </summary>
        /// <param name="masterPopulation"></param>
        /// <param name="bestFormation"></param>
        /// <param name="parentSchedules"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public ResultsForMultiThreading getNextFormationGenerationByNSGA3(List<SeruFormation> masterPopulation, SeruScheduling  bestSchedule, List<SeruFormation> parentFormations, String name)
        {
            Console.WriteLine(name);
            ResultsForMultiThreading resultsForMultiThreading = new ResultsForMultiThreading();
            List<SeruFormation> cloneParents = DeepCopyByBin<List<SeruFormation>>(parentFormations);
            List<SeruFormation> parents = DeepCopyByBin<List<SeruFormation>>(parentFormations);
            //----Step 1: 初始化当前解
            for (int p = 0; p < cloneParents.Count; p++)
            {
                SeruFormation tempParent = cloneParents[p];
                tempParent.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                caculateFitness(tempParent, bestSchedule);

            }
            //---Step 2: 精英策略：选择一对精英，一个来自父代种群，一个来自当前子代种群。
            while (parentFormations.Count > 0)
            {
                SeruFormation parent1 = new SeruFormation();
                SeruFormation parent2 = new SeruFormation();
                //---从留下来的精英中无放回地挑选父母
                int number1 = random.Next(0, parentFormations.Count);
                int number2 = random.Next(0, masterPopulation.Count);
                //---从父代种群和当前子代种群中选择个体
                parent1 = DeepCopyByBin<SeruFormation>(parentFormations[number1]);
                parent2 = DeepCopyByBin<SeruFormation>(masterPopulation[number2]);

                //---Step 3: 交叉
                List<List<int>> OffspringCode = new List<List<int>>();
                if (random.NextDouble() < probabilityOfCrossover)
                {
                    OffspringCode = CrossOver(parent1.formationCode, parent2.formationCode);
                    for (int i = 0; i < OffspringCode.Count; i++)
                    {
                        SeruFormation tempOffSpring = new SeruFormation();
                        tempOffSpring.formationCode = DeepCopyByBin<List<int>>(OffspringCode[i]);
                        tempOffSpring.serusSet = tempOffSpring.produceSerusSet(numOfWorkers);
                        tempOffSpring.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                        caculateFitness(tempOffSpring, bestSchedule);
                        for (int k = 0; k < cloneParents.Count; k++)
                        {
                            if (((tempOffSpring.throughPutTime <= (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime < (cloneParents[k]).labourTime)) || ((tempOffSpring.throughPutTime < (cloneParents[k]).throughPutTime) && (tempOffSpring.labourTime <= (cloneParents[k]).labourTime)))
                            {
                                cloneParents.RemoveAt(k);
                                cloneParents.Add(tempOffSpring);
                                break;
                            }
                        }
                    }
                }
                parentFormations.RemoveAll(x => ((x.throughPutTime == parent1.throughPutTime) && (x.labourTime == parent1.labourTime)));
            }
            //---Step 4: 变异
            mutation_SF(cloneParents, bestSchedule);
            //---Step5: 精英策略
            for (int i = 0; i < cloneParents.Count; i++)
            {
                //新种群中的individual在父种群有，可以添加的情况
                SeruFormation tempSolution = (SeruFormation)(cloneParents[i]).Clone();

                //新种群中的individual在父种群有，不可以添加的情况
                if (!ifSolutionExistedInPopulationWithObjectiveValue_SF(parents, tempSolution))
                {
                    parents.Add(tempSolution);
                }
            }
            //生成newParentSolutions，按照pareto front number and crowd distance 填充新种群
            for (int p = 0; p < parents.Count; p++)
            {
                SeruFormation tempParent = parents[p];
                tempParent.currentBatchSchedule = DeepCopyByBin<List<List<int>>>(bestSchedule.BatchesAssignment);
                caculateFitness(tempParent, bestSchedule);
            }
            parents = produceNewParentFormationByReferencePoint_SF(parents);
            //parents = produceNewParentSchesulingByDominatedSort_SS(parents);
            resultsForMultiThreading.newFormations = DeepCopyByBin<List<SeruFormation >>(parents);
            resultsForMultiThreading.nonDominatedFormations = getParetoOfPopulation_SF(resultsForMultiThreading.newFormations);
            Console.WriteLine($"------进化结束，当前线程为{Thread.CurrentThread.ManagedThreadId}------");

            return resultsForMultiThreading;
        }
        /// <summary>
        /// 通过非支配排序和参考点获得下一代赛汝构造种群
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> produceNewParentFormationByReferencePoint_SF(List<SeruFormation> population)
        {
            List<SeruFormation> frontSet = new List<SeruFormation>();                     //记录按pareto层排序的解集
            List<SeruFormation> newParentSolutions = new List<SeruFormation>();  //新种群
            //------Step1：生成参考集------
            int M = 2;
            int H = 10;
            List<double[]> referencePointSet = GenerateReferencePoints(M, H);
            //------初始化------
            for (int i = 0; i < population.Count; i++)
            {
                SeruFormation iIndividual = population[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<SeruFormation>();                     //每次计算pareto解时，重值为空
                iIndividual.distanceOfCrowd = 0;                                                           //每次计算pareto解时，distanceOfCrowd重置为空
                iIndividual.pointInformation = new Point();
            }
            //------Step2：归一化------
            population = Normalization_SF(population);
            //------Step3：联系个体与参考点------
            population = Association_SF(referencePointSet, population);

            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<SeruFormation> firstFrontSet = new List<SeruFormation>();       //第1层解集
            for (int p = 0; p < population.Count; p++)
            {
                SeruFormation pIndividual = population[p];
                for (int q = 0; q < population.Count; q++)
                {
                    SeruFormation qIndividual = population[q];
                    if (((pIndividual.throughPutTime <= qIndividual.throughPutTime) && (pIndividual.labourTime < qIndividual.labourTime)) || ((pIndividual.throughPutTime < qIndividual.throughPutTime) && (pIndividual.labourTime <= qIndividual.labourTime)))       //pIndividual 支配qIndividual
                    {
                        pIndividual.donimatedSet.Add(qIndividual);                               //qIndividual加入到pIndividual的支配解中
                    }
                    else if (((qIndividual.throughPutTime <= pIndividual.throughPutTime) && (qIndividual.labourTime < pIndividual.labourTime)) || ((qIndividual.throughPutTime < pIndividual.throughPutTime) && (qIndividual.labourTime <= pIndividual.labourTime)))     //qIndividual 支配pIndividual
                    {
                        pIndividual.numOfDonimateIndividual = pIndividual.numOfDonimateIndividual + 1;
                    }
                }

                if (pIndividual.numOfDonimateIndividual == 0)                             //不被任何个体支配，加入到第1层front中。
                {
                    pIndividual.frontNumber = 1;
                    firstFrontSet.Add(pIndividual);
                }
            }

            //如果第1 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((firstFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(firstFrontSet);                                            //第1层加入到newParentSolutions
            }
            else
            {
                int countOfParent = newParentSolutions.Count;
                int K = numOfPopular - countOfParent;
                newParentSolutions = FillNewParentFormation(K, referencePointSet, newParentSolutions, firstFrontSet);
                return newParentSolutions;
            }

            //------生成第2层pareto解集------
            List<SeruFormation> nextFrontSet = DeepCopyByBin<List<SeruFormation>>(produceNextFrontNumber_SF(firstFrontSet));
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                                //第2层加入到newParentSolutions
            }
            else
            {
                int countOfParent = newParentSolutions.Count;
                int K = numOfPopular - countOfParent;
                newParentSolutions = FillNewParentFormation(K, referencePointSet, newParentSolutions, nextFrontSet);
                return newParentSolutions;
            }

            while (nextFrontSet.Count != 0)
            {
                nextFrontSet = produceNextFrontNumber_SF(nextFrontSet);                   //继续生成其它层pareto解集
                if (nextFrontSet.Count != 0)
                {
                    //如果下层front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
                    if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
                    {
                        newParentSolutions.AddRange(nextFrontSet);                                           //该层加入到newParentSolutions
                    }
                    else
                    {
                        int countOfParent = newParentSolutions.Count;
                        int K = numOfPopular - countOfParent;
                        newParentSolutions = FillNewParentFormation(K, referencePointSet, newParentSolutions, nextFrontSet);
                        return newParentSolutions;
                    }
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 通过Niche-Preservation操作填充下一代赛汝构造种群
        /// </summary>
        /// <param name="K"></param>
        /// <param name="referencePointSet"></param>
        /// <param name="newParentSolutions"></param>
        /// <param name="nextFrontSet"></param>
        /// <returns></returns>
        public List<SeruFormation> FillNewParentFormation(int K, List<double[]> referencePointSet, List<SeruFormation > newParentSolutions, List<SeruFormation> nextFrontSet) 
        {
            if (newParentSolutions.Count == 0) 
            {
                for (int i =0;i < K;i++) 
                {
                    newParentSolutions.Add(nextFrontSet[i]);
                }
                return newParentSolutions;
            }
            List<ReferencePoint> referencePoints = CalculateNichesOfReferencePoints_SF(referencePointSet, newParentSolutions);
            for (int l = 0; l < referencePoints.Count; l++)
            {
                referencePoints[l] = FindFormationForSelection(referencePoints[l], nextFrontSet);
            }
            int k = 0;
            //------选择K个个体补充种群------
            while (k < K)
            {
                //------找出Niches最小的参考点------
                int minNiches = 100000;
                ReferencePoint minNichesForReference = new ReferencePoint();
                for (int j = 0; j < referencePoints.Count; j++)
                {
                    if (referencePoints[j].NichesOfSeruFormation < minNiches)
                    {
                        minNiches = referencePoints[j].NichesOfSeruFormation;
                        minNichesForReference = (ReferencePoint)referencePoints[j].Clone();
                    }
                }
                //------找到NearestReferencePoints中等于minNichesForReference的赛汝调度------
                if (minNichesForReference.formationsForSelection.Count == 0)
                {
                    //重新选择参考点
                    referencePoints.RemoveAll(x => (x.Coordinates[0] == minNichesForReference.Coordinates[0]) && (x.Coordinates[1] == minNichesForReference.Coordinates[1]));
                    continue;
                }
                else
                {
                    //------判断这个参考点的小生境是否为0------
                    if (minNichesForReference.NichesOfSeruFormation== 0)
                    {
                        //选择距离该参考点最小的赛汝构造加入种群
                        //计算参考点到当前解的距离
                        double minValue = 100000000;
                        SeruFormation  minDiatanceFormation = new SeruFormation();
                        for (int s = 0; s < minNichesForReference.formationsForSelection.Count; s++)
                        {
                            SeruFormation  temFormation = minNichesForReference.formationsForSelection[s];
                            double d = Math.Sqrt((temFormation.pointInformation.NCoordinates[0] - minNichesForReference.Coordinates[0]) * (temFormation.pointInformation.NCoordinates[0] - minNichesForReference.Coordinates[0]) + (temFormation.pointInformation.NCoordinates[1] - minNichesForReference.Coordinates[1]) * (temFormation.pointInformation.NCoordinates[1] - minNichesForReference.Coordinates[1]));
                            if (d < minValue)
                            {
                                minValue = d;
                                minDiatanceFormation = (SeruFormation)temFormation.Clone();
                            }
                        }
                        newParentSolutions.Add(minDiatanceFormation);
                        for (int j = 0;j < minNichesForReference.formationsForSelection.Count;j++) 
                        {
                            if ((minNichesForReference.formationsForSelection[j].pointInformation.Coordinates[0] == minDiatanceFormation.pointInformation.Coordinates[0]) && (minNichesForReference.formationsForSelection[j].pointInformation.Coordinates[1] == minDiatanceFormation.pointInformation.Coordinates[1])) 
                            {
                                minNichesForReference.formationsForSelection.RemoveAt(j);
                                break;
                            }
                        }
                    }
                    else
                    {
                        //随机选择一个赛汝调度个体加入种群
                        newParentSolutions.Add(minNichesForReference.formationsForSelection[0]);
                        minNichesForReference.formationsForSelection.RemoveAt(0);
                    }
                    k++;
                }
            }
            return newParentSolutions;
        }
        /// <summary>
        /// 对赛汝构造种群个体的归一化
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> Normalization_SF(List<SeruFormation> population) 
        {
            List<SeruFormation> clonePopulation = DeepCopyByBin<List<SeruFormation>>(population);
            //------计算理想点------
            double[] idealPoint = new double[2];
            clonePopulation = clonePopulation.OrderBy(x => x.throughPutTime).ToList();
            idealPoint[0] = clonePopulation[0].throughPutTime;
            clonePopulation = clonePopulation.OrderBy(x => x.labourTime).ToList();
            idealPoint[1] = clonePopulation[0].labourTime;

            //------计算极值点------
            List<Point> extremePoints = new List<Point>();

            double[] w1 = new double[] { 1, 0.000001 };
            double[] w2 = new double[] { 0.000001, 1 };

            //计算每个个体距离X轴的距离和距离Y轴的距离
            for (int i = 0; i < clonePopulation.Count; i++)
            {
                Point point = new Point();
                point.ID = i;
                point.Coordinates = new double[] { clonePopulation[i].throughPutTime, clonePopulation[i].labourTime };
                point.TCoordinates = new double[] { clonePopulation[i].throughPutTime - idealPoint[0], clonePopulation[i].labourTime - idealPoint[1] };

                List<double> tem1 = new List<double>();
                for (int j = 0; j < 2; j++)
                {
                    tem1.Add((double)point.TCoordinates[j] / (double)w1[j]);
                }
                point.DistanceX = tem1.Max();

                List<double> tem2 = new List<double>();
                for (int j = 0; j < 2; j++)
                {
                    tem2.Add((double)point.TCoordinates[j] / (double)w2[j]);
                }
                point.DistanceY = tem2.Max();

                clonePopulation[i].pointInformation = (Point)point.Clone();
            }

            //获得极值点
            Point minDistanceX = new Point();
            Point minDistanceY = new Point();
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double a1 = double.MinValue;
            double a2 = double.MinValue;

            for (int i = 0; i < clonePopulation.Count; i++)
            {
                if (a1 < clonePopulation[i].pointInformation.Coordinates[0])
                {
                    a1 = clonePopulation[i].pointInformation.Coordinates[0];
                }
                if (a2 < clonePopulation[i].pointInformation.Coordinates[1])
                {
                    a2 = clonePopulation[i].pointInformation.Coordinates[1];
                }

                if (minX > clonePopulation[i].pointInformation.DistanceX)
                {
                    minX = clonePopulation[i].pointInformation.DistanceX;
                    minDistanceX = (Point)clonePopulation[i].pointInformation.Clone();
                }
                if (minY > clonePopulation[i].pointInformation.DistanceY)
                {
                    minY = clonePopulation[i].pointInformation.DistanceY;
                    minDistanceY = (Point)clonePopulation[i].pointInformation.Clone();
                }

            }
            extremePoints.Add(minDistanceX);
            extremePoints.Add(minDistanceY);

            //截距不存在，则设置为该目标上的最大值
            List<double> a = new List<double>() { a1, a2 };
            //------归一化------
            for (int s = 0; s < clonePopulation.Count; s++)
            {
                clonePopulation[s].pointInformation.NCoordinates = new double[2];
                double tem0 = a[0] - idealPoint[0];
                double tem1 = a[1] - idealPoint[1];
                clonePopulation[s].pointInformation.NCoordinates[0] = clonePopulation[s].pointInformation.TCoordinates[0] / tem0;
                clonePopulation[s].pointInformation.NCoordinates[1] = clonePopulation[s].pointInformation.TCoordinates[1] / tem1;
            }
            return clonePopulation;
        }
        /// <summary>
        /// 找到赛汝调度种群中每个个体距离最近的参考点和参考线
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> Association_SF(List<double[]> referencePointSet, List<SeruFormation> population) 
        {
            List<SeruFormation> clonePopulation = DeepCopyByBin<List<SeruFormation>>(population);
            //计算种群中每个个体距离参考点的距离以及距离最近的参考点（可能为0,1或者多个）
            for (int s = 0;s < clonePopulation.Count;s++) 
            {
                clonePopulation[s].pointInformation.DistanceToReferencePoints = new List<double>();
                clonePopulation[s].pointInformation.NearestReferencePoints = new List<double[]>();
                double minDistance = 1000000;
                for (int w = 0; w < referencePointSet.Count; w++) 
                {
                    double r = (clonePopulation[s].pointInformation.NCoordinates[0] * referencePointSet[w][0] + clonePopulation[s].pointInformation.NCoordinates[1] * referencePointSet[w][1]) / (referencePointSet[w][0] * referencePointSet[w][0] + referencePointSet[w][1] * referencePointSet[w][1]);
                    double d1 = clonePopulation[s].pointInformation.NCoordinates[0] - r * referencePointSet[w][0];
                    double d2 = clonePopulation[s].pointInformation.NCoordinates[1] - r * referencePointSet[w][1];
                    double d = Math.Sqrt(d1*d1+d2*d2);
                    if (minDistance >d)
                    {
                        minDistance =d;
                        clonePopulation[s].pointInformation.NearestReferencePoints = new List<double[]>();
                        clonePopulation[s].pointInformation.NearestReferencePoints.Add(referencePointSet[w]);
                    }
                    else if (d == minDistance)
                    {
                        clonePopulation[s].pointInformation.NearestReferencePoints.Add(referencePointSet[w]);
                    }
                    clonePopulation[s].pointInformation.DistanceToReferencePoints.Add(d);
                }
            }
            return clonePopulation;
        }
        /// <summary>
        /// 计算参考点的小生境
        /// </summary>
        /// <param name="referencePointSet"></param>
        /// <param name="population"></param>
        public List<ReferencePoint> CalculateNichesOfReferencePoints_SF(List<double[]> referencePointSet,List<SeruFormation> population) 
        {
            List<ReferencePoint> references = new List<ReferencePoint>();
            for (int i = 0;i < referencePointSet.Count;i++) 
            {
                ReferencePoint temPoint = new ReferencePoint();
                temPoint.ID = i;
                temPoint.Coordinates = referencePointSet[i];
                temPoint.NichesOfSeruFormation = 0;

                //计算种群中和这个参考点相联系的个体数量，即为 Niche
                for (int s = 0; s < population.Count;s++) 
                {
                    SeruFormation  tempFormations = (SeruFormation)population[s].Clone();
                    for (int j =0;j < tempFormations.pointInformation.NearestReferencePoints.Count;j++) 
                    {
                        if ((tempFormations.pointInformation.NearestReferencePoints[j][0]== temPoint.Coordinates[0])&& (tempFormations.pointInformation.NearestReferencePoints[j][1] == temPoint.Coordinates[1])) 
                        {
                            temPoint.NichesOfSeruFormation = temPoint.NichesOfSeruFormation + 1;
                        }
                    }
                }
                references.Add(temPoint);
            }
            return references;
        }
        /// <summary>
        /// 找到NearestReferencePoints中等于minNichesForReference的赛汝构造
        /// </summary>
        /// <param name="minNichesForReference"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        public ReferencePoint FindFormationForSelection(ReferencePoint minNichesForReference,List<SeruFormation> population) 
        {
            ReferencePoint temPoint = (ReferencePoint)minNichesForReference.Clone();
            temPoint.formationsForSelection = new List<SeruFormation>();
            for (int s = 0; s < population.Count; s++)
            {
                SeruFormation  tempFormations = (SeruFormation)population[s].Clone();
                for (int j = 0; j < tempFormations.pointInformation.NearestReferencePoints.Count; j++)
                {
                    if ((tempFormations.pointInformation.NearestReferencePoints[j][0] == temPoint.Coordinates[0]) && (tempFormations.pointInformation.NearestReferencePoints[j][1] == temPoint.Coordinates[1]))
                    {
                        temPoint.formationsForSelection.Add(tempFormations);
                    }
                }
            }
            return temPoint;
        }
    }
}
