using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    [Serializable]
    public class SeruScheduling : System.Object, ICloneable
    {
        public List<int> scheduleCode { get; set; }                                                                                                //编码
        public List<List<int>> BatchesAssignment { get; set; }                                                                            //批次分配
        public Point pointInformation { get; set; }                                                                                                 //点的相关信息                                   
        public double throughPutTime { get; set; }                                                                                               //解的throughPutTime
        public double labourTime { get; set; }                                                                                                       //解的labourTime
        public List<SeruScheduling> donimatedSet { get; set; }                                                                          //放被它支配的个体
        public int numOfDonimateIndividual { get; set; }                                                                                    //支配它的个体数
        public int frontNumber { get; set; }                                                                                                           //front层号
        public double distanceOfCrowd { get; set; }                                                                                             //拥挤程度
        public List<Seru> currentSeruSet { get; set; }                                                                                           //当前最优构造，里面存放SeruFormation对象

        /// <summary>
        /// 根据scheduleCode生成seruSchedule
        /// </summary>
        /// <param name="numOfBatches"></param>
        /// <returns></returns>
        public List<List<int>> produceSeruSchedule(int numOfBatches)
        {
            //解码的中间变量
            string str = "";
            for (int i = 0; i < scheduleCode.Count; i++)
            {
                if ((int)scheduleCode[i] > numOfBatches)
                {
                    //"a"是seru间分隔符，"-"是batch间分隔符
                    str = str + "a" + "-";
                }
                else
                    str = str + (int)scheduleCode[i] + "-";
            }
            string[] BatchSeruList = str.Split('a');
            List<List<int>> BatchSet = new List<List<int>>();
            for (int i = 0; i < BatchSeruList.Length; i++)
            {
                if ((BatchSeruList[i].ToString() != "-") && (BatchSeruList[i].ToString() != ""))
                {
                    string[] BatchList = BatchSeruList[i].ToString().Split('-');
                    List<int> BatchesInSeru = new List<int>();
                    for (int j = 0; j < BatchList.Length; j++)
                    {
                        if ((BatchList[j].ToString() != "-") && (BatchList[j].ToString() != ""))
                        {
                            BatchesInSeru.Add(Convert.ToInt32(BatchList[j]));
                        }
                    }
                    BatchSet.Add(BatchesInSeru);
                }
            }
            return BatchSet;
        }
        /// <summary>
        /// 浅复制
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            //但是涉及到引用类型的字段不能浅复制
            return this.MemberwiseClone();
        }
    }
}
