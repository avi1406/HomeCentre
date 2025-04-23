using Castle.Core.Configuration;
using DocumentFormat.OpenXml.Office2013.Excel;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.Azure.Documents;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeCenter.PIM.UnitTest
{
    public class Tests
    {
        private readonly IDbCache _cache;
        private readonly ILabelManager _labelManager;
        private readonly IMasterManager _masterManager;
        private readonly IBlobRepo _blobRepo;
        private readonly IConfiguration configuration;


        public Tests(IConfiguration _configuration)
        {
            configuration = _configuration;
 
            _cache = Mock.Of<IDbCache>();
            _labelManager = Mock.Of<ILabelManager>();
            _masterManager = Mock.Of<IMasterManager>();
            _blobRepo = Mock.Of<IBlobRepo>();
        }

        [Test]
        public async Task GetLabels_Returns_Labels_From_Cache()
        {
            // Arrange
            long? languageId = 1; // Replace with desired languageId
            var expectedLabels = new List<LabelModel> { new LabelModel { Id = 1, Name = "Label 1" }, new LabelModel { Id = 2, Name = "Label 2" } };

            Mock.Get(_cache)
                .Setup(c => c.GetAllLabels(languageId))
                .ReturnsAsync(expectedLabels);

            var service = new MasterController(_cache, _labelManager, _masterManager, _blobRepo); // Replace MasterController with the actual service class name

            // Act
            var result = await service.GetLabels(languageId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedLabels));
            Mock.Get(_cache).Verify(c => c.GetAllLabels(languageId), Times.Once);
        }

        //[Test]

        //public async Task GetLabels_Returns_Labels_From_Cache()
        //{
        //    // Arrange
        //    long? languageId = 1; // Replace with desired languageId
        //    var expectedLabels = new List<LabelModel>
        //    {
        //new LabelModel { Id = 1, Name = "Label 1" },
        //new LabelModel { Id = 2, Name = "Label 2" }
        // };

        //    // Replace the following code with the actual implementation to retrieve labels from the database
        //    var database = new YourDatabase(); // Replace YourDatabase with the actual class representing your database
        //    var dbLabels = await database.GetLabels(languageId);

        //    var service = new MasterController(_cache, _labelManager, _masterManager, _blobRepo); // Replace MasterController with the actual service class name

        //    // Act
        //    var result = await service.GetLabels(languageId);

        //    // Assert
        //    Assert.That(result, Is.EqualTo(expectedLabels));
        //    Mock.Get(_cache).Verify(c => c.GetAllLabels(languageId), Times.Once);

        //    CollectionAssert.AreEqual(expectedLabels, dbLabels);
        //}


        


    }
}