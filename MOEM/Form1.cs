using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MOEM
{
    public partial class Form1 : Form
    {
        //-----工人数、批次数
        int numOfWorkers;
        int numOfBatches;
        //----赛汝生产的相关参数
        int maxNumOfMultipleTask = 10;
        double taskTime = 1.8;
        //---用于epision 约束法的相关参数
        //总劳动时间的上界
        double b = 10000;
        //最小误差（CPLEX精度问题）
        double minError = 0.1;
        //epision
        double RatioBetweenObjFun = 0.5;

        DataTable tableBatchToProductType = new DataTable();
        DataTable tableWorkerToProductType = new DataTable();
        DataTable tableWorkerToMultipleTask = new DataTable();

        public Form1()
        {
            InitializeComponent();
            readData();
        }
        private void MOEM_Click(object sender, EventArgs e)
        {
            List<int> workers = new List<int> { 6 };
            List<int> batches = new List<int> { 8 };
            //List<int> workers = new List<int> {6 };
            //List<int> batches = new List<int> { 8 };
            int Row = 0;
            for (int i = 0; i < workers.Count; i++)
            {
                numOfWorkers = workers[i];
                for (int j = 0; j < batches.Count; j++)
                {
                    Row++;
                    numOfBatches = batches[j];
                    MOEM_1();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void MOEM_1() 
        {
            string outPutName = @"D:\实验数据\MOEM_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";
            //------生成所有赛汝构造------
            List<string> group = new List<string>();
            for (int i = 1; i <= numOfWorkers; i++)
            {
                group.Add((i).ToString());
            }
            List<Solution> solutionSet = new List<Solution>();
            List<int> numOfSolutions = new List<int>();
            List<List<List<string>>> seruSetList = new List<List<List<string>>>();
            seruSetList = ProduceSeruList(group, numOfWorkers);
            DateTime StartTime = DateTime.Now;
            //------遍历所有赛汝构造------
            for (int n = 0; n < seruSetList.Count; n++)
            {
                Solution tempsolution = new Solution();
                List<Seru> seruSet = new List<Seru>();
                List<List<string>> workerSetList = seruSetList[n];
                for (int m = 0; m < workerSetList.Count; m++)
                {
                    Seru seru = new Seru();
                    List<string> workerSet = workerSetList[m];
                    seru.workerSet = workerSet;
                    seruSet.Add(seru);
                }
                //选取 Seru 构造
                tempsolution.Serus = seruSet;
                int numOfSerus = tempsolution.Serus.Count;
                //if (numOfSerus == 1) 
                //{
                //    continue;
                //}
                //------计算加工时间
                double[,] Pkj = CalculateProcessTime(tempsolution, numOfWorkers, numOfBatches);
                string processTime = ConvertToString(Pkj, tempsolution.Serus.Count);
                //------计算赛汝中的工人数
                double[] wj = new double[tempsolution.Serus.Count];
                for (int i = 0; i < tempsolution.Serus.Count; i++)
                {
                    wj[i] = tempsolution.Serus[i].workerSet.Count;
                }
                string workersInSeru = "[";
                for (int i = 0; i < wj.Length - 1; i++)
                {
                    workersInSeru += wj[i].ToString();
                    workersInSeru += ",";
                }
                workersInSeru += wj.Last().ToString(); ;
                workersInSeru += "]";
                //完工时间的上界
                b = 10000;
                //CplexSolver cplexSolver = new CplexSolver();
                //b = cplexSolver.ObtainIdealTLH(tempsolution,processTime, workersInSeru,numOfBatches);
                //------计算当前构造下的非支配解------
                List<Solution> currentParetoSolutions = new List<Solution>();
                currentParetoSolutions = Run(n, tempsolution, processTime, workersInSeru, b);
                if (currentParetoSolutions != null)
                {
                    solutionSet.AddRange(currentParetoSolutions);
                }
                //非支配排序
                solutionSet = NonDominatedSorting(solutionSet);
                numOfSolutions.Add(solutionSet.Count);
            }
            DateTime dateTime = DateTime.Now;
            //Console.WriteLine("------------------------------");
            //for (int i = 0; i < numOfSolutions.Count; i++)
            //{
            //    Console.WriteLine(numOfSolutions[i]);
            //}
            Console.WriteLine($"------运算时间为：{(dateTime - StartTime).TotalSeconds}秒");
            Console.WriteLine("------开始输出数据------");
            OUT outExcel = new OUT();
            outExcel.OutPut(solutionSet, outPutName, (dateTime - StartTime).TotalSeconds);
            Console.WriteLine("------输出数据结束------");
        }
        /// <summary>
        /// 读取数据
        /// </summary>
        public void  readData() 
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
        /// 产生给定赛汝构造下的加工时间（赛汝m，批次j）
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="numOfWorkers"></param>
        /// <param name="numOfBatches"></param>
        /// <returns></returns>
        public double[,] CalculateProcessTime(Solution solution, int numOfWorkers, int numOfBatches)
        {
            double[,] t = new double[solution.Serus.Count, numOfBatches];
            for (int m = 0; m < solution.Serus.Count; m++)
            {
                Seru tempSeru = solution.Serus[m];
                for (int j = 0; j < numOfBatches; j++)
                {
                    //产品批次
                    int batchID = Convert.ToInt16(tableBatchToProductType.Rows[j][0]);
                    //产品类型
                    int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[j][1]);
                    //批次规模
                    int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[j][2]);
                    double batchFlowTime = CalculateBatchFlowTime(tempSeru, productTypeID, batchSize, numOfWorkers);
                    t[m, j] = batchFlowTime;
                    //t[m, j] = Math.Round (batchFlowTime);
                }
            }
            return t;
        }
        /// <summary>
        /// 计算批次 m 在Seru中的加工时间
        /// </summary>
        /// <param name="tempSeru"></param>
        /// <param name="productTypeID"></param>
        /// <param name="batchSize"></param>
        /// <param name="numOfWorkers"></param>
        /// <returns></returns>
        public double CalculateBatchFlowTime(Seru tempSeru, int productTypeID, int batchSize, int numOfWorkers)
        {
            double taskTimeInSeru = 0;
            for (int i = 0; i < tempSeru.workerSet.Count; i++)
            {
                int workerID = Convert.ToInt16(tempSeru.workerSet[i]);
                double workerCoefficientOfMultipleTask = Convert.ToDouble(tableWorkerToMultipleTask.Rows[workerID - 1][1]);
                double c = 1;
                if (numOfWorkers > maxNumOfMultipleTask)
                {
                    c += workerCoefficientOfMultipleTask * (numOfWorkers - maxNumOfMultipleTask);
                }
                double workerToProductTypeCoefficient = Convert.ToDouble(tableWorkerToProductType.Rows[workerID - 1][productTypeID]);   //worker与productTyp对应的熟练系数
                taskTimeInSeru = taskTimeInSeru + taskTime * c * workerToProductTypeCoefficient;    //worker[i]的一个task加工时间
            }
            taskTimeInSeru = taskTimeInSeru / tempSeru.workerSet.Count;
            double flowTime = taskTimeInSeru * batchSize * numOfWorkers / tempSeru.workerSet.Count;
            return flowTime;
        }
        /// <summary>
        /// 将二维数组转化为字符串
        /// </summary>
        /// <param name="t">给定Seru构造，批次 m 在 单元 j 上的加工时间</param>
        /// <param name="i">给定Seru构造含有的单元数</param>
        /// <returns>二维加工时间对应的字符串</returns>
        public string ConvertToString(double[,] t, int i)
        {
            string result = "[";
            //遍历每个单元，获取该单元对每个批次的加工时间
            for (int j = 0; j < i; j++)
            {
                result += "[";
                //.GetLength(int dimension)：获取指定维数的元素数
                for (int m = 0; m < t.GetLength(1) - 1; m++)
                {
                    result += t[j, m].ToString();
                    result += ",";
                }
                result += t[j, t.GetLength(1) - 1].ToString();
                result += "],";
            }
            result = result.Remove(result.Length - 1, 1);
            result += "]";
            return result;
        }
        /// <summary>
        /// 递推算法生成所有赛汝构造
        /// </summary>
        /// <param name="group"></param>
        /// <param name="numOfWorkers"></param>
        /// <returns></returns>
        public List<List<List<string>>> ProduceSeruList(List<string> group, int numOfWorkers)
        {
            List<string> firstSeru = new List<string>();
            List<List<string>> firstSeruSet = new List<List<string>>();
            List<List<List<string>>> seruSetList = new List<List<List<string>>>();
            firstSeru.Add(group[0]);
            firstSeruSet.Add(firstSeru);
            seruSetList.Add(firstSeruSet);
            //从第2个工人开始进行递推，生成下一个Seru构造
            for (int i = 1; i < numOfWorkers; i++)
            {
                string nextWorker = group[i];
                //每次递推都需要一个集合接收 numOfWorkers = n-1 时的Seru构造,以便利用此集合计算 numOfWorkers = n-1 时的Seru构造
                seruSetList = Recurrence(seruSetList, nextWorker);
            }
            return seruSetList;
        }
        /// <summary>
        /// Recurrence - 递推
        /// </summary>
        /// <param name="List"></param>
        /// <param name="nextWorker"></param>
        /// <returns></returns>
        public List<List<List<string>>> Recurrence(List<List<List<string>>> List, string nextWorker)
        {
            List<List<List<string>>> serusList = new List<List<List<string>>>();
            for (int i = 0; i < List.Count; i++)
            {
                //下一个Seru构造生成情况1：i 依次加入到每一个Seru单元中
                for (int j = 0; j < List[i].Count; j++)
                {
                    List<List<string>> seruSetClone = new List<List<string>>(List[i]);
                    List<string> SeruClone = new List<string>(seruSetClone[j]);
                    SeruClone.Add(nextWorker);
                    //插入待分配工人到Seru单元中
                    seruSetClone.Insert(j, SeruClone);
                    //删除原位置的Seru单元
                    seruSetClone.RemoveAt(j + 1);
                    serusList.Add(seruSetClone);
                }
                //下一个Seru构造生成情况2：i 单独构成一个Seru构造
                List<string> seruSole = new List<string>()
                {
                    nextWorker
                };
                List<List<string>> newSeruSet = new List<List<string>>(List[i]);
                newSeruSet.Add(seruSole);
                serusList.Add(newSeruSet);
            }
            return serusList;
        }
        /// <summary>
        /// 产生当前构造下的非支配解
        /// </summary>
        /// <param name="n"></param>
        /// <param name="currentFormation"></param>
        /// <param name="tempsolution"></param>
        /// <returns></returns>
        public List<Solution> Run(int n,  Solution tempsolution, string processTime, string workersInSeru, double c) 
        {
            List<Solution> solutions = new List<Solution>();
            CplexSolver cplexSolver = new CplexSolver();
            solutions = cplexSolver.ProduceParetoSolutions(numOfBatches, n, tempsolution, processTime,  workersInSeru, c, minError, RatioBetweenObjFun);
            return solutions;
        }
        /// <summary>
        /// 通过非支配排序获得最终的非支配解
        /// </summary>
        /// <param name="solutions"></param>
        /// <returns></returns>
        public List<Solution> NonDominatedSorting(List<Solution> solutions) 
        {
            //------初始化------
            for (int i = 0; i < solutions.Count; i++)
            {
                Solution iIndividual = solutions[i];
                iIndividual.frontNumber = 0;                                                                   //每次计算pareto解时，frontNumber重值为0
                iIndividual.numOfDonimateIndividual = 0;                                            //每次计算pareto解时，重值为0
                iIndividual.donimatedSet = new List<Solution>();                                //每次计算pareto解时，重值为空
            }
            //------生成各个体的支配集和被支配个数，并找出第1层front------
            List<Solution> firstFrontSet = new List<Solution>();       //第1层解集
            for (int p = 0; p < solutions.Count; p++)
            {
                Solution pIndividual = solutions[p];
                for (int q = 0; q <solutions.Count; q++)
                {
                    Solution qIndividual = solutions[q];
                    if (((pIndividual.TotalThroughPutTime <= qIndividual.TotalThroughPutTime) && (pIndividual.TotalLaborTime < qIndividual.TotalLaborTime)) || ((pIndividual.TotalThroughPutTime < qIndividual.TotalThroughPutTime) && (pIndividual.TotalLaborTime <= qIndividual.TotalLaborTime)))       //pIndividual 支配qIndividual
                    {
                        pIndividual.donimatedSet.Add(qIndividual);                               //qIndividual加入到pIndividual的支配解中
                    }
                    else if (((qIndividual.TotalThroughPutTime <= pIndividual.TotalThroughPutTime) && (qIndividual.TotalLaborTime < pIndividual.TotalLaborTime)) || ((qIndividual.TotalThroughPutTime< pIndividual.TotalThroughPutTime) && (qIndividual.TotalLaborTime <= pIndividual.TotalLaborTime)))     //qIndividual 支配pIndividual
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
    }
}
