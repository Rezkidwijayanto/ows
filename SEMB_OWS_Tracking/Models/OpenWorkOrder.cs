using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class OpenWorkOrder
    {
        public string OWS_ID { get; set; }
        public string Reason { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime Deadline { get; set; }
        public string Request_Type { get; set; }
        public string Work_Order { get; set; }
        public string Request_By { get; set; }
        public string Status { get; set; }
        public string Comment  { get; set; }
        public string Family { get; set; }

    }
}
