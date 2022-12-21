using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEMB_OWS_Tracking.Function;
using SEMB_OWS_Tracking.Models;

namespace SEMB_OWS_Tracking.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult SignOut()
        {
            if (HttpContext.Session != null)
            {
                HttpContext.Session.SetString("name", "");
                HttpContext.Session.SetString("SesaID", "");
                HttpContext.Session.SetString("level", "");
                HttpContext.Session.SetString("plant", "");
                HttpContext.Session.Clear();
                HttpContext.Session = null;
                foreach (var cookie in Request.Cookies.Keys)
                {
                    if (cookie == ".AspNetCore.Session")
                        Response.Cookies.Delete(cookie);
                }
            }
            return View("Index");
        }
        public IActionResult Index()
        {
            return View();
        }

        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();
            string dbString = dbAccess.ConnectionString;

            return dbString;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginModel user)
        {
            var hashpassword = new Authentication();

            if (ModelState.IsValid)
            {
                List<LoginModel> userInfo = new List<LoginModel>();

                using (SqlConnection conn = new SqlConnection(DbConnection()))
                {
                    string passwordHash = hashpassword.MD5Hash(user.password);
                    string query = "SELECT * FROM mst_users WHERE sesa_id = '" + user.sesa_id + "' AND password = '" + passwordHash + "' ";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        ViewData["Message"] = "HAS DATA";
                        while (reader.Read())
                        {
                            var loginUser = new LoginModel();
                            loginUser.id = Convert.ToInt32(reader["id_user"]);
                            loginUser.name = reader["name"].ToString();
                            loginUser.sesa_id = reader["sesa_id"].ToString();
                            loginUser.level = reader["level"].ToString();
                            loginUser.plant = reader["plant"].ToString();


                            userInfo.Add(loginUser);

                            HttpContext.Session.SetString("name", loginUser.name);
                            HttpContext.Session.SetString("level", loginUser.level);
                            HttpContext.Session.SetString("SesaID", loginUser.sesa_id);
                            HttpContext.Session.SetString("plant", loginUser.plant);
                            HttpContext.Session.SetInt32("id", loginUser.id);

                        }

                        if (HttpContext.Session.GetString("level") == "admin")
                        {
                            return RedirectToAction("AdminDashboard", "Admin");

                        } else if (HttpContext.Session.GetString("level") == "user")
                        {
                            return RedirectToAction("NewRequest", "User");

                        } else if (HttpContext.Session.GetString("level") == "operator")
                        {
                            return RedirectToAction("OpenWorkOrder", "Operator");

                        } else if (HttpContext.Session.GetString("level") == "approver_mm" )
                        {
                            return RedirectToAction("ApprovalMM", "Approver");
                        }
                        else if (HttpContext.Session.GetString("level") == "approver_qa")
                        {
                            return RedirectToAction("ApprovalQA", "Approver");
                        }
                        else if (HttpContext.Session.GetString("level") == "approver_pe")
                        {
                            return RedirectToAction("ApprovalPE", "Approver");
                        }
                        else if (HttpContext.Session.GetString("level") == "approver_mtm")
                        {
                            return RedirectToAction("ApprovalMTM", "Approver");
                        }
                    }
                    else
                    {
                        ViewData["Message"] = "User and Password not Registered !";
                    }
                    conn.Close();
                    
                }
            }

            return View("Index");
        }
    }
}

