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
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Graph.ExternalConnectors;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Npgsql.Logging;
using Serilog;
using Serilog.Events;

namespace HomeCentre.PIM.Processor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // load configuration file and prereq settings
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
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

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            services.AddMemoryCache();
            services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMemoryStorage());
            services.AddHangfireServer();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDataProtection().SetApplicationName("HC.PIM");
            services.AddHttpClient();
            services.AddSingleton<IJsonSerializer>(s => new NewtonSoftJsonSerializer(Formatting.None,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
            services.AddTransient<IAuthenticationProvider, B2CAuthenticationProvider>();
            services.AddTransient<IBlobRepo, BlobRepo>();
            services.AddTransient<IUserManager, PgUserManager>();
            services.AddTransient<IDataProcess, DataProcess>();
            services.AddTransient<IDistributedCache, InMemoryCacheHandler>();
            services.AddTransient<IErrorLogger, ErrorLogger>();
            services.AddTransient<ICacheHandler, InMemoryCacheHandler>();
            services.AddTransient<IDbConnectionFactory>(s => new NpgSqlConnectionFactory(configuration));
            services.AddTransient<IDbCache, PgDbCache>();
            services.AddTransient<IMasterManager, PgMasterManager>();
            services.AddTransient<IUserContext, HttpUserContext>();
            services.AddTransient<ICipherService, CipherService>();
            services.AddMailService(configuration);
            services.AddTransient<IMailEngine, SmtpMailEngine>();
            services.AddTransient<IFileConfigurationManager, FileShareConfigurationManager>();
            services.AddTransient<IUserManager, PgUserManager>();
            
            services.AddTransient<IWorkFlowManager, TaskstepInstanceRepo>();
            services.AddTransient<IWorkFlowManager1, WorkFlowInstanceRepo>();
            services.AddTransient<IUIDManager, UIDRepo>();
            services.AddTransient<IUploadManager, PgUploadManager>();
            services.AddTransient<IDataProcessorFactory, PgDataProcessorFactory>();
            services.AddTransient<ITemplateManager, PgTemplateManager>();
            services.AddTransient<IProductManager, PgProductManager>();
            services.AddTransient<IFileExplorer, SftpExplorer>();
            services.AddTransient<IFileConfigurationManager, FileShareConfigurationManager>();
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
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
            app.UseHangfireDashboard("/process");
            var manager = services.BuildServiceProvider().GetRequiredService<IFileConfigurationManager>();
            foreach (var fileConfig in manager.GetConfigurations().Result)
            {
                RecurringJob.AddOrUpdate(
                    $"fileManagerJob-{fileConfig.Name}",
                    () => manager.ProcessConfiguration(fileConfig),
                    Cron.Minutely);
            }

            app.Run();
        }
    }
}