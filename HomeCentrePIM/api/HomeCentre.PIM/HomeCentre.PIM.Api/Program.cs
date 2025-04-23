using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using Hangfire;
using Hangfire.MemoryStorage;
using HomeCentre.PIM.BAL;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Implementations.B2C;
using HomeCentre.PIM.BAL.Implementations.FileProcessor;
using HomeCentre.PIM.BAL.Implementations.LandmarkProcess;
using HomeCentre.PIM.BAL.Implementations.Storage;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Cache;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.Common.PostgresTypes;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.Migrator;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Npgsql.Logging;
using Serilog;
using Serilog.Events;
using Storage.Net.Blobs;

namespace HomeCentre.PIM.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // load configuration file and prereq settings
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Staging";
            var jsonPath = $"appsettings.Api.{env}.json";
            var swaggerVersion =
                $"Build - {File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location):MM.dd.yyyy.HH.mm}";
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            if (env.IsDevOrQa())
            {
                NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Debug);
                NpgsqlLogManager.IsParameterLoggingEnabled = true;
            }

            SqlMapper.AddTypeHandler(new DictionaryObjectValueHandler());
            SqlMapper.AddTypeHandler(new DictionaryListOfStringValueHandler());

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            SimpleCRUD.SetTableNameResolver(new PostgresLowerCaseTableNameResolver());
            SimpleCRUD.SetColumnNameResolver(new PostgresLowerCaseColumnNameResolver());
            var clientId = Environment.GetEnvironmentVariable("AZ_CLIENTID");
            var clientSecret = Environment.GetEnvironmentVariable("AZ_SECRET");
            Console.WriteLine(clientId);
            Console.WriteLine(clientSecret);

            var builder = WebApplication.CreateBuilder(args);

            var services = builder.Services;

            builder.Configuration.AddJsonFile(jsonPath, optional: false, reloadOnChange: true);

            var configuration = builder.Configuration;
            builder.Configuration.AddEnvironmentVariables();
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                configuration.AddAzureKeyVault(
                    $"https://{configuration["Azure:KeyVaultName"]}.vault.azure.net/");
            }
            else
            {
                configuration.AddAzureKeyVault(
                    $"https://{configuration["Azure:KeyVaultName"]}.vault.azure.net/", clientId, clientSecret);
            }

            var instrumentationKey = configuration["Azure:ApplicationInsights:InstrumentationKey"];
            if (!string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")))
            {
                instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            }

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.InstrumentationKey = instrumentationKey;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo
                .ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
                .CreateLogger();
            builder.Host.UseSerilog();

            // if (env != "Development")
            // {
            //     var process = new MigrationProcess();
            //     process.Run(configuration["AppDB"]);
            // }

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            services.AddMemoryCache();
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());
            services.AddHangfireServer();
            services.AddAuthorizationPolicies();
            services.AddSingleton<IJsonSerializer>(s => new NewtonSoftJsonSerializer(Formatting.None,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
            Dapper.SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);
            SimpleCRUD.SetTableNameResolver(new PostgresLowerCaseTableNameResolver());
            SimpleCRUD.SetColumnNameResolver(new PostgresLowerCaseColumnNameResolver());
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDataProtection()
                .SetApplicationName("HC.PIM");
            services.AddHttpClient();
            services.AddTransient<IAuthenticationProvider, B2CAuthenticationProvider>();
            services.AddTransient<IAuthorizationHandler, HasRoleAuthorizationHandler>();
            var blob = configuration["UseFileShare"];
            if (blob.ToLower() == "false")
            {
                services.AddTransient<IBlobRepo, FileShareRepo>();
            }
            else
            {
                services.AddTransient<IBlobRepo, BlobRepo>();
            }

            services.AddTransient<IDataProcess, DataProcess>();
            services.AddTransient<IDistributedCache, InMemoryCacheHandler>();
            services.AddTransient<ICacheHandler, InMemoryCacheHandler>();
            services.AddTransient<IDbConnectionFactory>(s => new NpgSqlConnectionFactory(configuration));
            services.AddTransient<IDbCache, PgDbCache>();
            services.AddTransient<IMasterManager, PgMasterManager>();
            services.AddTransient<IUserContext, HttpUserContext>();
            services.AddTransient<ICipherService, CipherService>();
            services.AddTransient<IMailEngine, SmtpMailEngine>();
            services.AddTransient<IUserManager, PgUserManager>();
            services.AddTransient<IBlobStorage>(s =>
                new PostgreSqlBlobStorage(configuration.GetConnectionString("FileDB")));
            services.AddTransient<IFileManager, PgFileManager>();
            services.AddTransient<ILabelManager, PgLabelManager>();
            services.AddTransient<IErrorLogger, ErrorLogger>();
            services.AddTransient<IWorkFlowManager, TaskstepInstanceRepo>();
            services.AddTransient<IWorkFlowManager1, WorkFlowInstanceRepo>();
            services.AddTransient<IUIDManager, UIDRepo>();
            services.AddTransient<IUploadManager, PgUploadManager>();
            services.AddTransient<IDataProcessorFactory, PgDataProcessorFactory>();
            services.AddTransient<ITemplateManager, PgTemplateManager>();
            services.AddTransient<IProductManager, PgProductManager>();
            services.AddTransient<IFileExplorer, SftpExplorer>();
            services.AddTransient<IFileConfigurationManager, FileShareConfigurationManager>();
            services.AddTransient<IProductQueryManager, PgProductQueryManager>();
            services.AddTransient<IMarketplaceManager, PgMarketplaceManager>();
            services.AddTransient<IDataRequestManager, PgDataRequestManager>();
            services.AddTransient<IStudioManager, PgStudioManager>();
            services.AddTransient<IProductImageManager, PgProductImageManager>();
            services.AddTransient<IContentManager, PgContentManager>();
            services.AddTransient<IPlanningManager, PgPlanningManager>();
            services.AddTransient<ICamQcManager, PgCamQcManager>();
            services.AddTransient<IPhotoShootManager, PgPhotoShootManager>();

            services.AddTransient<IStorageFactory>(s =>
            {
                var factory = new DefaultStorageFactory();
                factory.AddStorage("pgsql",
                    () => new PostgreSqlBlobStorage(configuration.GetConnectionString("FileDB")));
                return factory;
            });

            services.AddMailService(configuration);
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ValidateModelStateAttribute));
                options.Filters.Add(typeof(ApiResponseFilter));
                options.Filters.Add(typeof(HandledExceptionFilter));
            });
            services.AddCors();
            services.AddResponseCompression();
            services.AddResponseCaching();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new OpenApiInfo
                    { Title = "HC.PIM Processor", Version = $"HC.PIM Processor V1 {swaggerVersion}" });
                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please insert ApiKey into field",
                        Name = "token",
                        Type = SecuritySchemeType.ApiKey
                    });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"}
                        },
                        new string[] { }
                    }
                });

                c.OperationFilter<EntityOperationFilter>();
                c.OperationFilter<FileUploadOperation>();
                c.SchemaFilter<EntitySchemaFilter>();
            });

            var app = builder.Build();

            app.UseHsts();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.None
            });
            app.UseAuthentication();
            app.UseResponseCaching();

            app.UseResponseCompression();
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", $"HC.PIM Processor V1 {swaggerVersion}"); });
            app.UseCors(option =>
                option.SetIsOriginAllowed((host) => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.UseHangfireDashboard("/process", new DashboardOptions
            {
                Authorization = new[] { new MyAuthorizationFilter() }
            });
            var manager = services.BuildServiceProvider().GetRequiredService<IFileConfigurationManager>();
            foreach (var fileConfig in manager.GetConfigurations().Result)
            {
                RecurringJob.AddOrUpdate(
                    $"fileManagerJob-{fileConfig.Name}",
                    () => manager.ProcessConfiguration(fileConfig),
                    "0 30 15 * * *");
            }

            // var uidrepo = services.BuildServiceProvider().GetRequiredService<IUIDManager>();
            // RecurringJob.AddOrUpdate(
            //    "DigestEmail",
            //    () => uidrepo.SendEmail(), "30 2 * * *"); // 8:00 AM
            var studio = services.BuildServiceProvider().GetRequiredService<IStudioManager>();
            RecurringJob.AddOrUpdate(
                "Studio",
                () => studio.UpdateIsactive(), "30 2 * * *"); // 8:00 AM

            var grn = services.BuildServiceProvider().GetRequiredService<IDataProcess>();
            RecurringJob.AddOrUpdate(
                "GRN",
                () => grn.ProcessDataFromTemplate("GRNList", false), "0 17 * * *"); // 10:30 PM

            var to = services.BuildServiceProvider().GetRequiredService<IDataProcess>();
            RecurringJob.AddOrUpdate(
                "TO",
                () => grn.ProcessDataFromTemplate("TOList", false), "30 17 * * *"); // 11:00 PM

            // var photoshoot = services.BuildServiceProvider().GetRequiredService<IProductImageManager>();
            // RecurringJob.AddOrUpdate(
            //    "Photoshoot",
            //    () => photoshoot.UpdatePhotoshootProduct(), "45 4 * * *"); // 10:15 AM

            // SFTP Timing for other Env

            // var sftp = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            // RecurringJob.AddOrUpdate(
            //    "SFTP",
            //    () => sftp.ListFilesFromSftp(), "30 4 * * *"); // 10:00 AM
            // var messageNames = new List<string>
            // {
            //    "EmployeeList",
            //    "RMSList",
            //    "ItemCodeList",
            //    "ClassList",
            //    "DepartmentList",
            //    "DivisionList",
            //    "GroupList",
            //    "SubclassList",
            //    "BrandList",
            //    "BusinessList",
            //    "MerchandiseList"
            // };

            // var blobToDb = services.BuildServiceProvider().GetRequiredService<IDataProcess>();
            // foreach (var messageName in messageNames)
            // {
            //    var jobName = "BlobToDb_" + messageName;
            //    RecurringJob.AddOrUpdate(jobName, () => blobToDb.ProcessDataFromTemplate(messageName, false), "30 4 * * *"); // 10:00 AM
            // }

            // SFTP Timing For Production
            var sftp = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            var timing = sftp.GetTiming();
            if (timing != null)
            {
                RecurringJob.AddOrUpdate(
                   "SFTP",
                   () => sftp.ListFilesFromSftp(), timing.Result); // 10:00 AM,12:00 PM,3:00 PM,6:00 PM 
            }

            var messageNames = new List<string>
             {
                "EmployeeList",
                "RMSList",
                "ItemCodeList",
                "ClassList",
                "DepartmentList",
                "DivisionList",
                "GroupList",
                "SubclassList",
                "BrandList",
                "BusinessList",
                "MerchandiseList"
             };

            var blobToDb = services.BuildServiceProvider().GetRequiredService<IDataProcess>();
            foreach (var messageName in messageNames)
            {
                var jobName = "BlobToDb_" + messageName;
                if (timing != null)
                {
                    RecurringJob.AddOrUpdate(jobName, () => blobToDb.ProcessDataFromTemplate(messageName, false), timing.Result); // 10:00 AM,12:00 PM,3:00 PM,6:00 PM 
                }
            }

            var itemEanCode = services.BuildServiceProvider().GetRequiredService<ITemplateManager>();
            RecurringJob.AddOrUpdate(
               "ItemEANCode",
               () => itemEanCode.UpdateItemAndEanCode(), "45 4 * * *"); // 10 : 15 AM

            var designerEmail = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            RecurringJob.AddOrUpdate(
                "DesignerL1Email",
                () => designerEmail.DesignerL1Digestemail(), "30 12 * * *"); // 6:00 PM

            var buyerEmail = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            RecurringJob.AddOrUpdate(
                "BuyerL1Email",
                () => buyerEmail.BuyerL1Digestemail(), "30 12 * * *"); // 6:00 PM

            var itemCodeEmail = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            RecurringJob.AddOrUpdate(
                "ItemCodeEmail",
                () => itemCodeEmail.ItemcodeDigestemail(), "30 12 * * *"); // 6:00 PM

            var camEmail = services.BuildServiceProvider().GetRequiredService<IUserManager>();
            RecurringJob.AddOrUpdate(
                "CAMEmail",
                () => camEmail.CamDigestemail(), "30 12 * * *"); // 6:00 PM

            app.Run();
        }
    }
}