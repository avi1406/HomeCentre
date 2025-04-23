using System.Data;
using Npgsql;

namespace HomeCentre.PIM.Common
{
    public class DapperConnection : IDbConnection
    {
        private readonly IDbConnection _connection;
        private readonly bool _myTransaction;
        private readonly IDbTransaction _transaction;

        public DapperConnection(string connectionString, IDbTransaction transaction = null)
        {
            if (transaction == null)
            {
                _connection = new NpgsqlConnection(connectionString);
                _transaction = BeginTransaction();
                _myTransaction = true;
            }
            else
            {
                _transaction = transaction;
                _connection = transaction.Connection;
                _myTransaction = false;
            }
        }

        public string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        public int ConnectionTimeout => _connection.ConnectionTimeout;

        public string Database => _connection.Database;

        public ConnectionState State => _connection.State;

        public IDbTransaction BeginTransaction()
        {
            return _transaction;
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return _transaction;
        }

        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public void Close()
        {
            if (_myTransaction)
            {
                _connection.Close();
            }
        }

        public IDbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public void Dispose()
        {
            if (_myTransaction)
            {
                _connection.Dispose();
            }
        }

        public void Open()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }
    }
}