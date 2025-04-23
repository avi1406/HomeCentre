using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using HomeCentre.PIM.Api.Controllers.api;
using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.Tenant;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Linq.Expressions;


namespace HomeCentre.PIM.UnitTesting
{
    [TestFixture]
    public class UserControllerTest
    {
        private IConfigurationRoot _configuration;
        private IDbCache _cache;
        private ILabelManager _labelManager;
        private IMasterManager _masterManager;
        private IBlobRepo _blobRepo;
        private IUserManager _userManager;
        private IDataProcess _dataProcess;
        private IDbCache _dbCache;
        private object _myClass;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _userManager = Mock.Of<IUserManager>();
            _dataProcess = Mock.Of<IDataProcess>();
            _dbCache = Mock.Of<IDbCache>();
        }

        [SetUp]
        public void Setup()
        {
            _dbCache = Mock.Of<IDbCache>();
        }

        [Test]
        public async Task ActiveUsers_Test()
        {
            var expectedUsers = new List<AppUser>
            {
             new AppUser { Id = 1, Name = "User 1", IsActive = true },
            new AppUser { Id = 2, Name = "User 2", IsActive = true },
            new AppUser { Id = 3, Name = "User 3", IsActive = true }
            };

            var userManagerMock = Mock.Get(_userManager);
            userManagerMock.Setup(mock => mock.GetAllActiveUsers())
                           .ReturnsAsync(expectedUsers);
            var controller = new UserController(_userManager, _dataProcess, _dbCache);
            var result = await controller.ActiveUsers();
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<AppUser>>(result);
            var userList = (List<AppUser>)result;
            CollectionAssert.AreEquivalent(expectedUsers, userList);
        }

        [Test]
        public async Task InactiveUsers_Test()
        {
           
            var userManagerMock = new Mock<IUserManager>();
            var expectedUsers = new List<AppUser>
             {
             new AppUser { Id = 1, Name = "User1", IsActive = false },
             new AppUser { Id = 2, Name = "User2", IsActive = false }
             };

            userManagerMock.Setup(mock => mock.GetAllInactiveUsers()).ReturnsAsync(expectedUsers);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.InactiveUsers();
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUsers.Count, result.Count);
            Assert.AreEqual(expectedUsers[0].Id, result[0].Id);
            Assert.AreEqual(expectedUsers[1].Name, result[1].Name);
         
