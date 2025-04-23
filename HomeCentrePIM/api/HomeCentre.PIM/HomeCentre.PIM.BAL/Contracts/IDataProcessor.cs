using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;
using Microsoft.Graph;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IDataProcessor
{
    Task<List<UploadedFileData>> ValidateData(List<UploadedFileData> data, long templateId);

    Task SaveData(List<UploadedFileData> data, long templateId);
}