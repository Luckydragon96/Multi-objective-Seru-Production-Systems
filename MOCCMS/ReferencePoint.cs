using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    [Serializable]
    public class ReferencePoint
    {
        public int ID{get;set;}                                                  //ID
        public double[] Coordinates { get; set; }                      //坐标
        //public List<SeruScheduling> schedules { get; set; }
        public List<SeruScheduling> schedulesForSelection { get; set; }
        public int NichesOfSeruScheduling { get; set; }
        //public List<SeruFormation> formations { get; set; }
        public List<SeruFormation> formationsForSelection { get; set; }
        public int NichesOfSeruFormation { get; set; }
        public object Clone()
        {
            //但是涉及到引用类型的字段不能浅复制
            return this.MemberwiseClone();
        }
    }
}
