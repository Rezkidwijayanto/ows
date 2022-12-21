using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class Verified
    {
        public string OWS_ID { get; set; }
        public string Reason { get; set; }
        public string Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Family { get; set; }
        public string Comments { get; set; }
        public string VerifyComment { get; set; }
        public string Verify_By { get; set; }



    }

    public class Started
    {
        public string OWS_ID { get; set; }
        public string Reason { get; set; }
        public DateTime Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Family { get; set; }
        public string Approver { get; set; }
        public string Comments { get; set; }
        public string LinkBox { get; set; }

    }
    public class Submitted
    {
        public string OWS_ID { get; set; }
        public string OWS_ID_Product { get; set; }
        public string Reason { get; set; }
        public DateTime Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Family { get; set; }
        public string[] Approver { get; set; }
        public string LinkBox { get; set; }
        public string Comments { get; set; }
        public string VerifyComments { get; set; }
    }

    public class Tracking
    {
        public string Family { get; set; }
        public string Product { get; set; }
        public string ChangeInfo { get; set; }
        public string Work_Order { get; set; }
        public string MMStatus { get; set; }
        public string MTMStatus { get; set; }
        public string QAStatus { get; set; }
        public string PEStatus { get; set; }
        public string VerifiedStatus { get; set; }


    }
    public class WOTracking
    {
        public string Reason { get; set; }
        public string Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Family { get; set; }
        public string Comments { get; set; }

    }

    public class LinkBox
    {
        public int ID { get; set; }
        public string Work_Order { get; set; }
        public string Box_link { get; set; }
    }

    public class DeadlineTracking
    {
        public string Work_Order { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime SubmitDate { get; set; }
        public string Late_Status { get; set; }


    }

    public class ApprovalDeadlineTracking
    {
        public string Work_Order { get; set; }
        public DateTime StartDate { get; set; }
        public string Deadline { get; set; }
        public DateTime ApprovalDate { get; set; }
        public string Late_Status { get; set; }

    }

    public class RequestCount
    {
        public DateTime Date { get; set; }
        public string Number { get; set; }
       
    }
    public class RequestCountWeek
    {
        public string Week { get; set; }
        public string Number { get; set; }

    }
    public class RequestCountMonth
    {
        public string Month { get; set; }
        public string Number { get; set; }

    }
}
