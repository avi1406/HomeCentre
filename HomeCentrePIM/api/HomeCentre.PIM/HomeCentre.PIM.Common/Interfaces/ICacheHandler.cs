using System;
using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface ICacheHandler
    {
        Task AddToCache(string key, object value);

        Task Clear();

        Task<T> GetFromCache<T>(string key, Func<object[], T> action, params object[] args);

        Task<T> GetFromCacheAsync<T>(string key, Func<object[], Task<T>> action, params object[] args);
      
        Task RemoveItemFromCache(string key);
    }
}