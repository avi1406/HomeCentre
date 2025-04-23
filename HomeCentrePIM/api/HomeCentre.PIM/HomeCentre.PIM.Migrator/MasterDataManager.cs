using System;
using System.Data;
using System.IO;
using System.Linq;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HomeCentre.PIM.Migrator
{
    public class MasterDataManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly NpgsqlConnection _connection;

        public MasterDataManager(string connectionString, ILogger logger)
        {
            _logger = logger;

            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
        }

        public void Run()
        {
            _logger.LogInformation("Starting Master data loading");

            // _connection.LoadMasterTable("AppUser", new List<object> {
            //     new AppUser
            //     {
            //         Id = 1,
            //         CreatedBy = 1,
            //         UpdatedBy = 1,
            //         Email = "test1@yopmail.com",
            //         FirstName = "Admin",
            //         LastName = "Admin",
            //         DateCreated = DateTimeOffset.Now,
            //         DateModified = DateTimeOffset.Now,
            //         Password = "$2a$11$JCJbi38GnRYA3VitE2xybueb8wDhGoixNTav0eA0D.ThYjHL9sdE6",
            //         IsActive = true
            //     }
            // });
            var lines = File.ReadAllLines("MasterData/roles.csv").Select(x => x.Split(','));
            var roles = lines.Select(line => new Role
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                Description = line[2].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("Role", roles);

            lines = File.ReadAllLines("MasterData/gender.csv").Select(x => x.Split(','));
            var genders = lines.Select(line => new Gender()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("Gender", genders);

            lines = File.ReadAllLines("MasterData/languages.csv").Select(x => x.Split(','));
            var languages = lines.Select(line => new Language
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                Description = line[2].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("Language", languages);

            lines = File.ReadAllLines("MasterData/countries.csv").Select(x => x.Split(','));
            var countries = lines.Select(line => new Country
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                Description = line[1].Trim(),
                CountryCode = line[2].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("country", countries);

            lines = File.ReadAllLines("MasterData/task-step-instance-status.csv").Select(x => x.Split(','));
            var taskStepInstances = lines.Select(line => new TaskStepInstanceStatus
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("taskstepinstancestatus", taskStepInstances);
            
            lines = File.ReadAllLines("MasterData/upload-file-status.csv").Select(x => x.Split(','));
            var uploadFileStatus = lines.Select(line => new UploadedFileStatus
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("uploadedfilestatus", uploadFileStatus);
            
            lines = File.ReadAllLines("MasterData/product-template-status.csv").Select(x => x.Split(','));
            var stageStatus = lines.Select(line => new ProductTemplateStatus()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("producttemplatestatus", stageStatus);
            
            lines = File.ReadAllLines("MasterData/divisions.csv").Select(x => x.Split(','));
            var divisions = lines.Select(line => new Division()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[2].Trim(),
                Code = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("division", divisions);
            
            // lines = File.ReadAllLines("MasterData/product-pricing.csv").Select(x => x.Split(','));
            // var priceGroupings = lines.Select(line => new PriceGrouping()
            // {
            //    Id = long.Parse(line[0].Trim()),
            //    Name = line[1].Trim(),
            //    Code = line[0].Trim(),
            //    DateCreated = DateTimeOffset.Now,
            //    DateModified = DateTimeOffset.Now,
            //    CreatedBy = 1,
            //    UpdatedBy = 1,
            //    IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("pricegrouping", priceGroupings);
            // lines = File.ReadAllLines("MasterData/product-purpose.csv").Select(x => x.Split(','));
            // var productPurpose = lines.Select(line => new ProductPurpose
            // {
            //    Id = long.Parse(line[0].Trim()),
            //    Name = line[1].Trim(),
            //    Code = line[0].Trim(),
            //    DateCreated = DateTimeOffset.Now,
            //    DateModified = DateTimeOffset.Now,
            //    CreatedBy = 1,
            //    UpdatedBy = 1,
            //    IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("productpurpose", productPurpose);
            lines = File.ReadAllLines("MasterData/product-season.csv").Select(x => x.Split(','));
            var productSeasons = lines.Select(line => new ProductSeason
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[2].Trim(),
                Code = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("productseason", productSeasons);
            
            lines = File.ReadAllLines("MasterData/divisions.csv").Select(x => x.Split(','));
            var departments = lines.Select(line => new Department()
            {
                Id = long.Parse(line[0].Trim()),
                DivisionId = long.Parse(line[1].Trim()),
                Name = line[2].Trim(),
                Description = line[3].Trim(),
                IsPlanogram = bool.Parse(line[4]),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("department", departments);
            
            lines = File.ReadAllLines("MasterData/departments.csv").Select(x => x.Split(','));
            var productDepartments = lines.Select(line => new ProductDepartment
            {
                Id = long.Parse(line[0].Trim()),
                Code = line[0].Trim(),
                DivisionId = long.Parse(line[1].Trim()),
                Name = line[2].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("productdepartment", productDepartments);
            
            // lines = File.ReadAllLines("MasterData/hierarchical-master.csv").Select(x => x.Split(','));
            // var hierarchicalMasters = lines.Select(line => new HierarchicalMaster
            // {
            //    Id = long.Parse(line[0].Trim()),
            //    BusinessCode = line[1],
            //    BusinessName = line[2],
            //    DivisionCode = line[3],
            //    DivisionName = line[4],
            //    GroupCode = line[5],
            //    GroupName = line[6],
            //    DepartmentCode = line[7],
            //    DepartmentName = line[8],
            //    ClassCode = line[9],
            //    ClassName = line[10],
            //    SubclassCode = line[11],
            //    SubclassName = line[12],
            //    DepartmentShortCode = line[13],
            //    Name = $"{line[1]}|{line[2]}|{line[4]}|{line[6]}",
            //    Description = $"{line[1]}|{line[3]}|{line[5]}|{line[7]}",
            //    DateCreated = DateTimeOffset.Now,
            //    DateModified = DateTimeOffset.Now,
            //    CreatedBy = 1,
            //    UpdatedBy = 1,
            //    IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("hierarchicalmaster", hierarchicalMasters);
            lines = File.ReadAllLines("MasterData/template-status.csv").Select(x => x.Split(','));
            var templateStatus = lines.Select(line => new TemplateStatus
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("templatestatus", templateStatus);
            
            // lines = File.ReadAllLines("MasterData/templates.csv").Select(x => x.Split(','));
            // var templates = lines.Select(line => new Template
            // {
            //     Id = long.Parse(line[0].Trim()),
            //     Name = line[1].Trim(),
            //     DivisionId = long.Parse(line[2]),
            //     TemplateStatusId = 2,
            //     IsUidTemplate = bool.Parse(line[3]),
            //     DateCreated = DateTimeOffset.Now,
            //     DateModified = DateTimeOffset.Now,
            //     CreatedBy = 1,
            //     UpdatedBy = 1,
            //     IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("template", templates);
            // lines = File.ReadAllLines("MasterData/product-attributes.csv").Select(x => x.Split(','));
            // var productAttributes = lines.Select(line => new ProductAttribute()
            // {
            //    Id = long.Parse(line[0].Trim()),
            //    Name = line[1].Trim(),
            //    Label = line[2].Trim(),
            //    DataType = (AttrDataType?)int.Parse(line[3]),
            //    LookupTable = line[4].Trim(),
            //    LookupTableColumn = line[5].Trim(),
            //    IsRequired = bool.Parse(line[6].Trim().ToUpper()),
            //    DivisionId = long.Parse(line[7]),
            //    DateCreated = DateTimeOffset.Now,
            //    DateModified = DateTimeOffset.Now,
            //    CreatedBy = 1,
            //    UpdatedBy = 1,
            //    IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("productattribute", productAttributes);
            
            // lines = File.ReadAllLines("MasterData/template-attributes.csv").Select(x => x.Split(','));
            // var templateAttributes = lines.Select(line => new TemplateAttribute()
            // {
            //     Id = long.Parse(line[0].Trim()),
            //     TemplateId = long.Parse(line[1].Trim()),
            //     AttributeId = long.Parse(line[2].Trim()),
            //     DateCreated = DateTimeOffset.Now,
            //     DateModified = DateTimeOffset.Now,
            //     CreatedBy = 1,
            //     UpdatedBy = 1,
            //     IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("templateattribute", templateAttributes);
            lines = File.ReadAllLines("MasterData/query-types.csv").Select(x => x.Split(','));
            var queryTypes = lines.Select(line => new QueryType()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("querytype", queryTypes);
            
            lines = File.ReadAllLines("MasterData/query-priorities.csv").Select(x => x.Split(','));
            var queryPriorities = lines.Select(line => new QueryPriority()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("querypriority", queryPriorities);
            
            lines = File.ReadAllLines("MasterData/product-qc-status.csv").Select(x => x.Split(','));
            var productQcStatus = lines.Select(line => new ProductQcStatus()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("productqcstatus", productQcStatus);
            
            lines = File.ReadAllLines("MasterData/query-status.csv").Select(x => x.Split(','));
            var queryStatus = lines.Select(line => new ProductQueryStatus()
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("productquerystatus", queryStatus);
            
            lines = File.ReadAllLines("MasterData/marketplaces.csv").Select(x => x.Split(','));
            var marketplaces = lines.Select(line => new Marketplace
            {
                Id = long.Parse(line[0].Trim()),
                Name = line[1].Trim(),
                ShortCode = line[2].Trim(),
                DateCreated = DateTimeOffset.Now,
                DateModified = DateTimeOffset.Now,
                CreatedBy = 1,
                UpdatedBy = 1,
                IsActive = true
            }).Cast<object>().ToList();
            _connection.LoadMasterTable("marketplace", marketplaces);
            
            // lines = File.ReadAllLines("MasterData/seasons.csv").Select(x => x.Split(','));
            // var seasons = lines.Select(line => new Season()
            // {
            //    Id = long.Parse(line[0].Trim()),
            //    Name = line[1].Trim(),
            //    Code = line[2].Trim(),
            //    Description = line[1].Trim(),
            //    DateCreated = DateTimeOffset.Now,
            //    DateModified = DateTimeOffset.Now,
            //    CreatedBy = 1,
            //    UpdatedBy = 1,
            //    IsActive = true
            // }).Cast<object>().ToList();
            // _connection.LoadMasterTable("season", seasons);
            _logger.LogInformation("Master data loading, completed");
        }

        public void Dispose()
        {
            if (_connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }
    }
}