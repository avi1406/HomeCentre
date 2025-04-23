using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.DAL;
using System.Collections;
using Microsoft.AspNetCore.Mvc;
using HomeCentre.PIM.DAL.Master;
using System.Text;
using DocumentFormat.OpenXml.ExtendedProperties;
using Template = HomeCentre.PIM.DAL.Template;

namespace HomeCentre.PIM.UnitTesting
{
    [TestFixture]
    public class TemplateControllerTest
    {
        private Mock<ITemplateManager> _managerMock;
        private IUserContext _contextMock;
        private IDbCache _cacheMock;
        private IDataProcess _processMock;
        private readonly bool? _masterDataUploadCalled;

        [SetUp]
        public void Setup()
        {
            _managerMock = new Mock<ITemplateManager>();
            _contextMock = Mock.Of<IUserContext>();
            _cacheMock = Mock.Of<IDbCache>();
            _processMock = Mock.Of<IDataProcess>();
        }

        [Test]
        public async Task ListTemplateAttributes_Test()
        {
            long templateId = 123;
            IEnumerable<TemplateAttributeModel> expectedAttributes = new List<TemplateAttributeModel>
            {
                new TemplateAttributeModel
                {
                    Name = "Attribute 1",
                    TemplateAttributeId = 1,
                    TemplateId = templateId,
                    AttributeId = 10,
                    IsDataList = false
                },
                new TemplateAttributeModel
                {
                    Name = "Attribute 2",
                    TemplateAttributeId = 2,
                    TemplateId = templateId,
                    AttributeId = 20,
                    IsDataList = true
                },
            };

            _managerMock.Setup(m => m.ListTemplateAttributes(templateId))
                        .ReturnsAsync(expectedAttributes);

            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.ListTemplateAttributes(templateId);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<IEnumerable<TemplateAttributeModel>>(result);
            var attributes = result as IEnumerable<TemplateAttributeModel>;
            Assert.AreEqual(expectedAttributes, attributes);
        }

        [Test]
        public async Task GetTemplateRoles_Test()
        {
            long templateId = 123;
            IEnumerable<TemplateRole> expectedRoles = new List<TemplateRole>
            {
                new TemplateRole {TemplateId = 1, RoleId = 1 },
                new TemplateRole { TemplateId = 2, RoleId = 3 },
             
            };

            _managerMock.Setup(m => m.GetTemplateRoles(templateId))
                        .ReturnsAsync(expectedRoles);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.GetTemplateRoles(templateId);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<IEnumerable<TemplateRole>>(result);
            var roles = result as IEnumerable<TemplateRole>;
            Assert.AreEqual(expectedRoles, roles);
        }

        [Test]
        public async Task AddOrUpdateTemplate_Test()
        {

            Template templateToAddOrUpdate = new Template
            {
                Id = 1,
                Name = "Test Template",
                DivisionId = 1,
                IsUidTemplate = true,
                TemplateStatusId = 1,
                AppUserId = 1,

            };

            _managerMock.Setup(m => m.AddOrUpdateTemplate(templateToAddOrUpdate))
                        .Returns(Task.CompletedTask);

            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.AddOrUpdateTemplate(templateToAddOrUpdate);
            _managerMock.Verify(m => m.AddOrUpdateTemplate(templateToAddOrUpdate), Times.Once);
        }

        [Test]
        public async Task LinkAttributeToTemplate_Test()
        {
            TemplateAttribute templateAttribute = new TemplateAttribute
            {
                TemplateId = 1,
                AttributeId = 123,

            };

            _managerMock.Setup(m => m.LinkAttributeToTemplate(templateAttribute))
                        .Returns(Task.CompletedTask);

            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.LinkAttributeToTemplate(templateAttribute);
            _managerMock.Verify(m => m.LinkAttributeToTemplate(templateAttribute), Times.Once);
        }

