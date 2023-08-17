using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOEM
{
    public class Seru
    {
        public List<string> workerSet = new List<string>();                  //工人集合-Seru构造
        public List<string> batchSet = new List<string>();                    //批次集合-Seru调度
        public double ThroughPutTime = new double();                        //加工时间
        public double LaborTime = new double();                                 //劳动时间
    }
}
