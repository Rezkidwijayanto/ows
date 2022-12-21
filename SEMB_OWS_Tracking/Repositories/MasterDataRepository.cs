using System.Data;
using System.Data.SqlClient;

namespace SEMB_OWS_Tracking.Repositories
{
    public class MasterDataRepository
    {
        public readonly string ConnectionString =
            "Data Source=10.155.152.114;Initial Catalog=SEMB_OWS;Persist Security Info=True;User ID=dt;Password=Dt@123;" +
            "MultipleActiveResultSets=True";

        public void Delete_Temp_Mst_Data(string plant)
        {
            string query = $"DELETE [mst_data_temp] WHERE plant = '{plant}'";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand(query , conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        public void Update_Temp_Mst_Data(string plant)
        {
            string query = $"UPDATE [mst_data_temp] SET plant = '{plant}' WHERE plant IS NULL";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        public void Run_SP_Import_MST_Data(string plant)
        {
            string query = "SP_IMPORT_MST_DATA";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var cmd = new SqlCommand(query);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@plant", plant);
                cmd.CommandTimeout = 0;
                cmd.Connection = conn;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void Bulk_Insert_Data(DataTable tbl)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(conn))
                {
                    //Set the database table name.
                    sqlBulkCopy.DestinationTableName = "dbo.mst_data_temp";

                    // Map the Excel columns with that of the database table, this is optional but good if you do
                    // 
                    sqlBulkCopy.ColumnMappings.Add("STT", "STT");
                    sqlBulkCopy.ColumnMappings.Add("Product", "Product");
                    sqlBulkCopy.ColumnMappings.Add("Family", "Family");
                    sqlBulkCopy.ColumnMappings.Add("Format", "Format");
                    sqlBulkCopy.ColumnMappings.Add("Operation_Name", "Operation_Name");
                    sqlBulkCopy.ColumnMappings.Add("Code", "Code");
                    sqlBulkCopy.ColumnMappings.Add("Safety_Required", "Safety_Required");
                    sqlBulkCopy.ColumnMappings.Add("Critical_Required", "Critical_Required");
                    sqlBulkCopy.ColumnMappings.Add("Total_Set", "Total_Set");
                    sqlBulkCopy.ColumnMappings.Add("No_Pape", "No_Pape");
                    sqlBulkCopy.ColumnMappings.Add("Version", "Version");
                    sqlBulkCopy.ColumnMappings.Add("Issued_Date", "Issued_Date");
                    sqlBulkCopy.ColumnMappings.Add("Sector", "Sector");
                    sqlBulkCopy.ColumnMappings.Add("comment", "comment");
                    conn.Open();
                    sqlBulkCopy.WriteToServer(tbl);
                    conn.Close();
                }
            }
        }
    }
}