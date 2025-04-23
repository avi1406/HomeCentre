using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgMasterManager : IMasterManager
    {
        private readonly IUserContext _userContext;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;
        private readonly IDbCache _dbCache;

        public PgMasterManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration, IDbCache dbCache)
        {
            _userContext = userContext;
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _dbCache = dbCache;
        }

        public Task<IEnumerable<T>> GetAll<T>()
            where T : Entity
        {
            var entityAction = new PgEntityAction<T>(_userContext, _connectionFactory, _configuration);
            return entityAction.GetAll();
        }

        public async Task<IEnumerable<AppSettings>> GetAppSettings()
        {
            var entityAction = new PgEntityAction<AppSettings>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();
            return await connection.QueryAsync<AppSettings>(
                "SELECT id, createdby, updatedby, datecreated, datemodified, isactive, name, description, value, jsonvalue::TEXT as jsonvalue FROM AppSettings;");
        }

        public async Task<IEnumerable<T>> Search<T>(bool isActive)
        {
            var entityAction = new PgEntityAction<AppSettings>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();

            var query = $"select * from {typeof(T).Name} where (lower(name) LIKE '%{string.Empty}%'OR lower(description) LIKE '%{string.Empty}%') and isActive=@Isactive order by name";
            var result = await connection.QueryAsync<T>(query, new { isactive = isActive });

            return result;
        }

        public async Task<IEnumerable> GetUserRole(long? appuserID)
        {
            var entityAction = new PgEntityAction<AppSettings>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();
            var basicRoleId = await _dbCache.FindAppSettings("BasicRole");
            var query = $"SELECT appuser.name, role.name, appuserrole.roleid, appuserrole.appuserid from appuserrole INNER join appuser ON appuser.id = appuserrole.appuserid INNER join role ON role.id = appuserrole.roleid WHERE appuserid={appuserID} and role.id != {basicRoleId.Value} order by roleid";
            var result = await connection.QueryAsync(query, new { appuserID = appuserID });

            return result;
        }

        public async Task<IEnumerable> GetUserDepartment(long? appuserID)
        {
            var entityAction = new PgEntityAction<AppSettings>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();

            // var departmentId = await connection.QueryAsync("select * from appuser where id = @appuserid", new { appuserid = appuserID });
            var result = await connection.QueryAsync("select department.name,department.id from department inner join appuserrole on department.id=appuserrole.departmentid where appuserrole.appuserid = @appuserID group by department.id", new { appuserID = appuserID });
            return result;
        }

        public Stream ExportExcelFromJObject(string jsonString)
        {
            var dataTable = JsonConvert.DeserializeObject<DataTable>(jsonString);
            return new FileExportHelper().ExportToExcel(dataTable);
        }

        public async Task<List<KeyValuePair<string, string>>> GetMaster(string attrLookupTable,
            string attrLookupTableColumn)
        {
            var entityAction = new PgEntityAction<AppSettings>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();
            var data = (await connection.QueryAsync<KeyValuePair<string, string>>($"select * from {attrLookupTable}")).ToList();
            return data;
        }

        public async Task<IEnumerable<RejectionReason>> GetRejectionReason()
        {
            var entityAction = new PgEntityAction<RejectionReason>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();
            return await connection.QueryAsync<RejectionReason>(
                "SELECT id, createdby, updatedby, datecreated, datemodified, isactive, code, name, description ::TEXT as jsonvalue FROM RejectionReason;");
        }

        public async Task<IEnumerable<Division>> GetDivision()
        {
            var entityAction = new PgEntityAction<Division>(_userContext, _connectionFactory, _configuration);
            using var connection = entityAction.GetConnection();
            return await connection.QueryAsync<Division>(
                "SELECT id, createdby, updatedby, datecreated, datemodified, isactive, name, description ::TEXT as jsonvalue FROM Division;");
        }

        public async Task<List<Merchandise_hierarchy>> GetMerchandiseHierarchy()
        {
            var action = new PgEntityAction<Merchandise_hierarchy>(_userContext, _connectionFactory, _configuration);
            using var connection = action.GetConnection();
            return connection.QueryAsync<Merchandise_hierarchy>("select * from merchandise_hierarchy").Result.ToList();
        }
    }
}