using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SEMB_OWS_Tracking.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SEMB_OWS_Tracking.Function;
using Microsoft.AspNetCore.Http;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using Microsoft.AspNetCore.Hosting;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SEMB_OWS_Tracking.Controllers
{
    public class ApproverController : Controller
    {
        string OWS_ID;
        string Work_Order;

        int rowsAffected;

        //string rowsAffected;
        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();

            string dbString = dbAccess.ConnectionString;

            return dbString;
        }

        string approvalmm, approvalqa, approvalpe;

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

        public ApproverController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
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
                    string filePath =
                        getNextFileName(hostingEnvironment.ContentRootPath + "\\Uploads\\" + formFile.FileName);

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
                    string query =
                        "INSERT INTO tbl_OWS_request_file (Workorder,Filepath,Filetype, plant) VALUES(@wo,@filepath,@Filetype,@plant)";
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
                    string filePath =
                        getNextFileName(hostingEnvironment.ContentRootPath + "\\Images\\" + formFile.FileName);

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
                    string query =
                        "INSERT INTO tbl_OWS_request_file (Workorder,Filepath,Filetype, plant) VALUES(@wo,@filepath,@Filetype, @plant)";
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

            return Ok(new {count = files.Count, size, size_image, filePaths});
        }

        [HttpGet]
        public IActionResult OpenWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm" ||
                HttpContext.Session.GetString("level") == "approver_qa" ||
                HttpContext.Session.GetString("level") == "approver_pe" ||
                HttpContext.Session.GetString("level") == "approver_mtm")
            {
                string userLogin = HttpContext.Session.GetString("SesaID");
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<Submitted> submitteds = db.GetApproverSubmittedWorkOrder(userLogin, plant);
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

        [HttpPost]
        public JsonResult DeleteWO(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            int result = 0;
            string Delete = "DELETE FROM tbl_OWS_request WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            SqlConnection con = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(Delete, con);
            con.Open();
            result = cmd.ExecuteNonQuery();
            con.Close();
            return Json(result);
        }

        public string CheckExistApprovedWorkOrder(string WorkOrder)
        {
            var plant = HttpContext.Session.GetString("plant");
            string querySelectWO = "SELECT Code FROM tbl_OWS_submitted WHERE Work_Order = '" + WorkOrder + "' AND plant = '"+plant+"'";
            string chkApproverMM = "SELECT Work_Order FROM tbl_OWS_approved WHERE Work_Order = '" + WorkOrder + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd1 = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd1.ExecuteReader())
            {
                while (dr.Read())
                {
                    OWS_ID = dr["Code"].ToString();
                }
            }

            SqlCommand cmd2 = new SqlCommand(chkApproverMM, connection);
            using (SqlDataReader dr = cmd2.ExecuteReader())
            {
                while (dr.Read())
                {
                    Work_Order = dr["Work_Order"].ToString();
                }
            }

            connection.Close();

            return Work_Order;
        }

        public string CheckExistWorkOrder(string WorkOrder)
        {
            var plant = HttpContext.Session.GetString("plant");
            string querySelectWO = "SELECT Code FROM tbl_OWS_submitted WHERE Work_Order = '" + WorkOrder + "' AND plant = '"+plant+"'";
            string chkApproverMM = "SELECT Work_Order FROM tbl_OWS_verified WHERE Work_Order = '" + WorkOrder + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd1 = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd1.ExecuteReader())
            {
                while (dr.Read())
                {
                    OWS_ID = dr["Code"].ToString();
                }
            }

            SqlCommand cmd2 = new SqlCommand(chkApproverMM, connection);
            using (SqlDataReader dr = cmd2.ExecuteReader())
            {
                while (dr.Read())
                {
                    Work_Order = dr["Work_Order"].ToString();
                }
            }

            connection.Close();

            return Work_Order;
        }

        public IActionResult WorkOrderTracking()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm" ||
                HttpContext.Session.GetString("level") == "approver_qa" ||
                HttpContext.Session.GetString("level") == "approver_pe" ||
                HttpContext.Session.GetString("level") == "approver_mtm")
            {
                var plant = HttpContext.Session.GetString("plant");
                List<WOTracking> approverWOs = new List<WOTracking>();
                string querySelect = "GET_WORK_ORDER_TRACK";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                approverWOs.Add(new WOTracking
                                {
                                    Work_Order = Convert.ToString(sdr["Work_Order"]),
                                    Request_Type = Convert.ToString(sdr["Request_Type"]),
                                    Request_By = Convert.ToString(sdr["Request_By"]),
                                    Reason = Convert.ToString(sdr["Reason"]),
                                    Deadline = Convert.ToString(sdr["Deadline"]),
                                    Family = Convert.ToString(sdr["Family"]),
                                    Comments = Convert.ToString(sdr["Comments"]),
                                    Status = Convert.ToString(sdr["WOStatus"])
                                });
                            }
                        }

                        conn.Close();
                    }
                }

                return View(approverWOs);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        public void InsertVerifyComment(string verify_comment, string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            string Update = "UPDATE tbl_OWS_verified SET VerifyComments = N'" + verify_comment +
                            "' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd1 = new SqlCommand(Update, connection);
            cmd1.ExecuteNonQuery();
        }

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

        [HttpGet]
        public IActionResult GetImage(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            string fetched_path = "";
            string query ="SELECT TOP (1) Filepath FROM tbl_OWS_request_file WHERE Workorder = @wo AND Filetype = 'Image' AND plant = @plant";
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
                    if (rowread.Read())
                    {
                        try
                        {
                            fetched_path = rowread["Filepath"].ToString();
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

            var image = System.IO.File.OpenRead(fetched_path);
            string fileName = Path.GetFileName(fetched_path);
            return File(image, "image/jpeg");
        }

        [HttpGet]
        public IActionResult ApprovalMM()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm" || HttpContext.Session.GetString("level") == "admin")
            {
                List<Approval> approverWOs = new List<Approval>();
                string sesa = "";
                if(HttpContext.Session.GetString("level") == "admin")
                {
                    sesa = "all";
                }
                else
                {
                    sesa = HttpContext.Session.GetString("SesaID");
                }
                var plant = HttpContext.Session.GetString("plant");
                string querySelect = "GET_APPROVAL_MM"; 

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Parameters.AddWithValue("@sesaid", sesa);
                        cmd.CommandTimeout = 0;
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                approverWOs.Add(new Approval
                                {
                                    Work_Order = Convert.ToString(sdr["Work_Order"]),
                                    Request_Type = Convert.ToString(sdr["Request_Type"]),
                                    Request_By = Convert.ToString(sdr["Request_By"]),
                                    Reason = Convert.ToString(sdr["Reason"]),
                                    Deadline = Convert.ToDateTime(sdr["Deadline"]),
                                    Family = Convert.ToString(sdr["Family"]),
                                    Status = Convert.ToString(sdr["Status"]),
                                    Comments = Convert.ToString(sdr["Comments"]),
                                    Link_Box = Convert.ToString(sdr["Link_Box"]),
                                    Submit_By = Convert.ToString(sdr["Submit_By"]),
                                });
                            }
                        }

                        conn.Close();
                    }
                }

                return View(approverWOs);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public IActionResult ApprovalQA()
        {
            if (HttpContext.Session.GetString("level") == "approver_qa" || HttpContext.Session.GetString("level") == "admin")
            {
                List<Approval> approverWOs = new List<Approval>();
                string sesa = "";
                if (HttpContext.Session.GetString("level") == "admin")
                {
                    sesa = "all";
                }
                else
                {
                    sesa = HttpContext.Session.GetString("SesaID");
                }
                var plant = HttpContext.Session.GetString("plant");

                string querySelect = "GET_APPROVAL_QA";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Parameters.AddWithValue("@sesaid", sesa);
                        cmd.CommandTimeout = 0;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                approverWOs.Add(new Approval
                                {
                                    Work_Order = Convert.ToString(sdr["Work_Order"]),
                                    Request_Type = Convert.ToString(sdr["Request_Type"]),
                                    Request_By = Convert.ToString(sdr["Request_By"]),
                                    Reason = Convert.ToString(sdr["Reason"]),
                                    Deadline = Convert.ToDateTime(sdr["Deadline"]),
                                    Family = Convert.ToString(sdr["Family"]),
                                    Status = Convert.ToString(sdr["Status"]),
                                    Comments = Convert.ToString(sdr["Comments"]),
                                    Link_Box = Convert.ToString(sdr["Link_Box"]),
                                    // Submit_By = Convert.ToString(sdr["Submit_By"]),
                                });
                            }
                        }

                        conn.Close();
                    }
                }

                return View(approverWOs);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public IActionResult ApprovalPE()
        {
            if (HttpContext.Session.GetString("level") == "approver_pe" || HttpContext.Session.GetString("level") == "admin")
            {
                List<Approval> approverWOs = new List<Approval>();
                string sesa = "";
                if (HttpContext.Session.GetString("level") == "admin")
                {
                    sesa = "all";
                }
                else
                {
                    sesa = HttpContext.Session.GetString("SesaID");
                }
                var plant = HttpContext.Session.GetString("plant");
                string querySelect = "GET_APPROVAL_PE";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Parameters.AddWithValue("@sesaid", sesa);
                        cmd.CommandTimeout = 0;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                approverWOs.Add(new Approval
                                {
                                    Work_Order = Convert.ToString(sdr["Work_Order"]),
                                    Request_Type = Convert.ToString(sdr["Request_Type"]),
                                    Request_By = Convert.ToString(sdr["Request_By"]),
                                    Reason = Convert.ToString(sdr["Reason"]),
                                    Deadline = Convert.ToDateTime(sdr["Deadline"]),
                                    Family = Convert.ToString(sdr["Family"]),
                                    Status = Convert.ToString(sdr["Status"]),
                                    Comments = Convert.ToString(sdr["Comments"]),
                                    Link_Box = Convert.ToString(sdr["Link_Box"]),
                                    Submit_By = Convert.ToString(sdr["Submit_By"]),
                                });
                            }
                        }

                        conn.Close();
                    }
                }

                return View(approverWOs);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpGet]
        public IActionResult ApprovalMTM()
        {
            if (HttpContext.Session.GetString("level") == "approver_mtm" || HttpContext.Session.GetString("level") == "admin")
            {
                List<Approval> approverWOs = new List<Approval>();
                string sesa = "";
                if (HttpContext.Session.GetString("level") == "admin")
                {
                    sesa = "all";
                }
                else
                {
                    sesa = HttpContext.Session.GetString("SesaID");
                }
                var plant = HttpContext.Session.GetString("plant");

                string querySelect = "GET_APPROVAL_MTM";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Parameters.AddWithValue("@sesaid", sesa);
                        cmd.CommandTimeout = 0;

                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                approverWOs.Add(new Approval
                                {
                                    Work_Order = Convert.ToString(sdr["Work_Order"]),
                                    Request_Type = Convert.ToString(sdr["Request_Type"]),
                                    Request_By = Convert.ToString(sdr["Request_By"]),
                                    Reason = Convert.ToString(sdr["Reason"]),
                                    Deadline = Convert.ToDateTime(sdr["Deadline"]),
                                    Family = Convert.ToString(sdr["Family"]),
                                    Status = Convert.ToString(sdr["Status"]),
                                    Comments = Convert.ToString(sdr["Comments"]),
                                    Link_Box = Convert.ToString(sdr["Link_Box"]),
                                    Submit_By = Convert.ToString(sdr["Submit_By"]),
                                });
                            }
                        }

                        conn.Close();
                    }
                }

                return View(approverWOs);
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpPost]
        public IActionResult ApproveWorkOrderRequest([FromBody] ApproveWorkOrder approveWo)
        {
            approveWo.Status = "Approved";
            return Ok();
        }

        [HttpPost]
        public JsonResult AddApprovalMM(string wo, string reason, string deadline, string type, string family,
            string by, string status, string submit_by)
        {
            DateTime dl = DateTime.Parse(deadline);
            var plant = HttpContext.Session.GetString("plant");

            Approval approval = new Approval();
            approval.Work_Order = wo;
            approval.Reason = reason;
            approval.Deadline = dl;
            approval.Family = family;
            approval.Request_Type = type;
            approval.Request_By = by;
            approval.Status = status;
            approval.Submit_By = submit_by;
            status = "Approved";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());
            string approvermm = "1";
            Work_Order = CheckExistApprovedWorkOrder(wo);
            if (Work_Order == null)
            {
                string queryInsert ="INSERT INTO tbl_OWS_approved (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverMM, plant) VALUES (@reason, @deadline, @Type, @wo, @By, @Status, @Family, '" +
                    approvermm + "', @plant)";

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(queryInsert))
                    {
                        cmd.Parameters.AddWithValue("@reason", approval.Reason);
                        cmd.Parameters.AddWithValue("@deadline", approval.Deadline);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@wo", approval.Work_Order);
                        cmd.Parameters.AddWithValue("@By", approval.Request_By);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Family", approval.Family);
                        cmd.Parameters.AddWithValue("@plant", plant);

                        cmd.Connection = conn;
                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_approved SET ApproverMM = '" + approvermm +"' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailapprove(approval)) return Json(rowsAffected);
            return Json(false);
        }

        [HttpPost]
        public JsonResult AddApprovalQA(string wo, string reason, string deadline, string type, string family,
            string by, string status, string submit_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            DateTime dl = DateTime.Parse(deadline);
            Approval approval = new Approval();
            approval.Work_Order = wo;
            approval.Reason = reason;
            approval.Deadline = dl;
            approval.Family = family;
            approval.Request_Type = type;
            approval.Request_By = by;
            approval.Status = status;
            approval.Submit_By = submit_by;
            status = "Approved";
            string approverqa = "1";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());

            Work_Order = CheckExistApprovedWorkOrder(wo);

            if (Work_Order == null)
            {
                string queryInsert ="INSERT INTO tbl_OWS_approved (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverQA, plant) VALUES(@reason, @deadline,@Type, @wo, @By, @Status, @Family, '" +
                    approverqa + "', @plant)";

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(queryInsert))
                    {
                        cmd.Parameters.AddWithValue("@reason", approval.Reason);
                        cmd.Parameters.AddWithValue("@deadline", approval.Deadline);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@wo", approval.Work_Order);
                        cmd.Parameters.AddWithValue("@By", approval.Request_By);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Family", approval.Family);
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_approved SET ApproverQA = '" + approverqa + "', QA_Timestamp = GETDATE() WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailapprove(approval)) return Json(rowsAffected);
            return Json(false);
        }

        [HttpPost]
        public JsonResult AddApprovalPE(string wo, string reason, string deadline, string type, string family,
            string by, string status, string submit_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            DateTime dl = DateTime.Parse(deadline);
            Approval approval = new Approval();
            approval.Work_Order = wo;
            approval.Reason = reason;
            approval.Deadline = dl;
            approval.Family = family;
            approval.Request_Type = type;
            approval.Request_By = by;
            approval.Status = status;
            approval.Submit_By = status;
            status = "Approved";
            int rowsAffected;
            string approverpe = "1";
            CheckExistApprovedWorkOrder(wo);
            SqlConnection connection = new SqlConnection(DbConnection());
            Work_Order = CheckExistApprovedWorkOrder(wo);
            if (Work_Order == null)
            {
                string queryInsert =
                    "INSERT INTO tbl_OWS_approved (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverPE, plant) VALUES(@reason, @deadline,@Type, @wo, @By, @Status, @Family, '" +
                    approverpe + "', @plant)";

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(queryInsert))
                    {
                        cmd.Parameters.AddWithValue("@reason", approval.Reason);
                        cmd.Parameters.AddWithValue("@deadline", approval.Deadline);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@wo", approval.Work_Order);
                        cmd.Parameters.AddWithValue("@By", approval.Request_By);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Family", approval.Family);
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_approved SET ApproverPE = '" + approverpe +"', PE_Timestamp = GETDATE() WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailapprove(approval)) return Json(rowsAffected);
            return Json(false);
        }

        [HttpPost]
        public JsonResult AddApprovalMTM(string wo, string reason, string deadline, string type, string family,
            string by, string status, string submit_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            DateTime dl = DateTime.Parse(deadline);
            Approval approval = new Approval();
            approval.Work_Order = wo;
            approval.Reason = reason;
            approval.Deadline = dl;
            approval.Family = family;
            approval.Request_Type = type;
            approval.Request_By = by;
            approval.Status = status;
            approval.Submit_By = submit_by;
            status = "Approved";
            int rowsAffected;
            string approvermtm = "1";
            CheckExistApprovedWorkOrder(wo);
            SqlConnection connection = new SqlConnection(DbConnection());
            Work_Order = CheckExistApprovedWorkOrder(wo);
            if (Work_Order == null)
            {
                string queryInsert =
                    "INSERT INTO tbl_OWS_approved (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverMTM, plant) VALUES(@reason, @deadline,@Type, @wo, @By, @Status, @Family, '" +
                    approvermtm + "', @plant)";

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(queryInsert))
                    {
                        cmd.Parameters.AddWithValue("@reason", approval.Reason);
                        cmd.Parameters.AddWithValue("@deadline", approval.Deadline);
                        cmd.Parameters.AddWithValue("@Type", type);
                        cmd.Parameters.AddWithValue("@wo", approval.Work_Order);
                        cmd.Parameters.AddWithValue("@By", approval.Request_By);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Family", approval.Family);
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_approved SET ApproverMTM = '" + approvermtm +"', MTM_Timestamp = GETDATE() WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailapprove(approval)) return Json(rowsAffected);
            return Json(false);
        }

        string Reason, Deadline, Request_Type, Request_By, Status, Family, Comments, LinkBox;

        public void SelectSubmit(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            string querySelect = "SELECT * FROM tbl_OWS_submitted WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd1 = new SqlCommand(querySelect, connection);
            Verified verified = new Verified();
            using (SqlDataReader sdr = cmd1.ExecuteReader())
            {
                while (sdr.Read())
                {
                    Reason = Convert.ToString(sdr["Reason"]);
                    Request_By = Convert.ToString(sdr["Request_By"]);
                    Request_Type = Convert.ToString(sdr["Request_Type"]);
                    Deadline = Convert.ToString(sdr["Deadline"]);
                    Work_Order = Convert.ToString(sdr["Work_Order"]);
                    Status = Convert.ToString(sdr["Status"]);
                    Family = Convert.ToString(sdr["Family"]);
                }
            }
        }

        [HttpPost]
        public JsonResult AddVerifyMM(string wo, string verify_comment, string submit_by)
        {
            var plant = HttpContext.Session.GetString("plant");
            string verify_by = HttpContext.Session.GetString("SesaID");
            Verified verified = new Verified();
            verified.Work_Order = wo;
            verified.VerifyComment = verify_comment;
            verified.Verify_By = verify_by;
            string status = "Verified";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());
            string approvermm = "1";
            Work_Order = CheckExistWorkOrder(wo);

            if (Work_Order == null)
            {
                SelectSubmit(wo);
                string queryInsert =
                    "INSERT INTO tbl_OWS_verified (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverMM, VerifyComments, Verify_By, plant) VALUES('" +
                    Reason + "', '" + Deadline + "', '" + Request_Type + "', '" + wo + "' , '" + Request_By + "', '" +
                    status + "', '" + Family + "', '" + approvermm + "', N'" + verify_comment + "', '" + verify_by +
                    "', '"+plant+"')";
                SqlCommand cmd1 = new SqlCommand(queryInsert, connection);
                connection.Open();
                rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_verified SET ApproverMM = '" + approvermm +
                                     "' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailverify(Reason, Deadline, Request_Type, submit_by, wo, status, Family, approvermm,
                verify_comment)) return Json(rowsAffected);
            return Json(false);
        }

        private bool sendemailverify(string reason, string deadline, string request_type, string submit_by, string wo,
            string status, string family, string approver, string verify_comment)
        {
            string by = HttpContext.Session.GetString("SesaID");
            var plant = HttpContext.Session.GetString("plant");
            List<string> operator_emails = new List<string>();
            string query_getemailoperator =
                "SELECT email FROM mst_users WHERE (level ='operator') AND email IS NOT NULL AND email <> '' AND name ='" +
                submit_by + "' AND plant = '"+plant+"'";
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
                                      + "\r\n\r\nRequest Type: " + request_type
                                      + "\r\n\r\nFamily: " + family
                                      + "\r\n\r\nWork Order: " + wo
                                      + "\r\n\r\nReason: " + reason
                                      + "\r\n\r\nDeadline: " + deadline
                                      + "\r\n\r\nApprover: " + by
                                      + "\r\n\r\nComment: " + verify_comment;
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in operator_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }

                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                email.Subject = "[OWS Application] Tracking Approval Fail - Request Verification: " + family;
                email.Body = new TextPart(TextFormat.Plain) {Text = emailContent};

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

        private bool sendemailapprove(Approval approval)
        {
            string by = HttpContext.Session.GetString("SesaID");
            var plant = HttpContext.Session.GetString("plant");

            List<string> operator_emails = new List<string>();
            string query_getemailoperator =
                "SELECT email FROM mst_users WHERE (level ='operator' OR level = 'user') AND email IS NOT NULL AND email <> '' AND name='" +
                approval.Submit_By + "' AND plant = '"+plant+"'";
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
                                      + "\r\n\r\nRequest Type: " + approval.Request_Type
                                      //+ "\r\n\r\nOWS ID: " + string.Join(",", approval.OWS_ID)
                                      + "\r\n\r\nWork Order Number: " + approval.Work_Order
                                      + "\r\n\r\nFamily: " + approval.Family
                                      + "\r\n\r\nReason: " + approval.Reason
                                      + "\r\n\r\nDeadline: " + approval.Deadline
                                      + "\r\n\r\nApprover: " + by;
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in operator_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }

                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                email.Subject = "[OWS Application] Tracking Approval Success: " + approval.Family;
                email.Body = new TextPart(TextFormat.Plain) {Text = emailContent};

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

        [HttpPost]
        public JsonResult AddVerifyMTM(string wo, string verify_comment)
        {
            var plant = HttpContext.Session.GetString("plant");
            Verified verified = new Verified();
            verified.Work_Order = wo;
            verified.VerifyComment = verify_comment;
            string status = "Verified";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());
            string approvermtm = "1";
            Work_Order = CheckExistWorkOrder(wo);

            if (Work_Order == null)
            {
                SelectSubmit(wo);
                string queryInsert =
                    "INSERT INTO tbl_OWS_verified (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverMTM, VerifyComments, plant) VALUES('" +
                    Reason + "', '" + Deadline + "', '" + Request_Type + "', '" + wo + "' , '" + Request_By + "', '" +
                    status + "', '" + Family + "', '" + approvermtm + "', N'" + verify_comment + "', '"+plant+"')";
                SqlCommand cmd1 = new SqlCommand(queryInsert, connection);
                connection.Open();
                rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_verified SET ApproverMTM = '" + approvermtm +
                                     "' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailverify(Reason, Deadline, Request_Type, Request_By, wo, status, Family, approvermtm,
                verify_comment)) return Json(rowsAffected);
            return Json(false);
        }

        [HttpPost]
        public JsonResult AddVerifyQA(string wo, string verify_comment)
        {
            var plant = HttpContext.Session.GetString("plant");
            Verified verified = new Verified();
            verified.Work_Order = wo;
            verified.VerifyComment = verify_comment;
            string status = "Verified";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());
            string approverqa = "1";
            Work_Order = CheckExistWorkOrder(wo);
            if (Work_Order == null)
            {
                SelectSubmit(wo);
                string queryInsert =
                    "INSERT INTO tbl_OWS_verified (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverQA, VerifyComments, plant) VALUES('" +
                    Reason + "', '" + Deadline + "', '" + Request_Type + "', '" + wo + "' , '" + Request_By + "', '" +
                    status + "', '" + Family + "', '" + approverqa + "', N'" + verify_comment + "', '"+plant+"')";
                SqlCommand cmd1 = new SqlCommand(queryInsert, connection);
                connection.Open();
                rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_verified SET ApproverQA = '" + approverqa +
                                     "' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailverify(Reason, Deadline, Request_Type, Request_By, wo, status, Family, approverqa,
                verify_comment)) return Json(rowsAffected);
            return Json(false);
        }

        [HttpPost]
        public JsonResult AddVerifyPE(string wo, string verify_comment)
        {
            var plant = HttpContext.Session.GetString("plant");
            Verified verified = new Verified();
            verified.Work_Order = wo;
            verified.VerifyComment = verify_comment;
            string status = "Verified";
            int rowsAffected;
            SqlConnection connection = new SqlConnection(DbConnection());
            string approverpe = "1";
            Work_Order = CheckExistWorkOrder(wo);
            if (Work_Order == null)
            {
                SelectSubmit(wo);
                string queryInsert =
                    "INSERT INTO tbl_OWS_verified (Reason,Deadline, Request_Type,Work_Order, Request_By, Status, Family, ApproverPE, VerifyComments, plant) VALUES('" +
                    Reason + "', '" + Deadline + "', '" + Request_Type + "', '" + wo + "' , '" + Request_By + "', '" +
                    status + "', '" + Family + "', '" + approverpe + "', N'" + verify_comment + "', '"+plant+"')";
                SqlCommand cmd1 = new SqlCommand(queryInsert, connection);
                connection.Open();
                rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
            }
            else
            {
                string queryUpdate = "UPDATE tbl_OWS_verified SET ApproverPE = '" + approverpe +
                                     "' WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
                SqlCommand cmd = new SqlCommand(queryUpdate, connection);
                connection.Open();
                rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
            }

            if (sendemailverify(Reason, Deadline, Request_Type, Request_By, wo, status, Family, approverpe,
                verify_comment)) return Json(rowsAffected);
            return Json(false);
        }

        public JsonResult Show_BoxLink(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            var db = new DatabaseAccessLayer();
            DataSet ds = db.GetBoxLink(wo, plant);
            List<SelectListItem> codelist = new List<SelectListItem>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                codelist.Add(new SelectListItem {Text = dr["Link_Box"].ToString(), Value = dr["Link_Box"].ToString()});
            }

            return Json(codelist);
        }

        public IActionResult NewRequest()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm" ||
                HttpContext.Session.GetString("level") == "approver_qa" ||
                HttpContext.Session.GetString("level") == "approver_pe" ||
                HttpContext.Session.GetString("level") == "approver_mtm")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<FamilyModel> familylist = db.GetFamilyData(plant);
                var dataModel = new NewRequestViewModel()
                {
                    FamilyDetails = familylist,
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
            if (HttpContext.Session.GetString("level") == "approver_mm" ||
                HttpContext.Session.GetString("level") == "approver_qa" ||
                HttpContext.Session.GetString("level") == "approver_pe" ||
                HttpContext.Session.GetString("level") == "approver_mtm")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                DataSet ds = db.Get_Family(plant);
                List<SelectListItem> famiylist = new List<SelectListItem>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    famiylist.Add(new SelectListItem {Text = dr["Family"].ToString(), Value = dr["Family"].ToString()});
                }

                ViewBag.Family = famiylist;
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm" ||
                HttpContext.Session.GetString("level") == "approver_qa" ||
                HttpContext.Session.GetString("level") == "approver_pe" ||
                HttpContext.Session.GetString("level") == "approver_mtm")
            {
                List<User> users = new List<User>();
                string querySelect = "SELECT TOP 1 id_user, sesa_id , name, level FROM mst_users WHERE id_user = @ID";
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
        public JsonResult AddNewRequest(string OWS_ID, string Reason, string deadline, string type, string wo,
            string by, string status, string family, string comment, string[] approvers)
        {
            var plant = HttpContext.Session.GetString("plant");
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

            string querySelectWO =
                "SELECT REPLACE(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','') as Work_Order";
            SqlConnection connection = new SqlConnection(DbConnection());
            connection.Open();
            SqlCommand cmd_1 = new SqlCommand(querySelectWO, connection);
            using (SqlDataReader dr = cmd_1.ExecuteReader())
            {
                while (dr.Read())
                {
                    Work_Order = dr["Work_Order"].ToString();
                }
            }

            connection.Close();

            List<string> operator_emails = new List<string>();
            string query_getemailoperator =
                "SELECT email FROM mst_users WHERE level ='operator' AND email IS NOT NULL AND email <> '' AND plant = '"+plant+"'";
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

            SqlDataReader rowread;

            int InsertedID;

            for (var i = 0; i < approvers.Length; i++)
            {
                var item = approvers[i];
            }

            string queryInsert =
                "INSERT INTO tbl_OWS_request (CodeProduct, Reason, Deadline, Request_Type,Work_Order, Request_By, Status, Comments, Family, Code, plant)" +
                "OUTPUT inserted.Id " + "VALUES(@OWS_ID, @Reason,@Deadline,@Type, + '" + Work_Order +
                "', @By,@Status, N'" + comment + "', @Family, @Code, @plant )";
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

                            try
                            {
                                var email = new MimeMessage();
                                string emailContent =
                                    "OWS Tracking Application - MM: https://eajdigitization.se.com/SEMB_OWS"
                                    + "\r\n\r\nTime Send: [" + DateTime.Now + "]"
                                    + "\r\n\r\nRequest Type: New OWS / Yêu Cầu: Tạo OWS Mới"
                                    + "\r\n\r\nWork Order: " + Work_Order
                                    + "\r\n\r\nOWS ID: " + OWS_ID
                                    + "\r\n\r\nFamily: " + family
                                    + "\r\n\r\nReason: " + Reason
                                    + "\r\n\r\nDeadline: " + deadline
                                    + "\r\n\r\nComment: " + comment
                                    + "\r\n\r\nApproving Depts: " +
                                    string.Join(",", approvers);
                                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                                foreach (var email_to in operator_emails)
                                {
                                    email.To.Add(MailboxAddress.Parse(email_to));
                                }

                                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                                email.Subject = "[OWS Application] New OWS Request: " + family;
                                email.Body = new TextPart(TextFormat.Plain) {Text = emailContent};

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
        public JsonResult changeRequest(string family, string reason, string deadline, string[] codeproduct,
            string[] ows_id, string type, string wo, string by, string status, string comments)
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
            var plant = HttpContext.Session.GetString("plant");
            by = HttpContext.Session.GetString("SesaID");
            SqlDataReader rowread;
            int InsertedID;
            string querySelectWO =
                "select replace(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','') as Work_Order";
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

                string queryInsert =
                    "INSERT INTO tbl_OWS_request (CodeProduct,Reason, Deadline, Request_Type, Work_Order, Request_By, Status, Comments, Family, Code, plant) VALUES(@CodeProduct, @Reason, @Deadline ,@Type, '" +
                    Work_Order + "' , @By, @Status,N'" + comments + "', @Family, @Code, @plant)";
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
            string query_getemailoperator =
                "SELECT email FROM mst_users WHERE level ='operator' AND email IS NOT NULL AND email <> ''";
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
            string query_getemailuser = "SELECT email FROM mst_users WHERE sesa_id = '" + by + "'";
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
                                      + "\r\n\r\nReason: " + reason
                                      + "\r\n\r\nDeadline: " + deadline
                                      + "\r\n\r\nComment: " + comments;
                email.From.Add(MailboxAddress.Parse("digitization.semb@alerts.se.com"));
                foreach (var email_to in operator_emails)
                {
                    email.To.Add(MailboxAddress.Parse(email_to));
                }

                if (user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                email.Subject = "[OWS Application] Change OWS Request: " + family;
                email.Body = new TextPart(TextFormat.Plain) {Text = emailContent};

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
        [HttpGet]
        public IActionResult ApprovalTracking()
        {
            if (HttpContext.Session.GetString("level") == "approver_mm")
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
        [HttpPost]
        public JsonResult GetCodeData(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            List<DataModel> datalist = new List<DataModel>();
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
    }
}