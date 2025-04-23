using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.DAL;
using NetBox.Extensions;
using Npgsql;
using Storage.Net;
using Storage.Net.Blobs;

namespace HomeCentre.PIM.BAL.Implementations.Storage
{
    public class PostgreSqlBlobStorage : IBlobStorage
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlLargeObjectManager _manager;
        private NpgsqlTransaction _transaction;

        public PostgreSqlBlobStorage(string connectionString)
        {
            StorageFactory.Blobs.InMemory();
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
            _manager = new NpgsqlLargeObjectManager(_connection);
        }

        public async Task DeleteAsync(IEnumerable<string> fullPaths,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var fullPath in fullPaths)
            {
                var fileTable = await GetFileTable(fullPath);
                if (fileTable == null)
                {
                    return;
                }

                var oidExists = (await _connection.QueryAsync<QueryResult<uint>>(
                    "select oid as value from pg_largeobject_metadata where oid = @oid limit 1", new
                    {
                        oid = int.Parse(fileTable.Oid)
                    })).FirstOrDefault();
                if (oidExists != null)
                {
                    await _manager.UnlinkAsync(oidExists.Value, cancellationToken);
                }

                await _connection.DeleteAsync<FileTable>(fileTable.Id);
            }
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
            }

            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        public async Task<IReadOnlyCollection<bool>> ExistsAsync(IEnumerable<string> fullPaths,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var collection = new Collection<bool>();
            foreach (var fullPath in fullPaths)
            {
                var fileTable = await GetFileTable(fullPath);
                if (fileTable == null)
                {
                    continue;
                }

                var exists = (await _connection.QueryAsync<QueryResult<long>>(
                    "SELECT id as value FROM FILETABLE WHERE OID = @oid",
                    new { fileTable.Oid })).Any();
                collection.Add(exists);
            }

            return collection;
        }

        public async Task<IReadOnlyCollection<Blob>> GetBlobsAsync(IEnumerable<string> fullPaths,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var collection = new Collection<Blob>();
            var oids = new List<string>();
            foreach (var fullPath in fullPaths)
            {
                var fileTable = await GetFileTable(fullPath);
                if (fileTable == null)
                {
                    continue;
                }

                oids.Add(fileTable.Oid);
            }

            var files = await _connection.QueryAsync<FileTable>("select * from filetable where oid = any(@oids)",
                new { oids });
            foreach (var file in files)
            {
                var blob = new Blob(file.Oid, file.Name, BlobItemKind.File)
                {
                    Size = file.ContentLength,
                    Properties = { { "id", file.Id.ToString() } }
                };

                collection.Add(blob);
            }

            return collection;
        }

        public async Task<IReadOnlyCollection<Blob>> ListAsync(ListOptions options = null,
                            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sqlBuilder = new StringBuilder("select * from filetable");
            var parameters = new DynamicParameters();
            var collection = new Collection<Blob>();
            if (options != null)
            {
                if (options.FilePrefix != "*")
                {
                    sqlBuilder.Append(" where name like @searchKey");
                    parameters.Add("@searckKey", $"{options.FilePrefix}%");
                }

                if (options.MaxResults != null)
                {
                    sqlBuilder.Append(" Limit @limit");
                    parameters.Add("@limit", options.MaxResults);
                }
            }

            var files = await _connection.QueryAsync<FileTable>(sqlBuilder.ToString(), parameters);
            foreach (var file in files)
            {
                var blob = new Blob(file.Oid, file.Name, BlobItemKind.File)
                {
                    Size = file.ContentLength,
                    Properties = { { "id", file.Id.ToString() } }
                };

                collection.Add(blob);
            }

            return collection;
        }

        public async Task<Stream> OpenReadAsync(string fullPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var fileTable = await GetFileTable(fullPath);
            if (fileTable == null)
            {
                throw new InvalidDataException("Record not found");
            }

            return await _manager.OpenReadAsync(uint.Parse(fileTable.Oid), cancellationToken);
        }

        public async Task<ITransaction> OpenTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new NpgsqlException("Transaction in progress");
            }

            _transaction = (NpgsqlTransaction)await _connection.BeginTransactionAsync();
            return new PostgresStorageTransaction(_transaction);
        }

        public async Task SetBlobsAsync(IEnumerable<Blob> blobs,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var blobList = blobs.ToList();
            var existingBlobs =
                await this.GetBlobsAsync(blobList.Select(x => x.Name),
                    cancellationToken);

            foreach (var blob in blobList)
            {
                var id = blob.GetId();
                var existingBlob = existingBlobs.FirstOrDefault(x => x.GetId() == id);
                if (existingBlob == null)
                {
                    var fileTable = new FileTable
                    {
                        Name = blob.Name,
                        Oid = _manager.Create().ToString(),
                        ContentLength = blob.Size.GetValueOrDefault(),
                        CreatedBy = 1,
                        UpdatedBy = 1,
                        Type = "pgsql",
                        UniqueId = blob.Name
                    };
                    await _connection.InsertAsync(fileTable);
                }
            }
        }

        public async Task WriteAsync(string fullPath, Stream dataStream, bool append = false,
                                    CancellationToken cancellationToken = default(CancellationToken))
        {
            var fileTable = await GetFileTable(fullPath);
            if (fileTable == null)
            {
                throw new InvalidDataException("Record not found");
            }

            using (var transaction = await this.OpenTransactionAsync())
            {
                var lobStream = await _manager.OpenReadWriteAsync(uint.Parse(fileTable.Oid), cancellationToken);
                dataStream.Position = 0;
                var bytesToWrite = dataStream.ToByteArray();
                await lobStream.WriteAsync(bytesToWrite, 0, bytesToWrite.Length, cancellationToken);
                lobStream.Close();
                await lobStream.DisposeAsync();
                await transaction.CommitAsync();
            }
        }

        private async Task<FileTable> GetFileTable(string uniqueId)
        {
            return (await _connection.QueryAsync<FileTable>(
                "SELECT * FROM filetable WHERE uniqueid = @name",
                new { name = uniqueId })).FirstOrDefault();
        }
    }
}