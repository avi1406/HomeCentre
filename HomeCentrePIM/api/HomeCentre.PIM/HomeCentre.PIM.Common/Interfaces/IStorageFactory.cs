using System;
using System.Threading.Tasks;
using Storage.Net.Blobs;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IStorageFactory
    {
        Task<IBlobStorage> GetStorage(string name);

        Task AddStorage(string name, Func<IBlobStorage> storage);
    }
}