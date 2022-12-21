using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class LoginModel
    {
        public int id { get; set; }
        public string sesa_id { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string level { get; set; }
        public string plant { get; set; }

    }
}
