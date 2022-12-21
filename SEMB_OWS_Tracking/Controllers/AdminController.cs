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
using System.Data.OleDb;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using OfficeOpenXml;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using System.Text.Json;
using SEMB_OWS_Tracking.Services;

namespace SEMB_OWS_Tracking.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly ImportExportFactory _importexportFactory;
        public AdminController(IHostingEnvironment hostingEnvironment, 
            ImportExportFactory importexportFactory)
        {
            _hostingEnvironment = hostingEnvironment;
            _importexportFactory = importexportFactory;
        }

        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();

            string dbString = dbAccess.ConnectionString;

            return dbString;
        }

        public IActionResult ApprovalHistory()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                List<OpenWorkOrder> openWOs = new List<OpenWorkOrder>();
                string querySelect = "GET_APP_HISTORY";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.CommandTimeout = 0;
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            try
                            {
                                while (sdr.Read())
                                {
                                    var data = new OpenWorkOrder();
                                    data.Reason = Convert.ToString(sdr["Reason"]);
                                    data.Request_By = Convert.ToString(sdr["Request_By"]);
                                    data.Request_Type = Convert.ToString(sdr["Request_Type"]);
                                    data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                                    data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                                    data.Status = Convert.ToString(sdr["ApprovalStatus"]);
                                    data.Comment = Convert.ToString(sdr["Comments"]);
                                    data.Family = Convert.ToString(sdr["Family"]);
                                    openWOs.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        conn.Close();
                    }
                }

                return View(openWOs);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }
        
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("level") == "admin")
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

        [HttpGet]
        public IActionResult RequestCount()
        {
            var db = new DatabaseAccessLayer();
            var plant = HttpContext.Session.GetString("plant");
            List<RequestCount> requestCounts = db.GetRequestCountdaily(plant);
            List<RequestCountWeek> requestCountsweek = db.GetRequestCountWeekly(plant);
            List<RequestCountMonth> requestCountsmonth = db.GetRequestCountMonthly(plant);
            var dataModel = new ViewModel()
            {
                RequestCountDetails = requestCounts,
                RequestCountWeekDetails = requestCountsweek,
                RequestCountMonthDetails = requestCountsmonth
            };
            return PartialView("_table_Request_count", dataModel);
            //return View();
        }


        [HttpGet]
        public IActionResult GetOwsDataChart(string recorddatefrom = null, string recorddateto = null)
        {
            if (recorddatefrom == null) recorddatefrom = DateTime.Today.ToString("d");
            if (recorddateto == null) recorddateto = DateTime.Today.ToString("d");
            var plant = HttpContext.Session.GetString("plant");
            var db = new DatabaseAccessLayer();
            ChartDataModel Chartdataget = db.GetOWSChartData(recorddatefrom, recorddateto, plant);
            if (Chartdataget == null) return StatusCode(500, "No data");
            var labels = Chartdataget.Datalabel;
            var data = Chartdataget.Datavalue;
            var data_2 = Chartdataget.Datavalue_2;
            var data_3 = Chartdataget.Datavalue_3;
            var data_4 = Chartdataget.Datavalue_4;

            return new JsonResult(new
            {
                returnedlabel = labels, total_wo = data, total_wo_started = data_2, total_wo_rejected = data_3,
                total_wo_approved = data_4
            });
        }


        public IActionResult ApprovalLeadTime()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                List<ApprovalDeadlineTracking> deadlineTrackings = new List<ApprovalDeadlineTracking>();
                var plant = HttpContext.Session.GetString("plant");
                string querySelect = "GET_APP_LEAD_TIME";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.CommandTimeout = 0;
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            try
                            {
                                while (sdr.Read())
                                {
                                    var data = new ApprovalDeadlineTracking();
                                    data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                                    data.StartDate = Convert.ToDateTime(sdr["Submit_Date"]);
                                    data.ApprovalDate = Convert.ToDateTime(sdr["Approval_Date"]);
                                    data.Late_Status = Convert.ToString(sdr["Late_Status"]);
                                    deadlineTrackings.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        conn.Close();
                    }
                }

                return View(deadlineTrackings);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        public IActionResult OperatorLeadTime()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                List<DeadlineTracking> deadlineTrackings = new List<DeadlineTracking>();
                string querySelect = "GET_OPE_LEAD_TIME";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.CommandTimeout = 0;
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            try
                            {
                                while (sdr.Read())
                                {
                                    var data = new DeadlineTracking();
                                    data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                                    data.StartDate = Convert.ToDateTime(sdr["Start_Date"]);
                                    data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                                    data.SubmitDate = Convert.ToDateTime(sdr["Submit_Date"]);
                                    data.Late_Status = Convert.ToString(sdr["late_status"]);
                                    deadlineTrackings.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        conn.Close();
                    }
                }

                return View(deadlineTrackings);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpGet]
        public IActionResult UploadFile()
        {
            if (HttpContext.Session.GetString("plant") != "" || HttpContext.Session.GetString("plant") is null)
            {
                var plant = HttpContext.Session.GetString("plant");
                List<SQLDatabaseModel> datalist = new List<SQLDatabaseModel>();
                string query = "GET_MST_DATA";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(query))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                            while (sdr.Read())
                            {
                                datalist.Add(new SQLDatabaseModel
                                {
                                    STT = sdr["STT"].ToString(),
                                    Product = sdr["Product"].ToString(),
                                    Family = sdr["Family"].ToString(),
                                    Format = sdr["Format"].ToString(),
                                    Operation_Name = sdr["Operation_Name"].ToString(),
                                    Code = sdr["Code"].ToString(),
                                    Safety_Required = sdr["Safety_Required"].ToString(),
                                    Critical_Required = sdr["Critical_Required"].ToString(),
                                    Total_Set = sdr["Total_Set"].ToString(),
                                    No_Pape = sdr["No_Pape"].ToString(),
                                    Version = sdr["Version"].ToString(),
                                    Issued_Date = sdr["Issued_Date"].ToString(),
                                    Sector = sdr["Sector"].ToString(),
                                    comment = sdr["comment"].ToString()

                                });
                            }

                        conn.Close();
                    }
                }
                return View(datalist);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }

        }

        
        [HttpPost]
        public IActionResult ImportFile(IFormFile FormFile)
        {
            if (HttpContext.Session.GetString("plant") != "" || HttpContext.Session.GetString("plant") != null)
            {
                var plant = HttpContext.Session.GetString("plant");

                _importexportFactory.ImportExcelFileToDatabase(FormFile, plant);

                ViewBag.Message = "File Imported and excel data saved into database";

                return RedirectToAction("UploadFile");
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
            
        }


        public IActionResult RequestHistory()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                List<OpenWorkOrder> openWOs = new List<OpenWorkOrder>();
                string querySelect = "GET_REQ_HISTORY";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.CommandTimeout = 0;
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            try
                            {
                                while (sdr.Read())
                                {
                                    var data = new OpenWorkOrder();
                                    data.Reason = Convert.ToString(sdr["Reason"]);
                                    data.Request_By = Convert.ToString(sdr["Request_By"]);
                                    data.Request_Type = Convert.ToString(sdr["Request_Type"]);
                                    data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                                    data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                                    data.Status = Convert.ToString(sdr["Status"]);
                                    data.Comment = Convert.ToString(sdr["Comments"]);
                                    data.Family = Convert.ToString(sdr["Family"]);

                                    openWOs.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        conn.Close();
                    }
                }

                return View(openWOs);
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        // [HttpPost]
        // public JsonResult GetMasterData()
        // {
        //     List<SQLDatabaseModel> datalist = new List<SQLDatabaseModel>();
        //     string query = "SELECT TOP 100 * FROM mst_data";
        //     using (SqlConnection conn = new SqlConnection(DbConnection()))
        //     {
        //         using (SqlCommand cmd = new SqlCommand(query))
        //         {
        //             cmd.Connection = conn;
        //             conn.Open();
        //             using (SqlDataReader sdr = cmd.ExecuteReader())
        //                 while (sdr.Read())
        //                 {
        //                     datalist.Add(new SQLDatabaseModel
        //                     {
        //                         STT = sdr["STT"].ToString(),
        //                         Product = sdr["Product"].ToString(),
        //                         Family = sdr["Family"].ToString(),
        //                         Format = sdr["Format"].ToString(),
        //                         Code = sdr["Code"].ToString(),
        //                         Safety_Required = sdr["Safety_Required"].ToString(),
        //                         Critical_Required = sdr["Critical_Required"].ToString(),
        //                         Total_Set = sdr["Total_Set"].ToString(),
        //                         No_Pape = sdr["No_Pape"].ToString(),
        //                         Version = sdr["Version"].ToString(),
        //                         Issued_Date = sdr["Issued_Date"].ToString(),
        //                         Sector = sdr["Sector"].ToString()
        //                     });
        //                 }
        //
        //             conn.Close();
        //         }
        //     }
        //     return Json(  datalist );
        // }

        public IActionResult UsersList()
        {
            if (HttpContext.Session.GetString("level") == "admin")
            {
                var plant = HttpContext.Session.GetString("plant");
                List<User> users = new List<User>();
                string querySelect = @"SELECT id_user, sesa_id, name, level FROM mst_users WHERE plant = @plant";
                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    using (SqlCommand cmd = new SqlCommand(querySelect))
                    {
                        cmd.Parameters.AddWithValue("@plant", plant);
                        cmd.Connection = conn;
                        conn.Open();
                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
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

        public JsonResult EditUser(int ID, string sesaid, string name, string level)
        {
            User user = new User();
            user.ID = ID;
            user.SESA_ID = sesaid;
            user.Name = name;
            user.Level = level;
            int Excute;
            string query = @"UPDATE [dbo].[mst_users] SET [name] = @Name ,[level] = @Level WHERE id_user = @ID";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Level", user.Level);
                    cmd.Parameters.AddWithValue("@ID", user.ID);
                    conn.Open();
                    Excute = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            return Json(Excute);
        }

        public JsonResult AddUser(string sesaid, string name, string level)
        {
            var hashpassword = new Authentication();
            var plant = HttpContext.Session.GetString("plant");

            User user = new User();
            user.SESA_ID = sesaid;
            user.Name = name;
            user.Level = level;
            string Pass = hashpassword.MD5Hash("123");
            int Excute;
            string query = @"INSERT INTO mst_users
           ([sesa_id]
           ,[name]
           ,[password]
           ,[level]
            ,[plant]) VALUES (@SESA, @Name, '" + Pass + "' ,@Level, @plant)";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SESA", user.SESA_ID);
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Level", user.Level);
                    cmd.Parameters.AddWithValue("@plant", plant);
                    conn.Open();
                    Excute = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            return Json(Excute);
        }

        public JsonResult DeleteUser(int ID)
        {
            User user = new User();
            user.ID = ID;
            int Excute;
            string query = "DELETE FROM mst_users WHERE id_user = @ID";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", user.ID);
                    conn.Open();
                    Excute = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            return Json(Excute);
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
        public JsonResult DeleteWO(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            int result = 0;
            string Delete = "DELETE FROM tbl_OWS_request WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            string DeleteSubmit = "DELETE FROM tbl_OWS_submitted WHERE Work_Order = '" + wo + "' AND plant = '" + plant + "'";
            SqlConnection con = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(Delete, con);
            SqlCommand cmd2 = new SqlCommand(DeleteSubmit, con);
            con.Open();
            result = cmd.ExecuteNonQuery();
            cmd2.ExecuteNonQuery();
            con.Close();
            return Json(result);
        }

        [HttpGet]
        public IActionResult GetWorkOrderData(string wo)
        {
            var plant = HttpContext.Session.GetString("plant");
            var db = new DatabaseAccessLayer();
            List<OpenWorkOrder> openWorkOrders = db.GetWorkOrderData(wo, plant);

            var dataModel = new ViewModel()
            {
                OpenWorkOrdersDetail = openWorkOrders
            };
            return PartialView("_tbl_data", dataModel);
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

        [HttpGet]
        public IActionResult GetDetailMstData(string product = null, string family = null, string code = null )
        {
            var db = new DatabaseAccessLayer();
            var plant = HttpContext.Session.GetString("plant");
            List<MSTDetailsModel> detailMST = db.GetDetailMstData(product, family, code, plant);
            var GetDataDetail = new ViewModel()
            {
                MSTDetailsData = detailMST
            };
            return PartialView("_table_Detail_MasterData", GetDataDetail);
        }

    }
}