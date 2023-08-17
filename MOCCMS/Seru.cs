using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
   //Seru类，是serusSet的存放对象
    [Serializable]
    public class Seru
    {
        public List<int> workersSet { get; set; }                         //存放workerID，表示seru中包含的worker
        public List<int> batchesSet { get; set; }                          //存放batchID，表示seru加工的batch
        public double throughPutTime { get; set; }                    //Seru中加工所有batch的作业时间
        public double labourTime { get; set; }                             //Seru中加工所有batch的工人劳动时间
    }
}
