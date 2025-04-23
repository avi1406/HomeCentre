using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Geo;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.Common.MetaAttributes;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgEntityAction<T>
        where T : Entity
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;
        private readonly IWorkFlowManager1 _workFlowInstanceRepo;

        internal PgEntityAction(IUserContext userContext, IDbConnectionFactory connectionFactory,
            IConfiguration configuration)
        {
            _userContext = userContext;
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _workFlowInstanceRepo = new WorkFlowInstanceRepo(_connectionFactory, _userContext, configuration);
        }

        // private IDbConnection _connection;
        // private IDbTransaction _transaction;
        public string ConnectionString { get; set; }

        public async Task Activate(long id)
        {
            using var connection = this.GetConnection();
            await connection.ExecuteAsync($"update {typeof(T).Name} set isactive = true where id = @id", new
            {
                id
            });
        }

        public async Task Activate(long[] ids)
        {
            using var connection = this.GetConnection();
            await connection.ExecuteAsync($"update {typeof(T).Name} set isactive = true where id = any(@ids)", new
            {
                ids
            });
        }

        public async Task Deactivate(long id)
        {
            using var connection = this.GetConnection();
            await connection.ExecuteAsync($"update {typeof(T).Name} set isactive = false where id = @id", new
            {
                id
            });
        }

        public async Task Deactivate(long[] ids)
        {
            using var connection = this.GetConnection();
            await connection.ExecuteAsync($"update {typeof(T).Name} set isactive = false where id = any(@ids)",
                new
                {
                    ids
                });
        }

        internal virtual async Task<T> AddOrUpdate(T entity, long? overrideUserId = null,
            IDbTransaction transaction = null)
        {
            using var connection = this.GetConnection(transaction);
            var userId = overrideUserId ?? await _userContext.GetUserId();
            if (entity.Id <= 0)
            {
                entity.CreatedBy = userId;
                entity.UpdatedBy = userId;
                entity.DateCreated = DateTime.Now;
                entity.DateModified = DateTime.Now;
                entity.IsActive = true;
                entity.Id = await connection.InsertAsync<long, T>(entity, transaction);

                // Work Flow Run Process
                var workFlow = new WorkflowRun();
                entity.CreatedBy = userId;
                workFlow.UpdatedBy = userId;
                workFlow.DateCreated = DateTime.Now;
                workFlow.DateModified = DateTime.Now;
                workFlow.Description = entity.GetType().GetTypeInfo().Name.ToString();
                workFlow.Process = string.Empty;
                workFlow.RecordId = entity.Id;
                workFlow.Trigger = "CREATE";
                workFlow.TableMetadataId = 1;
                workFlow.IsActive = true;
                var result = (await connection.QueryAsync<AppSettings>("Select * from appsettings where name = 'IsHangFire'")).FirstOrDefault();
                bool.TryParse(result.Value.ToLower(), out var useHangFire);
                if (useHangFire)
                {
                    BackgroundJob.Enqueue(() =>
                        this._workFlowInstanceRepo.AddOrUpdateWorkflowRun(workFlow, null));
                }
                else
                {
                    this._workFlowInstanceRepo.AddOrUpdateWorkflowRun(workFlow, connection);
                }
            }
            else
            {
                var existingEntity = await this.Get(entity.Id, transaction);
                entity.CreatedBy = existingEntity.CreatedBy;
                entity.DateCreated = existingEntity.DateCreated;
                entity.UpdatedBy = userId;
                entity.DateModified = DateTime.Now;
                await connection.UpdateAsync(entity, transaction);

                // Work Flow Run Process
                var workFlow = new WorkflowRun();
                entity.CreatedBy = userId;
                workFlow.UpdatedBy = userId;
                workFlow.DateCreated = DateTime.Now;
                workFlow.DateModified = DateTime.Now;
                workFlow.Description = entity.GetType().GetTypeInfo().Name.ToString();
                workFlow.Process = string.Empty;
                workFlow.RecordId = entity.Id;
                workFlow.Trigger = "UPDATE";
                workFlow.TableMetadataId = 1;
                workFlow.IsActive = true;
                var result = (await connection.QueryAsync<AppSettings>("Select * from appsettings where name = 'IsHangFire'")).FirstOrDefault();
                bool.TryParse(result.Value.ToLower(), out var useHangFire);
                if (useHangFire)
                {
                    BackgroundJob.Enqueue(() =>
                        this._workFlowInstanceRepo.AddOrUpdateWorkflowRun(workFlow, connection));
                }
                else
                {
                    this._workFlowInstanceRepo.AddOrUpdateWorkflowRun(workFlow, connection);
                }
            }

            return await this.Get(entity.Id, transaction: transaction);
        }

        internal virtual async Task<T> Get(long id, IDbTransaction transaction = null)
        {
            using var connection = this.GetConnection(transaction);
            return await connection.GetAsync<T>(id, transaction);
        }

        internal virtual async Task<IEnumerable<T>> GetAll(string sql = null, object parameters = null,
            IDbTransaction transaction = null)
        {
            using var connection = this.GetConnection(transaction);
            return sql == null
                ? await connection.GetListAsync<T>(new
                {
                    isactive = true
                }, transaction)
                : await connection.GetListAsync<T>(sql, parameters, transaction);
        }

        internal IDbConnection GetConnection(IDbTransaction transaction = null)
        {
            var connection = string.IsNullOrWhiteSpace(this.ConnectionString)
                ? _connectionFactory.GetAppDbConnection(transaction)
                : new SafeNpgsqlConnection(ConnectionString, transaction);
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }

        internal virtual async Task<bool> Remove(long id, IDbTransaction transaction = null)
        {
            using var connection = this.GetConnection(transaction);
            var result = await connection.DeleteAsync<T>(id, transaction: transaction);
            return result > 0;
        }

        internal async Task Remove(long[] ids, IDbTransaction transaction = null)
        {
            using var connection = this.GetConnection();
            await connection.ExecuteAsync($"delete from {typeof(T).Name} where id = any(@ids)", new
            {
                ids
            }, transaction);
        }

        internal virtual async Task<SearchResult<T>> Search(string searchKey, int pageNumber, int pageCount,
            string filterCondition = null, DynamicParameters filterParams = null, string orderBy = "DateCreated",
            bool orderByAsc = true, bool searchLower = true, IDbTransaction transaction = null,
            bool? isActive = true)
        {
            return await this.Search<T>(searchKey, pageNumber, pageCount, typeof(T).Name, filterCondition, filterParams,
                orderBy, orderByAsc,
                searchLower, transaction, isActive);
        }

        internal virtual async Task<SearchResult<TResult>> Search<TResult>(string searchKey, int pageNumber,
            int pageCount, string tableName,
            string filterCondition = null, DynamicParameters filterParams = null, string orderBy = "DateCreated",
            bool orderByAsc = true, bool searchLower = true, IDbTransaction transaction = null,
            bool? isActive = true)
            where TResult : class
        {
            if (!typeof(TResult).GetColumns()
                    .Any(x => string.Equals(x, orderBy, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new AccessViolationException("Looks like something out of ordinary.");
            }

            using var connection = this.GetConnection(transaction);

            // IDbConnection existingConnection = null;
            // var isMyConnection = false;
            // if (transaction == null)
            // {
            //     existingConnection = this.InitializeConnection(transaction);;
            //     isMyConnection = true;
            // }

            // var connection = connection;
            tableName ??= typeof(T).Name;
            var searchFieldAttribute =
                typeof(TResult).GetCustomAttribute<SearchFieldAttribute>();
            var searchFilter = new StringBuilder();
            if (searchFieldAttribute != null && searchFieldAttribute.FieldNames.Any() &&
                !string.IsNullOrWhiteSpace(searchKey))
            {
                searchFilter.Append(" WHERE ");
                if (searchLower)
                {
                    searchFilter.Append(
                        $"(lower({searchFieldAttribute.FieldNames[0]}) LIKE '%{searchKey.ToLower()}%'");
                }
                else
                {
                    searchFilter.Append($"({searchFieldAttribute.FieldNames[0]} LIKE '%{searchKey.ToLower()}%'");
                }

                foreach (var searchField in searchFieldAttribute.FieldNames.Skip(1))
                {
                    if (searchLower)
                    {
                        searchFilter.Append($"OR lower({searchField}) LIKE '%{searchKey.ToLower()}%'");
                    }
                    else
                    {
                        searchFilter.Append($"OR {searchField} LIKE '%{searchKey.ToLower()}%'");
                    }
                }

                if (!string.IsNullOrWhiteSpace(filterCondition))
                {
                    searchFilter.Append(") and (");
                    searchFilter.Append(filterCondition);
                    searchFilter.Append(")");
                }
                else
                {
                    searchFilter.Append(")");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(filterCondition))
                {
                    searchFilter.Append(" WHERE ");
                    searchFilter.Append(filterCondition);
                }
            }

            var orderByDirection = orderByAsc ? "asc" : "desc";

            // assuming here you want the newest rows first, and column name is "created_date"
            // may also wish to specify the exact columns needed, rather than *
            var limits = pageCount == 0 ? string.Empty : "Limit @Limit Offset @Offset";
            var isActiveFilter = string.Empty;
            if (isActive != null)
            {
                if (string.IsNullOrWhiteSpace(filterCondition) && searchFilter.Length == 0)
                {
                    isActiveFilter = $" where isactive = {isActive.ToString().ToLower()}";
                }
                else
                {
                    isActiveFilter = $" and isactive = {isActive.ToString().ToLower()}";
                }
            }

            var query =
                $"SELECT * FROM {tableName}{searchFilter}{isActiveFilter} ORDER BY {orderBy} {orderByDirection} NULLS LAST {limits}";
            var countQuery =
                $"SELECT count(*) FROM {tableName}{searchFilter}{isActiveFilter}";
            if (filterParams == null)
            {
                filterParams = new DynamicParameters();
            }

            filterParams.Add("@Limit", pageCount);
            filterParams.Add("@OffSet", pageCount * (pageNumber - 1));
            var searchResult = new SearchResult<TResult>
            {
                TotalNoOfRecords = await connection.ExecuteScalarAsync<long>(countQuery, filterParams,
                    transaction: transaction),
                Records =
                    (await connection.QueryAsync<TResult>(query, filterParams, transaction: transaction))
                    .ToList()
            };

            // if (!isMyConnection)
            // {
            //     return searchResult;
            // }

            // if (connection.State == ConnectionState.Open)
            // {
            //     connection.Close();
            // }
            //
            // connection.Dispose();
            return searchResult;
        }

        protected async Task DoSelect<T1>(T1 t, string tableName)
        {
            using var connection = this.GetConnection();
            await connection.QueryAsync<T1>(t.GetSelectStatement(tableName));
        }

        protected async Task DoUpdate<T1>(T1 t, string tableName, string keyName = "Id", IDbTransaction transaction = null)
        {
            var connection = transaction == null ? this.GetConnection() : transaction.Connection;
            await connection.ExecuteAsync(t.GetUpdateStatement(tableName, keyName), t, transaction);
            if (transaction == null)
            {
                connection.Close();
                connection.Dispose();
            }
        }

        protected PgEntityAction<T1> GetAction<T1>()
            where T1 : Entity
        {
            return new PgEntityAction<T1>(_userContext, _connectionFactory, _configuration);
        }

        protected async Task<string> GetRoleId()
        {
            return await _userContext.GetRoleId();
        }

        protected async Task<string> GetRoleLevel()
        {
            return await _userContext.GetRoleId();
        }

        protected async Task<long> GetUserId()
        {
            return await _userContext.GetUserId();
        }
    }
}