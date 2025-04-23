using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using FluentMigrator.Builders.Alter.Table;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Infrastructure;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.PostgresTypes;
using Npgsql;
using NpgsqlTypes;

namespace HomeCentre.PIM.Migrator
{
    public static class UtilExtensions
    {
        public static IFluentSyntax CreateTableIfNotExists(this MigrationBase self, string tableName,
            Func<ICreateTableWithColumnOrSchemaOrDescriptionSyntax, IFluentSyntax> constructTableFunction,
            string schemaName = "public")
        {
            if (!self.Schema.Schema(schemaName).Table(tableName).Exists())
            {
                return constructTableFunction(self.Create.Table(tableName));
            }

            return null;
        }

        public static void CreateForeignKeyColIfNotExists(this MigrationBase self, string tableName,
            string colName, string fromTableName, string fromColName = "Id", bool isnullable = true,
            string schemaName = "public")
        {
            tableName = tableName.ToLower();
            colName = colName.ToLower();
            fromTableName = fromTableName.ToLower();
            fromColName = fromColName.ToLower();
            if (!self.Schema.Schema(schemaName).Table(tableName).Column(colName).Exists())
            {
                // isnullable = true;
                if (isnullable)
                {
                    self.Alter.Table(tableName).AddColumn(colName).AsInt64().Nullable();
                }
                else
                {
                    self.Alter.Table(tableName).AddColumn(colName).AsInt64().NotNullable();
                }

                // self.Create.ForeignKey($"FK_{tableName}_{colName}_{fromTableName}_{fromColName}") 
                self.Create.ForeignKey()
                    .FromTable(tableName).ForeignColumn(colName)
                    .ToTable(fromTableName).PrimaryColumn(fromColName).OnDelete(Rule.SetNull);
            }
        }

        public static IFluentSyntax DropTableIfExists(this FluentMigrator.Migration self, string tableName,
            string schemaName = "public")
        {
            if (self.Schema.Schema(schemaName).Table(tableName).Exists())
            {
                return self.Delete.Table(tableName);
            }
            else
            {
                return null;
            }
        }

        public static IFluentSyntax CreateColIfNotExists(this MigrationBase self, string tableName, string colName,
            Func<IAlterTableColumnAsTypeSyntax, IFluentSyntax> constructColFunction, string schemaName = "public")
        {
            tableName = tableName.ToLower();
            colName = colName.ToLower();
            if (!self.Schema.Schema(schemaName).Table(tableName).Column(colName).Exists())
            {
                return constructColFunction(self.Alter.Table(tableName).AddColumn(colName));
            }
            else
            {
                return null;
            }
        }

        public static void DropColIfExists(this FluentMigrator.Migration self, string tableName, string colName,
            string schemaName = "public")
        {
            tableName = tableName.ToLower();
            colName = colName.ToLower();
            if (self.Schema.Schema(schemaName).Table(tableName).Column(colName).Exists())
            {
                self.Delete.Column(colName).FromTable(tableName);
            }
        }

        public static void CreateMasterTable(this FluentMigrator.Migration self, string tableName,
            bool addUserContextFields = true,
            string schemaName = "public")
        {
            CreateTableAndAddStandardColumns(self, tableName, addUserContextFields);

            CreateColIfNotExists(self, tableName, "Name", column => column.AsString().NotNullable());
            CreateColIfNotExists(self, tableName, "Description", column => column.AsString().Nullable());
        }

        public static void CreateStagingTable(this FluentMigrator.Migration self, string tableName,
            bool addUserContextFields = true,
            string schemaName = "public")
        {
            CreateTableAndAddStandardColumns(self, tableName, addUserContextFields);
            CreateColIfNotExists(self, tableName, "UploadedFileName", column => column.AsString().NotNullable());
        }

