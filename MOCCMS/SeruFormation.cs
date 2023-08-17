using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    [Serializable]
    public class SeruFormation : System.Object, ICloneable
    {
        public List<int> formationCode { get; set; }                                                                                                               //编码,里面放workerID，大于numOfWorker的表示分组
        public List<Seru> serusSet { get; set; }                                                                                                                      //Seru分配，里面放seru对象
        public Point pointInformation { get; set; }                                                                                                                 //点的相关信息       
        public double throughPutTime { get; set; }                                                                                                                //解的totalThroughPutTime
        public double labourTime { get; set; }                                                                                                                        //解的totalTabourTime
        public List<SeruFormation> donimatedSet { get; set; }                                                                                             //放被它支配的赛汝构造个体
        public int numOfDonimateIndividual { get; set; }                                                                                                      //支配它的个体数
        public int frontNumber { get; set; }                                                                                                                             //front层号
        public double distanceOfCrowd { get; set; }                                                                                                               //拥挤程度
        public List<List<int>> currentBatchSchedule { get; set; }                                                                                         //当前的最优调度，里面放SeruScheduling对象

        /// <summary>
        /// 根据solutionCode生成seruSet
        /// </summary>
        /// <returns></returns>
        public List<Seru> produceSerusSet(int numOfWorkers)
        {
            //解码的中间变量,是一个空的字符串变量
            string str = "";
            for (int i = 0; i < formationCode.Count; i++)
            {
                if (formationCode[i] > numOfWorkers)
                {
                    //"a"是seru间分隔符，"-"是worker间分隔符，如果解码出来的是一个大于工人数的量，就输出“a-”,否则就“输出原来数字-”
                    str = str + "a" + "-";
                }
                else
                    str = str + formationCode[i] + "-";
            }
            //Console.WriteLine(str);
            //生成Seru组
            string[] seruList = str.Split('a');
            serusSet = new List<Seru>();
            for (int i = 0; i < seruList.Length; i++)
            {
                if ((seruList[i].ToString() != "-") && (seruList[i].ToString() != ""))                          //Seru里不空
                {
                    string[] workerList = seruList[i].ToString().Split('-');                                            //生成Seru里的worker数组
                    Seru seru = new Seru();
                    seru.workersSet = new List<int>();
                    for (int j = 0; j < workerList.Length; j++)
                    {
                        if ((workerList[j].ToString() != "-") && (workerList[j].ToString() != ""))       //worker不为空
                        {
                            seru.workersSet.Add(Convert.ToInt16(workerList[j]));                             //worker加入到Seru
                        }
                    }
                    serusSet.Add(seru);                                                                                               //seru加入到seruSet里
                }
            }
            return serusSet;
        }
        /// <summary>
        /// 浅复制
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            //但是涉及到引用类型不能浅复制
            return this.MemberwiseClone();
        }
    }
}
