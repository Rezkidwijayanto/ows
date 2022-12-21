using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace SEMB_OWS_Tracking.Models
{
    public class ImageModel
    {
        public string Title { get; set; }
        public IFormFile Photo { get; set; }
    }
}
