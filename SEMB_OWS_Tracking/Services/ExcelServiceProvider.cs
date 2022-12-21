using System.Data;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace SEMB_OWS_Tracking.Services
{
    public class ExcelServiceProvider
    {
        public ExcelWorksheet Load_Excel_File(FileInfo fileInfo)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excelPackage = new ExcelPackage(fileInfo);
            
            var excelWorkSheet = excelPackage.Workbook.Worksheets.First();

            return excelWorkSheet;
        }
        
        public DataTable Excel_To_DataTable(FileInfo fileInfo , int row = 1 , int col = 1, bool hasHeader = true )
        {

            var worksheet = Load_Excel_File(fileInfo);
            
            DataTable tbl = new DataTable();
            
            foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                tbl.Columns.Add(hasHeader ? firstRowCell.Text : string.Format("Column {0}", firstRowCell.Start.Column));
            }
            var startRow = hasHeader ? 2 : 1;
            for (int rowNum = startRow; rowNum <= worksheet.Dimension.End.Row; rowNum++)
            {
                var worksheetRow = worksheet.Cells[rowNum, 1, rowNum, worksheet.Dimension.End.Column];
                DataRow tblRow = tbl.Rows.Add();
                foreach (var cell in worksheetRow)
                {
                    tblRow[cell.Start.Column - 1] = cell.Text;
                }
            }

            
            return tbl;
        }
    }
}