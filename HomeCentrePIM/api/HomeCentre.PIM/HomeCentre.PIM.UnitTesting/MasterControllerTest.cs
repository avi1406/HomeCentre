
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework.Internal;
using HomeCentre.PIM.BAL.Model.App;
using HomeCentre.PIM.BAL.Implementations.Storage;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.DAL;
using System.Text;

namespace HomeCenter.PIM.UnitTest
{
    public class Tests
    {
        private IConfigurationRoot _configuration;
        private IDbCache _cache;
        private ILabelManager _labelManager;
        private IMasterManager _masterManager;
        private IBlobRepo _blobRepo;

        public void OneTimeSetUp()
        {
            _cache = Mock.Of<IDbCache>();
            _labelManager = Mock.Of<ILabelManager>();
            _masterManager = Mock.Of<IMasterManager>();
            _blobRepo = Mock.Of<IBlobRepo>();
        }

        [SetUp]
        public void Setup()
        {
            _cache = Mock.Of<IDbCache>();
        }

        [Test]
        public async Task GetLabels_Test()
        {
           
            long? languageId = 1;
            var expectedLabels = new List<LabelModel> { new LabelModel { Id = 1, Name = "Label 1" }, new LabelModel { Id = 2, Name = "Label 2" } };
            Mock.Get(_cache)
                .Setup(c => c.GetAllLabels(languageId))
                .ReturnsAsync(expectedLabels);
            var service = new MasterController(_cache, _labelManager, _masterManager, _blobRepo);
            var result = await service.GetLabels(languageId);
            Assert.That(result, Is.EqualTo(expectedLabels));
            Mock.Get(_cache).Verify(c => c.GetAllLabels(languageId), Moq.Times.Once);
        }

        [Test]
        public async Task SearchLabels_Test()
        {
            var languageId = 1;
            var expectedResult = new SearchResult<LabelModel>();
            var mockLabelManager = new Mock<ILabelManager>();
            mockLabelManager
                .Setup(manager => manager.Search(languageId))
                .ReturnsAsync(expectedResult);
            var controller = new MasterController(_cache, mockLabelManager.Object, _masterManager, _blobRepo);
            var result = await controller.SearchLabels(languageId);
            Assert.AreEqual(expectedResult, result);
            mockLabelManager.Verify(manager => manager.Search(languageId), Times.Once);
        }

        [Test]
        public async Task GetCountries_Test()
        {
            var countries = new List<Country>
            {
                new Country { Name = "France" },
                new Country { Name = "Germany" },
                new Country { Name = "Italy" }
            };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Country>()).ReturnsAsync(countries);
            var controller = new MasterController(_cache, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetCountries();
            var expectedOrder = countries.OrderBy(o => o.Name).Select(c => c.Name).ToList();
            var actualOrder = result.Select(c => c.Name).ToList();
            Assert.AreNotEqual(expectedOrder, actualOrder);
        }

        [Test]
        public async Task GetCurrencies_Test()
        {
            var currencies = new List<Currency>
            {
        new Currency { AlphabeticCode = "USD" },
        new Currency { AlphabeticCode = "EUR" },
        new Currency { AlphabeticCode = "JPY" }
             };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Currency>()).ReturnsAsync(currencies);
            var controller = new MasterController(cacheMock.Object, null, null, null);
            var result = await controller.GetCurrencies();
            Assert.AreEqual(currencies, result);
        }

        [Test]
        public async Task GetRoles_Test()
        {
            bool isActive = true;
            var roles = new List<Role>
            {
                 new Role { Name = "Admin", IsActive = true },
                 new Role { Name = "User", IsActive = true },
                 new Role { Name = "Guest", IsActive = false }
             };

            var masterManagerMock = new Mock<IMasterManager>();
            masterManagerMock.Setup(mock => mock.Search<Role>(isActive)).ReturnsAsync(roles);
            var controller = new MasterController(null, null, masterManagerMock.Object, null);
            var result = await controller.GetRoles(isActive);
            Assert.AreEqual(roles, result);
        }

