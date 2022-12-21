using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class DataModel
    {
        public string Family { get; set; }
        public string Code { get; set; }
        public string Product { get; set; }
        public string Version { get; set; }
        public string Work_Order { get; set; }
        public string Format { get; set; }


    }

    public class SQLDatabaseModel
    {
        public string STT { get; set; }
        public string Product { get; set; }
        public string Family { get; set; }
        public string Format { get; set; }
        public string Code { get; set; }
        public string Operation_Name { get; set; }
        public string Safety_Required { get; set; }
        public string Critical_Required { get; set; }
        public string Total_Set { get; set; }
        public string No_Pape { get; set; }
        public string Version { get; set; }
        public string Issued_Date { get; set; }
        public string Sector { get; set; }
        public string comment { get; set; }

    }

    public class MSTDetailsModel
    {
        public string STT { get; set; }
        public string Product { get; set; }
        public string Family { get; set; }
        public string Format { get; set; }
        public string Code { get; set; }
        public string Operation_Name { get; set; }
        public string Safety_Required { get; set; }
        public string Critical_Required { get; set; }
        public string Total_Set { get; set; }
        public string No_Pape { get; set; }
        public string Version { get; set; }
        public string Issued_Date { get; set; }
        public string Sector { get; set; }
        public string comment { get; set; }


    }
}
