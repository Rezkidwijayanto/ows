using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SEMB_OWS_Tracking.Models;
using System.Data.SqlClient;
using System.Data;

namespace SEMB_OWS_Tracking.Function
{
    public class DatabaseAccessLayer
    {
        public string ConnectionString =
            "Data Source=10.155.152.114;Initial Catalog=SEMB_OWS;Persist Security Info=True;User ID=dt;Password=Dt@123;" +
            "MultipleActiveResultSets=True";

        private string DbConnection()
        {
            var dbAccess = new DatabaseAccessLayer();

            string dbString = dbAccess.ConnectionString;

            return dbString;
        }


        public ChartDataModel GetOWSChartData(string recorddatefrom, string recorddateto, string plant)
        {
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                var chartdata = new ChartDataModel();
                string query = "GET_OWS_DATA_PLANT";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@recorddatefrom", recorddatefrom);
                cmd.Parameters.AddWithValue("@recorddateto", recorddateto);
                cmd.Parameters.AddWithValue("@plant", plant);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        chartdata.Datalabel.Add(reader["Record_Date"].ToString().Replace(" 12:00:00 AM", ""));
                        chartdata.Datavalue.Add(Convert.IsDBNull(reader["Total_WO"])
                            ? 0
                            : Convert.ToDouble(reader["Total_WO"]));
                        chartdata.Datavalue_2.Add(Convert.IsDBNull(reader["Total_WO_Started"])
                            ? 0
                            : Convert.ToDouble(reader["Total_WO_Started"]));
                        chartdata.Datavalue_3.Add(Convert.IsDBNull(reader["Total_WO_Rejected"])
                            ? 0
                            : Convert.ToDouble(reader["Total_WO_Rejected"]));
                        chartdata.Datavalue_4.Add(Convert.IsDBNull(reader["Total_WO_Approved"])
                            ? 0
                            : Convert.ToDouble(reader["Total_WO_Approved"]));
                    }

                    conn.Close();
                    return chartdata;
                }

