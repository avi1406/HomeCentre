using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HomeCentre.PIM.BAL.Model;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IDataProcess
    {
        Task ApproveOrReject(long id);

        Task<List<ExcelModel>> ExcelModel(string messageName);

        Task<string> ProcessDataFromTemplate(string messageName, bool? returnDataOnly = true);

        Task<string> FindAppSettings(string configName);

        Task MasterDataUpload(string filePath, long masterId);
    }
}
