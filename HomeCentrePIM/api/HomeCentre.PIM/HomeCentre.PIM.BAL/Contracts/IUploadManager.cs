using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IUploadManager
{
    Task<UploadedFile> UploadFile(UploadedFile file, IDbTransaction transaction = null);

    Task<List<UploadedFileData>> ParseFile(UploadedFile file, FileConfiguration fileConfiguration);

    Task<List<UploadedFileData>> ProcessFile(UploadedFile file);

    Task<SearchResult<UploadedFile>> SearchFiles(string searchKey, int pageSize, int pageCount, long? templateId);

    Task<SearchResult<UploadedFileData>> SearchRecords(string searchKey, int pageSize, int pageCount,
        long uploadedFileId);

    Task UpdateRecords(IEnumerable<UploadedFileData> records);

    Task UpdateFile(List<Product> dataList, Template template);

    Task PublishFile(UploadedFile file);

    Task<List<UploadedFileData>> ValidateFile(UploadedFile file);

    Task<string> DownloadFile(long uploadedFileId);

    Task<UploadedFile> GetFile(long uploadedFileId);

    Task AddPicture(UploadPicture model);

    Task<List<UploadPicture>> GetFileNameAndUrl();

    Task<List<UploadPicture>> GetPSFileNameAndUrl();

    Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtraction();

    // added by Avinash
    Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionHPD();

    Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionHPI();

    Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionFPD();

    Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionFPI();

    Task UpdateInputTemplateData(long id, string value);

    Task<List<UploadedFile>> GetInputTemplateData(long id);

    Task Deleteproductimage(long id);
}