                conn.Close();
                return null;
            }
        }

        public List<OpenWorkOrder> GetOpenWorkOrder(string plant)
        {
            List<OpenWorkOrder> openWOs = new List<OpenWorkOrder>();
            string querySelect =$"SELECT DISTINCT Work_Order, Reason, Deadline, Request_Type, Request_By,Status,Comments, Family FROM tbl_OWS_request WHERE Status = 'Open' AND plant = '{plant}'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(querySelect))
                {
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

            return openWOs;
        }

        public List<RequestCount> GetRequestCountdaily(string plant)
        {
            List<RequestCount> requestCounts = new List<RequestCount>();
            string querySelect = @"SELECT CAST([Timestamp] AS DATE) [Date], 
                                   Count(1)  [Request Count]   
                                FROM tbl_OWS_request WHERE plant = @plant
                                GROUP BY CAST([Timestamp] AS DATE)
                                ORDER BY 1 ";
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
                            var data = new RequestCount();
                            data.Date = Convert.ToDateTime(sdr["Date"]);
                            data.Number = Convert.ToString(sdr["Request Count"]);
                            requestCounts.Add(data);
                        }
                    }

                    conn.Close();
                }
            }

            return requestCounts;
        }

        public List<RequestCountWeek> GetRequestCountWeekly(string plant)
        {
            List<RequestCountWeek> requestCounts = new List<RequestCountWeek>();
            string querySelect =@"SELECT DATEPART(Week,CAST([Timestamp] AS DATE)) as Weekly, Count(1)  [Request Count]   
                                    FROM tbl_OWS_request WHERE plant = @plant
                                    GROUP BY  DATEPART(Week,CAST([Timestamp] AS DATE))
                                    ORDER BY 1 ";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(querySelect))
                {
                    cmd.Parameters.AddWithValue("@plant",plant);
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            var data = new RequestCountWeek();
                            data.Week = Convert.ToString(sdr["Weekly"]);
                            data.Number = Convert.ToString(sdr["Request Count"]);
                            requestCounts.Add(data);
                        }
                    }

                    conn.Close();
                }
            }

            return requestCounts;
        }

        public List<RequestCountMonth> GetRequestCountMonthly(string plant)
        {
            List<RequestCountMonth> requestCounts = new List<RequestCountMonth>();
            string querySelect =
                @"SELECT DATEPART(Month,CAST([Timestamp] AS DATE)) as Monthly, Count(1)  [Request Count]   
                                    FROM tbl_OWS_request WHERE plant = @plant
                                    GROUP BY DATEPART(Month,CAST([Timestamp] AS DATE))
                                    ORDER BY 1";
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
                            var data = new RequestCountMonth();
                            data.Month = Convert.ToString(sdr["Monthly"]);
                            data.Number = Convert.ToString(sdr["Request Count"]);
                            requestCounts.Add(data);
                        }
                    }

                    conn.Close();
                }
            }

            return requestCounts;
        }

        public List<Started> GetStartedWorkOrder(string plant)
        {
            List<Started> starteds = new List<Started>();
            string querySelect = "GET_START_WORK_ORDER";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(querySelect, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.CommandTimeout = 0;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    try
                    {
                        while (sdr.Read())
                        {
                            var data = new Started();
                            data.Reason = Convert.ToString(sdr["Reason"]);
                            data.Request_By = Convert.ToString(sdr["Request_By"]);
                            data.Request_Type = Convert.ToString(sdr["Request_Type"]);
                            data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                            data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                            data.Status = Convert.ToString(sdr["Status"]);
                            data.Family = Convert.ToString(sdr["Family"]);
                            data.Comments = Convert.ToString(sdr["Comments"]);

                            starteds.Add(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                conn.Close();
            }

            return starteds;
        }

        public List<Submitted> GetSubmittedWorkOrder(string requested_user, string plant)
        {
            List<Submitted> submitteds = new List<Submitted>();
            string querySelect = "GET_SUBMIT_WORK_ORD";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(querySelect, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.Parameters.AddWithValue("@requested_user", requested_user);
                cmd.CommandTimeout = 0;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    try
                    {
                        while (sdr.Read())
                        {
                            var data = new Submitted();
                            data.Reason = Convert.ToString(sdr["Reason"]);
                            data.Request_By = Convert.ToString(sdr["Request_By"]);
                            data.Request_Type = Convert.ToString(sdr["Request_Type"]);
                            data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                            data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                            data.Status = Convert.ToString(sdr["Status"]);
                            data.Family = Convert.ToString(sdr["Family"]);
                            data.Comments = Convert.ToString(sdr["Comments"]);
                            submitteds.Add(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                conn.Close();
            }

            return submitteds;
        }

        public List<Submitted> GetApproverSubmittedWorkOrder(string userLogin, string plant)
        {
            List<Submitted> submitteds = new List<Submitted>();
            string querySelect = "GET_APP_SUBMIT_WORKORD";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(querySelect))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userLogin", userLogin);
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
                                var data = new Submitted();
                                data.Reason = Convert.ToString(sdr["Reason"]);
                                data.Request_By = Convert.ToString(sdr["Request_By"]);
                                data.Request_Type = Convert.ToString(sdr["Request_Type"]);
                                data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                                data.Work_Order = Convert.ToString(sdr["Work_Order"]);
                                data.Status = Convert.ToString(sdr["Status"]);
                                data.Family = Convert.ToString(sdr["Family"]);
                                data.Comments = Convert.ToString(sdr["Comments"]);
                                submitteds.Add(data);
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

            return submitteds;
        }

        public List<Verified> GetVerifiedWorkOrder(string plant)
        {
            List<Verified> verifieds = new List<Verified>();
            string querySelect = "GET_VERIFY_WORK_ORD";
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
                        verifieds.Add(new Verified
                        {
                            Reason = Convert.ToString(sdr["Reason"]),
                            Request_By = Convert.ToString(sdr["Request_By"]),
                            Request_Type = Convert.ToString(sdr["Request_Type"]),
                            Deadline = Convert.ToString(sdr["Deadline"]),
                            Work_Order = Convert.ToString(sdr["Work_Order"]),
                            Status = Convert.ToString(sdr["VerifiedStatus"]),
                            Family = Convert.ToString(sdr["Family"]),
                            VerifyComment = Convert.ToString(sdr["VerifyComments"]),
                            Verify_By = Convert.ToString(sdr["Verify_By"])
                        });
                    }
                }

                conn.Close();
                
            }

            return verifieds;
        }

        public List<FamilyModel> GetFamilyData(string plant)
        {
            List<FamilyModel> familyList = new List<FamilyModel>();
            string query = "NEW_REQ_FAMILY";

            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.CommandTimeout = 0;
                cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                    while (sdr.Read())
                    {
                        var data = new FamilyModel();
                        data.Family = sdr["Family"].ToString();
                        familyList.Add(data);
                    }

                conn.Close();
            }

            return familyList;
        }


        public List<Tracking> GetTrackingData(string plant)
        {
            List<Tracking> trackings = new List<Tracking>();
            string query = "GET_TRACKING_DATA_PLANT";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.CommandTimeout = 0;
                conn.Open();
                using (SqlDataReader sdr = cmd.ExecuteReader())
                    while (sdr.Read())
                    {
                        var data = new Tracking();
                        data.Family = sdr["Family"].ToString();
                        data.Product = sdr["CodeProduct"].ToString();
                        data.ChangeInfo = sdr["Comments"].ToString();
                        data.Work_Order = sdr["Work_Order"].ToString();
                        data.MMStatus = sdr["MMStatus"].ToString();
                        data.MTMStatus = sdr["MTMStatus"].ToString();
                        data.QAStatus = sdr["QAStatus"].ToString();
                        data.PEStatus = sdr["PEStatus"].ToString();
                        data.VerifiedStatus = sdr["VerifiedStatus"].ToString();
                        trackings.Add(data);
                    }

                conn.Close();
                
            }

            return trackings;
        }

        public List<LinkBox> GetLinkBoxesTable(string wo)
        {
            List<LinkBox> boxlink = new List<LinkBox>();
            string query = "SELECT Link_Box FROM tbl_box_link WHERE WorkOrder = '" + wo + "'";
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
                            data.Box_link = sdr["Link_Box"].ToString();
                            boxlink.Add(data);
                        }

                    conn.Close();
                }
            }

            return boxlink;
        }

        public DataSet GetOWSID(string wo, string plant)
        {
            string query = "SELECT CodeProduct FROM tbl_OWS_request WHERE Work_Order = '" + wo + "' AND plant = '"+plant+"'";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        public DataSet GetBoxLink(string wo, string plant)
        {
            string query = "SELECT Link_Box FROM tbl_box_link WHERE WorkOrder = '" + wo + "' AND plant = '"+plant+"' ";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        public List<ApproverMMModel> GetApproverMM(string plant)
        {
            List<ApproverMMModel> approverMMs = new List<ApproverMMModel>();
            string query = $"SELECT level, name, sesa_id FROM mst_users WHERE [level] = 'approver_mm' AND plant = '{plant}'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ApproverMMModel();
                            data.ApproverMM = sdr["name"].ToString();
                            data.LevelMM = sdr["level"].ToString();
                            data.ApproverMMSESA = sdr["sesa_id"].ToString();
                            approverMMs.Add(data);
                        }

                    conn.Close();
                }
            }

            return approverMMs;
        }

        public List<ApproverQAModel> GetApproverQA(string plant)
        {
            List<ApproverQAModel> approverQAs = new List<ApproverQAModel>();
            string query = $"SELECT level, name, sesa_id FROM mst_users WHERE [level] = 'approver_qa' AND plant = '{plant}'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ApproverQAModel();
                            data.ApproverQA = sdr["name"].ToString();
                            data.LevelQA = sdr["level"].ToString();
                            data.ApproverQASESA = sdr["sesa_id"].ToString();
                            approverQAs.Add(data);
                        }

                    conn.Close();
                }
            }

            return approverQAs;
        }

        public List<ApproverPEModel> GetApproverPE(string plant)
        {
            List<ApproverPEModel> approverPEs = new List<ApproverPEModel>();
            string query = $"SELECT level, name, sesa_id FROM mst_users WHERE [level] = 'approver_pe' AND plant = '{plant}' ";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ApproverPEModel();
                            data.ApproverPE = sdr["name"].ToString();
                            data.LevelPE = sdr["level"].ToString();
                            data.ApproverPESESA = sdr["sesa_id"].ToString();
                            approverPEs.Add(data);
                        }

                    conn.Close();
                }
            }

            return approverPEs;
        }

        public List<ApproverMTMModel> GetApproverMTM(string plant)
        {
            List<ApproverMTMModel> approverMTMs = new List<ApproverMTMModel>();
            string query = $"SELECT level, name, sesa_id FROM mst_users WHERE [level] = 'approver_mtm' AND plant = '{plant}' ";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ApproverMTMModel();
                            data.ApproverMTM = sdr["name"].ToString();
                            data.LevelMTM = sdr["level"].ToString();
                            data.ApproverMTMSESA = sdr["sesa_id"].ToString();
                            approverMTMs.Add(data);
                        }

                    conn.Close();
                }
            }

            return approverMTMs;
        }

        public List<ListReasonModel> GetAllReason(string plant)
        {
            List<ListReasonModel> allReason = new List<ListReasonModel>();
            string query = @"SELECT DISTINCT reason FROM mst_reason WHERE plant = @plant";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Parameters.AddWithValue("@plant",plant);
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ListReasonModel();
                            data.reason = sdr["reason"].ToString();
                            allReason.Add(data);
                        }

                    conn.Close();
                }
            }

            return allReason;
        }
        public List<ListApproverModel> GetAllApprover()
        {
            List<ListApproverModel> allApprove = new List<ListApproverModel>();
            string query = "SELECT DISTINCT [no], Approvers as approver FROM mst_approvers ORDER BY [no] asc";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new ListApproverModel();
                            data.approver = sdr["approver"].ToString();
                            allApprove.Add(data);
                        }

                    conn.Close();
                }
            }

            return allApprove;
        }


        public List<OpenWorkOrder> GetWorkOrderData(string wo, string plant)
        {
            List<OpenWorkOrder> openWorkOrders = new List<OpenWorkOrder>();
            string query =
                "SELECT CodeProduct, Request_Type, Request_By, Reason, Deadline, Family, Comments FROM tbl_OWS_request WHERE Work_Order = '" +
                wo + "' AND plant = '"+plant+"'";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                        while (sdr.Read())
                        {
                            var data = new OpenWorkOrder();
                            data.OWS_ID = sdr["CodeProduct"].ToString();
                            data.Request_Type = sdr["Request_Type"].ToString();
                            data.Request_By = sdr["Request_By"].ToString();
                            data.Reason = sdr["Reason"].ToString();
                            data.Deadline = Convert.ToDateTime(sdr["Deadline"]);
                            data.Family = sdr["Family"].ToString();
                            data.Comment = sdr["Comments"].ToString();
                            openWorkOrders.Add(data);
                        }

                    conn.Close();
                }
            }

            return openWorkOrders;
        }

        public DataSet Get_Family(string plant)
        {
            string query = "GET_CHANGE_REQ_FAMILY";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@plant", plant);
            cmd.CommandTimeout = 0;
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        public DataSet Get_Code(string familyId, string plant)
        {
            string query = "GET_CODE";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@plant", plant);
            cmd.Parameters.AddWithValue("@Family", familyId);
            cmd.CommandTimeout = 0;
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        public DataSet Get_Product(string familyId, string plant)
        {
            string query = "GET_PRODUCT";
            SqlConnection conn = new SqlConnection(DbConnection());
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@plant", plant);
            cmd.Parameters.AddWithValue("@Family", familyId);
            cmd.CommandTimeout = 0;
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        public List<MSTDetailsModel> GetDetailMstData(string product, string family, string code, string plant)
        {
            List<MSTDetailsModel> dtlmst = new List<MSTDetailsModel>();
            string query = "GET_DETAIL_MST_DATA";
            using (SqlConnection conn = new SqlConnection(DbConnection()))
            {
                SqlCommand cmd = new SqlCommand(query,conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@product", product);
                cmd.Parameters.AddWithValue("@family", family);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.CommandTimeout = 0;
                conn.Open();
                using(SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var data = new MSTDetailsModel();
                        data.STT = dr["STT"].ToString();
                        data.Product = dr["Product"].ToString();
                        data.Family = dr["Family"].ToString();
                        data.Format = dr["Format"].ToString();
                        data.Operation_Name = dr["Operation_Name"].ToString();
                        data.Code = dr["Code"].ToString();
                        data.Safety_Required = dr["Safety_required"].ToString();
                        data.Critical_Required = dr["Critical_Required"].ToString();
                        data.Total_Set = dr["Total_Set"].ToString();
                        data.No_Pape = dr["No_Pape"].ToString();
                        data.Version = dr["Version"].ToString();
                        data.Issued_Date = dr["Issued_Date"].ToString();
                        data.Sector = dr["Sector"].ToString();
                        data.comment = dr["comment"].ToString();
                        dtlmst.Add(data);
                    }
                }
            }
            return dtlmst;
        }
    }
}