using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AgileObjects.AgileMapper;
using Azure.Storage.Blobs;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.App;
using HomeCentre.PIM.BAL.Model.Dashboard;
using HomeCentre.PIM.BAL.Model.Email;
using HomeCentre.PIM.BAL.Model.Tenant;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.DomainModel;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using Renci.SshNet;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgUserManager : PgEntityAction<AppUser>, IUserManager
    {
        private readonly IUserContext _userContext;

        // private readonly IDbConnection _connection;
        private readonly IDbCache _cache;

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _factory;
        private readonly ICipherService _cipherService;
        private readonly IJsonSerializer _serializer;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IMailEngine _mailEngine;
        private readonly IWorkFlowManager _taskStepInstance;

        public PgUserManager(IUserContext userContext,
            IDbConnectionFactory connectionFactory,
            IDbCache cache,
            IConfiguration configuration,
            IHttpClientFactory factory,
            ICipherService cipherService,
            IJsonSerializer serializer,
            IAuthenticationProvider authenticationProvider,
            IWorkFlowManager taskStepInstance,
            IMailEngine mailEngine)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _cache = cache;
            _configuration = configuration;
            _factory = factory;
            _cipherService = cipherService;
            _serializer = serializer;
            _authenticationProvider = authenticationProvider;
            _mailEngine = mailEngine;
            _taskStepInstance = taskStepInstance;

            // _connection = connectionFactory.GetAppDbConnection();
            // _connection.Open();
        }

        public async Task UpdateLastLoginHistory(long userId)
        {
            using var connection = this.GetConnection();
            var sql = @"select AppUserId,
                                       DateCreated as lastlogindate,
                                       case
                                           when GeoJson -> 'results' -> (0) ->> 'components' is null then
                                                   (GeoIpJson ->> 'region_name')::text || ', ' || (GeoIpJson ->> 'country_name')::text
                                           else (GeoJson -> 'results' -> (0) -> 'components' ->> 'state')::text || ', ' ||
                                                (GeoJson -> 'results' -> (0) -> 'components' ->> 'country')::text end as location
                                from LoginHistory where AppUserId = @userId order by DateCreated desc   limit 1";
            var lastLoginHistory = (await connection.QueryAsync<LastLoginHistory>(sql, new
            {
                userId
            })).FirstOrDefault();
            await connection.ExecuteAsync(
                "UPDATE APPUSER set lastLoginDate = @lastLoginDate, lastloginlocation = @lastLoginLocation where id = @userId ",
                new
                {
                    userId,
                    lastLoginDate = lastLoginHistory.LastLoginDate,
                    lastLoginLocation = lastLoginHistory.Location
                });
            await _cache.ClearUserCache(userId);
        }

        public async Task AddLoginHistory(long userId, decimal? latitude, decimal? longitude, string ipAddress)
        {
            using var connection = this.GetConnection();
            string geojson = null;
            string geoIpJson = null;
            if (latitude != null && longitude != null)
            {
                var cageKey = this._configuration["AppSettingsOpenCageKey"];
                var client = this._factory.CreateClient();
                var response = await client.GetAsync(
                    $"https://api.opencagedata.com/geocode/v1/json?key={cageKey}&q={latitude}+{longitude}&pretty=0&no_annotations=1");
                geojson = await response.Content.ReadAsStringAsync();
            }

            if (ipAddress != null)
            {
                var ipStackKey = this._configuration["AppSettingsIpStackKey"];
                var client = this._factory.CreateClient();
                var response = await client.GetAsync(
                    $"http://api.ipstack.com/{ipAddress}?access_key={ipStackKey}");
                geoIpJson = await response.Content.ReadAsStringAsync();
            }

            geoIpJson = null;
            geojson = null;

            const string sql =
                "insert into LoginHistory(AppUserId, Latitude, Longitude, Geojson, IpAddress, GeoIpJson, CreatedBy, UpdatedBy) values(@userId, @latitude, @longitude, @geojson::jsonb, @ipAddress, @geoIpJson::jsonb, @createdBy, @updatedBy)";
            await connection.ExecuteAsync(sql, new
            {
                userId,
                latitude,
                longitude,
                createdBy = userId,
                updatedBy = userId,
                geojson,
                ipAddress = ipAddress,
                geoIpJson
            });

            await UpdateLastLoginHistory(userId);
        }

        public async Task AddOrUpdateUser(HyperlinkEdit modelAdminUserModel, long? overrideUserId = null)
        {
            using var connection = this.GetConnection();
            modelAdminUserModel.Email = modelAdminUserModel.Email.Trim();
            if (modelAdminUserModel != null)
            {
                // update is happening here
                if (modelAdminUserModel.Id != 0)
                {
                    await connection.ExecuteAsync(
                        "UPDATE AppUser SET FirstName = @firstName, LastName = @lastName, Name = @name, mobile = @mobile, isactive= @IsActive,rolelevel=@rolelevel where id = @id",
                        new
                        {
                            firstName = modelAdminUserModel.FirstName,
                            lastName = modelAdminUserModel.LastName,
                            name = $"{modelAdminUserModel.FirstName.ToPascalCase()} {modelAdminUserModel.LastName.ToPascalCase()}",
                            profilePicture = modelAdminUserModel.ProfilePicture,
                            mobile = modelAdminUserModel.Mobile,
                            isactive = modelAdminUserModel.IsActive,
                            id = modelAdminUserModel.Id,
                            rolelevel = modelAdminUserModel.Rolelevel,
                        });

                    // if (!model.ProfilePicture.ToUpper().StartsWith("HTTP"))
                    // {
                    //    await connection.ExecuteAsync(
                    //        "UPDATE \"AppUser\" SET \"ProfilePicture\" = @profilePicture where \"Id\" = @id",
                    //        new
                    //        {
                    //            profilePicture = model.ProfilePicture,
                    //            id = model.Id,
                    //        });
                    // }
                    await this._cache.ClearUserCache(modelAdminUserModel.Id);
                    return;
                }

                var existingUserId = await connection.ExecuteScalarAsync<long?>(
                    "select id from appuser where email = @email",
                    new
                    {
                        email = modelAdminUserModel.Email
                    });
                if (existingUserId != null)
                {
                    throw new Exception(
                        "Email address is already in use, please provide a different email address.");
                }

                var transaction = connection.BeginTransaction();
                var key = Guid.NewGuid();

                var verificationKey = UtilExtensions
                    .GenerateRandomPassword(new Microsoft.AspNetCore.Identity.PasswordOptions
                    {
                        RequireDigit = true,
                        RequiredLength = 6,
                        RequireNonAlphanumeric = false,
                        RequiredUniqueChars = 6
                    }).ToLower();

                var password = UtilExtensions
                    .GenerateRandomPassword(new Microsoft.AspNetCore.Identity.PasswordOptions
                    {
                        RequireUppercase = true,
                        RequireLowercase = true,
                        RequireDigit = true,
                        RequiredLength = 10,
                        RequireNonAlphanumeric = true,
                        RequiredUniqueChars = 6
                    }).ToLower();

                var user = new AppUser
                {
                    FirstName = modelAdminUserModel.FirstName.ToPascalCase(),
                    LastName = modelAdminUserModel.LastName.ToPascalCase(),
                    Email = modelAdminUserModel.Email.ToLower(),
                    Name = $"{modelAdminUserModel.FirstName.ToPascalCase()} {modelAdminUserModel.LastName.ToPascalCase()}",
                    EncryptionKey = key.ToString(),
                    VerificationKey = verificationKey,
                    VerificationKeyExpiryTime = DateTimeOffset.UtcNow.AddMinutes(20),
                    Mobile = modelAdminUserModel.Mobile,
                    UpdatedBy = 1,
                    CreatedBy = 1,
                    HasAcceptedPrivacyPolicy = true,
                    HasAcceptedTerms = true,
                    IsDeleted = false,
                    IsActive = false,
                    Isemailverified = false,
                    ProfilePicture = modelAdminUserModel.ProfilePicture,
                    DepartmentId = modelAdminUserModel.DepartmentId,
                    Password = password,
                    Rolelevel = modelAdminUserModel.Rolelevel,
                };
                var valueFromConfig = await _cache.FindAppSettings("EmployeeApproval");
                user = await this.AddOrUpdate(user, overrideUserId);

                // await _taskStepInstance.CreateTaskStepInstance(int.Parse(valueFromConfig.Value), user.Id, true, 0, user.Id);
                await _authenticationProvider.RegisterUsers(new[] { user });
                transaction.Commit();
                await this._cache.ClearUserCache(user.Id);
            }
        }

        public new async Task<AppUser> Get(long userId)
        {
            var user = await base.Get(userId);
            user.Password = null;
            return user;
        }

        public async Task<AppUser> GetUserByEmail(string userEmail)
        {
            return (await this.GetAll(" where email = @userEmail", new { userEmail })).FirstOrDefault();
        }

        public async Task<string> Authenticate(LoginViewModel model)
        {
            using var connection = this.GetConnection();
            var authenticatedUser = await _authenticationProvider.GenerateToken(model);

            // var user = (await connection.GetListAsync<AppUser>(new
            // {
            //    Email = authenticatedUser.Email.ToLower(),
            //    IsActive = true,
            //    IsDeleted = false
            // })).FirstOrDefault();
            var query = $"SELECT * FROM appuser WHERE LOWER(email) = @email and isactive =@isactive AND isdeleted = @isdeleted ";
            var parameters = new
            {
                @email = authenticatedUser.Email.ToLower(),
                @isActive = true,
                @isDeleted = false
            };
            var users = await connection.QueryAsync<AppUser>(query, parameters);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                throw new HandledException("Deactivated User Login", 401, HttpStatusCode.OK);
            }

            await _cache.ClearUserCache(user.Id);
            await AddLoginHistory(user.Id, model.Latitude, model.Longitude, model.IpAddress);
            return await GenerateToken(user.Id);
        }

        public async Task<string> GenerateToken(InstallationViewModel model)
        {
            return await this.GenerateToken(1, 2, new MeModel
            {
                Email = model.AdminUserModel.Email,
                Name = $"{model.AdminUserModel.FirstName} {model.AdminUserModel.LastName}",
                FirstName = model.AdminUserModel.FirstName,
                LastName = model.AdminUserModel.LastName,
                Id = 1
            });
        }

        public async Task<MeModel> GetCurrentUser()
        {
            return await this.Me();
        }

        public async Task<MeModel> Me(long userId)
        {
            var user = await _cache.GetUser(userId);
            using var connection = this.GetConnection();
            var appuserRole = await connection.QueryAsync<AppUserRole>("select roleid from appuserrole where appuserid = @userid GROUP BY roleid", new { userid = userId });
            var appuserDepartment = await connection.QueryAsync<AppUserRole>("select departmentid from appuserrole where appuserid = @userid GROUP BY departmentid", new { userid = userId });
            var roleIds = appuserRole.ToList()
                .Select(x => x.RoleId);
            var departmentIds = appuserDepartment.ToList()
                .Select(x => x.DepartmentId);

            var meModel = Mapper.Map(user).ToANew<MeModel>();
            meModel.RoleId = roleIds.ToList();
            meModel.DepartmentId = departmentIds.ToList();
            return meModel;
        }

        public async Task<MeModel> Me()
        {
            var user = await _cache.GetUser(await _userContext.GetUserId());
            var meModel = Mapper.Map(user).ToANew<MeModel>();

            return meModel;
        }

        public async Task<string> GenerateToken(long userId, int expireMinutes = 8440, MeModel model = null)
        {
            var secret = _configuration["Jwt-Secret"];
            var symmetricKey = Convert.FromBase64String(secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var me = model ?? await this.Me(userId);
            var now = DateTime.UtcNow;
            using var connection = this.GetConnection();
            var roleIds = await connection.QueryAsync<long>("select roleid from appuserrole where appuserid = @userId",
                new
                {
                    userId
                });
            var roleLevels = await connection.QueryAsync<string>("select rolelevel from appuser where id = @userId",
                new
                {
                    userId
                });

            var claims = new List<Claim>()
            {
                new (ClaimTypes.Name, me.Email),
                new ("UserId", me.Id.ToString()),
                new ("RoleId", string.Join(",", me.RoleId)),
                new ("RoleIds", string.Join(",", roleIds)),
                new ("RoleLevel", string.Join(",", roleLevels)),
                new ("DepartmentId", string.Join(",", me.DepartmentId)),
                new ("UserData", _serializer.Serialize(me))
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims.ToArray()),
                Expires = now.AddMinutes(Convert.ToInt32(expireMinutes)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(symmetricKey),
                    SecurityAlgorithms.HmacSha256Signature),
                Audience = _configuration["Jwt-Audience"],
                Issuer = _configuration["Jwt-Issuer"]
            };

            var secureToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(secureToken);
        }

        public async Task UpdateUser(UserListItemModel appuser)
        {
            await this.DoUpdate(appuser, "appuser");
            await this._cache.ClearUserCache(appuser.Id);
        }

        public async Task ActivateUser(long id)
        {
            await this.Activate(id);
        }

        public async Task ActivateUserBatch(long[] ids)
        {
            foreach (long id in ids)
            {
                await this.Activate(id);
            }
        }

        public async Task DeactivateUser(long id)
        {
            using var connection = this.GetConnection();
            var appUser = await this.Me(id);
            await this.Deactivate(id);
            await this._cache.ClearUserCache(id);
        }

        public async Task DeactivateUserBatch(long[] ids)
        {
            foreach (long id in ids)
            {
                await this.Deactivate(id);
            }
        }

        public async Task<SearchResult<UserListItemModel>> SearchUsers(bool isActive)
        {
            return await this.Search<UserListItemModel>(string.Empty, 0, 0, "appuser",
                isActive: isActive, orderBy: "name");
        }

        public async Task<SearchResult<UserListItemModel>> SearchAllUsers(IsActiveFilter isActiveFilter)
        {
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            return await this.Search<UserListItemModel>(string.Empty, 0, 0, "appuser",
                isActive: isActivee, orderBy: "name");
        }

        public async Task<List<GetDetails>> GetRoleName()
        {
            using (var connection = this.GetConnection())
            {
                var roles = await connection.QueryAsync<GetDetails>("SELECT ar.RoleId, ar.AppUserId, ar.Name, d.name as DepartmentName FROM Appuserrole ar Left JOIN Department d ON ar.DepartmentId = d.Id GROUP BY ar.RoleId, ar.AppUserId, ar.Name, d.name");
                return roles.ToList();
            }
        }

        public async Task<List<GetUserStudioModal>> GetAllUserStudioDetails()
        {
            try
            {
                using var connection = this.GetConnection();
                var query =
                    $"SELECT pu.userid AS UserId, pu.studioid AS StudioId, s.name AS StudioName from photoshootuser pu JOIN studio s ON pu.studioid = s.id";
                var userStudioList = await connection.QueryAsync<GetUserStudioModal>(query);
                return userStudioList?.ToList();
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task<int> GetUserCount()
        {
            using var connection = this.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<int>(
                "select count(id) from appuser");
        }

        public async Task<ProfileViewModel> GetProfile()
        {
            using var connection = this.GetConnection();
            long id = await _userContext.GetUserId();
            return await connection.QueryFirstOrDefaultAsync<ProfileViewModel>(
                "select id, firstname,lastname,email,mobile from appuser where id=@id", new
                {
                    id = id
                });
        }

        public async Task UpdateProfile(ProfileViewModel appUser)
        {
            await this.DoUpdate(new
            {
                appUser.Id,
                appUser.FirstName,
                appUser.LastName,
                appUser.Mobile,
            }, "appuser");
        }

        public async Task<IEnumerable<ReportCountsModel>> GetRecentLoginHistoryCountDateWise()
        {
            using var connection = this.GetConnection();
            return await connection.QueryAsync<ReportCountsModel>(
                "select  date(datecreated) as GroupedItem, count(1) as ReportCount from loginhistory group by  date(datecreated) order by date(datecreated) desc limit 30");
        }

        public async Task<List<AppUser>> GetAllActiveUsers()
        {
            using var connection = this.GetConnection();
            var result = await connection.QueryAsync<AppUser>("select * from appuser where isactive = true");
            return result.ToList();
        }

        public async Task<List<AppUser>> GetAllInactiveUsers()
        {
            using var connection = this.GetConnection();
            var result = await connection.QueryAsync<AppUser>("select * from appuser where isactive = false");
            return result.ToList();
        }

        // public async Task<string> AddLabel(LabelModel model)
        // {
        //    var connection = this.GetConnection();
        //    var data = connection.Query<LabelModel>("select * from label where  name=@name", new { name = model.Name }).ToArray();
        //    var count = 0;
        //    foreach (var item in data)
        //    {
        //        if (item.Name == model.Name)
        //        {
        //            count++;
        //        }
        //    }
        //    if (data.Length == 0)
        //    {
        // LabelModel label = new LabelModel()
        //        {
        //            Name = model.Name.ToUpper(),
        //            Description = model.Description,
        //            LanguageId = model.LanguageId,
        //            IsActive = true,
        //        };
        //        string sqlQuery = "INSERT  INTO LABEL (Name, Description,IsActive,LanguageId)" +
        //                "VALUES(@Name, @Description,@IsActive,@LanguageId)";
        //        var Query = connection.Execute(sqlQuery, label);
        //        return "Label Added Successfully";
        //    }
        //    else
        //    {
        //        return "Name Already Existing";
        //    }
        // }
        public async Task AddRole(Role model)
        {
            using var connection = this.GetConnection();
            var data = connection.Query<Role>("select * from role where  name=@name", new { name = model.Name }).ToArray();
            var count = 0;
            foreach (var item in data)
            {
                if (item.Name == model.Name)
                {
                    count++;
                }
            }

            if (data.Length == 0)
            {
                Role role = new Role()
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = true,
                    CreatedBy = 1,
                    UpdatedBy = 1,
                };
                string sqlQuery = "INSERT  INTO ROLE (Name, Description,IsActive,createdby,updatedby)" +
                        "VALUES(@Name, @Description,@IsActive,@createdby,@updatedby)";
                var query = connection.Execute(sqlQuery, role);
            }
            else
            {
                throw new HandledException(code: 400, message: "Name Already Existing");
            }
        }

        public async Task AddUserRole(AppUserRoleModel model)
        {
            try
            {
                using var connection = this.GetConnection();
                foreach (var models in model.AppUserRoleDomainModel)
                {
                    string sqlQueryCheck = "SELECT COUNT(*) FROM APPUSERROLE WHERE appuserid = @appuserid AND roleid = @RoleId";
                    var departmentExists = await connection.QueryFirstOrDefaultAsync<int>(sqlQueryCheck, new
                    {
                        appuserid = model.AppUserid,
                        RoleId = models.RoleId
                    });
                    if (departmentExists == 0)
                    {
                        string sqlQuery = "INSERT  INTO APPUSERROLE (Name, RoleId,appuserid)" +
                            "VALUES(@name, @roleid,@appuserid)";
                        var query1 = connection.Execute(sqlQuery, new { name = models.Name, roleId = models.RoleId, appuserid = model.AppUserid });
                    }
                }
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task AddUserDepartment(AppUserRoleModel appUserRoleModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var configJsonString = await _cache.FindAppSettings("BasicRole");

                foreach (var models in appUserRoleModel.AppUserRoleDomainModel)
                {
                    string sqlQueryCheck = "SELECT COUNT(*) FROM APPUSERROLE WHERE appuserid = @appuserid AND departmentid = @departmentid";
                    var departmentExists = await connection.QueryFirstOrDefaultAsync<int>(sqlQueryCheck, new
                    {
                        appuserid = appUserRoleModel.AppUserid,
                        departmentid = models.DepartmentId
                    });

                    if (departmentExists == 0)
                    {
                        string sqlQueryInsert = "INSERT INTO APPUSERROLE (Name, departmentid, appuserid, roleid) " +
                                                "VALUES (@name, @departmentid, @appuserid, @roleid)";
                        var queryInsert = await connection.ExecuteAsync(sqlQueryInsert, new
                        {
                            name = models.Name,
                            departmentid = models.DepartmentId,
                            appuserid = appUserRoleModel.AppUserid,
                            roleid = int.Parse(configJsonString.Value)
                        });
                    }
                }
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task DeleteDepartment(AppUserRole appUserRole)
        {
            using var connection = this.GetConnection();
            await connection.QueryAsync<AppUserRoleModel>($"Delete from appuserrole where appuserid = @AppuserId AND departmentid =@departmentId ",
                new { AppuserId = appUserRole.AppUserId, departmentId = appUserRole.DepartmentId });
        }

        public async Task LinkUsersToMarketPlaces(IEnumerable<AppuserMarketplace> appuserMarketplaces)
        {
            using var connection = this.GetConnection();
            using var transaction = connection.BeginTransaction();
            var action = this.GetAction<AppuserMarketplace>();
            foreach (var marketplace in appuserMarketplaces)
            {
                await action.AddOrUpdate(marketplace, transaction: transaction);
            }

            transaction.Commit();
        }

        public async Task UnlinkUserFromMarketPlaces(long[] ids)
        {
            using var connection = this.GetConnection();
            var action = this.GetAction<AppuserMarketplace>();
            await action.Remove(ids);
        }

        public async Task<IEnumerable<Marketplace>> GetMarketplacesOfUser(long userId)
        {
            using var connection = this.GetConnection();
            return await connection.QueryAsync<Marketplace>(
                "select am.*,m.name,m.shortcode from marketplace m join appusermarketplace am on am.marketplaceid = m.id where am.appuserid = @userId",
                new
                {
                    userId
                });
        }

        public async Task<List<TemplateMapping>> GetTemplateMappings(long id)
        {
            try
            {
                using (var connection = this.GetConnection())
                {
                    return (await connection.QueryAsync<TemplateMapping>($"select * from templatemapping where templateid = {id}")).ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<UserDepartment>> GetDepartmentDetails(string userId)
        {
            try
            {
                using var connection = this.GetConnection();
                var query =
                    $"select d.name as DepartmentName,d.isplanogram as IsPlanogram, di.name as DivisionName,d.id as DepartmentId , di.id as DivisionId from department d INNER JOIN appuserrole a on a.departmentid = d.id INNER JOIN division di ON di.id = d.divisionid where a.appuserid = {userId}";
                var workFlowDesignList = await connection.QueryAsync<UserDepartment>(query);
                return workFlowDesignList?.ToList();
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task AddStudioToUser(AppUserStudioModal appUserStudioModal)
        {
            try
            {
                using var connection = this.GetConnection();

                foreach (var data in appUserStudioModal.StudioId)
                {
                    string sqlQueryCheck = "SELECT COUNT(*) FROM photoshootuser WHERE userid = @userid AND studioid = @studioid";
                    var studioExists = await connection.QueryFirstOrDefaultAsync<int>(sqlQueryCheck, new
                    {
                        userid = appUserStudioModal.UserId,
                        studioid = data
                    });

                    if (studioExists == 0)
                    {
                        string sqlQueryInsert = "INSERT INTO photoshootuser (userid, studioid) " +
                                                "VALUES (@userid, @studioid)";
                        var queryInsert = await connection.ExecuteAsync(sqlQueryInsert, new
                        {
                            studioid = data,
                            userid = appUserStudioModal.UserId
                        });
                    }
                }
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task<List<GetUserStudioModal>> GetUserStudioDetails(long userId)
        {
            try
            {
                using var connection = this.GetConnection();
                var query =
                    $"SELECT pu.userid AS UserId, pu.studioid AS StudioId, s.name AS StudioName from photoshootuser pu JOIN studio s ON pu.studioid = s.id WHERE pu.userid = @userId";
                var userStudioList = await connection.QueryAsync<GetUserStudioModal>(query, new { userId = userId });
                return userStudioList?.ToList();
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        public async Task DeleteStudioFromUser(AppUserStudioDeleteModal appUserStudioDeleteModal)
        {
            using var connection = this.GetConnection();
            await connection.QueryAsync($"Delete from photoshootuser where userid = @userId AND studioid =@studioId ",
                new { userId = appUserStudioDeleteModal.UserId, studioId = appUserStudioDeleteModal.StudioId });
        }

        public async Task<List<PlanoCountModel>> GetPlanoCount(DateTime? fromDate, DateTime? todate, long[]? divisionId)
        {
            using var connection = this.GetConnection();
            string divisionFilter = divisionId != null && divisionId.Any() ? divisionId.ToString() : null;
            var divisionIdsDB = await connection.QueryAsync<Division>($"SELECT * FROM Division");
            if (fromDate == null && todate == null && divisionFilter == null)
            {
                divisionId = divisionIdsDB.Select(d => Convert.ToInt64(d.Code)).ToArray();
                var desiredOrder = new long[] { 61, 4, 65 };
                var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                string divisionIdString = string.Join(",", orderedDivisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"SELECT * FROM get_counts_by_status(null,null,ARRAY[{divisionIdString}]);");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
            else if (fromDate != null && todate != null && divisionFilter != null)
            {
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = todate ?? DateTime.MaxValue;
                string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
                string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
                var multiDivision = string.Join(",", divisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"select * from get_counts_by_status('{fromDateStr}'::timestamp without time zone, '{toDateStr}'::timestamp without time zone,ARRAY[{multiDivision}])");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
            else if (fromDate != null && divisionFilter != null)
            {
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = todate ?? DateTime.MaxValue;
                string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
                string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
                var multiDivision = string.Join(",", divisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"select * from get_counts_by_status('{fromDateStr}'::timestamp without time zone,'{toDateStr}'::timestamp without time zone,ARRAY[{multiDivision}])");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
            else if (todate != null && divisionFilter != null)
            {
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = todate ?? DateTime.MaxValue;
                string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
                string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
                var multiDivision = string.Join(",", divisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"select * from get_counts_by_status('{fromDateStr}'::timestamp without time zone,'{toDateStr}'::timestamp without time zone,ARRAY[{multiDivision}])");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
            else if (fromDate != null || todate != null)
            {
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = todate ?? DateTime.MaxValue;
                string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
                string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
                divisionId = divisionIdsDB.Select(d => Convert.ToInt64(d.Code)).ToArray();
                string divisionIdString = string.Join(",", divisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"SELECT division_code, total_count, approved_count, rejected_count, pending_count FROM get_counts_by_status('{fromDateStr}'::timestamp without time zone, '{toDateStr}'::timestamp without time zone, ARRAY[{divisionIdString}])");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
            else
            {
                var divisionIdsString = string.Join(",", divisionId);
                var count = await connection.QueryAsync<PlanoCountModel>($"SELECT * FROM public.get_counts_by_status(NULL,NULL,ARRAY[{divisionIdsString}])");
                var uidCount = await this.GetUidCount(fromDate, todate, divisionId);

                foreach (var planoCountModel in count)
                {
                    if (planoCountModel.Division_code == null)
                    {
                        planoCountModel.Division_code = 0;
                    }

                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        int divisionCode;
                        if (int.TryParse(uid.Division, out divisionCode))
                        {
                            return divisionCode == planoCountModel.Division_code;
                        }
                        else if (uid.Division == "Total")
                        {
                            return planoCountModel.Division_code == 0;
                        }

                        return false;
                    });
                    if (matchingUidCount != null)
                    {
                        planoCountModel.Uid_Count = matchingUidCount.Uidcount;
                    }
                }

                return count.ToList();
            }
        }

        public async Task<byte[]> GetPlanoCountExcel(GetPlanoModalDto inputData)
        {
            try
            {
                using var connection = this.GetConnection();
                var result = new List<PlanoExportExcelModel>();
                DateTime? fromDate = inputData.FromDate;
                DateTime? toDate = inputData.ToDate;
                string[] cols = new string[] { "Division", "Total PlanoFamily Created", "Total PlanoFamily Approved", "Total PlanoFamily Rejected", "Total PlanoFamily Approval Pending", "Total UID Generated" };
                long[] divisionId = inputData.Data != null
                                    ? inputData.Data.Where(x => x.DivisionId.HasValue).Select(x => x.DivisionId.Value).ToArray()
                                    : Array.Empty<long>();

                List<UidCountModel> uidCount;
                var expansionList = new List<PlanoUserCountModel>();
                var divisionIdsDB = await connection.QueryAsync<Division>($"SELECT * FROM Division");
                string divisionFilter = divisionId != null && divisionId.Any() ? divisionId.ToString() : null;
                if (fromDate == null && toDate == null && divisionFilter == null)
                {
                    divisionId = divisionIdsDB.Select(d => Convert.ToInt64(d.Code)).ToArray();
                    var desiredOrder = new long[] { 61, 4, 65 };
                    var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                    string divisionIdString = string.Join(",", orderedDivisionId);
                    result = (await connection.QueryAsync<PlanoExportExcelModel>($"select CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending      from     get_counts_by_status(null,null,ARRAY[{divisionIdString}])")).ToList();
                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                }
                else if (fromDate != null && toDate != null && divisionFilter != null)
                {
                    string fromDateStr = fromDate?.Date.ToString("yyyy/MM/dd");
                    string toDateStr = toDate?.Date.ToString("yyyy/MM/dd");
                    var desiredOrder = new long[] { 61, 4, 65 };
                    var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                    var multiDivision = string.Join(",", orderedDivisionId);
                    result = (await connection.QueryAsync<PlanoExportExcelModel>($"SELECT CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  FROM public.get_counts_by_status('{fromDateStr}'::timestamp without time zone,'{toDateStr}'::timestamp without time zone,ARRAY[{multiDivision}])")).ToList();
                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                    var expandedDiv = inputData.Data.Where(d => d.DivisionId != null && d.Status);
                    foreach (var id in expandedDiv)
                    {
                        var userCounts = await this.GetPlanoUserCount(id.DivisionId.Value, toDate, fromDate);
                        expansionList.AddRange(userCounts); // Add to expansionList
                    }
                }
                else if (fromDate != null && divisionFilter != null)
                {
                    string fromDateStr = fromDate?.Date.ToString("yyyy/MM/dd");
                    var desiredOrder = new long[] { 61, 4, 65 };
                    var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                    var multiDivision = string.Join(",", orderedDivisionId);
                    result = (await connection.QueryAsync<PlanoExportExcelModel>($"SELECT CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  FROM public.get_counts_by_status('{fromDateStr}'::timestamp without time zone,NULL,ARRAY[{multiDivision}])")).ToList();
                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                    var expandedDiv = inputData.Data.Where(d => d.DivisionId != null && d.Status);
                    foreach (var id in expandedDiv)
                    {
                        var userCounts = await this.GetPlanoUserCount(id.DivisionId.Value, toDate, fromDate);
                        expansionList.AddRange(userCounts); // Add to expansionList
                    }
                }
                else if (toDate != null && divisionFilter != null)
                {
                    string toDateStr = toDate?.Date.ToString("yyyy/MM/dd");
                    var desiredOrder = new long[] { 61, 4, 65 };
                    var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                    var multiDivision = string.Join(",", orderedDivisionId);
                    result = (await connection.QueryAsync<PlanoExportExcelModel>($"SELECT CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  FROM public.get_counts_by_status(NULL,'{toDateStr}'::timestamp without time zone,,ARRAY[{multiDivision}])")).ToList();
                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                    var expandedDiv = inputData.Data.Where(d => d.DivisionId != null && d.Status);
                    foreach (var id in expandedDiv)
                    {
                        var userCounts = await this.GetPlanoUserCount(id.DivisionId.Value, toDate, fromDate);
                        expansionList.AddRange(userCounts); // Add to expansionList
                    }
                }
                else if (fromDate != null || toDate != null)
                {
                    divisionId = divisionIdsDB.Select(d => Convert.ToInt64(d.Code)).ToArray();
                    var desiredOrder = new long[] { 61, 4, 65 };
                    var orderedDivisionId = divisionId.OrderBy(id => Array.IndexOf(desiredOrder, id)).ToArray();
                    string divisionIdString = string.Join(",", orderedDivisionId);
                    string fromDateStr = fromDate?.Date.ToString("yyyy/MM/dd");
                    string toDateStr = toDate?.Date.ToString("yyyy/MM/dd");
                    if (fromDate != null && toDate != null)
                    {
                        result = (await connection.QueryAsync<PlanoExportExcelModel>($"select CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  from get_counts_by_status('{fromDateStr}'::timestamp without time zone, '{toDateStr}'::timestamp without time zone,ARRAY[{divisionIdString}])")).ToList();
                    }
                    else if (fromDate != null)
                    {
                        result = (await connection.QueryAsync<PlanoExportExcelModel>($"select CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  from get_counts_by_status('{fromDateStr}'::timestamp without time zone, NULL,ARRAY[{divisionIdString}])")).ToList();
                    }
                    else if (toDate != null)
                    {
                        result = (await connection.QueryAsync<PlanoExportExcelModel>($"select CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  from get_counts_by_status(NULL, '{toDateStr}'::timestamp without time zone,ARRAY[{divisionIdString}])")).ToList();
                    }

                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                }
                else
                {
                    var multiDivision = string.Join(",", divisionId);
                    result = (await connection.QueryAsync<PlanoExportExcelModel>($"SELECT CAST(division_code AS VARCHAR) As Division,total_count AS TotalPlanoFamilyCreated, approved_count As TotalPlanoFamilyApproved,rejected_count As TotalPlanoFamilyRejected,pending_count As TotalPlanoFamilyApprovalPending  FROM public.get_counts_by_status(NULL,NULL,ARRAY[{multiDivision}])")).ToList();
                    uidCount = await this.GetUidCount(fromDate, toDate, divisionId);
                    var expandedDiv = inputData.Data.Where(d => d.DivisionId != null && d.Status);
                    foreach (var id in expandedDiv)
                    {
                        var userCounts = await this.GetPlanoUserCount(id.DivisionId.Value, toDate, fromDate);
                        expansionList.AddRange(userCounts); // Add to expansionList
                    }
                }

                foreach (var item in result)
                {
                    var matchingUidCount = uidCount.FirstOrDefault(uid =>
                    {
                        if (uid.Division == "Total" && (item.Division == "0" || item.Division == null))
                        {
                            return true;
                        }
                        else
                        {
                            return uid.Division.ToString() == item.Division;
                        }
                    });

                    // var matchingUidCount = uidCount.FirstOrDefault(uid => uid.Division.ToString() == item.Division);
                    if (matchingUidCount != null)
                    {
                        item.ToatalUIDGenerated = matchingUidCount.Uidcount;
                    }
                }

                // Define the divisions to target
                var targetDivisions = new HashSet<string> { "61", "4", "65" };

                // Create a dictionary to store indices for the target divisions
                var divisionIndices = new Dictionary<string, int>();

                // Find the indices of the target divisions
                for (int i = 0; i < result.Count; i++)
                {
                    if (targetDivisions.Contains(result[i].Division) && !divisionIndices.ContainsKey(result[i].Division))
                    {
                        divisionIndices[result[i].Division] = i;
                    }
                }

                // Ensure you add records only if the target divisions exist
                foreach (var userCount in expansionList)
                {
                    if (divisionIndices.TryGetValue(userCount.Division.ToString(), out var index))
                    {
                        var model = new PlanoExportExcelModel
                        {
                            Division = userCount.Name, // Assuming Name is the desired value to add
                            TotalPlanoFamilyCreated = userCount.Planogram_Total_Count,
                            TotalPlanoFamilyApproved = userCount.Planogram_Approved_Count,
                            TotalPlanoFamilyRejected = userCount.Planogram_Rejected_Count,
                            TotalPlanoFamilyApprovalPending = userCount.Planogram_Pending_Count,
                            ToatalUIDGenerated = userCount.Uid_Count
                        };

                        // Insert each model after the corresponding division entry
                        result.Insert(index + 1, model);

                        // Increment the indices of all subsequent divisions to ensure correct placement for subsequent entries
                        foreach (var key in divisionIndices.Keys.ToList())
                        {
                            if (divisionIndices[key] > index)
                            {
                                divisionIndices[key]++;
                            }
                        }

                        // Update the index for the current division to point to the newly inserted model
                        divisionIndices[userCount.Division.ToString()]++;
                    }
                }

                var divisionMappings = new Dictionary<string, string>
                {
                    { "61", "HC-House Hold" },
                    { "4", "HC-Furniture" },
                    { "65", "HC-Cash And Carry" }
                };

                foreach (var item in result)
                {
                    if (item.Division == null)
                    {
                        item.Division = "Total";
                    }
                    else if (divisionMappings.TryGetValue(item.Division, out var name))
                    {
                        item.Division = name;
                    }
                    else
                    {
                        item.Division = item.Division;
                    }
                }

                var responseDataTable = this.ToDataTable(result);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Plano Count");

                var headerBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#002e63");
                var headerOtherBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#0070ff");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
                var divisionDataColor = System.Drawing.ColorTranslator.FromHtml("#fffafa");
                var rowDataColor1 = System.Drawing.ColorTranslator.FromHtml("#c9dced");
                var rowDataColor2 = System.Drawing.ColorTranslator.FromHtml("#f0ffff");

                // Set header row styles
                for (int i = 0; i < cols.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = cols[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(i == 0 ? headerBackgroundColor : headerOtherBackgroundColor);
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
                    worksheet.Cells[row, 1].Value = record.Division;
                    worksheet.Cells[row, 2].Value = record.TotalPlanoFamilyCreated;
                    worksheet.Cells[row, 3].Value = record.TotalPlanoFamilyApproved;
                    worksheet.Cells[row, 4].Value = record.TotalPlanoFamilyRejected;
                    worksheet.Cells[row, 5].Value = record.TotalPlanoFamilyApprovalPending;
                    worksheet.Cells[row, 6].Value = record.ToatalUIDGenerated;

                    var fillColor = row % 2 == 0 ? rowDataColor1 : rowDataColor2;

                    for (int col = 1; col <= 6; col++)
                    {
                        var dataCell = worksheet.Cells[row, col];
                        dataCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        dataCell.Style.Fill.BackgroundColor.SetColor(col == 1 ? divisionDataColor : fillColor);
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
                for (int i = 1; i <= 6; i++)
                {
                    worksheet.Column(i).Width = 20;
                }

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<byte[]> GetTemplatesUidCountExcel(GetTemplatesUidCountExcel getTemplatesUidCountExcel)
        {
            try
            {
                using var connection = GetConnection();
                string[] cols = { "Division", "Total UID Generated", "Buyer Temp1", "Buyer Temp2", "Designer Temp", "QC Temp", "Itemcode Ready", "CAM Ready" };
                string query;
                var resultData = new List<TemplateUidExcelModel>();
                var templateExpansion = new List<TemplateDeptCount>();

                if (getTemplatesUidCountExcel.Division_code != null || getTemplatesUidCountExcel.Dept_code != null || getTemplatesUidCountExcel.Class_code != null || getTemplatesUidCountExcel.Subclass_code != null || getTemplatesUidCountExcel.FromDate != null || getTemplatesUidCountExcel.ToDate != null)
                {
                    string[] divisionCodes = getTemplatesUidCountExcel.Data
                                .Where(d => d.Division_code.ToString() != null)
                                .Select(d => d.Division_code.ToString())
                                .ToArray();
                    string[] deptCodes = getTemplatesUidCountExcel.Dept_code?.Split(',');
                    string[] classCodes = getTemplatesUidCountExcel.Class_code?.Split(',');
                    string[] subclassCodes = getTemplatesUidCountExcel.Subclass_code?.Split(',');
                    string fromDateStr = getTemplatesUidCountExcel.FromDate?.ToString("yyyy/MM/dd");
                    string toDateStr = getTemplatesUidCountExcel.ToDate?.ToString("yyyy/MM/dd");

                    string deptCodeFilter = deptCodes != null && deptCodes.Any() ? $"ARRAY[{string.Join(",", deptCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                    string divisionCodeFilter = divisionCodes != null && divisionCodes.Any() ? $"ARRAY[{string.Join(",", divisionCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                    string classCodeFilter = classCodes != null && classCodes.Any() ? $"ARRAY[{string.Join(",", classCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                    string subclassCodeFilter = subclassCodes != null && subclassCodes.Any() ? $"ARRAY[{string.Join(",", subclassCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                    string startDateFilter = fromDateStr != null ? $"'{fromDateStr}'" : "NULL";
                    string endDateFilter = toDateStr != null ? $"'{toDateStr}'" : "NULL";

                    query = $"SELECT divisioncode AS Division, buyertemplate1 As BuyerTemp1, buyertemplate2 As BuyerTemp2,fab As FABTemp,packagingdesigner As DesignerTemp, qctemplate AS QcTemp,itemcode_ready As ItemcodeReady,cam_ready As CAMReady, uidcount As ToatalUIDGenerated FROM uidcount(" +
                        $"dept_code_filter := {deptCodeFilter}," +
                        $"division_code_filter := {divisionCodeFilter}," +
                        $"class_code_filter := {classCodeFilter}," +
                        $"subclass_code_filter := {subclassCodeFilter}," +
                        $"start_date := {startDateFilter}," +
                        $"end_date := {endDateFilter}" +
                    ")";

                    var expandedDiv = getTemplatesUidCountExcel.Data.Where(d => d.Division_code != null && d.Status);
                    foreach (var id in expandedDiv)
                    {
                        var templateCount = await this.GetDeptAllCount(id.Division_code.Value, getTemplatesUidCountExcel.Dept_code, getTemplatesUidCountExcel.Class_code, getTemplatesUidCountExcel.Subclass_code, getTemplatesUidCountExcel.FromDate, getTemplatesUidCountExcel.ToDate);
                        templateExpansion.AddRange(templateCount);
                    }
                }
                else
                {
                    query = "SELECT divisioncode AS Division, buyertemplate1 As BuyerTemp1, buyertemplate2 As BuyerTemp2,fab As FABTemp,packagingdesigner As DesignerTemp, qctemplate AS QcTemp,itemcode_ready As ItemcodeReady,cam_ready As CAMReady, uidcount As ToatalUIDGenerated FROM uidcount()";
                }

                resultData = (await connection.QueryAsync<TemplateUidExcelModel>(query)).ToList();

                var targetDivisions = new HashSet<string> { "61", "4", "65" };
                var divisionIndices = new Dictionary<string, int>();

                for (int i = 0; i < resultData.Count; i++)
                {
                    if (targetDivisions.Contains(resultData[i].Division) && !divisionIndices.ContainsKey(resultData[i].Division))
                    {
                        divisionIndices[resultData[i].Division] = i;
                    }
                }

                foreach (var userCount in templateExpansion)
                {
                    if (divisionIndices.TryGetValue(userCount.Division_Code.ToString(), out var index))
                    {
                        var model = new TemplateUidExcelModel
                        {
                            Division = userCount.Dept_name,
                            ToatalUIDGenerated = userCount.Dept_counts.ToString(),
                            BuyerTemp1 = userCount.Buyer1_Count.ToString(),
                            BuyerTemp2 = userCount.Buyer2_Count.ToString(),
                            DesignerTemp = userCount.Packaging_Count.ToString(),
                            QcTemp = userCount.Qc_Count.ToString(),
                            ItemcodeReady = userCount.Itemcode_Ready.ToString(),
                            CAMReady = userCount.Cam_Ready.ToString()
                        };

                        resultData.Insert(index + 1, model);

                        foreach (var key in divisionIndices.Keys.ToList())
                        {
                            if (divisionIndices[key] > index)
                            {
                                divisionIndices[key]++;
                            }
                        }

                        divisionIndices[userCount.Division_Code.ToString()]++;
                    }
                }

                var divisionMappings = new Dictionary<string, string>
                {
                    { "61", "HC-House Hold" },
                    { "4", "HC-Furniture" },
                    { "65", "HC-Cash And Carry" }
                };

                foreach (var item in resultData)
                {
                    if (item.Division == null)
                    {
                        item.Division = "Total";
                    }
                    else if (divisionMappings.TryGetValue(item.Division, out var name))
                    {
                        item.Division = name;
                    }
                    else
                    {
                        item.Division = item.Division;
                    }
                }

                var responseDataTable = ToDataTable(resultData);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("UID Count");

                var headerDivisionBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#002e63");
                var headerOtherBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#0070ff");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
                var divisionDataColor = System.Drawing.ColorTranslator.FromHtml("#fffafa");
                var rowDataColor1 = System.Drawing.ColorTranslator.FromHtml("#c9dced");
                var rowDataColor2 = System.Drawing.ColorTranslator.FromHtml("#f0ffff");

                // Set header colors
                for (int i = 0; i < cols.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = cols[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(i == 0 ? headerDivisionBackgroundColor : headerOtherBackgroundColor);
                    cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(borderColor);
                    cell.Style.Border.Left.Color.SetColor(borderColor);
                    cell.Style.Border.Right.Color.SetColor(borderColor);
                    cell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                // Fill data with alternating row colors
                var row = 2;
                foreach (var record in resultData)
                {
                    worksheet.Cells[row, 1].Value = record.Division;
                    worksheet.Cells[row, 2].Value = record.ToatalUIDGenerated;
                    worksheet.Cells[row, 3].Value = record.BuyerTemp1;
                    worksheet.Cells[row, 4].Value = record.BuyerTemp2;
                    //// worksheet.Cells[row, 5].Value = record.FABTemp;
                    worksheet.Cells[row, 5].Value = record.DesignerTemp;
                    worksheet.Cells[row, 6].Value = record.QcTemp;
                    worksheet.Cells[row, 7].Value = record.ItemcodeReady;
                    worksheet.Cells[row, 8].Value = record.CAMReady;

                    var fillColor1 = row % 2 == 0 ? rowDataColor2 : rowDataColor1;
                    var fillColor2 = row % 2 == 0 ? rowDataColor1 : rowDataColor2;

                    for (int col = 1; col <= 8; col++)
                    {
                        var dataCell = worksheet.Cells[row, col];
                        dataCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        if (col == 1)
                        {
                            dataCell.Style.Fill.BackgroundColor.SetColor(divisionDataColor);
                        }
                        else
                        {
                            dataCell.Style.Fill.BackgroundColor.SetColor(fillColor2);
                        }

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

                for (int i = 1; i <= 8; i++)
                {
                    worksheet.Column(i).Width = (i == 2) ? 20 : 16;
                }

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<UidCountModel>> GetUidCount(DateTime? fromDate, DateTime? toDate, long[]? divisionId)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
            string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
            var defaultDivisionIds = new List<int> { 61, 4, 65 };
            string divisionFilter = divisionId != null && divisionId.Any() ? divisionId.ToString() : null;
            var divisionIds = divisionId?.Any() == true ? divisionId.Select(id => id.ToString()).ToList() : defaultDivisionIds.Select(id => id.ToString()).ToList();
            var divisionIdsString = string.Join(",", divisionIds.Select(id => $"'{id}'"));
            string startDateFilter = fromDateStr != null ? $"'{fromDateStr}'" : "NULL";
            string endDateFilter = toDateStr != null ? $"'{toDateStr}'" : "NULL";
            if (fromDate == null && toDate == null && divisionFilter == null)
            {
                ////var query = $"select count(cd.uid) as uid_count,u.divisionid as Division from product cd " +
                ////   $"JOIN uidinitiation u ON u.planocode = cd.columndata ->> 'Planogram/Family Code' " +
                ////   $"where cd.columndata->>'Division Code' IN ({divisionIdsString}) AND u.status = 'Approved'::text " +
                ////   $"GROUP BY GROUPING SETS ((u.divisionid), ()) ORDER BY CASE WHEN u.divisionid IS NULL THEN 1 ELSE 0 END, u.divisionid ";

                var query = $"select * from uidusercount(null,null,array[{divisionIdsString}])";
                var count = await connection.QueryAsync<UidCountModel>(query);
                return count.ToList();
            }
            else if (fromDate != null || toDate != null)
            {
                ////var query = $"select count(cd.uid) as uid_count,u.divisionid as Division from product cd " +
                ////   $"JOIN uidinitiation u ON u.planocode = cd.columndata ->> 'Planogram/Family Code' " +
                ////   $"where u.divisionid IN ({divisionIdsString}) AND to_char(u.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{fromDateStr}' AND '{toDateStr}' AND u.status = 'Approved'::text " +
                ////   $"GROUP BY GROUPING SETS ((u.divisionid), ()) ORDER BY CASE WHEN u.divisionid IS NULL THEN 1 ELSE 0 END, u.divisionid ";
                string query = $"SELECT * FROM uidusercount(" +
                                    $"{startDateFilter}," +
                                    $"{endDateFilter}," +
                                     $"array[{divisionIdsString}]" +
                                    ")";
                var count = await connection.QueryAsync<UidCountModel>(query);
                return count.ToList();
            }
            else
            {
                var multidivision = string.Join(",", divisionId.Select(id => $"'{id}'"));
                ////var query = $"select count(cd.uid) as uid_count,u.divisionid as Division from product cd " +
                ////   $"JOIN uidinitiation u ON u.planocode = cd.columndata ->> 'Planogram/Family Code' " +
                ////   $"where u.divisionid IN ({multidivision}) AND u.status = 'Approved'::text " +
                ////   $"GROUP BY GROUPING SETS ((u.divisionid), ()) ORDER BY CASE WHEN u.divisionid IS NULL THEN 1 ELSE 0 END, u.divisionid ";
                var query = $"select * from uidusercount(null,null,array[{divisionIdsString}])";
                var count = await connection.QueryAsync<UidCountModel>(query);
                return count.ToList();
            }
        }

        public async Task<List<PlanoUserCountModel>> GetPlanoUserCount(long divisionId, DateTime? fromDate, DateTime? toDate)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
            string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
            string query = $"SELECT COUNT(CONCAT(LTRIM(RTRIM(u.planogram)), '', LTRIM(RTRIM(u.familyname)))) AS planogram_total_count, " +
                $"COUNT(CASE WHEN u.status = 'Approved' THEN CONCAT(LTRIM(RTRIM(u.planogram)), '', LTRIM(RTRIM(u.familyname))) END) AS planogram_approved_count, " +
                $"COUNT(CASE WHEN u.status = 'Rejected' THEN CONCAT(LTRIM(RTRIM(u.planogram)), '', LTRIM(RTRIM(u.familyname))) END) AS planogram_rejected_count, " +
                $"COUNT(CASE WHEN u.status = 'Pending' THEN CONCAT(LTRIM(RTRIM(u.planogram)), '', LTRIM(RTRIM(u.familyname))) END) AS planogram_pending_count, " +
                $"a.name,u.divisionid AS Division FROM uidinitiation u " +
                $"INNER JOIN appuser a ON u.createdby = a.id Where u.divisionid = {divisionId} ";
            if (fromDate != null || toDate != null)
            {
                query += $" And to_char(u.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{fromDateStr}' AND '{toDateStr}' ";
            }

            query += $"  GROUP BY a.name,u.divisionid ";

            var count = await connection.QueryAsync<PlanoUserCountModel>(query);
            var uidCount = await this.GetPlanoUidUserCount(divisionId, fromDate, toDate);

            foreach (var userCountModel in count)
            {
                var matchingUidCount = uidCount.FirstOrDefault(uid => uid.Name == userCountModel.Name);
                if (matchingUidCount != null)
                {
                    userCountModel.Uid_Count = matchingUidCount.Uid_Count;
                }
            }

            var combinedCount = new List<PlanoUserCountModel>(count);

            foreach (var userCountModel in uidCount)
            {
                var matchingCountModel = count.FirstOrDefault(countModel => countModel.Name == userCountModel.Name);
                if (matchingCountModel == null && userCountModel.Name != null)
                {
                    combinedCount.Add(new PlanoUserCountModel
                    {
                        Name = userCountModel.Name,
                        Uid_Count = userCountModel.Uid_Count,
                        Division = Convert.ToInt64(userCountModel.Division),
                        Planogram_Total_Count = 0,
                        Planogram_Approved_Count = 0,
                        Planogram_Rejected_Count = 0,
                        Planogram_Pending_Count = 0
                    });
                }
            }

            return combinedCount;
        }

        public async Task<string> GetTiming()
        {
            using var connection = this.GetConnection();
            var timingdetails = connection.QueryFirstOrDefault<AppSetting>("SELECT *,value as Description  FROM APPSETTINGS WHERE NAME='SFTP'");
            if (timingdetails != null)
            {
                string timing = timingdetails.Description;
                return timing;
            }

            return null;
        }

        public async Task<List<PlanoUidUserCount>> GetPlanoUidUserCount(long divisionId, DateTime? fromDate, DateTime? toDate)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
            string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
            string query = $"SELECT COUNT(cd.uid) AS uid_count, a.name, cd.columndata ->>'Division Code' as Division  FROM product cd " +
                $"JOIN uidinitiation u ON u.planocode = cd.columndata ->> 'Planogram/Family Code' " +
                $"JOIN appuser a ON a.id = cd.createdby " +
                $"WHERE cd.columndata ->>'Division Code' = '{divisionId}' and u.status = 'Approved' ";

            if (fromDate != null || toDate != null)
            {
                query += $" And to_char(u.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{fromDateStr}' AND '{toDateStr}' ";
            }

            query += $" GROUP BY GROUPING SETS ((cd.columndata ->>'Division Code', a.name), (cd.columndata ->>'Division Code'))";

            // string query = $"WITH PlanocodesCreatedByUser AS ( SELECT u.createdby, u.planocode,u.datecreated FROM uidinitiation u ";
            // if (fromDate != null || toDate != null)
            // {
            //    query += $" where to_char(u.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{fromDateStr}' AND '{toDateStr}' ";
            // }

            // query += $"),UserRecordCounts AS (SELECT pu.createdby, COUNT(p.uid) " +
            //    $"AS uid_count FROM product p  JOIN PlanocodesCreatedByUser pu ON p.columndata ->> 'Planogram/Family Code' = pu.planocode WHERE " +
            //    $"p.columndata ->> 'Division Code' = '{divisionId}'    GROUP BY pu.createdby) SELECT urc.uid_count, a.name FROM UserRecordCounts urc " +
            //    $"JOIN appuser a ON a.id = urc.createdby";
            var count = await connection.QueryAsync<PlanoUidUserCount>(query);
            return count.ToList();
        }

        public async Task<List<TemplateUidCountModel>> GetTemplatesUidCount(string? division_code, string? dept_code, string? class_code, string? subclass_code, DateTime? fromDate, DateTime? toDate)
        {
            using var connection = this.GetConnection();
            if (division_code != null || dept_code != null || class_code != null || subclass_code != null || fromDate != null || toDate != null)
            {
                string[] divisionCodes = division_code?.Split(',');
                string[] deptCodes = dept_code?.Split(',');
                string[] classCodes = class_code?.Split(',');
                string[] subclassCodes = subclass_code?.Split(',');
                string fromDateStr = fromDate?.Date.ToString("yyyy/MM/dd");
                string toDateStr = toDate?.Date.ToString("yyyy/MM/dd");

                string deptCodeFilter = deptCodes != null && deptCodes.Any() ? $"ARRAY[{string.Join(",", deptCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                string divisionCodeFilter = divisionCodes != null && divisionCodes.Any() ? $"ARRAY[{string.Join(",", divisionCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                string classCodeFilter = classCodes != null && classCodes.Any() ? $"ARRAY[{string.Join(",", classCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                string subclassCodeFilter = subclassCodes != null && subclassCodes.Any() ? $"ARRAY[{string.Join(",", subclassCodes.Select(x => "'" + x + "'"))}]" : "NULL";
                string startDateFilter = fromDateStr != null ? $"'{fromDateStr}'" : "NULL";
                string endDateFilter = toDateStr != null ? $"'{toDateStr}'" : "NULL";

                string query = $"SELECT * FROM uidcount(" +
                    $"dept_code_filter := {deptCodeFilter}," +
                    $"division_code_filter := {divisionCodeFilter}," +
                    $"class_code_filter := {classCodeFilter}," +
                    $"subclass_code_filter := {subclassCodeFilter}," +
                    $"start_date := {startDateFilter}," +
                    $"end_date := {endDateFilter}" +
                    ")";

                var result = await connection.QueryAsync<TemplateUidCountModel>(query);
                return result.ToList();
            }
            else
            {
                var result = await connection.QueryAsync<TemplateUidCountModel>("SELECT * FROM uidcount()");
                return result.ToList();
            }
        }

        public async Task<List<TemplateUidCountModel>> GetTemplatesUidDivisionCount()
        {
            using var connection = this.GetConnection();
            var result = await connection.QueryAsync<TemplateUidCountModel>("SELECT * FROM uidcount()");
            return result.ToList();
        }

        public async Task<List<TemplateDeptCount>> GetDeptUidCount(long divisionId)
        {
            using var connection = this.GetConnection();

            var buyer1 = (await this.GetDeptBuyer1Count(divisionId)).ToList();
            return buyer1;
        }

        public async Task<List<TemplateDeptCount>> GetDeptBuyer1Count(long divisionId)
        {
            using var connection = this.GetConnection();
            if (divisionId == 61)
            {
                var counts = await connection.QueryAsync<TemplateDeptCount>($@"
        SELECT dept_name,
               division_code,
               SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%4%' THEN 1 ELSE 0 END) AS buyer1_count,
               SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%6%' THEN 1 ELSE 0 END) AS buyer2_count,
               SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%8%' THEN 1 ELSE 0 END) AS qc_count,
               SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%10%' THEN 1 ELSE 0 END) AS packaging_count,
               SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%12%' THEN 1 ELSE 0 END) AS fab_count,
               COUNT(CASE WHEN t.status = '1' THEN t.status END) as itemcode_ready,
               COUNT(CASE WHEN statusfilter = '1' THEN statusfilter END) as cam_ready
        FROM vw_columndata t LEFT JOIN uidinitiation u ON planogram_family_code = u.planocode
        WHERE division_code = '{divisionId}' 
        AND dept_name IS NOT NULL 
        AND u.status = 'Approved'::text
        GROUP BY dept_name, division_code
        ORDER BY division_code, dept_name");
                ////AND (templateid LIKE '%2%' AND (templateid LIKE '%4%' OR templateid LIKE '%6%' OR templateid LIKE '%8%' OR templateid LIKE '%10%' OR templateid LIKE '%12%'))  AND status = '1'  AND dept_name != 'Total' 
                return counts.ToList();
            }
            else if (divisionId == 4)
            {
                var counts = await connection.QueryAsync<TemplateDeptCount>($@"
        SELECT dept_name,
               division_code,
               SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%5%' THEN 1 ELSE 0 END) AS buyer1_count,
               SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%7%' THEN 1 ELSE 0 END) AS buyer2_count,
               SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%9%' THEN 1 ELSE 0 END) AS qc_count,
               SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%11%' THEN 1 ELSE 0 END) AS packaging_count,
               SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%13%' THEN 1 ELSE 0 END) AS fab_count,
               COUNT(CASE WHEN t.status = '1' THEN t.status END) as itemcode_ready,
               COUNT(CASE WHEN statusfilter = '1' THEN statusfilter END) as cam_ready
        FROM vw_columndata t LEFT JOIN uidinitiation u ON planogram_family_code = u.planocode
        WHERE division_code = '{divisionId}' 
        AND dept_name IS NOT NULL 
        AND u.status = 'Approved'::text
        GROUP BY dept_name, division_code
        ORDER BY division_code, dept_name");
                ////  AND (templateid LIKE '%3%' AND (templateid LIKE '%5%' OR templateid LIKE '%7%' OR templateid LIKE '%9%' OR templateid LIKE '%11%' OR templateid LIKE '%13%'))  AND status = '1' AND dept_name != 'Total' 
                return counts.ToList();
            }
            else
            {
                var counts = await connection.QueryAsync<TemplateDeptCount>($@"
        SELECT dept_name,
               division_code,
               SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%19%' THEN 1 ELSE 0 END) AS buyer1_count,
               SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%18%' THEN 1 ELSE 0 END) AS buyer2_count,
               SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%22%' THEN 1 ELSE 0 END) AS qc_count,
               SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%21%' THEN 1 ELSE 0 END) AS packaging_count,
               SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%20%' THEN 1 ELSE 0 END) AS fab_count,
               COUNT(CASE WHEN t.status = '1' THEN t.status END) as itemcode_ready,
               COUNT(CASE WHEN statusfilter = '1' THEN statusfilter END) as cam_ready
        FROM vw_columndata t LEFT JOIN uidinitiation u ON planogram_family_code = u.planocode
        WHERE division_code = '{divisionId}' 
        AND dept_name IS NOT NULL 
        AND u.status = 'Approved'::text
        GROUP BY dept_name, division_code
        ORDER BY division_code, dept_name");
                //// AND (templateid LIKE '%17%' AND (templateid LIKE '%19%' OR templateid LIKE '%18%' OR templateid LIKE '%22%' OR templateid LIKE '%21%' OR templateid LIKE '%20%')) AND status = '1' AND dept_name != 'Total' 
                return counts.ToList();
            }
        }

        public async Task<List<TemplateDeptCount>> GetDeptAllCount(long divisionId, string? dept, string? deptclass, string subclass, DateTime? fromDate, DateTime? toDate)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            string fromDateStr = startDates.Date.ToString("yyyy/MM/dd");
            string toDateStr = endDates.Date.ToString("yyyy/MM/dd");
            if (divisionId == 61)
            {
                var query = $"SELECT t.dept_code, t.dept_name, t.division_code as Division_Code, COUNT(*) AS dept_counts, " +
                    $"SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%4%' THEN 1 ELSE 0 END) AS buyer1_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%6%' THEN 1 ELSE 0 END) AS buyer2_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%8%' THEN 1 ELSE 0 END) AS qc_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%10%' THEN 1 ELSE 0 END) AS packaging_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%2%' AND templateid LIKE '%12%' THEN 1 ELSE 0 END) AS fab_count, " +
                    $"COUNT(CASE WHEN t.status = '1' THEN  t.status END) as itemcode_ready, " +
                    $"COUNT(CASE WHEN statusfilter = '1' " +
                    $"AND EXISTS(SELECT 1 FROM itemcodestaging WHERE itemcodestaging.uidcode = t.uid) THEN 1 ELSE NULL END) AS cam_ready " +
                    $"FROM vw_columndata t " +
                    $"LEFT JOIN uidinitiation u ON t.planogram_family_code = u.planocode " +
                    $"WHERE u.status = 'Approved' AND t.division_code = '{divisionId}'";

                if (dept != null)
                {
                    var formattedDept = string.Join(",", dept.Split(',').Select(d => $"'{d.Trim()}'"));
                    query += $" AND t.dept_code IN ({formattedDept})";

                    if (deptclass != null)
                    {
                        var formattedDeptClass = string.Join(",", deptclass.Split(',').Select(dc => $"'{dc.Trim()}'"));
                        query += $" AND t.class_code IN ({formattedDeptClass})";

                        if (subclass != null)
                        {
                            var formattedSubclass = string.Join(",", subclass.Split(',').Select(sc => $"'{sc.Trim()}'"));
                            query += $" AND t.subclass_code IN ({formattedSubclass})";
                        }
                    }
                }

                if (fromDate != null || fromDate != null)
                {
                    query += $" AND to_char(t.datecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN '{fromDateStr}' AND '{toDateStr}'";
                }

                query += " GROUP BY t.dept_code, t.dept_name, t.division_code ";

                return (await connection.QueryAsync<TemplateDeptCount>(query)).ToList();
            }
            else if (divisionId == 4)
            {
                var query = $"SELECT t.dept_code, t.dept_name, t.division_code as Division_Code, COUNT(*) AS dept_counts, " +
                    $"SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%5%' THEN 1 ELSE 0 END) AS buyer1_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%7%' THEN 1 ELSE 0 END) AS buyer2_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%9%' THEN 1 ELSE 0 END) AS qc_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%11%' THEN 1 ELSE 0 END) AS packaging_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%3%' AND templateid LIKE '%13%' THEN 1 ELSE 0 END) AS fab_count, " +
                    $"COUNT(CASE WHEN t.status = '1' THEN  t.status END) as itemcode_ready, " +
                    $"COUNT(CASE WHEN statusfilter = '1' " +
                    $"AND EXISTS(SELECT 1 FROM itemcodestaging WHERE itemcodestaging.uidcode = t.uid) THEN 1 ELSE NULL END) AS cam_ready " +
                    $"FROM vw_columndata t " +
                    $"LEFT JOIN uidinitiation u ON t.planogram_family_code = u.planocode " +
                    $"WHERE u.status = 'Approved' AND t.division_code = '{divisionId}'";

                if (dept != null)
                {
                    var formattedDept = string.Join(",", dept.Split(',').Select(d => $"'{d.Trim()}'"));
                    query += $" AND t.dept_code IN ({formattedDept})";

                    if (deptclass != null)
                    {
                        var formattedDeptClass = string.Join(",", deptclass.Split(',').Select(dc => $"'{dc.Trim()}'"));
                        query += $" AND t.class_code IN ({formattedDeptClass})";

                        if (subclass != null)
                        {
                            var formattedSubclass = string.Join(",", subclass.Split(',').Select(sc => $"'{sc.Trim()}'"));
                            query += $" AND t.subclass_code IN ({formattedSubclass})";
                        }
                    }
                }

                if (fromDate != null || toDate != null)
                {
                    query += $" AND to_char(t.datecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN '{fromDateStr}' AND '{toDateStr}'";
                }

                query += " GROUP BY t.dept_code, t.dept_name, t.division_code ";

                var counts = await connection.QueryAsync<TemplateDeptCount>(query);
                return counts.ToList();
            }
            else
            {
                var query = $"SELECT t.dept_code, t.dept_name, t.division_code as Division_Code, COUNT(*) AS dept_counts, " +
                    $"SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%19%' THEN 1 ELSE 0 END) AS buyer1_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%18%' THEN 1 ELSE 0 END) AS buyer2_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%22%' THEN 1 ELSE 0 END) AS qc_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%21%' THEN 1 ELSE 0 END) AS packaging_count, " +
                    $"SUM(CASE WHEN templateid LIKE '%17%' AND templateid LIKE '%20%' THEN 1 ELSE 0 END) AS fab_count, " +
                    $"COUNT(CASE WHEN t.status = '1' THEN  t.status END) as itemcode_ready, " +
                    $"COUNT(CASE WHEN statusfilter = '1' " +
                    $"AND EXISTS(SELECT 1 FROM itemcodestaging WHERE itemcodestaging.uidcode = t.uid) THEN 1 ELSE NULL END) AS cam_ready " +
                    $"FROM vw_columndata t " +
                    $"LEFT JOIN uidinitiation u ON t.planogram_family_code = u.planocode " +
                    $"WHERE u.status = 'Approved' AND t.division_code = '{divisionId}'";

                if (dept != null)
                {
                    var formattedDept = string.Join(",", dept.Split(',').Select(d => $"'{d.Trim()}'"));
                    query += $" AND t.dept_code IN ({formattedDept})";

                    if (deptclass != null)
                    {
                        var formattedDeptClass = string.Join(",", deptclass.Split(',').Select(dc => $"'{dc.Trim()}'"));
                        query += $" AND t.class_code IN ({formattedDeptClass})";

                        if (subclass != null)
                        {
                            var formattedSubclass = string.Join(",", subclass.Split(',').Select(sc => $"'{sc.Trim()}'"));
                            query += $" AND t.subclass_code IN ({formattedSubclass})";
                        }
                    }
                }

                if (fromDate != null || toDate != null)
                {
                    query += $" AND to_char(t.datecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN '{fromDateStr}' AND '{toDateStr}'";
                }

                query += " GROUP BY t.dept_code, t.dept_name, t.division_code ORDER BY t.division_code, t.dept_name";

                var counts = await connection.QueryAsync<TemplateDeptCount>(query);
                return counts.ToList();
            }
        }

        public async Task<byte[]> UserExportExcel(bool? isActive)
        {
            using var connection = this.GetConnection();
            var result = new List<ExportUserModel>();

            if (isActive is not null)
            {
                result = (await connection.QueryAsync<ExportUserModel>($"SELECT a.name AS Name, a.email AS Email, a.mobile AS Mobile, a.isactive, a.rolelevel AS RoleLevel, " +
                                                            $"MAX(d.name) AS Division, MAX(r.name) AS Roles, MAX(s.name) AS Studio FROM appuser a " +
                                                            $"LEFT JOIN appuserrole ap ON ap.appuserid = a.id " +
                                                            $"LEFT JOIN role r ON r.id = ap.roleid " +
                                                            $"LEFT JOIN division d ON d.id = ap.departmentid " +
                                                            $"LEFT JOIN photoshootuser ph ON ph.userid = a.id " +
                                                            $"LEFT JOIN studio s ON s.id = ph.studioid where a.isactive = '{isActive}' " +
                                                            $"GROUP BY a.name, a.email, a.mobile, a.isactive,a.rolelevel " +
                                                            $"ORDER BY a.name asc")).ToList();
            }
            else
            {
                result = (await connection.QueryAsync<ExportUserModel>("SELECT a.name AS Name, a.email AS Email, a.mobile AS Mobile, a.isactive, a.rolelevel AS RoleLevel, " +
                                                            "MAX(d.name) AS Division, MAX(r.name) AS Roles, MAX(s.name) AS Studio FROM appuser a " +
                                                            "LEFT JOIN appuserrole ap ON ap.appuserid = a.id " +
                                                            "LEFT JOIN role r ON r.id = ap.roleid " +
                                                            "LEFT JOIN division d ON d.id = ap.departmentid " +
                                                            "LEFT JOIN photoshootuser ph ON ph.userid = a.id " +
                                                            "LEFT JOIN studio s ON s.id = ph.studioid " +
                                                            "GROUP BY a.name, a.email, a.mobile, a.isactive,a.rolelevel " +
                                                            "ORDER BY a.name asc")).ToList();
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("User Records");

            if (result.Any())
            {
                var headers = new[] { "Name", "Email", "Mobile", "Division", "Studio", "Roles", "RoleLevel", "Isactive" };
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
                    worksheet.Cells[row, 1].Value = record.Name;
                    worksheet.Cells[row, 2].Value = record.Email;
                    worksheet.Cells[row, 3].Value = record.Mobile;
                    worksheet.Cells[row, 4].Value = record.Division;
                    worksheet.Cells[row, 5].Value = record.Studio;
                    worksheet.Cells[row, 6].Value = record.Roles;
                    worksheet.Cells[row, 7].Value = record.RoleLevel;
                    worksheet.Cells[row, 8].Value = record.Isactive;

                    worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                    for (int col = 1; col <= 8; col++)
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
            }

            using var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<byte[]> RoleExportExcel(bool? isActive)
        {
            using var connection = this.GetConnection();
            var result = new List<ExportRoleModel>();
            if (isActive is not null)
            {
                result = (await connection.QueryAsync<ExportRoleModel>($"select name,description,isactive from role where isactive = '{isActive}' order by name asc")).ToList();
            }
            else
            {
                result = (await connection.QueryAsync<ExportRoleModel>("select name,description,isactive from role")).ToList();
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Role Records");

            if (result.Any())
            {
                var headers = new[] { "Role", "Description", "IsActive" };
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
                    worksheet.Cells[row, 1].Value = record.Name;
                    worksheet.Cells[row, 2].Value = record.Description;
                    worksheet.Cells[row, 3].Value = record.IsActive;

                    worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                    for (int col = 1; col <= 3; col++)
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
            }

            using var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task DeleteUser(IEnumerable<long> ids)
        {
            using var connection = this.GetConnection();
            foreach (var id in ids)
            {
                await connection.ExecuteAsync($"Delete from loginhistory where appuserid = {id}");
                await connection.ExecuteAsync($"DELETE FROM APPUSERROLE WHERE appuserid = {id}");
                await connection.ExecuteAsync($"DELETE FROM APPUSER WHERE id = {id}");
            }
        }

        public async Task DeleteRole(IEnumerable<long> ids)
        {
            using var connection = this.GetConnection();
            foreach (var id in ids)
            {
                await connection.ExecuteAsync($"Delete from appuserrole where roleid = {id}");
                await connection.ExecuteAsync($"Delete from role where id = {id}");
            }
        }

        public async Task<SearchResult<Role>> SearchAllRoles(IsActiveFilter isActiveFilter)
        {
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            return await this.Search<Role>(string.Empty, 0, 0, "role",
                isActive: isActivee, orderBy: "name");
        }

        public async Task<SearchResult<ProductAttribute>> SearchAllAttributes(IsActiveFilter isActiveFilter)
        {
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            return await this.Search<ProductAttribute>(string.Empty, 0, 0, "attribute",
                isActive: isActivee, orderBy: "name");
        }

        public async Task<IEnumerable<ProductAttribute>> SearchAttributes(IsActiveFilter isActiveFilter, string? divisionId)
        {
            using var connection = this.GetConnection();
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            if (isActivee is not null && divisionId is not null)
            {
                return await connection.QueryAsync<ProductAttribute>($"select * from productattribute where isactive = '{isActivee}' and divisionid IN ({divisionId}) order by name asc");
            }
            else if (isActivee is not null)
            {
                return await connection.QueryAsync<ProductAttribute>($"SELECT * FROM productattribute WHERE isactive = '{isActivee}' AND divisionid = 61 ORDER BY name ASC");
            }
            else if (divisionId is not null)
            {
                return await connection.QueryAsync<ProductAttribute>($"select * from productattribute where isactive IS NOT NULL and divisionid IN ({divisionId}) order by name asc");
            }
            else
            {
                return await connection.QueryAsync<ProductAttribute>($"select * from productattribute where isactive IS NOT NULL and divisionid = 61 order by name asc");
            }
        }

        public async Task<IEnumerable<Template>> SearchTemplates(IsActiveFilter isActiveFilter)
        {
            using var connection = this.GetConnection();
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            if (isActivee is not null)
            {
                return await connection.QueryAsync<Template>($"select * from template where isactive = '{isActivee}' order by name desc");
            }
            else
            {
                return await connection.QueryAsync<Template>($"select * from template where isactive IS NOT NULL order by name desc");
            }
        }

        public async Task<SearchResult<Template>> SearchAllTemplates(IsActiveFilter isActiveFilter)
        {
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            return await this.Search<Template>(string.Empty, 0, 0, "template",
                isActive: isActivee, orderBy: "name");
        }

        public async Task<SearchResult<Marketplace>> SearchAllMarketPlace(IsActiveFilter isActiveFilter)
        {
            bool? isActivee = null;

            switch (isActiveFilter)
            {
                case IsActiveFilter.Yes:
                    isActivee = true;
                    break;
                case IsActiveFilter.No:
                    isActivee = false;
                    break;
                case IsActiveFilter.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(isActiveFilter));
            }

            return await this.Search<Marketplace>(string.Empty, 0, 0, "marketplace",
                isActive: isActivee, orderBy: "name");
        }

        public async Task<List<PlanoCountModel>> GetDivisionPlanoCount()
        {
            using var connection = this.GetConnection();
            var count = await connection.QueryAsync<PlanoCountModel>($"SELECT * FROM get_counts_by_status(null,null,ARRAY[61,4,65]);");

            foreach (var planoCountModel in count)
            {
                if (planoCountModel.Division_code == null)
                {
                    planoCountModel.Division_code = 0;
                }
            }

            return count.ToList();
        }

        public async Task<string> DesignerL1Digestemail()
        {
            using var connection = this.GetConnection();

            var emailContentListHC = await connection.QueryAsync<EmailItem>(
                                    "SELECT " +
                                    "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                                    "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                                    "a.name AS UserName,d.name as Division " +
                                    "FROM uidinitiation u " +
                                    "JOIN appuser a ON u.createdby = a.id " +
                                    "JOIN division d ON d.id = u.divisionid " +
                                    "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid = 61 order by u.datecreated desc");

            var emailContentListFUR = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid = 4 order by u.datecreated desc");

            var emailContentListCC = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid = 65 order by u.datecreated desc");

            var emailContentListHCFUR = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid in (61,4) order by u.datecreated desc");

            var emailContentListHCCC = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid in (65,4) order by u.datecreated desc");

            var emailContentListFURCC = await connection.QueryAsync<EmailItem>(
             "SELECT " +
             "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
             "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
             "a.name AS UserName,d.name as Division " +
             "FROM uidinitiation u " +
             "JOIN appuser a ON u.createdby = a.id " +
             "JOIN division d ON d.id = u.divisionid " +
             "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid in (65,61) order by u.datecreated desc");

            var emailContentListFURCCHC = await connection.QueryAsync<EmailItem>(
             "SELECT " +
             "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
             "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
             "a.name AS UserName,d.name as Division " +
             "FROM uidinitiation u " +
             "JOIN appuser a ON u.createdby = a.id " +
             "JOIN division d ON d.id = u.divisionid " +
             "WHERE u.status = 'Pending' and u.firstlevelaction = 'Pending' and u.divisionid in (65,61,4) order by u.datecreated desc");

            var action = await connection.QueryFirstOrDefaultAsync<string>("select template from emailtemplate where action = 'Designer'");
            var query = await connection.QueryFirstOrDefaultAsync<string>("select query from emailtemplate where action = 'Designer'");
            var users = await connection.QueryAsync<BuyerDto>(query);

            var householdEmails = new List<string>();
            var furnitureEmails = new List<string>();
            var cashandcarryEmails = new List<string>();

            var hcFURDepartmentEmails = new List<string>();
            var hcCCDepartmentEmails = new List<string>();
            var ccFURDepartmentEmails = new List<string>();

            var hcFURCCDepartmentEmails = new List<string>();

            foreach (var user in users)
            {
                if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold") && user.Department.Contains("HC-Cash And Carry"))
                {
                    hcFURCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold"))
                {
                    hcFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-Cash And Carry"))
                {
                    ccFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry") && user.Department.Contains("HC-House Hold"))
                {
                    hcCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-House Hold"))
                {
                    householdEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture"))
                {
                    furnitureEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry"))
                {
                    cashandcarryEmails.Add(user.Email);
                }
            }

            if (action == "yes")
            {
                foreach (var currentemail in householdEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in furnitureEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFUR.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in cashandcarryEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcFURDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHCFUR.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcCCDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFURCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in ccFURDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHCCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcFURCCDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFURCCHC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                return "Mail sent successfully";
            }
            else
            {
                return "Mail not sent";
            }
        }

        public async Task<string> BuyerL1Digestemail()
        {
            using var connection = this.GetConnection();

            var emailContentListHC = await connection.QueryAsync<EmailItem>(
                                    "SELECT " +
                                    "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                                    "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                                    "a.name AS UserName,d.name as Division " +
                                    "FROM uidinitiation u " +
                                    "JOIN appuser a ON u.createdby = a.id " +
                                    "JOIN division d ON d.id = u.divisionid " +
                                    "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid = 61 order by u.datecreated desc");

            var emailContentListFUR = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid = 4 order by u.datecreated desc");

            var emailContentListCC = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid = 65 order by u.datecreated desc");

            var emailContentListHCFUR = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid in (61,4) order by u.datecreated desc");

            var emailContentListHCCC = await connection.QueryAsync<EmailItem>(
                "SELECT " +
                "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
                "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
                "a.name AS UserName,d.name as Division " +
                "FROM uidinitiation u " +
                "JOIN appuser a ON u.createdby = a.id " +
                "JOIN division d ON d.id = u.divisionid " +
                "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid in (65,4) order by u.datecreated desc");

            var emailContentListFURCC = await connection.QueryAsync<EmailItem>(
             "SELECT " +
             "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
             "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
             "a.name AS UserName,d.name as Division " +
             "FROM uidinitiation u " +
             "JOIN appuser a ON u.createdby = a.id " +
             "JOIN division d ON d.id = u.divisionid " +
             "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid in (65,61) order by u.datecreated desc");

            var emailContentListFURCCHC = await connection.QueryAsync<EmailItem>(
             "SELECT " +
             "TRIM(BOTH ' ' FROM CONCAT(planogram, ' ', familyname)) AS PlanoOrFamilyName, " +
             "TO_CHAR(u.datecreated, 'DD/MM/YYYY') AS CreatedDate, " +
             "a.name AS UserName,d.name as Division " +
             "FROM uidinitiation u " +
             "JOIN appuser a ON u.createdby = a.id " +
             "JOIN division d ON d.id = u.divisionid " +
             "WHERE u.status = 'Pending' and u.firstlevelaction = 'Approved' and u.divisionid in (65,61,4) order by u.datecreated desc");

            var action = await connection.QueryFirstOrDefaultAsync<string>("select template from emailtemplate where action = 'Buyer'");
            var query = await connection.QueryFirstOrDefaultAsync<string>("select query from emailtemplate where action = 'Buyer'");
            var users = await connection.QueryAsync<BuyerDto>(query);

            var householdEmails = new List<string>();
            var furnitureEmails = new List<string>();
            var cashandcarryEmails = new List<string>();

            var hcFURDepartmentEmails = new List<string>();
            var hcCCDepartmentEmails = new List<string>();
            var ccFURDepartmentEmails = new List<string>();

            var hcFURCCDepartmentEmails = new List<string>();

            foreach (var user in users)
            {
                if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold") && user.Department.Contains("HC-Cash And Carry"))
                {
                    hcFURCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold"))
                {
                    hcFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-Cash And Carry"))
                {
                    ccFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry") && user.Department.Contains("HC-House Hold"))
                {
                    hcCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-House Hold"))
                {
                    householdEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture"))
                {
                    furnitureEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry"))
                {
                    cashandcarryEmails.Add(user.Email);
                }
            }

            if (action == "yes")
            {
                foreach (var currentemail in householdEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in furnitureEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFUR.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in cashandcarryEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcFURDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHCFUR.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcCCDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFURCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in ccFURDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListHCCC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                foreach (var currentemail in hcFURCCDepartmentEmails)
                {
                    await _mailEngine.SendEmail("DigestEmail", new DigestEmailContent
                    {
                        EmailContent = emailContentListFURCCHC.ToList()
                    }, "Planogram/Family approval pending", null, new[] { currentemail });
                }

                return "Mail sent successfully";
            }
            else
            {
                return "Mail Not Sent";
            }
        }

        public async Task<string> ItemcodeDigestemail()
        {
            using var connection = this.GetConnection();

            var emailContentListHC = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name = 'HC-House Hold' AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailContentList = emailContentListHC.ToList();

            var emailContentListFUR = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name = 'HC-Furniture' AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailFurList = emailContentListFUR.ToList();

            var emailContentListCC = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name = 'HC-Cash And Carry' AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailCCList = emailContentListCC.ToList();

            var emailContentListHCFUR = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name in ('HC-House Hold','HC-Furniture') AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailHCFURList = emailContentListHCFUR.ToList();

            var emailContentListHCCC = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name in ('HC-House Hold','HC-Cash And Carry') AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailHCCCList = emailContentListHCCC.ToList();

            var emailContentListFURCC = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name in ('HC-Cash And Carry','HC-Furniture') AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailFURCCList = emailContentListFURCC.ToList();

            var emailContentListHCFURCC = await connection.QueryAsync<ItemCodecontent>("select uid as Uid,division_name as Division from vw_columndata where status = '1' AND  division_name in ('HC-Cash And Carry','HC-Furniture','HC-House Hold') AND CAST(datecreated AS DATE) = CURRENT_DATE");
            var emailHCFURCCList = emailContentListHCFURCC.ToList();

            var itemcodeCount = await connection.QueryFirstOrDefaultAsync<int>("select count(*) from vw_columndata where status = '1'");

            var action = await connection.QueryFirstOrDefaultAsync<string>("select template from emailtemplate where action = 'Itemcode'");
            var query = await connection.QueryFirstOrDefaultAsync<string>("select query from emailtemplate where action = 'Itemcode'");
            var users = await connection.QueryAsync<BuyerDto>(query);

            var householdEmails = new List<string>();
            var furnitureEmails = new List<string>();
            var cashandcarryEmails = new List<string>();

            var hcFURDepartmentEmails = new List<string>();
            var hcCCDepartmentEmails = new List<string>();
            var ccFURDepartmentEmails = new List<string>();

            var hcFURCCDepartmentEmails = new List<string>();

            foreach (var user in users)
            {
                if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold") && user.Department.Contains("HC-Cash And Carry"))
                {
                    hcFURCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold"))
                {
                    hcFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-Cash And Carry"))
                {
                    ccFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry") && user.Department.Contains("HC-House Hold"))
                {
                    hcCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-House Hold"))
                {
                    householdEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture"))
                {
                    furnitureEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry"))
                {
                    cashandcarryEmails.Add(user.Email);
                }
            }

            if (action == "yes")
            {
                if (emailContentList.Count() > 0)
                {
                    foreach (var currentemail in householdEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListHC.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailFurList.Count() > 0)
                {
                    foreach (var currentemail in furnitureEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListFUR.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailCCList.Count() > 0)
                {
                    foreach (var currentemail in cashandcarryEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListCC.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCFURList.Count() > 0)
                {
                    foreach (var currentemail in hcFURDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListHCFUR.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCCCList.Count() > 0)
                {
                    foreach (var currentemail in hcCCDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListHCCC.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailFURCCList.Count() > 0)
                {
                    foreach (var currentemail in ccFURDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListFURCC.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCFURCCList.Count() > 0)
                {
                    foreach (var currentemail in hcFURCCDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("ItemcodeEmail", new ItemCodeEmailContent
                        {
                            ItemContent = emailContentListHCFURCC.ToList(),
                            TotalItemcodeCount = itemcodeCount
                        }, "Itemcode ready Status", null, new[] { currentemail });
                    }
                }

                return "Mail sent successfully";
            }
            else
            {
                return "Mail Not Sent";
            }
        }

        public async Task<string> CamDigestemail()
        {
            using var connection = this.GetConnection();

            var emailContentListHC = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name = 'HC-House Hold' AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailcontentHCList = emailContentListHC.ToList();

            var emailContentListFUR = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name = 'HC-Furniture' AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailcontentFURList = emailContentListFUR.ToList();

            var emailContentListCC = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND  cd.division_name = 'HC-Cash And Carry' AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailcontentCCList = emailContentListCC.ToList();

            var emailContentListHCCC = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name in ('HC-Cash And Carry','HC-House Hold') AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailHCCCList = emailContentListHCCC.ToList();

            var emailContentListHCFUR = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name in ('HC-Furniture','HC-House Hold') AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailHCFURList = emailContentListHCCC.ToList();

            var emailContentListCCFUR = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name in ('HC-Cash And Carry','HC-Furniture') AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailFURCCList = emailContentListHCCC.ToList();

            var emailContentListHCCCFUR = await connection.QueryAsync<Camcontent>("select cd.uid as Uid,cd.division_name as Division,ui.planogram_family_name as PlanoOrFamily,i.code as Itemcode from vw_columndata cd " +
                                                                                "join itemcodestaging i on cd.uid = i.uidcode " +
                                                                                "join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1' AND cd.division_name in ('HC-Cash And Carry','HC-House Hold','HC-Furniture') AND CAST(cd.datecreated AS DATE) = CURRENT_DATE");
            var emailHCCCFURList = emailContentListHCCC.ToList();

            var camCount = await connection.QueryFirstOrDefaultAsync<int>("select count(*) from vw_columndata cd join itemcodestaging i on cd.uid = i.uidcode join vw_uid ui on cd.uid = ui.uid " +
                                                                                "where cd.statusfilter = '1'");
            var action = await connection.QueryFirstOrDefaultAsync<string>("select template from emailtemplate where action = 'CAM'");
            var query = await connection.QueryFirstOrDefaultAsync<string>("select query from emailtemplate where action = 'CAM'");
            var users = await connection.QueryAsync<BuyerDto>(query);

            var householdEmails = new List<string>();
            var furnitureEmails = new List<string>();
            var cashandcarryEmails = new List<string>();

            var hcFURDepartmentEmails = new List<string>();
            var hcCCDepartmentEmails = new List<string>();
            var ccFURDepartmentEmails = new List<string>();

            var hcFURCCDepartmentEmails = new List<string>();

            foreach (var user in users)
            {
                if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold") && user.Department.Contains("HC-Cash And Carry"))
                {
                    hcFURCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-House Hold"))
                {
                    hcFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture") && user.Department.Contains("HC-Cash And Carry"))
                {
                    ccFURDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry") && user.Department.Contains("HC-House Hold"))
                {
                    hcCCDepartmentEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-House Hold"))
                {
                    householdEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Furniture"))
                {
                    furnitureEmails.Add(user.Email);
                }
                else if (user.Department.Contains("HC-Cash And Carry"))
                {
                    cashandcarryEmails.Add(user.Email);
                }
            }

            if (action == "yes")
            {
                if (emailcontentHCList.Count() > 0)
                {
                    foreach (var currentemail in householdEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListHC.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailcontentFURList.Count() > 0)
                {
                    foreach (var currentemail in furnitureEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListFUR.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailcontentCCList.Count() > 0)
                {
                    foreach (var currentemail in cashandcarryEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListCC.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCCCList.Count() > 0)
                {
                    foreach (var currentemail in hcCCDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListHCCC.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCFURList.Count() > 0)
                {
                    foreach (var currentemail in hcFURDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListHCFUR.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailFURCCList.Count() > 0)
                {
                    foreach (var currentemail in ccFURDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListCCFUR.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                if (emailHCCCFURList.Count() > 0)
                {
                    foreach (var currentemail in hcFURCCDepartmentEmails)
                    {
                        await _mailEngine.SendEmail("CamEmail", new CamEmailContent
                        {
                            CamContent = emailContentListHCCCFUR.ToList(),
                            TotalCamCount = camCount
                        }, "CAM ready Status", null, new[] { currentemail });
                    }
                }

                return "Mail sent successfully";
            }
            else
            {
                return "Mail Not Sent";
            }
        }

        public async Task<IEnumerable<string>> ListFilesFromSftp()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(
                    _configuration["SFTPConfig:Host"],
                    int.Parse(_configuration["SFTPConfig:Port"]),
                    _configuration["SFTPConfig:Username"],
                    new PasswordAuthenticationMethod(_configuration["SFTPConfig:Username"], _configuration["SFTPConfig:Password"]));

                var sftpPaths = new List<string>
                 {
                    "/home/PIM/PIM_Master_Data/Master_Data",
                    "/home/PIM/PIM_Planogram_Family/RMS_PlanoFamily_Name&Code",
                    "/home/PIM/PIM_ItemCode/RMS_ItemCode"
                 };
                var tempFilePaths = new List<string>();

                using (var client = new SftpClient(connectionInfo))
                {
                    client.Connect();

                    foreach (var sftpPath in sftpPaths)
                    {
                        var files = client.ListDirectory(sftpPath);

                        foreach (var file in files)
                        {
                            if (!file.IsDirectory && IsAllowedFileType(file.Name))
                            {
                                using (var stream = new MemoryStream())
                                {
                                    client.DownloadFile(file.FullName, stream);
                                    stream.Position = 0;

                                    var fileName = Path.GetFileName(file.Name);
                                    var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

                                    if (file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                                    {
                                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                                        {
                                            stream.CopyTo(fileStream);
                                        }
                                    }
                                    else if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                                        {
                                            stream.CopyTo(fileStream);
                                        }
                                    }

                                    tempFilePaths.Add(tempFilePath);
                                }
                            }
                        }
                    }

                    client.Disconnect();
                }

                await UploadToAzureStorage(tempFilePaths);

                foreach (var tempFilePath in tempFilePaths)
                {
                    File.Delete(tempFilePath);
                }

                return tempFilePaths;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task UploadToAzureStorage(IEnumerable<string> tempFilePaths)
        {
            var azureConnection = _configuration["AzureBlobStorageKey"];
            var containerName = _configuration["SFTPContainerName"];

            var blobServiceClient = new BlobServiceClient(azureConnection);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            foreach (var filePath in tempFilePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var blobClient = containerClient.GetBlobClient(fileName);

                using (FileStream fs = File.OpenRead(filePath))
                {
                    await blobClient.UploadAsync(fs, true);
                }

                File.Delete(filePath);
            }
        }

        private bool IsAllowedFileType(string fileName)
        {
            var allowedFileTypes = new string[] { ".xlsx", ".csv" };

            return allowedFileTypes.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
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
    }
}