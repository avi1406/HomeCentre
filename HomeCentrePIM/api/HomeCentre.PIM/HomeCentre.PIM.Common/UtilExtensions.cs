using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using AgileObjects.AgileMapper;
using ClosedXML.Excel;
using CsvHelper;
using Dapper;
using ExcelMapper;
using HashidsNet;
using HomeCentre.PIM.Common.Cache;
using HomeCentre.PIM.Common.MetaAttributes;
using HomeCentre.PIM.Common.PostgresTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using NetBox.Extensions;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using Storage.Net.Blobs;
using KeyAttribute = System.ComponentModel.DataAnnotations.KeyAttribute;
using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;

namespace HomeCentre.PIM.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static class UtilExtensions
    {
        /// <summary>
        /// Adds to cache.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public static async Task AddToCache(this IDistributedCache handler, string key, object value)
        {
            var id = key;
            try
            {
                await Task.Run(() => handler.Set(id, value.ToBytes(), new DistributedCacheEntryOptions
                {
                    SlidingExpiration = new TimeSpan(6, 0, 0)
                })); // hard coded 6 hours caching
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void BulkCopyData(this IDbTransaction transaction, DataTable dt, string tableName,
            Dictionary<string, NpgsqlDbType> columnNamesAndTypes)
        {
            tableName = tableName.ToLower();
            if (!(transaction.Connection is NpgsqlConnection connection))
            {
                return;
            }

            if (dt == null)
            {
                return;
            }

            if (dt.Rows.Count < 1)
            {
                return;
            }

            var sbColumnNames = dt.Columns.Cast<DataColumn>().Select(x => $"{x.ColumnName.ToLower()}").ToList();
            var commandSql = string.Format(CultureInfo.InvariantCulture, "COPY {0} ({1}) FROM STDIN BINARY",
                tableName, string.Join(",", sbColumnNames));

            using (var writer = connection.BeginBinaryImport(commandSql))
            {
                foreach (DataRow row in dt.Rows)
                {
                    writer.StartRow();
                    foreach (DataColumn column in dt.Columns)
                    {
                        var columnName = column.ColumnName.ToLower();
                        var value = row[columnName];
                        var type = columnNamesAndTypes[columnName];
                        if ((type == NpgsqlDbType.Varchar || type == NpgsqlDbType.Char || type == NpgsqlDbType.Text) &&
                            value.ToString().Contains("\u0000"))
                        {
                            value = value.ToString().Replace("\u0000", string.Empty);
                            if (string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                value = null;
                            }
                        }

                        if (value == null || value == DBNull.Value)
                        {
                            writer.WriteNull();
                            continue;
                        }

                        writer.Write(row[columnName], columnNamesAndTypes[columnName]);
                    }
                }

                writer.Complete();
            }
        }

        public static byte[] ConvertToByteArray(this string value)
        {
            byte[] bytes = null;
            if (string.IsNullOrEmpty(value))
            {
                bytes = null;
            }
            else
            {
                var stringLength = value.Length;
                var characterIndex =
                    value.StartsWith("0x", StringComparison.Ordinal)
                        ? 2
                        : 0; // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                var numberOfCharacters = stringLength - characterIndex;

                var addLeadingZero = false;
                if ((numberOfCharacters % 2) != 0)
                {
                    addLeadingZero = true;

                    numberOfCharacters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[numberOfCharacters / 2]; // Initialize our byte array to hold the converted string.

                var writeIndex = 0;
                if (addLeadingZero)
                {
                    bytes[writeIndex++] = FromCharacterToByte(value[characterIndex], characterIndex);
                    characterIndex += 1;
                }

                for (var readIndex = characterIndex; readIndex < value.Length; readIndex += 2)
                {
                    var upper = FromCharacterToByte(value[readIndex], readIndex, 4);
                    var lower = FromCharacterToByte(value[readIndex + 1], readIndex + 1);

                    bytes[writeIndex++] = (byte)(upper | lower);
                }
            }

            return bytes;
        }

        /// <summary>
        /// Decrypts the specified key.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string Decrypt(this string input, string key)
        {
            var cipherBytes = Convert.FromBase64String(input);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(key,
                    new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }

                    input = Encoding.Unicode.GetString(ms.ToArray());
                }
            }

            return input;
        }

        /// <summary>
        /// Generic  Deserialize
        /// </summary>
        /// <typeparam name="T">The Type</typeparam>
        /// <param name="xmlText">The XML Text</param>
        /// <returns>
        /// deserialize of any generic type function
        /// </returns>
        public static T Deserialize<T>(this string xmlText)
        {
            var stringReader = new System.IO.StringReader(xmlText);
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stringReader);
        }

        public static IEnumerable<DateTime> EachDay(this DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            {
                yield return day;
            }
        }

        public static object ToFirstLetterUpperCase(this object value)
        {
            return value switch
            {
                null => null,
                string {Length: 1} strValue => char.ToUpper(strValue[0]).ToString(),
                string strValue => char.ToUpper(strValue[0]) + strValue[1..],
                _ => null
            };
        }

        /// <summary>
        /// Encrypts the specified key.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string Encrypt(this string input, string key)
        {
            var clearBytes = Encoding.Unicode.GetBytes(input);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(key,
                    new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }

                    input = Convert.ToBase64String(ms.ToArray());
                }
            }

            return input;
        }

        public static string FormatNumber(long value)
        {
            if (value >= 1000000000)
            {
                return (value / 1000000000D).ToString("0.##") + "B";
            }

            if (value >= 1000000)
            {
                return (value / 1000000D).ToString("0.##") + "M";
            }

            if (value >= 1000)
            {
                return (value / 1000D).ToString("0.##") + "K";
            }

            return value.ToString("#,0");
        }

        /// <summary>
        /// Generates the random password.
        /// </summary>
        /// <param name="opts">The opts.</param>
        /// <returns></returns>
        public static string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null)
            {
                opts = new PasswordOptions()
                {
                    RequiredLength = 8,
                    RequiredUniqueChars = 4,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireNonAlphanumeric = true,
                    RequireUppercase = true
                };
            }

            string[] randomChars =
            {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ", // uppercase
                "abcdefghijkmnopqrstuvwxyz", // lowercase
                "0123456789", // digits
                "!@$?_-" // non-alphanumeric
            };
            var rand = new Random(Environment.TickCount);
            var chars = new List<char>();

            if (opts.RequireUppercase)
            {
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);
            }

            if (opts.RequireLowercase)
            {
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);
            }

            if (opts.RequireDigit)
            {
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);
            }

            if (opts.RequireNonAlphanumeric)
            {
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);
            }

            for (var i = chars.Count;
                 i < opts.RequiredLength
                 || chars.Distinct().Count() < opts.RequiredUniqueChars;
                 i++)
            {
                var rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        public static string GetClaim(this ClaimsPrincipal principal, string claimName)
        {
            return principal?.Claims.FirstOrDefault(x => x.Type == claimName)?.Value;
        }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetColumns(this Type t)
        {
            return t.GetProperties().Select(p => p.Name);
        }

        public static DataTable GetDataTableFromObjects(this object[] objects, string tableName = "")
        {
            if (objects == null || objects.Length <= 0)
            {
                return null;
            }

            var t = objects[0].GetType();
            var isAnonymous = CheckIfAnonymousType(t);
            if (!isAnonymous)
            {
                tableName = t.Name;
            }

            var dt = new DataTable(tableName);
            foreach (var pi in t.GetProperties())
            {
                if (!pi.GetCustomAttributes(typeof(IgnoreInsertAttribute), false).Any())
                {
                    if (pi.SetMethod != null || isAnonymous)
                    {
                        dt.Columns.Add(new DataColumn(pi.Name.ToLower(),
                            Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType));
                    }
                }
            }

            foreach (var o in objects)
            {
                var dr = dt.NewRow();
                foreach (var pi in t.GetProperties())
                {
                    if (!pi.GetCustomAttributes(typeof(IgnoreInsertAttribute), false).Any())
                    {
                        if (pi.SetMethod != null || isAnonymous)
                        {
                            dr[pi.Name.ToLower()] = o.GetType().GetProperty(pi.Name).GetValue(o, null) ?? DBNull.Value;
                        }
                    }
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }

        public static string GetEmailHost(this string email)
        {
            var address = new MailAddress(email);
            return address.Host.ToUpper();
        }

        public static T GetEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// Gets the env string.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="envName">Name of the env.</param>
        /// <returns></returns>
        public static string GetEnvString(this IConfiguration configuration, string envName)
        {
            return
                $"{configuration["Application:Name"]}-{envName}";
        }

        /// <summary>
        /// Gets from cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="key">The key.</param>
        /// <param name="action">The action.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static async Task<T> GetFromCache<T>(this IDistributedCache handler, string key,
            Func<object[], T> action, params object[] args)
        {
            var result = await handler.GetAsync(key);
            if (result != null)
            {
                return result.ToObject<T>();
            }

            var value = action(args);
            if (value != null)
            {
                await handler.AddToCache(key, value);
            }

            return value;
        }

        public static long GetId(this Blob blob)
        {
            if (blob == null || !blob.Properties.ContainsKey("id"))
            {
                return 0;
            }

            return long.TryParse(blob.Properties["id"].ToString(), out var id) ? id : 0;
        }

        /// <summary>
        /// Gets the search configuration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchKey">The search key.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageCount">The page count.</param>
        /// <param name="filterCondition">The filter condition.</param>
        /// <param name="filterParams">The filter parameters.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="orderByAsc">if set to <c>true</c> [order by asc].</param>
        /// <returns></returns>
        /// <exception cref="AccessViolationException">Looks like something out of ordinary.</exception>
        /// <exception cref="MissingKeyException"></exception>
        public static (string CountQuery, string SelectQuery, DynamicParameters Parameters) GetSearchConfig<T>(
            this string searchKey, int pageNumber, int pageCount,
            string filterCondition = null, DynamicParameters filterParams = null, string orderBy = "",
            bool orderByAsc = true)
        {
            var columns = typeof(T).GetColumns();
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                var orderBys = orderBy.Split(',');
                foreach (var order in orderBys)
                {
                    if (!string.IsNullOrEmpty(order) && !columns.Any(x =>
                            string.Equals(x, order, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new AccessViolationException("Looks like something out of ordinary.");
                    }
                }
            }

            var keys = typeof(T).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(KeyAttribute))).Select(x => x.Name).ToList();
            if (!keys.Any())
            {
                throw new KeyNotFoundException();
            }

            if (string.IsNullOrEmpty(orderBy))
            {
                orderBy = keys.FirstOrDefault();
            }

            var tableName = typeof(T).Name;
            var tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null)
            {
                tableName = tableAttribute.Name;
            }

            var searchFieldAttribute =
                typeof(T).GetCustomAttribute<SearchFieldAttribute>();
            var searchFilter = new StringBuilder();
            if (filterParams == null)
            {
                filterParams = new DynamicParameters();
            }

            if (searchFieldAttribute != null && searchFieldAttribute.FieldNames.Any() &&
                !string.IsNullOrWhiteSpace(searchKey))
            {
                searchFilter.Append(" WHERE ");
                searchFilter.Append(
                    $"(LOWER({searchFieldAttribute.FieldNames[0]}) LIKE @{searchFieldAttribute.FieldNames[0]}");
                filterParams.Add(searchFieldAttribute.FieldNames[0], $"%{searchKey.ToLower()}%");
                foreach (var searchField in searchFieldAttribute.FieldNames.Skip(1))
                {
                    searchFilter.Append($" OR LOWER({searchField}) LIKE @{searchField}");
                    filterParams.Add(searchField, $"%{searchKey.ToLower()}%");
                }

                searchFilter.Append(")");
                if (!string.IsNullOrWhiteSpace(filterCondition))
                {
                    searchFilter.Append(" and ");
                    searchFilter.Append(filterCondition);
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
            var query =
                $"SELECT * FROM {tableName} {searchFilter} ORDER BY {orderBy} {orderByDirection} OFFSET @OffSet ROWS FETCH NEXT @Limit ROWS ONLY;";
            var countQuery =
                $"SELECT count(*) FROM {tableName}{searchFilter}";

            filterParams.Add("@Limit", pageCount);
            filterParams.Add("@OffSet", pageCount * (pageNumber - 1));
            return (countQuery, query, filterParams);
        }

        public static string GetSelectStatement<T>(this T t, string tableOrViewName)
        {
            var columnNames = t.GetType().GetProperties()
                .Select(x => x.Name);
            return $"select {string.Join(",", columnNames)} from {tableOrViewName}";
        }

        public static string GetString<T>(this T t)
            where T : Enum
        {
            return Enum.GetName(typeof(T), t);
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static JwtSecurityToken GetToken(this HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Authorization"))
            {
                return null;
            }

            var authHeader = request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var tokenStr = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            if (!(handler.ReadToken(tokenStr) is JwtSecurityToken token))
            {
                throw new UnauthorizedAccessException();
            }

            return token;
        }

        /// <summary>
        /// Gets the update statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">The t.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="keyColumnName">Name of the key column.</param>
        /// <returns></returns>
        public static string GetUpdateStatement<T>(this T t, string tableName, string keyColumnName)
        {
            tableName = tableName.ToLower();
            var columnNames = t.GetType().GetProperties()
                .Where(x => !string.Equals(x.Name, keyColumnName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Name);
            var sb = new StringBuilder($"update {tableName} set ");
            foreach (var columnName in columnNames)
            {
                sb.Append($"{columnName.ToLower()} = @{columnName},");
            }

            sb.Length--;
            sb.Append($" WHERE {keyColumnName.ToLower()} = @{keyColumnName}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException">
        /// </exception>
        public static int? GetUserId(this HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Authorization"))
            {
                return null;
            }

            var authHeader = request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var tokenStr = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            if (!(handler.ReadToken(tokenStr) is JwtSecurityToken token))
            {
                throw new UnauthorizedAccessException();
            }

            var subjectId = token.Claims.FirstOrDefault(x => x.Type == "subject_id")?.Value;
            if (subjectId == null || !int.TryParse(subjectId, out var a))
            {
                throw new UnauthorizedAccessException();
            }

            return int.Parse(subjectId);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string GetValue(this IConfiguration configuration, string key)
        {
            return configuration?[$"AppSettings:{key}"];
        }

        /// <summary>
        /// Hashes it.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns></returns>
        public static string HashIt(this int val)
        {
            var hashIds = new Hashids("b-i-z-t-o-o-l-b-o-x");
            return hashIds.Encode(val);
        }

        public static bool IsDevOrQa(this string env)
        {
            return env == "Development" || env == "Testing" || env == "Staging" || env == "Production" || env == "Support";
        }

        /// <summary>
        /// Determines whether this instance is nullable.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>
        ///   <c>true</c> if the specified t is nullable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullable(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static ExcelRangeBase LoadFromCollectionFiltered<T>(this ExcelRangeBase @this, IEnumerable<T> collection,
            bool printHeaders)
            where T : class
        {
            var membersToInclude = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !Attribute.IsDefined(p, typeof(ExcelIgnoreAttribute)))
                .ToArray();

            return @this.LoadFromCollection<T>(collection, printHeaders,
                OfficeOpenXml.Table.TableStyles.None,
                BindingFlags.Instance | BindingFlags.Public,
                membersToInclude);
        }

        public static void LoadMasterTableUsingMerge(this IDbTransaction transaction, string tableName,
            List<object> masterData, string[] keyColumns = null)
        {
            keyColumns ??= new[] {"Id"};
            var pgConnection = transaction.Connection as NpgsqlConnection;
            if (pgConnection == null)
            {
                return;
            }

            var addIdentityColumn = keyColumns.Any(x => x.ToLower() == "id");
            var tempTableName = $"temp_{tableName}_{Guid.NewGuid().ToString().Replace('-', '_')}";
            var dt = masterData.ToArray().GetDataTableFromObjects(tableName);
            var dtColumnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower()).ToList();
            var mergeColumns = addIdentityColumn
                ? dtColumnNames.ToList()
                : dtColumnNames.Where(x => x != "id").ToList();
            var updateSetSql = string.Join(",", mergeColumns.Select(x => $"\"{x}\" = EXCLUDED.\"{x}\""));
            var insertSetSql = string.Join(",", mergeColumns);
            var valuesSetSql = string.Join(",", dtColumnNames.Select(x => $"t.{x}"));
            var columns = pgConnection.GetSchema("Columns", new[] {pgConnection.Database, "public", tableName});
            var createColumns = string.Join(",",
                columns.Rows.Cast<DataRow>().Select(row => $"\"{row["column_name"]}\" {row["data_type"]}"));
            var createSql = $"CREATE TEMPORARY TABLE {tempTableName}({createColumns})";
            transaction.Connection.Execute(createSql);
            var columnNamesAndTypes = dtColumnNames.ToDictionary(x => x,
                x => DbTypeMapping.NpgsqlDbTypesMapping[
                    columns.Rows.Cast<DataRow>()
                        .FirstOrDefault(row => row["column_name"].ToString() == x)?["data_type"]
                        .ToString() ??
                    throw new PostgresException($"{x}-Column not found", "Critical", "Critical", "None")]);
            transaction.BulkCopyData(dt, tempTableName, columnNamesAndTypes);
            Console.WriteLine(tempTableName);
            var mergeSql = @$"INSERT into {tableName} ({insertSetSql})
                            SELECT {insertSetSql} FROM {tempTableName} S
                                ON CONFLICT ({string.Join(",", keyColumns)}) 
                            DO UPDATE SET {updateSetSql}";
            transaction.Connection.Execute(mergeSql);
        }

        public static bool IsNumeric(this string value)
        {
            return double.TryParse(value, out _);
        }

        public static async Task<string> AsTempFile(this DataTable dt, string format,
            Dictionary<string, string> properties = null, List<string[]>? myList = null, List<int>? cells = null)
        {
            var tempFile = Path.GetTempFileName();
            switch (format.ToLower())
            {
                case "xlsx":
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add(dt);
                    if (ws.Tables.Any())
                    {
                        ws.Tables.Table(0).Theme = XLTableTheme.TableStyleLight9;
                        ws.Tables.Table(0).ShowAutoFilter = false;
                        ws.Tables.Table(0).SetShowAutoFilter(false); 
                            
                        /*Adding DropDown Selectoin to Exce*/ 
                        if (myList != null && cells != null)
                        {
                            var poiWorkbook = new XSSFWorkbook();
                            var poiSheet = (XSSFSheet)poiWorkbook.CreateSheet("Sheet1");

                            int rowIndex = 0;
                            foreach (var row in ws.Rows())
                            {
                                var poiRow = (XSSFRow)poiSheet.CreateRow(rowIndex);

                                int columnIndex = 0;
                                foreach (var cell in row.Cells())
                                {
                                    var poiCell = (XSSFCell)poiRow.CreateCell(columnIndex);
                                    poiCell.SetCellValue(cell.Value.ToString());

                                    var poiStyle = (XSSFCellStyle)poiWorkbook.CreateCellStyle();
                                    poiStyle.CloneStyleFrom((XSSFCellStyle)poiWorkbook.GetCellStyleAt(0));
                                    poiCell.CellStyle = poiStyle;

                                    // Set the header cell fill color
                                    if (rowIndex == 0)
                                    {
                                        poiStyle.FillForegroundColor = IndexedColors.LightBlue.Index;
                                        poiStyle.FillPattern = FillPattern.SolidForeground;
                                        var headerFont = (XSSFFont)poiWorkbook.CreateFont();
                                        headerFont.Color = IndexedColors.White.Index;
                                        poiStyle.SetFont(headerFont);
                                        poiStyle.GetFont().IsBold = true;
                                    }

                                    columnIndex++;
                                }

                                rowIndex++;
                            }

                            for (var i = 0; i < myList.Count; i++)
                            {                                    
                                IDataValidationHelper validationHelper = new XSSFDataValidationHelper(poiSheet);
                                CellRangeAddressList addressList = new CellRangeAddressList(1, 100, cells[i], cells[i]);
                                IDataValidationConstraint constraint = validationHelper.CreateExplicitListConstraint(myList[i]);
                                IDataValidation dataValidation = validationHelper.CreateValidation(constraint, addressList);
                                dataValidation.SuppressDropDownArrow = true;
                                poiSheet.AddValidationData(dataValidation);
                        
                                using var fileStream = new FileStream(tempFile, FileMode.Create);
                                poiWorkbook.Write(fileStream);
                            }

                            if (properties != null)
                            {
                                foreach (var property in properties)
                                {
                                    wb.CustomProperties.Add(property.Key, property.Value);
                                }
                            }

                            return tempFile;
                        }
                    }

                    using var stream = new MemoryStream();
                    wb.SaveAs(stream);
                    
                    await using var fs = File.OpenWrite(tempFile);
                    stream.Position = 0;
                    await stream.CopyToAsync(fs);
                    break;
                }

                case "csv":
                {
                    await using var writer = new StringWriter();
                    await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    foreach (DataColumn dc in dt.Columns)
                    {
                        csv.WriteField(dc.ColumnName);
                    }

                    csv.NextRecord();

                    foreach (DataRow dr in dt.Rows)
                    {
                        foreach (DataColumn dc in dt.Columns)
                        {
                            csv.WriteField(dr[dc]);
                        }

                        csv.NextRecord();
                    }

                    await File.WriteAllTextAsync(tempFile, writer.ToString());
                    break;
                }
            }

            return tempFile;
        }

        public static void BulkInsert(this IDbTransaction transaction, string tableName,
            DataTable dt)
        {
            tableName = tableName.ToLower();
            var pgConnection = transaction.Connection as NpgsqlConnection;
            if (pgConnection == null)
            {
                return;
            }

            if (dt.Columns.Contains("id"))
            {
                dt.Columns.Remove("id");
            }

            var dtColumnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower()).ToList();
            var columns = pgConnection.GetSchema("Columns", new[] {pgConnection.Database, "public", tableName});
            var columnNamesAndTypes = dtColumnNames.ToDictionary(x => x,
                x => DbTypeMapping.NpgsqlDbTypesMapping[
                    columns.Rows.Cast<DataRow>()
                        .FirstOrDefault(row => row["column_name"].ToString() == x)?["data_type"]
                        .ToString() ??
                    throw new PostgresException($"{x}-Column not found", "Critical", "Critical", "None")]);
            transaction.BulkCopyData(dt, tableName, columnNamesAndTypes);
        }

        public static void BulkMerge(this IDbTransaction transaction, string tableName,
            DataTable dt, string[] keyColumns = null)
        {
            tableName = tableName.ToLower();
            keyColumns ??= new[] {"Id"};
            var pgConnection = transaction.Connection as NpgsqlConnection;
            if (pgConnection == null)
            {
                return;
            }

            var addIdentityColumn = keyColumns.Any(x => x.ToLower() == "id");
            var tempTableName = $"temp_{tableName}_{Guid.NewGuid().ToString().Replace('-', '_')}";
            var dtColumnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower()).ToList();
            var mergeColumns = addIdentityColumn
                ? dtColumnNames.ToList()
                : dtColumnNames.Where(x => x != "id").ToList();
            var updateSetSql = string.Join(",", mergeColumns.Select(x => $"{x} = EXCLUDED.{x}"));
            var insertSetSql = string.Join(",", mergeColumns);
            var valuesSetSql = string.Join(",", dtColumnNames.Select(x => $"t.{x}"));
            var columns = pgConnection.GetSchema("Columns", new[] {pgConnection.Database, "public", tableName});
            var createColumns = string.Join(",",
                columns.Rows.Cast<DataRow>().Select(row => $"{row["column_name"]} {row["data_type"]}"));
            var createSql = $"CREATE TEMPORARY TABLE {tempTableName}({createColumns})";
            transaction.Connection.Execute(createSql);
            var columnNamesAndTypes = dtColumnNames.ToDictionary(x => x,
                x => DbTypeMapping.NpgsqlDbTypesMapping[
                    columns.Rows.Cast<DataRow>()
                        .FirstOrDefault(row => row["column_name"].ToString() == x)?["data_type"]
                        .ToString() ??
                    throw new PostgresException($"{x}-Column not found", "Critical", "Critical", "None")]);
            transaction.BulkCopyData(dt, tempTableName, columnNamesAndTypes);
            Console.WriteLine(tempTableName);
            var mergeSql = @$"INSERT into {tableName} ({insertSetSql})
                            SELECT {insertSetSql} FROM {tempTableName} S
                                ON CONFLICT ({string.Join(",", keyColumns)}) 
                            DO UPDATE SET {updateSetSql}";
            transaction.Connection.Execute(mergeSql);
        }

        public static async Task BulkUpdate(this IDbTransaction transaction, string tableName,
            DataTable dt, string[] keyColumns = null)
        {
            keyColumns ??= new[] {"Id"};
            var pgConnection = transaction.Connection as NpgsqlConnection;
            if (pgConnection == null)
            {
                return;
            }

            var addIdentityColumn = keyColumns.Any(x => x.ToLower() == "id");
            var tempTableName = $"temp_{tableName}_{Guid.NewGuid().ToString().Replace('-', '_')}";
            var dtColumnNames = dt.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower()).ToList();
            var mergeColumns = addIdentityColumn
                ? dtColumnNames.ToList()
                : dtColumnNames.Where(x => x != "id").ToList();
            var updateSetSql = string.Join(",", mergeColumns.Select(x => $"{x} = t.{x}"));
            var insertSetSql = string.Join(",", mergeColumns);
            var valuesSetSql = string.Join(",", dtColumnNames.Select(x => $"t.{x}"));
            var columns = pgConnection.GetSchema("Columns", new[] {pgConnection.Database, "public", tableName});
            var createColumns = string.Join(",",
                columns.Rows.Cast<DataRow>().Select(row => $"{row["column_name"]} {row["data_type"]}"));
            var createSql = $"CREATE TEMPORARY TABLE {tempTableName}({createColumns})";
            transaction.Connection.Execute(createSql);
            var columnNamesAndTypes = dtColumnNames.ToDictionary(x => x,
                x => DbTypeMapping.NpgsqlDbTypesMapping[
                    columns.Rows.Cast<DataRow>()
                        .FirstOrDefault(row => row["column_name"].ToString() == x)?["data_type"]
                        .ToString() ??
                    throw new PostgresException($"{x}-Column not found", "Critical", "Critical", "None")]);
            transaction.BulkCopyData(dt, tempTableName, columnNamesAndTypes);
            Console.WriteLine(tempTableName);
            var whereClause = string.Join(" and ", keyColumns.Select(x => $"v.{x} = t.{x}"));
            var mergeSql =
                @$"UPDATE {tableName} as v SET {updateSetSql} from {tempTableName} as t where {whereClause}";
            await transaction.Connection.ExecuteAsync(mergeSql);
        }

        public static string MakeValidFileName(this string name)
        {
            var invalidChars =
                System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        public static string[] MergeHiddenArrays(this string[] array)
        {
            var updatedStatus = new List<string>();
            if (array == null)
            {
                return updatedStatus.ToArray();
            }

            foreach (var status in array)
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    continue;
                }

                var trimmedStatus = status.Trim();
                if (trimmedStatus.StartsWith("[") && trimmedStatus.EndsWith("]"))
                {
                    var statusList = JsonConvert.DeserializeObject<string[]>(trimmedStatus);
                    updatedStatus.AddRange(statusList);
                }
                else
                {
                    updatedStatus.Add(trimmedStatus);
                }
            }

            return updatedStatus.ToArray();
        }

        public static void Over(this object o, object to)
        {
            if (to == null || o == null)
            {
                return;
            }

            Mapper.Map(o).Over(to);
        }

        /// <summary>
        /// Hashes it.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns></returns>
        public static string RemoveNonAlphaNumericCharacters(this string val)
        {
            var rgx = new Regex("[^a-zA-Z0-9]");
            return rgx.Replace(val, string.Empty)
                .Replace("\t", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty).Trim();
        }

        /// <summary>
        /// Generic Serialize
        /// </summary>
        /// <typeparam name="T">The Type</typeparam>
        /// <param name="dataToSerialize">The Data to be Serialized</param>
        /// <returns>
        /// &gt;serialize of any generic type function
        /// </returns>
        public static string Serialize<T>(this T dataToSerialize)
        {
            var stringwriter = new System.IO.StringWriter();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stringwriter, dataToSerialize);
            return stringwriter.ToString();
        }

        /// <summary>
        /// To the specified o.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o">The o.</param>
        /// <returns></returns>
        public static T To<T>(this object o)
        {
            var t = Activator.CreateInstance<T>();
            Mapper.Map(o).Over(t);
            return t;

            // return Mapper.Map(o).ToANew<T>();
        }

        public static byte[] ToBytes<T>(this T obj)
        {
            if (obj == null)
            {
                return null;
            }

            var rawJson = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new AllPropertiesResolver()
            });
            return Encoding.UTF8.GetBytes(rawJson);
        }

        public static DataTable ToDataTable<T>(this T[] items)
        {
            var dataTable = new DataTable(typeof(T).Name);

            // Get all the properties
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Setting column names as Property names
                dataTable.Columns.Add(field.Name);
            }

            // Get all the properties
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                // Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }

            foreach (var item in items)
            {
                var values = new object[fields.Length + props.Length];
                for (var i = 0; i < fields.Length; i++)
                {
                    // inserting field values to datatable rows
                    values[i] = fields[i].GetValue(item);
                }

                for (var i = fields.Length; i < fields.Length + props.Length; i++)
                {
                    // inserting property values to datatable rows
                    values[i] = props[i].GetValue(item);
                }

                dataTable.Rows.Add(values);
            }

            // put a breakpoint here and check datatable
            return dataTable;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ToObject<T>(this byte[] data)
        {
            if (data == null)
            {
                return default(T);
            }

            var rawValues = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(rawValues, new JsonSerializerSettings
            {
                ContractResolver = new AllPropertiesResolver()
            });
        }

        /// <summary>
        /// To the query string.
        /// </summary>
        /// <param name="nvc">The NVC.</param>
        /// <returns></returns>
        public static string ToQueryString(this NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                    from value in nvc.GetValues(key)
                    select $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}")
                .ToArray();
            return "?" + string.Join("&", array);
        }

        /// <summary>
        /// Tvalue enum cast integer value
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public static int ToValue<TValue>(this TValue value)
            where TValue : Enum
            => (int)(object)value;

        public static string UpdateConnectionString(this string connectionString, bool ignoreHostInUserName)
        {
            var builder = new NpgsqlConnectionStringBuilder()
            {
                ConnectionString = connectionString
            };
            builder.Username = builder.Username?.Split('@')[0];
            if (!ignoreHostInUserName && (!builder.Username?.Contains("@") ?? false))
            {
                var host = builder.Host?.Split('.')[0];
                builder.Username = $"{builder.Username}@{host}";
            }

            return builder.ConnectionString;
        }

        public static bool IsValidEmail(this string emailaddress)
        {
            try
            {
                var m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string ToUniqueFileNameUrl(this string fileName)
        {
            var pattern = "[^a-zA-Z0-9]";
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            var res = fileName.Split('.');

            return Regex.Replace(res[0], pattern, "_") + "_" + RandomString(3) + "_" + GetTimestampString("_") + "." +
                   res[res.Length - 1];
        }

        public static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetFileExtensionFromUrl(this string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : string.Empty;
        }

        private static string GetTimestampString(string relChar)
        {
            return @DateTime.Now.ToString($"yyyy{relChar}MM{relChar}dd{relChar}HH{relChar}mm{relChar}sss");
        }

        private static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            var value = (byte)character;
            if (((value > 0x40) && (value < 0x47)) || ((value > 0x60) && (value < 0x67)))
            {
                if ((0x40 & value) == 0x40)
                {
                    if ((0x20 & value) == 0x20)
                    {
                        value = (byte)(((value + 0xA) - 0x61) << shift);
                    }
                    else
                    {
                        value = (byte)(((value + 0xA) - 0x41) << shift);
                    }
                }
            }
            else if ((value > 0x29) && (value < 0x40))
            {
                value = (byte)((value - 0x30) << shift);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Character '{character}' at index '{index}' is not valid alphanumeric character.");
            }

            return value;
        }

        private static bool CheckIfAnonymousType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }
    }
}