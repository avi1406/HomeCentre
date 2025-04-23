using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
public class ProductQueryController: BaseApiController
{
    private readonly IProductQueryManager _manager;

    public ProductQueryController(IProductQueryManager manager)
    {
        _manager = manager;
    }

    [HttpPost]
    public async Task<ProductQuery> CreateQuery([FromBody] ProductQuery query)
    {
        return await _manager.CreateQuery(query);
    }

    [HttpPut]
    public async Task ReassignQuery(long[] queryIds, long assignedToId)
    {
        await _manager.ReassignQuery(queryIds, assignedToId);
    }
    
    [HttpPut]
    public async Task ResolveQuery(long[] queryIds)
    {
        await _manager.ResolveQuery(queryIds);
    }
    
    [HttpPut]
    public async Task ActivateQuery(long[] queryIds)
    {
        await _manager.ActivateQuery(queryIds);
    }
    
    [HttpPut]
    public async Task DeactivateQuery(long[] queryIds)
    {
        await _manager.DeactivateQuery(queryIds);
    }
    
    [HttpGet]
    public async Task<IEnumerable<Product>> ListProducts(long queryId)
    {
        return await _manager.ListProducts(queryId);
    }
    
    [HttpGet]
    public async Task<SearchResult<ProductQuery>> SearchQueries(string? searchKey, int pageNumber, int pageSize, long? assignedToId)
    {
        return await _manager.SearchQueries(searchKey, pageNumber, pageSize, assignedToId);
    }
    
    [HttpGet]
    public async Task<SearchResult<ProductQuery>> SearchMyQueries(string? searchKey, int pageNumber, int pageSize, long statusId = 30)
    {
        return await _manager.SearchMyQueries(searchKey, pageNumber, pageSize, statusId);
    }

    [HttpPost]
    public async Task<ProductQueryComment> CreateQueryComment([FromBody] ProductQueryComment queryComment)
    {
        return await _manager.CreateQueryComment(queryComment);
    }
    
    [HttpGet]
    public async Task<SearchResult<ProductQueryComment>> SearchQueryComments(string? searchKey, int pageNumber, int pageSize, long queryId)
    {
        return await _manager.SearchQueryComments(searchKey, pageNumber, pageSize, queryId);
    }
}