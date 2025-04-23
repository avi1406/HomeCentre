using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DocumentFormat.OpenXml.Wordprocessing;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgLabelManager : PgEntityAction<Label>, ILabelManager
    {
        private readonly IDbCache _dbCache;

        public PgLabelManager(IUserContext userContext, IDbCache dbCache, IDbConnectionFactory connectionFactory, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _dbCache = dbCache;
        }

        // public async Task AddOrUpdateLabel(LabelModel model)
        // {
        //    await this.AddOrUpdate(model.To<Label>());
        // }
        public async Task<string> AddOrUpdateLabel(LabelModel model)
        {
            using var connection = this.GetConnection();
            var data = connection.Query<LabelModel>("select * from label where  name=@name", new { name = model.Name }).ToArray();
            foreach (var item in data)
            {
                if (item.Id != model.Id && item.Name == model.Name)
                {
                    throw new HandledException(code: 400, message: "Name Already Existing");
                }
                else
                {
                    await this.AddOrUpdate(model.To<Label>());
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        public async Task<IEnumerable<LabelModel>> GetLabels(long? languageId)
        {
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@languageId", languageId);
            return (await this.Search<LabelModel>(string.Empty, 0, 0, "vw_labels", orderBy: "name",
                filterCondition: "languageId = @languageId", filterParams: dynamicParameters))?.Records;
        }

        public async Task<SearchResult<LabelModel>> Search(long languageId)
        {
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@languageId", languageId);
            return await this.Search<LabelModel>(string.Empty, 0, 0, "vw_labels", orderBy: "name",
                filterCondition: "languageId = @languageId", filterParams: dynamicParameters);
        }

        public async Task ActivateRole(long id)
        {
            using var connection = this.GetConnection();
            var data = await connection.QueryAsync<Role>("select * from role where id = @id", new { id });

            if (data.Count() == 0)
            {
                throw new HandledException(code: 400, message: "no data found with this id");
            }

            await connection.ExecuteScalarAsync<Role>($"update role set isactive = true where id = @id", new
            {
                id
            });
            connection.Close();
        }

        public async Task ActivateRoleBatch(long[] ids)
        {
            foreach (long id in ids)
            {
                await this.ActivateRole(id);
            }
        }

        public async Task DeactivateRole(long id)
        {
            using var connection = this.GetConnection();
            var data = await connection.QueryAsync<Role>("select * from role where id = @id", new { id });

            if (data.Count() == 0)
            {
                throw new HandledException(code: 400, message: "no data found with this id");
            }

            await connection.ExecuteScalarAsync<Role>($"update role set isactive = false where id = @id", new
            {
                id
            });
            connection.Close();
        }

        public async Task DeactivateRoleBatch(long[] ids)
        {
            foreach (long id in ids)
            {
                await this.DeactivateRole(id);
            }
        }

        public async Task UpdateRoles(long id, string? name, string? description, bool? isActive)
        {
            using var connection = this.GetConnection();
            var data = await connection.QueryAsync<Role>("select * from role where id = @id", new { id });

            if (data.Count() == 0)
            {
                throw new HandledException(code: 400, message: "no data found with this id");
            }

            var sqlQuery = "update role set " +
                "name=@name," +
                "description=@description, isactive=@isactive where id=@id";
            await connection.ExecuteScalarAsync<Role>(sqlQuery, new
            {
                id,
                name,
                description,
                isActive
            });
        }

        public async Task Ismail(string email)
        {
            using var connection = this.GetConnection();
            var data = await connection.QueryAsync<Role>("select * from appuser where email = @email", new { email });

            if (data.Count() == 0)
            {
                throw new HandledException(code: 400, message: "no data found with this id");
            }

            await connection.ExecuteScalarAsync<Role>($"update appuser set isemailverified = true where email = @email", new
            {
                email
            });
            connection.Close();
        }

        public async Task DeleteRoles(long appuserId, long roleId)
        {
            using var connection = this.GetConnection();
            await connection.QueryAsync<AppUserRoleModel>($"Delete from appuserrole where appuserid = @AppuserId AND roleid =@RoleId ",
                new { appuserId, roleId });
        }

        public async Task<IEnumerable> GetErrorLogUser(DateTime fromDate, DateTime toDate)
        {
            using var connection = this.GetConnection();
            string query;
            if (fromDate == DateTime.MinValue && toDate == DateTime.MinValue)
            {
                var numberOfDaysfromConfig = await _dbCache.FindAppSettings("NoOfDaysForErrorLogger");
                var date = DateTime.Now.AddDays(-int.Parse(numberOfDaysfromConfig.Value));
                fromDate = date;
                toDate = DateTime.Now;
                query = $"select * from errorlogtable where datecreated between @FromDate and @ToDate";
            }
            else if (toDate == DateTime.MinValue)
            {
                query = $"select * from errorlogtable where datecreated>=@FromDate ";
            }
            else if (fromDate == DateTime.MinValue)
            {
                query = $"select * from errorlogtable where datecreated<=@ToDate ";
            }
            else
            {
                query = $"select * from errorlogtable where datecreated  between @FromDate and @ToDate";
            }

            var result = await connection.QueryAsync(query, new { FromDate = fromDate, ToDate = toDate });
            return result;
        }
    }
}