            userManagerMock.Verify(mock => mock.GetAllInactiveUsers(), Times.Once);
        }

        [Test]
        public async Task SearchUsers_Test()
        {        
            bool isActive = true;
            var expectedResult = new SearchResult<UserListItemModel>();

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock
                .Setup(manager => manager.SearchUsers(isActive))
                .ReturnsAsync(expectedResult);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.SearchUsers(isActive);
            Assert.AreEqual(expectedResult, result);
            userManagerMock.Verify(manager => manager.SearchUsers(isActive), Times.Once);
        }

        [Test]
        public async Task GetUserByEmail_Test()
        {

            string userEmail = "test@example.com";
            var expectedUser = new AppUser();

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock
                .Setup(manager => manager.GetUserByEmail(userEmail))
                .ReturnsAsync(expectedUser);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.GetUserByEmail(userEmail);
            Assert.AreEqual(expectedUser, result);
            userManagerMock.Verify(manager => manager.GetUserByEmail(userEmail), Times.Once);
        }

        [Test]
        public async Task UpdateUser_Test()
        {
            var appUser = new UserListItemModel
            {
                Id = 1,
                Name = "John Doe",
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com",
                Mobile = "1234567890",
                IsActive = true,
                Ismailverified = true,
                Role = "User",
                RoleId = 2,
                TenantId = 3,
                GenderId = 4,
                Age = 30
            };
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.UpdateUser(appUser)).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.UpdateUser(appUser);
            userManagerMock.Verify(manager => manager.UpdateUser(appUser), Times.Once);
        }

        [Test]
        public async Task ActivateUser_Test()
        {
            var userId = 1;
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.ActivateUser(userId)).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.ActivateUser(userId);
            userManagerMock.Verify(manager => manager.ActivateUser(userId), Times.Once);
        }

        [Test]
        public async Task ActivateUserBatch_ValidIds_Test()
        {
            var userIds = new long[] { 1, 2, 3 };
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.ActivateUserBatch(userIds)).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.ActivateUserBatch(userIds);
            userManagerMock.Verify(manager => manager.ActivateUserBatch(userIds), Times.Once);
        }

        [Test]
        public async Task DeactivateUser_Test()
        {
            var userId = 1;

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.DeactivateUser(userId)).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.DeactivateUser(userId);
            userManagerMock.Verify(manager => manager.DeactivateUser(userId), Times.Once);
        }

        [Test]
        public async Task DeactivateUserBatch_Test()
        {
            var userIds = new long[] { 1, 2, 3 };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.DeactivateUserBatch(userIds)).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.DeactivateUserBatch(userIds);
            userManagerMock.Verify(manager => manager.DeactivateUserBatch(userIds), Times.Once);
        }

        [Test]
        public async Task GetUserCount_Test()
        {
            int expectedUserCount = 10;
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.GetUserCount()).ReturnsAsync(expectedUserCount);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            int result = await controller.GetUserCount();
            Assert.AreEqual(expectedUserCount, result);
            userManagerMock.Verify(manager => manager.GetUserCount(), Times.Once);
        }

        [Test]
        public async Task AddRole_Test()
        {
            var model = new Role
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-06-24T16:12:08.696Z"),
                DateModified = DateTime.Parse("2023-06-24T16:12:08.696Z"),
                Id = 7,
                UpdatedBy = 0,
                IsActive = true,
                Description = "Admin",
                Name = "Admin"
            };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.AddRole(It.IsAny<Role>())).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.AddRole(model);
            userManagerMock.Verify(manager => manager.AddRole(It.IsAny<Role>()), Times.Once);
        }

        [Test]
        public async Task AddUserRole_Test()
        {
            var model = new AppUserRoleModel
            {
               
                AppUserRoleDomainModel = new List<AppUserRoleDomainModel>
            {
            new AppUserRoleDomainModel
                {
                RoleId = 5,
                DepartmentId = 2,
                Name = "string",
                FromDate = DateTime.Parse("2023-06-24T16:16:12.228Z"),
                ToDate = DateTime.Parse("2023-06-24T16:16:12.228Z")
                 }
             }
            };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.AddUserRole(It.IsAny<AppUserRoleModel>())).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.AddUserRole(model);
            userManagerMock.Verify(manager => manager.AddUserRole(It.IsAny<AppUserRoleModel>()), Times.Once);
        }

        [Test]
        public async Task AddDepartmentToUser_Test()
        {
            var appUserRoleModel = new AppUserRoleModel
            {
                AppUserid = 7,
                AppUserRoleDomainModel = new List<AppUserRoleDomainModel>
            {
            new AppUserRoleDomainModel
              {
                RoleId = 0,
                DepartmentId = 0,
                Name = "string",
                FromDate = DateTime.Parse("2023-06-24T16:20:35.271Z"),
                ToDate = DateTime.Parse("2023-06-24T16:20:35.271Z")
               }
            }
            };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.AddUserDepartment(It.IsAny<AppUserRoleModel>())).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.AddDepartmentToUser(appUserRoleModel);
            userManagerMock.Verify(manager => manager.AddUserDepartment(It.IsAny<AppUserRoleModel>()), Times.Once);
        }

        [Test]
        public async Task GetDepartmentDetails_Test()
        {
            var userId = "1";
            var expectedDepartmentDetails = new List<UserDepartment>
             {
        new UserDepartment
             {
            DepartmentId = 1,
            DepartmentName = "Department 1",
            DivisionName = "Division 1",
            IsPlanogram = true,
            DivisionId = 1
              },
             };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock
                .Setup(manager => manager.GetDepartmentDetails(userId))
                .ReturnsAsync(expectedDepartmentDetails);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.GetDepartmentDetails(userId);
            Assert.AreEqual(expectedDepartmentDetails, result);
            userManagerMock.Verify(manager => manager.GetDepartmentDetails(userId), Times.Once);
        }

        [Test]
        public async Task DeleteDepartmentToUser_Test()
        {
            var appUserRole = new AppUserRole
            {
                CreatedBy = 0,
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now,
                Id = 1,
                UpdatedBy = 0,
                IsActive = true,
                Description = "string",
                Name = "string",
                AppUserId = 2,
                RoleId = 0,
                DepartmentId = 0
            };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.DeleteDepartment(It.IsAny<AppUserRole>())).Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.DeleteDepartmentToUser(appUserRole);
            userManagerMock.Verify(manager => manager.DeleteDepartment(It.IsAny<AppUserRole>()), Times.Once);
        }

        [Test]
        public async Task LinkUsersToMarketPlaces_Test()
        {
            var appuserMarketplaces = new List<AppuserMarketplace>
                 {
            new AppuserMarketplace
            {
                CreatedBy = 0,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                MarketplaceId = 0,
                AppUserId = 0
                }
            };

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.LinkUsersToMarketPlaces(It.IsAny<IEnumerable<AppuserMarketplace>>()))
                .Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.LinkUsersToMarketPlaces(appuserMarketplaces);
            userManagerMock.Verify(manager => manager.LinkUsersToMarketPlaces(It.IsAny<IEnumerable<AppuserMarketplace>>()), Times.Once);
        }

        [Test]
        public async Task UnlinkUserFromMarketPlaces_Test()
        {
            var ids = new long[] { 1, 2, 3 }; 

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.UnlinkUserFromMarketPlaces(It.IsAny<long[]>()))
                .Returns(Task.CompletedTask);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await controller.UnlinkUserFromMarketPlaces(ids);
            userManagerMock.Verify(manager => manager.UnlinkUserFromMarketPlaces(It.IsAny<long[]>()), Times.Once);
        }

        [Test]
        public async Task GetMarketplacesOfUser_Test()
        {
            var userId = 1; 
            var expectedMarketplaces = new List<Marketplace>();
            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(manager => manager.GetMarketplacesOfUser(It.IsAny<long>()))
                .ReturnsAsync(expectedMarketplaces);
            var controller = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.GetMarketplacesOfUser(userId);
            Assert.AreEqual(expectedMarketplaces, result);
            userManagerMock.Verify(manager => manager.GetMarketplacesOfUser(It.IsAny<long>()), Times.Once);
        }

        [Test]
        public async Task AddOrUpdateUser_Test()
        {
            AddOrUpdateUserViewModel model = new AddOrUpdateUserViewModel
            {
                FirstName = "demon",
                LastName = "nnnn",
                Email = "",
                Mobile = "1234567897",
                Id = 10,
                ProfilePicture = "string",
                DepartmentId = 1,
                RoleId = 1
            };

            var userManagerMock = new Mock<IUserManager>();
            var userController = new UserController(userManagerMock.Object, _dataProcess, _dbCache);
            await userController.AddOrUpdateUser(model);
            Assert.Pass();
        }

        [Test]
        public async Task SearchAllUsers_Test()
        {
            var isActiveFilter = new IsActiveFilter();
            var expectedUsers = new SearchResult<UserListItemModel>
            {
                Records = new List<UserListItemModel>
                {
                    new UserListItemModel
                    {
                        Id = 136,
                        Name = "Aadhithya Kk",
                        FirstName = "Aadhithya",
                        LastName = "KK",
                        Email = "mohit123@gmail.com",
                        Mobile = "6380370061",
                        IsActive = false,
                        Ismailverified = false
                    }
                }
            };

            var _userManagerMock = new Mock<IUserManager>();
            _userManagerMock.Setup(manager => manager.SearchAllUsers(isActiveFilter))
                            .ReturnsAsync(expectedUsers);
            var controller = new UserController(_userManagerMock.Object, _dataProcess, _dbCache);
            var result = await controller.SearchAllUsers(isActiveFilter);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUsers.Records.Count, result.Records.Count);

            for (int i = 0; i < expectedUsers.Records.Count; i++)
            {
                Assert.AreEqual(expectedUsers.Records[i].Id, result.Records[i].Id);
                Assert.AreEqual(expectedUsers.Records[i].Name, result.Records[i].Name);
            }
        }
    }
}
