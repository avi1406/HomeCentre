using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgProductQueryManager : PgEntityAction<ProductQuery>, IProductQueryManager
{
    public PgProductQueryManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration)
        : base(userContext, connectionFactory, configuration)
    {
    }

    public async Task<ProductQuery> CreateQuery(ProductQuery query)
    {
        query.ProductQueryStatusId = 10; // new
        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();
        var productIds = query.ProductIds;
        query = await this.AddOrUpdate(query, transaction: transaction);
        var userId = await this.GetUserId();
        var productQueryItems = productIds.Select(x => new ProductQueryItem
        {
            ProductQueryId = query.Id,
            ProductId = x,
            CreatedBy = userId,
            UpdatedBy = userId,
            IsActive = true,
            DateCreated = DateTimeOffset.UtcNow,
            DateModified = DateTimeOffset.UtcNow
        }).ToList();
        transaction.BulkInsert(nameof(ProductQueryItem), productQueryItems.ToArray().GetDataTableFromObjects());
        transaction.Commit();
        return query;
    }

    public async Task ReassignQuery(long[] queryIds, long assignedToId)
    {
        using var connection = this.GetConnection();
        await connection.ExecuteAsync("update productquery set assignedToId = @assignedToId where id = any(@queryIds)",
            new
            {
                queryIds,
                assignedToId
            });
    }

    public async Task DeactivateQuery(long[] ids)
    {
        await this.Deactivate(ids);
    }

    public async Task ActivateQuery(long[] ids)
    {
        await this.Activate(ids);
    }

    public async Task<SearchResult<ProductQuery>> SearchQueries(string searchKey, int pageNumber, int pageSize,
        long? assignedToId)
    {
        if (assignedToId != null)
        {
            var dynParams = new DynamicParameters();
            dynParams.Add("assignedToId", assignedToId);
            return await this.Search(searchKey, pageNumber, pageSize, "assignedToId = @assignedToId", dynParams);
        }

        return await this.Search(searchKey, pageNumber, pageSize);
    }

    public async Task<IEnumerable<Product>> ListProducts(long queryId)
    {
        using var connection = this.GetConnection();
        return await connection.QueryAsync<Product>(
            "select p.* from product p join productqueryitem p2 on p.id = p2.productid where p2.productqueryid = @queryId and p2.isactive = true",
            new
            {
                queryId
            });
    }

    public async Task ResolveQuery(long[] queryIds)
    {
        using var connection = this.GetConnection();

        await connection.ExecuteAsync("update productquery set ProductQueryStatusId = 30 where id = any(@queryIds)", new
        {
            queryIds
        }); // mark resolved.
        
        var productIds = await connection.QueryAsync<long>(
            "select p.id from product p join productqueryitem p2 on p.id = p2.productid where p2.productqueryid = any(@queryIds) and p2.isactive = true",
            new
            {
                queryIds
            });
        foreach (var productId in productIds)
        {
            var queryStatus = await connection.QueryAsync<long?>(
                "select p.ProductQueryStatusId from productquery p join productqueryitem p2 on p.id = p2.productqueryid where p2.productid = @productId and p2.isactive = true",
                new
                {
                    productId
                });
            
            // all completed
            if (queryStatus.All(r => r == 30)) 
            {
                await connection.ExecuteAsync("update product set QcStatusId = 10 where id = any(@productIds)", new
                {
                    productIds = new[]{productId}
                });
            }
        }
    }

    public async Task<ProductQueryComment> CreateQueryComment(ProductQueryComment queryComment)
    {
        var action = this.GetAction<ProductQueryComment>();
        return await action.AddOrUpdate(queryComment);
    }

    public async Task<SearchResult<ProductQueryComment>> SearchQueryComments(string searchKey, int pageNumber, int pageSize, long queryId)
    {
        var action = this.GetAction<ProductQueryComment>();
        var dynParams = new DynamicParameters();
        dynParams.Add("queryId", queryId);
        return await action.Search(searchKey, pageNumber, pageSize, "ProductQueryId = @queryId", dynParams);
    }

    public async Task<SearchResult<ProductQuery>> SearchMyQueries(string searchKey, int pageNumber, int pageSize,
        long statusId)
    {
        var dynParams = new DynamicParameters();
        dynParams.Add("assignedToId", await this.GetUserId());
        dynParams.Add("statusId", statusId);
        return await this.Search(searchKey, pageNumber, pageSize, "assignedToId = @assignedToId and productQueryStatusId = @statusId", dynParams);
    }
}