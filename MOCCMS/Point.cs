using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    [Serializable]
    public class Point
    {
        public int ID { get; set; }
        public double[] Coordinates { get; set; }                      //原有坐标
        public double[] TCoordinates { get; set; }                  //确定理想点后转换的坐标
        public double[] NCoordinates { get; set; }                 //确定极值点后归一化的坐标
        public double DistanceX { get; set; }
        public double DistanceY { get; set; }
        public List<double> DistanceToReferencePoints { get; set; }
        public List<double[]> NearestReferencePoints { get; set; }
        public List<SeruScheduling> IndividualsForSelection_SS { get; set; }
        public List<SeruFormation> IndividualsForSelection_SF { get; set; }
        public object Clone()
        {
            //但是涉及到引用类型不能浅复制
            return this.MemberwiseClone();
        }
    }

}
