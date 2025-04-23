using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.Api.Models;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Moq;
using NUnit.Framework.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using HomeCentre.PIM.BAL.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace HomeCentre.PIM.UnitTesting
{
    public class ProductControllerTest
    {
        private IConfigurationRoot _configuration;
        private IDbCache _cache;
        private ILabelManager _labelManager;
        private IMasterManager _masterManager;
        private IBlobRepo _blobRepo;
        private IProductManager _manager;
        private IProductManager _productManager;
        private ProductController _productController;

        public void OneTimeSetUp()
        {
            _manager = Mock.Of<IProductManager>(); 
            _cache = Mock.Of<IDbCache>();

        }

        [SetUp]
        public void Setup()
        {
            _cache = Mock.Of<IDbCache>();
            _productManager = Mock.Of<IProductManager>();
            _productController = new ProductController(_productManager, _cache);
        }

        [Test]
        public async Task ListAttributes_Test()
        {
           
            long? divisionId = 1;
            bool isActive = true;
            IEnumerable<ProductAttribute> expectedAttributes = new List<ProductAttribute>()
            {
                new ProductAttribute { Id = 1, Name = "Attribute 1" },
                new ProductAttribute { Id = 2, Name = "Attribute 2" }
            };
            Mock.Get(_cache)
                .Setup(c => c.GetAttributes(divisionId, isActive))
                .ReturnsAsync(expectedAttributes);
            IEnumerable<ProductAttribute> result = await _productController.ListAttributes(divisionId, isActive);
            Assert.AreEqual(expectedAttributes, result);
        }

        [Test]
        public async Task AddOrUpdateAttribute_Test()
        {
            var attribute = new ProductAttribute();
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.AddOrUpdateAttribute(attribute)).Returns(Task.CompletedTask);
            var controller = new ProductController(managerMock.Object, _cache);
            await controller.AddOrUpdateAttribute(attribute);
            managerMock.Verify(m => m.AddOrUpdateAttribute(attribute), Times.Once);
        }

        [Test]
        public async Task ActivateAttributes_Test()
        {
            var attributeIds = new long[] { 1, 2, 3 };
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.ActivateAttributes(attributeIds)).Returns(Task.CompletedTask);
            var controller = new ProductController(managerMock.Object, _cache);
            await controller.ActivateAttributes(attributeIds);
            managerMock.Verify(m => m.ActivateAttributes(attributeIds), Times.Once);
        }

        [Test]
        public async Task DeactivateAttributes_Test()
        {
            var attributeIds = new long[] { 1, 2, 3 };
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.DeactivateAttributes(attributeIds)).Returns(Task.CompletedTask);
            var controller = new ProductController(managerMock.Object, _cache);
            await controller.DeactivateAttributes(attributeIds);
            managerMock.Verify(m => m.DeactivateAttributes(attributeIds), Times.Once);
        }

        [Test]
        public async Task MoveAttribute_Test()
        {
            var model = new MoveAttributeModel
            {
                AttributeId = 1,
                DepartmentId = 2
            };
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.MoveAttribute(model.AttributeId, model.DepartmentId)).Returns(Task.CompletedTask);
            var controller = new ProductController(managerMock.Object, _cache);
            await controller.MoveAttribute(model);
            managerMock.Verify(m => m.MoveAttribute(model.AttributeId, model.DepartmentId), Times.Once);
        }

        [Test]
        public async Task SearchProducts_Test()
        {
            string searchKey = "test";
            int pageNumber = 1;
            int pageCount = 10;
            long? qcStatusId = null;

            var expectedResult = new SearchResult<Product>();
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.SearchProducts(searchKey, pageNumber, pageCount, qcStatusId)).ReturnsAsync(expectedResult);
            var controller = new ProductController(managerMock.Object, _cache);
            var result = await controller.SearchProducts(searchKey, pageNumber, pageCount, qcStatusId);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task ListUidProducts_Test()
        {
            DateTime fromdate = new DateTime(2023, 1, 1);
            DateTime todate = new DateTime(2023, 12, 31);
            string status = "Active";
            long deptcode = 123;
            var expectedResult = new List<ItemCodeProducts>();
            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.ListProducts(fromdate, todate, status, deptcode)).ReturnsAsync(expectedResult);
            var controller = new ProductController(managerMock.Object, _cache);
            var result = await controller.ListUidProducts(fromdate, todate, status, deptcode);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task DownloadProducts_Test()
        {
            var expectedContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var expectedFileNamePattern = @"^Products_[A-Za-z0-9_]+\.(xlsx)$";
            var testFilePath = "C:\\Users\\naga\\Downloads\\DataRequest_104_CMP_2023_07_02_14_00_36.xlsx";

            var managerMock = new Mock<IProductManager>();
            managerMock.Setup(m => m.DownloadProducts()).ReturnsAsync(testFilePath);
            var controller = new ProductController(managerMock.Object, _cache);
            var result = await controller.DownloadProducts();
            Assert.NotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual(expectedContentType, fileContentResult.ContentType);
            Assert.IsTrue(Regex.IsMatch(fileContentResult.FileDownloadName, expectedFileNamePattern));
            var expectedFileBytes = await System.IO.File.ReadAllBytesAsync(testFilePath);
            var actualFileBytes = fileContentResult.FileContents;
            Assert.AreEqual(expectedFileBytes.Length, actualFileBytes.Length);
            for (int i = 0; i < expectedFileBytes.Length; i++)
            {
                Assert.AreEqual(expectedFileBytes[i], actualFileBytes[i]);
            }
        }
    }
}







 
