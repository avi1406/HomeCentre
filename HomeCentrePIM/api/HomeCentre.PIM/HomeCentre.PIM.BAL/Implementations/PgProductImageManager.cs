using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Status = HomeCentre.PIM.BAL.Model.Geo.Status;

namespace HomeCentre.PIM.BAL.Implementations
{
    public static class DataTableExtensions
    {
        public static DataTable SetColumnsOrder(this DataTable table, params string[] columnNames)
        {
            int columnIndex = 0;
            foreach (var columnName in columnNames)
            {
                table.Columns[columnName].SetOrdinal(columnIndex);
                columnIndex++;
            }

            return table;
        }
    }

    public class PgProductImageManager : PgEntityAction<ProductImage>, IProductImageManager
    {
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;
        private readonly IBlobRepo _blobRepo;

        public PgProductImageManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration, IBlobRepo blobRepo)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _configuration = configuration;
            _blobRepo = blobRepo;
        }

        public async Task AddOrUpdateProductImages(IEnumerable<ProductImage> productImages)
        {
            try
            {
                using var connection = this.GetConnection();
                var check = false;
                var invalidItemMessages = new List<string>();
                var incompleteMessages = new List<string>();
                var alreadyExist = new List<string>();
                foreach (var item in productImages)
                {
                    var checks = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>($"select * from itemcodestaging where code ={item.ItemCode}");
                    if (checks == null)
                    {
                        invalidItemMessages.Add(item.ItemCode.ToString());
                    }
                }

                if (invalidItemMessages.Any())
                {
                    throw new Exception($"Invalid itemcodes: {string.Join(", ", invalidItemMessages)}");
                }

                foreach (var data in productImages)
                {
                    var itemcode = await connection.QueryAsync<ItemCodeStaging>($"select it.code from itemcodestaging it join product p on p.uid = it.uidcode where (p.statusfilter = '0' or p.statusfilter is null) and it.code={data.ItemCode}");

                    if (itemcode.Count() != 0)
                    {
                        incompleteMessages.Add(data.ItemCode.ToString());
                    }
                }

                if (incompleteMessages.Any())
                {
                    throw new Exception($"Attributes Incomplete for itemcodes: {string.Join(", ", incompleteMessages)}");
                }

                foreach (var value in productImages)
                {
                    var existingItemcode = await connection.QueryFirstOrDefaultAsync<ProductImage>("SELECT * FROM productimage WHERE itemcode =@Itemcode", new { Itemcode = value.ItemCode });
                    if (existingItemcode != null)
                    {
                        alreadyExist.Add(value.ItemCode.ToString());
                    }
                }

                if (alreadyExist.Any())
                {
                    throw new Exception($" Itemcodes '{string.Join(", ", alreadyExist)}' are already exists");
                }

                foreach (var productImage in productImages)
                {
                    var itemcodecheck = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>($"select * from itemcodestaging where code ={productImage.ItemCode}");

                    // if (itemcodecheck == null)
                    // {
                    //    throw new Exception($"Invalid itemcode '{productImage.ItemCode}'");
                    // }
                    var itemcodeUidcheck = await connection.QueryAsync<Product>($"select * from product where isactive = true and uid ='{itemcodecheck.UidCode}'");
                    if (itemcodeUidcheck.Count() == 0)
                    {
                        throw new Exception($"Uid is inactive or not exist for this '{productImage.ItemCode}' Itemcode");
                    }

                    var divisionCodesString = string.Empty;
                    if (productImage.Userdivision != null)
                    {
                        var itemcodeUidDiv = await connection.QueryFirstOrDefaultAsync<Product>($"select * from product where uid ='{itemcodecheck.UidCode}'");
                        var productDivisionIds = productImage.Userdivision.ToString().Split(',').Select(id => id.Trim());
                        divisionCodesString = itemcodeUidDiv.ColumnData["Division Code"].ToString();
                        var divisionCodesList = divisionCodesString.Split(',').Select(code => code.Trim()).ToList();
                        if (!productDivisionIds.Any(id => divisionCodesList.Contains(id)))
                        {
                            throw new Exception($"Can't able to create, Due to the itemcode is from different division");
                        }
                    }

                    var itemcode = await connection.QueryAsync<ItemCodeStaging>($"select it.code from itemcodestaging it join product p on p.uid = it.uidcode where (p.statusfilter = '0' or p.statusfilter is null) and it.code={productImage.ItemCode}");

                    // if (itemcode.Count() != 0)
                    // {
                    //    throw new Exception($"Attributes Incomplete for '{productImage.ItemCode}'");
                    // }
                    // var existingItemcode = await connection.QueryFirstOrDefaultAsync<ProductImage>("SELECT * FROM productimage WHERE itemcode =@Itemcode", new { Itemcode = productImage.ItemCode });
                    // if (existingItemcode != null)
                    // {
                    //    throw new Exception($" Itemcode '{productImage.ItemCode}' is already exists");
                    // }
                    if (itemcodecheck != null && itemcodeUidcheck.Count() != 0 && !itemcode.Any(ic => ic.Code == productImage.ItemCode))
                    {
                        check = true;
                        ////if (long.TryParse(divisionCodesString, out long divisionId))
                        ////{
                        ////    productImage.Divisionid = divisionId;
                        ////    productImage.Userdivision = null;
                        ////}

                        ////var product = await this.AddOrUpdate(productImage);
                        ////var photoshootProduct = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE itemcode = @ItemCode", new { ItemCode = productImage.ItemCode });
                        ////await connection.ExecuteAsync("UPDATE productimage SET productid = @ProductId,userdivision=null WHERE itemcode = @ItemCode", new { ProductId = photoshootProduct.Id, ItemCode = productImage.ItemCode });
                        ////insertedProducts.Add(product);
                    }
                }

                if (check)
                {
                    foreach (var productImage in productImages)
                    {
                        var itemcodecheck = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>($"select * from itemcodestaging where code ={productImage.ItemCode}");
                        var itemcodeUidDiv = await connection.QueryFirstOrDefaultAsync<Product>($"select * from product where uid ='{itemcodecheck.UidCode}'");
                        var divisionCodesString = itemcodeUidDiv.ColumnData["Division Code"].ToString();
                        if (long.TryParse(divisionCodesString, out long divisionId))
                        {
                            productImage.Divisionid = divisionId;
                            productImage.Userdivision = null;
                        }

                        var product = await connection.ExecuteAsync("INSERT INTO productimage (itemcode, imagetype, imageurl, receiveddate, divisionid) VALUES (@Itemcode, @ImageType,@ImageUrl,@ReceivedDate, @DivisionId)",
        new { Itemcode = productImage.ItemCode, ImageType = productImage.ImageType, ImageUrl = productImage.ImageUrl, ReceivedDate = productImage.ReceivedDate, DivisionId = productImage.Divisionid });
                        var photoshootProduct = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE itemcode = @ItemCode", new { ItemCode = productImage.ItemCode });
                        await connection.ExecuteAsync("UPDATE productimage SET productid = @ProductId,userdivision=null WHERE itemcode = @ItemCode", new { ProductId = photoshootProduct.Id, ItemCode = productImage.ItemCode });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task UpdateReceivedproducts(long id, long newid, string? imageType, string? imageUrl, DateTime? receivedDate, string? userdivision)
        {
            try
            {
                using var connection = this.GetConnection();
                bool isvalied = true;
                if (newid != 0)
                {
                    var itemcodecheck = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>($"select * from itemcodestaging where code ={newid}");
                    if (itemcodecheck == null)
                    {
                        isvalied = false;
                        throw new Exception($"Invalid itemcode '{newid}'");
                    }

                    var itemcodeUidcheck = await connection.QueryAsync<Product>($"select * from product where isactive = true and uid ='{itemcodecheck.UidCode}'");
                    if (itemcodeUidcheck.Count() == 0)
                    {
                        isvalied = false;
                        throw new Exception($"Uid is inactive or not exist for this '{newid}' Itemcode");
                    }

                    var divisionCodesString = string.Empty;
                    if (userdivision != null)
                    {
                        var itemcodeUidDiv = await connection.QueryFirstOrDefaultAsync<Product>($"select * from product where uid ='{itemcodecheck.UidCode}'");
                        var productDivisionIds = userdivision.ToString().Split(',').Select(id => id.Trim());
                        divisionCodesString = itemcodeUidDiv.ColumnData["Division Code"].ToString();
                        var divisionCodesList = divisionCodesString.Split(',').Select(code => code.Trim()).ToList();
                        if (!productDivisionIds.Any(id => divisionCodesList.Contains(id)))
                        {
                            isvalied = false;
                            throw new Exception($"Can't able to create, Due to the itemcode is from different division");
                        }
                    }

                    var itemcode = await connection.QueryAsync<ItemCodeStaging>($"select it.code from itemcodestaging it join product p on p.uid = it.uidcode where (p.statusfilter = '0' or p.statusfilter is null) and it.code={newid}");

                    if (itemcode.Count() != 0)
                    {
                        isvalied = false;
                        throw new Exception($"Attributes Incomplete for '{newid}'");
                    }
                }

                var existingItemcode = await connection.QueryFirstOrDefaultAsync<ProductImage>("SELECT * FROM productimage WHERE itemcode =@Itemcode", new { Itemcode = newid });
                if (existingItemcode != null)
                {
                    isvalied = false;
                    throw new Exception($" Itemcode '{newid}' is already exists");
                }

                var getitemcode = await connection.QueryFirstOrDefaultAsync<ProductImage>("SELECT * FROM productimage WHERE id =@id", new { id });
                var getproductid = await connection.QueryFirstOrDefaultAsync<int>("SELECT id FROM photoshootproduct WHERE itemcode =@Itemcode", new { Itemcode = getitemcode.ItemCode });
                if (isvalied)
                {
                    var setClauses = new List<string>();
                    if (newid != 0)
                    {
                        setClauses.Add("Itemcode = @newid");
                    }

                    if (getproductid != null)
                    {
                        setClauses.Add("productid = @getproductid");
                    }

                    if (imageType != null)
                    {
                        setClauses.Add("imagetype = @ImageType");
                    }

                    if (imageUrl != null)
                    {
                        setClauses.Add("imageurl = @ImageUrl");
                    }
                    else
                    {
                        setClauses.Add("imageurl = @ImageUrl");
                    }

                    if (receivedDate != null)
                    {
                        setClauses.Add("receiveddate = @ReceivedDate");
                    }

                    var setClause = string.Join(", ", setClauses);

                    var query = $"UPDATE productimage SET {setClause} WHERE id = @Id";

                    await connection.ExecuteAsync(query, new { Id = id, newid, ImageType = imageType, ImageUrl = imageUrl, ReceivedDate = receivedDate, getproductid });
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateQCAction(long itemcode, string? status, string? samplestatusrejection, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                if (status == "Approved")
                {
                    ////                var studios = await connection.QueryFirstOrDefaultAsync<Studio>("SELECT priority FROM studio");
                    ////                if (studios != null)
                    ////                {
                    ////                    var division = await connection.QueryFirstOrDefaultAsync<string>("SELECT p.columndata ->'Division Code' as Division FROM product p join itemcodestaging i on i.uidcode=p.uid WHERE i.code  = @itemcode", new { itemcode });
                    ////                    var studios1 = await connection.QueryAsync<Studio>(
                    ////"SELECT priority\r\nFROM studio\r\nWHERE divisionid = CAST(@division AS INTEGER)\r\n  AND DATE(effectivefromdate) <= CURRENT_DATE\r\n  AND remainingcapacity!=0 AND isactive = true\r\nORDER BY\r\n  CASE\r\n    WHEN priority = 'P0' THEN 0\r\n    ELSE 1\r\n  END,\r\n  substring(priority from '^[^0-9]+')::text, \r\n  nullif(substring(priority from '[0-9]+')::int, 0) ASC;",
                    ////new { division });

                    ////                    var product = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE Itemcode = @itemcode", new { itemcode });
                    ////                    bool check = false;
                    ////                    foreach (var studio in studios1)
                    ////                    {
                    ////                        var photoshootProduct = await connection.QueryFirstOrDefaultAsync<Studio>($"SELECT remainingcapacity as TotalCapacity,id FROM studio WHERE divisionid = CAST(@division AS INTEGER) AND priority = '{studio.Priority}' LIMIT 1", new { division });
                    ////                        if (photoshootProduct != null && photoshootProduct.TotalCapacity != 0)
                    ////                        {
                    ////                            await connection.ExecuteAsync("INSERT INTO productstudio (productid, studioid, priority,datecreated,itemcode) VALUES (@productid, @studioid, @priority,@datecreate,@itemcode)", new { productid = product.Id, studioid = photoshootProduct.Id, priority = studio.Priority, datecreate = DateTime.UtcNow, itemcode });
                    ////                            var remainingcapacity = photoshootProduct.TotalCapacity - 1;
                    ////                            await connection.ExecuteAsync($"Update studio set remainingcapacity = {remainingcapacity} where id = {photoshootProduct.Id}");
                    ////                            var query = $"UPDATE productimage SET samplestatus = '{status}' , statusdate = @CurrentDate WHERE itemcode = @ItemCode";
                    ////                            await connection.ExecuteAsync(query, new { ItemCode = itemcode, CurrentDate = DateTime.UtcNow });
                    ////                            await connection.ExecuteAsync($"Update productimage set camqcstatus = 1 where itemcode = {itemcode}");
                    ////                            await connection.ExecuteAsync($"Update productimage set handoverstatus = 1 where itemcode = {itemcode}");
                    ////                            check = true;
                    ////                            break;
                    ////                        }
                    ////                    }

                    ////                    if (!check)
                    ////                    {
                    ////                        throw new Exception("No capacity");
                    ////                    }
                    ////                }
                    ////                else
                    ////                {
                    ////                    throw new Exception("No Studio is created.");
                    ////                }

                    var query = $"UPDATE productimage SET samplestatus = '{status}' , statusdate = @CurrentDate, samplestatusby = '{username}' WHERE itemcode = @ItemCode";
                    await connection.ExecuteAsync(query, new { ItemCode = itemcode, CurrentDate = DateTime.UtcNow });
                    await connection.ExecuteAsync($"Update productimage set camqcstatus = 1, camqcl1submitdate = null, camqcl1submitby = null where itemcode = {itemcode}");
                    await connection.ExecuteAsync($"Update productimage set handoverstatus = 1 where itemcode = {itemcode}");
                }
                else if (status == "Rejected")
                {
                    var query = $"UPDATE productimage SET samplestatus = '{status}', samplestatusrejection= '{samplestatusrejection}' , statusdate = @CurrentDate WHERE itemcode = @ItemCode";
                    await connection.ExecuteAsync(query, new { ItemCode = itemcode, CurrentDate = DateTime.UtcNow });
                    await connection.ExecuteAsync($"Update productimage set camqcstatus = 1, camqcl1submitdate = null, camqcl1submitby = null where itemcode = {itemcode}");
                }
                else
                {
                    await connection.ExecuteAsync($"Update productimage set camqcstatus = null, camqcl1submitdate = null, camqcl1submitby = null where itemcode = {itemcode}");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetRecords()
        {
            try
            {
                using var connection = this.GetConnection();
                var resultList = new List<ReceivedAndHandoverStatus>();

                // var query = await connection.QueryAsync<ReceivedAndHandoverStatus>($"SELECT DISTINCT pi.id,pi.datemodified,pi.itemcode,ics.eancode,pi.imagetype,pi.receiveddate,pi.handoverdate, pi.pictureid as PictureId, pi.handoverstatus, pi.imageurl, pi.camqcstatus,pi.stockreturncomments AS Comments,pi.photoshootstatus,pi.samplestatus,pi.stockreturn, pi.statusdate, pi.camqcl1rejection, pi.divisionid AS DivisionId, pi.qcaction, pi.rejecteditemsubmit, pi.rejectedcount ,s.name as studioName,s.city as StudioLocation,s.totalcapacity as StudioCapacity,TO_CHAR(p.allocationdate, 'DD/MM/YY') AS StudioAllocationDate, " +
                //    $"pi.qualitycheck,pi.camqcl2submit,pi.camqcl1action,pi.camqcl1qualitycheck,pi.productreceived,pi.rejectedby,pi.datecreated,pi.productsubmitdate,pi.rejecteditemsubmitdate FROM productimage AS pi LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode LEFT JOIN productstudio as p on p.productid =pi.productid LEFT JOIN studio as s on s.id =p.studioid where rejectedby is null  or rejectedby is not null and rejectedreason is null or rejectedreason IS NOT NULL or grnstatus = '1' order by pi.datemodified desc "); // where qcaction IS NULL order by id desc ");
                var query = await connection.QueryAsync<ReceivedAndHandoverStatus>($"SELECT pi.id, MAX(pi.datemodified) AS datemodified, MAX(pi.itemcode) AS itemcode,MAX(ics.eancode) AS eancode,MAX(pi.imagetype) AS imagetype, " +
                                                                                    $"MAX(pi.receiveddate) AS receiveddate,MAX(pi.handoverdate) AS handoverdate,MAX(pi.pictureid) AS PictureId,MAX(pi.handoverstatus) AS handoverstatus, " +
                                                                                    $"MAX(pi.imageurl) AS imageurl,MAX(pi.camqcstatus) AS camqcstatus,MAX(pi.stockreturncomments) AS Comments,MAX(pi.photoshootstatus) AS photoshootstatus, " +
                                                                                    $"MAX(pi.samplestatus) AS samplestatus,MAX(pi.stockreturn) AS stockreturn,MAX(pi.statusdate) AS statusdate,MAX(pi.camqcl1rejection) AS camqcl1rejection, " +
                                                                                    $"MAX(pi.divisionid) AS DivisionId,MAX(pi.qcaction) AS qcaction,MAX(pi.rejecteditemsubmit) AS rejecteditemsubmit,MAX(pi.rejectedcount) AS rejectedcount, " +
                                                                                    $"MAX(s.name) AS studioName,MAX(s.city) AS StudioLocation,MAX(s.totalcapacity) AS StudioCapacity,MAX(TO_CHAR(p.allocationdate, 'DD/MM/YY')) AS StudioAllocationDate, " +
                                                                                    $"MAX(pi.qualitycheck) AS qualitycheck,MAX(pi.camqcl2submit) AS camqcl2submit,MAX(pi.camqcl1action) AS camqcl1action,MAX(pi.camqcl1qualitycheck) AS camqcl1qualitycheck, " +
                                                                                    $"MAX(pi.productreceived) AS productreceived,MAX(pi.rejectedby) AS rejectedby,MAX(pi.datecreated) AS datecreated,MAX(pi.productsubmitdate) AS productsubmitdate,MAX(pi.rejecteditemsubmitdate) AS rejecteditemsubmitdate FROM productimage AS pi " +
                                                                                    $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                                                                                    $"LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
                                                                                    $"LEFT JOIN productstudio as p on p.productid = pi.productid " +
                                                                                    $"LEFT JOIN studio as s on s.id = p.studioid " +
                                                                                    $"WHERE rejectedby IS NULL OR rejectedby IS NOT NULL AND rejectedreason IS NULL OR rejectedreason IS NOT NULL OR grnstatus = '1' " +
                                                                                    $"GROUP BY pi.id ORDER BY CASE WHEN pi.qcaction IS NULL THEN 0 ELSE 1 END, CASE WHEN pi.allaction IS NULL THEN 0 ELSE 1 END, MAX(pi.id) DESC"); // where qcaction IS NULL order by id desc ");
                var results = query.ToList();

                foreach (var result in results)
                {
                    switch (result.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            result.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            result.HandoverStatus = string.Empty;
                            break;
                    }

                    switch (result.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            result.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            result.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            result.PhotoShootStatus = null;
                            break;
                    }

                    switch (result.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("select name from mtrcamqcstatus where id = 1");
                            result.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("select name from mtrcamqcstatus where id = 2");
                            result.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            result.CamQCStatus = string.Empty;
                            break;
                    }

                    if (result.Qcaction == null)
                    {
                        result.ProductStatus = "WIP";
                    }
                    else if (result.Rejectedcount != null && result.Qcaction == null)
                    {
                        result.ProductStatus = "Submitted";
                    }
                    else if (result.Qcaction == "true")
                    {
                        result.ProductStatus = "Submitted";
                    }
                    else
                    {
                        result.ProductStatus = null;
                    }
                }

                // var r = results.OrderByDescending(r => r.ProductStatus)
                // .ThenByDescending(r =>
                // {
                //    if (r.ProductSubmitDate.HasValue && r.RejectedItemSubmitDate.HasValue)
                //    {
                //        return r.ProductSubmitDate > r.RejectedItemSubmitDate ? r.ProductSubmitDate.Value : r.RejectedItemSubmitDate.Value;
                //    }
                //    else if (r.ProductSubmitDate.HasValue)
                //    {
                //        return r.ProductSubmitDate.Value;
                //    }
                //    else if (r.RejectedItemSubmitDate.HasValue)
                //    {
                //        return r.RejectedItemSubmitDate.Value;
                //    }
                //    else
                //    {
                //        return DateTime.MinValue;
                //    }
                // })
                //        .ThenByDescending(r => r.Id)
                //        .ToList();
                return results;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<byte[]> QCExportExcel(ReceivedProductsModel receivedProductsModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedProductsModel.ReceivedFromDate ?? DateTime.MinValue;
                var endDates = receivedProductsModel.ReceivedToDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd");
                var handoverstartDate = receivedProductsModel.HandOverFromDate ?? DateTime.MinValue;
                var handoverendDate = receivedProductsModel.HandOverToDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new List<QCExportModel>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "ImageURL" };
                var basequery = new StringBuilder("SELECT DISTINCT p.itemcode,p.id, ics.eancode, p.imagetype AS SampleType,  TO_CHAR(p.receiveddate, 'DD/MM/YY') AS ReceivedDate, TO_CHAR(p.statusdate, 'DD/MM/YY') AS HandOverDate, p.samplestatus AS SampleStatus, p.productsubmitdate , p.statusdate, p.handoverdate, p.handoverstatus, p.camqcstatus as SampleReceivedByCamQC,p.camqcl1rejection,p.samplestatusrejection,d.name as Division, p.imageurl As ImageURL FROM productimage p INNER JOIN division d ON p.divisionid = d.id  LEFT JOIN itemcodestaging AS ics ON p.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode WHERE pro.isactive = true ");
                //// if (receivedProductsModel.Status == "Approved")
                // {
                //    basequery.Append($" WHERE p.samplestatus = 'Approved' ");
                // }
                // else if (receivedProductsModel.Status == "Rejected")
                // {
                //    basequery.Append($" WHERE p.samplestatus = 'Rejected' or p.samplestatus IS NULL ");
                //// }

                if (receivedProductsModel.Division.Any())
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.Division.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.name IN {formattedDivisions} ");
                }

                if (receivedProductsModel.UserDivision.Any() && receivedProductsModel.Division == string.Empty)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.UserDivision.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.code IN {formattedDivisions} ");
                }

                if (receivedProductsModel.ReceivedFromDate != null || receivedProductsModel.ReceivedToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                }

                if (receivedProductsModel.HandOverFromDate != null || receivedProductsModel.HandOverToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(p.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                }

                if (receivedProductsModel.ReceivedFromDate == null && receivedProductsModel.ReceivedToDate == null && receivedProductsModel.HandOverFromDate == null && receivedProductsModel.HandOverToDate == null && receivedProductsModel.Status == null)
                {
                    basequery.Append(" And (p.handoverstatus IS NOT NULL ) and  samplestatus is null and  p.qcaction='true' order by p.id desc");
                }
                else
                {
                    basequery.Append(" And (p.handoverstatus IS NOT NULL ) and  samplestatus is null and  p.qcaction='true' order by p.id desc");
                }

                var result = await connection.QueryAsync<QCExportModel>(basequery.ToString());
                var data = result.OrderByDescending(r => r.ProductSubmitDate).ToList();

                // foreach (var item in data)
                // {
                //    switch (item.SampleStatus)
                //    {
                //        case "Approved":
                //            item.HandOverToBusinessStatus = "Sent";
                //            break;

                // case "Rejected":
                //            item.HandOverToBusinessStatus = "Not Sent";
                //            break;
                //        case null:
                //        case "null":
                //            item.HandOverToBusinessStatus = "Not Sent";
                //            break;
                //    }

                // switch (item.SampleReceivedByCamQC)
                //    {
                //        case "1":
                //            item.SampleReceivedByCamQC = "Pending";
                //            break;

                // case "2":
                //            item.SampleReceivedByCamQC = "Received";
                //            break;
                //        case null:
                //        case "null":
                //            item.SampleReceivedByCamQC = string.Empty;
                //            break;
                //    }
                //// }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("QCExportExcel");

                var headers = cols;
                var headerBackgroundColor = ColorTranslator.FromHtml("#5787C5");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = ColorTranslator.FromHtml("#5787C5");

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerBackgroundColor);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(borderColor);
                    cell.Style.Border.Left.Color.SetColor(borderColor);
                    cell.Style.Border.Right.Color.SetColor(borderColor);
                    cell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                var row = 2;
                foreach (var item in result)
                {
                    worksheet.Cells[row, 1].Value = item.ItemCode.ToString();
                    worksheet.Cells[row, 2].Value = item.EANCode;
                    worksheet.Cells[row, 3].Value = item.Division;
                    worksheet.Cells[row, 4].Value = item.SampleType;
                    worksheet.Cells[row, 5].Value = item.ReceivedDate;
                    worksheet.Cells[row, 6].Value = item.ImageURL;

                    for (int col = 1; col <= headers.Length; col++)
                    {
                        var dataCell = worksheet.Cells[row, col];
                        dataCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        dataCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Top.Color.SetColor(borderColor);
                        dataCell.Style.Border.Left.Color.SetColor(borderColor);
                        dataCell.Style.Border.Right.Color.SetColor(borderColor);
                        dataCell.Style.Border.Bottom.Color.SetColor(borderColor);
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 20;
                worksheet.Column(3).Width = 20;
                worksheet.Column(4).Width = 20;
                worksheet.Column(5).Width = 20;
                worksheet.Column(6).Width = 20;

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetProductImage(string? status)
        {
            try
            {
                using var connection = this.GetConnection();
                var productImage = string.Empty;
                if (status == null)
                {
                    productImage += "SELECT p.imagetype,p.receiveddate,p.handoverdate,p.imageurl,h.name as handoverstatus,ph.name as photoshootstatus,qc.name as camqcstatus,pt.itemcode, pt.eancode FROM productimage p " +
                                    "JOIN mtrhandoverstatus h ON p.handoverstatus = h.id " +
                                    "JOIN mtrphotoshootstatus ph ON p.photoshootstatus = ph.id " +
                                    "JOIN mtrcamqcstatus qc ON p.camqcstatus = qc.id " +
                                    "JOIN photoshootproduct pt ON p.productid = pt.id";
                }
                else
                {
                    productImage += $"SELECT p.imagetype, p.receiveddate, p.handoverdate, p.imageurl, h.name AS handoverstatus, ph.name AS photoshootstatus, qc.name AS camqcstatus,pt.itemcode,pt.eancode " +
                                    $"FROM productimage p " +
                                    $"JOIN mtrhandoverstatus h ON p.handoverstatus = h.id " +
                                    $"JOIN mtrphotoshootstatus ph ON p.photoshootstatus = ph.id " +
                                    $"JOIN mtrcamqcstatus qc ON p.camqcstatus = qc.id " +
                                    $"JOIN photoshootproduct pt ON p.productid = pt.id " +
                                    $"WHERE lower(h.name) = lower('{status}')";
                }

                var productList = connection.Query<ReceivedAndHandoverStatus>(productImage);
                return productList.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetReceivedDate(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = toDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd");
                var query = $"SELECT id, itemcode, eancode, imagetype, receiveddate, handoverdate, handoverstatus, imageurl, camqcstatus, photoshootstatus, samplestatus, statusdate " +
               $"FROM productimage " +
               $"WHERE to_char(receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' " +
               $"ORDER BY id DESC";
                var receiveddate = await connection.QueryAsync<ReceivedAndHandoverStatus>(query);
                var result = receiveddate.ToList();

                foreach (var data in result)
                {
                    switch (data.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            data.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            data.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            data.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (data.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            data.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = photshootquery4.Name;
                            break;
                    }

                    switch (data.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            data.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 2");
                            data.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            var camqc4 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            data.CamQCStatus = camqc4.Name;
                            break;
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetHandOverDateRange(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = toDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd");
                var query = $"SELECT id, itemcode, eancode, imagetype, receiveddate, handoverdate, handoverstatus, imageurl, camqcstatus, photoshootstatus, samplestatus, statusdate " +
            $"FROM productimage " +
            $"WHERE to_char(handoverdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' " +
            $"ORDER BY id DESC";
                var handoverdate = await connection.QueryAsync<ReceivedAndHandoverStatus>(query);
                var result = handoverdate.ToList();
                foreach (var data in result)
                {
                    switch (data.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            data.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            data.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            data.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (data.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            data.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = photshootquery4.Name;
                            break;
                    }

                    switch (data.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            data.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 2");
                            data.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            var camqc4 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            data.CamQCStatus = camqc4.Name;
                            break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetAllFilters(string? status, string? pSstatus, string? camPstatus, string? camRstatus, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedfromDate ?? DateTime.MinValue;
                var endDates = receivedtoDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd");
                var handoverstartDate = handOverfromDate ?? DateTime.MinValue;
                var handoverendDate = handOvertoDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new StringBuilder("SELECT DISTINCT p.id,p.itemcode,ics.eancode,p.imagetype,p.receiveddate,p.handoverdate, p.handoverstatus, p.imageurl, p.camqcstatus,p.stockreturncomments AS Comments,p.photoshootstatus,p.samplestatus,p.stockreturn, p.statusdate, p.camqcl1rejection, p.divisionid AS DivisionId, p.qcaction, p.rejecteditemsubmit, p.rejectedcount ,s.name as studioName,s.city as StudioLocation,s.totalcapacity as StudioCapacity,TO_CHAR(p.allocationdate, 'DD/MM/YY') AS StudioAllocationDate," +
                    "p.qualitycheck,p.camqcl2submit,p.camqcl1action,p.camqcl1qualitycheck,p.productreceived,p.rejectedby,p.datecreated,p.productsubmitdate,p.rejecteditemsubmitdate  FROM productimage AS p LEFT JOIN itemcodestaging AS ics ON p.itemcode = ics.code LEFT JOIN productstudio as ps on p.productid =ps.productid LEFT JOIN studio as s on s.id =ps.studioid ");

                if (status == "Approved")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" ((p.samplestatus = '2' AND p.qcaction IS NOT NULL) OR (p.samplestatus = 'Approved' AND p.qcaction IS NOT NULL) )  ");

                    // query.Append($" (p.samplestatus = '2' AND p.qcaction IS NOT NULL ) ");
                }
                else if (status == "Rejected")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.samplestatus = '1' ");
                }
                else if (status == "Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" (p.samplestatus IS NULL and p.qcaction = 'true') ");
                }
                else if (status == "Pending, Approved" || status == "Approved, Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append("((p.samplestatus = '2' AND p.qcaction IS NOT NULL) OR ");
                    query.Append("(p.samplestatus = 'Approved' AND p.qcaction IS NOT NULL) OR ");
                    query.Append("(p.samplestatus IS NULL AND p.qcaction = 'true')) ");
                }
                else if (status == "Pending, Rejected" || status == "Rejected, Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append("(p.samplestatus = '1' OR ");
                    query.Append("(p.samplestatus IS NULL AND p.qcaction = 'true')) ");
                }
                else if (status == "Approved, Rejected" || status == "Rejected, Approved")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append("(p.samplestatus = '1' OR ");
                    query.Append("(p.samplestatus = '2' AND p.qcaction IS NOT NULL) OR ");
                    query.Append("(p.samplestatus = 'Approved' AND p.qcaction IS NOT NULL)) ");
                }
                else if (status == "Rejected, Approved, Pending" || status == "Rejected, Pending, Approved" ||
         status == "Approved, Rejected, Pending" || status == "Approved, Pending, Rejected" ||
         status == "Pending, Approved, Rejected" || status == "Pending, Rejected, Approved")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append("(");
                    query.Append("p.samplestatus = '1' OR ");
                    query.Append("(p.samplestatus = '2' AND p.qcaction IS NOT NULL) OR ");
                    query.Append("(p.samplestatus IS NULL AND p.qcaction = 'true') OR ");
                    query.Append("(p.samplestatus = 'Approved' AND p.qcaction IS NOT NULL)");
                    query.Append(") ");
                }
                else if (status == "WIP")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" (p.qcaction IS NULL ) ");
                }
                else if (status == "Submitted")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" (p.qcaction = 'true') ");
                }

                if (pSstatus == "Completed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"  (p.photoshootstatus = '2') ");
                }
                else if (pSstatus == "Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" (p.photoshootstatus = '1') ");
                }
                else if (pSstatus == "Closed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.camqcl1action = '1' AND p.camqcl1qualitycheck is null or p.samplestatus = '1' or p.productreceived = '1' ");
                }
                else if (pSstatus == "Completed, Pending" || pSstatus == "Pending, Completed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.photoshootstatus = '2' OR p.photoshootstatus = '1' ");
                }
                else if (pSstatus == "Completed, Closed" || pSstatus == "Closed, Completed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.photoshootstatus = '2' OR  p.camqcl1action = '1' AND p.camqcl1qualitycheck is null or p.samplestatus = '1' or p.productreceived = '1' ");
                }
                else if (pSstatus == "Pending, Closed" || pSstatus == "Closed, Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.photoshootstatus = '1' OR p.camqcl1action = '1' AND p.camqcl1qualitycheck is null or p.samplestatus = '1' or p.productreceived = '1' ");
                }
                else if (pSstatus == "Pending, Closed, Completed" || pSstatus == "Closed, Completed, Pending" || pSstatus == "Pending, Completed, Closed" || pSstatus == "Completed, Pending, Closed" || pSstatus == "Closed, Pending, Completed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.photoshootstatus = '2' OR p.photoshootstatus = '1' OR p.camqcl1action = '1' AND p.camqcl1qualitycheck is null or p.samplestatus = '1' or p.productreceived = '1' ");
                }

                if (camPstatus == "Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"  (p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' or p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1') ");
                }
                else if (camPstatus == "Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL ");
                }
                else if (camPstatus == "Rejected")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.camqcl1action = '1' and p.productreceived IS NULL ");
                }
                else if (camPstatus == "Closed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.samplestatus = '1' and p.camqcl1action is null  ");
                }
                else if (camPstatus == "Approved, Rejected" || camPstatus == "Rejected, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.camqcl1action = '1' and p.productreceived IS NULL ) ");
                }
                else if (camPstatus == "Approved, Pending" || camPstatus == "Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL ) ");
                }
                else if (camPstatus == "Rejected, Pending" || camPstatus == "Pending, Rejected")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"( p.camqcl1action = '1' and p.productreceived IS NULL OR p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL ) ");
                }
                else if (camPstatus == "Approved, Closed" || camPstatus == "Closed, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.samplestatus = '1' and p.camqcl1action is null ) ");
                }
                else if (camPstatus == "Pending, Closed" || camPstatus == "Closed, Pending")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"( p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL OR p.samplestatus = '1' and p.camqcl1action is null ) ");
                }
                else if (camPstatus == "Rejected, Closed" || camPstatus == "Closed, Rejected")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"( p.camqcl1action = '1' and p.productreceived IS NULL OR p.samplestatus = '1' and p.camqcl1action is null ) ");
                }
                else if (camPstatus == "Pending, Approved, Rejected" || camPstatus == "Pending, Rejected, Approved" || camPstatus == "Approved, Pending, Rejected" || camPstatus == "Approved, Rejected, Pending" || camPstatus == "Rejected, Approved, Pending" || camPstatus == "Rejected, Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL OR p.camqcl1action = '1' and p.productreceived IS NULL ) ");
                }
                else if (camPstatus == "Closed, Approved, Rejected" || camPstatus == "Closed, Rejected, Approved" || camPstatus == "Approved, Closed, Rejected" || camPstatus == "Approved, Rejected, Closed" || camPstatus == "Rejected, Approved, Closed" || camPstatus == "Rejected, Closed, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.samplestatus = '1' and p.camqcl1action is null and p.camqcl1qualitycheck IS NULL OR p.camqcl1action = '1' and p.productreceived IS NULL ) ");
                }
                else if (camPstatus == "Closed, Pending, Rejected" || camPstatus == "Closed, Rejected, Pending" || camPstatus == "Pending, Closed, Rejected" || camPstatus == "Pending, Rejected, Closed" || camPstatus == "Rejected, Pending, Closed" || camPstatus == "Rejected, Closed, Pending")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL OR p.samplestatus = '1' and p.camqcl1action is null and p.camqcl1qualitycheck IS NULL OR p.camqcl1action = '1' and p.productreceived IS NULL ) ");
                }
                else if (camPstatus == "Pending, Approved, Closed" || camPstatus == "Pending, Closed, Approved" || camPstatus == "Approved, Pending, Closed" || camPstatus == "Approved, Closed, Pending" || camPstatus == "Closed, Approved, Pending" || camPstatus == "Closed, Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL OR p.samplestatus = '1' and p.camqcl1action is null ) ");
                }
                else if (camPstatus == "Approved, Pending, Rejected, Closed" || camPstatus == "Approved, Pending, Closed, Rejected" || camPstatus == "Approved, Closed, Rejected, Pending"
                    || camPstatus == "Approved, Closed, Pending, Rejected" || camPstatus == "Pending, Approved, Rejected, Closed" || camPstatus == "Closed, Rejected, Approved, Pending")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.camqcl1qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.camqcl1qualitycheck is null and p.rejectedby is not null and p.productreceived='1' OR p.camqcl1action = 'Received' and p.camqcl1qualitycheck IS NULL OR p.camqcl1action = '1' and p.productreceived IS NULL OR p.samplestatus = '1' and p.camqcl1action is null ) ");
                }

                if (camRstatus == "Approved")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"  (p.qualitycheck = 'Pass' ) ");
                }
                else if (camRstatus == "Pending")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL ");
                }
                else if (camRstatus == "Rejected")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.qualitycheck = '1' ");
                }
                else if (camRstatus == "Closed")
                {
                    if (query.ToString().Contains("WHERE "))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true'  ");
                }
                else if (camRstatus == "Approved, Rejected" || camRstatus == "Rejected, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.qualitycheck = 'Pass' OR p.qualitycheck = '1' ) ");
                }
                else if (camRstatus == "Approved, Pending" || camRstatus == "Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.qualitycheck = 'Pass' OR p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL ) ");
                }
                else if (camRstatus == "Rejected, Pending" || camRstatus == "Pending, Rejected")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.qualitycheck = '1' OR p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL ) ");
                }
                else if (camRstatus == "Approved, Closed" || camRstatus == "Closed, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.qualitycheck = 'Pass' OR  p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' ) ");
                }
                else if (camRstatus == "Pending, Closed" || camRstatus == "Closed, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL OR  p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' ) ");
                }
                else if (camRstatus == "Rejected, Closed" || camRstatus == "Closed, Rejected")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.qualitycheck = '1' OR  p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' ) ");
                }
                else if (camRstatus == "Pending, Approved, Rejected" || camRstatus == "Pending, Rejected, Approved" || camRstatus == "Approved, Pending, Rejected" || camRstatus == "Approved, Rejected, Pending" || camRstatus == "Rejected, Approved, Pending" || camRstatus == "Rejected, Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL OR p.qualitycheck = 'Pass' OR p.qualitycheck = '1' ) ");
                }
                else if (camRstatus == "Pending, Closed, Rejected" || camRstatus == "Pending, Rejected, Closed" || camRstatus == "Closed, Pending, Rejected" || camRstatus == "Closed, Rejected, Pending" || camRstatus == "Rejected, Closed, Pending" || camRstatus == "Rejected, Pending, Closed")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL OR  p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' OR p.qualitycheck = '1' ) ");
                }
                else if (camRstatus == "Closed, Approved, Rejected" || camRstatus == "Closed, Rejected, Approved" || camRstatus == "Approved, Closed, Rejected" || camRstatus == "Approved, Rejected, Closed" || camRstatus == "Rejected, Approved, Closed" || camRstatus == "Rejected, Closed, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' OR p.qualitycheck = 'Pass' OR p.qualitycheck = '1' ) ");
                }
                else if (camRstatus == "Pending, Approved, Closed" || camRstatus == "Pending, Closed, Approved" || camRstatus == "Approved, Pending, Closed" || camRstatus == "Approved, Closed, Pending" || camRstatus == "Closed, Approved, Pending" || camRstatus == "Closed, Pending, Approved")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL OR p.qualitycheck = 'Pass' OR p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true' ) ");
                }
                else if (camRstatus == "Approved, Pending, Rejected, Closed" || camRstatus == "Approved, Pending, Closed, Rejected" || camRstatus == "Approved, Closed, Rejected, Pending"
                    || camRstatus == "Approved, Closed, Pending, Rejected" || camRstatus == "Pending, Approved, Rejected, Closed" || camRstatus == "Closed, Rejected, Approved, Pending")
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"(p.photoshootstatus = 2 and p.camqcl2action IS NULL and p.qualitycheck IS NULL OR p.qualitycheck = 'Pass' OR p.qualitycheck = '1' OR p.samplestatus = '1' or p.camqcl1action = '1' and p.qcaction = 'true') ");
                }

                if (!string.IsNullOrEmpty(division) && division != "0")
                {
                    string[] divisions = division.Split(',');
                    division = string.Join(", ", divisions.Select(d => d.Trim()));
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" p.divisionid in ({division}) ");
                }

                if (receivedfromDate != null || receivedtoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" to_char(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                }

                if (handOverfromDate != null || handOvertoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($" to_char(p.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                }

                query.Append(" ORDER BY p.id DESC");

                var result = await connection.QueryAsync<ReceivedAndHandoverStatus>(query.ToString());
                var data = result.ToList();

                foreach (var item in data)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            item.PhotoShootStatus = string.Empty;
                            break;
                    }

                    switch (item.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            item.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 2");
                            item.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            item.CamQCStatus = string.Empty;
                            break;
                    }

                    switch (item.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            item.HandoverStatus = string.Empty;
                            break;
                    }

                    switch (item.SampleStatus)
                    {
                        case "Approved":
                            var sample1 = "Sent";
                            break;

                        case "Rejected":
                            var sample2 = "Not Sent";
                            break;
                        case null:
                        case "null":
                            var sample3 = "Not Sent";
                            break;
                    }

                    if (item.Qcaction == null)
                    {
                        item.ProductStatus = "WIP";
                    }
                    else if (item.Rejectedcount != null && item.Qcaction == null)
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else if (item.Qcaction == "true")
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else
                    {
                        item.ProductStatus = null;
                    }
                }

                return data.OrderByDescending(r => r.ProductStatus)
                    .ThenByDescending(r =>
                    {
                        if (r.ProductSubmitDate.HasValue && r.RejectedItemSubmitDate.HasValue)
                        {
                            return r.ProductSubmitDate > r.RejectedItemSubmitDate ? r.ProductSubmitDate.Value : r.RejectedItemSubmitDate.Value;
                        }
                        else if (r.ProductSubmitDate.HasValue)
                        {
                            return r.ProductSubmitDate.Value;
                        }
                        else if (r.RejectedItemSubmitDate.HasValue)
                        {
                            return r.RejectedItemSubmitDate.Value;
                        }
                        else
                        {
                            return DateTime.MinValue;
                        }
                    }).ThenByDescending(r => r.DateCreated).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetrejectedFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedfromDate ?? DateTime.MinValue;
                var endDates = receivedtoDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
                var handoverstartDate = handOverfromDate ?? DateTime.MinValue;
                var handoverendDate = handOvertoDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new StringBuilder("SELECT p.itemcode,(select eancode from itemcodestaging where code=p.itemcode) as EanCode, p.imagetype, p.divisionid, p.receiveddate, p.rejecteddate, p.statusdate as HandOverDate, p.productsubmitdate, p.rejecteditemsubmitdate, p.imageurl, p.samplestatus as SampleStatus, p.handoverstatus as HandoverStatus, camqcl1action as CamQCStatus, p.camqcl1rejection, p.photoshootstatus,p.statusdate, p.statusdate, p.stockreturn,stockreturncomments as Comments, p.rejectedby, p.rejectedreason, p.rejectedcount FROM productimage p  ");

                if (status == "Approved")
                {
                    query.Append($" WHERE p.handoverstatus = 1 ");
                }
                else if (status == "Rejected")
                {
                    query.Append($" WHERE p.handoverstatus = 2 or p.handoverstatus is null ");
                }

                if (receivedfromDate != null || receivedtoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"to_char(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd HH24:MI:SS'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                }

                if (handOverfromDate != null || handOvertoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    query.Append($"to_char(p.handoverdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                }

                if (status == null && receivedfromDate == null && receivedtoDate == null && handOverfromDate == null && handOvertoDate == null)
                {
                    query.Append(" WHERE p.rejectedby is not null and p.rejectedreason is not null ORDER BY p.id DESC");
                }
                else
                {
                    query.Append(" and p.rejectedby is not null and p.rejectedreason is not null ORDER BY p.id DESC");
                }

                var result = await connection.QueryAsync<ReceivedAndHandoverStatus>(query.ToString());
                var data = result.ToList();

                foreach (var item in data)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                            break;
                            ////case "null":
                            ////    var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            ////    item.PhotoShootStatus = photshootquery4.Name;
                    }

                    switch (item.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            item.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 2");
                            item.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            var camqc4 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            item.CamQCStatus = camqc4.Name;
                            break;
                    }

                    switch (item.HandoverStatus)
                    {
                        case "1":
                            var handover1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverStatus = handover1.Name;
                            break;

                        case "2":
                            var handover2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = handover2.Name;
                            break;
                    }
                }

                return data.OrderBy(r =>
                {
                    var samplestatus = string.Empty;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus;
                }).ThenByDescending(r =>
                {
                    var samplestatus = string.Empty;
                    var lastRejectedDate = DateTime.MinValue;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                        var rejectedDate = r.Rejecteddate;
                        string[] dates = rejectedDate.Split(',');
                        string lastDateString = dates[dates.Length - 1].Trim();
                        lastRejectedDate = DateTime.ParseExact(lastDateString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus == "Rejected" ? lastRejectedDate : r.ProductSubmitDate;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetAllQCFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedfromDate ?? DateTime.MinValue;
                var endDates = receivedtoDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd");
                var handoverstartDate = handOverfromDate ?? DateTime.MinValue;
                var handoverendDate = handOvertoDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new StringBuilder("SELECT p.itemcode, (select eancode from itemcodestaging where code=p.itemcode) as EanCode, p.imagetype, p.receiveddate, p.samplestatus, p.productsubmitdate, p.statusdate, p.handoverdate, p.handoverstatus, p.camqcstatus as CamQCStatus,p.camqcl1rejection,p.divisionid as DivisionId,p.samplestatusrejection FROM productimage p ");

                if (status == "Approved")
                {
                    query.Append($" WHERE p.samplestatus = 'Approved' ");
                }
                else if (status == "Rejected")
                {
                    query.Append($" WHERE p.samplestatus = 'Rejected' or p.handoverstatus = 2 ");
                }

                if (receivedfromDate != null || receivedtoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    if (query.ToString().Contains("qcaction"))
                    {
                        query.Append($"to_char(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                    }
                    else
                    {
                        query.Append($"to_char(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                    }
                }

                if (handOverfromDate != null || handOvertoDate != null)
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append("AND ");
                    }
                    else
                    {
                        query.Append("WHERE ");
                    }

                    if (query.ToString().Contains("qcaction"))
                    {
                        query.Append($" to_char(p.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                    }
                    else
                    {
                        query.Append($" to_char(p.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                    }
                }

                if (!string.IsNullOrEmpty(division))
                {
                    if (query.ToString().Contains("WHERE"))
                    {
                        query.Append(" AND ");
                    }
                    else
                    {
                        query.Append(" WHERE ");
                    }

                    string[] divisions = division.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    query.Append($" p.divisionid IN {formattedDivisions} ");
                }

                if (query.ToString().Contains("WHERE"))
                {
                    query.Append(" And (p.handoverstatus IS NOT NULL or rejecteditemsubmit is not null) and  samplestatus is null ORDER BY p.id DESC");
                }
                else
                {
                    query.Append("where (p.handoverstatus IS NOT NULL or rejecteditemsubmit is not null) and  samplestatus is null ORDER BY p.id DESC");
                }

                var result = await connection.QueryAsync<ReceivedAndHandoverStatus>(query.ToString());
                var data = result.ToList();

                foreach (var item in data)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery4.Name;
                            break;
                    }

                    switch (item.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 1");
                            item.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("SELECT name FROM mtrcamqcstatus WHERE id = 2");
                            item.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            item.CamQCStatus = string.Empty;
                            break;
                    }

                    switch (item.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (item.SampleStatus)
                    {
                        case "Approved":
                            var sample1 = "Sent";
                            break;
                        case "Rejected":
                            var sample2 = "Not Sent";
                            break;
                        case null:
                        case "null":
                            var sample3 = "Not Sent";
                            break;
                    }
                }

                return data.OrderByDescending(r => r.ProductSubmitDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<byte[]> GetAllFitersExportExcel(ReceivedProductsModel receivedProductsModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedProductsModel.ReceivedFromDate ?? DateTime.MinValue;
                var endDates = receivedProductsModel.ReceivedToDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd");
                var handoverstartDate = receivedProductsModel.HandOverFromDate ?? DateTime.MinValue;
                var handoverendDate = receivedProductsModel.HandOverToDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new List<GetAllFitersModel>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "ImageURL", "ProductStatus", "QCStatus", "QCApprovedDate", "CAMQCL1ACKStatus", "CAMQCL1Status", "CAMQCL1SubmissionStatus", "PhotoshootActionStatus", "PhotoShootStatus", "CAMQCL2Status", "CAMQCL2SubmissionStatus", "StockReturnToQC" };

                // var basequery = new StringBuilder("SELECT DISTINCT p.id, p.itemcode, ics.eancode, p.imagetype AS Type, TO_CHAR(p.receiveddate, 'DD/MM/YY') AS ReceivedDate, TO_CHAR(p.statusdate, 'DD/MM/YY') AS HandOverDate, p.statusdate, p.imageurl, p.handoverstatus As HandoverToBusinessStatus, p.camqcstatus AS SampleReceivedByCamQC, p.photoshootstatus,p.stockreturn AS StockReturnToQC, d.name as Division FROM productimage p INNER JOIN division d ON p.divisionid = d.id  LEFT JOIN itemcodestaging AS ics ON p.itemcode = ics.code  ");
                // var basequery = new StringBuilder($"SELECT DISTINCT pi.id,pi.datemodified, pi.itemcode AS ItemCode, ics.eancode AS EANCode, pi.imagetype AS SampleType, TO_CHAR(pi.receiveddate, 'DD/MM/YY') AS ReceivedDate,pi.handoverdate, pi.handoverstatus, pi.imageurl AS ImageURL, " +
                //    $"pi.camqcstatus AS CamQCStatus ,pi.stockreturncomments AS Comments,pi.photoshootstatus AS PhotoshootStatus,pi.samplestatus AS SampleStatus,pi.stockreturn AS StockReturnToQC, TO_CHAR(pi.statusdate, 'DD/MM/YY') AS QCApprovedDate , pi.camqcl1rejection," +
                //    $" pi.divisionid AS DivisionId, pi.qcaction AS QCAction, pi.rejecteditemsubmit AS RejectedItemSubmit, pi.rejectedcount AS RejectedCount, pi.datecreated AS DateCreated, pi.productsubmitdate AS ProductSubmitDate, pi.rejecteditemsubmitdate AS RejectedItemSubmitDate, " +
                //    $"s.name as studioName,s.city as StudioLocation,s.totalcapacity as StudioCapacity,TO_CHAR(p.allocationdate, 'DD/MM/YY') AS StudioAllocationDate, " +
                //    $"pi.qualitycheck AS QualityCheck,pi.camqcl2submit AS CamQcL2Submit,pi.camqcl1action AS CAMQCL1Action ,pi.camqcl1qualitycheck AS CAMQCL1QualityCheck ,pi.productreceived AS ProductReceived,pi.rejectedby, d.name as Division FROM productimage AS pi " +
                //    $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode LEFT JOIN productstudio as p on p.productid =pi.productid LEFT JOIN studio as s on s.id =p.studioid " +
                //    $"LEFT JOIN division as d on d.id =pi.divisionid WHERE pro.isactive = true ");
                var basequery = new StringBuilder($"SELECT MAX(pi.id) AS Id, MAX(pi.datemodified) AS DateModified, MAX(pi.itemcode) AS ItemCode, " +
    $"MAX(ics.eancode) AS EANCode, MAX(pi.imagetype) AS SampleType, TO_CHAR(MAX(pi.receiveddate), 'DD/MM/YY') AS ReceivedDate, " +
    $"MAX(pi.handoverdate) AS HandoverDate, MAX(pi.handoverstatus) AS HandoverStatus, MAX(pi.imageurl) AS ImageURL, " +
    $"MAX(pi.camqcstatus) AS CamQCStatus, MAX(pi.stockreturncomments) AS Comments, MAX(pi.photoshootstatus) AS PhotoshootStatus, " +
    $"MAX(pi.samplestatus) AS SampleStatus, MAX(pi.stockreturn) AS StockReturnToQC, TO_CHAR(MAX(pi.statusdate), 'DD/MM/YY') AS QCApprovedDate, " +
    $"MAX(pi.camqcl1rejection) AS CamQCL1Rejection, MAX(pi.divisionid) AS DivisionId, MAX(pi.qcaction) AS QCAction, " +
    $"MAX(pi.rejecteditemsubmit) AS RejectedItemSubmit, MAX(pi.rejectedcount) AS RejectedCount, MAX(pi.datecreated) AS DateCreated, " +
    $"MAX(pi.productsubmitdate) AS ProductSubmitDate, MAX(pi.rejecteditemsubmitdate) AS RejectedItemSubmitDate, " +
    $"MAX(s.name) AS StudioName, MAX(s.city) AS StudioLocation, MAX(s.totalcapacity) AS StudioCapacity, " +
    $"TO_CHAR(MAX(p.allocationdate), 'DD/MM/YY') AS StudioAllocationDate, MAX(pi.qualitycheck) AS QualityCheck, " +
    $"MAX(pi.camqcl2submit) AS CamQcL2Submit, MAX(pi.camqcl1action) AS CAMQCL1Action, MAX(pi.camqcl1qualitycheck) AS CAMQCL1QualityCheck, " +
    $"MAX(pi.productreceived) AS ProductReceived, MAX(pi.rejectedby) AS RejectedBy, MAX(d.name) AS Division " +
    $"FROM productimage AS pi " +
    $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
    $"LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
    $"LEFT JOIN productstudio AS p ON p.productid = pi.productid " +
    $"LEFT JOIN studio AS s ON s.id = p.studioid " +
    $"LEFT JOIN division AS d ON d.id = pi.divisionid " +
    $"WHERE pro.isactive = true ");

                // if (!string.IsNullOrEmpty(receivedProductsModel.Status))
                // {
                //    var handoverstatus = await connection.QueryFirstOrDefaultAsync<long>($"SELECT id FROM mtrhandoverstatus WHERE name = '{receivedProductsModel.Status}'");

                // if (handoverstatus > 0)
                //    {
                //        basequery.Append("WHERE ");
                //        basequery.Append($"p.handoverstatus = {handoverstatus} ");
                //    }
                // }
                if (receivedProductsModel.Status == "Pending")
                {
                    basequery.Append($" AND (pi.samplestatus IS NULL and pi.qcaction='true')");
                }
                else if (receivedProductsModel.Status == "Approved")
                {
                    basequery.Append($" AND (pi.samplestatus = '2' or pi.samplestatus = 'Approved' ) ");

                    // basequery.Append($" AND (pi.samplestatus = '2' AND p.qcaction IS NOT NULL ) ");
                }
                else if (receivedProductsModel.Status == "Rejected")
                {
                    basequery.Append($" AND  pi.samplestatus='1' ");
                }
                else if (receivedProductsModel.Status == "WIP")
                {
                    basequery.Append($" AND  pi.qcaction IS NULL ");
                }
                else if (receivedProductsModel.Status == "Submitted")
                {
                    basequery.Append($" AND  pi.qcaction = 'true' ");
                }
                else if (receivedProductsModel.Status == "Pending, Rejected" || receivedProductsModel.Status == "Rejected, Pending")
                {
                    if (basequery.ToString().Contains("WHERE "))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append("(pi.samplestatus = '1' OR ");
                    basequery.Append("(pi.samplestatus IS NULL AND pi.qcaction = 'true')) ");
                }
                else if (receivedProductsModel.Status == "Pending, Approved" || receivedProductsModel.Status == "Approved, Pending")
                {
                    if (basequery.ToString().Contains("WHERE "))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append("((pi.samplestatus = '2' AND pi.qcaction IS NOT NULL) OR ");
                    basequery.Append("(pi.samplestatus = 'Approved' AND pi.qcaction IS NOT NULL) OR ");
                    basequery.Append("(pi.samplestatus IS NULL AND pi.qcaction = 'true')) ");
                }
                else if (receivedProductsModel.Status == "Rejected, Approved" || receivedProductsModel.Status == "Approved, Rejected")
                {
                    if (basequery.ToString().Contains("WHERE "))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append("((pi.samplestatus = '1') OR ");
                    basequery.Append("(pi.samplestatus = '2' AND pi.qcaction IS NOT NULL) OR ");
                    basequery.Append("(pi.samplestatus = 'Approved' AND pi.qcaction IS NOT NULL)) ");
                }
                else if (receivedProductsModel.Status == "Rejected, Approved, Pending")
                {
                    if (basequery.ToString().Contains("WHERE "))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append("((pi.samplestatus = '1') OR ");
                    basequery.Append("(pi.samplestatus = '2' AND pi.qcaction IS NOT NULL) OR ");
                    basequery.Append("(pi.samplestatus = 'Approved' AND pi.qcaction IS NOT NULL) OR ");
                    basequery.Append("(pi.samplestatus IS NULL and pi.qcaction='true')) ");
                }

                // else if (receivedProductsModel.Status == "WIP, Submitted" || receivedProductsModel.Status == "Submitted, WIP")
                // { 
                //    basequery.Append($" AND  pi.qcaction = 'true' OR pi.qcaction IS NULL ");
                // }
                if (receivedProductsModel.Division.Any())
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.Division.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.name IN {formattedDivisions} ");
                }

                if (receivedProductsModel.UserDivision.Any() && receivedProductsModel.Division == string.Empty)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.UserDivision.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.code IN {formattedDivisions} ");
                }

                if (receivedProductsModel.ReceivedFromDate != null || receivedProductsModel.ReceivedToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                }

                if (receivedProductsModel.HandOverFromDate != null || receivedProductsModel.HandOverToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(pi.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                }

                // if (receivedProductsModel.ReceivedFromDate == null && receivedProductsModel.ReceivedToDate == null && receivedProductsModel.HandOverFromDate == null && receivedProductsModel.HandOverToDate == null && receivedProductsModel.Status == null)
                // {
                //    basequery.Append(" ORDER BY pi.datemodified DESC");
                // }
                // else
                // {
                //    basequery.Append(" ORDER BY pi.datemodified DESC");
                // }
                if (receivedProductsModel.ReceivedFromDate == null && receivedProductsModel.ReceivedToDate == null && receivedProductsModel.HandOverFromDate == null && receivedProductsModel.HandOverToDate == null && receivedProductsModel.Status == null)
                {
                    basequery.Append(" GROUP BY pi.id, pi.datemodified ORDER BY CASE WHEN pi.qcaction IS NULL THEN 0 ELSE 1 END, CASE WHEN pi.allaction IS NULL THEN 0 ELSE 1 END, MAX(pi.id) DESC");
                }
                else
                {
                    basequery.Append(" GROUP BY pi.id, pi.datemodified ORDER BY CASE WHEN pi.qcaction IS NULL THEN 0 ELSE 1 END, CASE WHEN pi.allaction IS NULL THEN 0 ELSE 1 END, MAX(pi.id) DESC");
                }

                var queryresult = await connection.QueryAsync<GetAllFitersModel>(basequery.ToString());
                var result = queryresult.ToList();

                foreach (var item in result)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            item.PhotoShootStatus = null;
                            break;
                    }

                    switch (item.HandoverToBusinessStatus)
                    {
                        case "1":
                            var handover1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverToBusinessStatus = handover1.Name;
                            break;

                        case "2":
                            var handover2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverToBusinessStatus = handover2.Name;
                            break;
                        case null:
                        case "null":
                            item.HandoverToBusinessStatus = string.Empty;
                            break;
                    }

                    switch (item.SampleReceivedByCamQC)
                    {
                        case "1":
                            item.SampleReceivedByCamQC = "Pending";
                            break;

                        case "2":
                            item.SampleReceivedByCamQC = "Received";
                            break;
                        case null:
                        case "null":
                            item.SampleReceivedByCamQC = string.Empty;
                            break;
                    }

                    switch (item.StockReturnToQC)
                    {
                        case "0":
                            item.StockReturnToQC = "Yet To Return";
                            break;

                        case "1":
                            item.StockReturnToQC = "Returned";
                            break;

                        case "null":
                            item.StockReturnToQC = string.Empty;
                            break;
                    }

                    if (item.QCAction == null)
                    {
                        item.ProductStatus = "WIP";
                    }
                    else if (item.RejectedCount != null && item.QCAction == null)
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else if (item.QCAction == "true")
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else
                    {
                        item.ProductStatus = null;
                    }

                    if (item.SampleStatus == "1")
                    {
                        item.QCStatus = "Rejected";
                    }
                    else if (item.SampleStatus == "2" || item.SampleStatus == "Approved")
                    {
                        item.QCStatus = "Approved";
                    }
                    else if (item.QCAction == "true" && (item.SampleStatus != "Approved" || item.SampleStatus != "2"))
                    {
                        item.QCStatus = "Pending";
                    }
                    else if (item.RejectedItemSubmit == "yes" && (item.SampleStatus != "Approved" || item.SampleStatus != "2"))
                    {
                        item.QCStatus = " ";
                    }
                    else
                    {
                        item.QCStatus = null;
                    }

                    if (item.CamQCStatus == "1" && item.CAMQCL1Action == null)
                    {
                        item.CAMQCL1ACKStatus = "Pending";
                    }
                    else if (item.CAMQCL1Action == "Received")
                    {
                        item.CAMQCL1ACKStatus = "Received";
                    }
                    else if (item.ProductReceived == "1" || item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL1ACKStatus = "Received";
                    }
                    else
                    {
                        item.CAMQCL1ACKStatus = null;
                    }

                    if (item.CAMQCL1Action == "Received" && item.CAMQCL1QualityCheck == null)
                    {
                        item.CAMQCL1Status = "Pending";
                        if (item.ProductReceived == "1")
                        {
                            item.CAMQCL1Status = "Approved";
                        }
                    }
                    else if (item.CAMQCL1QualityCheck == "Pass")
                    {
                        item.CAMQCL1Status = "Approved";
                    }
                    else if (item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL1Status = "Rejected";
                    }
                    else
                    {
                        item.CAMQCL1Status = null;
                    }

                    if (item.CAMQCL1QualityCheck == "Pass" && item.CamQCStatus != "2")
                    {
                        item.CAMQCL1SubmissionStatus = "WIP";
                    }
                    else if (item.CamQCStatus == "2")
                    {
                        item.CAMQCL1SubmissionStatus = "Submitted";
                    }
                    else if (item.ProductReceived == "1")
                    {
                        item.CAMQCL1SubmissionStatus = "Submitted";
                    }
                    else
                    {
                        item.CAMQCL1SubmissionStatus = null;
                    }

                    if (item.ProductReceived == null && item.CamQCStatus == "2")
                    {
                        item.PhotoshootActionStatus = "Pending";
                    }
                    else if (item.ProductReceived == "Received")
                    {
                        item.PhotoshootActionStatus = "Approved";
                    }
                    else if (item.ProductReceived == "1")
                    {
                        item.PhotoshootActionStatus = "Rejected";
                    }
                    else
                    {
                        item.PhotoshootActionStatus = null;
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved" || item.SampleStatus == "1") && item.CAMQCL1Action == "1" && item.ProductReceived == "1")
                    {
                        item.PhotoShootStatus = "Closed";
                    }

                    if (item.PhotoShootStatus == "Completed" && item.QualityCheck == null)
                    {
                        item.CAMQCL2Status = "Pending";
                    }
                    else if (item.QualityCheck == "Pass")
                    {
                        item.CAMQCL2Status = "Approved";
                    }
                    else if (item.QualityCheck == "1")
                    {
                        item.CAMQCL2Status = "Rejected";
                    }
                    else
                    {
                        item.CAMQCL2Status = null;
                    }

                    if (item.QualityCheck == "Pass" && item.CamQcL2Submit == null)
                    {
                        item.CAMQCL2SubmissionStatus = "WIP";
                    }
                    else if (item.CamQcL2Submit == "1")
                    {
                        item.CAMQCL2SubmissionStatus = "Submitted";
                    }
                    else
                    {
                        item.CAMQCL2SubmissionStatus = null;
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") &&
                      item.CAMQCL1Action == "1" &&
                      item.ProductReceived == "1")
                    {
                        item.CAMQCL2SubmissionStatus = "Closed";
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") && item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL2SubmissionStatus = "Closed";
                    }

                    if (item.SampleStatus == "1")
                    {
                        item.CAMQCL2SubmissionStatus = "Closed";
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") &&
                      item.CAMQCL1Action == "1" &&
                      item.ProductReceived == "1")
                    {
                        item.CAMQCL2Status = "Closed";
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") && item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL2Status = "Closed";
                    }

                    if (item.SampleStatus == "1")
                    {
                        item.CAMQCL2Status = "Closed";
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") &&
                      item.CAMQCL1Action == "1" &&
                      item.ProductReceived == "1")
                    {
                        item.StockReturnToQC = "Closed";
                    }

                    if ((item.SampleStatus == "2" || item.SampleStatus == "Approved") && item.CAMQCL1Action == "1")
                    {
                        item.StockReturnToQC = "Closed";
                    }

                    if (item.SampleStatus == "1")
                    {
                        item.StockReturnToQC = "Closed";
                    }
                }

                // var data = result.OrderByDescending(r => r.ProductStatus)
                //    .ThenByDescending(r =>
                //    {
                //        if (r.ProductSubmitDate.HasValue && r.RejectedItemSubmitDate.HasValue)
                //        {
                //            return r.ProductSubmitDate > r.RejectedItemSubmitDate ? r.ProductSubmitDate.Value : r.RejectedItemSubmitDate.Value;
                //        }
                //        else if (r.ProductSubmitDate.HasValue)
                //        {
                //            return r.ProductSubmitDate.Value;
                //        }
                //        else if (r.RejectedItemSubmitDate.HasValue)
                //        {
                //            return r.RejectedItemSubmitDate.Value;
                //        }
                //        else
                //        {
                //            return DateTime.MinValue;
                //        }
                //    }).ThenByDescending(r => r.DateCreated).ToList();
                var responseDataTable = this.ToDataTable1(result, cols);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("FilteredData");

                var headers = new[] { "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "ImageURL", "ProductStatus", "QCStatus", "QCApprovedDate", "CAMQCL1ACKStatus", "CAMQCL1Status", "CAMQCL1SubmissionStatus", "PhotoshootActionStatus", "PhotoShootStatus", "CAMQCL2Status", "CAMQCL2SubmissionStatus", "StockReturnToQC", "WebSiteLiveStatus", "WebsiteLiveDate" };
                var headerBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerBackgroundColor);
                    cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(borderColor);
                    cell.Style.Border.Left.Color.SetColor(borderColor);
                    cell.Style.Border.Right.Color.SetColor(borderColor);
                    cell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                var row = 2;
                foreach (var record in result)
                {
                    worksheet.Cells[row, 1].Value = record.ItemCode.ToString();
                    worksheet.Cells[row, 2].Value = record.EANCode.ToString();
                    worksheet.Cells[row, 3].Value = record.Division;
                    worksheet.Cells[row, 4].Value = record.SampleType;
                    worksheet.Cells[row, 5].Value = record.ReceivedDate;
                    worksheet.Cells[row, 6].Value = record.ImageURL;
                    worksheet.Cells[row, 7].Value = record.ProductStatus;
                    worksheet.Cells[row, 8].Value = record.QCStatus;
                    worksheet.Cells[row, 9].Value = record.QCApprovedDate;
                    worksheet.Cells[row, 10].Value = record.CAMQCL1ACKStatus;
                    worksheet.Cells[row, 11].Value = record.CAMQCL1Status;
                    worksheet.Cells[row, 12].Value = record.CAMQCL1SubmissionStatus;
                    worksheet.Cells[row, 13].Value = record.PhotoshootActionStatus;
                    worksheet.Cells[row, 14].Value = record.PhotoShootStatus;
                    worksheet.Cells[row, 15].Value = record.CAMQCL2Status;
                    worksheet.Cells[row, 16].Value = record.CAMQCL2SubmissionStatus;
                    worksheet.Cells[row, 17].Value = record.StockReturnToQC;
                    worksheet.Cells[row, 18].Value = record.WebSiteLiveStatus;
                    worksheet.Cells[row, 19].Value = record.WebsiteLiveDate;

                    for (int col = 1; col <= 17; col++)
                    {
                        var dataCell = worksheet.Cells[row, col];
                        dataCell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Top.Color.SetColor(borderColor);
                        dataCell.Style.Border.Left.Color.SetColor(borderColor);
                        dataCell.Style.Border.Right.Color.SetColor(borderColor);
                        dataCell.Style.Border.Bottom.Color.SetColor(borderColor);
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                worksheet.Column(1).Width = 22;
                worksheet.Column(2).Width = 22;
                worksheet.Column(3).Width = 22;
                worksheet.Column(4).Width = 22;
                worksheet.Column(5).Width = 22;
                worksheet.Column(6).Width = 22;
                worksheet.Column(7).Width = 22;
                worksheet.Column(8).Width = 22;
                worksheet.Column(9).Width = 26;
                worksheet.Column(10).Width = 24;
                worksheet.Column(11).Width = 24;
                worksheet.Column(12).Width = 30;
                worksheet.Column(13).Width = 30;
                worksheet.Column(14).Width = 24;
                worksheet.Column(15).Width = 22;
                worksheet.Column(16).Width = 30;
                worksheet.Column(17).Width = 22;

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                byte[] byteArray = memoryStream.ToArray();

                return byteArray;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> GetAllFitersPSExportExcel(ReceivedProductsModel receivedProductsModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = receivedProductsModel.ReceivedFromDate ?? DateTime.MinValue;
                var endDates = receivedProductsModel.ReceivedToDate ?? DateTime.MaxValue;
                var receivedformattedStartDate = startDates.ToString("yyyy/MM/dd");
                var receivedformattedEndDate = endDates.ToString("yyyy/MM/dd");
                var handoverstartDate = receivedProductsModel.HandOverFromDate ?? DateTime.MinValue;
                var handoverendDate = receivedProductsModel.HandOverToDate ?? DateTime.MaxValue;
                var handoverformattedStartDate = handoverstartDate.ToString("yyyy/MM/dd");
                var handoverformattedEndDate = handoverendDate.ToString("yyyy/MM/dd");
                var query = new List<GetAllFiltersPSModal>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "QCApprovedDate", "ImageURL", "StudioName", "City", "TotalCapacity", "StudioAllocationDate", "ProductStatus", "QCStatus", "CAMQCL1ACKStatus", "CAMQCL1Status", "CAMQCL1SubmissionStatus", "PhotoshootActionStatus", "PhotoShootStatus", "CAMQCL2Status", "CAMQCL2SubmissionStatus", "StockReturnToQC", "WebSiteLiveStatus", "WebsiteLiveDate" };

                // var basequery = new StringBuilder("SELECT DISTINCT p.id, p.itemcode, ics.eancode, p.imagetype AS Type, TO_CHAR(p.receiveddate, 'DD/MM/YY') AS ReceivedDate, TO_CHAR(p.statusdate, 'DD/MM/YY') AS HandOverDate, p.statusdate, p.imageurl, p.handoverstatus As HandoverToBusinessStatus, p.camqcstatus AS SampleReceivedByCamQC, p.photoshootstatus,p.stockreturn AS StockReturnToQC, d.name as Division FROM productimage p INNER JOIN division d ON p.divisionid = d.id  LEFT JOIN itemcodestaging AS ics ON p.itemcode = ics.code  ");
                var basequery = new StringBuilder($"SELECT MAX(pi.id) AS Id, MAX(pi.datemodified) AS DateModified, MAX(pi.itemcode) AS ItemCode, " +
    $"MAX(ics.eancode) AS EANCode, MAX(pi.imagetype) AS SampleType, TO_CHAR(MAX(pi.receiveddate), 'DD/MM/YY') AS ReceivedDate, " +
    $"MAX(pi.handoverdate) AS HandoverDate, MAX(pi.handoverstatus) AS HandoverStatus, MAX(pi.imageurl) AS ImageURL, " +
    $"MAX(pi.camqcstatus) AS CamQCStatus, MAX(pi.stockreturncomments) AS Comments, MAX(pi.photoshootstatus) AS PhotoshootStatus, " +
    $"MAX(pi.samplestatus) AS SampleStatus, MAX(pi.stockreturn) AS StockReturnToQC, TO_CHAR(MAX(pi.statusdate), 'DD/MM/YY') AS QCApprovedDate, " +
    $"MAX(pi.camqcl1rejection) AS CamQCL1Rejection, MAX(pi.divisionid) AS DivisionId, MAX(pi.qcaction) AS ProductStatus, " +
    $"MAX(pi.rejecteditemsubmit) AS RejectedItemSubmit, MAX(pi.rejectedcount) AS RejectedCount, MAX(pi.datecreated) AS DateCreated, " +
    $"MAX(pi.productsubmitdate) AS ProductSubmitDate, MAX(pi.rejecteditemsubmitdate) AS RejectedItemSubmitDate, " +
    $"MAX(s.name) AS StudioName, MAX(s.city) AS City, MAX(s.totalcapacity) AS TotalCapacity, TO_CHAR(MAX(p.allocationdate), 'DD/MM/YY') AS StudioAllocationDate, " +
    $"MAX(pi.qualitycheck) AS QualityCheck, MAX(pi.camqcl2submit) AS CamQcL2Submit, MAX(pi.camqcl1action) AS CAMQCL1Action, " +
    $"MAX(pi.camqcl1qualitycheck) AS CAMQCL1QualityCheck, MAX(pi.productreceived) AS ProductReceived, MAX(pi.rejectedby) AS RejectedBy, " +
    $"MAX(d.name) AS Division FROM productimage AS pi " +
    $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
    $"LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
    $"LEFT JOIN productstudio AS p ON p.productid = pi.productid " +
    $"LEFT JOIN studio AS s ON s.id = p.studioid " +
    $"LEFT JOIN division AS d ON d.id = pi.divisionid " +
    $"WHERE pro.isactive = true ");

                // if (!string.IsNullOrEmpty(receivedProductsModel.Status))
                // {
                //    var handoverstatus = await connection.QueryFirstOrDefaultAsync<long>($"SELECT id FROM mtrhandoverstatus WHERE name = '{receivedProductsModel.Status}'");

                // if (handoverstatus > 0)
                //    {
                //        basequery.Append("WHERE ");
                //        basequery.Append($"p.handoverstatus = {handoverstatus} ");
                //    }
                // }
                var statusP = await connection.QueryAsync<(int key, string text)>(@"SELECT v.value->>'key' AS key, v.value->>'text' AS text FROM appsettings, jsonb_array_elements(jsonvalue) AS v(value) WHERE name = 'PhotoshootStatus'");
                var statusesP = statusP.AsList();
                foreach (var status in statusesP)
                {
                    if (receivedProductsModel.PSstatus == status.text)
                    {
                        if (status.key == 1)
                        {
                            basequery.Append($" AND (pi.photoshootstatus = 1) ");
                        }
                        else if (status.key == 2)
                        {
                            basequery.Append($" AND (pi.photoshootstatus = 2) ");
                        }
                        else if (status.key == 3)
                        {
                            basequery.Append($" AND (pi.camqcl1action = '1' AND pi.camqcl1qualitycheck is null) or pi.productreceived = '1' or pi.samplestatus = '1' ");
                        }

                        break;
                    }
                    else if (receivedProductsModel.PSstatus == "Pending, Closed" || receivedProductsModel.PSstatus == "Closed, Pending")
                    {
                        basequery.Append($" AND (pi.photoshootstatus = 1 OR (pi.camqcl1action = '1' AND pi.camqcl1qualitycheck IS NULL) OR pi.productreceived = '1' OR pi.samplestatus = '1') ");
                    }
                    else if (receivedProductsModel.PSstatus == "Pending, Completed" || receivedProductsModel.PSstatus == "Completed, Pending")
                    {
                        basequery.Append($" AND (pi.photoshootstatus = 1 OR pi.photoshootstatus = 2) ");
                    }
                    else if (receivedProductsModel.PSstatus == "Closed, Completed" || receivedProductsModel.PSstatus == "Completed, Closed")
                    {
                        basequery.Append($" AND ((pi.camqcl1action = '1' AND pi.camqcl1qualitycheck IS NULL) OR pi.productreceived = '1' OR pi.samplestatus = '1' OR pi.photoshootstatus = 2) ");
                    }
                    else if (receivedProductsModel.PSstatus == "Pending, Closed, Completed" || receivedProductsModel.PSstatus == "Pending, Completed, Closed" || receivedProductsModel.PSstatus == "Completed, Closed, Pending" || receivedProductsModel.PSstatus == "Completed, Pending, Closed" || receivedProductsModel.PSstatus == "Closed, Pending, Completed" || receivedProductsModel.PSstatus == "Closed, Completed, Pending")
                    {
                        basequery.Append($" AND ((pi.photoshootstatus = '2' OR pi.photoshootstatus = '1') OR (pi.camqcl1action = '1' AND pi.camqcl1qualitycheck IS NULL) OR pi.samplestatus = '1' OR pi.productreceived = '1') ");
                    }
                }

                var statusC = await connection.QueryAsync<(string key, string text)>(@"SELECT v.value->>'key' AS key, v.value->>'text' AS text FROM appsettings, jsonb_array_elements(jsonvalue) AS v(value) WHERE name = 'CamStatus'");
                var statusesC = statusC.AsList();

                foreach (var statusCam in statusesC)
                {
                    if (receivedProductsModel.CamPstatus == statusCam.text)
                    {
                        if (statusCam.key == "1")
                        {
                            basequery.Append(" AND (pi.camqcl1action = 'Received' and pi.camqcl1qualitycheck IS NULL ) ");
                        }
                        else if (statusCam.key == "2")
                        {
                            basequery.Append(" AND (pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' ) ");
                        }
                        else if (statusCam.key == "3")
                        {
                            basequery.Append(" AND (pi.camqcl1action = '1' and pi.productreceived IS NULL ) ");
                        }
                        else if (statusCam.key == "4")
                        {
                            basequery.Append(" AND (pi.samplestatus = '1' and pi.camqcl1action is null ) ");
                        }

                        break;
                    }

                    if (receivedProductsModel.CamPstatus == "Closed, Rejected" || receivedProductsModel.CamPstatus == "Rejected, Closed")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = '1' AND pi.productreceived IS NULL) OR (pi.samplestatus = '1' AND pi.camqcl1action IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Approved, Pending" || receivedProductsModel.CamPstatus == "Pending, Approved")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' ) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Pending, Closed" || receivedProductsModel.CamPstatus == "Closed, Pending")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR (pi.samplestatus = '1' AND pi.camqcl1action IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Pending, Rejected" || receivedProductsModel.CamPstatus == "Rejected, Pending")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR (pi.camqcl1action = '1' AND pi.productreceived IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Approved, Rejected" || receivedProductsModel.CamPstatus == "Rejected, Approved")
                    {
                        basequery.Append(" AND (pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR (pi.camqcl1action = '1' AND pi.productreceived IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Approved, Closed" || receivedProductsModel.CamPstatus == "Closed, Approved")
                    {
                        basequery.Append(" AND (pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR (pi.samplestatus = '1' AND pi.camqcl1action IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Approved, Closed, Pending" || receivedProductsModel.CamPstatus == "Pending, Closed, Approved" || receivedProductsModel.CamPstatus == "Closed, Approved, Pending")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR (pi.samplestatus = '1' AND pi.camqcl1action IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Pending, Approved, Rejected" || receivedProductsModel.CamPstatus == "Pending, Rejected, Approved" || receivedProductsModel.CamPstatus == "Approved, Pending, Rejected" || receivedProductsModel.CamPstatus == "Approved, Rejected, Pending" || receivedProductsModel.CamPstatus == "Rejected, Approved, Pending" || receivedProductsModel.CamPstatus == "Rejected, Pending, Approved")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR (pi.camqcl1action = '1' AND pi.productreceived IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Closed, Approved, Rejected" || receivedProductsModel.CamPstatus == "Closed, Rejected, Approved" || receivedProductsModel.CamPstatus == "Approved, Closed, Rejected" || receivedProductsModel.CamPstatus == "Approved, Rejected, Closed" || receivedProductsModel.CamPstatus == "Rejected, Approved, Closed" || receivedProductsModel.CamPstatus == "Rejected, Closed, Approved")
                    {
                        basequery.Append(" AND ((pi.samplestatus = '1' AND pi.camqcl1action IS NULL) OR pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR   (pi.camqcl1action = '1' AND pi.productreceived IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Pending, Approved, Closed" || receivedProductsModel.CamPstatus == "Pending, Closed, Approved" || receivedProductsModel.CamPstatus == "Approved, Pending, Closed" || receivedProductsModel.CamPstatus == "Approved, Closed, Pending" || receivedProductsModel.CamPstatus == "Closed, Approved, Pending" || receivedProductsModel.CamPstatus == "Closed, Pending, Approved")
                    {
                        if (basequery.ToString().Contains("WHERE"))
                        {
                            basequery.Append("AND ");
                        }
                        else
                        {
                            basequery.Append("WHERE ");
                        }

                        basequery.Append($"(pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR pi.camqcl1action = 'Received' and pi.camqcl1qualitycheck IS NULL OR pi.samplestatus = '1' and pi.camqcl1action is null ) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Approved, Pending, Rejected, Closed" || receivedProductsModel.CamPstatus == "Approved, Pending, Closed, Rejected" || receivedProductsModel.CamPstatus == "Approved, Closed, Rejected, Pending" || receivedProductsModel.CamPstatus == "Approved, Closed, Pending, Rejected" || receivedProductsModel.CamPstatus == "Pending, Approved, Rejected, Closed" || receivedProductsModel.CamPstatus == "Closed, Rejected, Approved, Pending")
                    {
                        basequery.Append(" AND ((pi.camqcl1action = 'Received' AND pi.camqcl1qualitycheck IS NULL) OR pi.camqcl1qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.camqcl1qualitycheck is null and pi.rejectedby is not null and pi.productreceived='1' OR (pi.camqcl1action = '1' AND pi.productreceived IS NULL) OR (pi.samplestatus = '1' AND pi.camqcl1action IS NULL)) ");
                    }
                    else if (receivedProductsModel.CamPstatus == "Closed, Pending, Rejected" || receivedProductsModel.CamPstatus == "Closed, Rejected, Pending" || receivedProductsModel.CamPstatus == "Pending, Closed, Rejected" || receivedProductsModel.CamPstatus == "Pending, Rejected, Closed" || receivedProductsModel.CamPstatus == "Rejected, Pending, Closed" || receivedProductsModel.CamPstatus == "Rejected, Closed, Pending")
                    {
                        if (basequery.ToString().Contains("WHERE"))
                        {
                            basequery.Append("AND ");
                        }
                        else
                        {
                            basequery.Append("WHERE ");
                        }

                        basequery.Append($"(pi.camqcl1action = 'Received' and pi.camqcl1qualitycheck IS NULL OR pi.samplestatus = '1' and pi.camqcl1action is null and pi.camqcl1qualitycheck IS NULL OR pi.camqcl1action = '1' and pi.productreceived IS NULL ) ");
                    }
                    else if (receivedProductsModel.CamRstatus == statusCam.text)
                    {
                        if (statusCam.key == "1")
                        {
                            basequery.Append(" AND (pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL ) ");
                        }
                        else if (statusCam.key == "2")
                        {
                            basequery.Append(" AND (pi.qualitycheck = 'Pass' ) ");
                        }
                        else if (statusCam.key == "3")
                        {
                            basequery.Append(" AND (pi.qualitycheck = '1' ) ");
                        }
                        else if (statusCam.key == "4")
                        {
                            basequery.Append(" AND (pi.samplestatus = '1' or pi.camqcl1action = '1' and pi.qcaction = 'true' ) ");
                        }

                        break;
                    }
                    else if (receivedProductsModel.CamRstatus == "Approved, Pending, Rejected, Closed" ||
          receivedProductsModel.CamRstatus == "Approved, Pending, Closed, Rejected" ||
          receivedProductsModel.CamRstatus == "Approved, Closed, Rejected, Pending" ||
          receivedProductsModel.CamRstatus == "Approved, Closed, Pending, Rejected" ||
          receivedProductsModel.CamRstatus == "Pending, Approved, Rejected, Closed" ||
          receivedProductsModel.CamRstatus == "Closed, Rejected, Approved, Pending")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.qualitycheck = 'Pass' OR pi.qualitycheck = '1' OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Pending, Approved, Rejected" ||
                             receivedProductsModel.CamRstatus == "Pending, Rejected, Approved" ||
                             receivedProductsModel.CamRstatus == "Approved, Pending, Rejected" ||
                             receivedProductsModel.CamRstatus == "Approved, Rejected, Pending" ||
                             receivedProductsModel.CamRstatus == "Rejected, Approved, Pending" ||
                             receivedProductsModel.CamRstatus == "Rejected, Pending, Approved")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.qualitycheck = 'Pass' OR pi.qualitycheck = '1') ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Pending, Approved, Closed" ||
                             receivedProductsModel.CamRstatus == "Pending, Closed, Approved" ||
                             receivedProductsModel.CamRstatus == "Approved, Pending, Closed" ||
                             receivedProductsModel.CamRstatus == "Approved, Closed, Pending" ||
                             receivedProductsModel.CamRstatus == "Closed, Approved, Pending" ||
                             receivedProductsModel.CamRstatus == "Closed, Pending, Approved")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.qualitycheck = 'Pass' OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Pending, Rejected, Closed" ||
                             receivedProductsModel.CamRstatus == "Pending, Closed, Rejected" ||
                             receivedProductsModel.CamRstatus == "Rejected, Pending, Closed" ||
                             receivedProductsModel.CamRstatus == "Rejected, Closed, Pending" ||
                             receivedProductsModel.CamRstatus == "Closed, Rejected, Pending" ||
                             receivedProductsModel.CamRstatus == "Closed, Pending, Rejected")
                    {
                        basequery.Append(" AND ((pi.camqcl1qualitycheck = 'Pass' AND pi.camqcl2action IS NULL AND pi.qualitycheck IS NULL) OR pi.qualitycheck = '1' OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Closed, Approved, Rejected" || receivedProductsModel.CamRstatus == "Closed, Rejected, Approved" || receivedProductsModel.CamRstatus == "Approved, Closed, Rejected" || receivedProductsModel.CamRstatus == "Approved, Rejected, Closed" || receivedProductsModel.CamRstatus == "Rejected, Approved, Closed" || receivedProductsModel.CamRstatus == "Rejected, Closed, Approved")
                    {
                        if (basequery.ToString().Contains("WHERE"))
                        {
                            basequery.Append("AND ");
                        }
                        else
                        {
                            basequery.Append("WHERE ");
                        }

                        basequery.Append($"(pi.samplestatus = '1' or pi.camqcl1action = '1' and pi.qcaction = 'true' OR pi.qualitycheck = 'Pass' OR pi.qualitycheck = '1' ) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Pending, Approved, Closed" || receivedProductsModel.CamRstatus == "Pending, Closed, Approved" || receivedProductsModel.CamRstatus == "Approved, Pending, Closed" || receivedProductsModel.CamRstatus == "Approved, Closed, Pending" || receivedProductsModel.CamRstatus == "Closed, Approved, Pending" || receivedProductsModel.CamRstatus == "Closed, Pending, Approved")
                    {
                        if (basequery.ToString().Contains("WHERE"))
                        {
                            basequery.Append("AND ");
                        }
                        else
                        {
                            basequery.Append("WHERE ");
                        }

                        basequery.Append($"(pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL OR pi.qualitycheck = 'Pass' OR pi.samplestatus = '1' or pi.camqcl1action = '1' and pi.qcaction = 'true' ) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Rejected, Closed" ||
                             receivedProductsModel.CamRstatus == "Closed, Rejected")
                    {
                        basequery.Append(" AND (pi.qualitycheck = '1' OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Pending, Closed" ||
                             receivedProductsModel.CamRstatus == "Closed, Pending")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Approved, Closed" ||
                             receivedProductsModel.CamRstatus == "Closed, Approved")
                    {
                        basequery.Append(" AND (pi.qualitycheck = 'Pass' OR pi.samplestatus = '1' OR (pi.camqcl1action = '1' AND pi.qcaction = 'true')) ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Rejected, Pending" ||
                             receivedProductsModel.CamRstatus == "Pending, Rejected")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.qualitycheck = '1') ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Approved, Pending" ||
                             receivedProductsModel.CamRstatus == "Pending, Approved")
                    {
                        basequery.Append(" AND ((pi.photoshootstatus = 2 and pi.camqcl2action IS NULL and pi.qualitycheck IS NULL) OR pi.qualitycheck = 'Pass') ");
                    }
                    else if (receivedProductsModel.CamRstatus == "Approved, Rejected" ||
                             receivedProductsModel.CamRstatus == "Rejected, Approved")
                    {
                        basequery.Append(" AND (pi.qualitycheck = 'Pass' OR pi.qualitycheck = '1') ");
                    }
                }

                if (receivedProductsModel.Division.Any())
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.Division.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.name IN {formattedDivisions} ");
                }

                if (receivedProductsModel.UserDivision.Any() && receivedProductsModel.Division == string.Empty)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    string[] divisions = receivedProductsModel.UserDivision.Split(',');

                    string formattedDivisions = $"('{string.Join("', '", divisions)}')";

                    basequery.Append($" d.code IN {formattedDivisions} ");
                }

                if (receivedProductsModel.ReceivedFromDate != null || receivedProductsModel.ReceivedToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{receivedformattedStartDate}' AND '{receivedformattedEndDate}' ");
                }

                if (receivedProductsModel.HandOverFromDate != null || receivedProductsModel.HandOverToDate != null)
                {
                    if (basequery.ToString().Contains("WHERE"))
                    {
                        basequery.Append("AND ");
                    }
                    else
                    {
                        basequery.Append("WHERE ");
                    }

                    basequery.Append($"to_char(pi.statusdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{handoverformattedStartDate}' AND '{handoverformattedEndDate}' ");
                }

                if (receivedProductsModel.ReceivedFromDate == null && receivedProductsModel.ReceivedToDate == null && receivedProductsModel.HandOverFromDate == null && receivedProductsModel.HandOverToDate == null && receivedProductsModel.Status == null)
                {
                    basequery.Append("GROUP BY pi.id, pi.datemodified ORDER BY CASE WHEN pi.qcaction IS NULL THEN 0 ELSE 1 END, CASE WHEN pi.allaction IS NULL THEN 0 ELSE 1 END, MAX(pi.id) DESC");
                }
                else
                {
                    basequery.Append("GROUP BY pi.id, pi.datemodified ORDER BY CASE WHEN pi.qcaction IS NULL THEN 0 ELSE 1 END, CASE WHEN pi.allaction IS NULL THEN 0 ELSE 1 END, MAX(pi.id) DESC");
                }

                var result = await connection.QueryAsync<GetAllFiltersPSModal>(basequery.ToString());
                var data = result.ToList();

                foreach (var item in data)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            item.PhotoShootStatus = string.Empty;
                            break;
                    }

                    switch (item.HandoverToBusinessStatus)
                    {
                        case "1":
                            var handover1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverToBusinessStatus = handover1.Name;
                            break;

                        case "2":
                            var handover2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverToBusinessStatus = handover2.Name;
                            break;
                        case null:
                        case "null":
                            item.HandoverToBusinessStatus = string.Empty;
                            break;
                    }

                    switch (item.SampleReceivedByCamQC)
                    {
                        case "1":
                            item.SampleReceivedByCamQC = "Pending";
                            break;

                        case "2":
                            item.SampleReceivedByCamQC = "Received";
                            break;
                        case null:
                        case "null":
                            item.SampleReceivedByCamQC = string.Empty;
                            break;
                    }

                    switch (item.StockReturnToQC)
                    {
                        case "0":
                            item.StockReturnToQC = "Yet To Return";
                            break;

                        case "1":
                            item.StockReturnToQC = "Returned";
                            break;

                        case "null":
                            item.StockReturnToQC = string.Empty;
                            break;
                    }

                    if (item.SampleStatus == "1")
                    {
                        item.QCStatus = "Rejected";
                    }
                    else if (item.SampleStatus == "2" || item.SampleStatus == "Approved")
                    {
                        item.QCStatus = "Approved";
                    }
                    else if (item.ProductStatus == "true" && (item.SampleStatus != "Approved" || item.SampleStatus != "2"))
                    {
                        item.QCStatus = "Pending";
                    }
                    else if (item.RejectedItemSubmit == "yes" && (item.SampleStatus != "Approved" || item.SampleStatus != "2"))
                    {
                        item.QCStatus = " ";
                    }
                    else
                    {
                        item.QCStatus = null;
                    }

                    if (item.ProductStatus == null)
                    {
                        item.ProductStatus = "WIP";
                    }
                    else if (item.RejectedCount != null && item.ProductStatus == null)
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else if (item.ProductStatus == "true")
                    {
                        item.ProductStatus = "Submitted";
                    }
                    else
                    {
                        item.ProductStatus = null;
                    }

                    if (item.CamQCStatus == "1" && item.CAMQCL1Action == null)
                    {
                        item.CAMQCL1ACKStatus = "Pending";
                    }
                    else if (item.CAMQCL1Action == "Received")
                    {
                        item.CAMQCL1ACKStatus = "Received";
                    }
                    else if (item.ProductReceived == "1" || item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL1ACKStatus = "Received";
                    }
                    else
                    {
                        item.CAMQCL1ACKStatus = null;
                    }

                    if (item.CAMQCL1Action == "Received" && item.CAMQCL1QualityCheck == null)
                    {
                        item.CAMQCL1Status = "Pending";
                        if (item.ProductReceived == "1")
                        {
                            item.CAMQCL1Status = "Approved";
                        }
                    }
                    else if (item.CAMQCL1QualityCheck == "Pass")
                    {
                        item.CAMQCL1Status = "Approved";
                    }
                    else if (item.CAMQCL1Action == "1")
                    {
                        item.CAMQCL1Status = "Rejected";
                    }
                    else
                    {
                        item.CAMQCL1Status = null;
                    }

                    if (item.CAMQCL1QualityCheck == "Pass" && item.CamQCStatus != "2")
                    {
                        item.CAMQCL1SubmissionStatus = "WIP";
                    }
                    else if (item.CamQCStatus == "2")
                    {
                        item.CAMQCL1SubmissionStatus = "Submitted";
                    }
                    else if (item.ProductReceived == "1")
                    {
                        item.CAMQCL1SubmissionStatus = "Submitted";
                    }
                    else
                    {
                        item.CAMQCL1SubmissionStatus = null;
                    }

                    if (item.ProductReceived == null && item.CamQCStatus == "2")
                    {
                        item.PhotoshootActionStatus = "Pending";
                    }
                    else if (item.ProductReceived == "Received")
                    {
                        item.PhotoshootActionStatus = "Approved";
                    }
                    else if (item.ProductReceived == "1")
                    {
                        item.PhotoshootActionStatus = "Rejected";
                    }
                    else
                    {
                        item.PhotoshootActionStatus = null;
                    }

                    if (item.PhotoShootStatus == "Completed" && item.QualityCheck == null)
                    {
                        item.CAMQCL2Status = "Pending";
                    }
                    else if (item.QualityCheck == "Pass")
                    {
                        item.CAMQCL2Status = "Approved";
                    }
                    else if (item.QualityCheck == "1")
                    {
                        item.CAMQCL2Status = "Rejected";
                    }
                    else
                    {
                        item.CAMQCL2Status = null;
                    }

                    if (item.QualityCheck == "Pass" && item.CamQcL2Submit == null)
                    {
                        item.CAMQCL2SubmissionStatus = "WIP";
                    }
                    else if (item.CamQcL2Submit == "1")
                    {
                        item.CAMQCL2SubmissionStatus = "Submitted";
                    }
                    else
                    {
                        item.CAMQCL2SubmissionStatus = null;
                    }
                }

                // var orderedData = data.OrderByDescending(r => r.ProductStatus)
                //    .ThenByDescending(r =>
                //    {
                //        if (r.ProductSubmitDate.HasValue && r.RejectedItemSubmitDate.HasValue)
                //        {
                //            return r.ProductSubmitDate > r.RejectedItemSubmitDate ? r.ProductSubmitDate.Value : r.RejectedItemSubmitDate.Value;
                //        }
                //        else if (r.ProductSubmitDate.HasValue)
                //        {
                //            return r.ProductSubmitDate.Value;
                //        }
                //        else if (r.RejectedItemSubmitDate.HasValue)
                //        {
                //            return r.RejectedItemSubmitDate.Value;
                //        }
                //        else
                //        {
                //            return DateTime.MinValue;
                //        }
                //    }).ThenByDescending(r => r.DateCreated).ToList();
                var tempFile = Path.GetTempFileName();
                using var wb = new XLWorkbook();
                var sheet = wb.AddWorksheet();
                using var stream = new MemoryStream();

                if (data.Any())
                {
                    var desiredColumnOrder = new List<string>
                {
                    "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "ImageURL", "PSImage", "StudioName", "City", "TotalCapacity", "StudioAllocationDate", "ProductStatus", "QCStatus", "QCApprovedDate", "CAMQCL1ACKStatus", "CAMQCL1Status", "CAMQCL1SubmissionStatus", "PhotoshootActionStatus", "PhotoShootStatus", "CAMQCL2Status", "CAMQCL2SubmissionStatus", "StockReturnToQC", "WebSiteLiveStatus", "WebsiteLiveDate"
                };

                    var r = 1;
                    var cH = 1;

                    foreach (var key in desiredColumnOrder)
                    {
                        sheet.Cell(r, cH).Value = key;
                        cH++;
                    }

                    r++;
                    foreach (var record in data)
                    {
                        var c = 1;

                        foreach (var columnName in desiredColumnOrder)
                        {
                            string imageUrl1 = null;
                            var imgquery = @"SELECT pi.pictureid FROM productimage pi WHERE pi.itemcode = @Itemcode";
                            var pictureId = await connection.QueryFirstOrDefaultAsync<string>(imgquery, new { Itemcode = record.ItemCode });

                            if (pictureId is not null)
                            {
                                var lastPictureId = pictureId.Split(',').Last();
                                var imageUrlQuery = $@"
    SELECT up.imageurl 
    FROM photoshootuploadpicture up 
    WHERE up.id::text IN ('{lastPictureId}')
    ORDER BY up.id DESC 
    LIMIT 1";

                                imageUrl1 = await connection.QueryFirstOrDefaultAsync<string>(imageUrlQuery);
                            }

                            if (columnName == "ItemCode")
                            {
                                ////sheet.Cell(r, c).Value = record.ItemCode;
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                double itemCodeValue = record.ItemCode;
                                sheet.Cell(r, c).Value = itemCodeValue;
                                sheet.Column(c).Width = 30;
                                sheet.Row(r).Height = 20;
                                sheet.Cell(r, c).Style.NumberFormat.Format = "0";
                            }
                            else if (columnName == "EANCode")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.EANCode;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "Division")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.Division;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "SampleType")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.SampleType;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "ReceivedDate")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.ReceivedDate;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "ImageURL")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.ImageURL;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "StudioName")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.StudioName;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "City")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.City;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "TotalCapacity")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.TotalCapacity;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "StudioAllocationDate")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.StudioAllocationDate;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "ProductStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.ProductStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "QCStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.QCStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "QCApprovedDate")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.QCApprovedDate;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "CAMQCL1ACKStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.CAMQCL1ACKStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "CAMQCL1Status")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.CAMQCL1Status;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "CAMQCL1SubmissionStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.CAMQCL1SubmissionStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "PhotoshootActionStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.PhotoshootActionStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "PhotoShootStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.PhotoShootStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "CAMQCL2Status")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.CAMQCL2Status;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "CAMQCL2SubmissionStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.CAMQCL2SubmissionStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "StockReturnToQC")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.StockReturnToQC;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "WebSiteLiveStatus")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.WebSiteLiveStatus;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "WebsiteLiveDate")
                            {
                                sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                                sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                sheet.Cell(r, c).Value = record.WebsiteLiveDate;
                                sheet.Column(c).Width = 30;
                            }
                            else if (columnName == "PSImage")
                            {
                                if (!string.IsNullOrEmpty(imageUrl1))
                                {
                                    string imageUrl = imageUrl1;
                                    var imgFile = await _blobRepo.DownloadFile(imageUrl);
                                    if (Path.GetExtension(imgFile).Equals(".avif", StringComparison.OrdinalIgnoreCase))
                                    {
                                        using (MagickImage image = new MagickImage(imgFile))
                                        {
                                            image.Write("converted_image.png");
                                            imgFile = "converted_image.png";
                                        }
                                    }

                                    var picture = sheet.Pictures.Add(imgFile).MoveTo(sheet.Cell(r, c));
                                    picture.Height = 63;
                                    picture.Width = 80;
                                    sheet.Column(c).Width = 14;
                                    sheet.Row(r).Height = 50;
                                }
                                else
                                {
                                    sheet.Cell(r, c).Value = null;
                                }
                            }

                            c++;
                        }

                        r++;
                    }
                }

                sheet.RangeUsed().CreateTable();
                sheet.Tables.Table(0).Theme = XLTableTheme.TableStyleLight9;
                sheet.Tables.Table(0).ShowAutoFilter = false;
                sheet.Tables.Table(0).SetShowAutoFilter(false);
                wb.SaveAs(stream);
                await using var fs = File.OpenWrite(tempFile);
                stream.Position = 0;
                await stream.CopyToAsync(fs);
                return tempFile;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Stream> ProductImageExportExcel(ProductImageModel productImageModel)
        {
            try
            {
                var startDates = productImageModel.FromDate ?? DateTime.MinValue;
                var endDates = productImageModel.ToDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd");
                using var connection = this.GetConnection();
                var result = new List<ProductImageExportModel>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Division", "Type", "ReceivedDate", "HandOverDate", "HandOverToBusinessStatus", "Image", "SampleReceivedByCamQc_QC", "PhotoshootStatus", "StockReturnToQC" };
                result = (await connection.QueryAsync<ProductImageExportModel>($"SELECT p.itemcode, p.eancode,d.name as Division, p.imagetype AS Type, TO_CHAR(p.receiveddate, 'DD/MM/YY') AS ReceivedDate, TO_CHAR(p.statusdate, 'DD/MM/YY') AS HandOverDate, p.imageurl AS Image, p.samplestatus AS HandOverToBusinessStatus, p.camqcstatus AS SampleReceivedByCamQc_QC, p.photoshootstatus, p.stockreturn as StockReturnToQC FROM productimage p inner join division d on p.divisionid = d.id " +
                    $"WHERE TO_CHAR(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' AND p.rejectedby is null or p.rejectedby is not null AND p.rejectedreason IS NULL or p.rejectedreason IS NOT NULL ORDER BY p.id DESC")).ToList();
                foreach (var item in result)
                {
                    switch (item.HandOverToBusinessStatus)
                    {
                        case "Approved":
                            item.HandOverToBusinessStatus = " Sent ";
                            break;

                        case "Rejected":
                            item.HandOverToBusinessStatus = " Not Sent ";
                            break;
                        case null:
                        case "null":
                            item.HandOverToBusinessStatus = " Not Sent ";
                            break;
                    }

                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photoshoot1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photoshoot1.Name;
                            break;

                        case "2":
                            var photoshoot2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photoshoot2.Name;
                            break;
                        case null:
                        case "null":
                            item.PhotoShootStatus = string.Empty;
                            break;
                    }

                    switch (item.SampleReceivedByCamQc_QC)
                    {
                        case "1":
                            item.SampleReceivedByCamQc_QC = string.Empty;
                            break;

                        case "2":
                            item.SampleReceivedByCamQc_QC = "Received";
                            break;
                        case null:
                        case "null":
                            item.SampleReceivedByCamQc_QC = string.Empty;
                            break;
                    }

                    switch (item.StockReturnToQC)
                    {
                        case "0":
                            item.StockReturnToQC = "Yet To Return";
                            break;

                        case "1":
                            item.StockReturnToQC = "Returned";
                            break;

                        case "null":
                            item.StockReturnToQC = string.Empty;
                            break;
                    }
                }

                var responseDataTable = this.ToDataTable(result);

                return new FileExportHelper().ExportToExcel(responseDataTable.SetColumnsOrder(cols));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<MtrProductImageType>> GetProductImageType()
        {
            try
            {
                using var connection = this.GetConnection();
                var productImageType = await connection.QueryAsync<MtrProductImageType>("select id, name from mtrproductimagetype");
                return productImageType.ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<PhotoShoot>> GetPhotoShoot([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = toDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
                string query = $"SELECT DISTINCT * " +
    $"FROM (SELECT pi.itemcode AS Itemcode, pi.id,pi.datemodified, TO_CHAR(pi.receiveddate, 'DD/MM/YY') AS receiveddate, pi.imageurl, pi.productreceived, pi.pictureid, TO_CHAR(pi.inwarddate, 'DD/MM/YY') AS inwarddate, " +
            $"TO_CHAR(pi.stockreturndate, 'DD/MM/YY') AS stockreturndate, pi.imagetype AS Imagetype, ics.eancode AS Eancode, pi.rangeshot, pi.submitdate AS SubmitDate, pi.camqcl1submitdate AS CamQcL1SubmitDate, " +
            $"pi.individualstyle, pi.stockreturncomments AS comments, ps.priority AS Priority,s.id AS StudioId, s.name AS StudioName, pi.divisionid AS DivisionId, " +
            $"s.city AS Studiolocation, pi.photoshootstatus AS photoshootstatus, pi.stockreturn AS stockReturnToQC, d.name AS division, " +
            $"pi.catalogshot AS catologshot, CASE WHEN pi.photoshootstatus IS NULL THEN 0 ELSE 1 END AS photoshootstatus_order " +
        $"FROM " +
            $"photoshootproduct p " +
        $"JOIN " +
            $"productimage pi ON p.id = pi.productid " +
        $"LEFT JOIN " +
            $"productstudio ps ON p.id = ps.productid " +
        $"LEFT JOIN " +
            $"studio s ON s.id = ps.studioid " +
        $"JOIN " +
            $"division d ON d.id = pi.divisionid " +
        $"LEFT JOIN " +
            $"itemcodestaging AS ics ON pi.itemcode = ics.code " +
        $"LEFT JOIN " +
            $"product AS pro ON pro.uid = ics.uidcode " +
        $"LEFT JOIN " +
            $"mtrproductimagetype mit ON mit.name = pi.imagetype " +
        $"WHERE " +
            $"TO_CHAR(pi.receiveddate::date::timestamp WITH TIME ZONE, 'yyyy/MM/dd HH24:MI:SS'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' " +
            $"AND pro.isactive = true " +
            $"AND pi.camqcstatus = 2";

                if (!string.IsNullOrEmpty(imageid))
                {
                    query += $" AND mit.id = {imageid}";
                }

                query += @" ) AS subquery";

                var productImageType = await connection.QueryAsync<PhotoShoot>(query);
                var item = productImageType.ToList();
                foreach (var data in item)
                {
                    switch (data.RangeShot)
                    {
                        case "0":
                            data.RangeShot = "Not Applicable";
                            break;
                        case "1":
                            data.RangeShot = "Done";
                            break;
                        case null:
                            break;
                    }
                }

                // return item.OrderBy(i =>
                // {
                //    var photoshootStatus = i.PhotoShootStatus;
                //    if (i.PhotoShootStatus.IsNullOrEmpty())
                //    {
                //        photoshootStatus = "1";
                //    }

                // return photoshootStatus;
                // }).ThenByDescending(i =>
                // {
                //    if (i.PhotoShootStatus == "1" || i.PhotoShootStatus == null)
                //    {
                //        return i.CamQcL1SubmitDate;
                //    }
                //    else
                //    {
                //        return i.SubmitDate;
                //    }
                // }).ToList();
                return item.OrderByDescending(i => i.DateModified) // Primary ordering by pi.id in descending order
           .ThenBy(i =>
           {
               var photoshootStatus = i.PhotoShootStatus;
               if (i.PhotoShootStatus.IsNullOrEmpty())
               {
                   photoshootStatus = "1"; // Default to "1" if PhotoShootStatus is null or empty
               }

               return photoshootStatus;
           }).ThenByDescending(i =>
           {
               if (i.PhotoShootStatus == "1" || i.PhotoShootStatus == null)
               {
                   return i.CamQcL1SubmitDate;
               }
               else
               {
                   return i.SubmitDate;
               }
           }).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<PhotoShootPicture>> GetPictureCamQcL1()
        {
            using var connection = this.GetConnection();
            var query = await connection.QueryAsync<PhotoShootPicture>("select * from photoshootuploadpicture");
            return query.ToList();
        }

        public async Task AddProductPicture(string? imageUrl, string? imageName, long? itemcode, string? username)
        {
            var imageName1 = imageName.ToLower();
            using var connection = this.GetConnection();
            var productId = connection.QueryFirstOrDefault<int>("SELECT id FROM Productimage WHERE itemcode = @itemcode", new { itemcode });
            if (productId == 0)
            {
                throw new Exception($"Item Code Not exist.");
            }

            var existingRecord = connection.QueryFirstOrDefault<PhotoShootPicture>("SELECT * FROM photoshootuploadpicture WHERE lower(Name) = lower(@name)", new { name = imageName });

            // if (existingRecord != null)
            // {
            //    throw new Exception($"{imageName} already exists.");
            // }
            if (imageUrl != null)
            {
                var queryResult = connection.Execute(
                      "INSERT INTO photoshootuploadpicture (Name, ImageUrl) VALUES (@name, @imageurl)",
                      new { name = imageName, imageurl = imageUrl });

                if (queryResult == null)
                {
                    throw new Exception($"{imageName} not Add.");
                }

                var currentPictureIdsString = connection.QueryFirstOrDefault<string>(
    "SELECT pictureid FROM Productimage WHERE itemcode = @itemcode",
    new { itemcode });

                var existingId = connection.QueryFirstOrDefault<PhotoShootPicture>(
                    "SELECT * FROM photoshootuploadpicture WHERE lower(Name) = lower(@name) and imageurl = @imageurl",
                    new { name = imageName, imageurl = imageUrl });
                long newPictureId = existingId.Id;

                List<string> reorderedPictureIds;

                if (string.IsNullOrEmpty(currentPictureIdsString))
                {
                    reorderedPictureIds = new List<string> { newPictureId.ToString() };
                }
                else
                {
                    var currentPictureIds = currentPictureIdsString.Split(',').ToList();
                    var uniqueIds = new HashSet<string>(currentPictureIds.Where(id => id != null));
                    if (!uniqueIds.Contains(newPictureId.ToString()))
                    {
                        reorderedPictureIds = uniqueIds.ToList();
                        reorderedPictureIds.Insert(0, newPictureId.ToString());
                    }
                    else
                    {
                        reorderedPictureIds = uniqueIds.ToList();
                    }
                }

                string reorderedPictureIdsString = string.Join(",", reorderedPictureIds);
                var existingPictureIds = await connection.QueryAsync<string>($"SELECT pictureid FROM productimage WHERE itemcode = {itemcode}");

                // Initialize a list to store all picture IDs
                List<string> pictureIdList = new List<string>();

                // Add the existing picture IDs to the list
                foreach (var pictureId in reorderedPictureIds)
                {
                    pictureIdList.Add(pictureId);
                }

                // Add the new picture ID to the list
                pictureIdList.Add(reorderedPictureIdsString);

                // Combine all picture IDs into a single string
                string updatedPictureIdsString = string.Join(",", pictureIdList.Where(id => !string.IsNullOrWhiteSpace(id)));
                var picture = await connection.QueryAsync<dynamic>($"select pictureid from productimage where itemcode = {itemcode}");
                if (picture.Count() > 0)
                {
                    await connection.ExecuteAsync($"Update productimage set pictureid = '{reorderedPictureIdsString}' where itemcode = {itemcode}");
                }
                else
                {
                    var queryResult1 = connection.Execute("UPDATE Productimage SET pictureid = @pictureid, psimageuploadby = @username, psimageuploaddate = @psimageuploaddate WHERE itemcode = @itemcode", new { itemcode, pictureid = reorderedPictureIdsString, username, psimageuploaddate = DateTime.UtcNow });
                }
            }
        }

        public async Task RemovePicture(long? itemcode, string? imageIds, string? username)
        {
            try
            {
                using var connection = this.GetConnection();

                if (itemcode != null && !string.IsNullOrEmpty(imageIds))
                {
                    var imageIdList = imageIds.Split(',').Select(id => id.Trim()).ToList();
                    connection.Execute($"DELETE FROM photoshootuploadpicture WHERE id IN ({string.Join(",", imageIdList.Select(id => Convert.ToInt64(id)))})");
                    var existingPictureIds = connection.QuerySingleOrDefault<string>(
                        "SELECT pictureid FROM Productimage WHERE itemcode = @itemcode",
                        new { itemcode });
                    if (!string.IsNullOrEmpty(existingPictureIds))
                    {
                        var existingPictureIdList = existingPictureIds.Split(',').Select(id => id.Trim()).ToList();
                        existingPictureIdList.RemoveAll(id => imageIdList.Contains(id));
                        var reorderedPictureIdsString = string.Join(",", existingPictureIdList);
                        connection.Execute(
                            "UPDATE Productimage SET pictureid = @pictureid, psimageuploadby = @username, psimageuploaddate = @psimageuploaddate WHERE itemcode = @itemcode",
                            new { itemcode, pictureid = reorderedPictureIdsString, username, psimageuploaddate = DateTime.UtcNow });
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateProductImage(long itemcode, string? range, long id, string? comment, string? removeData, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                string value = "yes";
                string user = username ?? string.Empty;
                DateTimeOffset? date = DateTimeOffset.UtcNow;
                if (removeData == "no")
                {
                    value = null;
                    date = null;
                    user = null;
                }

                int? photoshootstatus = 2;
                string? stockreturnval = "1";
                string? stockreturndata = "0";
                if (range == "0")
                {
                    id = 6;
                }

                if (id == 4 || id == 5)
                {
                    var remove = await connection.QueryFirstOrDefaultAsync<long>($"select studioid from productstudio where itemcode={itemcode}");
                    var removestudio = await connection.ExecuteAsync($"UPDATE studio set remainingcapacity=remainingcapacity + 1 where id={remove}");
                }

                var qualitycheck = await connection.QueryFirstOrDefaultAsync<string>($"select qualitycheck from productimage where itemcode={itemcode}");
                switch (id)
                {
                    case 1:
                        await connection.ExecuteAsync("UPDATE Productimage SET rangeshot=@range,rangeshotdate=@date, psrangeshotby=@psrangeshotby  WHERE itemcode = @itemcode", new { itemcode, range, date, psrangeshotby = user });
                        break;
                    case 2:
                        await connection.ExecuteAsync("UPDATE Productimage SET catalogshot = @value, catalogshotdate=@date, pscatalogby=@pscatalogby WHERE itemcode = @itemcode", new { itemcode, value, date, pscatalogby = user });
                        break;
                    case 3:
                        await connection.ExecuteAsync("UPDATE Productimage SET individualstyle=@value, individualstyledate=@date, psindividualby=@user WHERE itemcode = @itemcode", new { itemcode, value, date, user });
                        break;
                    case 4:
                        if (qualitycheck == "1")
                        {
                            await connection.ExecuteAsync("UPDATE Productimage SET qualitycheck=null  WHERE itemcode = @itemcode", new { itemcode });
                        }

                        await connection.ExecuteAsync("UPDATE Productimage SET stockreturn=@stockreturndata, photoshootstatus=@photoshootstatus,submitdate=@date, pssubmitedby= @user  WHERE itemcode = @itemcode", new { itemcode, photoshootstatus, stockreturndata, date, user });
                        break;
                    case 5:
                        if (qualitycheck == "1")
                        {
                            await connection.ExecuteAsync("UPDATE Productimage SET qualitycheck=null  WHERE itemcode = @itemcode", new { itemcode });
                        }

                        await connection.ExecuteAsync("UPDATE Productimage SET stockreturn=@stockreturnval, stockreturndate=@date, photoshootstatus=@photoshootstatus,stockreturncomments=@comment, psreturnsubmitedby = @psreturnsubmitedby WHERE itemcode = @itemcode", new { itemcode, stockreturnval, date, photoshootstatus, comment, psreturnsubmitedby = date });
                        break;
                    case 6:
                        await connection.ExecuteAsync("UPDATE Productimage SET rangeshot=@range,rangeshotdate=null, psrangeshotby=null  WHERE itemcode = @itemcode", new { itemcode, range });
                        break;
                    case 7:
                        await connection.ExecuteAsync("UPDATE Productimage SET rangeshot=null,rangeshotdate=null, psrangeshotby=null  WHERE itemcode = @itemcode", new { itemcode });
                        break;
                    case 8:
                        await connection.ExecuteAsync("UPDATE Productimage SET catalogshot = null, catalogshotdate=null, pscatalogby=null  WHERE itemcode = @itemcode", new { itemcode });
                        break;
                    case 9:
                        await connection.ExecuteAsync("UPDATE Productimage SET individualstyle=null, individualstyledate=null, psindividualby=null  WHERE itemcode = @itemcode", new { itemcode });
                        break;
                    default:
                        throw new Exception("Record Not Updated");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdatePhotoshotstate(long itemcode, string? productreceived, string? rcomment, string? username)
        {
            try
            {
                using var connection = this.GetConnection();

                // var picture = await connection.QueryFirstOrDefaultAsync<string>($"SELECT pictureid FROM productimage WHERE itemcode = {itemcode}");

                // if (picture != null)
                // {
                //    var deleteQuery = $"DELETE FROM photoshootuploadpicture  WHERE id IN ({picture})";
                //    await connection.ExecuteAsync(deleteQuery);
                //    await connection.ExecuteAsync("UPDATE Productimage SET psimageuploadby= null,psimageuploaddate= null,pictureid=null WHERE itemcode = @itemcode", new { itemcode });
                // }
                await connection.ExecuteAsync("UPDATE Productimage SET productreceived = @productreceived,rejectioncomments=@rcomment,photoshootstatus=1, psactionby=@username, psactiondate=@psactiondate,qualitycheck=null WHERE itemcode = @itemcode", new { itemcode, productreceived, rcomment, username, psactiondate = DateTime.UtcNow });
            }
            catch
            {
                throw new Exception($"Record not Saved.");
            }
        }

        public async Task RemoveReceivedProductRecords(long[] ids)
        {
            var action = this.GetAction<ProductImage>();
            await action.Remove(ids);
        }

        public async Task UpdateQCRecords(long[] itemcode, string status, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                foreach (var data in itemcode)
                {
                    var rejectedItemcode = await connection.QueryAsync<ProductImage>($"select itemcode from productimage where itemcode = {data} and qcaction = 'true' AND samplestatus = 'Approved' AND samplestatus ='Rejected' ");
                    if (rejectedItemcode.Any())
                    {
                        throw new Exception($"Already Submitted");
                    }

                    var duplicateCheck = await connection.QueryAsync<ProductImage>($"SELECT qcaction FROM productimage WHERE itemcode = {data} AND qcaction = 'true'");
                    if (duplicateCheck.Any())
                    {
                        throw new Exception($"Already Submitted");
                    }
                }

                foreach (var item in itemcode)
                {
                    var query = await connection.ExecuteAsync($"Update productimage set qcaction = '{status}',handoverstatus = 2, productsubmitby= '{username}', productsubmitdate = '{DateTime.UtcNow}' where itemcode = {item}");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateRejectedRecords(long[] itemcode, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                foreach (var data in itemcode)
                {
                    var duplicateCheck = await connection.QueryAsync<ProductImage>($"SELECT rejecteditemsubmit FROM productimage WHERE itemcode = {data} AND rejecteditemsubmit = 'yes'");
                    if (duplicateCheck.Any())
                    {
                        throw new Exception($"Already Submitted");
                    }
                }

                foreach (var item in itemcode)
                {
                    string status = "yes";
                    var query = await connection.ExecuteAsync($"Update productimage set rejecteditemsubmit = '{status}',handoverstatus=2,qcaction=null, rejecteditemsubmitdate='{DateTime.UtcNow}', rejecteditemsubmitby= '{username}' where itemcode = {item}");
                    await connection.ExecuteAsync($"UPDATE productimage SET qualitycheck=null,productreceived=null,qcaction=null,samplestatus=null,camqcl1action=null,statusdate=null WHERE itemcode = {item} ");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<byte[]> RejectedItemcodeExportExcel(ProductImageModel productImageModel)
        {
            try
            {
                var startDates = productImageModel.FromDate ?? DateTime.MinValue;
                var endDates = productImageModel.ToDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
                using var connection = this.GetConnection();
                var results = new List<RejectedItemcodeExportModel>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Division", "SampleType", "ReceivedDate", "RejectedBy", "Role", "RejectedReason", "RejectedCount", "Status" };
                var query = $"SELECT DISTINCT  p.itemcode, p.id, ics.eancode, p.imagetype AS SampleType, TO_CHAR(p.receiveddate, 'DD/MM/YY') AS ReceivedDate, p.stockreturn AS StockReturnToQC, p.photoshootstatus, " +
                    $"p.productsubmitdate AS ProductSubmitDate, p.rejecteditemsubmitdate AS RejectedItemSubmitDate, p.samplestatus AS SampleStatus, p.rejectedby,p.rejecteditemsubmit, p.rejecteddate, p.rejectedreason, p.rejectedcount, d.name as Division " +
            $"FROM productimage p INNER JOIN division d ON d.id = p.divisionid LEFT JOIN itemcodestaging AS ics ON p.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
            $"WHERE pro.isactive = true AND TO_CHAR(p.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd HH24:MI:SS'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' " +
            $"AND rejectedby is not null and rejectedreason is not null AND p.divisionid IN ({productImageModel.UserDivision}) " +
            $"ORDER BY p.id DESC";
                var statusSettings = await connection.QueryFirstOrDefaultAsync<AppSettings>("SELECT Jsonvalue FROM appsettings WHERE name = 'status'");
                var statusList = JsonConvert.DeserializeObject<List<Status>>(statusSettings.JsonValue);
                results = (await connection.QueryAsync<RejectedItemcodeExportModel>(query)).ToList();
                foreach (var item in results)
                {
                    var names = new List<string>();
                    var roles = new List<string>();

                    if (!string.IsNullOrEmpty(item.RejectedBy))
                    {
                        var entries = item.RejectedBy.Split(',');
                        foreach (var entry in entries)
                        {
                            var match = Regex.Match(entry.Trim(), @"^(.*?)(?:\((.*?)\))?$");
                            if (match.Success)
                            {
                                var name = match.Groups[1].Value.Trim();
                                var role = match.Groups[2].Value.Trim();

                                names.Add(name);
                                if (!string.IsNullOrEmpty(role))
                                {
                                    roles.Add(role);
                                }
                            }
                        }
                    }

                    item.RejectedBy = string.Join(", ", names);
                    item.Rolename = string.Join(", ", roles);
                    if (item.Rejecteditemsubmit == null)
                    {
                        var status = statusList.FirstOrDefault(s => s.Code == 1);
                        item.Status = status?.Message ?? string.Empty;
                    }
                    else
                    {
                        var status = statusList.FirstOrDefault(s => s.Code == 0);
                        item.Status = status?.Message ?? string.Empty;
                    }
                }

                var result = results.OrderBy(r =>
                {
                    var samplestatus = string.Empty;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus;
                }).ThenByDescending(r =>
                {
                    var samplestatus = string.Empty;
                    var lastRejectedDate = DateTime.MinValue;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                        var rejectedDate = r.RejectedDate;
                        string[] dates = rejectedDate.Split(',');
                        string lastDateString = dates[dates.Length - 1].Trim();
                        lastRejectedDate = DateTime.ParseExact(lastDateString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus == "Rejected" ? lastRejectedDate : r.ProductSubmitDate;
                }).ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Rejected Items");

                var headers = cols;
                var headerBackgroundColor = ColorTranslator.FromHtml("#5787C5");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = ColorTranslator.FromHtml("#5787C5");

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerBackgroundColor);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(borderColor);
                    cell.Style.Border.Left.Color.SetColor(borderColor);
                    cell.Style.Border.Right.Color.SetColor(borderColor);
                    cell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                var row = 2;
                foreach (var item in result)
                {
                    worksheet.Cells[row, 1].Value = item.ItemCode.ToString();
                    worksheet.Cells[row, 2].Value = item.EANCode.ToString();
                    worksheet.Cells[row, 3].Value = item.Division;
                    worksheet.Cells[row, 4].Value = item.SampleType;
                    worksheet.Cells[row, 5].Value = item.ReceivedDate;
                    worksheet.Cells[row, 6].Value = item.RejectedBy;
                    worksheet.Cells[row, 7].Value = item.Rolename;
                    worksheet.Cells[row, 8].Value = item.RejectedReason;
                    worksheet.Cells[row, 9].Value = item.RejectedCount;
                    worksheet.Cells[row, 10].Value = item.Status;

                    for (int col = 1; col <= headers.Length; col++)
                    {
                        var dataCell = worksheet.Cells[row, col];
                        dataCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        dataCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        dataCell.Style.Border.Top.Color.SetColor(borderColor);
                        dataCell.Style.Border.Left.Color.SetColor(borderColor);
                        dataCell.Style.Border.Right.Color.SetColor(borderColor);
                        dataCell.Style.Border.Bottom.Color.SetColor(borderColor);
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                worksheet.Column(1).Width = 22;
                worksheet.Column(2).Width = 22;
                worksheet.Column(3).Width = 22;
                worksheet.Column(4).Width = 22;
                worksheet.Column(5).Width = 22;
                worksheet.Column(6).Width = 26;
                worksheet.Column(7).Width = 26;
                worksheet.Column(8).Width = 26;
                worksheet.Column(9).Width = 22;
                worksheet.Column(10).Width = 22;

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<QCModel>> GetQcRecords()
        {
            try
            {
                using var connection = GetConnection();
                var query = await connection.QueryAsync<QCModel>(@"
            SELECT DISTINCT 
                pi.itemcode, pi.id, ics.eancode, pi.imagetype, pi.receiveddate, pi.samplestatus, pi.imageurl, pi.productsubmitdate, 
                pi.statusdate, pi.handoverdate, pi.handoverstatus, pi.camqcstatus,pi.camqcl1rejection,pi.divisionid as DivisionId,pi.samplestatusrejection
            FROM 
                productimage as pi
 
            LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code
            LEFT JOIN product AS pro ON pro.uid = ics.uidcode
           WHERE  (pi.handoverstatus IS NOT NULL ) and  samplestatus is null and  pi.qcaction='true' order by pi.id,pi.itemcode, ics.eancode, pi.imagetype, pi.receiveddate, pi.samplestatus,
 
pi.statusdate, pi.handoverdate, pi.handoverstatus, pi.camqcstatus,pi.camqcl1rejection,pi.divisionid,pi.samplestatusrejection desc
            ");
                //// 
                ////    (  AND samplestatus != 'Approved') 
                ////    OR (qcaction IS NOT NULL AND samplestatus IS NULL);

                var results = query.ToList();

                foreach (var result in results)
                {
                    switch (result.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            result.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (result.CamQCStatus)
                    {
                        case "1":
                            var camqc1 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("select name from mtrcamqcstatus where id = 1");
                            result.CamQCStatus = camqc1.Name;
                            break;

                        case "2":
                            var camqc2 = await connection.QueryFirstOrDefaultAsync<MtrCamQcStatus>("select name from mtrcamqcstatus where id = 2");
                            result.CamQCStatus = camqc2.Name;
                            break;
                        case null:
                        case "null":
                            result.CamQCStatus = string.Empty;
                            break;
                    }

                    switch (result.SampleStatus)
                    {
                        case null:
                            var samplestatus = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            result.SampleStatus = samplestatus.Name;
                            break;
                    }
                }

                return results.OrderByDescending(r => r.ProductSubmitDate).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdatePhotoshootProduct()
        {
            using var connection = this.GetConnection();
            var itemcode = await connection.QueryAsync<ItemCodeStaging>("select code, eancode from itemcodestaging");

            foreach (var item in itemcode)
            {
                var query = @"INSERT INTO photoshootproduct (itemcode, eancode)SELECT @code, @eancode WHERE NOT EXISTS (SELECT 1 FROM photoshootproduct WHERE itemcode = @code AND eancode = @eancode)";
                await connection.ExecuteAsync(query, new { code = item.Code, eancode = item.EanCode });
            }
        }

        private DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                dataTable.Columns.Add(prop.Name);
            }

            foreach (T item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item, null);
                }

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        private DataTable ToDataTable1<T>(List<T> items, string[] columns)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            // Add only the specified columns to the DataTable
            foreach (string columnName in columns)
            {
                dataTable.Columns.Add(columnName);
            }

            // Get the properties corresponding to the specified columns
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(prop => columns.Contains(prop.Name)).ToArray();

            foreach (T item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item, null);
                }

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }
    }
}
