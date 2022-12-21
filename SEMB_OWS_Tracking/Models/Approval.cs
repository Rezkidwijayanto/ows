using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class Approval
    {
        public string OWS_ID { get; set; }
        public string Reason { get; set; }
        public DateTime Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Family { get; set; }
        public string Comments { get; set; }
        public string Link_Box { get; set; }
        public string Approval_Status { get; set; }
        public string Submit_By { get; set; }
        public int OWS_Request_id { get; set; }

    }

    public class AprroversList {
        public string ApproverMM { get; set; }
        public string ApproverMTM { get; set; }
        public string ApproverQA { get; set; }
        public string ApproverPE { get; set; }

    }
}
