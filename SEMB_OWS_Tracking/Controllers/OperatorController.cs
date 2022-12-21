using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SEMB_OWS_Tracking.Models;
using System.Data.SqlClient;
using SEMB_OWS_Tracking.Function;
using Microsoft.AspNetCore.Http;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.IO.Compression;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SEMB_OWS_Tracking.Controllers
{
    public class OperatorController : Controller
    {
        int rowsAffected;
        string OWS_ID, Reason, Deadline, Request_Type, Request_By, Status, Family, Comments, Work_Order, LinkBox, OWS_ID_Product;
        string LevelMM, LevelQA, LevelPE, LevelMTM;
        

        public IActionResult DownloadDocument(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            string query = @"SELECT Filepath FROM tbl_OWS_request_file WHERE Workorder = @wo AND Filetype = 'File' AND plant = @plant";
            var outputStream = new MemoryStream();
            List<string> fetched_path = new List<string>();
            SqlDataReader rowread;
            using (SqlConnection con = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Parameters.AddWithValue("@wo", wo);
                    cmd.Parameters.AddWithValue("@plant", plant);

                    cmd.Connection = con;
                    con.Open();
                    rowread = cmd.ExecuteReader();
                    while (rowread.Read())
                    {
                        try
                        {
                            fetched_path.Add(rowread["Filepath"].ToString());
                        }
                        catch (Exception ex)
                        {
                            con.Close();
                            throw ex;
                        }
                    }
                    con.Close();
                }
            }
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (string path in fetched_path)
                    {
                        try
                        {
                            string fileName = Path.GetFileName(path);
                            archive.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Optimal);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                return File(memoryStream.ToArray(), System.Net.Mime.MediaTypeNames.Application.Octet, "Files.zip");
            }
        }
        private string getNextFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            int i = 0;
            while (System.IO.File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);
            }

            return fileName;
        }

        private readonly IHostingEnvironment hostingEnvironment;
        public OperatorController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
        }

        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();

            string dbString = dbAccess.ConnectionString;

            return dbString;
        }
        public string SelectWO(int id_string)
        {
            var plant = HttpContext.Session.GetString("plant");
            string querySelectWO = "SELECT Work_Order FROM tbl_OWS_request WHERE Id = '" + id_string + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Work_Order = dr["Work_Order"].ToString();
                }
            }
            connection.Close();
            return Work_Order;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequestFile(List<IFormFile> files, List<IFormFile> image, int id_string)
        {
            long size = files.Sum(f => f.Length);

            long size_image = image.Sum(f => f.Length);
            var plant = HttpContext.Session.GetString("plant");
            string checkPath = hostingEnvironment.WebRootPath + "\\Uploads";
            string checkPathImg = hostingEnvironment.WebRootPath + "\\Images";
            if (!Directory.Exists(checkPath))
            {
                Directory.CreateDirectory(checkPath);
            }

            if (!Directory.Exists(checkPathImg))
            {
                Directory.CreateDirectory(checkPathImg);
            }
            var filePaths = new List<string>();
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    string filePath = getNextFileName(hostingEnvironment.ContentRootPath + "\\Uploads\\" + formFile.FileName);

                    filePaths.Add(filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    string wo = SelectWO(id_string);

                    NewRequestFile data = new NewRequestFile();
                    data.Workorder = wo;
                    data.FilePath = filePath;

                    data.Filetype = "File";
                    string query = "INSERT INTO tbl_OWS_request_file (Workorder,Filepath,Filetype, plant) VALUES(@wo,@filepath,@Filetype, @plant)";
                    using (SqlConnection con = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Parameters.AddWithValue("@filepath", data.FilePath);
                            cmd.Parameters.AddWithValue("@wo", data.Workorder);
                            cmd.Parameters.AddWithValue("@Filetype", data.Filetype);
                            cmd.Parameters.AddWithValue("@plant", plant);
                            cmd.Connection = con;
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                }
            }
            foreach (var formFile in image)
            {
                if (formFile.Length > 0)
                {
                    string filePath = getNextFileName(hostingEnvironment.ContentRootPath + "\\Images\\" + formFile.FileName);

                    filePaths.Add(filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                    string wo = SelectWO(id_string);
                    NewRequestFile data = new NewRequestFile();
                    data.Workorder = wo;
                    data.FilePath = filePath;
                    data.Filetype = "Image";
                    string query = "INSERT INTO tbl_OWS_request_file (Workorder,Filepath,Filetype, plant) VALUES(@wo,@filepath,@Filetype, @plant)";
                    using (SqlConnection con = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Parameters.AddWithValue("@filepath", data.FilePath);
                            cmd.Parameters.AddWithValue("@wo", data.Workorder);
                            cmd.Parameters.AddWithValue("@Filetype", data.Filetype);
                            cmd.Parameters.AddWithValue("@plant", plant);

                            cmd.Connection = con;
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                }
            }
            return Ok(new { count = files.Count, size, size_image, filePaths });
        }

        [HttpGet]
        public IActionResult OpenWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<OpenWorkOrder> openWorkOrders = db.GetOpenWorkOrder(plant);
                List<FamilyModel> FamilyData = db.GetFamilyData(plant);
                List<ApproverMMModel> approverMMs = db.GetApproverMM(plant);
                List<ApproverQAModel> approverQAs = db.GetApproverQA(plant);
                List<ApproverPEModel> approverPEs = db.GetApproverPE(plant);
                List<ApproverMTMModel> approverMTMs = db.GetApproverMTM(plant);
                var dataModel = new ViewModel()
                {
                    FamilyDetails = FamilyData,
                    OpenWorkOrdersDetail = openWorkOrders,
                    approverMMModelsDetails = approverMMs,
                    approverQAModelsDetails = approverQAs,
                    approverPEModelsDetails = approverPEs,
                    approverMTMModelsDetails = approverMTMs
                };
                return View(dataModel);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }  
        }

        [HttpGet]
        public IActionResult ApprovalWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                List<Approval> approvals = new List<Approval>();
                string querySelect = "GET_APPROVAL_WORKORD";
                var plant = HttpContext.Session.GetString("plant");
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    SqlCommand cmd = new SqlCommand(querySelect, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@plant", plant);
                    cmd.CommandTimeout = 0;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            approvals.Add(new Approval
                            {
                                Reason = Convert.ToString(sdr["Reason"]),
                                Request_By = Convert.ToString(sdr["Request_By"]),
                                Request_Type = Convert.ToString(sdr["Request_Type"]),
                                Deadline = Convert.ToDateTime(sdr["Deadline"]),
                                Work_Order = Convert.ToString(sdr["Work_Order"]),
                                Family = Convert.ToString(sdr["Family"]),
                                Status = Convert.ToString(sdr["ApprovalStatus"]),
                                Comments = Convert.ToString(sdr["Comments"])
                            });
                        }
                    }
                    conn.Close();
                    
                }
                return View(approvals);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
            
        }

        [HttpGet]
        public IActionResult StartedWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<Started> starteds = db.GetStartedWorkOrder(plant);
                List<FamilyModel> FamilyData = db.GetFamilyData(plant);
                List<ApproverMMModel> approverMMs = db.GetApproverMM(plant);
                List<ApproverMTMModel> approverMTMs = db.GetApproverMTM(plant);
                List<ApproverQAModel> approverQAs = db.GetApproverQA(plant);
                List<ApproverPEModel> approverPEs = db.GetApproverPE(plant);

                var dataModel = new ViewModel()
                {
                    FamilyDetails = FamilyData,
                    StartedsDetail = starteds,
                    approverMMModelsDetails = approverMMs,
                    approverQAModelsDetails = approverQAs,
                    approverPEModelsDetails = approverPEs,
                    approverMTMModelsDetails = approverMTMs
                };

                return View(dataModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }         
        }

        [HttpGet]
        public IActionResult SubmittedWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var username = HttpContext.Session.GetString("SesaID");
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<Submitted> submitteds = db.GetSubmittedWorkOrder(username, plant);
                var dataModel = new ViewModel()
                {
                    SubmittedsDetail = submitteds,

                };
                return View(dataModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }          
        }

        [HttpGet]
        public IActionResult ApprovalTracking()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<Tracking> submitteds = db.GetTrackingData(plant);
                var dataModel = new ViewModel()
                {
                    TrackingDetails = submitteds,

                };
                return View(dataModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        
        [HttpGet]
        public IActionResult VerifiedWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<Verified> verifieds = db.GetVerifiedWorkOrder(plant);
                var dataModel = new ViewModel()
                {
                    VerifiedDetails = verifieds,

                };
                return View(dataModel);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpPost]
        public JsonResult StartnewRequest(string deadline_operator, string approver, string wo)
        {
            DateTime dl = DateTime.Parse(deadline_operator);
            Started started = new Started();
            started.Deadline = dl;
            started.Work_Order = wo;
            started.Approver = approver;
            int affectedRows;
            var plant = HttpContext.Session.GetString("plant");
            string queryUpdate = "UPDATE tbl_OWS_request SET Status = 'Started', ApproverMM = @Approver, Deadline = @Deadline WHERE Work_Order = @WO AND plant = @plant";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(queryUpdate))
                {
                    cmd.Parameters.AddWithValue("@WO", started.Work_Order);
                    cmd.Parameters.AddWithValue("@Approver", started.Approver);
                    cmd.Parameters.AddWithValue("@Deadline", started.Deadline);
                    cmd.Parameters.AddWithValue("@plant", plant);
                    cmd.Connection = conn;
                    conn.Open();
                    affectedRows = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            return Json(affectedRows);
        }

        public void CreateWorkOrder(int rowsAffected, string levelmm, string levelmtm, string levelqa, string levelpe, int id=0,string by = null, string[] approvers = null, string[] approver_names = null)
        {
            // 1 mm | 2 qa | 3 pe | 4 mtm
            var plant = HttpContext.Session.GetString("plant");
            string InsertSubmit = "INSERT INTO tbl_OWS_submitted " +
                                  "(CodeProduct, Code , Reason, Deadline, Request_Type, Request_By, Status, Family, Comments, Work_Order, ApproverMM, ApproverQA, ApproverPE, ApproverMTM,OWS_Request_id,Submit_By,ApproverMMName,ApproverQAName,ApproverPEName,ApproverMTMName,ApproverMMSesa,ApproverQASesa,ApproverPESesa,ApproverMTMSesa,plant) " +
                                  "VALUES " +
                                  "('" + OWS_ID_Product + "','" + OWS_ID + "','" + Reason + "', '" + Deadline + "', '" + Request_Type + "', '" + Request_By + "', '" + Status + "', '" + Family + "', N'" + Comments + "', '" + Work_Order + "', '" + levelmm + "', '" + levelqa + "', '" + levelpe + "', '" + levelmtm + "','"+id+ "','" + by + "','" + approvers[0] + "','" + approvers[1] + "','" + approvers[2] + "','" + approvers[3] + "','" + approver_names[0] + "','" + approver_names[1] + "','" + approver_names[2] + "','" + approver_names[3] + "','" + plant + "')";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd1 = new SqlCommand(InsertSubmit, conn))
                {
                    conn.Open();
                    rowsAffected = cmd1.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        [HttpPost]
        public JsonResult InsertSubmitted(string[] arr, string wo, string levelmm, string levelqa, string levelpe, string levelmtm, string[] boxlinks, string[] arr_sesa)
        {
            // 1 mm | 2 qa | 3 pe | 4 mtm
            var plant = HttpContext.Session.GetString("plant");
            Submitted started = new Submitted();
            started.Work_Order = wo;
            started.Approver = arr;
            int affectedRows = 0;
            Work_Order = wo;
            string querySelectWO = 
                @"SELECT Id, CodeProduct, Reason, Deadline, Request_Type, Request_By, Status, Family, Comments, Work_Order, Code 
                FROM tbl_OWS_request 
                WHERE Work_Order = '" + Work_Order + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            Status = "Submitted";
            string by = HttpContext.Session.GetString("SesaID");
            connection.Open();
            SqlCommand cmd = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    OWS_ID = dr["Code"].ToString();
                    OWS_ID_Product = dr["CodeProduct"].ToString();
                    Reason = dr["Reason"].ToString();
                    Deadline = dr["Deadline"].ToString();
                    Request_Type = dr["Request_Type"].ToString();
                    Request_By = dr["Request_By"].ToString();
                    Family = dr["Family"].ToString();
                    Comments = dr["Comments"].ToString();
                    CreateWorkOrder(affectedRows, levelmm, levelmtm, levelqa, levelpe,Convert.ToInt32(dr["Id"].ToString()),by,arr, arr_sesa);

                    //////THIS PART SEND EMAIL
                    List<string> operatorEmails = new List<string>();
                    //arr = [approver_mm,approver_qa,approver_pe,approver_mtm];
                    string getOperatorEmail = "SELECT email FROM mst_users";
                    string where = " WHERE ";
                    if(levelmm == "1") where += "(name = '"+ arr[0] +"' AND level = 'approver_mm' AND plant = '"+plant+"') ";
                    if(levelqa == "1")
                    {
                        if (where != "WHERE ") where += "OR ";
                        where += "(name = '" + arr[1] + "' AND level = 'approver_qa')  AND plant = '" + plant + "'";
                    }
                    if(levelpe == "1")
                    {
                        if (where != "WHERE ") where += "OR ";
                        where += "(name = '" + arr[2] + "' AND level = 'approver_pe')  AND plant = '" + plant + "'";
                    }
                    if (levelmtm == "1")
                    {
                        if (where != "WHERE ") where += "OR ";
                        where += "(name = '" + arr[3] + "' AND level = 'approver_mtm')  AND plant = '" + plant + "'";
                    }
                    if (levelmm == "1" || levelqa == "1" || levelpe == "1" || levelmtm == "1") getOperatorEmail += where;
                    using (SqlConnection conn = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd_1 = new SqlCommand(getOperatorEmail))
                        {
                            cmd_1.Connection = conn;
                            conn.Open();
                            using (SqlDataReader sdr = cmd_1.ExecuteReader())
                            {
                                while (sdr.Read())
                                {
                                    operatorEmails.Add(sdr["email"].ToString());
                                }
                            }
                            conn.Close();
                        }
                    }

                    string userEmail = "";
                    string getUserEmail = "SELECT email FROM mst_users WHERE sesa_id = '" + by + "' AND plant = '" + plant + "'";
                    using (SqlConnection conn = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd_1 = new SqlCommand(getUserEmail))
                        {
                            cmd_1.Connection = conn;
                            conn.Open();
                            using (SqlDataReader sdr = cmd_1.ExecuteReader())
                            {
                                if (sdr.Read())
                                {
                                    userEmail = sdr["email"].ToString();
                                }
                            }
                            conn.Close();
                        }
                    }

                    // Send email to OWS Operator
                    try
                    {
                        var email = new MimeMessage();
                        string emailContent = "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                            + "\r\n\r\nTime Send: [" + DateTime.Now + "]" 
                            + "\r\n\r\nRequest Type: " + Request_Type 
                            + "\r\n\r\nOWS ID: " + string.Join(",", OWS_ID)
                            + "\r\n\r\nWork Order: " + wo
                            + "\r\n\r\nFamily: " + Family 
                            + "\r\n\r\nReason : " + Reason 
                            + "\r\n\r\nComments / Bình luận: " + Comments
                            + "\r\n\r\nDeadline: " + Deadline
                            + "\r\n\r\nBoxlinks: " + string.Join("\r\n", boxlinks);
                        email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                        foreach (var email_to in operatorEmails)
                        {
                            email.To.Add(MailboxAddress.Parse(email_to));
                        }
                        if (userEmail != "") email.Cc.Add(MailboxAddress.Parse(userEmail));
                        email.Subject = "[OWS Application] Request Approval: " + Family;
                        email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                        // send email
                        using var smtp = new SmtpClient();
                        smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                        smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                        smtp.Send(email);
                        smtp.Disconnect(true);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }
            connection.Close();
            return Json(affectedRows);
        }

        public JsonResult Show_OWS(string wo)
        {
            var db = new DatabaseAccessLayer();
            var plant = HttpContext.Session.GetString("plant");
            DataSet ds = db.GetOWSID(wo, plant);
            List<SelectListItem> codelist = new List<SelectListItem>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                codelist.Add(new SelectListItem { Text = dr["CodeProduct"].ToString(), Value = dr["CodeProduct"].ToString() });
            }
            return Json(codelist);
        }

        [HttpPost]
        public JsonResult ReSubmit(string comment, string wo, string box_link, int id)
        {
            var plant = HttpContext.Session.GetString("plant");
            int rowsAffected;
            string UpdateBox_Link = "UPDATE tbl_box_link SET Link_Box = '"+box_link+"' WHERE Id = '"+id+"' AND WorkOrder = '"+wo+"' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd3 = new SqlCommand(UpdateBox_Link, conn))
                {
                    conn.Open();
                    rowsAffected = cmd3.ExecuteNonQuery();
                    conn.Close();
                }
            }

            
            return Json(rowsAffected);
        }


        [HttpPost]
        public JsonResult ConfirmResubmit(string comment, string wo, string verify_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            int rowsAffected;
            string InsertSubmit = "DELETE FROM tbl_OWS_verified WHERE Work_Order = '" + wo + "' AND plant='"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd1 = new SqlCommand(InsertSubmit, conn))
                {
                    conn.Open();
                    rowsAffected = cmd1.ExecuteNonQuery();
                    conn.Close();
                }
            }

            string UpdateSubmit = "UPDATE tbl_OWS_submitted SET Comments = N'" + comment + "' WHERE Work_Order = '" + wo + "' AND plant='" + plant + "' ";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd2 = new SqlCommand(UpdateSubmit, conn))
                {
                    conn.Open();
                    rowsAffected = cmd2.ExecuteNonQuery();
                    conn.Close();
                }
            }
            if (sendemailresubmit(comment, wo, verify_by)) return Json(rowsAffected);
            return Json(false);
        }

        public List<string> ApproversEmail(string verify_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            List<string> approver_emails = new List<string>();
            string query = "SELECT email FROM mst_users WHERE sesa_id = '"+verify_by+"' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(query))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            approver_emails.Add(sdr["email"].ToString());
                        }
                    }                   
                    conn.Close();
                }
            }
            return approver_emails;
        }

        private bool sendemailresubmit(string comment, string wo, string verify_by)
        {
            string level;
            string by = HttpContext.Session.GetString("SesaID"); 
            var plant = HttpContext.Session.GetString("plant");
            List<string> approver_emails = new List<string>();
            string getapprover = "SELECT ApproverMM, ApproverMTM, ApproverQA, ApproverPE FROM tbl_OWS_submitted WHERE Work_Order = '"+wo+"' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(getapprover))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            LevelMM = sdr["ApproverMM"].ToString();
                            LevelMTM = sdr["ApproverMTM"].ToString();
                            LevelQA = sdr["ApproverQA"].ToString();
                            LevelPE = sdr["ApproverPE"].ToString();
                        }
                    }

                    if(LevelMM == "1")
                    {
                        level = "approver_mm";
                        approver_emails = ApproversEmail(verify_by);
                    }
                    if (LevelMTM == "1")
                    {
                        level = "approver_mtm";
                        approver_emails = ApproversEmail(verify_by);
                    }
                    if (LevelQA == "1")
                    {
                        level = "approver_qa";
                        approver_emails = ApproversEmail(verify_by);
                    }
                    if (LevelPE == "1")
                    {
                        level = "approver_pe";
                        approver_emails = ApproversEmail(verify_by);
                    }

                    conn.Close();
                }
            }

            // Send email to OWS Operator
            try
            {
                var email = new MimeMessage();
                string emailContent = "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                    + "\r\n\r\nTime Send: [" + DateTime.Now + "]"
                    + "\r\n\r\nWork Order: " + wo
                    + "\r\n\r\nComment: " + comment
                    + "\r\n\r\nResubmit by: " + by;
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in approver_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }
                
                email.Subject = "[OWS Application] Resubmit Workorder " + wo;
                email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                // send email
                using var smtp = new SmtpClient();
                smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                smtp.Send(email);
                smtp.Disconnect(true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return true;
        }

        [HttpGet]
        public JsonResult GetLinkBoxVerify(string wo)
        {
            List<LinkBox> boxlink = new List<LinkBox>();
            var plant = HttpContext.Session.GetString("plant");
            string query = "SELECT Id, Link_Box FROM tbl_box_link WHERE WorkOrder = '" + wo + "' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new LinkBox();
                            data.ID = Convert.ToInt32(sdr["Id"]);
                            data.Box_link = sdr["Link_Box"].ToString();
                            boxlink.Add(data);
                        }
                    conn.Close();
                }
            }
            return Json(boxlink);
        }

        
        [HttpGet]
        public JsonResult GetChangeData(string wo)
        {
            List<DataModel> datalist = new List<DataModel>();
            var plant = HttpContext.Session.GetString("plant");

            string query = "GET_CHANGE_DATA";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(query);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.Parameters.AddWithValue("@wo", wo);
                cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                    while (sdr.Read())
                    {
                        datalist.Add(new DataModel
                        {
                            Code = sdr["Code"].ToString(),
                            Product = sdr["Product"].ToString(),
                            Work_Order = sdr["Work_Order"].ToString(),
                            Family = sdr["Family"].ToString(),
                            Version = sdr["Version"].ToString()
                        });
                    }
                conn.Close();
            
            }
            return Json(datalist);
        }

        [HttpPost]
        public JsonResult GetCodeData(string wo)
        {
            List<DataModel> datalist = new List<DataModel>();
            var plant = HttpContext.Session.GetString("plant");
            string query = "SELECT CodeProduct FROM tbl_OWS_request WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            datalist.Add(new DataModel
                            {
                                Code = sdr["CodeProduct"].ToString()                               
                            });
                        }
                    conn.Close();
                }
            }
            return Json(datalist);
        }

        public JsonResult ChangeVersion(string code, string product, string wo, string family, string version)
        {
            int result = 0;
            var plant = HttpContext.Session.GetString("plant");
            SqlConnection conn = new SqlConnection(DbConnection());
            if (version == "[A]")
            {
                string Insert = "INSERT INTO mst_data(Code, Version, plant) VALUES('" + code + "', '" + version + "', '" + plant + "') ";
                conn.Open();
                SqlCommand cmd = new SqlCommand(Insert, conn);
                result = cmd.ExecuteNonQuery();
                conn.Close();
            }
            else
            {
                string query = "UPDATE mst_data SET Version = '" + version + "' WHERE Code = '" + code + "' AND plant = '"+plant+"'";
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                result = cmd.ExecuteNonQuery();
                conn.Close();
            }
            if (sendemailchangeversion(code, product, wo, family, version)) return Json(rowsAffected);
            return Json(false);
        }

        private bool sendemailchangeversion(string code, string product, string wo ,string family, string version)
        {
            string by = HttpContext.Session.GetString("SesaID");
            var plant = HttpContext.Session.GetString("plant");
            List<string> operator_emails = new List<string>();
            string query_getemailoperator = "SELECT email FROM mst_users WHERE (level ='operator' OR level = 'supervisor') AND email IS NOT NULL AND email <> '' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(query_getemailoperator))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            operator_emails.Add(sdr["email"].ToString());
                        }
                    }
                    conn.Close();
                }
            }

            List<string> approver_emails = new List<string>();
            string getapproveremail = "SELECT email FROM mst_users WHERE level = 'approver_mm' AND plant = "+plant+"''";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(getapproveremail))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            approver_emails.Add(sdr["email"].ToString());
                        }
                    }
                    conn.Close();
                }
            }

            // Send email to OWS Operator
            try
            {
                var email = new MimeMessage();
                string emailContent = "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                    + "\r\n\r\nTime Send: [" + DateTime.Now + "]"
                    + "\r\n\r\nFamily: " + family
                    + "\r\n\r\nCode: " + code
                    + "\r\n\r\nProduct/ Sản phẩm: " + product
                    + "\r\n\r\nNew Version/ Mã phiên bản mới: " + version
                    + "\r\n\r\nWork Order: " + wo
                    + "\r\n\r\nChange by/ Người thay đổi: " + by;
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in approver_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }
                foreach (var email_cc in operator_emails)
                {
                    email.Cc.Add(MailboxAddress.Parse(email_cc));
                }
                email.Subject = "[OWS Application] Change Version Successful " + family;
                email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                // send email
                using var smtp = new SmtpClient();
                smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                smtp.Send(email);
                smtp.Disconnect(true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return true;
        }

        public JsonResult Insert_Box_Link(string boxlink, string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            int result = 0;
            string query = "INSERT INTO tbl_box_link(WorkOrder, Link_Box, plant) VALUES ('"+wo+"', '"+ boxlink + "', '"+plant+"')";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            conn.Open();
            result = cmd.ExecuteNonQuery();
            conn.Close();
            return Json(result);
        }

        public IActionResult NewRequest()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<FamilyModel> familylist = db.GetFamilyData(plant);
                List<ListApproverModel> approverList = db.GetAllApprover();
                List<ListReasonModel> reasonList = db.GetAllReason(plant);
                var dataModel = new ViewModel()
                {
                    NewFamilyDetails = familylist,
                    ListApprover = approverList,
                    ListReason = reasonList
                };
                return View(dataModel);

            }
            else
            {
                return RedirectToAction("Index", "Login");

            }

        }

        public IActionResult ChangeRequest()
        {
            if (HttpContext.Session.GetString("level") == "operator" || HttpContext.Session.GetString("level") == "admin")
            {
                var db = new DatabaseAccessLayer();
                var plant = HttpContext.Session.GetString("plant");
                DataSet ds = db.Get_Family(plant);
                List<SelectListItem> famiylist = new List<SelectListItem>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    famiylist.Add(new SelectListItem { Text = dr["Family"].ToString(), Value = dr["Family"].ToString() });
                }
                List<ListApproverModel> approverList = db.GetAllApprover();
                List<ListReasonModel> reasonList = db.GetAllReason(plant);
                var dataSelectChange = new ViewModel()
                {
                    ListApprover = approverList,
                    ListReason = reasonList,
                };
                ViewBag.Family = famiylist;
                return View(dataSelectChange);

            }
            else
            {
                return RedirectToAction("Index", "Login");

            }

        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("level") == "operator")
            {
                List<User> users = new List<User>();
                string querySelect = @"SELECT TOP 1 id_user, sesa_id , name, level FROM mst_users WHERE id_user = @ID";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.Parameters.AddWithValue("@ID", HttpContext.Session.GetInt32("id"));
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                users.Add(new User
                                {
                                    ID = Convert.ToInt32(sdr["id_user"]),
                                    SESA_ID = Convert.ToString(sdr["sesa_id"]),
                                    Name = Convert.ToString(sdr["name"]),
                                    Level = Convert.ToString(sdr["level"])
                                });
                            }
                        }
                        conn.Close();
                    }
                }

                return View(users);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpPost]

        public JsonResult AddNewRequest(string OWS_ID, string Reason, string deadline, string type, string wo, string by, string status, string family, string comment, string[] approvers)
        {
            NewRequest newRequest = new NewRequest();
            newRequest.OWS_ID = OWS_ID;
            newRequest.Reason = Reason;
            newRequest.Deadline = deadline;
            newRequest.Request_Type = type;
            newRequest.Work_Order = wo;
            newRequest.Family = family;
            newRequest.Comment = comment;
            newRequest.Approvers = approvers;
            type = "New";
            status = "Open";
            by = HttpContext.Session.GetString("SesaID");
            var plant = HttpContext.Session.GetString("plant");
            SqlDataReader rowread;
            int InsertedID;
            List<string> operator_emails = new List<string>();
            string query_getemailoperator = "SELECT email FROM mst_users WHERE level ='operator' AND email IS NOT NULL AND email <> '' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query_getemailoperator))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            operator_emails.Add(sdr["email"].ToString());
                        }
                    }
                    conn.Close();
                }
            }

            string user_email = "";
            string query_getemailuser = "SELECT email FROM mst_users WHERE sesa_id = '" + by + "' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query_getemailuser))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            user_email = sdr["email"].ToString();
                        }
                    }
                    conn.Close();
                }
            }

            for (var i = 0; i < approvers.Length; i++)
            {
                var item = approvers[i];
            }
            string queryInsert = "INSERT INTO tbl_OWS_request (CodeProduct, Reason, Deadline, Request_Type,Work_Order, Request_By, Status, Comments, Family, Code, plant)" +
                "OUTPUT inserted.Id " + "VALUES(@OWS_ID, @Reason,@Deadline,@Type, replace(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':',''), @By,@Status, N'" + comment + "', @Family, @Code, @plant )";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(queryInsert))
                {
                    cmd.Parameters.AddWithValue("@OWS_ID", newRequest.OWS_ID);
                    cmd.Parameters.AddWithValue("@Reason", newRequest.Reason);
                    cmd.Parameters.AddWithValue("@Deadline", newRequest.Deadline);
                    cmd.Parameters.AddWithValue("@Family", newRequest.Family);
                    cmd.Parameters.AddWithValue("@Type", type);
                    cmd.Parameters.AddWithValue("@By", by);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Code", newRequest.OWS_ID);
                    cmd.Parameters.AddWithValue("@plant", plant);
                    cmd.Connection = conn;
                    conn.Open();
                    try
                    {
                        rowread = cmd.ExecuteReader();
                        if (rowread.HasRows)
                        {
                            rowread.Read();
                            InsertedID = Convert.ToInt32(rowread["Id"]);
                            conn.Close();

                            // Send email to OWS Operator
                            try
                            {
                                var email = new MimeMessage();
                                string emailContent = "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                                    + "\r\n\r\nTime Send: [" + DateTime.Now + "]"
                                    + "\r\n\r\nRequest Type: New OWS / Yêu Cầu: Tạo OWS Mới"
                                    + "\r\n\r\nWork Order: " + Work_Order
                                    + "\r\n\r\nOWS ID: " + OWS_ID
                                    + "\r\n\r\nFamily: " + family
                                    + "\r\n\r\nReason : " + Reason
                                    + "\r\n\r\nDeadline: " + deadline
                                    + "\r\n\r\nApproving Depts: " + string.Join(",", approvers);
                                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                                foreach (var email_to in operator_emails)
                                {
                                    email.To.Add(MailboxAddress.Parse(email_to));
                                }
                                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                                email.Subject = "[OWS Application] New OWS Request: " + family;
                                email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                                // send email
                                using var smtp = new SmtpClient();
                                smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                                smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                                smtp.Send(email);
                                smtp.Disconnect(true);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                            return Json(InsertedID);
                        }
                        else
                        {
                            conn.Close();
                            return Json(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }
        }

        public JsonResult ChangePass(int id, string oldpsw, string newpsw)
        {

            var hashpassword = new Authentication();

            User user = new User();
            string Oldpassword = hashpassword.MD5Hash(oldpsw);
            string Newpassword = hashpassword.MD5Hash(newpsw);
            int Excute = 0;
            string query = @"SELECT TOP 1 id_user FROM mst_users WHERE id_user = @id AND password = @password";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@password", Oldpassword);
                    conn.Open();
                    using (SqlDataReader checkpw = cmd.ExecuteReader())
                    {
                        if (checkpw.Read())
                        {
                            cmd.Parameters.Clear();
                            cmd.CommandText = "UPDATE mst_users SET password = @password WHERE id_user = @id";
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@password", Newpassword);
                        }
                    }
                    Excute = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            return Json(Excute);
        }

        [HttpPost]

        public JsonResult changeRequest(string family, string reason, string deadline, string[] codeproduct, string[] ows_id, string type, string wo, string by, string status, string comments, string[] approvers)
        {
            OperatorModel changeRequest = new OperatorModel();
            //int rowsAffected;
            changeRequest.Reason = reason;
            changeRequest.Deadline = deadline;
            changeRequest.Request_Type = type;
            changeRequest.Work_Order = wo;
            changeRequest.OWS_ID = codeproduct;
            changeRequest.Family = family;
            changeRequest.Comments = comments;
            type = "Change";
            status = "Open";
            by = HttpContext.Session.GetString("SesaID");
            var plant = HttpContext.Session.GetString("plant");
            SqlDataReader rowread;
            int InsertedID;
            string querySelectWO = "select replace(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','') as Work_Order";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    Work_Order = dr["Work_Order"].ToString();
                }
            }
            connection.Close();

            for (var i = 0; i < codeproduct.Length; i++)
            {

                var j = i;

                var item = codeproduct[i];
                var item1 = ows_id[j];

                string queryInsert = "INSERT INTO tbl_OWS_request (CodeProduct,Reason, Deadline, Request_Type, Work_Order, Request_By, Status, Comments, Family, Code, plant) VALUES(@CodeProduct, @Reason, @Deadline ,@Type, '" + Work_Order + "' , @By, @Status,N'" + comments + "', @Family, @Code, @plant)";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd1 = new SqlCommand(queryInsert))
                    {
                        cmd1.Parameters.AddWithValue("@CodeProduct", item.ToString());
                        cmd1.Parameters.AddWithValue("@Reason", changeRequest.Reason);
                        cmd1.Parameters.AddWithValue("@Deadline", changeRequest.Deadline);
                        cmd1.Parameters.AddWithValue("@Type", type);
                        cmd1.Parameters.AddWithValue("@By", by);
                        cmd1.Parameters.AddWithValue("@Status", status);
                        cmd1.Parameters.AddWithValue("@Family", changeRequest.Family);
                        cmd1.Parameters.AddWithValue("@Code", item1.ToString());
                        cmd1.Parameters.AddWithValue("@plant", plant);
                        cmd1.Connection = conn;
                        conn.Open();
                        rowsAffected = cmd1.ExecuteNonQuery();
                        conn.Close();
                    }
                }


            }

            //////THIS PART SEND EMAIL
            List<string> operator_emails = new List<string>();
            string query_getemailoperator = "SELECT email FROM mst_users WHERE level ='operator' AND email IS NOT NULL AND email <> '' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(query_getemailoperator))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            operator_emails.Add(sdr["email"].ToString());
                        }
                    }
                    conn.Close();
                }
            }

            string user_email = "";
            string query_getemailuser = "SELECT email FROM mst_users WHERE sesa_id = '" + by + "' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd_1 = new SqlCommand(query_getemailuser))
                {
                    cmd_1.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd_1.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            user_email = sdr["email"].ToString();
                        }
                    }
                    conn.Close();
                }
            }

            // Send email to OWS Operator
            try
            {
                var email = new MimeMessage();
                string emailContent = "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                                    + "\r\n\r\nTime Send: [" + DateTime.Now + "]"
                                    + "\r\n\r\n" + "Request Type: Change OWS"
                                    + "\r\n\r\nWork Order Number: " + Work_Order
                                    + "\r\n\r\nOWS ID: " + string.Join(",", ows_id)
                                    + "\r\n\r\nFamily: " + family
                                    + "\r\n\r\nReason : " + reason
                                    + "\r\n\r\nDeadline: " + deadline
                                    + "\r\n\r\nApproving Depts: " + string.Join(",", approvers);
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in operator_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }
                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                email.Subject = "[OWS Application] Change OWS Request: " + family;
                email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                // send email
                using var smtp = new SmtpClient();
                smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                smtp.Send(email);
                smtp.Disconnect(true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            string select = "SELECT Id FROM tbl_OWS_request WHERE Work_Order = '" + Work_Order + "'";
            SqlConnection con = new SqlConnection(DbConnection());
            SqlCommand cmd2 = new SqlCommand(select, con);
            try
            {
                con.Open();
                rowread = cmd2.ExecuteReader();
                if (rowread.HasRows)
                {
                    rowread.Read();
                    InsertedID = Convert.ToInt32(rowread["Id"]);
                    con.Close();
                    return Json(InsertedID);
                }
                else
                {
                    con.Close();
                    return Json(false);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