        [Test]
        public async Task LinkAttributesToTemplate_Test()
        {

            List<TemplateAttribute> templateAttributes = new List<TemplateAttribute>
            {
                new TemplateAttribute
                {
                    TemplateId = 1,
                    AttributeId = 123,

                }

            };

            _managerMock.Setup(m => m.LinkAttributesToTemplate(templateAttributes))
                        .Returns(Task.CompletedTask);

            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.LinkAttributesToTemplate(templateAttributes);
            _managerMock.Verify(m => m.LinkAttributesToTemplate(templateAttributes), Times.Once);
        }

        [Test]
        public async Task RemoveAttributeFromTemplate_Test()
        {
            long templateAttributeId = 123;

            _managerMock.Setup(m => m.RemoveAttributeFromTemplate(templateAttributeId))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.RemoveAttributeFromTemplate(templateAttributeId);
            _managerMock.Verify(m => m.RemoveAttributeFromTemplate(templateAttributeId), Times.Once);
        }

        [Test]
        public async Task RemoveAttributesFromTemplate_Test()
        {
            long[] templateAttributeIds = new long[] { 123, 456, 789 };

            _managerMock.Setup(m => m.RemoveAttributesFromTemplate(templateAttributeIds))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.RemoveAttributesFromTemplate(templateAttributeIds);
            _managerMock.Verify(m => m.RemoveAttributesFromTemplate(templateAttributeIds), Times.Once);
        }

        [Test]
        public async Task DeactivateTemplates_Test()
        {

            long[] templateIds = new long[] { 1, 2, 3 };

            _managerMock.Setup(m => m.DeactivateTemplates(templateIds))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.DeactivateTemplates(templateIds);
            _managerMock.Verify(m => m.DeactivateTemplates(templateIds), Times.Once);
        }

        [Test]
        public async Task ActivateTemplates_Test()
        {
            long[] templateIds = new long[] { 1, 2, 3 };

            _managerMock.Setup(m => m.ActivateTemplate(templateIds))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.ActivateTemplates(templateIds);
            _managerMock.Verify(m => m.ActivateTemplate(templateIds), Times.Once);
        }

        [Test]
        public async Task PublishTemplate_Test()
        {

            long[] templateIds = new long[] { 1, 2, 3 };

            _managerMock.Setup(m => m.PublishTemplate(templateIds))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            await controller.PublishTemplate(templateIds);
            _managerMock.Verify(m => m.PublishTemplate(templateIds), Times.Once);
        }

        [Test]
        public async Task MasterDataUpload_Test()
        {

            string filePath = "path/to/file.csv";
            long masterId = 123;

            var _processMock = new Mock<IDataProcess>();
            _processMock.Setup(p => p.MasterDataUpload(filePath, masterId))
                        .Returns(Task.CompletedTask);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock.Object);
            await controller.MasterDataUpload(filePath, masterId);
            _processMock.Verify(p => p.MasterDataUpload(filePath, masterId), Times.Once);
        }

        [Test]
        public async Task SearchTemplates_Test()
        {
            string key = "example";
            int pageNumber = 1;
            int pageCount = 10;
            bool isActive = true;
            int departmentId = 123; 

            var expectedResult = new SearchResult<Template>();
            _managerMock
                .Setup(manager => manager.SearchTemplates(key, pageNumber, pageCount, departmentId, isActive))
                .ReturnsAsync(expectedResult);
            Mock.Get(_contextMock)
                .Setup(context => context.GetDepartmentId())
                .ReturnsAsync(departmentId);

            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.SearchTemplates(key, pageNumber, pageCount, isActive);
            Assert.AreEqual(expectedResult, result);
            _managerMock.Verify(manager => manager.SearchTemplates(key, pageNumber, pageCount, departmentId, isActive), Times.Once);
            Mock.Get(_contextMock).Verify(context => context.GetDepartmentId(), Times.Once);
        }

