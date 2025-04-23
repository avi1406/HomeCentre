using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using ExcelDataReader;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace HomeCentre.PIM.BAL.Implementations.LandmarkProcess
{
    public class DataProcess : PgEntityAction<EmployeeStaging>, IDataProcess
    {
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IBlobRepo _blobRepo;
        private readonly IDbConnectionFactory _connectionfactory;
        private readonly IDbCache _dbCache;
        private readonly IErrorLogger _errorLogger;
        private readonly IUserContext _usercontext;
        private readonly IUserManager _userManager;
        private readonly IProductImageManager _productImageManager;

        public DataProcess(
    IUserContext userContext,
    IUserManager userManager,
    IDbConnectionFactory connectionFactory,
    IBlobRepo blobRepo,
    IErrorLogger errorLogger,
    IAuthenticationProvider authenticationProvider, IConfiguration configuration,
    IDbCache dbCache, IProductImageManager productImageManager)
    : base(userContext, connectionFactory, configuration)
        {
            _usercontext = userContext;
            _connectionfactory = connectionFactory;
            _dbCache = dbCache;
            _userManager = userManager;
            _blobRepo = blobRepo;
            _errorLogger = errorLogger;
            _authenticationProvider = authenticationProvider;
            _productImageManager = productImageManager;
        }

        /// <summary>
        /// By using message name Getting file from blob
        /// and truncating the tempDbTable
        /// </summary>
        /// <param name="messageName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task ApproveOrReject(long id)
        {
            using var connection = this.GetConnection();

            var user = (await connection.QueryAsync<AppUser>("select * from appuser where id = (select recordid from workflowrun where id = (select workflowrunid from taskstepinstance where id = @Id))", new { Id = id })).FirstOrDefault();

            await _authenticationProvider.RegisterUsers(new[] { user });
        }

        public async Task<List<ExcelModel>> ExcelModel(string messageName)
        {
            var excelModel = new ExcelModel();
            var excel = await this.ProcessDataFromTemplate(messageName, true);
            var data = await GetExcelDataToEmployeeStaging(excel, null);
            var excelList = new List<ExcelModel>();

            for (var i = 0; i < data.Rows.Count; i++)
            {
                var count = 0;
                excelModel.FirstName = data.Rows[i]["Firstname"].ToString();
                excelModel.LastName = data.Rows[i]["Lastname"].ToString();
                excelModel.Email = data.Rows[i]["Email"].ToString();
                excelModel.Mobile = data.Rows[i]["Contactnumber"].ToString();
                excelModel.IsRemoved = data.Rows[i]["Isremoved"].ToString();
                excelModel.Error = " ";
                excelList.Add(excelModel);
            }

            return excelList;
        }

        public async Task<string> ProcessDataFromTemplate(string messageName, bool? returnDataOnly = false)
        {
            if (string.IsNullOrEmpty(messageName))
            {
                throw new ArgumentNullException(nameof(messageName));
            }

            try
            {
                var allowToExeute = false;
                using var connection = this.GetConnection();

                var configJsonString = await _dbCache.FindAppSettings("CreateRecordFromExcelConfig");

                if (configJsonString == null)
                {
                    throw new Exception("ConfigJsonString is Empty");
                }

                var configJson = JArray.Parse(configJsonString.JsonValue);
                var excelJsonModel = new ExcelJsonModel();
                foreach (var item in configJson.Children<JObject>())
                {
                    if (item.GetValue("MessageName").ToString() == messageName)
                    {
                        excelJsonModel.Container = item.GetValue("Container").ToString();
                        excelJsonModel.FileName = item.GetValue("FileName").ToString();
                        excelJsonModel.IntegrationType = item.GetValue("IntegrationType").ToString();
                        excelJsonModel.TemplateID = int.Parse(item.GetValue("TemplateID").ToString());
                        excelJsonModel.StagingTable = item.GetValue("TempTable").ToString();
                        excelJsonModel.MessageName = item.GetValue("MessageName").ToString();
                        allowToExeute = true;
                        break;
                    }
                }

                if (!allowToExeute)
                {
                    throw new Exception("Messagename is not match with ConfigJsonValue");
                }

                // if (excelJsonModel.IntegrationType.ToLower() == "f")
                // {
                //    connection.ExecuteScalar($"TRUNCATE {excelJsonModel.StagingTable}");
                // }
                var blobUrl = _blobRepo.GetLastModifiedFileUrl(excelJsonModel.FileName, excelJsonModel.Container);
                var path = await _blobRepo.DownloadFile(blobUrl);
                if ((bool)returnDataOnly)
                {
                    return path;
                }

                switch (messageName.ToLower())
                {
                    case "employeelist":
                        {
                            await this.GetExcelDataToEmployeeStaging(path, excelJsonModel);
                            break;
                        }

                    case "rmslist":
                        {
                            await this.GetExcelDataToRmsStaging(path, excelJsonModel);
                            break;
                        }

                    case "itemcodelist":
                        {
                            await this.GetExcelDataToItemCodeStaging(path, excelJsonModel);
                            break;
                        }

                    case "grnlist":
                        {
                            await this.GetGrnData(path, excelJsonModel);
                            break;
                        }

                    case "tolist":
                        {
                            await this.GetTOData(path, excelJsonModel);
                            break;
                        }

                    case "classlist":
                        {
                            await this.MasterDataUpload(path, 3);
                            break;
                        }

                    case "departmentlist":
                        {
                            await this.MasterDataUpload(path, 4);
                            break;
                        }

                    case "divisionlist":
                        {
                            await this.MasterDataUpload(path, 5);
                            break;
                        }

                    case "grouplist":
                        {
                            await this.MasterDataUpload(path, 6);
                            break;
                        }

                    case "subclasslist":
                        {
                            await this.MasterDataUpload(path, 7);
                            break;
                        }

                    case "brandlist":
                        {
                            await this.MasterDataUpload(path, 8);
                            break;
                        }

                    case "businesslist":
                        {
                            await this.MasterDataUpload(path, 9);
                            break;
                        }

                    case "merchandiselist":
                        {
                            await this.HierarchicalMaster(path, excelJsonModel);
                            break;
                        }

                    default:
                        {
                            throw new Exception("The message name was not found");
                        }
                }

                return null;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on ProcessDataFromTemplate : {e.Message}");
            }
        }

        public async Task Planocode()
        {
            using var connection = this.GetConnection();
            IDbCommand command = connection.CreateCommand();
            command.CommandText = "CALL public.planocodeupdate();";
            command.CommandTimeout = 300;

            // execute stored procedure
            command.ExecuteNonQuery();
        }

        public async Task<string> FindAppSettings(string configName)
        {
            var valueFromConfig = await _dbCache.FindAppSettings(configName);
            return valueFromConfig.Value;
        }

        /// <summary>
        /// Adding excel values into temp table and
        /// checking for duplicate values and adding tempDbtable to Appuser DbTable
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// 
        public async Task MasterDataUpload(string filePath, long masterId)
        {
            using var connection = this.GetConnection();
            var tempFile = filePath;
            var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
            var ws = wb.Worksheet(1);
            ws.Tables.Select(x => x.ShowAutoFilter = false);
            var dataTable = ws.Tables.Any() ? ws.Tables.First().AsNativeDataTable() : ws.RangeUsed().AsTable().AsNativeDataTable();
            var dataRows = dataTable.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field?.ToString())));
            string constr = connection.ConnectionString;
            using (var cn = new NpgsqlConnection(constr))
            {
                if (cn.State != ConnectionState.Open)
                {
                    cn.Open();
                }

                switch (masterId)
                {
                    case 3:

                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_class(@g_departmentcode,@g_classcode,@g_name,@g_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@g_name", dataRow["CLASS_NAME"]);
                            cmdd.Parameters.AddWithValue("@g_departmentcode", parameterType: NpgsqlDbType.Integer, value: dataRow["DEPT"]);
                            cmdd.Parameters.AddWithValue("@g_classcode", parameterType: NpgsqlDbType.Integer, value: dataRow["CLASS"]);
                            cmdd.Parameters.AddWithValue("@g_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 4:

                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_department(@g_departmentcode,@g_groupcode,@g_name,@g_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@g_name", dataRow["DEPT_NAME"]);
                            cmdd.Parameters.AddWithValue("@g_departmentcode", parameterType: NpgsqlDbType.Integer, value: dataRow["DEPT"]);
                            cmdd.Parameters.AddWithValue("@g_groupcode", parameterType: NpgsqlDbType.Integer, value: dataRow["GROUP_NO"]);
                            cmdd.Parameters.AddWithValue("@g_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 5:

                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_division(@p_name,@p_id,@p_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@p_name", dataRow["DIV_NAME"]);
                            cmdd.Parameters.AddWithValue("@p_id", parameterType: NpgsqlDbType.Integer, value: dataRow["DIVISION"]);
                            cmdd.Parameters.AddWithValue("@p_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 6:

                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_group(@g_divisioncode,@g_groupcode,@g_name,@g_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@g_name", dataRow["GROUP_NAME"]);
                            cmdd.Parameters.AddWithValue("@g_divisioncode", parameterType: NpgsqlDbType.Integer, value: dataRow["DIVISION"]);
                            cmdd.Parameters.AddWithValue("@g_groupcode", parameterType: NpgsqlDbType.Integer, value: dataRow["GROUP_NO"]);
                            cmdd.Parameters.AddWithValue("@g_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 7:
                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_subclass(@g_departmentcode,@g_classcode,@g_subclasscode,@g_name,@g_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@g_departmentcode", parameterType: NpgsqlDbType.Integer, value: dataRow["DEPT"]);
                            cmdd.Parameters.AddWithValue("@g_classcode", parameterType: NpgsqlDbType.Integer, value: dataRow["CLASS"]);
                            cmdd.Parameters.AddWithValue("@g_subclasscode", parameterType: NpgsqlDbType.Integer, value: dataRow["SUBCLASS"]);
                            cmdd.Parameters.AddWithValue("@g_name", dataRow["SUB_NAME"]);
                            cmdd.Parameters.AddWithValue("@g_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 8:
                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_brand(@p_name,@p_brandcode,@p_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@p_name", dataRow["BRAND_NAME"]);
                            cmdd.Parameters.AddWithValue("@p_brandcode", parameterType: NpgsqlDbType.Integer, value: dataRow["BARND"]);
                            cmdd.Parameters.AddWithValue("@p_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    case 9:

                        foreach (DataRow dataRow in dataRows)
                        {
                            var procName = "call public.sync_business(@p_name,@p_businesscode,@p_isactive);";
                            NpgsqlCommand cmdd = new NpgsqlCommand();
                            cmdd.Connection = cn;
                            cmdd.CommandText = procName;
                            cmdd.Parameters.AddWithValue("@p_name", dataRow["BUSINESS_NAME"]);
                            cmdd.Parameters.AddWithValue("@p_businesscode", parameterType: NpgsqlDbType.Integer, value: dataRow["BUSINESS"]);
                            cmdd.Parameters.AddWithValue("@p_isactive", dataRow["ACTIVE_IND"].ToString().ToUpper() == "A" ? true : false);
                            cmdd.ExecuteNonQuery();
                        }

                        break;

                    default:
                        throw new ArgumentException("Invalid Master Data");
                }
            }
        }

        private DataTable ConvertCsvToDataTable(string filePath)
        {
            try
            {
                DataTable dt = new DataTable();
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string[] headers = sr.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        dt.Columns.Add(header);
                    }

                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }

                        dt.Rows.Add(dr);
                    }
                }

                return dt;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on ConvertCsvToDataTable : {e.Message}");
            }
        }

        private DataTable ConvertExcelToDataTable(string filePath, bool isXlsx = false)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });
                        if (result != null && result.Tables.Count > 0)
                        {
                            return result.Tables[0];
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error on ConvertExcelToDataTable : {e.Message}");
            }
        }

        private DataTable ConvertFileToDataTable(string filePath)
        {
            try
            {
                string fileExtension = Path.GetExtension(filePath);
                switch (fileExtension.ToLower())
                {
                    case ".xlsx":
                        return ConvertExcelToDataTable(filePath, true);

                    case ".xls":
                        return ConvertExcelToDataTable(filePath);

                    case ".csv":
                        return ConvertCsvToDataTable(filePath);

                    default:
                        throw new Exception("File format not supports");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error on ConvertFileToDataTable : {e.Message}");
            }
        }

        private async Task<DataTable> GetExcelDataToEmployeeStaging(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var count = 0;
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                using var connection = this.GetConnection();
                var data = ConvertFileToDataTable(filePath);
                var templateMappingTable = await _dbCache.GetTemplateMappings(excelJsonModel.TemplateID);
                var keyValuePairs = templateMappingTable.Select(x => new KeyValuePair<string, string>(x.SourceColumn, x.DestinationColumn)).ToList();

                foreach (DataRow row in data.Rows)
                {
                    var model = new EmployeeStaging();
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        switch (templateMappingTable
                            .Where(x => x.SourceColumn == keyValuePair.Key)
                            .Select(x => x.Datatype)
                            .FirstOrDefault()
                            .ToString()
                            .ToLower())
                        {
                            case "string":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key].ToString());
                                    break;
                                }

                            case "integer":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, int.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "long":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, long.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "datetime":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, DateTime.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            default:
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key]);
                                    break;
                                }
                        }
                    }

                    model.UploadedFileName = "Employee";
                    model = await this.AddOrUpdate(model);
                }

                var newEmployeesData = await connection.QueryAsync<EmployeeStaging>("select * from employeestaging where email not in (select email from appuser)");
                var appUsers = (from a in newEmployeesData
                                select new HyperlinkEdit
                                {
                                    FirstName = a.FirstName,
                                    LastName = a.LastName,
                                    Email = a.Email,
                                    Mobile = a.PhoneNo,
                                }).ToList();

                foreach (var appUser in appUsers)
                {
                    await _userManager.AddOrUpdateUser(appUser, userId);
                }

                connection.ExecuteScalar("UPDATE appuser set isactive = false where email = (SELECT email from employeestaging where isremoved = TRUE)");
                return data;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetExcelDataToEmployeeStaging : {e.Message}");
            }
        }

        private async Task GetExcelDataToRmsStaging(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var action = this.GetAction<RmsStaging>();
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                using var connection = this.GetConnection();
                var data = ConvertFileToDataTable(filePath);

                var templateMappingTable = await _dbCache.GetTemplateMappings(excelJsonModel.TemplateID);
                var keyValuePairs = templateMappingTable.Select(x => new KeyValuePair<string, string>(x.SourceColumn, x.DestinationColumn)).ToList();

                foreach (DataRow row in data.Rows)
                {
                    var model = new RmsStaging();

                    foreach (var keyValuePair in keyValuePairs)
                    {
                        switch (templateMappingTable
                            .Where(x => x.SourceColumn == keyValuePair.Key)
                            .Select(x => x.Datatype)
                            .FirstOrDefault()
                            .ToString()
                            .ToLower())
                        {
                            case "string":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key].ToString());
                                    break;
                                }

                            case "integer":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, int.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "long":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, long.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "datetime":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, DateTime.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            default:
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key]);
                                    break;
                                }
                        }
                    }

                    var duplicate = await connection.QueryAsync<RmsStaging>($"select 1 from rmsstaging where uda_value = {model.UDA_VALUE}");

                    if (duplicate.Count() == 0)
                    {
                        model = await action.AddOrUpdate(model);
                    }

                    // IDbCommand command = connection.CreateCommand();
                    // command.CommandText = "CALL public.planocodeupdate();";

                    // execute stored procedure
                    // command.ExecuteNonQuery();
                    BackgroundJob.Enqueue(() => Planocode());
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task GetGrnData(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var action = this.GetAction<Grn>();
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                using var connection = this.GetConnection();
                var data = ConvertFileToDataTable(filePath);

                var templateMappingTable = await _dbCache.GetTemplateMappings(excelJsonModel.TemplateID);
                var keyValuePairs = templateMappingTable.Select(x => new KeyValuePair<string, string>(x.SourceColumn, x.DestinationColumn)).ToList();

                foreach (DataRow row in data.Rows)
                {
                    var model = new Grn();

                    foreach (var keyValuePair in keyValuePairs)
                    {
                        switch (templateMappingTable
                            .Where(x => x.SourceColumn == keyValuePair.Key)
                            .Select(x => x.Datatype)
                            .FirstOrDefault()
                            .ToString()
                            .ToLower())
                        {
                            case "string":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key].ToString());
                                    break;
                                }

                            case "integer":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, int.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "long":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, long.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "datetime":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, DateTime.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            default:
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key]);
                                    break;
                                }
                        }
                    }

                    var duplicate = await connection.QueryAsync<Grn>($"select 1 from grn where itemcode = {model.ItemCode}");
                    var itemduplicate = await connection.QueryAsync<ProductImage>($"select 1 from productimage where itemcode = {model.ItemCode}");

                    if (duplicate.Count() == 0)
                    {
                        model = await action.AddOrUpdate(model);
                        var todaydate = DateTime.Today;

                        var itemcode = await connection.QueryAsync<ItemCodeStaging>($"select it.code from itemcodestaging it join product p on p.uid = it.uidcode " +
                            $"where uidcolumndata is not null and buyer1columndata is not null and buyer2columndata is not null and " +
                            $"qccolumndata is not null and packagingcolumndata is not null and it.code = {model.ItemCode}");
                        var itemcodecheck = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>($"select * from itemcodestaging where code ={model.ItemCode}");
                        var photoshootProduct = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE itemcode = @ItemCode", new { ItemCode = model.ItemCode });
                        if (itemcodecheck is not null)
                        {
                            var itemcodeUidDiv = await connection.QueryFirstOrDefaultAsync<Product>($"select * from product where uid ='{itemcodecheck.UidCode}'");
                            if (itemcodeUidDiv is not null)
                            {
                                var divisionCodesString = itemcodeUidDiv.ColumnData["Division Code"].ToString();
                                long divisionCode = Convert.ToInt64(divisionCodesString);
                                if (itemcode.Count() == 1)
                                {
                                    if (itemduplicate.Count() == 0)
                                    {
                                        await connection.ExecuteAsync($"INSERT INTO PRODUCTIMAGE(itemcode,divisionid,grnstatus,productid,receiveddate,imagetype,producttostatus)VALUES({model.ItemCode},{divisionCode},'1',{photoshootProduct.Id},'{todaydate}','GRN','WIP')");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task GetTOData(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var action = this.GetAction<TransferOrder>();
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                using var connection = this.GetConnection();
                var data = ConvertFileToDataTable(filePath);

                var templateMappingTable = await _dbCache.GetTemplateMappings(excelJsonModel.TemplateID);
                var keyValuePairs = templateMappingTable.Select(x => new KeyValuePair<string, string>(x.SourceColumn, x.DestinationColumn)).ToList();

                foreach (DataRow row in data.Rows)
                {
                    var model = new TransferOrder();

                    foreach (var keyValuePair in keyValuePairs)
                    {
                        switch (templateMappingTable
                            .Where(x => x.SourceColumn == keyValuePair.Key)
                            .Select(x => x.Datatype)
                            .FirstOrDefault()
                            .ToString()
                            .ToLower())
                        {
                            case "string":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key].ToString());
                                    break;
                                }

                            case "integer":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, int.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "long":
                                {
                                    string value = row[keyValuePair.Key]?.ToString();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        model.SetPropertyValue(keyValuePair.Value, long.Parse(value));
                                    }

                                    break;
                                }

                            case "datetime":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, DateTime.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            default:
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key]);
                                    break;
                                }
                        }
                    }

                    var duplicate = await connection.QueryAsync<TransferOrder>($"select 1 from transferorder where itemcode = {model.ItemCode}");

                    if (duplicate.Count() == 0)
                    {
                        model = await action.AddOrUpdate(model);
                        var division = await connection.QueryFirstOrDefaultAsync<string>("SELECT divisionid from productimage WHERE itemcode  = @itemcode", new { model.ItemCode });
                        var studios1 = await connection.QueryAsync<Studio>(
    "SELECT priority\r\nFROM studio\r\nWHERE divisionid = CAST(@division AS INTEGER)\r\n  AND DATE(effectivefromdate) <= CURRENT_DATE\r\n  AND remainingcapacity!=0 AND isactive = true\r\nORDER BY\r\n  CASE\r\n    WHEN priority = 'P0' THEN 0\r\n    ELSE 1\r\n  END,\r\n  substring(priority from '^[^0-9]+')::text, \r\n  nullif(substring(priority from '[0-9]+')::int, 0) ASC;",
    new { division });

                        var product = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE Itemcode = @itemcode", new { model.ItemCode });
                        bool checking = false;
                        foreach (var studio in studios1)
                        {
                            var photoshootProduct = await connection.QueryFirstOrDefaultAsync<Studio>($"SELECT remainingcapacity as TotalCapacity,id FROM studio WHERE divisionid = CAST(@division AS INTEGER) AND priority = '{studio.Priority}' LIMIT 1", new { division });
                            var imageType = await connection.QueryFirstOrDefaultAsync<string>("SELECT Imagetype from productimage WHERE itemcode  = @itemcode", new { model.ItemCode });
                            if (photoshootProduct != null && photoshootProduct.TotalCapacity != 0)
                            {
                                string priority = string.Empty;
                                if (imageType == "Drape Image" || imageType == "Physical Sample" || imageType == "GRN")
                                {
                                    priority = "P1";
                                }
                                else
                                {
                                    priority = "P0";
                                }

                                var productstudio = await connection.QueryAsync<string>($"select 1 from productstudio where itemcode = {model.ItemCode}");
                                if (productstudio.Count() == 0)
                                {
                                    await connection.ExecuteAsync("INSERT INTO productstudio (productid, studioid, priority,datecreated,itemcode,allocationdate) VALUES (@productid, @studioid, @priority,@datecreate,@itemcode,@allocationdate)", new { productid = product.Id, studioid = photoshootProduct.Id, priority = priority, datecreate = DateTime.UtcNow, allocationdate = DateTime.UtcNow, model.ItemCode });
                                }

                                var remainingcapacity = photoshootProduct.TotalCapacity - 1;
                                await connection.ExecuteAsync($"Update studio set remainingcapacity = {remainingcapacity} where id = {photoshootProduct.Id}");
                                await connection.ExecuteAsync($"update productimage set grnstatus = '2',qcaction = 'true',samplestatus = 'Approved',camqcl1action = 'Received',camqcl1qualitycheck = 'Pass', camqcstatus = 2, photoshootstatus = 1, camqcl1qccheckdate = '{DateTime.UtcNow}', statusdate = '{DateTime.UtcNow}', allocationdate = '{DateTime.UtcNow}' where itemcode = {model.ItemCode}");
                                checking = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task GetExcelDataToItemCodeStaging(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var action = this.GetAction<ItemCodeStaging>();
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                using var connection = this.GetConnection();
                var data = ConvertFileToDataTable(filePath);
                var product = await connection.QueryAsync<Product>("select uid from product where status = '1'");
                var templateMappingTable = await _dbCache.GetTemplateMappings(excelJsonModel.TemplateID);
                var keyValuePairs = templateMappingTable.Select(x => new KeyValuePair<string, string>(x.SourceColumn, x.DestinationColumn)).ToList();
                var productUIDs = product.Select(p => p.Uid).ToList();

                var filteredRows = data.AsEnumerable().Where(row => productUIDs.Contains(row.Field<string>("UID_CODE"))).ToList();

                foreach (DataRow row in filteredRows)
                {
                    var model = new ItemCodeStaging();
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        switch (templateMappingTable
                           .Where(x => x.SourceColumn == keyValuePair.Key)
                           .Select(x => x.Datatype)
                           .FirstOrDefault()
                           .ToString()
                           .ToLower())
                        {
                            case "string":
                                {
                                    object propertyValueObject = row[keyValuePair.Key];
                                    string propertyValue = propertyValueObject?.ToString();
                                    if (string.IsNullOrEmpty(propertyValue))
                                    {
                                        model.SetPropertyValue(keyValuePair.Value, null);
                                    }
                                    else
                                    {
                                        model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key].ToString());
                                    }

                                    break;
                                }

                            case "integer":
                                {
                                    model.SetPropertyValue(keyValuePair.Value, int.Parse(row[keyValuePair.Key].ToString()));
                                    break;
                                }

                            case "long":
                                {
                                    object propertyValueObject = row[keyValuePair.Key];
                                    string propertyValue = propertyValueObject?.ToString();
                                    if (string.IsNullOrEmpty(propertyValue))
                                    {
                                        model.SetPropertyValue(keyValuePair.Value, null);
                                    }
                                    else
                                    {
                                        var valueString = row[keyValuePair.Key]?.ToString();
                                        if (decimal.TryParse(valueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal decimalValue))
                                        {
                                            long longValue = (long)decimalValue;
                                            model.SetPropertyValue(keyValuePair.Value, longValue);
                                        }

                                        // model.SetPropertyValue(keyValuePair.Value, long.Parse(row[keyValuePair.Key].ToString()));
                                    }

                                    break;
                                }

                            case "datetime":
                                {
                                    var dateString = row[keyValuePair.Key]?.ToString();
                                    DateTime? parsedDateTime = null;

                                    if (!string.IsNullOrEmpty(dateString))
                                    {
                                        DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss.f", null, System.Globalization.DateTimeStyles.None, out var tempParsedDateTime);
                                        parsedDateTime = tempParsedDateTime;
                                    }

                                    model.SetPropertyValue(keyValuePair.Value, parsedDateTime);
                                    break;
                                }

                            default:
                                {
                                    model.SetPropertyValue(keyValuePair.Value, row[keyValuePair.Key]);
                                    break;
                                }
                        }
                    }

                    if (model.Code != null && model.EanCode != null && model.SourceDateCreated != null)
                    {
                        var duplicate = await connection.QueryAsync<ItemCodeStaging>($"select 1 from itemcodestaging where uidcode = '{model.UidCode}'");

                        if (duplicate.Count() == 0)
                        {
                            model = await action.AddOrUpdate(model);
                        }
                        else
                        {
                            await connection.ExecuteAsync($"update itemcodestaging set code = {model.Code}, eancode = {model.EanCode}, sourcedatecreated = '{model.SourceDateCreated}' where uidcode = '{model.UidCode}'");
                        }
                    }

                    var itemcodeid = await connection.QueryAsync<ItemCode>("select * from itemcodestaging join product on uid=uidcode");
                    if (itemcodeid != null)
                    {
                        foreach (var itemcode in itemcodeid)
                        {
                            var query = $"SELECT uid, columndata " +
                              $"FROM product " +
                              $"WHERE uid = '{itemcode.UidCode}' limit 1";
                            var products = await connection.QueryAsync<Product>(query);

                            foreach (var productdetails in products)
                            {
                                var divisionCode = Convert.ToInt64(productdetails.ColumnData["Division Code"]);
                                var deptCode = Convert.ToInt64(productdetails.ColumnData["Dept Code"]);
                                if (divisionCode != null && deptCode != null)
                                {
                                    var marketplacetempIds = await connection.QueryAsync<int>(
                                        "SELECT id FROM marketplacetemplate WHERE divisionid = @DivisionId AND productdepartmentid = @DeptId",
                                        new { DivisionId = divisionCode, DeptId = deptCode });
                                    var marketplaceAttributeData = $"SELECT DISTINCT id, name " +
                                                                    $"FROM productattribute " +
                                                                    $"WHERE id IN (SELECT DISTINCT productattributeid FROM marketplacetemplateattribute " +
                                                                    $"WHERE datatype IS NOT NULL AND marketplaceattribute IS NOT NULL " +
                                                                    $"AND marketplacetemplateid = @Ids) " +
                                                                    $"ORDER BY id ASC";
                                    foreach (var id in marketplacetempIds)
                                    {
                                        var marketplaceAttributeNames = new List<string>();
                                        var result = await connection.QueryAsync<ProductAttribute>(
                                            marketplaceAttributeData, new { Ids = id });
                                        marketplaceAttributeNames.AddRange(result.Select(attr => attr.Name));
                                        var columnData = productdetails.ColumnData;
                                        var keysToSearch = columnData.Keys.ToList();

                                        // var allKeysFound = marketplaceAttributeNames.All(attrName =>
                                        // {
                                        //    if (!keysToSearch.Contains(attrName))
                                        //    {
                                        //        return false;
                                        //    }

                                        // var value = columnData[attrName];
                                        //    return !string.IsNullOrEmpty(value?.ToString());
                                        // });
                                        bool allKeysFound = marketplaceAttributeNames.Any() && marketplaceAttributeNames.All(attrName =>
                                        {
                                            if (!keysToSearch.Contains(attrName))
                                            {
                                                return false;
                                            }

                                            var value = columnData[attrName];
                                            return !string.IsNullOrEmpty(value?.ToString());
                                        });
                                        long statusFilter = allKeysFound ? 1 : 0;
                                        long itemid = Convert.ToInt64(itemcode.Id);
                                        await connection.ExecuteAsync(
                                    $"UPDATE product SET statusfilter = '{statusFilter}', itemcodeid = {itemid} " +
                                    $"WHERE uid = '{itemcode.UidCode}' ");
                                    }
                                }
                            }
                        }
                    }

                    var photoshoot = _productImageManager.UpdatePhotoshootProduct();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task HierarchicalMaster(string filePath, ExcelJsonModel excelJsonModel)
        {
            try
            {
                var valueFromConfig = await _dbCache.FindAppSettings("RecordCreatedOrUpdatedBy");
                var userId = int.Parse(valueFromConfig.Value);
                var data = ConvertFileToDataTable(filePath);
                using var connection = this.GetConnection();
                foreach (DataRow row in data.Rows)
                {
                    var merchandise = new Merchandise_hierarchy
                    {
                        Business = Convert.ToInt64(row["BUSINESS"]),
                        Business_name = row["BUSINESS_NAME"].ToString(),
                        Division = Convert.ToInt64(row["DIVISION"]),
                        Div_name = row["DIV_NAME"].ToString(),
                        Group_no = Convert.ToInt64(row["GROUP_NO"]),
                        Group_name = row["GROUP_NAME"].ToString(),
                        Dept = Convert.ToInt64(row["DEPT"]),
                        Dept_name = row["DEPT_NAME"].ToString(),
                        Class = Convert.ToInt32(row["CLASS"]),
                        Class_name = row["CLASS_NAME"].ToString(),
                        Subclass = Convert.ToInt32(row["SUBCLASS"]),
                        Sub_name = row["SUB_NAME"].ToString(),
                        Active_ind = row["ACTIVE_IND"].ToString().Trim().Equals("A", StringComparison.OrdinalIgnoreCase)
                    };

                    var sql = @"INSERT INTO merchandise_hierarchy (Business, Business_name, Division, Div_name, Group_no, Group_name, Dept, Dept_name, Class, Class_name, Subclass, Sub_name, Active_ind)
                        VALUES (@Business, @Business_name, @Division, @Div_name, @Group_no, @Group_name, @Dept, @Dept_name, @Class, @Class_name, @Subclass, @Sub_name, @Active_ind)";

                    var parameters = new
                    {
                        Business = merchandise.Business,
                        Business_name = merchandise.Business_name,
                        Division = merchandise.Division,
                        Div_name = merchandise.Div_name,
                        Group_no = merchandise.Group_no,
                        Group_name = merchandise.Group_name,
                        Dept = merchandise.Dept,
                        Dept_name = merchandise.Dept_name,
                        Class = merchandise.Class,
                        Class_name = merchandise.Class_name,
                        Subclass = merchandise.Subclass,
                        Sub_name = merchandise.Sub_name,
                        Active_ind = merchandise.Active_ind
                    };

                    var duplicate = await connection.QueryAsync<MerchandiseHierarchy>($"select 1 from merchandise_hierarchy where dept = {merchandise.Dept} and division = {merchandise.Division} and class = {merchandise.Class} and subclass = {merchandise.Subclass} and group_no = {merchandise.Group_no}");

                    if (duplicate.Count() == 0)
                    {
                        connection.Execute(sql, parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}