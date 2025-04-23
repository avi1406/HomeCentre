using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeCentre.PIM.UnitTesting
{
    public class DataRequiredControllerTest
    {
        private  IDataRequestManager _manager;

        public void OneTimeSetUp()
        {
            _manager = Mock.Of<IDataRequestManager>();
        }

        [SetUp]
        public void Setup()
        {
            _manager = Mock.Of<IDataRequestManager>();
        }

        [Test]
        public async Task CreateDataRequest_ReturnsDataRequest()
        {
            DataRequest expectedRequest = new DataRequest {
            Name = "John Doe",
           TemplateId = 123,
        };
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(manager => manager.CreateRequest(It.IsAny<DataRequest>())).ReturnsAsync(expectedRequest);
            var controller = new DataRequestController(managerMock.Object);
            DataRequest result = await controller.CreateDataRequest(expectedRequest);
            Assert.AreEqual(expectedRequest, result);
            managerMock.Verify(manager => manager.CreateRequest(expectedRequest), Times.Once);
        }

        [Test]
        public async Task ActivateDataRequests_CallsManagerWithCorrectIds()
        {
            long[] expectedIds = { 1, 2, 3 };
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.ActivateDataRequests(expectedIds)).Returns(Task.CompletedTask);
            var controller = new DataRequestController(managerMock.Object);       
            await controller.ActivateDataRequests(expectedIds);
            managerMock.Verify(m => m.ActivateDataRequests(expectedIds), Times.Once);
        }

        [Test]
        public async Task DeactivateDataRequests_CallsManagerWithCorrectIds()
        {
            long[] expectedIds = { 4, 5, 6 };
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.DeactivateDataRequests(expectedIds)).Returns(Task.CompletedTask);
            var controller = new DataRequestController(managerMock.Object);
            await controller.DeactivateDataRequests(expectedIds);
            managerMock.Verify(m => m.DeactivateDataRequests(expectedIds), Times.Once);
        }

        [Test]
        public async Task SearchDataRequests_ReturnsSearchResultFromManager()
        {
            string expectedSearchKey = "test";
            int expectedPageNumber = 1;
            int expectedPageSize = 10;
            bool expectedIsActive = true;
            var expectedSearchResult = new SearchResult<DataRequest>();
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.SearchDataRequests(expectedSearchKey, expectedPageNumber, expectedPageSize, expectedIsActive))
                       .ReturnsAsync(expectedSearchResult);
            var controller = new DataRequestController(managerMock.Object);
            var result = await controller.SearchDataRequests(expectedSearchKey, expectedPageNumber, expectedPageSize, expectedIsActive);
            Assert.AreEqual(expectedSearchResult, result);
        }

        [Test]
        public async Task SearchDataRequestItems_ReturnsSearchResultFromManager()
        {
            string expectedSearchKey = "test";
            int expectedPageNumber = 1;
            int expectedPageSize = 10;
            long expectedDataRequestId = 123;
            var expectedSearchResult = new SearchResult<DataRequestProduct>();
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.SearchDataRequestItems(expectedSearchKey, expectedPageNumber, expectedPageSize, expectedDataRequestId))
                       .ReturnsAsync(expectedSearchResult);
            var controller = new DataRequestController(managerMock.Object);
            var result = await controller.SearchDataRequestItems(expectedSearchKey, expectedPageNumber, expectedPageSize, expectedDataRequestId);
            Assert.AreEqual(expectedSearchResult, result);
        }

        [Test]
        public async Task GetDataRequestItemCountByStatus_ReturnsDictionaryFromManager()
        {
            long expectedDataRequestId = 123;
            var expectedItemCountByStatus = new Dictionary<string, int>();
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.GetDataRequestItemCountByStatus(expectedDataRequestId))
                       .ReturnsAsync(expectedItemCountByStatus);

            var controller = new DataRequestController(managerMock.Object);
            var result = await controller.GetDataRequestItemCountByStatus(expectedDataRequestId);
            Assert.AreEqual(expectedItemCountByStatus, result);
        }

        [Test]
        public async Task SendRecordToQc_CallsManagerWithCorrectProductId()
        {
            long expectedProductId = 456;
            var managerMock = new Mock<IDataRequestManager>();
            managerMock.Setup(m => m.SendRecordToQc(expectedProductId)).Returns(Task.CompletedTask);
            var controller = new DataRequestController(managerMock.Object);
            await controller.SendRecordToQc(expectedProductId);
            managerMock.Verify(m => m.SendRecordToQc(expectedProductId), Times.Once);
        }
    }
}
