using NPOI.HSSF.UserModel;
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MOCC_II_For_Seru_Production
{
    public partial class Form1 : Form
    {
        //-----工人数、批次数
        int numOfWorkers;
        int numOfBatches;
        //----赛汝生产的相关参数
        int maxNumOfMultipleTask = 10;                                                                                        //多能工最大值，Seru里tasks大于这个值加工时间就要延长。
        double taskTime = 1.8;
        //种群规模必须为偶数
        int numOfPopular = 50;                                                                                                        //种群大小
        int maxItera =40;                                                                                                                    //最大迭代次数
        Random random = new Random();
        //---批次与产品类型关系的数据/工人与产品类型的熟练程度的数据/多能工系数数据
        DataTable tableBatchToProductType = new DataTable();
        DataTable tableWorkerToProductType = new DataTable();
        DataTable tableWorkerToMultipleTask = new DataTable();
        //父代赛汝构造种群和赛汝调度种群
        List<SeruFormation> parentPopulation_SF = new List<SeruFormation>();
        List<SeruScheduling> parentPopulation_SS = new List<SeruScheduling>();
        //最优的赛汝构造和随机的赛汝构造
        SeruFormation helpIndividual_SF = new SeruFormation();
        //最优的赛汝调度和随机的赛汝调度
        SeruScheduling helpIndividual_SS = new SeruScheduling();
        //外部保存集
        List<SeruFormation> elitist_Formation = new List<SeruFormation>();
        List<SeruScheduling> elitist_Schedule = new List<SeruScheduling>();
        public Form1()
        {
            InitializeComponent();
            readData();
        }
        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            List<int> workers = new List<int> { 8, 10, 15, 20 };
            List<int> batches = new List<int> { 10, 15, 20, 25, 30 };
            int Row = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                numOfWorkers = workers[i];
                for (int j = 0; j < batches.Count; j++)
                {
                    Row++;
                    elitist_Formation = new List<SeruFormation>();
                    elitist_Schedule = new List<SeruScheduling>();
                    numOfBatches = batches[j];
                    MOCC();
                }
            }
        }
        /// <summary>
        /// MOCC
        /// </summary>
        public void MOCC() 
        {
            Console.WriteLine("-------------------------正在进化--------------------------------");
            string name = @"D:\Result_MOCC\MOCC\MOCC_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";

            DateTime beginTime = DateTime.Now;
            parentPopulation_SF = new List<SeruFormation>();
            parentPopulation_SS = new List<SeruScheduling>();

            //------初始化赛汝构造种群和赛汝调度种群------
            for (int i = 0; i < numOfPopular; i++)
            {
                SeruFormation formation1 = new SeruFormation();
                formation1.formationCode = initialFormationCode();                                                                       //初始化构造编码
                formation1.serusSet = formation1.produceSerusSet(numOfWorkers);                                             //通过编码解码成Seru构造
                parentPopulation_SF.Add(formation1);

                SeruScheduling schedule1 = new SeruScheduling();
                schedule1.scheduleCode = initialScheduleCode();                                                                            //初始化调度编码
                schedule1.BatchesAssignment = schedule1.produceSeruSchedule(numOfBatches);                       //通过编码解码成Seru调度
                parentPopulation_SS.Add(schedule1);
            }

            //------初始化协同进化个体------
            helpIndividual_SF = tournament_SF(parentPopulation_SF);
            helpIndividual_SS = tournament_SS(parentPopulation_SS);

            int iterator = 0;
            for (int m = 0; m < maxItera; m++)
            {
                Console.WriteLine($"-==================第{m+1}次迭代================");
                //------进化赛汝调度------
                if (iterator%2==0)
                {
                    //------进化种群------
                    Swap_SS(parentPopulation_SS);
                    //------评价赛汝调度------
                    for (int i = 0; i < numOfPopular; i++) 
                    {
                        SeruScheduling currentSchedule = parentPopulation_SS[i];
                        SeruFormation currentFormation = DeepCopyByBin<SeruFormation>(helpIndividual_SF);
                        caculateFitness(currentFormation, currentSchedule);
                        currentSchedule.currentSeruSet = DeepCopyByBin<List<Seru>>(currentFormation.serusSet);
                    }
                    helpIndividual_SS = DeepCopyByBin<SeruScheduling>(tournament_SS(parentPopulation_SS));
                    //------精英策略------
                    parentPopulation_SS.AddRange(DeepCopyByBin<List<SeruScheduling>>(elitist_Schedule));
                    parentPopulation_SS = produceNewParentSchesulingByDominatedSort_SS(parentPopulation_SS);
                    elitist_Schedule = DeepCopyByBin<List<SeruScheduling>> (getParetoOfPopulation_SS(parentPopulation_SS));
                    iterator++;
                }
                //------进化赛汝构造------
                else 
                {
                    //------进化种群------
                    Swap_SF(parentPopulation_SF);
                    //------评价赛汝调度------
                    for (int i = 0; i < numOfPopular; i++)
                    {
                        SeruFormation currentFormation = parentPopulation_SF[i]; ;
                        SeruScheduling currentSchedule = DeepCopyByBin<SeruScheduling>(helpIndividual_SS); ;
                        caculateFitness(currentFormation, currentSchedule);
                        currentFormation.currentSchedule = DeepCopyByBin<List<List<int>>>(currentSchedule.BatchesAssignment);
                    }
                    helpIndividual_SF = DeepCopyByBin<SeruFormation>(tournament_SF(parentPopulation_SF));
                    //------精英策略------
                    parentPopulation_SF.AddRange(DeepCopyByBin<List<SeruFormation>>(elitist_Formation));
                    parentPopulation_SF = produceNewParentSchesulingByDominatedSort_SF(parentPopulation_SF);
                    elitist_Formation = DeepCopyByBin<List<SeruFormation>>(getParetoOfPopulation_SF(parentPopulation_SF));
                    iterator++;
                }   
            }
            List<Solution> solutions = new List<Solution>();
            for (int i = 0; i < elitist_Schedule.Count; i++ ) 
            {
                Solution solution = new Solution();
                solution.currentSchedule = elitist_Schedule[i];
                solution.throughPutTime = elitist_Schedule[i].throughPutTime;
                solution.labourTime = elitist_Schedule[i].labourTime;
                solutions.Add(solution);
            }
            for (int i =0; i < elitist_Formation.Count; i++) 
            {
                Solution solution = new Solution();
                solution.currentFormation = elitist_Formation[i];
                solution.throughPutTime = elitist_Formation[i].throughPutTime;
                solution.labourTime = elitist_Formation[i].labourTime;
                solutions.Add(solution);
            }
            solutions = DominatedSort(solutions);
            //输出非支配解
            List<Solution> tem = new List<Solution>();
            for (int i = 0; i < solutions.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < tem.Count; j++)
                {
                    if ((((Solution)solutions[i]).throughPutTime == ((Solution)tem[j]).throughPutTime) && (((Solution)solutions[i]).labourTime == ((Solution)tem[j]).labourTime))
                        flag = false;
                }
                if (flag == true)
                    tem.Add(solutions[i]);
            }
            solutions = tem;
            for (int i = 0; i < solutions.Count; i++)
            {
                Console.WriteLine($"{solutions[i].throughPutTime}    {solutions[i].labourTime}");
            }
            DateTime endTime = DateTime.Now;
            TimeSpan t = endTime - beginTime;
            Output(solutions, name, t.TotalSeconds);
        }
         /// <summary>
         /// 读取数据
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
        /// 赛汝调度内部交换
        /// </summary>
        /// <param name="populations"></param>
        /// <param name="bestSchedule"></param>
        public void Swap_SS(List<SeruScheduling> populations)
        {
            for (int i = 0; i < populations.Count; i++)
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
                temp.produceSeruSchedule(numOfBatches);

                populations[i] = (SeruScheduling)temp.Clone();
                populations[i].scheduleCode = DeepCopyByBin<List<int>>(temp.scheduleCode);
            }
        }
        /// <summary>
        /// 赛汝构造内部交换
        /// </summary>
        /// <param name="populations"></param>
        /// <param name="bestSchedule"></param>
        public void Swap_SF(List<SeruFormation> populations)
        {
            for (int i = 0; i < populations.Count; i++)
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

                populations[i] = (SeruFormation)temp.Clone();
                populations[i].formationCode = DeepCopyByBin<List<int>>(temp.formationCode);
            }
        }
        /// <summary>
        ///  对赛汝调度中的个体进行非支配排序
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
            }
            else
            {
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
            if (newParentSolutions.Count==numOfPopular)
            {
                return newParentSolutions;
            }

            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                                //第2层加入到newParentSolutions
            }
            else
            {
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
                    }
                    else
                    {
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
            tempPopulation.OrderBy(x => x.throughPutTime);
            return tempPopulation;
        }
        /// <summary>
        ///  对赛汝构造中的个体进行非支配排序
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
            }
            else
            {
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
            if (newParentSolutions.Count==numOfPopular) 
            {
                return newParentSolutions;
            }
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                         //第2层加入到newParentSolutions
            }
            else
            {
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
                    }
                    else
                    {
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
        /// 锦标赛选择赛汝构造的协助个体
        /// </summary>
        /// <param name="individual1"></param>
        /// <param name="individual2"></param>
        /// <returns></returns>
        public SeruFormation tournament_SF(List<SeruFormation> population)
        {
            int number1 = random.Next(0, population.Count);
            int number2 = random.Next(0, population.Count);
            SeruFormation individual1 = population[number1];
            SeruFormation individual2 = population[number2];
            //1在2前，选1
            if (individual1.frontNumber < individual2.frontNumber)
            {
                return individual1;
            }
            //1在2后，选2
            else
            {
                return individual2;
            }
        }
        /// <summary>
        /// 锦标赛选择赛汝调度的协助个体
        /// </summary>
        /// <param name="individual1"></param>
        /// <param name="individual2"></param>
        /// <returns></returns>
        public SeruScheduling tournament_SS(List<SeruScheduling> population)
        {
            int number1 = random.Next(0, population.Count);
            int number2 = random.Next(0, population.Count);
            SeruScheduling individual1 = population[number1];
            SeruScheduling individual2 = population[number2];
            //1在2前，选1
            if (individual1.frontNumber <= individual2.frontNumber)
            {
                return individual1;
            }
            //1在2后，选2
            else
            {
                return individual2;
            }          
        }
        /// <summary>
        ///  非支配排序
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<Solution> DominatedSort(List<Solution> population)
        {
            List<Solution> frontSet = new List<Solution>();                     //记录按pareto层排序的解集
            List<Solution> newParentSolutions = new List<Solution>();  //新种群

            //------初始化------
            for (int i = 0; i < population.Count; i++)
            {
                Solution iIndividual = population[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<Solution>();                                 //每次计算pareto解时，重值为空
                iIndividual.distanceOfCrowd = 0;                                                           //每次计算pareto解时，distanceOfCrowd重置为空
            }

            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<Solution> firstFrontSet = new List<Solution>();       //第1层解集
            for (int p = 0; p < population.Count; p++)
            {
                Solution pIndividual = population[p];
                for (int q = 0; q < population.Count; q++)
                {
                    Solution qIndividual = population[q];
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
            return firstFrontSet;
        }
        /// <summary>
        /// 输出到Excel中
        /// </summary>
        public void Output(List<Solution> solutions, string name, double t) 
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Test");
            var row = sheet.CreateRow(0);
            var title = row.CreateCell(0);
            int index = 1; //从1行开始写入
            for (int i = 0; i < solutions.Count; i++)
            {
                int x = index + i;
                var rowi = sheet.CreateRow(x);
                var cols = rowi.CreateCell(0);
                cols.SetCellValue(solutions[i].throughPutTime);
                var colss = rowi.CreateCell(1);
                colss.SetCellValue(solutions[i].labourTime);
                var num = rowi.CreateCell(2);
                num.SetCellValue(solutions.Count);
                var time = rowi.CreateCell(3);
                time.SetCellValue(t);
            }
            FileStream file = new FileStream(name, FileMode.OpenOrCreate, FileAccess.Write);
            workbook.Write(file);
            file.Dispose();
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
    }
}
