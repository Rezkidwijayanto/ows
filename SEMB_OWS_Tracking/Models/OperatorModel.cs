using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class OperatorModel
    {
        public string[] OWS_ID { get; set; }
        public string Reason { get; set; }
        public string Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Family { get; set; }
#nullable enable
        public string? Comments { get; set; }


    }
}
