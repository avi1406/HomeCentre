using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IDataRequestManager
{
    Task<DataRequest> CreateRequest(DataRequest request);

    Task<SearchResult<DataRequest>> SearchDataRequests(string searchKey, int pageNumber, int pageSize, bool isActive);
    
    Task<SearchResult<DataRequestProduct>> SearchDataRequestItems(string searchKey, int pageNumber, int pageSize, long dataRequestId);

    Task<string> DownloadRequestRecords(long dataRequestId);
    
    Task DeactivateDataRequests(long[] ids);
    
    Task ActivateDataRequests(long[] ids);
    
    Task<IDictionary<string, int>> GetDataRequestItemCountByStatus(long dataRequestId);
    
    Task SendRecordToQc(long productId);
}