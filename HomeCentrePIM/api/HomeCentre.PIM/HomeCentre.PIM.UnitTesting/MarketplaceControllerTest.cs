using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.Api.Models;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.UnitTesting
{
    public class MarketplaceControllerTest
    {
        private IMarketplaceManager _manager;

        [SetUp]
        public void Setup()
        {
            _manager = Mock.Of<IMarketplaceManager>();
        }

        [Test]
        public async Task AddOrUpdateMarketplace_Test()
        {
          
            var mockManager = new Mock<IMarketplaceManager>();
            var marketplace = new Marketplace
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-06-26T07:07:07.537Z"),
                DateModified = DateTime.Parse("2023-06-26T07:07:07.537Z"),
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                Name = "string",
                ShortCode = "string"
            };
            var expectedMarketplace = new Marketplace();
            var controller = new MarketplaceController(mockManager.Object);
            mockManager.Setup(m => m.AddOrUpdate(marketplace))
                       .ReturnsAsync(expectedMarketplace);
            var result = await controller.AddOrUpdateMarketplace(marketplace);
            Assert.AreEqual(expectedMarketplace, result);
        }

        [Test]
        public async Task AddOrUpdateMarketplaceAttribute_Test()
        {
            var mockManager = new Mock<IMarketplaceManager>();
            var attribute = new MarketplaceAttribute
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-06-26T09:13:48.615Z"),
                DateModified = DateTime.Parse("2023-06-26T09:13:48.615Z"),
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                Name = "string",
                Description = "string",
                DataType = 0,
                IsRequired = true,
                MarketplaceId = 0,
                DivisionId = 0
            };
            var expectedAttribute = new MarketplaceAttribute();
            mockManager.Setup(m => m.AddOrUpdateAttribute(attribute))
                       .ReturnsAsync(expectedAttribute);
            var controller = new MarketplaceController(mockManager.Object);
            var result = await controller.AddOrUpdateMarketplaceAttribute(attribute);
            Assert.AreEqual(expectedAttribute, result);
        }

        [Test]
        public async Task ProcessUploadedMarketplaceAttributes_Test()
        {
            var mockManager = new Mock<IMarketplaceManager>();
            var controller = new MarketplaceController(mockManager.Object);
            var model = new UploadMarketplaceAttributeModel
            {
                MarketplaceId = 0,
                DivisionId = 0,
                BlobUrl = "https://example.com/attributes.csv"
            };
            await controller.ProcessUploadedMarketplaceAttributes(model);
            mockManager.Verify(m => m.ProcessUploadedMarketplaceAttributes(model.MarketplaceId, model.DivisionId, model.BlobUrl), Times.Once());
        }

        [Test]
        public async Task AddOrUpdateMarketplaceTemplate_Test()
        {
            var mockManager = new Mock<IMarketplaceManager>();
            var controller = new MarketplaceController(mockManager.Object);
            var template = new MarketplaceTemplate
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-06-26T10:00:16.966Z"),
                DateModified = DateTime.Parse("2023-06-26T10:00:16.966Z"),
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                Name = "string",
                MarketplaceId = 0,
                ProductDepartmentId = 0,
                DivisionId = 0
            };
            var expectedTemplate = new MarketplaceTemplate();
            mockManager.Setup(m => m.AddOrUpdateTemplate(template))
                       .ReturnsAsync(expectedTemplate);
            var result = await controller.AddOrUpdateMarketplaceTemplate(template);
            Assert.AreEqual(expectedTemplate, result);
        }

        [Test]
        public async Task AddOrUpdateMarketplaceTemplateAttributes_Test()
        {
            var mockManager = new Mock<IMarketplaceManager>();
            var controller = new MarketplaceController(mockManager.Object);
            var attributes = new List<MarketplaceTemplateAttribute>
        {
        new MarketplaceTemplateAttribute
        {
            CreatedBy = 0,
            DateCreated = DateTime.Parse("2023-06-26T10:24:39.718Z"),
            DateModified = DateTime.Parse("2023-06-26T10:24:39.718Z"),
            Id = 0,
            UpdatedBy = 0,
            IsActive = true,
            MarketplaceTemplateId = 0,
            MarketplaceAttributeId = 0,
            ProductAttributeId = 0,
            ProductDepartmentId = 0,
            MarketPlaceId = 0
        }
         };

            var expectedAttributes = new List<MarketplaceTemplateAttribute>();

            mockManager.Setup(m => m.AddOrUpdateTemplateAttributes(attributes))
                       .Returns(expectedAttributes.ToAsyncEnumerable());
            var result = await controller.AddOrUpdateMarketplaceTemplateAttributes(attributes);
            Assert.AreEqual(expectedAttributes, result.ToList());
        }

        [Test]
        public async Task AddOrUpdateMarketplaceTemplateAttribute_Test()
        {
            var mockManager = new Mock<IMarketplaceManager>();
            var controller = new MarketplaceController(mockManager.Object);
            var attribute = new MarketplaceTemplateAttribute
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-06-26T10:00:16.966Z"),
                DateModified = DateTime.Parse("2023-06-26T10:00:16.966Z"),
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                MarketplaceTemplateId = 0,
                MarketplaceAttributeId = 0,
                ProductAttributeId = 0,
                ProductDepartmentId = 0,
                MarketPlaceId = 0
            };
            var expectedAttribute = new MarketplaceTemplateAttribute();
            mockManager.Setup(m => m.AddOrUpdateTemplateAttribute(attribute))
                       .ReturnsAsync(expectedAttribute);
            var result = await controller.AddOrUpdateMarketplaceTemplateAttribute(attribute);
            Assert.AreEqual(expectedAttribute, result);
        }

        [Test]
        public async Task ActivateMarketplaces_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.ActivateMarketplaces(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.ActivateMarketplaces(ids);
            mockManager.Verify(m => m.ActivateMarketplaces(ids), Times.Once);
        }

        [Test]
        public async Task ActivateMarketplaceAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.ActivateMarketplaceAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.ActivateMarketplaceAttributes(ids);
            mockManager.Verify(m => m.ActivateMarketplaceAttributes(ids), Times.Once);
        }

        [Test]
        public async Task ActivateMarketplaceTemplates_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.ActivateMarketplaceTemplates(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.ActivateMarketplaceTemplates(ids);
            mockManager.Verify(m => m.ActivateMarketplaceTemplates(ids), Times.Once);
        }

        [Test]
        public async Task ActivateMarketplaceTemplateAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.ActivateMarketplaceTemplateAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.ActivateMarketplaceTemplateAttributes(ids);
            mockManager.Verify(m => m.ActivateMarketplaceTemplateAttributes(ids), Times.Once);
        }

        [Test]
        public async Task DeactivateMarketplaceTemplateAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.DeactivateMarketplaceTemplateAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.DeactivateMarketplaceTemplateAttributes(ids);
            mockManager.Verify(m => m.DeactivateMarketplaceTemplateAttributes(ids), Times.Once);
        }

        [Test]
        public async Task DeactivateMarketplaces_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.DeactivateMarketplaces(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.DeactivateMarketplaces(ids);
            mockManager.Verify(m => m.DeactivateMarketplaces(ids), Times.Once);
        }

        [Test]
        public async Task DeactivateMarketplaceAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.DeactivateMarketplaceAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.DeactivateMarketplaceAttributes(ids);
            mockManager.Verify(m => m.DeactivateMarketplaceAttributes(ids), Times.Once);
        }

        [Test]
        public async Task DeactivateMarketplaceTemplates_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.DeactivateMarketplaceTemplates(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.DeactivateMarketplaceTemplates(ids);
            mockManager.Verify(m => m.DeactivateMarketplaceTemplates(ids), Times.Once);
        }

        [Test]
        public async Task RemoveMarketplaceTemplateAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.RemoveMarketplaceTemplateAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.RemoveMarketplaceTemplateAttributes(ids);
            mockManager.Verify(m => m.RemoveMarketplaceTemplateAttributes(ids), Times.Once);
        }

        [Test]
        public async Task RemoveMarketplaces_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.RemoveMarketplaces(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.RemoveMarketplaces(ids);
            mockManager.Verify(m => m.RemoveMarketplaces(ids), Times.Once);
        }

        [Test]
        public async Task RemoveMarketplaceAttributes_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.RemoveMarketplaceAttributes(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.RemoveMarketplaceAttributes(ids);
            mockManager.Verify(m => m.RemoveMarketplaceAttributes(ids), Times.Once);
        }

        [Test]
        public async Task RemoveMarketplaceTemplates_Test()
        {
            var ids = new long[] { 1, 2, 3 };
            var mockManager = new Mock<IMarketplaceManager>();
            mockManager.Setup(m => m.RemoveMarketplaceTemplates(ids)).Returns(Task.CompletedTask);
            var controller = new MarketplaceController(mockManager.Object);
            await controller.RemoveMarketplaceTemplates(ids);
            mockManager.Verify(m => m.RemoveMarketplaceTemplates(ids), Times.Once);
        }

        [Test]
        public async Task SearchMarketplaceAttributes_Test()
        {
            string searchKey = "keyword";
            int pageNumber = 1;
            int pageSize = 10;
            long marketPlaceId = 1;
            long divisionId = 4;

            var expectedSearchResult = new SearchResult<MarketplaceAttribute>
            {
                TotalNoOfRecords = 2,
                Records = new List<MarketplaceAttribute>
        {
            new MarketplaceAttribute
            {
                Name = "demoattri",
                Description = "added for demo purpose",
                DataType = 0,
                IsRequired = false,
                MarketplaceId = 1,
                DivisionId = 4,
                CreatedBy = 83,
                DateCreated = DateTimeOffset.Parse("2023-06-15T17:35:03.793551+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-21T11:12:31.279846+05:30"),
                Id = 3575,
                UpdatedBy = 7,
                IsActive = true
            },
            new MarketplaceAttribute
            {
                Name = "sampleattri",
                Description = "sample attribute added",
                //DataType = 10,
                IsRequired = false,
                MarketplaceId = 1,
                DivisionId = 4,
                CreatedBy = 7,
                DateCreated = DateTimeOffset.Parse("2023-06-27T13:02:24.983064+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-27T13:02:24.983067+05:30"),
                Id = 3577,
                UpdatedBy = 7,
                IsActive = true
            }
        }
            };

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.SearchMarketplaceAttributes(searchKey, pageNumber, pageSize, marketPlaceId, divisionId))
                       .ReturnsAsync(expectedSearchResult);

            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.SearchMarketplaceAttributes(searchKey, pageNumber, pageSize, marketPlaceId, divisionId);
            Assert.NotNull(result);
            Assert.IsInstanceOf<SearchResult<MarketplaceAttribute>>(result);

            var searchResult = (SearchResult<MarketplaceAttribute>)result;
            Assert.AreEqual(expectedSearchResult.TotalNoOfRecords, searchResult.TotalNoOfRecords);
            Assert.AreEqual(expectedSearchResult.Records.Count, searchResult.Records.Count);

            for (int i = 0; i < expectedSearchResult.Records.Count; i++)
            {
                Assert.AreEqual(expectedSearchResult.Records[i].Name, searchResult.Records[i].Name);
                Assert.AreEqual(expectedSearchResult.Records[i].Description, searchResult.Records[i].Description);
                
            }
        }

        [Test]
        public async Task SearchMarketplaces_Test()
        {
            string searchKey = "keyword";
            int pageNumber = 1;
            int pageSize = 10;
            bool isActive = true;

            var expectedSearchResult = new SearchResult<Marketplace>
            {
                TotalNoOfRecords = 2,
                Records = new List<Marketplace>
        {
            new Marketplace
            {
                Name = "Flipkart",
                ShortCode = "FlipKart",
                CreatedBy = 1,
                DateCreated = DateTimeOffset.Parse("2023-02-08T11:56:12.951983+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-28T16:07:50.543082+05:30"),
                Id = 2,
                UpdatedBy = 7,
                IsActive = true
            },
            new Marketplace
            {
                Name = "Amazon",
                ShortCode = "Amazon",
                CreatedBy = 1,
                DateCreated = DateTimeOffset.Parse("2023-02-08T12:20:40.317152+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-15T17:28:35.019515+05:30"),
                Id = 3,
                UpdatedBy = 1,
                IsActive = true
            }
        }
            };

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.SearchMarketplaces(searchKey, pageNumber, pageSize, isActive))
                       .ReturnsAsync(expectedSearchResult);

            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.SearchMarketplaces(searchKey, pageNumber, pageSize, isActive);

            Assert.NotNull(result);
            Assert.IsInstanceOf<SearchResult<Marketplace>>(result);

            var searchResult = (SearchResult<Marketplace>)result;
            Assert.AreEqual(expectedSearchResult.TotalNoOfRecords, searchResult.TotalNoOfRecords);
            Assert.AreEqual(expectedSearchResult.Records.Count, searchResult.Records.Count);

            for (int i = 0; i < expectedSearchResult.Records.Count; i++)
            {
                Assert.AreEqual(expectedSearchResult.Records[i].Name, searchResult.Records[i].Name);
                Assert.AreEqual(expectedSearchResult.Records[i].ShortCode, searchResult.Records[i].ShortCode);            
            }
        }

        [Test]
        public async Task SearchMarketplaceTemplateAttributes_Test()
        {
            string searchKey = "keyword";
            int pageNumber = 1;
            int pageSize = 10;
            long marketPlaceTemplateId = 269;

            var expectedSearchResult = new SearchResult<MarketplaceTemplateAttribute>
            {
                TotalNoOfRecords = 2,
                Records = new List<MarketplaceTemplateAttribute>
        {
            new MarketplaceTemplateAttribute
            {
                MarketplaceTemplateId = 269,
                MarketplaceAttributeId = null,
                ProductAttributeId = 136,
                ProductDepartmentId = 652,
                MarketPlaceId = 1,
                MarketplaceAttribute = "demo",
                CreatedBy = 7,
                DateCreated = DateTimeOffset.Parse("2023-06-26T10:47:05.678263+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-30T11:02:07.160467+05:30"),
                Id = 502,
                UpdatedBy = 7,
                IsActive = true
            },
            new MarketplaceTemplateAttribute
            {
                MarketplaceTemplateId = 269,
                MarketplaceAttributeId = null,
                ProductAttributeId = 134,
                ProductDepartmentId = 652,
                MarketPlaceId = 1,
                MarketplaceAttribute = "testing",
                CreatedBy = 7,
                DateCreated = DateTimeOffset.Parse("2023-06-27T11:11:43.76409+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-30T11:02:08.177925+05:30"),
                Id = 504,
                UpdatedBy = 7,
                IsActive = true
            }
        }
            };

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.SearchMarketplaceTemplateAttributes(searchKey, pageNumber, pageSize, marketPlaceTemplateId))
                       .ReturnsAsync(expectedSearchResult);
            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.SearchMarketplaceTemplateAttributes(searchKey, pageNumber, pageSize, marketPlaceTemplateId);
            Assert.NotNull(result);
            Assert.IsInstanceOf<SearchResult<MarketplaceTemplateAttribute>>(result);
            var searchResult = (SearchResult<MarketplaceTemplateAttribute>)result;
            Assert.AreEqual(expectedSearchResult.TotalNoOfRecords, searchResult.TotalNoOfRecords);
            Assert.AreEqual(expectedSearchResult.Records.Count, searchResult.Records.Count);

            for (int i = 0; i < expectedSearchResult.Records.Count; i++)
            {
                Assert.AreEqual(expectedSearchResult.Records[i].MarketplaceTemplateId, searchResult.Records[i].MarketplaceTemplateId);
                Assert.AreEqual(expectedSearchResult.Records[i].MarketplaceAttributeId, searchResult.Records[i].MarketplaceAttributeId);
            }
        }

        [Test]
        public async Task DownloadMarketplaceAttributeTemplate_Test()
        {
            var filePath = @"C:\Users\naga\Downloads\DataRequest_95_1F6_2023_07_01_18_38_33.xlsx";

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.DownloadMarketplaceAttributeTemplate())
                       .ReturnsAsync(filePath);
            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.DownloadMarketplaceAttributeTemplate();
            Assert.NotNull(result);
            Assert.IsInstanceOf<FileContentResult>(result);
            var fileContentResult = (FileContentResult)result;
            Assert.AreEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileContentResult.ContentType);
            var expectedFileBytes = await File.ReadAllBytesAsync("C:\\Users\\naga\\Downloads\\DataRequest_95_1F6_2023_07_01_18_38_33.xlsx");
            Assert.AreEqual(expectedFileBytes, fileContentResult.FileContents);
        }

        [Test]
        public async Task AddOrUpdateMarketplaceAttributes_Test()
        {
            var attributes = new List<MarketplaceAttribute>
            {
        new MarketplaceAttribute
        {
            Id = 3575,
            CreatedBy = 83,
            UpdatedBy = 7,
            DateCreated = DateTime.Parse("2023-06-15 12:05:03.793551+00"),
            DateModified = DateTime.Parse("2023-06-21 05:42:31.279846+00"),
            IsActive = true,
            MarketplaceId = 1,
            Name = "demoattri",
            Description = "added for demo purpose",
            DataType = 0,
            IsRequired = false,
            DivisionId = 4
          }
            };

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.AddOrUpdateAttributes(It.IsAny<IEnumerable<MarketplaceAttribute>>()))
                     .Returns(attributes.ToAsyncEnumerable());

            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.AddOrUpdateMarketplaceAttributes(attributes);

            Assert.NotNull(result);
            Assert.IsInstanceOf<IEnumerable<MarketplaceAttribute>>(result);

            var attributeList = result.ToList();
            Assert.AreEqual(attributes.Count, attributeList.Count);

            for (int i = 0; i < attributes.Count; i++)
            {
                Assert.AreEqual(attributes[i].Id, attributeList[i].Id);
                Assert.AreEqual(attributes[i].CreatedBy, attributeList[i].CreatedBy);
                Assert.AreEqual(attributes[i].UpdatedBy, attributeList[i].UpdatedBy);
                Assert.AreEqual(attributes[i].DateCreated, attributeList[i].DateCreated);
                Assert.AreEqual(attributes[i].DateModified, attributeList[i].DateModified);
                Assert.AreEqual(attributes[i].IsActive, attributeList[i].IsActive);
                Assert.AreEqual(attributes[i].MarketplaceId, attributeList[i].MarketplaceId);
                Assert.AreEqual(attributes[i].Name, attributeList[i].Name);
                Assert.AreEqual(attributes[i].Description, attributeList[i].Description);
                Assert.AreEqual(attributes[i].DataType, attributeList[i].DataType);
                Assert.AreEqual(attributes[i].IsRequired, attributeList[i].IsRequired);
                Assert.AreEqual(attributes[i].DivisionId, attributeList[i].DivisionId);
            }
        }

        [Test]
        public async Task SearchMarketplaceTemplates_Test()
        {
            string searchKey = "keyword";
            int pageNumber = 1;
            int pageSize = 10;
            long marketPlaceId = 1;
            long productDepartmentId = 652;
            bool isActive = true;

            var expectedSearchResult = new SearchResult<MarketplaceTemplate>
            {
                TotalNoOfRecords = 1,
                Records = new List<MarketplaceTemplate>
        {
            new MarketplaceTemplate
            {
                Name = "demotemplate",
                MarketplaceId = 1,
                ProductDepartmentId = 652,
                DivisionId = 4,
                CreatedBy = 1,
                DateCreated = DateTimeOffset.Parse("2023-06-15T17:32:23.963089+05:30"),
                DateModified = DateTimeOffset.Parse("2023-06-23T15:39:43.809862+05:30"),
                Id = 269,
                UpdatedBy = 1,
                IsActive = true
            }
        }
            };

            var managerMock = new Mock<IMarketplaceManager>();
            managerMock.Setup(m => m.SearchMarketplaceTemplates(
                searchKey,
                pageNumber,
                pageSize,
                marketPlaceId,
                productDepartmentId,
                isActive))
                .ReturnsAsync(expectedSearchResult);

            var controller = new MarketplaceController(managerMock.Object);
            var result = await controller.SearchMarketplaceTemplates(
                searchKey,
                pageNumber,
                pageSize,
                marketPlaceId,
                productDepartmentId,
                isActive);
            Assert.NotNull(result);
            Assert.IsInstanceOf<SearchResult<MarketplaceTemplate>>(result);

            var searchResult = (SearchResult<MarketplaceTemplate>)result;
            Assert.AreEqual(expectedSearchResult.TotalNoOfRecords, searchResult.TotalNoOfRecords);
            Assert.AreEqual(expectedSearchResult.Records.Count, searchResult.Records.Count);

            for (int i = 0; i < expectedSearchResult.Records.Count; i++)
            {
                Assert.AreEqual(expectedSearchResult.Records[i].Name, searchResult.Records[i].Name);
                Assert.AreEqual(expectedSearchResult.Records[i].MarketplaceId, searchResult.Records[i].MarketplaceId);
            }
        }
    }
}


