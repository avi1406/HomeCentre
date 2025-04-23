using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
[MultiplePoliciesAuthorize(Constants.Policies.All)]
public class DataRequestController : BaseApiController
{
    private readonly IDataRequestManager _manager;

    public DataRequestController(IDataRequestManager manager)
    {
        _manager = manager;
    }

    [HttpPost]
    public async Task<DataRequest> CreateDataRequest([FromBody] DataRequest request)
    {
        return await _manager.CreateRequest(request);
    }
    
    [HttpPut]
    public async Task ActivateDataRequests(long[] ids)
    {
        await _manager.ActivateDataRequests(ids);
    }
    
    [HttpPut]
    public async Task DeactivateDataRequests(long[] ids)
    {
        await _manager.DeactivateDataRequests(ids);
    }

    [HttpGet]
    public async Task<SearchResult<DataRequest>> SearchDataRequests(string searchKey, int pageNumber, int pageSize,
        bool isActive)
    {
        return await _manager.SearchDataRequests(searchKey, pageNumber, pageSize, isActive);
    }
    
    [HttpGet]
    public async Task<SearchResult<DataRequestProduct>> SearchDataRequestItems(string searchKey, int pageNumber, int pageSize,
        long dataRequestId)
    {
        return await _manager.SearchDataRequestItems(searchKey, pageNumber, pageSize, dataRequestId);
    }
    
    [HttpGet]
    public async Task<FileContentResult> DownloadRequestRecords(long dataRequestId)
    {
        var file = await _manager.DownloadRequestRecords(dataRequestId);
        return this.File(await System.IO.File.ReadAllBytesAsync(file),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DataRequest-{dataRequestId}.xlsx".ToUniqueFileNameUrl());
    }

    [HttpGet]
    public async Task<IDictionary<string, int>> GetDataRequestItemCountByStatus(long dataRequestId)
    {
        return await _manager.GetDataRequestItemCountByStatus(dataRequestId);
    }

    [HttpPut]
    public async Task SendRecordToQc(long productId)
    {
        await _manager.SendRecordToQc(productId);
    }
}