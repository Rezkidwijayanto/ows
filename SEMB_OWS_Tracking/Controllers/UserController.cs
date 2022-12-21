using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using SEMB_OWS_Tracking.Function;
using Microsoft.AspNetCore.Http;
using SEMB_OWS_Tracking.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SEMB_OWS_Tracking.Controllers
{
    public class UserController : Controller
    {
        string Work_Order;
        int rowsAffected;
        public IActionResult Index()
        {
            return View();
        }

        public List<User> GetAllUser()
        {
            var plant = HttpContext.Session.GetString("plant");

            var users = new List<User>();
            string query = @"SELECT * FROM mst_users WHERE plant = @plant";
            
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Parameters.AddWithValue("@plant", plant);
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        var user = new User();
                        while (sdr.Read())
                        {
                            user.Name = Convert.ToString(sdr["name"]);
                            user.SESA_ID = Convert.ToString(sdr["sesa_id"]);
                            user.Level = Convert.ToString(sdr["level"]);
                            users.Add(user);
                        }
                    }
                    conn.Close();
                }
            }

            return users;
        }

        public List<string> GetUserAutherization()
        {
            var plant = HttpContext.Session.GetString("plant");
            var autherize = new List<string>();
            string query = @"SELECT [level] FROM mst_users AND plant = @plant";
            
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Parameters.AddWithValue("@plant", plant);
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            autherize.Add(sdr["level"].ToString());
                        }
                    }
                    conn.Close();
                }
            }
            return autherize;
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

        [HttpPost]
        public async Task<IActionResult> SubmitRequestFile(List<IFormFile> files, List<IFormFile> image, int id_string)
        {
            long size = files.Sum(f => f.Length);

            long size_image = image.Sum(f => f.Length);
            var plant = HttpContext.Session.GetString("plant");

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

                    NewRequestFile data = new NewRequestFile();
                    data.Id = id_string;
                    data.FilePath = filePath;
                    data.Filetype = "File";
                    string query = "INSERT INTO tbl_OWS_request_file (OWS_request_id,Filepath,Filetype, plant) VALUES(@Id,@filepath,@Filetype, @plant)";
                    using (SqlConnection con = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Parameters.AddWithValue("@filepath", data.FilePath);
                            cmd.Parameters.AddWithValue("@Id", data.Id);
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

                    NewRequestFile data = new NewRequestFile();
                    data.Id = id_string;
                    data.FilePath = filePath;
                    data.Filetype = "Image";
                    string query = "INSERT INTO tbl_OWS_request_file (OWS_request_id,Filepath,Filetype, plant) VALUES(@Id,@filepath,@Filetype, @plant)";
                    using (SqlConnection con = new SqlConnection(DbConnection()))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Parameters.AddWithValue("@filepath", data.FilePath);
                            cmd.Parameters.AddWithValue("@Id", data.Id);
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

            return Ok(new { count = files.Count, size,size_image ,filePaths });
        }

        [HttpGet]
        public IActionResult OpenWorkOrder()
        {
            if (HttpContext.Session.GetString("level") == "user")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                List<OpenWorkOrder> openWorkOrders = db.GetOpenWorkOrder(plant);
                var dataModel = new ViewModel()
                {
                    OpenWorkOrdersDetail = openWorkOrders,
                };

                return View(dataModel);
            }
            else
            {
                return RedirectToAction("Index", "Login");

            }
        }
        private readonly IHostingEnvironment hostingEnvironment;
        public UserController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
        }
        public IActionResult NewRequest()
        {
            if(HttpContext.Session.GetString("level") == "user")
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
            return RedirectToAction("Index", "Login");
        }

        public IActionResult ChangeRequest()
        {
            if (HttpContext.Session.GetString("level") == "user")
            {
                var plant = HttpContext.Session.GetString("plant");
                var db = new DatabaseAccessLayer();
                DataSet ds = db.Get_Family(plant);
                List<SelectListItem> famiylist = new List<SelectListItem>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    famiylist.Add(new SelectListItem { Text = dr["Family"].ToString(), Value = dr["Family"].ToString() });
                }
                ViewBag.Family = famiylist;
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Login");
            }
            
        }


        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();

            string dbString = dbAccess.ConnectionString;

            return dbString;
        }

        [HttpPost]

        public JsonResult AddNewRequest(string OWS_ID, string Reason, string deadline, string type, string wo, string by, string status, string family, string comment, string [] approvers)
        {
            NewRequestViewModel model = new NewRequestViewModel();
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

            string querySelectWO = "SELECT REPLACE(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','') as Work_Order";
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
            string query_getemailoperator = "SELECT email FROM mst_users WHERE level ='operator' AND email IS NOT NULL AND email <> '' AND plant = '"+plant+"'";
            using(SqlConnection conn = new SqlConnection(DbConnection()))
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
            string query_getemailuser = "SELECT email FROM mst_users WHERE sesa_id = '"+by+"' AND plant = '"+plant+"'";
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
            string queryInsert = "INSERT INTO tbl_OWS_request (CodeProduct, Reason, Deadline, Request_Type,Work_Order, Request_By, Status, Comments, Family, Code, plant)" +
                "OUTPUT inserted.Id " +
                "VALUES(@OWS_ID,@Reason,@Deadline,@Type,'"+ Work_Order + "',@By,@Status,N'" + comment + "',@Family,@Code, @plant)";
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
                                foreach(var email_to in operator_emails)
                                {
                                    email.To.Add(MailboxAddress.Parse(email_to));
                                }
                                if(user_email != "") email.Cc.Add(MailboxAddress.Parse(user_email));
                                email.Subject = "[OWS Application] New OWS Request: " + family;
                                email.Body = new TextPart(TextFormat.Plain) { Text = emailContent };

                                // send email
                                using var smtp = new SmtpClient();
                                smtp.Connect("smtp.se.com", 587, SecureSocketOptions.StartTls);
                                smtp.Authenticate("digitization.semb@alerts.se.com", "5}StOuv2#*?phM-^J4K3u}]v3");
                                smtp.Send(email);
                                smtp.Disconnect(true);
                            }
                            catch(Exception ex)
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
                    catch(Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }

        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("level") == "user")
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
            string querySelectWO = "SELECT REPLACE(convert(varchar, getdate(),112),'/','') + replace(convert(varchar, getdate(),108),':','') as Work_Order";
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
                string queryInsert = "INSERT INTO tbl_OWS_request (CodeProduct,Reason, Deadline, Request_Type, Work_Order, Request_By, Status, Comments, Family, Code, plant)" +
            "OUTPUT inserted.Id " + "VALUES(@CodeProduct, @Reason, @Deadline ,@Type, '" + Work_Order + "' , @By, @Status,N'" + comments + "', @Family, @Code, @plant)";
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

            string select = "SELECT Id FROM tbl_OWS_request WHERE Work_Order = '" + Work_Order + "' AND plant = '"+plant+"'";
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
                con.Close();
                return Json(false);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public string SelectWO(int id_string)
        {
            var plant = HttpContext.Session.GetString("plant");
            string querySelectWO = "SELECT Work_Order FROM tbl_OWS_request WHERE Id = '"+ id_string + "' AND plant = '"+plant+"'";
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
        public async Task<IActionResult> SubmitChangeRequestFile(List<IFormFile> files, List<IFormFile> image, int id_string)
        {
            long size = files.Sum(f => f.Length);

            long size_image = image.Sum(f => f.Length);
            var plant = HttpContext.Session.GetString("plant");
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
                    string query = "INSERT INTO tbl_OWS_request_file (Workorder,Filepath,Filetype, plant) VALUES(@wo,@filepath,@Filetype,@plant)";
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

        public JsonResult Code_Bind(string familyId)
        {
            var plant = HttpContext.Session.GetString("plant");
            var db = new DatabaseAccessLayer();
            DataSet ds = db.Get_Code(familyId, plant);
            List<SelectListItem> codelist = new List<SelectListItem>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                codelist.Add(new SelectListItem { Text = dr["CodeProduct"].ToString(), Value = dr["Code"].ToString() });
            }
            return Json(codelist);
        }

        public JsonResult Product_Bind(string familyId)
        {
            var plant = HttpContext.Session.GetString("plant");
            var db = new DatabaseAccessLayer();
            DataSet ds = db.Get_Product(familyId, plant);
            List<SelectListItem> productlist = new List<SelectListItem>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                productlist.Add(new SelectListItem { Text = dr["Product"].ToString(), Value = dr["Product"].ToString() });
            }
            return Json(productlist);
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

    }
}
