using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.Common.Interfaces;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgAppConfiguration : IAppConfiguration
    {
        private const string AppSettingsTable = "appsettings";
        private readonly IDbConnection _connection;
        private Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public PgAppConfiguration(IDbConnection connection)
        {
            _connection = connection;
            LoadConfiguration().Wait();
        }

        public async Task<string> GetValue(string key)
        {
            if (!_dictionary.ContainsKey(key))
            {
                return await _connection.ExecuteScalarAsync<string>(
                    $"select value from {AppSettingsTable} where key = @key", new
                    {
                        key
                    });
            }

            return _dictionary[key];
        }

        public async Task Reload()
        {
            _dictionary.Clear();
            await LoadConfiguration();
        }

        private async Task LoadConfiguration()
        {
            var result = await
                _connection.QueryAsync<dynamic>(
                    $"select key, value from {AppSettingsTable} where key = @key");
            _dictionary = result.ToDictionary(k => (string)k.key, v => (string)v.value);
        }
    }
}