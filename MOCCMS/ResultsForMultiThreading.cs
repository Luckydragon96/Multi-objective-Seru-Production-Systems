using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOCCSNMS_NSGA2
{
    public class ResultsForMultiThreading
    {
        public List<SeruScheduling> newSchedules= new List<SeruScheduling>();
        public List<SeruScheduling> nonDominatedSchedules = new List<SeruScheduling>();
        public List<SeruFormation> newFormations = new List<SeruFormation>();
        public List<SeruFormation> nonDominatedFormations = new List<SeruFormation>();
    }
}
