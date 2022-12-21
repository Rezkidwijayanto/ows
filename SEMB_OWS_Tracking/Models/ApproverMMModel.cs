using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEMB_OWS_Tracking.Models
{
    public class ApproverMMModel
    {
        public string ApproverMM { get; set; }
        public string LevelMM { get; set; }

        public string ApproverMMSESA { get; set; }

    }

    public class ApproverMTMModel
    {
        public string ApproverMTM { get; set; }
        public string LevelMTM { get; set; }
        public string ApproverMTMSESA { get; set; }

    }

    public class ApproverPEModel
    {
        public string ApproverPE { get; set; }
        public string LevelPE { get; set; }
        public string ApproverPESESA { get; set; }

    }

    public class ApproverQAModel
    {
        public string ApproverQA { get; set; }
        public string LevelQA { get; set; }
        public string ApproverQASESA { get; set; }

    }
}
