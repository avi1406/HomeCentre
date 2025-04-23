using System;
using Dapper;
using HomeCentre.PIM.Common.PostgresTypes;

namespace HomeCentre.PIM.Migrator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var connStrings = new[]
            {
                Environment.GetEnvironmentVariable("TargetConnectionString")
            };
            Dapper.SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            SimpleCRUD.SetTableNameResolver(new PostgresLowerCaseTableNameResolver());
            SimpleCRUD.SetColumnNameResolver(new PostgresLowerCaseColumnNameResolver());

            // NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Debug);
            // NpgsqlLogManager.IsParameterLoggingEnabled = true;
            var process = new MigrationProcess();
            foreach (var connStr in connStrings)
            {
                process.Run(connStr);
            }
        }
    }
}