using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace SEMB_OWS_Tracking.Services
{
    public class FileManagementService
    {
        public string FileUpload(IFormFile file)
        {
            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim();

            var mainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");

            if (!Directory.Exists(mainPath))
            {
                Directory.CreateDirectory(mainPath);
            }

            var filePath = Path.Combine(mainPath, file.FileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                { 
                    file.CopyTo(stream);
                }

                return filePath;
            }
            catch (Exception e)
            {
                return "Upload Fail";
            }
            
        } 
    }
}