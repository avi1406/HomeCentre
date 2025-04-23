using System;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using NetBox.Extensions;
using Storage.Net.Blobs;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgFileManager : PgEntityAction<FileTable>, IFileManager
    {
        private readonly IBlobStorage _blobStorage;

        public PgFileManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IBlobStorage blobStorage, IConfiguration configuration)
            : base(
            userContext, connectionFactory, configuration)
        {
            _blobStorage = blobStorage;
        }

        public async Task DeleteFile(long id)
        {
            var table = await this.Get(id);
            await this.Remove(id);
            await _blobStorage.DeleteAsync(table.UniqueId);
        }

        public async Task<byte[]> DownloadFile(long fileTableId)
        {
            var table = await this.Get(fileTableId);

            using (var stream = await _blobStorage.OpenReadAsync(table.UniqueId))
            {
                stream.Position = 0;
                return stream.ToByteArray();
            }
        }

        public async Task<FileTable> GetDetail(long id)
        {
            return await this.Get(id);
        }

        public async Task<long> UploadFile(FileTable table, Stream content)
        {
            if (table.Id == 0)
            {
                table.UniqueId = Guid.NewGuid().ToString();
            }

            await _blobStorage.SetBlobsAsync(new[]
            {
                new Blob(table.UniqueId, BlobItemKind.File)
                {
                    Size = content.Length,
                    LastModificationTime = DateTimeOffset.UtcNow
                },
            });

            await _blobStorage.WriteAsync(table.UniqueId, content);
            using var connection = this.GetConnection();
            return await connection.QueryFirstAsync<long>(
                "select id from filetable where uniqueid = @uniqueId", new
                {
                    uniqueId = table.UniqueId
                });
        }
    }
}