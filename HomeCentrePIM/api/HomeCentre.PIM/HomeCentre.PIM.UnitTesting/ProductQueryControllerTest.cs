using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
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
    public class ProductQueryControllerTest
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
        }

        [SetUp]
        public void Setup()
        {
            _manager = Mock.Of<IProductManager>();
        }

        [Test]
        public async Task CreateQuery_Test()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var query = new ProductQuery();

            mockManager.Setup(m => m.CreateQuery(It.IsAny<ProductQuery>()))
                       .ReturnsAsync(query);
            var result = await controller.CreateQuery(query);
            Assert.AreEqual(query, result);
            mockManager.Verify(m => m.CreateQuery(It.IsAny<ProductQuery>()), Times.Once);
        }

        [Test]
        public async Task ReassignQuery_Test()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var queryIds = new long[] { 1, 2, 3 };
            var assignedToId = 4;
            await controller.ReassignQuery(queryIds, assignedToId);
            mockManager.Verify(m => m.ReassignQuery(queryIds, assignedToId), Times.Once);
        }

        [Test]
        public async Task ResolveQuery_Test()
        {
            var mockManager = new Mock<IProductQueryManager>();       
            var queryIds = new long[] { 1, 2, 3 };
            var controller = new ProductQueryController(mockManager.Object);
            await controller.ResolveQuery(queryIds);
            mockManager.Verify(m => m.ResolveQuery(queryIds), Times.Once);
        }

        [Test]
        public async Task ActivateQuery_CallsManagerWithCorrectParameters()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var queryIds = new long[] { 1, 2, 3 };
            await controller.ActivateQuery(queryIds);
            mockManager.Verify(m => m.ActivateQuery(queryIds), Times.Once);
        }

        [Test]
        public async Task DeactivateQuery_CallsManagerWithCorrectParameters()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var queryIds = new long[] { 1, 2, 3 };
            await controller.DeactivateQuery(queryIds);
            mockManager.Verify(m => m.DeactivateQuery(queryIds), Times.Once);
        }

        [Test]
        public async Task ListProducts_ReturnsProductsFromManager()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var queryId = 1;
            var expectedProducts = new List<Product> { new Product(), new Product() };

            mockManager.Setup(m => m.ListProducts(queryId))
                       .ReturnsAsync(expectedProducts);
            var result = await controller.ListProducts(queryId);
            Assert.AreEqual(expectedProducts, result);
        }

        [Test]
        public async Task SearchQueries_ReturnsSearchResultFromManager()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var searchKey = "example";
            var pageNumber = 1;
            var pageSize = 10;
            var assignedToId = 1;
            var expectedResult = new SearchResult<ProductQuery>();

            mockManager.Setup(m => m.SearchQueries(searchKey, pageNumber, pageSize, assignedToId))
                       .ReturnsAsync(expectedResult);
            var result = await controller.SearchQueries(searchKey, pageNumber, pageSize, assignedToId);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task SearchMyQueries_ReturnsSearchResultFromManager()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var searchKey = "example";
            var pageNumber = 1;
            var pageSize = 10;
            var statusId = 30;
            var expectedResult = new SearchResult<ProductQuery>();

            mockManager.Setup(m => m.SearchMyQueries(searchKey, pageNumber, pageSize, statusId))
                       .ReturnsAsync(expectedResult);
            var result = await controller.SearchMyQueries(searchKey, pageNumber, pageSize, statusId);
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task CreateQueryComment_ReturnsCreatedQueryComment()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var queryComment = new ProductQueryComment();
            var expectedComment = new ProductQueryComment();

            mockManager.Setup(m => m.CreateQueryComment(queryComment))
                       .ReturnsAsync(expectedComment);
            var result = await controller.CreateQueryComment(queryComment);
            Assert.AreEqual(expectedComment, result);
        }

        [Test]
        public async Task SearchQueryComments_ReturnsSearchResultFromManager()
        {
            var mockManager = new Mock<IProductQueryManager>();
            var controller = new ProductQueryController(mockManager.Object);
            var searchKey = "example";
            var pageNumber = 1;
            var pageSize = 10;
            var queryId = 1;
            var expectedResult = new SearchResult<ProductQueryComment>();

            mockManager.Setup(m => m.SearchQueryComments(searchKey, pageNumber, pageSize, queryId))
                       .ReturnsAsync(expectedResult);
            var result = await controller.SearchQueryComments(searchKey, pageNumber, pageSize, queryId);
            Assert.AreEqual(expectedResult, result);
        }
    }
}
