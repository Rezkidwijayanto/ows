using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class ViewModel
    {
        public IEnumerable<OpenWorkOrder> OpenWorkOrdersDetail { get; set; }
        public IEnumerable<Started> StartedsDetail { get; set; }
        public IEnumerable<Submitted> SubmittedsDetail { get; set; }
        public IEnumerable<NewRequest> NewRequestsDetail { get; set; }
        public IEnumerable<DatePickerModel> datePickers { get; set; }
        public List<FamilyModel> FamilyDetails { get; set; }
        public List<DataModel> DataDetails { get; set; }
        public List<OWSIDModel> OWSIDDetails { get; set; }
        public List<Tracking> TrackingDetails { get; set; }
        public List<Verified> VerifiedDetails { get; set; }
        public List<LinkBox> LinkBoxDetails { get; set; }
        public List<RequestCount> RequestCountDetails { get; set; }
        public List<RequestCountWeek> RequestCountWeekDetails { get; set; }
        public List<RequestCountMonth> RequestCountMonthDetails { get; set; }
        public List<ApproverMMModel> approverMMModelsDetails { get; set; }
        public List<ApproverQAModel> approverQAModelsDetails { get; set; }
        public List<ApproverPEModel> approverPEModelsDetails { get; set; }
        public List<ApproverMTMModel> approverMTMModelsDetails { get; set; }
        public List<ListApproverModel> ListApprover { get;set; }
        public List<ListReasonModel> ListReason { get; set; }
        public List<FamilyModel> NewFamilyDetails { get; set; }
        public List<MSTDetailsModel> MSTDetailsData { get; set; }

    }
}
