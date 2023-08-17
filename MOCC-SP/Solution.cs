using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCC_II_For_Seru_Production
{
    [Serializable]
    public class Solution
    {
        public double throughPutTime { get; set; }                                                                                                                //解的totalThroughPutTime
        public double labourTime { get; set; }                                                                                                                        //解的totalTabourTime
        public SeruFormation currentFormation { get; set; }
        public SeruScheduling currentSchedule { get; set; }
        public List<Solution> donimatedSet { get; set; }                                                                                                       //放被它支配的赛汝构造个体
        public int numOfDonimateIndividual { get; set; }                                                                                                      //支配它的个体数
        public int frontNumber { get; set; }                                                                                                                             //front层号
        public double distanceOfCrowd { get; set; }                                                                                                               //拥挤程度
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
