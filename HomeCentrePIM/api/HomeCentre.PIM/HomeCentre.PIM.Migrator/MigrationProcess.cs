using System;
using System.Linq;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.Postgres;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.Postgres;
using HomeCentre.PIM.Migrator.Migration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeCentre.PIM.Migrator
{
    public class MigrationProcess
    {
        /// <summary>
        /// Runs the specified connection string.
        /// </summary>
        /// <param name="constring">The connection string.</param>
        /// <param name="migrateUp">The migrate up.</param>
        /// <param name="migrateDown">The migrate down.</param>
        public void Run(string constring, int? migrateUp = null, int? migrateDown = null)
        {
            var serviceProvider = CreateServices(constring);

            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                UpdateDatabase(scope.ServiceProvider, constring, migrateUp, migrateDown);
            }
        }

        /// <summary>
        /// Creates the services.
        /// </summary>
        /// <param name="connStr">The connection string.</param>
        /// <returns></returns>
        private IServiceProvider CreateServices(string connStr)
        {
            var asm = typeof(BaseLine).Assembly;
            foreach (var n in asm.GetManifestResourceNames())
            {
                Console.WriteLine(n);
            }

            return new ServiceCollection()

                // Add common FluentMigrator services
                .AddFluentMigratorCore()

                // .AddScoped<PostgresQuoter, NoPostgresQuoter>()
                .ConfigureRunner(rb => rb

                    // Add SQLite support to FluentMigrator
                    .AddPostgres()

                    // Set the connection string
                    .WithGlobalConnectionString(connStr)

                    // Define the assembly containing the migrations

                    // .ScanIn(typeof(BaseLine).Assembly).For.Migrations())
                    .ScanIn(typeof(BaseLine).Assembly))

                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())

                // Build the service provider
                .BuildServiceProvider(false);
        }

        /// <summary>
        /// Updates the database.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="migrateUp">The migrate up.</param>
        /// <param name="migrateDown">The migrate down.</param>
        private void UpdateDatabase(IServiceProvider serviceProvider, string connString, int? migrateUp = null,
            int? migrateDown = null)
        {
            // Instantiate the runner
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            var loggerProvider = serviceProvider.GetRequiredService<ILoggerProvider>();

            GenericProcessorBase processor = null;
            if (runner.MigrationLoader.LoadMigrations().Any())
            {
                var recentMigration = runner.MigrationLoader.LoadMigrations().Max(x => x.Key);
                runner.Processor.BeginTransaction();
                processor = runner.Processor as GenericProcessorBase;
                var firstRun = !runner.Processor.TableExists("public", "VersionInfo");
                if (!firstRun)
                {
                    runner.Processor.Execute($"DELETE FROM \"VersionInfo\" WHERE \"Version\" = {recentMigration}");
                }
            }

            // Execute the migrations
            if (migrateDown != null)
            {
                runner.MigrateDown(migrateDown.GetValueOrDefault());
            }

            if (migrateUp != null)
            {
                runner.MigrateUp(migrateUp.GetValueOrDefault());
            }
            else
            {
                runner.MigrateUp();
            }

            runner.Processor.CommitTransaction();
            if (processor != null)
            {
                var logger = loggerProvider.CreateLogger("MasterData");
                using (var masterDataManager = new MasterDataManager(connString, logger))
                {
                    masterDataManager.Run();
                }
            }
        }
    }

    public class NoPostgresQuoter : PostgresQuoter
    {
        public NoPostgresQuoter(PostgresOptions options)
            : base(options)
        {
            options.ForceQuote = false;
        }
    }
}