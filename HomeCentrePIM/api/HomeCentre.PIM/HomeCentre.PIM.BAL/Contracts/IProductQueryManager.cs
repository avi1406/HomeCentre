using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IProductQueryManager
{
    Task<ProductQuery> CreateQuery(ProductQuery query);
    
    Task ReassignQuery(long[] queryIds, long assignedToId);

    Task DeactivateQuery(long[] ids);

    Task ActivateQuery(long[] ids);

    Task<SearchResult<ProductQuery>> SearchQueries(string searchKey, int pageNumber, int pageSize, long? assignedToId);

    Task<IEnumerable<Product>> ListProducts(long queryId);

    Task ResolveQuery(long[] queryIds);
    
    Task<ProductQueryComment> CreateQueryComment(ProductQueryComment queryComment);
    
    Task<SearchResult<ProductQueryComment>> SearchQueryComments(string searchKey, int pageNumber, int pageSize, long queryId);
    
    Task<SearchResult<ProductQuery>> SearchMyQueries(string searchKey, int pageNumber, int pageSize, long statusId);
}