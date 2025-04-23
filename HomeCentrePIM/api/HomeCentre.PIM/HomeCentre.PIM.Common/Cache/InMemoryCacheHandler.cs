using System;
using System.Threading;
using System.Threading.Tasks;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace HomeCentre.PIM.Common.Cache
{
    public class InMemoryCacheHandler : ICacheHandler, IDistributedCache
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public byte[] Get(string key)
        {
            return _cache.Get(key)?.ToBytes();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return await Task.Run(() => Get(key), token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _cache.Set(key, value.ToObject<object>());
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            await Task.Run(() => Set(key, value, options), token);
        }

        public void Refresh(string key)
        {
        }

        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            await Task.Run(() => { }, token);
        }

        public void Remove(string key)
        {
            if (key == Constants.AllCacheKeys)
            {
                _cache.Dispose();
                _cache = new MemoryCache(new MemoryCacheOptions());
            }
            else
            {
                _cache.Remove(key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            await Task.Run(() => Remove(key), token);
        }

        public async Task AddToCache(string key, object value)
        {
            await this.SetAsync(key, value.ToBytes());
        }

        public async Task Clear()
        {
            await Task.FromResult(_cache = new MemoryCache(new MemoryCacheOptions()));
        }

        public async Task<T> GetFromCache<T>(string key, Func<object[], T> action, params object[] args)
        {
            var existingObject = await this.GetAsync(key);
            if (existingObject != null)
            {
                return existingObject.ToObject<T>();
            }

            var output = action(args);
            await this.SetAsync(key, output.ToBytes());
            return output;
        }

        public async Task<T> GetFromCacheAsync<T>(string key, Func<object[], Task<T>> action, params object[] args)
        {
            var existingObject = await this.GetAsync(key);
            if (existingObject != null)
            {
                return existingObject.ToObject<T>();
            }

            var output = await action(args);
            await this.SetAsync(key, output.ToBytes());
            return output;
        }

        public async Task RemoveItemFromCache(string key)
        {
            await this.RemoveAsync(key);
        }
    }
}