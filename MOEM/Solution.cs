using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOEM
{
    public class Solution
    {
        public int ID { get; set; }
        public int frontNumber { get; set; }
        public List<Solution> donimatedSet { get; set; }
        public int numOfDonimateIndividual { get; set; }
        public List<Seru> Serus = new List<Seru>();                                                                    //Seru单元集合
        public double TotalThroughPutTime = new double();                                                       //总通过时间
        public double TotalLaborTime = new double();                                                                //总劳动时间
    }
}
