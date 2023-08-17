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

namespace CCMO
{
    public partial class Form1 : Form
    {
        //-----工人数、批次数
        int numOfWorkers ;
        int numOfBatches ;
        //----赛汝生产的相关参数
        int maxNumOfMultipleTask = 10;                                                                                        //多能工最大值，Seru里tasks大于这个值加工时间就要延长。
        double probabilityOfMutation = 0.7;
        double probabilityOfCrossover = 0.8;
        double taskTime = 1.8;
        int numOfPopular = 50;                                                                                                       //种群大小
        int maxItera = 40;                                                                                                                 //最大迭代次数
        Random random = new Random();
        //---批次与产品类型关系的数据/工人与产品类型的熟练程度的数据/多能工系数数据
        DataTable tableBatchToProductType = new DataTable();
        DataTable tableWorkerToProductType = new DataTable();
        DataTable tableWorkerToMultipleTask = new DataTable();
        //------Origin父代种群、Help父代种群------
        List<SeruFormation> parentPopulation_Origin = new List<SeruFormation>();
        List<SeruFormation> parentPopulation_Help = new List<SeruFormation>();
        //------非支配------
        List<SeruFormation> paretoSolutions = new List<SeruFormation>();
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
                    parentPopulation_Origin = new List<SeruFormation>();
                    parentPopulation_Help = new List<SeruFormation>();
                    paretoSolutions = new List<SeruFormation>();
                    numOfBatches = batches[j];
                    CCMO();
                }
            }
        }
        /// <summary>
        /// CCMO
        /// </summary>
        public void CCMO() 
        {
            Console.WriteLine("-------------------------正在进化--------------------------------");
            string name = @"D:\Result_MOCC\CCMO\CCMO_" + numOfWorkers.ToString() + "_" + numOfBatches.ToString() + ".xls";
            DateTime beginTime = DateTime.Now;

            for (int i = 0; i < numOfPopular; i++)
            {
                SeruFormation solution = new SeruFormation();
                solution.formationCode = initialFormationCode();                         //初始化编码
                solution.produceSerusSet(numOfWorkers);                                    //通过编码解码成赛汝构造
                caculateFitness(solution);                                                                //计算该解下的目标值
                parentPopulation_Origin.Add(solution);
            }

            for (int i = 0; i < numOfPopular; i++)
            {
                SeruFormation solution = new SeruFormation();
                solution.formationCode = initialFormationCode();                         //初始化编码
                solution.produceSerusSet(numOfWorkers);                                    //通过编码解码成赛汝构造
                caculateFitnessWithSPT(solution);                                                  //计算该解下的目标值
                parentPopulation_Help.Add(solution);
            }
            //------ 评价Origin种群、Help种群------
            parentPopulation_Origin = DeepCopyByBin<List<SeruFormation>>(produceNewParentSchesulingByDominatedSort_SF(parentPopulation_Origin));
            parentPopulation_Origin = parentPopulation_Origin.OrderBy(x => x.frontNumber).ThenByDescending(x => x.distanceOfCrowd).ToList();
            parentPopulation_Help = DeepCopyByBin<List<SeruFormation>>(produceNewParentSchesulingByDominatedSort_SF(parentPopulation_Help));
            parentPopulation_Help = parentPopulation_Help.OrderBy(x => x.frontNumber).ThenByDescending(x => x.distanceOfCrowd).ToList();

            for (int m = 0; m < maxItera; m++)
            {
                Console.WriteLine($"-==================第{m + 1}次迭代================");
                List<SeruFormation> offSpringPopulation_Origin = getOffSpringPopulation_SF(parentPopulation_Origin);
                List<SeruFormation> offSpringPopulation_Help = getOffSpringPopulation_SF(parentPopulation_Help);
                //------交叉组合父代种群和子代种群------
                parentPopulation_Origin.AddRange(offSpringPopulation_Origin);
                parentPopulation_Origin.AddRange(offSpringPopulation_Help);
                parentPopulation_Help.AddRange(offSpringPopulation_Origin);
                parentPopulation_Help.AddRange(offSpringPopulation_Help);
                //------评价Origin种群、Help种群------
                for (int i = 0; i < parentPopulation_Origin.Count; i++)
                {
                    for (int j = 0; j < parentPopulation_Origin[i].serusSet.Count; j++)
                    {
                        parentPopulation_Origin[i].serusSet[j].batchesSet = new List<int>();
                        parentPopulation_Origin[i].serusSet[j].throughPutTime = 0;
                        parentPopulation_Origin[i].serusSet[j].labourTime = 0;
                    }
                    caculateFitness(parentPopulation_Origin[i]);
                }
                for (int i = 0; i < parentPopulation_Help.Count; i++)
                {
                    for (int j = 0; j < parentPopulation_Help[i].serusSet.Count; j++)
                    {
                        parentPopulation_Help[i].serusSet[j].batchesSet = new List<int>();
                        parentPopulation_Help[i].serusSet[j].throughPutTime = 0;
                        parentPopulation_Help[i].serusSet[j].labourTime = 0;

                    }
                    caculateFitnessWithSPT(parentPopulation_Help[i]);
                }
                parentPopulation_Origin = DeepCopyByBin<List<SeruFormation>>(produceNewParentSchesulingByDominatedSort_SF(parentPopulation_Origin));
                //parentPopulation_Origin = parentPopulation_Origin.OrderBy(x => x.frontNumber).ThenByDescending(x => x.distanceOfCrowd).ToList();
                parentPopulation_Help = DeepCopyByBin<List<SeruFormation>>(produceNewParentSchesulingByDominatedSort_SF(parentPopulation_Help));
                //parentPopulation_Help = parentPopulation_Help.OrderBy(x => x.frontNumber).ThenByDescending(x => x.distanceOfCrowd).ToList();
                paretoSolutions = DeepCopyByBin<List<SeruFormation>>(getParetoOfPopulation_SF(parentPopulation_Origin));
            }
            //------ 去除重复解 ------
            List<SeruFormation> tem = new List<SeruFormation>();
            for (int i = 0; i < paretoSolutions.Count; i++)
            {
                bool flag = true;
                for (int j = 0; j < tem.Count; j++)
                {
                    if ((((SeruFormation)paretoSolutions[i]).throughPutTime == ((SeruFormation)tem[j]).throughPutTime) && (((SeruFormation)paretoSolutions[i]).labourTime == ((SeruFormation)tem[j]).labourTime))
                        flag = false;
                }
                if (flag == true)
                    tem.Add(paretoSolutions[i]);
            }
            paretoSolutions = tem;
            //------ 输出非支配解 ------
            for (int i = 0; i < paretoSolutions.Count; i++)
            {
                Console.WriteLine($"{paretoSolutions[i].throughPutTime}  {paretoSolutions[i].labourTime}");
            }
            DateTime endTime = DateTime.Now;
            TimeSpan t = endTime - beginTime;
            Output(paretoSolutions, name, t.TotalSeconds);
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
        /// 
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
        /// 按FCFS规则计算解的时间
        /// </summary>
        /// <param name="solution"></param>
        void caculateFitness(SeruFormation solution)
        {
            scheduleCell(solution);                             //cell调度

            //计算totalThroughPutTime，为cell中最长的
            //计算totalLabourTime，为cell的总和
            solution.throughPutTime = (solution.serusSet[0]).throughPutTime;
            solution.labourTime = (solution.serusSet[0]).labourTime;
            for (int i = 1; i < solution.serusSet.Count; i++)
            {
                if ((solution.serusSet[i]).throughPutTime > solution.throughPutTime)
                {
                    solution.throughPutTime = (solution.serusSet[i]).throughPutTime;
                }
                solution.labourTime = solution.labourTime + (solution.serusSet[i]).labourTime;
            }
            solution.throughPutTime = System.Math.Round(solution.throughPutTime, 3);
            solution.labourTime = System.Math.Round(solution.labourTime, 3);
            //Console.WriteLine(solution.throughPutTime + "    " + solution.labourTime);
        }
        /// <summary>
        /// 按SPT规则计算解的时间
        /// </summary>
        /// <param name="solution"></param>
        void caculateFitnessWithSPT(SeruFormation solution)
        {
            scheduleCellSPT(solution);                             //cell调度按SPT规则

            //计算totalThroughPutTime，为cell中最长的
            //计算totalLabourTime，为cell的总和

            solution.throughPutTime = (solution.serusSet[0]).throughPutTime;
            solution.labourTime = (solution.serusSet[0]).labourTime;
            for (int i = 1; i < solution.serusSet.Count; i++)
            {
                if ((solution.serusSet[i]).throughPutTime > solution.throughPutTime)
                {
                    solution.throughPutTime = (solution.serusSet[i]).throughPutTime;
                }
                solution.labourTime = solution.labourTime + (solution.serusSet[i]).labourTime;
            }
            solution.throughPutTime = System.Math.Round(solution.throughPutTime, 3);
            solution.labourTime = System.Math.Round(solution.labourTime, 3);
            //Console.WriteLine(solution.throughPutTime + "    " + solution.labourTime);
        }
        /// <summary>
        /// 赛汝调度，把批次按FCFS规则分配给Seru，计算各Seru的分配批次，及throughputTime和laborTime
        /// </summary>
        /// <param name="solution"></param>
        void scheduleCell(SeruFormation solution)
        {
            //分配批次
            //第一轮cell load，依次分配
            for (int i = 0; i < solution.serusSet.Count; i++)
            {
                int batchID = Convert.ToInt16(tableBatchToProductType.Rows[i][0]);
                int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[i][1]);
                int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[i][2]);

                Seru celltmp = solution.serusSet[i];
                celltmp.batchesSet = new List<int>();
                celltmp.batchesSet.Add(batchID);
                double flowTimeOfBatch = caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);
                celltmp.throughPutTime =  flowTimeOfBatch;
                celltmp.labourTime = flowTimeOfBatch * celltmp.workersSet.Count;

            }

            //第一轮后的cell load，cell谁先加工完分配给谁
            for (int i = solution.serusSet.Count; i < numOfBatches; i++)
            {
                int batchID = Convert.ToInt16(tableBatchToProductType.Rows[i][0]);
                int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[i][1]);
                int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[i][2]);

                int earliestEndCellID = findEarliestEndCell(solution.serusSet);
                Seru celltmp = solution.serusSet[earliestEndCellID];

                int oldBatchID = Convert.ToInt16(celltmp.batchesSet[celltmp.batchesSet.Count - 1].ToString());
                int oldproductTypeID = Convert.ToInt16(tableBatchToProductType.Rows[oldBatchID - 1][1]);

                if (productTypeID != oldproductTypeID)    //不同，有setup时间
                {
                    celltmp.throughPutTime = celltmp.throughPutTime  + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);//与前项加工的产品类型不一致，加入setupTime
                }
                else
                   celltmp.throughPutTime = celltmp.throughPutTime + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);   //与前项加工类型一致，不加入setupTime
                celltmp.labourTime = celltmp.labourTime + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize) * celltmp.workersSet.Count;
                celltmp.batchesSet.Add(batchID);
            }
        }
        /// <summary>
        /// Seru调度，把批次按SPT规则分配给Seru，计算各cell的分配批次，及throughputTime和laborTime
        /// </summary>
        /// <param name="solution"></param>
        void scheduleCellSPT(SeruFormation solution)
        {
            //分配批次
            //第一轮cell load，按照最短加工时间SPT调度
            for (int i = 0; i < solution.serusSet.Count; i++)
            {
                int batchID = Convert.ToInt16(tableBatchToProductType.Rows[i][0]);
                int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[i][1]);
                int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[i][2]);

                //发现cellsSet里面空Cell且对batchID有最短完成时间的
                int shortesProcessTimeCellID = findShortesProcessTimeCell(solution.serusSet, productTypeID, batchSize);

                Seru celltmp = solution.serusSet[shortesProcessTimeCellID];
                celltmp.batchesSet = new List<int>();
                celltmp.batchesSet.Add(batchID);

                double flowTimeOfBatch = celltmp.throughPutTime;
                celltmp.throughPutTime =  flowTimeOfBatch;
                celltmp.labourTime = flowTimeOfBatch * celltmp.workersSet.Count;

            }
            //第一轮后的cell load，cell谁先加工完分配给谁
            for (int i = solution.serusSet.Count; i < numOfBatches; i++)
            {
                int batchID = Convert.ToInt16(tableBatchToProductType.Rows[i][0]);
                int productTypeID = Convert.ToInt16(tableBatchToProductType.Rows[i][1]);
                int batchSize = Convert.ToInt16(tableBatchToProductType.Rows[i][2]);

                int earliestEndCellID = findEarliestEndCell(solution.serusSet);
                Seru celltmp = solution.serusSet[earliestEndCellID];

                int oldBatchID = Convert.ToInt16(celltmp.batchesSet[celltmp.batchesSet.Count - 1].ToString());
                int oldproductTypeID = Convert.ToInt16(tableBatchToProductType.Rows[oldBatchID - 1][1]);

                if (productTypeID != oldproductTypeID)    //不同，有setup时间
                {
                    celltmp.throughPutTime = celltmp.throughPutTime + + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);//与前项加工的产品类型不一致，加入setupTime
                }
                else
                    celltmp.throughPutTime = celltmp.throughPutTime + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);   //与前项加工类型一致，不加入setupTime
                celltmp.labourTime = celltmp.labourTime + caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize) * celltmp.workersSet.Count;
                celltmp.batchesSet.Add(batchID);
            }
        }
        /// <summary>
        /// 计算给定productType和batchSize的batch在cell中的flowTime
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="productTypeID"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        double caculateBatchFlowTimeInCell(Seru cell, int productTypeID, int batchSize)
        {
            double flowTime = 0;
            double taskTimeInCell = 0;
            for (int i = 0; i < cell.workersSet.Count; i++)
            {
                int workerID = Convert.ToInt16(cell.workersSet[i].ToString());
                double workerCoefficientOfMultipleTask = Convert.ToDouble(tableWorkerToMultipleTask.Rows[workerID - 1][1]);
                double c = 1;
                if ((numOfWorkers - maxNumOfMultipleTask) > 0)
                {
                    c = c + workerCoefficientOfMultipleTask * (numOfWorkers - maxNumOfMultipleTask);   //多能工系数
                }
                double workerToProductTypeCoefficient = Convert.ToDouble(tableWorkerToProductType.Rows[workerID - 1][productTypeID]);   //worker与productTyp对应的熟练系数
                taskTimeInCell = taskTimeInCell + taskTime * c * workerToProductTypeCoefficient;                                                         //worker[i]的一个task加工时间

                //flowTime = flowTime + taskTime * c * workerToProductTypeCoefficient * numOfTaskInCell * batchSize;                      
            }
            taskTimeInCell = taskTimeInCell / cell.workersSet.Count;                                                                    //cell的平均一个taske加工时间
            flowTime = taskTimeInCell * batchSize * numOfWorkers / cell.workersSet.Count;            //除以worker个数，是cell加工该产品的flowTime。
            return flowTime;
        }
        /// <summary>
        /// 计算最早结束的Seru，被安排为后续batch的加工Seru
        /// </summary>
        /// <param name="cellsSet"></param>
        /// <returns></returns>
        int findEarliestEndCell(List<Seru> cellsSet)
        {
            double ealiestThroughPutTime = (cellsSet[0]).throughPutTime;
            int lableID = 0;
            for (int i = 1; i < cellsSet.Count; i++)
            {
                if ((cellsSet[i]).throughPutTime < ealiestThroughPutTime)
                {
                    ealiestThroughPutTime = (cellsSet[i]).throughPutTime;
                    lableID = i;
                }
            }
            return lableID;
        }
        /// <summary>
        /// 计算针对给定batchID的最小SPT的cell，被安排为该batchID的加工cell
        /// </summary>
        /// <param name="cellsSet"></param>
        /// <param name="productTypeID"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        int findShortesProcessTimeCell(List<Seru> cellsSet, int productTypeID, int batchSize)
        {
            double shortesProcessTime = double.MaxValue;    //记录加工该次产品的最小TTPT
            int lableID = -1;                                //记录加工该次产品最小TTPT的cellID

            for (int i = 0; i < cellsSet.Count; i++)
            {
                Seru celltmp = cellsSet[i];

                //判断当前cell是否已经加工产品，如果没加工开始计算TTPT时间，并选取最小的
                if (celltmp.throughPutTime == 0)
                {
                    //计算cell加工该批次产品的时间
                    double tempProcessTime = caculateBatchFlowTimeInCell(celltmp, productTypeID, batchSize);

                    //如果小于shortesProcessTime，则将shortesProcessTime设为该cell的TTPT，记录cell的ID；
                    if (tempProcessTime < shortesProcessTime)
                    {
                        shortesProcessTime = tempProcessTime;
                        lableID = i;
                    }
                }
            }

            //将shortesProcessTime付给相应的cell
            (cellsSet[lableID]).throughPutTime = shortesProcessTime;
            return lableID;
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
                caculateCrowdDistance_SF(firstFrontSet);                                 //计算第1层crowdDistance
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
            if (newParentSolutions.Count == numOfPopular)
            {
                return newParentSolutions;
            }
            //如果第2 front没有将newParentSolutions填充满，即元素个数小于numOfPopular，加入到新种群中
            if ((nextFrontSet.Count + newParentSolutions.Count) <= numOfPopular)
            {
                newParentSolutions.AddRange(nextFrontSet);                         //第2层加入到newParentSolutions
                caculateCrowdDistance_SF(nextFrontSet);                            //计算第2层crowdDistance
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
                        caculateCrowdDistance_SF(nextFrontSet);                                         //计算该层crowdDistance
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
        /// 赛汝构造种群进行锦标赛选择
        /// </summary>
        /// <param name="individual1"></param>
        /// <param name="individual2"></param>
        /// <returns></returns>
        public SeruFormation tournament_SF(SeruFormation individual1, SeruFormation individual2)
        {
            //1在2前，选1
            if (individual1.frontNumber < individual2.frontNumber)
            {
                return individual1;
            }
            //1在2后，选2
            if (individual1.frontNumber > individual2.frontNumber)
            {
                return individual2;
            }
            //1,2同一层
            else
                //1的distanceOfCrowd大于2的，选1
                if (individual1.distanceOfCrowd > individual2.distanceOfCrowd)
            {
                return individual1;
            }
            //1的distanceOfCrowd小于2的，选2
            if (individual1.distanceOfCrowd < individual2.distanceOfCrowd)
            {
                return individual2;
            }
            //1,2的distanceOfCrowd相等，返回1
            else return individual1;
        }
        /// <summary>
        /// 交叉
        /// </summary>
        /// <param name="parent1"></param>
        /// <param name="parent2"></param>
        public void crossover_SF(SeruFormation parent1, SeruFormation parent2)
        {
            if (random.NextDouble() < probabilityOfCrossover)
            {
            laberCrossover:
                int number1 = random.Next(0, parent1.formationCode.Count);
                int number2 = random.Next(0, parent1.formationCode.Count);

                //相等的话继续生成2个随机点，作为截取交换部分
                if (number1 == number2)
                {
                    goto laberCrossover;
                }

                //如果number1大于number2，交换
                if (number1 > number2)
                {
                    int temp = number1;
                    number1 = number2;
                    number2 = temp;
                }
                List<int> subList1 = new List<int>();
                List<int> subList2 = new List<int>();
                //备份parent1、parent2的编码
                List<int> tempParent1 = DeepCopyByBin<List<int>>(parent1.formationCode);
                List<int> tempParent2 = DeepCopyByBin<List<int>>(parent2.formationCode);

                for (int i = number1; i < number2; i++)
                {
                    subList1.Add(tempParent1[i]);
                    parent2.formationCode.Remove((int)tempParent1[i]);

                    subList2.Add(tempParent2[i]);
                    parent1.formationCode.Remove((int)tempParent2[i]);
                }

                parent2.formationCode.AddRange(subList1);     //将parent1截取部分加入到parent2的尾端
                parent2.produceSerusSet(numOfWorkers);

                parent1.formationCode.AddRange(subList2);   //将parent2截取部分加入到parent1的尾端  
                parent1.produceSerusSet(numOfWorkers);
            }
        }
        /// <summary>
        /// 变异
        /// </summary>
        /// <param name="populations"></param>
        public void mutation_SF(List<SeruFormation> populations)
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

                    populations[i] = (SeruFormation)temp.Clone();
                    populations[i].formationCode = DeepCopyByBin<List<int>>(temp.formationCode);
                }
            }
        }
        /// <summary>
        /// 进化得到赛汝构造的子代种群
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        public List<SeruFormation> getOffSpringPopulation_SF(List<SeruFormation> population)
        {
            //选择交叉，by binary tournament
            List<int> randomList1 = new List<int>();    //生成随机序列1，在select时用
            List<int> randomList2 = new List<int>();    //生成随机序列2，在select时用

            for (int i = 0; i < numOfPopular/2; i++)
            {
                randomList1.Add(i);
                randomList2.Add(i);
            }
            for (int i = 0; i < numOfPopular/2; i++)
            {
                int number1 = random.Next(i, randomList1.Count);
                int temp1 = (int)randomList1[number1];
                randomList1[number1] = randomList1[i];
                randomList1[i] = temp1;

                int number2 = random.Next(i, randomList2.Count);
                int temp2 = (int)randomList2[number2];
                randomList2[number2] = randomList2[i];
                randomList2[i] = temp2;
            }
            for (int i = 3; i < numOfPopular/2; i += 4)
            {
                SeruFormation parent1 = tournament_SF(population[(int)randomList1[i - 3]], population[(int)randomList1[i - 2]]);
                SeruFormation parent2 = tournament_SF(population[(int)randomList1[i - 1]], population[(int)randomList1[i]]);

                if (!parent1.formationCode.Equals(parent2.formationCode))
                {
                    crossover_SF(parent1, parent2);
                }

                SeruFormation parent3 = tournament_SF(population[(int)randomList1[i - 3]], population[(int)randomList1[i - 2]]);
                SeruFormation parent4 = tournament_SF(population[(int)randomList1[i - 1]], population[(int)randomList1[i]]);

                if (!parent3.formationCode.Equals(parent4.formationCode))
                {
                    crossover_SF(parent3, parent4);
                }
            }
            mutation_SF(population);
            List<SeruFormation> populationClone = DeepCopyByBin<List<SeruFormation>>(population);
            population = new List<SeruFormation>();
            for (int i = 0; i < numOfPopular/2;i++) 
            {
                population.Add(DeepCopyByBin<SeruFormation>(populationClone[i]));
            }
            return population;
        }
        /// <summary>
        /// 输出到Excel中
        /// </summary>
        public void Output(List<SeruFormation> solutions, string name, double t)
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
