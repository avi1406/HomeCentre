using System.Data;
using Npgsql;

namespace HomeCentre.PIM.Common
{
    public class SafeNpgsqlConnection : IDbConnection
    {
        private readonly NpgsqlConnection _connection;
        private readonly IDbTransaction _transaction;

        public SafeNpgsqlConnection(string connectionString, IDbTransaction transaction = null)
        {
            _transaction = transaction;
            _connection = transaction != null ? (NpgsqlConnection)transaction.Connection : new NpgsqlConnection(connectionString);
            this.ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }

        public int ConnectionTimeout => _connection.ConnectionTimeout;

        public string Database => _connection.Database;

        public ConnectionState State => _connection.State;

        public IDbConnection UnderlyingConnection => _connection;

        public IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _connection.BeginTransaction(il);
        }

        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            _connection.Close();
        }

        public IDbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public void Dispose()
        {
            if (_transaction == null)
            {
                _connection.Dispose();
                NpgsqlConnection.ClearPool(_connection);
            }
        }

        public void Open()
        {
            _connection.Open();
        }
    }
}