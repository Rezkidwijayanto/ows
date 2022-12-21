using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class ChartDataModel
    {
        public List<string> Datalabel { get; set; }
        public List<double> Datavalue { get; set; }
        public List<double> Datavalue_2 { get; set; }
        public List<double> Datavalue_3 { get; set; }
        public List<double> Datavalue_4 { get; set; }
        public List<double> Datavalue_5 { get; set; }
        public ChartDataModel()
        {
            Datalabel = new List<string>();
            Datavalue = new List<double>();
            Datavalue_2 = new List<double>();
            Datavalue_3 = new List<double>();
            Datavalue_4 = new List<double>();
            Datavalue_5 = new List<double>();
        }
    }
}
