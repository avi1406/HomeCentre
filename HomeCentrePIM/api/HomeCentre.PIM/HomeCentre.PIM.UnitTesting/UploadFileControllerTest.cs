using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Implementations.FileProcessor;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;
using HomeCentre.PIM.BAL.Model;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Data;

namespace HomeCentre.PIM.UnitTesting
{
    public class UploadFileControllerTest
    {
        private IUploadManager _manager;
        private IBlobRepo _blobRepo;
        private IFileConfigurationManager _configurationManager;
        private IDbCache _cache;
        private IUserContext _context;
        private IUploadManager _managerMock;

        [SetUp]
        public void Setup()
        {
            _cache = Mock.Of<IDbCache>();
            _manager = Mock.Of<IUploadManager>();
            _blobRepo = Mock.Of<IBlobRepo>();
            _configurationManager = Mock.Of<IFileConfigurationManager>();
            _context = Mock.Of<IUserContext>();
        }

        [Test]
        public async Task UpdateRecords_Test()
        {
            var expectedFileData = new List<UploadedFileData>
            {
                new UploadedFileData
                {
                    UploadedFileId = 123,
                    ColumnData = new Dictionary<string, object>(),
                    ErrorData = new Dictionary<string, List<string>>()
                }
            };

            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(m => m.UpdateRecords(It.IsAny<IEnumerable<UploadedFileData>>()))
                       .Verifiable();
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            await controller.UpdateRecords(expectedFileData);
            managerMock.Verify(m => m.UpdateRecords(expectedFileData), Times.Once);
        }

        [Test]
        public async Task ValidateFile_Test()
        {
            var expectedUploadedFile = new UploadedFile();
            var expectedUploadedFileDataList = new List<UploadedFileData>();
            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(m => m.ValidateFile(expectedUploadedFile))
                       .ReturnsAsync(expectedUploadedFileDataList);
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await controller.ValidateFile(expectedUploadedFile);
            Assert.AreEqual(expectedUploadedFileDataList, result);
        }

        [Test]
        public async Task PublishFile_Test()
        {
            var expectedUploadedFile = new UploadedFile();

            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(m => m.PublishFile(expectedUploadedFile))
                       .Verifiable();
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            await controller.PublishFile(expectedUploadedFile);
            managerMock.Verify(m => m.PublishFile(expectedUploadedFile), Times.Once);
        }

        [Test]
        public async Task GetUploadedFile_Test()
        {
            long expectedFileId = 2025;
            var expectedFile = new UploadedFile
            {
                Id = expectedFileId,
                Name = "HH-UID Template.xlsx",
                BlobUrl = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/HH_UID_Template__IGN_2023_05_18_10_57_02.xlsx?sv=2021-08-06&st=2023-05-18T10%3A52%3A02Z&se=2023-05-18T11%3A07%3A02Z&sr=b&sp=rcw&sig=fPxSVbh7%2BL86IJJmAHN9d5SWoR3CjCLor%2Fw8Zp54UOU%3D",
                UploadedFileStatusId = 1,
                TableName = "Product",
                ConfigName = null,
                ErrorCount = 0,
                TemplateId = 2,
                CreatedBy = 13,
                DateCreated = DateTime.Parse("2023-05-18T16:27:03.428358+05:30"),
                DateModified = DateTime.Parse("2023-05-18T16:27:03.428359+05:30"),
                UpdatedBy = 13,
                IsActive = true
            };

            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(m => m.GetFile(expectedFileId))
                       .ReturnsAsync(expectedFile);
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await controller.GetUploadedFile(expectedFileId);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedFile,result);
            
        }

        [Test]
        public async Task AddPicture_test()
        {
            var expectedModel = new UploadPicture
            {
               Name = "Table.jpg",
               ImageUrl = "C:\\Users\\naga\\Downloads\\Table.jpg"
            };

            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(m => m.AddPicture(expectedModel))
                       .Returns(Task.CompletedTask);
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            await controller.AddPicture(expectedModel);
            managerMock.Verify(m => m.AddPicture(expectedModel), Times.Once);
        }