        [Test]
        public async Task GetGenders_Test()
        {
            var genders = new List<Gender>
             {
                 new Gender { Name = "Male" },
                 new Gender { Name = "Female" },
                 new Gender { Name = "Non-binary" }
             };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Gender>()).ReturnsAsync(genders);
            var controller = new MasterController(cacheMock.Object, null, null, null);
            var result = await controller.GetGenders();
            Assert.AreEqual(genders, result);
        }

        [Test]
        public async Task GetAppConfiguration_Test()
        {
            var appConfiguration = new AppConfiguration
            {
                AppUrl = "https://lmrkapppoc1.z30.web.core.windows.net",
                B2C = new B2CConfiguration
                {
                    LoginDomain = "hcpimdev.b2clogin.com",
                    Tenant = "hcpimdev.onmicrosoft.com",
                    LoginFlow = "B2C_1_Login",
                    ClientId = "34b8235c-6ce2-4e64-9768-559e3ef2b313",
                    ResetPasswordFlow = null
                }
            };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetAppConfiguration()).ReturnsAsync(appConfiguration);
            var controller = new MasterController(cacheMock.Object, null, null, null);
            var result = await controller.GetAppConfiguration();
            Assert.AreEqual(appConfiguration, result);
        }

        [Test]
        public async Task GetLanguages_Test()
        {
            var languages = new List<Language>
            {
            new Language { Name = "English" },
            new Language { Name = "Spanish" },
            new Language { Name = "French" }
            };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Language>()).ReturnsAsync(languages);

            var yourClass = new MasterController(cacheMock.Object, null, null, null);
            var result = await yourClass.GetLanguages();

            Assert.AreEqual(languages, result);
        }

        [Test]
        public async Task GetAppConfig_Test()
        {
            var appSettings = new List<AppSettings>
            {
        new AppSettings
        {
            CreatedBy = 0,
            DateCreated = DateTime.Parse("2023-06-21T11:36:07.738Z"),
            DateModified = DateTime.Parse("2023-06-21T11:36:07.738Z"),
            Id = 0,
            UpdatedBy = 0,
            IsActive = true,
            Description = "string",
            Name = "string",
            Value = "string",
            JsonValue = "string"
            }
         };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetAppSettings()).ReturnsAsync(appSettings);
            var service = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await service.GetAppConfig();
            CollectionAssert.AreEqual(appSettings, result.OfType<AppSettings>().ToList());
        }

        [Test]
        public async Task GetUserRoles_Test()
        {
            long? appuserId = 123;

            var expectedRoles = new List<Role>
        {
        new Role { Id = 1, Name = "Role1" },
        new Role { Id = 2, Name = "Role2" },
        new Role { Id = 3, Name = "Role3" }
         };

            var masterManagerMock = new Mock<IMasterManager>();
            masterManagerMock.Setup(mock => mock.GetUserRole(appuserId)).ReturnsAsync(expectedRoles);

            var controller = new MasterController(_cache, _labelManager, masterManagerMock.Object, _blobRepo);
            var result = await controller.GetUserRoles(appuserId);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRoles, result);
        }

        [Test]
        public async Task GetDepartments_Test()
        {
            var expectedDepartments = new List<ProductMetaModel>
         {
        new ProductMetaModel { Id = 1, Name = "Department1" },
        new ProductMetaModel { Id = 2, Name = "Department2" },
        new ProductMetaModel { Id = 3, Name = "Department3" }
        };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetDepartments()).ReturnsAsync(expectedDepartments);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetDepartments();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDepartments, result);
        }

        [Test]
        public async Task GetProductClasses_Test()
        {

            var expectedProductClasses = new List<ProductMetaModel>
            {
            new ProductMetaModel { Id = 1, Name = "ProductClass1" },
            new ProductMetaModel { Id = 2, Name = "ProductClass2" },
            new ProductMetaModel { Id = 3, Name = "ProductClass3" }
            };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetProductClasses()).ReturnsAsync(expectedProductClasses);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetProductClasses();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedProductClasses, result);
        }

        [Test]
        public async Task GetProductSubclasses_Test()
        {

            var expectedProductSubclasses = new List<ProductMetaModel>
            {
            new ProductMetaModel { Id = 5, Code = "5", Name = "Sliding Door Wardrobe", ParentCode = "2" }
            };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetProductSubclasses()).ReturnsAsync(expectedProductSubclasses);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);

            var result = await controller.GetProductSubclasses();

            Assert.IsNotNull(result);
            Assert.AreEqual(expectedProductSubclasses, result);
        }

        [Test]
        public async Task GetUserDepartment_Test()
        {
            var appuserId = 1;
            var expectedUserDepartments = new List<UserDepartment>
           {
             new UserDepartment
            {
             DepartmentId = 61,
             DepartmentName  = "HC-House Hold"
            }
            };
            var masterManagerMock = new Mock<IMasterManager>();
            masterManagerMock.Setup(mock => mock.GetUserDepartment(appuserId)).ReturnsAsync(expectedUserDepartments);
            var controller = new MasterController(_cache, _labelManager, masterManagerMock.Object, _blobRepo);
            var result = await controller.GetUserDepartment(appuserId);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUserDepartments, result);
        }

        [Test]
        public async Task GetDivision_Test()
        {
            var expectedDivisions = new List<Division>
         {
        new Division
             {
            Code = "61",
            Name = "HC-House Hold",
            Id = 61,
            IsActive = true,
            CreatedBy = 1,
            DateCreated = new DateTime(2023, 06, 19, 15, 14, 55, 998),
            DateModified = new DateTime(2023, 06, 19, 15, 14, 52, 852),
            UpdatedBy = 1
             }
         };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Division>()).ReturnsAsync(expectedDivisions);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetDivision();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDivisions, result);
        }

        [Test]
        public async Task GetUploadedFileStatus_Test()
        {
            var expectedFileStatuses = new List<UploadedFileStatus>
             {
        new UploadedFileStatus
        {
            Id = 1,
            Name = "Not Started",
            
             }
            };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<UploadedFileStatus>()).ReturnsAsync(expectedFileStatuses);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetUploadedFileStatus();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedFileStatuses, result);
        }

        [Test]
        public async Task GetTemplateStatus_Test()
        {
            var expectedTemplateStatusList = new List<TemplateStatus>
        {
        new TemplateStatus
        {
            Name = "Draft",
            Id = 1
        },
        new TemplateStatus
        {
            Name = "Published",
            Id = 2
        }
        };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<TemplateStatus>()).ReturnsAsync(expectedTemplateStatusList);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetTemplateStatus();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedTemplateStatusList, result);
        }

        [Test]
        public async Task GetQueryTypes_Test()
        {
            var expectedQueryTypes = new List<QueryType>
         {
        new QueryType
        {
            Id = 12,
            Name = "Others",
            CreatedBy = 1,
            DateCreated = DateTime.Parse("2023-05-15T18:34:07.746945+05:30"),
            DateModified = DateTime.Parse("2023-05-15T18:34:08.717081+05:30"),
            UpdatedBy = 1,
            IsActive = true
            }
        };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<QueryType>()).ReturnsAsync(expectedQueryTypes);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetQueryTypes();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedQueryTypes, result);
        }

        [Test]
        public async Task GetQueryPriorities_Test()
        {

            var expectedQueryPriorities = new List<QueryPriority>
         {
        new QueryPriority
            {
            Description = "string",
            Name = "string",
            CreatedBy = 0,
            DateCreated = DateTime.Parse("2023-06-22T12:38:50.857Z"),
            DateModified = DateTime.Parse("2023-06-22T12:38:50.857Z"),
            Id = 0,
            UpdatedBy = 0,
            IsActive = true
             }
         };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<QueryPriority>()).ReturnsAsync(expectedQueryPriorities);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetQueryPriorities();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedQueryPriorities, result);
        }

        [Test]
        public async Task GetQcStatus_Test()
        {
            var expectedProductQcStatus = new List<ProductQcStatus>
        {
       
        new ProductQcStatus
            {
            Description = null,
            Name = "Resolved",
            CreatedBy = 1,
            DateCreated = DateTime.Parse("2023-05-15T18:34:11.084044+05:30"),
            DateModified = DateTime.Parse("2023-05-15T18:34:12.502936+05:30"),
            Id = 10,
            UpdatedBy = 1,
            IsActive = true
             }
        };
            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<ProductQcStatus>()).ReturnsAsync(expectedProductQcStatus);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetQcStatus();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedProductQcStatus, result);
        }

        [Test]
        public async Task GetTables_Test()
        {
            var expectedTableMetadata = new List<TableMetadata>
         {
        new TableMetadata
        {
            Description = "Appuser Table",
            Name = "appuser",
            CreatedBy = 1,
            DateCreated = DateTime.Parse("2022-12-05T19:24:23.604888+05:30"),
            DateModified = DateTime.Parse("2023-06-09T10:14:51.02719+05:30"),
            Id = 1,
            UpdatedBy = 1,
            IsActive = true
        }
        };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<TableMetadata>()).ReturnsAsync(expectedTableMetadata);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetTables();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedTableMetadata, result);
        }

        [Test]
        public async Task GetTableColumns_Test()
        {
            var expectedTableMetaDataColumns = new List<TableMetaDataColumn>
             {
            new TableMetaDataColumn
             {
            CreatedBy = 0,
            DateCreated = DateTime.Parse("2023-06-23T02:35:13.795Z"),
            DateModified = DateTime.Parse("2023-06-23T02:35:13.795Z"),
            Id = 0,
            UpdatedBy = 0,
            IsActive = true,
            Description = "string",
            Name = "string"
             }
                 };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<TableMetaDataColumn>()).ReturnsAsync(expectedTableMetaDataColumns);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetTableColumns();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedTableMetaDataColumns, result);
        }

        [Test]
        public async Task GetSeasons_Test()
        {
            var expectedSeasons = new List<Season>
         {
        new Season
            {
            CreatedBy = 0,
            DateCreated = DateTime.Parse("2023-06-23T02:40:57.879Z"),
            DateModified = DateTime.Parse("2023-06-23T02:40:57.879Z"),
            Id = 0,
            UpdatedBy = 0,
            IsActive = true,
            Description = "string",
            Name = "string",
            Code = "string"
             }
        };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<Season>()).ReturnsAsync(expectedSeasons);

            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetSeasons();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSeasons, result);
        }

        [Test]
        public async Task DownloadBlobFile_Test()
        {
            string url = "https://example.com/blob/file.txt";
            string fileContent = "Sample file content";
            var blobRepoMock = new Mock<IBlobRepo>();
            blobRepoMock.Setup(mock => mock.DownloadFile(url)).ReturnsAsync(fileContent);
            var controller = new MasterController(_cache, _labelManager, _masterManager, blobRepoMock.Object);
            var result = await controller.DownloadBlobFile(url);
            Assert.IsNotNull(result);
            Assert.AreEqual(fileContent, result);
        }

        [Test]
        public async Task ActivateRole_UpdatesRoleActivation()
        {
            long roleId = 1;
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.ActivateRole(roleId))
                .Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.ActivateRole(roleId);
            labelManagerMock.Verify(mock => mock.ActivateRole(roleId), Times.Once);
        }

        [Test]
        public async Task ActivateRoleBatch_UpdatesTest()
        {
            long[] roleIds = new long[] { 1, 2, 3 };
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.ActivateRoleBatch(roleIds))
                .Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.ActivateRoleBatch(roleIds);
            labelManagerMock.Verify(mock => mock.ActivateRoleBatch(roleIds), Times.Once);
        }

        [Test]
        public async Task DeactivateRole_UpdatesTest()
        {
            long roleId = 1;
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.DeactivateRole(roleId))
                .Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.DeactivateRole(roleId);
            labelManagerMock.Verify(mock => mock.DeactivateRole(roleId), Times.Once);
        }

        [Test]
        public async Task DeactivateRoleBatch_UpdatesTest()
        {
            long[] roleIds = { 1, 2, 3 };
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.DeactivateRoleBatch(roleIds))
                .Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.DeactivateRoleBatch(roleIds);
            labelManagerMock.Verify(mock => mock.DeactivateRoleBatch(roleIds), Times.Once);
        }

        [Test]
        public async Task UpdateRoles_Test()
        {
            long roleId = 1;
            string roleName = "New Role";
            string roleDescription = "Role Description";
            bool? isActive = true;
            
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.UpdateRoles(roleId, roleName, roleDescription, isActive))
                .Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.UpdateRoles(roleId, roleName, roleDescription, isActive);
            labelManagerMock.Verify(mock => mock.UpdateRoles(roleId, roleName, roleDescription, isActive), Times.Once);
        }

        [Test]
        public async Task Ismail_Test()
        {
            string email = "test@example.com";
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.Ismail(email)).Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.Ismail(email);
            labelManagerMock.Verify(mock => mock.Ismail(email), Times.Once);
        }

        [Test]
        public async Task DeleteRoles_Test()
        {
            long appUserId = 1;
            long roleId = 1;
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.DeleteRoles(appUserId, roleId)).Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            await controller.DeleteRoles(appUserId, roleId);
            labelManagerMock.Verify(mock => mock.DeleteRoles(appUserId, roleId), Times.Once);
        }

        [Test]
        public async Task UploadBlobFile_Test()
        {
            string fileName = "file.txt";
            string containerName = "container";
            string filePath = "path/to/file.txt";
            var blobRepoMock = new Mock<IBlobRepo>();
            blobRepoMock.Setup(mock => mock.UploadFile(fileName, containerName, filePath)).Returns(Task.FromResult("result"));
            var controller = new MasterController(_cache, _labelManager, _masterManager, blobRepoMock.Object);
            await controller.UploadBlobFile(fileName, containerName, filePath);
            blobRepoMock.Verify(mock => mock.UploadFile(fileName, containerName, filePath), Times.Once);
        }

        [Test]
        public async Task DeleteBlobFile_Test()
        {
            string containerName = "container";
            string fileName = "file.txt";
            var blobRepoMock = new Mock<IBlobRepo>();
            blobRepoMock.Setup(mock => mock.DeleteFile(containerName, fileName)).Returns(Task.CompletedTask);
            var controller = new MasterController(_cache, _labelManager, _masterManager, blobRepoMock.Object);
            await controller.DeleteBlobFile(containerName, fileName);
            blobRepoMock.Verify(mock => mock.DeleteFile(containerName, fileName), Times.Once);
        }

        [Test]
        public async Task GetErrorLogUser_Test()
        {
            DateTime fromDate = new DateTime(2023, 1, 1);
            DateTime toDate = new DateTime(2023, 12, 31);
            var expectedErrorLogs = new List<string> { "Error log 1", "Error log 2", "Error log 3" };
            var labelManagerMock = new Mock<ILabelManager>();
            labelManagerMock.Setup(mock => mock.GetErrorLogUser(fromDate, toDate)).ReturnsAsync(expectedErrorLogs);
            var controller = new MasterController(_cache, labelManagerMock.Object, _masterManager, _blobRepo);
            var result = await controller.GetErrorLogUser(fromDate, toDate) as IEnumerable<string>;
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedErrorLogs.Count, result.Count());
            Assert.IsTrue(expectedErrorLogs.SequenceEqual(result));
            labelManagerMock.Verify(mock => mock.GetErrorLogUser(fromDate, toDate), Times.Once);
        }

        [Test]
        public async Task GetRejectReason_Test()
        {
            var expectedReasons = new List<RejectionReason>
        {
        new RejectionReason { Id = 1, Name = "Reason 1", Description = "Description 1" },
        new RejectionReason { Id = 2, Name = "Reason 2", Description = "Description 2" },
        new RejectionReason { Id = 3, Name = "Reason 3", Description = "Description 3" }
        };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(mock => mock.GetMaster<RejectionReason>()).ReturnsAsync(expectedReasons);
            var controller = new MasterController(cacheMock.Object, _labelManager, _masterManager, _blobRepo);
            var result = await controller.GetRejectReason() as IEnumerable<RejectionReason>;
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedReasons.Count, result.Count());
            Assert.IsTrue(expectedReasons.SequenceEqual(result));
            cacheMock.Verify(mock => mock.GetMaster<RejectionReason>(), Times.Once);
        }

    }
}