        public static void CreateTableAndAddStandardColumns(this FluentMigrator.Migration self, string tableName,
            bool addUserContextFields = true,
            string schemaName = "public")
        {
            var result = CreateTableIfNotExists(self, tableName.ToLower(),
                table => table.WithColumn("Id".ToLower()).AsInt64().Identity().NotNullable().PrimaryKey());

            if (addUserContextFields)
            {
                CreateForeignKeyColIfNotExists(self, tableName, "CreatedBy".ToLower(), "AppUser");
                CreateForeignKeyColIfNotExists(self, tableName, "UpdatedBy".ToLower(), "AppUser");
            }

            CreateColIfNotExists(self, tableName, "DateCreated".ToLower(),
                column => column.AsDateTimeOffset().WithDefaultValue(SystemMethods.CurrentDateTime).NotNullable());
            CreateColIfNotExists(self, tableName, "DateModified".ToLower(),
                column => column.AsDateTimeOffset().WithDefaultValue(SystemMethods.CurrentDateTime).NotNullable());
            CreateColIfNotExists(self, tableName, "IsActive".ToLower(),
                column => column.AsBoolean().WithDefaultValue(true).NotNullable());

            if (result != null)
            {
                self.Execute.Sql(string.Format(@"CREATE TRIGGER {0}_updatedatemodifiedcolumn
                        BEFORE UPDATE ON
                          {0}
                        FOR EACH ROW EXECUTE PROCEDURE
                          update_modified_column(); ", tableName));
            }
        }

        public static void TruncateTable(this FluentMigrator.Migration self, string tableName,
            string schemaName = "public")
        {
            self.Execute.Sql(string.Format(@"TRUNCATE TABLE ""{0}"" RESTART IDENTITY;", tableName));
        }

        public static void TruncateTable(this IDbTransaction transaction, string tableName,
            string schemaName = "public")
        {
            // transaction.Connection.ExecuteAsync(string.Format(@"TRUNCATE TABLE ""{0}"" RESTART IDENTITY;", tableName),
            //     transaction: transaction).Wait();
            transaction.Connection.ExecuteAsync($@"DELETE FROM ""{tableName}"";",
                transaction: transaction).Wait();
            transaction.Connection.ExecuteAsync($@"ALTER SEQUENCE {tableName}_id_seq RESTART WITH 1;",
                transaction: transaction).Wait();
        }

        public static void DisableTableTriggers(this IDbTransaction transaction, string tableName, string triggerName = "ALL",
            string schemaName = "public")
        {
            transaction.Connection.ExecuteAsync($@"ALTER TABLE ""{tableName}"" DISABLE TRIGGER ""{triggerName}"";",
                transaction: transaction).Wait();
        }

        public static void EnableTableTriggers(this IDbTransaction transaction, string tableName, string triggerName = "ALL",
            string schemaName = "public")
        {
            transaction.Connection.ExecuteAsync($@"ALTER TABLE ""{tableName}"" ENABLE TRIGGER ""{triggerName}"";",
                transaction: transaction).Wait();
        }

        public static void LoadMasterTable(this FluentMigrator.Migration self, string tableName,
            List<object> masterData)
        {
            self.TruncateTable(tableName);
            masterData.ToList().ForEach(x => self.Insert.IntoTable(tableName).Row(x));
        }

        public static void LoadMasterTable(this FluentMigrator.Migration self, string tableName, string[] names)
        {
            self.TruncateTable(tableName);
            names.ToList().ForEach(x => self.Insert.IntoTable(tableName).Row(new Dictionary<string, object>
            {
                {"name", x},
                {"createdby", 1},
                {"updatedby", 1}
            }));
        }

        public static void LoadMasterTable(this IDbConnection connection, string tableName,
            List<object> masterData, bool merge = true)
        {
            tableName = tableName.ToLower();
            connection.Execute("SET CONSTRAINTS ALL DEFERRED");
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var transaction = connection.BeginTransaction();
            if (merge)
            {
                transaction.LoadMasterTableUsingMerge(tableName, masterData);
            }
            else
            {
                LoadMasterTable(transaction, tableName, masterData);
            }
            
            transaction.Commit();
            connection.Execute($"select setval('{tableName}_id_seq',  (SELECT MAX(id) FROM {tableName}))");
            connection.Close();
        }

        public static void LoadMasterTable(this IDbTransaction transaction, string tableName,
            List<object> masterData)
        {
            transaction.TruncateTable(tableName);
            var pgConnection = transaction.Connection as NpgsqlConnection;
            if (pgConnection == null)
            {
                return;
            }

            var dt = masterData.ToArray().GetDataTableFromObjects();
            var dtColumnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower());
            var columns = pgConnection.GetSchema("Columns", new[] { pgConnection.Database, "public", tableName });
            try
            {
                var columnNamesAndTypes = dtColumnNames.ToDictionary(x => x,
                    x => DbTypeMapping.NpgsqlDbTypesMapping[
                        columns.Rows.Cast<DataRow>()
                            .FirstOrDefault(row => row["column_name"].ToString() == x)?["data_type"]
                            .ToString() ??
                        throw new PostgresException($"{x}-Column not found", "Critical", "Critical", "None")]);

                // transaction.DisableTableTriggers(tableName, $"FK_{tableName}_CreatedBy_AppUser_Id");
                // transaction.DisableTableTriggers(tableName, $"FK_{tableName}_UpdatedBy_AppUser_Id");
                transaction.BulkCopyData(dt, tableName, columnNamesAndTypes);

                // transaction.EnableTableTriggers(tableName, $"FK_{tableName}_CreatedBy_AppUser_Id");
                // transaction.EnableTableTriggers(tableName, $"FK_{tableName}_UpdatedBy_AppUser_Id");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void LoadMasterTable(this IDbConnection connection, string tableName, string[] names)
        {
            var transaction = connection.BeginTransaction();
            transaction.LoadMasterTable(tableName, names);
            transaction.Commit();
        }

        public static void LoadMasterTable(this IDbTransaction transaction, string tableName, string[] names)
        {
            transaction.TruncateTable(tableName);
            var dataTable = new DataTable();
            dataTable.Columns.Add("name");
            dataTable.Columns.Add("createdby", typeof(long));
            dataTable.Columns.Add("updatedby", typeof(long));
            foreach (var name in names)
            {
                var row = dataTable.NewRow();
                row["name"] = name;
                row["createdby"] = 1;
                row["updatedby"] = 1;
                dataTable.Rows.Add(row);
            }

            transaction.BulkCopyData(dataTable, tableName, new Dictionary<string, NpgsqlDbType>
            {
                {"name", NpgsqlDbType.Varchar},
                {"createdby", NpgsqlDbType.Bigint},
                {"updatedby", NpgsqlDbType.Bigint}
            });
        }

        public static void DropView(this FluentMigrator.Migration self, string viewName)
        {
            self.Execute.Sql($"DROP VIEW IF EXISTS {viewName};");
        }

        public static void CreateInheritedTable(this FluentMigrator.Migration self, string tableName,
            string parentTableName, string schemaName = "public")
        {
            if (!self.Schema.Schema(schemaName).Table(tableName).Exists())
            {
                self.Execute.Sql($"CREATE TABLE {tableName} () INHERITS ({parentTableName});");
            }
        }

        public static void CreateJsonBColIfNotExists(this FluentMigrator.Migration self, string tableName,
            string colName,
            string schemaName = "public")
        {
            tableName = tableName.ToLower();
            colName = colName.ToLower();
            if (!self.Schema.Schema(schemaName).Table(tableName).Column(colName).Exists())
            {
                self.Execute.Sql(string.Format($@"ALTER TABLE {schemaName}.{tableName} ADD COLUMN {colName} jsonb"));
            }
        }
    }
}