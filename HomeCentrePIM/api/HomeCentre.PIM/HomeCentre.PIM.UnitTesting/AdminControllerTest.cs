using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.UnitTesting
{
    public class AdminControllerTest
    {
        private  ILabelManager _labelManager;
        private  IDbCache _cache;
        [SetUp]
        public void Setup()
        {
            _cache = Mock.Of<IDbCache>();
            _labelManager = Mock.Of<ILabelManager>();
        }
        [Test]
        public async Task AddOrUpdateLabel_Test()
        {
            var expectedLabelModel = new LabelModel { 
                Id = 1,
                Name = "string",
                Description = "string",
                Language = "string",
                LanguageId = 0,
                IsActive = true };
            var labelManagerMock = new Mock<ILabelManager>();
            var cacheMock = new Mock<IDbCache>();
            var controller = new AdminController(labelManagerMock.Object, cacheMock.Object);
            await controller.AddOrUpdateLabel(expectedLabelModel);
            labelManagerMock.Verify(m => m.AddOrUpdateLabel(expectedLabelModel), Times.Once);
            cacheMock.Verify(c => c.Clear(), Times.Once);
        }
    }
}