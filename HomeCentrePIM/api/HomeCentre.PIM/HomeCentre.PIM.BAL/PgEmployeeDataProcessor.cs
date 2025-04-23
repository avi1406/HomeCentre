using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL;

public class PgEmployeeDataProcessor : PgEntityAction<AppUser>, IDataProcessor
{
    public PgEmployeeDataProcessor(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration)
        : base(userContext, connectionFactory, configuration)
    {
    }

    public async Task<List<UploadedFileData>> ValidateData(List<UploadedFileData> data, long templateId)
    {
        var requiredFields = new[] {"EMP_NAME", "EMAIL_ADDRESS", "STATUS", "EMP_CODE"};
        foreach (var fileData in data)
        {
            fileData.ErrorData ??= new Dictionary<string, List<string>>();

            foreach (var field in requiredFields)
            {
                if (string.IsNullOrWhiteSpace(fileData.ColumnData[field].ToString()))
                {
                    fileData.ErrorData.Add(field, new List<string>
                    {
                        $"Empty '{field}' field, its not optional."
                    });
                }

                if (fileData.ColumnData.ContainsKey("EMAIL_ADDRESS"))
                {
                    var email = fileData.ColumnData["EMAIL_ADDRESS"]?.ToString();
                    if (!email.IsValidEmail())
                    {
                        fileData.ErrorData.Add(field, new List<string>
                        {
                            $"Email is not valid. '{email}'"
                        });
                    }
                }
            }
        }

        return data;
    }

    public async Task SaveData(List<UploadedFileData> data, long templateId)
    {
        var validateData = await this.ValidateData(data, templateId);
        if (validateData.SelectMany(x => x.ErrorData.Values).Any())
        {
            throw new HandledException("Data contains errors");
        }

        if (data.Count == 0)
        {
            return;
        }

        // var departments = await _cache.GetMaster<Department>();
        var employeeData = data.Select(x => new
        {
            // DepartmentId = departments.First(d => d.Name == x.ColumnData["Department"]?.ToString()),
            Email = x.ColumnData["EMAIL_ADDRESS"]?.ToString(),
            Name = x.ColumnData["EMP_NAME"]?.ToString(),
            EmployeeNo = x.ColumnData["EMP_CODE"]?.ToString(),
            IsActive = x.ColumnData["STATUS"].ToString() == "C"
        }).Cast<object>().ToList();

        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();
        transaction.LoadMasterTableUsingMerge("appuser", employeeData, new[] { "employeeno" });
        transaction.Commit();
    }
}