        [Test]
        public async Task GetMasterData_Test()
        {
            long masterDataId = 1;

            var expectedResult = new List<object>();
            _managerMock
                .Setup(manager => manager.GetMasterData(masterDataId))
                .ReturnsAsync(expectedResult);
            var controller = new TemplateController(_managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.GetMasterData(masterDataId);
            Assert.AreEqual(expectedResult, result);
            _managerMock.Verify(manager => manager.GetMasterData(masterDataId), Times.Once);
        }

        [Test]
        public async Task DownloadTemplate_Test()
        {
         
            long templateId = 9;
            string format = "xlsx";

            var template = new Template
            {
                Id = templateId,
                Name = "TestTemplate",
                DivisionId = 4
            };
            var managerMock = new Mock<ITemplateManager>();
            managerMock.Setup(m => m.GetTemplate(templateId))
                       .ReturnsAsync(template);
            var division = new Division
            {
                Id = 4,
                Name = "TestDivision"
            };

            var cacheMock = new Mock<IDbCache>();
            cacheMock.Setup(c => c.GetMaster<Division>())
                     .ReturnsAsync(new List<Division> { division });

            var fileBytes = Encoding.UTF8.GetBytes("Test file content");

            var filePath = "C:\\Users\\naga\\Downloads\\FUR-QC template.xlsx";
            managerMock.Setup(m => m.DownloadTemplate(templateId, format))
                       .ReturnsAsync(filePath);
            var controller = new TemplateController(managerMock.Object, _contextMock, cacheMock.Object, _processMock);
            var result = await controller.DownloadTemplate(templateId, format);
            Assert.NotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileContentResult.ContentType);
            Assert.AreEqual($"{template.Name}.{format}", fileContentResult.FileDownloadName);
            var fileBytesResult = fileContentResult.FileContents;
            var expectedFileBytes = await File.ReadAllBytesAsync(filePath);
            Assert.AreEqual(expectedFileBytes.Length, fileBytesResult.Length);
            for (int i = 0; i < expectedFileBytes.Length; i++)
            {
                Assert.AreEqual(expectedFileBytes[i], fileBytesResult[i]);
            }
        }

        [Test]
        public async Task DownloadItemCodeTemplate_Test()
        {
            DateTime fromDate = new DateTime(2023, 1, 1);
            DateTime toDate = new DateTime(2023, 12, 31);
            string status = "Active";
            long deptCode = 123;

            var fileBytes = Encoding.UTF8.GetBytes("Test file content");
            var filePath = "C:\\Users\\naga\\Downloads\\FUR-QC template.xlsx"; 
            var managerMock = new Mock<ITemplateManager>();
            managerMock.Setup(m => m.DownloadItemCodeTemplate(fromDate, toDate, status, deptCode))
                       .ReturnsAsync(filePath);
            var controller = new TemplateController(managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.DownloadItemCodeTemplate(fromDate, toDate, status, deptCode);
            Assert.NotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileContentResult.ContentType);
            Assert.AreEqual("Item Code Template.xlsx", fileContentResult.FileDownloadName);
            var fileBytesResult = fileContentResult.FileContents;
            var expectedFileBytes = await File.ReadAllBytesAsync(filePath);
            Assert.AreEqual(expectedFileBytes.Length, fileBytesResult.Length);
            for (int i = 0; i < expectedFileBytes.Length; i++)
            {
                Assert.AreEqual(expectedFileBytes[i], fileBytesResult[i]);
            }
        }

        [Test]
        public async Task DownloadMasterDataAsExcel_Test()
        {
            long masterDataID = 0;
            string name = "TestMasterData";

            var fileData = Encoding.UTF8.GetBytes("Test file content");
            var managerMock = new Mock<ITemplateManager>();
            managerMock.Setup(m => m.DownloadMasterDataAsExcel(masterDataID))
                       .ReturnsAsync(fileData);

            var controller = new TemplateController(managerMock.Object, _contextMock, _cacheMock, _processMock);
            var result = await controller.DownloadMasterDataAsExcel(masterDataID, name);
            Assert.NotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileContentResult.ContentType);
            Assert.AreEqual($"{name}.xlsx", fileContentResult.FileDownloadName);
            var fileDataResult = fileContentResult.FileContents;
            Assert.AreEqual(fileData.Length, fileDataResult.Length);
            for (int i = 0; i < fileData.Length; i++)
            {
                Assert.AreEqual(fileData[i], fileDataResult[i]);
            }
        }
    }
}

