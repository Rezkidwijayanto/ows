using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class NewRequestFile
    {
        public int Id { get; set; }
        public int OWS_request_id { get; set; }
        public string FilePath { get; set; }
        public string Filetype { get; set; }
        public string Workorder { get; set; }
    }

}
