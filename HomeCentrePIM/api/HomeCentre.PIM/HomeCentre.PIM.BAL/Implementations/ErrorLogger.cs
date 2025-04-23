using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations.Storage
{
    public class ErrorLogger : PgEntityAction<ErrorLogTable>, IErrorLogger
    {
        private readonly IUserContext _userContext;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDbCache _dbCache;

        public ErrorLogger(IUserContext userContext, IDbConnectionFactory connectionFactory, IDbCache dbCache, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _dbConnectionFactory = connectionFactory;
            _dbCache = dbCache;
        }

        public async Task InsertError(string stagingtablename, string stagingtablecolumnname, string errordetails)
        {
            var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
            var userId = int.Parse(valueFromConfig.Value);
            using var connection = this.GetConnection();
            var error = new ErrorLogTable();

            error.StagingTableName = stagingtablename;
            error.StagingTableColumnName = stagingtablecolumnname;
            error.ErrorDetails = errordetails;

            error = await AddOrUpdate(error, userId);

            throw new HandledException(code: 400, message: "Email should not be null");
        }
    }
}