        [Test]
        public async Task GetFileNameAndUrl_Test()
        {
           
            List<UploadPicture> expectedPictures = new List<UploadPicture>
         {
        new UploadPicture
             {
            Name = "Boeing_safety.jpg",
            ImageUrl = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/Boeing_safety_ZGD_2023_06_05_16_12_59.jpg?sv=2021-08-06&st=2023-06-05T10%3A37%3A59Z&se=2023-06-05T10%3A52%3A59Z&sr=b&sp=rcw&sig=BjdEbYsGqEKwNSPoO1LpYFPTqyi7ePvbldCGd43S%2F%2F4%3D",
            CreatedBy = 0,
            Id = 26,
            UpdatedBy = 0,
            IsActive = true
            }
             };

            var _managerMock = new Mock<IUploadManager>();
            _managerMock.Setup(m => m.GetFileNameAndUrl()).ReturnsAsync(expectedPictures);
            var controller = new UploadFileController(_managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await controller.GetFileNameAndUrl();
            Assert.AreEqual(expectedPictures, result);
        }

        [Test]
        public async Task GetAuthorizedUrlForWrite_Test()
        {
            string fileName = "HH-UID Template  (1)";
            string expectedUrl = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/HH_UID_Template___1__HIR_2023_06_30_11_23_31.xlsx?sv=2021-08-06&st=2023-06-30T11%3A18%3A31Z&se=2023-06-30T11%3A33%3A31Z&sr=b&sp=rcw&sig=YEiX4GmqbCWCsSBpXLgbtKIJGSCqclCDGA9eOuwpEn8%3D";

            var blobRepoMock = new Mock<IBlobRepo>();
            blobRepoMock.Setup(m => m.GetPublicAccessWriteUrl(ContainerNames.FileContainer, fileName.ToUniqueFileNameUrl(), It.IsAny<int>()))
                        .ReturnsAsync(expectedUrl);
            var controller = new UploadFileController(_manager, blobRepoMock.Object, _configurationManager, _cache, _context);
            var result = await controller.GetAuthorizedUrlForWrite(fileName);
            Assert.IsNull(result);
        }

        [Test]
        public async Task SearchUploadedFileRecords_Test()
        {
            string searchKey = "home";
            int pageNumber = 1;
            int pageCount = 10;
            long uploadedFileId = 1866;
            var expectedSearchResult = new SearchResult<UploadedFileData>();

            var _managerMock = new Mock<IUploadManager>();
            _managerMock.Setup(m => m.SearchRecords(searchKey, pageNumber, pageCount, uploadedFileId))
                .ReturnsAsync(expectedSearchResult);
            var _controller = new UploadFileController(_managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await _controller.SearchUploadedFileRecords(searchKey, pageNumber, pageCount, uploadedFileId);
            Assert.AreEqual(expectedSearchResult, result);
        }

        [Test]
        public async Task SearchUploadedFiles_Test()
        {
            string searchKey = "home";
            int pageNumber = 1;
            int pageCount = 10;
            long? templateId = 1234;
            var expectedSearchResult = new SearchResult<UploadedFile>();

            var _managerMock = new Mock<IUploadManager>();
            _managerMock.Setup(m => m.SearchFiles(searchKey, pageNumber, pageCount, templateId))
                .ReturnsAsync(expectedSearchResult);
            var _controller = new UploadFileController(_managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await _controller.SearchUploadedFiles(searchKey, pageNumber, pageCount, templateId);
            Assert.AreEqual(expectedSearchResult, result);
        }

        [Test]
        public async Task DownloadUploadedFile_Test()
        {
            long uploadedFileId = 2025;
            var uploadedFile = new UploadedFile
            {
                Id = uploadedFileId,
                Name = "SampleFile.xlsx"
            };
            var filePath = "C:\\Users\\naga\\Downloads\\HH-UID Template (1).xlsx";
            var managerMock = new Mock<IUploadManager>();
            managerMock.Setup(manager => manager.GetFile(uploadedFileId))
                .ReturnsAsync(uploadedFile);

            managerMock.Setup(manager => manager.DownloadFile(uploadedFileId))
                .ReturnsAsync(filePath);
            var controller = new UploadFileController(managerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await controller.DownloadUploadedFile(uploadedFileId);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileContentResult.ContentType);
            Assert.AreEqual(uploadedFile.Name, fileContentResult.FileDownloadName);
        }

        [Test]
        public async Task GetTemplates_Test()
        {
            var expectedUserId = 42;
            var userTemplates = new List<Template>
            {
                new Template { AppUserId = expectedUserId },
                new Template { AppUserId = 24 },
                new Template { AppUserId = expectedUserId },
                new Template { AppUserId = expectedUserId }
            };

            var cacheMock = new Mock<IDbCache>();
            var contextMock = new Mock<IUserContext>();
            cacheMock.Setup(mock => mock.GetUserTemplates())
                .ReturnsAsync(userTemplates);

            contextMock.Setup(mock => mock.GetUserId())
                .ReturnsAsync(expectedUserId);
            var controller = new UploadFileController(_manager, _blobRepo, _configurationManager, cacheMock.Object, contextMock.Object);
            var result = await controller.GetTemplates();
            Assert.IsNotNull(result);
           
        }

        [Test]
        public async Task GetAuthorizedUrlForWriteForUrl_Test()
        {
            string url = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/HH_UID_Template___1__HIR_2023_06_30_11_23_31.xlsx?sv=2021-08-06&st=2023-06-30T11%3A18%3A31Z&se=2023-06-30T11%3A33%3A31Z&sr=b&sp=rcw&sig=YEiX4GmqbCWCsSBpXLgbtKIJGSCqclCDGA9eOuwpEn8%3D";
            string expectedWriteUrl = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/HH_UID_Template___1__HIR_2023_06_30_11_23_31.xlsx?sv=2021-08-06&st=2023-07-05T12%3A17%3A22Z&se=2023-07-05T12%3A27%3A22Z&sr=b&sp=rcw&sig=cdNtvTBdSqI9ZOg9V0oqP12DPVs8DM%2B1s%2FCbbtKzIKI%3D";

            var _blobRepoMock = new Mock<IBlobRepo>();
            _blobRepoMock.Setup(repo => repo.GetPublicAccessWriteUrl(It.IsAny<string>(), It.IsAny<int>()))
                         .ReturnsAsync((string inputUrl, int timeOutInMinutes) => expectedWriteUrl);
            var controller = new UploadFileController(_manager, _blobRepoMock.Object, _configurationManager, _cache, _context);
            var result = await controller.GetAuthorizedUrlForWriteForUrl(url);
            Assert.AreEqual(expectedWriteUrl, result);
        }

        [Test]
        public async Task UploadAndValidateFile_Test()
        {
            var file = new UploadedFile
            {
                IsActive = true,
                Name = "HH-UID Template.xlsx",
                BlobUrl = "https://lmrkapppoc1.blob.core.windows.net/filecontainer/HH_UID_Template___56__HVS_2023_07_05_16_25_37.xlsx?sv=2021-08-06&st=2023-07-05T10%3A50%3A37Z&se=2023-07-05T11%3A05%3A37Z&sr=b&sp=rcw&sig=bBSUsCxUPHcB8cftIXoPUvqGYoTjle1zgZ%2Bv6I9%2FXqQ%3D",
                TableName = "Product",
                TemplateId = 2
            };
            var uploadedFileData = new List<UploadedFileData>
            {
            };

            var fileManagerMock = new Mock<IUploadManager>();
            fileManagerMock.Setup(manager => manager.UploadFile(It.IsAny<UploadedFile>(), It.IsAny<IDbTransaction>()))
              .ReturnsAsync(file);

            fileManagerMock.Setup(manager => manager.ProcessFile(It.IsAny<UploadedFile>()))
                .ReturnsAsync(uploadedFileData);
            var controller = new UploadFileController(fileManagerMock.Object, _blobRepo, _configurationManager, _cache, _context);
            var result = await controller.UploadAndValidateFile(file);
            Assert.AreSame(uploadedFileData, result);
        }
    }
}
