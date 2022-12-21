using System.Data;
using System.IO;
using Microsoft.AspNetCore.Http;
using SEMB_OWS_Tracking.Repositories;

namespace SEMB_OWS_Tracking.Services
{
    public class ImportExportFactory
    {
        private readonly FileManagementService _fileManagement;
        private readonly ExcelServiceProvider _excelService;
        private readonly MasterDataRepository _dataRepository;

        public ImportExportFactory(FileManagementService fileManagement,
            ExcelServiceProvider excelService,
            MasterDataRepository dataRepository)
        {
            _fileManagement = fileManagement;
            _excelService = excelService;
            _dataRepository = dataRepository;
        }

        public ImportExportFactory() :
            this(new FileManagementService(),
                new ExcelServiceProvider(),
                new MasterDataRepository())
        {
        }
        
        public void ImportExcelFileToDatabase(IFormFile file, string plant)
        {
            _dataRepository.Delete_Temp_Mst_Data(plant);
            
            var uploadedFilePath = _fileManagement.FileUpload(file);
            
             if (uploadedFilePath == "Upload Fail")
             {
                return;
             }
            var fileInfo = new FileInfo(uploadedFilePath);
            
            var dataTable = _excelService.Excel_To_DataTable(fileInfo);

            _dataRepository.Bulk_Insert_Data(dataTable);
            _dataRepository.Update_Temp_Mst_Data(plant);
            _dataRepository.Run_SP_Import_MST_Data(plant);

        }
    }
}