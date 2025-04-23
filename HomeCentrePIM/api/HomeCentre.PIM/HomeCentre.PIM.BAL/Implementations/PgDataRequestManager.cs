using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgDataRequestManager : PgEntityAction<DataRequest>, IDataRequestManager
{
    public PgDataRequestManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration)
        : base(userContext, connectionFactory, configuration)
    {
    }

    public async Task<DataRequest> CreateRequest(DataRequest request)
    {
        using var connection = this.GetConnection();
        var requestItems = request.ItemCodes;
        var mappedRecords = (await connection.QueryAsync<(long, string)>(
            "select id, uid from product where isactive = true and uid = any(@uids)", new
            {
                uids = requestItems
            })).ToList();
        var userId = await this.GetUserId();
        var missingRecords = requestItems.Except(mappedRecords.Select(x => x.Item2)).ToList();
        if (missingRecords.Any())
        {
            throw new InvalidDataException(
                $"No matching records found for '{string.Join(",", missingRecords)}' item codes");
        }

        using var transaction = connection.BeginTransaction();
        request = await this.AddOrUpdate(request, transaction: transaction);
        var dataRequestItems = mappedRecords.Select(x => new DataRequestItem
        {
            DataRequestId = request.Id,
            ProductId = x.Item1,
            CreatedBy = userId,
            UpdatedBy = userId,
            IsActive = true,
            DateCreated = DateTimeOffset.UtcNow,
            DateModified = DateTimeOffset.UtcNow,
        }).ToList();
        transaction.BulkInsert(nameof(DataRequestItem), dataRequestItems.ToArray().GetDataTableFromObjects());
        transaction.Commit();
        return request;
    }

    public async Task<SearchResult<DataRequest>> SearchDataRequests(string searchKey, int pageNumber, int pageSize,
        bool isActive)
    {
        return await this.Search(searchKey, pageNumber, pageSize, isActive: isActive);
    }

    public async Task<SearchResult<DataRequestProduct>> SearchDataRequestItems(string searchKey, int pageNumber,
        int pageSize, long dataRequestId)
    {
        var dataRequest = await this.Get(dataRequestId);

        if (dataRequest == null)
        {
            throw new KeyNotFoundException("Data request not found");
        }

        using var connection = this.GetConnection();
        var columns = await connection.QueryAsync<string>(
            "select pa.name from productattribute pa join marketplacetemplateattribute m on pa.id = m.productattributeid where m.marketplacetemplateid = @templateId",
            new
            {
                templateId = dataRequest.TemplateId
            });
        var dynParams = new DynamicParameters();
        dynParams.Add("dataRequestId", dataRequestId);
        var result = await this.Search<DataRequestProduct>(searchKey, pageNumber, pageSize,
            tableName: "vw_product_datarequest_items", filterCondition: "dataRequestId = @dataRequestId", dynParams);
        foreach (var requestProduct in result.Records)
        {
            requestProduct.ColumnData = requestProduct.ColumnData.Where(x => columns.Contains(x.Key))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        return result;
    }

    public async Task<string> DownloadRequestRecords(long dataRequestId)
    {
        var dataRequest = await this.Get(dataRequestId);
        using var connection = this.GetConnection();
        var columns = await connection.QueryAsync<string>(
            "select pa.name from productattribute pa join marketplacetemplateattribute m on pa.id = m.productattributeid where m.marketplacetemplateid = @templateId",
            new
            {
                templateId = dataRequest.TemplateId
            });
        var dynParams = new DynamicParameters();
        dynParams.Add("dataRequestId", dataRequestId);
        var records = (await connection.QueryAsync<DataRequestProduct>(
            "select * from vw_product_datarequest_items where dataRequestId = @dataRequestId and qcstatusid = 10", new
            {
                dataRequestId
            })).ToList(); // qc resolved only.

        foreach (var requestProduct in records)
        {
            requestProduct.ColumnData = requestProduct.ColumnData.Where(x => columns.Contains(x.Key))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        var tempFile = Path.GetTempFileName();
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet();
        using var stream = new MemoryStream();

        if (records.Count > 0)
        {
            var keys = columns.Distinct().ToArray();

            var r = 1;
            var cH = 1;

            sheet.Cell(r, cH).Value = "id";
            cH++;
            sheet.Cell(r, cH).Value = "uid";
            cH++;
            sheet.Cell(r, cH).Value = "qcstatusid";
            cH++;
            foreach (var key in keys)
            {
                sheet.Cell(r, cH).Value = key;
                cH++;
            }

            r++;
            foreach (var record in records)
            {
                var c = 1;
                sheet.Cell(r, c).Value = record.Id.ToString();
                c++;
                sheet.Cell(r, c).Value = record.Uid;
                c++;
                sheet.Cell(r, c).Value = record.QcStatusId.ToString();
                c++;
                foreach (var key in keys)
                {
                    sheet.Cell(r, c).Value = record.ColumnData.ContainsKey(key) ? record.ColumnData[key] : string.Empty;
                    c++;
                }

                r++;
            }
        }

        if (sheet.Tables.Any())
        {
            sheet.Tables.Table(0).ShowAutoFilter = false;
            sheet.Tables.Table(0).SetShowAutoFilter(false);
        }

        wb.SaveAs(stream);
        await using var fs = File.OpenWrite(tempFile);
        stream.Position = 0;
        await stream.CopyToAsync(fs);
        return tempFile;
    }

    public async Task DeactivateDataRequests(long[] ids)
    {
        await this.Activate(ids);
    }

    public async Task ActivateDataRequests(long[] ids)
    {
        await this.Deactivate(ids);
    }

    public async Task<IDictionary<string, int>> GetDataRequestItemCountByStatus(long dataRequestId)
    {
        using var connection = this.GetConnection();
        var result = await connection.QueryAsync<(string, int)>(
            "select COALESCE(p2.name, 'New'), count(1) as recordcount from datarequestitem dr join product p on p.id = dr.productid left join productqcstatus p2 on p.qcstatusid = p2.id where dr.datarequestid = @dataRequestId group by p2.name;",
            new
            {
                dataRequestId
            });
        return result.ToDictionary(k => k.Item1, v => v.Item2);
    }

    public async Task SendRecordToQc(long productId)
    {
        using var connection = this.GetConnection();
        await connection.ExecuteAsync("update product qcstatusid = 2 where id = @productId", new
        {
            productId
        });
    }
}