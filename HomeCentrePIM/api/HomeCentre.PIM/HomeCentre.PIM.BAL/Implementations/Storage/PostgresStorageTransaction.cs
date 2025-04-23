using System;
using System.Threading.Tasks;
using Npgsql;
using Storage.Net;

namespace HomeCentre.PIM.BAL.Implementations.Storage
{
    public class PostgresStorageTransaction : ITransaction
    {
        private readonly NpgsqlTransaction _transaction;

        public PostgresStorageTransaction(NpgsqlTransaction transaction)
        {
            _transaction = transaction;
        }

        public void Dispose()
        {
            try
            {
                _transaction.Dispose();
                _transaction?.Connection?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
        }
    }
}