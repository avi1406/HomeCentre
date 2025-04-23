using HomeCentre.PIM.Api.Controllers.api;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.Dashboard;
using HomeCentre.PIM.Common.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.UnitTesting
{
   public class DashBoardControllerTest
    {
        private  IUserManager _userManager;
        [SetUp]
        public void Setup()
        {
            _userManager = Mock.Of<IUserManager>();
        }

        [Test]
        public async Task GetUserCount_ReturnsUserCount()
        {
            int expectedUserCount = 10;
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(um => um.GetUserCount())
                           .ReturnsAsync(expectedUserCount);
            var controller = new DashBoardController(userManagerMock.Object);
            int result = await controller.GetUserCount();
            Assert.AreEqual(expectedUserCount, result);
        }

        [Test]
        public async Task GetRecentLoginHistoryCountDateWise_ReturnsReportCounts()
        {
            var expectedReportCounts = new List<ReportCountsModel>
            {
               new ReportCountsModel { GroupedItem = "Group A", ReportCount = 10 },
               new ReportCountsModel { GroupedItem = "Group B", ReportCount = 15 }
            };
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(um => um.GetRecentLoginHistoryCountDateWise())
                           .ReturnsAsync(expectedReportCounts);
            var controller = new DashBoardController(userManagerMock.Object);
            IEnumerable<ReportCountsModel> result = await controller.GetRecentLoginHistoryCountDateWise();
            Assert.AreEqual(expectedReportCounts, result);
        }
    }
}
