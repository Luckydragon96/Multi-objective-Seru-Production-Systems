using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    public class Result
    {
        public List<SeruFormation> masterPopulation_SF { get; set; }
        public List<SeruScheduling> masterPopulation_SS { get; set; }

        public List<SeruFormation> P_mCmax_SF { get; set; }
        public  List<SeruFormation> P_mTLH_SF { get; set; }
        public  List<SeruFormation> P_aveND_SF { get; set; }
        public List<SeruScheduling> P_mCmax_SS { get; set; }
        public List<SeruScheduling> P_mTLH_SS { get; set; }
        public List<SeruScheduling> P_aveND_SS { get; set; }

        public SeruFormation mCmax_SF { get; set; }
        public SeruFormation mTLH_SF { get; set; }
        public SeruFormation aveND_SF { get; set; }

        //---协助进化的三个调度个体
        public SeruScheduling mCmax_SS { get; set; }
        public SeruScheduling mTLH_SS { get; set; }
        public SeruScheduling aveND_SS { get; set; }
    }
}
