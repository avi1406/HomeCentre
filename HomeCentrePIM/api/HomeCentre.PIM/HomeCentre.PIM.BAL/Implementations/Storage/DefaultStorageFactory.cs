using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.Common.Interfaces;
using Storage.Net.Blobs;

namespace HomeCentre.PIM.BAL.Implementations.Storage
{
    public class DefaultStorageFactory : IStorageFactory
    {
        private readonly IDictionary<string, Func<IBlobStorage>> _storageNameMappings = new ConcurrentDictionary<string, Func<IBlobStorage>>();

        public async Task<IBlobStorage> GetStorage(string name)
        {
            return await Task.Run(() => _storageNameMappings.ContainsKey(name) ? _storageNameMappings[name]() : null);
        }

        public async Task AddStorage(string name, Func<IBlobStorage> storage)
        {
            await Task.Run(() => { _storageNameMappings.Add(name, storage); });
        }
    }
}