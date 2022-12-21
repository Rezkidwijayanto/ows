using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class NewRequestViewModel
    {
        public string OWS_ID { get; set; }
        public string Reason { get; set; }
        public string Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Family { get; set; }
        public string? Comment { get; set; }
        public string[] Approvers { get; set; }
        public IFormFile Photo { get; set; }
        public List<FamilyModel> FamilyDetails { get; set; }
    }
}
