using System;

namespace SEMB_OWS_Tracking.Models
{
    public class ApproveWorkOrder
    {
        public string WO { get; set; }
        public string Reason { get; set; }
        public DateTime Deadline { get; set; }
        public string Type { get; set; }
        public string Family { get; set; }
        public string ApproveUser { get; set; }
        public string Status { get; set; } 
        public string RequestUser { get; set; } 
    }
}