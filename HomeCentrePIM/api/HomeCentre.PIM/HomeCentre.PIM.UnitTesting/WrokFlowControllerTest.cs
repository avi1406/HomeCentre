using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations.LandmarkProcess;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using HomeCentre.PIM.Api.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Uid;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Presentation;
using NUnit.Framework.Internal;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace HomeCentre.PIM.UnitTesting
{
    public class WrokFlowControllerTest
    {
        private IDbCache _cache;
        private IWorkFlowManager _taskStepInstance;
        private IWorkFlowManager1 _taskStepInstance1;
        private IDataProcess _dataprocess;
        private IUIDManager _uidrepo;
        private IUserContext _userContext;
        private IDbCache _dbcache;
        private Mock<IUIDManager> _uidrepoMock;
        private object _userContextMock;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _taskStepInstance = Mock.Of<IWorkFlowManager>();
            _taskStepInstance1 = Mock.Of<IWorkFlowManager1>();
            _dataprocess = Mock.Of<IDataProcess>();
            _uidrepo = Mock.Of<IUIDManager>();
            _userContext = Mock.Of<IUserContext>();
            _dbcache = Mock.Of<IDbCache>();
        }

        [SetUp]
        public void Setup()
        {
            _dbcache = Mock.Of<IDbCache>();
        }

        [Test]
        public async Task GetUserInfoByWorkFlowRunId_ReturnsUserInfo()
        {
            int workFlowRunId = 123;
            var expectedUserInfo = new AppUser();
            var _uidrepoMock = new Mock<IUIDManager>();

            _uidrepoMock
                .Setup(repo => repo.GetPreviewInfo<AppUser>(workFlowRunId, "appuser"))
                .ReturnsAsync(expectedUserInfo);
            var controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepoMock.Object, _userContext, _dbcache);
            var result = await controller.GetUserInfoByWorkFlowRunId(workFlowRunId);
            Assert.AreEqual(expectedUserInfo, result);
            _uidrepoMock.Verify(repo => repo.GetPreviewInfo<AppUser>(workFlowRunId, "appuser"), Times.Once);
        }

        [Test]
        public async Task AddPlanogramOrFamilyName_ReturnsUidDomainModel()
        {
            var uidDomainModel = new UidInitiation();
            var _controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepo, _userContext, _dbcache);
            var result = await _controller.AddPlanogramOrFamilyName(uidDomainModel);
            Assert.AreEqual(uidDomainModel, result);
        }

        [Test]
        public async Task GetDataTableAsync_ReturnsDataTable()
        {
            string messageName = "testMessage";
            var expectedData = new List<ExcelModel>();
            var _dataprocessMock = new Mock<IDataProcess>();
            _dataprocessMock
                .Setup(dp => dp.ExcelModel(messageName))
                .ReturnsAsync(expectedData);
            var _controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocessMock.Object, _uidrepo, _userContext, _dbcache);
            var result = await _controller.GetDataTableAsync(messageName);
            Assert.AreEqual(expectedData, result);
            _dataprocessMock.Verify(dp => dp.ExcelModel(messageName), Times.Once);
        }

        [Test]
        public async Task GetTask_ReturnsTaskStepInstances()
        {
            string action = "approve";
            var expectedTaskList = new List<TaskStepInstance>();

            var _taskStepInstanceMock = new Mock<IWorkFlowManager>();
            _taskStepInstanceMock
                .Setup(ts => ts.GetTaskStepInstance(action))
                .ReturnsAsync(expectedTaskList);
            var controller = new WorkFlowController(_taskStepInstanceMock.Object, _taskStepInstance1, _dataprocess, _uidrepo, _userContext, _dbcache);
            var result = await controller.GetTask(action);
            Assert.AreEqual(expectedTaskList, result);
            _taskStepInstanceMock.Verify(ts => ts.GetTaskStepInstance(action), Times.Once);
        }

        [Test]
        public async Task SearchUID_ReturnsUidInitiationList()
        {
            var expectedSearchkey = "searchKey";
            var expectedIsPlanogram = true;
            var expectedUidInitiationList = new List<UidInitiation>();

            var _uidrepoMock = new Mock<IUIDManager>();
            _uidrepoMock.Setup(repo => repo.SearchUID(expectedSearchkey, expectedIsPlanogram)).ReturnsAsync(expectedUidInitiationList);

            var controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepoMock.Object, _userContext, _dbcache);
            var result = await controller.SearchUID(expectedSearchkey, expectedIsPlanogram);
            Assert.AreEqual(expectedUidInitiationList, result);
            _uidrepoMock.Verify(repo => repo.SearchUID(expectedSearchkey, expectedIsPlanogram), Times.Once);
        }

        [Test]
        public async Task GetPlanogramOrFamilyNameByWorkFlowId_ReturnsUidInitiation()
        {
           
            int workFlowRunId = 123; 
            var expectedUidInitiation = new UidInitiation(); 

            var _uidrepoMock = new Mock<IUIDManager>();
            _uidrepoMock
                .Setup(repo => repo.GetPreviewInfo<UidInitiation>(workFlowRunId, "uidinitiation"))
                .ReturnsAsync(expectedUidInitiation);
            var _controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepoMock.Object, _userContext, _dbcache);
            var result = await _controller.GetPlanogramOrFamilyNameByWorkFlowId(workFlowRunId);
            Assert.AreEqual(expectedUidInitiation, result);
            _uidrepoMock.Verify(repo => repo.GetPreviewInfo<UidInitiation>(workFlowRunId, "uidinitiation"), Times.Once);
        }

        [Test]
        public async Task GetIfAlreadyExistorNot_ReturnsTrue()
        {
            string planogramOrFamilyName = "ExamplePlanogram";
            bool expectedResult = true;

            var _uidrepoMock = new Mock<IUIDManager>();
            _uidrepoMock
                .Setup(repo => repo.IsAlreadyExist(planogramOrFamilyName))
                .ReturnsAsync(expectedResult);
            var _controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepoMock.Object, _userContext, _dbcache);
            var result = await _controller.GetIfAlreadyExistorNot(planogramOrFamilyName);
            Assert.AreEqual(expectedResult, result);
            _uidrepoMock.Verify(repo => repo.IsAlreadyExist(planogramOrFamilyName), Times.Once);
        }

        [Test]
        public async Task UidSearchByCriteria_ReturnsPlanagromRecortList()
        {
            UidSearchModel expectedUidSearchModel = new UidSearchModel();
            List<PlanagromRecortList> expectedPlanagromRecortList = new List<PlanagromRecortList>();
            var uidRepoMock = new Mock<IUIDManager>();
            uidRepoMock.Setup(repo => repo.UidSearchByCriteria(expectedUidSearchModel)).ReturnsAsync(expectedPlanagromRecortList);
            WorkFlowController controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, _userContext, _dbcache);
            List<PlanagromRecortList> result = await controller.UidSearchByCriteria(expectedUidSearchModel);
            Assert.AreEqual(expectedPlanagromRecortList, result);
            uidRepoMock.Verify(repo => repo.UidSearchByCriteria(expectedUidSearchModel), Times.Once);
        }

        [Test]
        public async Task ExportExcel_ReturnsFileResult()
        {
            bool expectedIsGetAll = true;
            DateTime? expectedStartDate = null;
            DateTime? expectedEndDate = null;
            string expectedFileName = "SampleFile";

            var uidRepoMock = new Mock<IUIDManager>();
            var expectedFileStream = new MemoryStream(); 
            uidRepoMock.Setup(repo => repo.GetPlanogramApprovalListAsync(expectedIsGetAll, expectedStartDate, expectedEndDate)).ReturnsAsync(expectedFileStream);
            WorkFlowController controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, _userContext, _dbcache);
            FileResult result = await controller.ExportExcel(expectedIsGetAll, expectedStartDate, expectedEndDate, expectedFileName);
            Assert.AreEqual(expectedFileName + ".xlsx", result.FileDownloadName);
            Assert.AreEqual("application/octet-stream", result.ContentType);
        }

        [Test]
        public async Task UIDExportExcel_ReturnsFileResult()
        {
            UidSearchModel expectedUidSearchModel = new UidSearchModel();
            var uidRepoMock = new Mock<IUIDManager>();
            var expectedFileStream = new MemoryStream();
            uidRepoMock.Setup(repo => repo.UIDExcel(expectedUidSearchModel)).ReturnsAsync(expectedFileStream);
            WorkFlowController controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, _userContext, _dbcache);
            FileResult result = await controller.UIDExportExcel(expectedUidSearchModel);
            Assert.AreEqual("UidExcel.xlsx", result.FileDownloadName);
            Assert.AreEqual("application/octet-stream", result.ContentType);
        }

        [Test]
        public async Task Sentemail_ReturnsString()
        {
            var expectedEmail = "example@example.com"; 
            var uidRepoMock = new Mock<IUIDManager>();
            uidRepoMock.Setup(repo => repo.SendEmail()).ReturnsAsync(expectedEmail);
            WorkFlowController controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, _userContext, _dbcache);
            string result = await controller.Sentemail();
            Assert.AreEqual(expectedEmail, result);
        }

        [Test]
        public async Task EmailContent_ReturnsEmailContentModel()
        {
            long expectedUserId = 123; 
            var expectedEmailContentModel = new EmailContentModel(); 
            var uidRepoMock = new Mock<IUIDManager>();
            uidRepoMock.Setup(repo => repo.EmailContent(expectedUserId)).ReturnsAsync(expectedEmailContentModel);
            var userContextMock = new Mock<IUserContext>();
            userContextMock.Setup(context => context.GetUserId()).ReturnsAsync(expectedUserId);
            WorkFlowController controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, userContextMock.Object, _dbcache);
            EmailContentModel result = await controller.EmailContent();
            Assert.AreEqual(expectedEmailContentModel, result);
            uidRepoMock.Verify(repo => repo.EmailContent(expectedUserId), Times.Once);
        }

        [Test]
        public async Task AddorUpdateTaskStepInstance_Test()
        {
            var taskStepInstance = new TaskStepInstance
            {
                CreatedBy = 0,
                DateCreated = DateTime.Parse("2023-07-02T14:17:45.386Z"),
                DateModified = DateTime.Parse("2023-07-02T14:17:45.386Z"),
                Id = 0,
                UpdatedBy = 0,
                IsActive = true,
                Name = "string",
                TableName = "string",
                Sequence = 0,
                IsParallel = true,
                NextStepId = 0,
                IsFinalStep = true,
                IsNextStep = true,
                OwnerId = 0,
                OwnerRoleId = 0,
                IsAll = true,
                WorkflowDesignId = 0,
                RejectedStageId = 0,
                Action = "string",
                UserActionJson = "string",
                Activity = 0,
                TypeId = 0,
                ActionTemplateId = 0,
                RejectTemplateId = 0,
                TaskStepId = 0,
                WorkflowRunId = 0,
                TaskStepInstanceStatusId = 0,
                UserComments = "string",
                Title = "string"
            };

            var taskStepInstanceMock = new Mock<IWorkFlowManager>();
            taskStepInstanceMock.Setup(m => m.AddorUpdateTaskStepInstance(taskStepInstance))
                                .Verifiable();
            var controller = new WorkFlowController(taskStepInstanceMock.Object, _taskStepInstance1, _dataprocess, _uidrepo, _userContext, _dbcache);
            await controller.AddorUpdateTaskStepInstance(taskStepInstance);
            taskStepInstanceMock.Verify(m => m.AddorUpdateTaskStepInstance(taskStepInstance), Times.Once);
        }

        [Test]
        public async Task GetPlanogramRecordList_Test()
        {
            var expectedList = new List<PlanagromRecortList>()
    {
        new PlanagromRecortList
        {
            ActionedbyL1 = "User1",
            L1ActionDate = "2022-12-01",
            PlanoOrFamilyNameCreationStatusL1 = "Status1",
            FirstLevelComments = "Comments1",
            SecondLevelComments = "Comments2",
            ActionedbyL2 = "User2",
            PlanoOrFamilyNameCreationStatusL2 = "Status2",
            L2ActionDate = "2022-12-02",
            Id = 1,
            RecordId = 1001,
            PlanoOrFamilyName = "Planogram1",
            RequestType = "Type1",
            Division = "Division1",
            Description = "Description1",
            PlanoOrFamilyCreatedBy = "User3",
            PlanoOrFamilyCreatedDate = "2022-11-30",
            TaskStepInstanceId = 2001,
            PlanoOrFamilyCode = "Code1",
            PlanoOrFamilyCodeUpdatedDate = "2022-11-29",
            PlanoOrFamilyCodeStatus = "CodeStatus1",
            L1RejectDescription = "Reject1",
            L2RejectDescription = "Reject2",
            Datecreated = "2022-11-28"
        },
       
    };

            var uidRepoMock = new Mock<IUIDManager>();
            uidRepoMock.Setup(m => m.GetPlanogramListAsync())
                       .ReturnsAsync(expectedList);
            var controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, uidRepoMock.Object, _userContext, _dbcache);
            var result = await controller.GetPlanogramRecordList();
            Assert.AreEqual(expectedList.Count, result.Count);
            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.AreEqual(expectedList[i].ActionedbyL1, result[i].ActionedbyL1);
                Assert.AreEqual(expectedList[i].L1ActionDate, result[i].L1ActionDate);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyNameCreationStatusL1, result[i].PlanoOrFamilyNameCreationStatusL1);
                Assert.AreEqual(expectedList[i].FirstLevelComments, result[i].FirstLevelComments);
                Assert.AreEqual(expectedList[i].SecondLevelComments, result[i].SecondLevelComments);
                Assert.AreEqual(expectedList[i].ActionedbyL2, result[i].ActionedbyL2);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyNameCreationStatusL2, result[i].PlanoOrFamilyNameCreationStatusL2);
                Assert.AreEqual(expectedList[i].L2ActionDate, result[i].L2ActionDate);
                Assert.AreEqual(expectedList[i].Id, result[i].Id);
                Assert.AreEqual(expectedList[i].RecordId, result[i].RecordId);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyName, result[i].PlanoOrFamilyName);
                Assert.AreEqual(expectedList[i].RequestType, result[i].RequestType);
                Assert.AreEqual(expectedList[i].Division, result[i].Division);
                Assert.AreEqual(expectedList[i].Description, result[i].Description);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyCreatedBy, result[i].PlanoOrFamilyCreatedBy);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyCreatedDate, result[i].PlanoOrFamilyCreatedDate);
                Assert.AreEqual(expectedList[i].TaskStepInstanceId, result[i].TaskStepInstanceId);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyCode, result[i].PlanoOrFamilyCode);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyCodeUpdatedDate, result[i].PlanoOrFamilyCodeUpdatedDate);
                Assert.AreEqual(expectedList[i].PlanoOrFamilyCodeStatus, result[i].PlanoOrFamilyCodeStatus);
                Assert.AreEqual(expectedList[i].L1RejectDescription, result[i].L1RejectDescription);
                Assert.AreEqual(expectedList[i].L2RejectDescription, result[i].L2RejectDescription);
                Assert.AreEqual(expectedList[i].Datecreated, result[i].Datecreated);
            }
        }

        [Test]
        public async Task GetApprovedorRejectedList_Test()
        {
            var roleId = 1;
            var divisionId = 1;
            var expectedResult = new List<PlanagromRecortList>
            {
            new PlanagromRecortList
            {
              ActionedbyL1 = "string",
                L1ActionDate = "string",
                PlanoOrFamilyNameCreationStatusL1 = "string",
                FirstLevelComments = "string",
                SecondLevelComments = "string",
                ActionedbyL2 = "string",
                PlanoOrFamilyNameCreationStatusL2 = "string",
                L2ActionDate = "string",
                Id = 0,
                RecordId = 0,
                PlanoOrFamilyName = "string",
                RequestType = "string",
                Division = "string",
                Description = "string",
                PlanoOrFamilyCreatedBy = "string",
                PlanoOrFamilyCreatedDate = "string",
                TaskStepInstanceId = 0,
                PlanoOrFamilyCode = "string",
                PlanoOrFamilyCodeUpdatedDate = "string",
                PlanoOrFamilyCodeStatus = "string",
                L1RejectDescription = "string",
                L2RejectDescription = "string",
                Datecreated = "string"

              }
            };

            var _userContextMock = new Mock<IUserContext>();
            _userContextMock.Setup(u => u.GetRoleId()).ReturnsAsync(roleId.ToString());
            _userContextMock.Setup(u => u.GetDepartmentId()).ReturnsAsync(divisionId);
            var _uidrepoMock = new Mock<IUIDManager>();
            _uidrepoMock.Setup(u => u.GetPlanogramApprovalListAsync(string.Join(",", roleId), divisionId)).ReturnsAsync(expectedResult);
            var _controller = new WorkFlowController(_taskStepInstance, _taskStepInstance1, _dataprocess, _uidrepoMock.Object, _userContextMock.Object, _dbcache);
            var result = await _controller.GetApprovedorRejectedList();
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task UpdateUserAction_WhenCalled_ReturnsTasklevelSequenceList()
        {
  
            var inputList = new List<TasklevelSequence>
            {
            new TasklevelSequence
               {
            TaskStepInstanceId = 16018,
            RecordId = 3652,
            UserAction = "Approve"
            }
          };

            var _taskStepInstance1 = new Mock<IWorkFlowManager1>();
            _taskStepInstance1.Setup(w => w.UpdateUserAction(inputList)).ReturnsAsync(inputList);

            var controller =new WorkFlowController(_taskStepInstance, _taskStepInstance1.Object, _dataprocess, _uidrepo, _userContext, _dbcache);
            var result = await controller.UpdateUserAction(inputList);
            Assert.AreEqual(inputList, result);
            _taskStepInstance1.Verify(w => w.UpdateUserAction(inputList), Times.Once);
        }
    }
}






















    

