using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace HomeCentre.PIM.WorkFlow.Repository
{
    public class TaskstepInstanceRepo : PgEntityAction<TaskStepInstance>, IWorkFlowManager
    {
        private readonly IUserContext _userContext;

        public TaskstepInstanceRepo(IDbConnectionFactory connectionFactory,
            IUserContext userContext, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
        }

        public async Task AddorUpdateTaskStepInstance(TaskStepInstance taskStepInstance)
        {
            try
            {
                var tableinstance = new TaskStepInstance();
                tableinstance.Id = taskStepInstance.Id;
                tableinstance.Name = taskStepInstance.Name;
                tableinstance.TaskStepInstanceStatusId = taskStepInstance.TaskStepInstanceStatusId;
                tableinstance.TableName = taskStepInstance.TableName;
                tableinstance.UserComments = taskStepInstance.UserComments;
                tableinstance.Action = taskStepInstance.Action;
                tableinstance.Sequence = taskStepInstance.Sequence;
                tableinstance.IsParallel = taskStepInstance.IsParallel;
                tableinstance.NextStepId = taskStepInstance.NextStepId;
                tableinstance.IsFinalStep = taskStepInstance.IsFinalStep;
                tableinstance.OwnerId = taskStepInstance.OwnerId;
                tableinstance.OwnerRoleId = taskStepInstance.OwnerRoleId;
                tableinstance.IsAll = taskStepInstance.IsAll;
                taskStepInstance.WorkflowDesignId = taskStepInstance.WorkflowDesignId;

                taskStepInstance = await AddOrUpdate(taskStepInstance, await _userContext.GetUserId());
            }
            catch (Exception e)
            {
                throw new Exception($"Error on AddorUpdateTaskStepInstance : {e.Message}");
            }
        }

        public async Task<string> GetllTaskStepInstance(int id)
        {
            try
            {
                using var connection = this.GetConnection();
                var result = (await connection.QueryAsync<TaskStepInstance>("select * from taskstepinstance where id = @id", new { id = id })).FirstOrDefault();
                return result.TableName;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetllTaskStepInstance : {e.Message}");
            }
        }

        public async Task CreateTaskStepInstance(long taskStepId, long recordId, bool isFirstStep, long previousStepId, long? updatedById = null)
        {
            try
            {
                using var connection = this.GetConnection();
                var title = string.Empty;
                var taskStep = (await connection.QueryAsync<TaskStep>("select * from taskstep where id = @Id", new { Id = taskStepId })).FirstOrDefault();
                if (taskStep == null)
                {
                    throw new Exception("Error on CreateTaskStepInstance : The taskStepId is null");
                }

                var workflowRun = await CreateWorkflowRun(taskStep.WorkflowDesignId, recordId, updatedById);
                var workflowdesignconfig = (await connection.QueryAsync("select configjson from workflowdesign where id = @id", new { id = taskStep.WorkflowDesignId })).FirstOrDefault();
                var tableName = (await connection.QueryAsync<TableMetadata>("select * from tablemetadata where id = @id", new { id = workflowRun.TableMetadataId })).FirstOrDefault();

                // var record = (await connection.QueryAsync($"select * from {tableName.Name} where id = @id", new { id = recordId })).FirstOrDefault();
                var configJson = JArray.Parse(workflowdesignconfig.configjson);
                var updateValue = string.Empty;
                var updateData = string.Empty;
                foreach (var item in configJson.Children<JObject>())
                {
                    if (item.GetValue("Action").ToString().ToLower() == "title")
                    {
                        updateValue += updateValue == string.Empty ?
                            item.GetValue("FieldName").ToString() :
                            "," + item.GetValue("FieldName").ToString();
                    }
                }

                var record = (await connection.QueryAsync($"select {updateValue} from {tableName.Name} where id = @id", new { id = recordId })).FirstOrDefault();

                if (tableName.Name.ToLower() == "uidinitiation")
                {
                    title = record.planogram == " " ? (string)record.familyname : (string)record.planogram;
                }
                else
                {
                    title = record.name;
                }

                var taskstepInstance = new TaskStepInstance();
                taskstepInstance.Name = taskStep.Name;
                taskstepInstance.Sequence = taskStep.Sequence;
                taskstepInstance.IsParallel = taskStep.IsParallel;
                taskstepInstance.NextStepId = null;
                taskstepInstance.RejectedStageId = null;
                taskstepInstance.IsFinalStep = taskStep.IsFinalStep;
                taskstepInstance.OwnerId = taskStep.OwnerId;
                taskstepInstance.IsAll = taskStep.IsAll;
                taskstepInstance.WorkflowDesignId = taskStep.WorkflowDesignId;
                taskstepInstance.WorkflowRunId = workflowRun.Id;
                taskstepInstance.TaskStepId = taskStep.Id;
                taskstepInstance.TableName = tableName.Name;
                taskstepInstance.UserComments = string.Empty;
                taskstepInstance.Title = title;
                taskstepInstance.OwnerId = taskStep.OwnerId;
                taskstepInstance.Action = isFirstStep ? "InProgress" : "YetToStart";
                taskstepInstance = await AddOrUpdate(taskstepInstance, updatedById);
                if (!isFirstStep)
                {
                    await connection.ExecuteAsync("update taskstepinstance set NextStepId = @stepId where id = @previousStepId",
                         new { previousStepId, stepId = taskstepInstance.Id });
                }

                if (!taskStep.IsFinalStep && taskStep.NextStepId != null)
                {
                    await this.CreateTaskStepInstance(taskStep.NextStepId ?? 0, recordId, false, taskstepInstance.Id, updatedById);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error on CreateTaskStepInstance : {e.Message}");
            }
        }

        public async Task UpdateAction(string action, long id, string userComments)
        {
            try
            {
                string value = string.Empty, fieldName = string.Empty, nextStepStatus = "Rejected";
                using var connectionOut = this.GetConnection();
                if (action.ToLower() == "approved")
                {
                    userComments = "NA";
                    nextStepStatus = "InProgress";
                }

                var taskstepinstance = (await connectionOut.QueryAsync<TaskStepInstance>("select * from taskstepinstance where id = @id", new { id = id })).FirstOrDefault();
                var workFlowRun = (await connectionOut.QueryAsync("select * from workflowrun where id = @id", new { id = taskstepinstance.WorkflowRunId })).FirstOrDefault();
                var configJson = JArray.Parse(workFlowRun.configjson);
                var updateValue = string.Empty;
                var parameter = new DynamicParameters();
                object updateData = string.Empty;
                foreach (var item in configJson.Children<JObject>())
                {
                    if (item.GetValue("Action").ToString().ToLower() == action.ToLower())
                    {
                        updateValue += updateValue == string.Empty ?
                            $"{item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}" :
                            $" , {item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}";
                        parameter.Add(name: $"@{item.GetValue("FieldName").ToString()}", value: item.GetValue("Value").ToString());
                    }

                    if (item.GetValue("Action").ToString().ToLower() == "comment" && item.GetValue("Value").ToString().ToLower() == "usercomment")
                    {
                        updateValue += updateValue == string.Empty ?
                            $"{item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}" :
                            $" , {item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}";
                        parameter.Add(name: $"@{item.GetValue("FieldName").ToString()}", value: userComments);
                    }

                    if (item.GetValue("Action").ToString().ToLower() == "time" && item.GetValue("Value").ToString().ToLower() == "currenttime")
                    {
                        updateValue += updateValue == string.Empty ?
                            $"{item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}" :
                            $" , {item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}";
                        parameter.Add(name: $"@{item.GetValue("FieldName").ToString()}", value: DateTime.Now);
                    }

                    if (item.GetValue("Action").ToString().ToLower() == "approvedby" && item.GetValue("Value").ToString().ToLower() == "appuser")
                    {
                        updateValue += updateValue == string.Empty ?
                            $"{item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}" :
                            $" , {item.GetValue("FieldName").ToString()} = @{item.GetValue("FieldName").ToString()}";

                        parameter.Add(name: $"@{item.GetValue("FieldName").ToString()}", value: await _userContext.GetUserId());
                    }
                }

                await connectionOut.ExecuteAsync("update taskstepinstance set action = @action,usercomments = @usercomments where id = @id",
                    new { id, action = action, usercomments = userComments });

                if (taskstepinstance.NextStepId != null)
                {
                    await connectionOut.ExecuteAsync("update taskstepinstance set action = @action where id = @id",
                         new { id = taskstepinstance.NextStepId, action = nextStepStatus });
                }

                var query = $"Update {taskstepinstance.TableName} set {updateValue} where id = {workFlowRun.recordid}";
                await connectionOut.ExecuteAsync(query, parameter);
            }
            catch (Exception e)
            {
                throw new Exception($"Error on UpdateAction : {e.Message}");
            }
        }

        public async Task<List<TaskStepInstance>> GetpendingRequest(int id)
        {
            try
            {
                using var connection = this.GetConnection();
                var query = await connection.QueryAsync<TaskStepInstance>("select * from taskstepinstance where action = 'InProgress' and createdby = @id", new { id = id });
                return query.ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetpendingRequest : {e.Message}");
            }
        }

        public async Task UpdateWorkflowstatusBatch(long[] ids, string action, string userComments)
        {
            foreach (long id in ids)
            {
                await this.UpdateAction(action, id, userComments);
            }
        }

        public async Task<List<TaskStepInstance>> GetTaskInstancebyStatus(long id, string name, string action)
        {
            try
            {
                var query = string.Empty;

                // var actionList = action.Split(",");
                // if (actionList.Length < 1)
                // {
                //    throw new Exception("Error on GetTaskInstancebyStatus : Action is null, Please enter valid input");
                // }

                // string condition = $"action = '{actionList[0]}'";
                // for (var i = 1; i < actionList.Length; i++)
                // {
                //    condition += $" or action = '{actionList[i]}'";
                // }
                if (action.ToLower() == "approved")
                {
                    query = $"select * from taskstepinstance where updatedby = {id} and( name = '{name}' and action = '{action}')";
                }
                else
                {
                    query = $"select * from taskstepinstance where name = '{name}' and action = '{action}'";
                }

                using var connection = this.GetConnection();
                var query1 = await connection.QueryAsync<TaskStepInstance>(query);
                return query1.ToList();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetTaskInstancebyStatus : {e.Message}");
            }
        }

        public async Task<List<TaskStepInstance>> GetTaskStepInstance(string action)
        {
            var userId = await _userContext.GetUserId();
            using var connection = this.GetConnection();
            var result = new List<TaskStepInstance>();
            var roleId = await _userContext.GetRoleId();
            var splitRoleId = roleId.Split(",");
            foreach (var roleIds in splitRoleId)
            {
                var roleid = long.Parse(roleIds);
                var role = (await connection.QueryAsync<Role>("select name from role where id = @roleIds", new { roleIds = roleid })).FirstOrDefault();
                if (role.Name.ToLower() == "super admin")
                {
                    result = await this.GetTaskInstancebyStatus(userId, "EmployeeApproval", action);
                    break;
                }
            }

            return result.ToList();
        }

        public async Task<WorkflowRun> CreateWorkflowRun(long workflowDesignId, long recordId, long? updatedById)
        {
            try
            {
                using var connection = this.GetConnection();
                var action = this.GetAction<WorkflowRun>();
                var workFlowDesign = (await connection.QueryAsync<WorkflowDesign>("select * from workflowdesign where id = @Id", new { Id = workflowDesignId })).FirstOrDefault();
                var workflowrun = new WorkflowRun();
                workflowrun.Process = workFlowDesign.Process;
                workflowrun.Description = workFlowDesign.Description;
                workflowrun.TableMetadataId = workFlowDesign.TableMetadataId;
                workflowrun.Trigger = workFlowDesign.Trigger;
                workflowrun.RecordId = recordId;
                workflowrun = await action.AddOrUpdate(workflowrun, updatedById);
                await connection.ExecuteAsync($"update workflowrun set configjson = (select configjson from workflowdesign where id = @id) where id = @workflowrunid", new { id = workFlowDesign.Id, workflowrunid = workflowrun.Id });
                return workflowrun;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on CreateWorkflowRun:{e.Message}");
            }
        }

        public async Task<AppUser> GetUserInfoByWorkFlowRunId(long workFlowRunId)
        {
            using (var connection = this.GetConnection())
            {
                return (await connection.QueryAsync<AppUser>("select * from appuser where id = (select recordid from workflowrun where id = @id)", new { id = workFlowRunId })).FirstOrDefault();
            }
        }
    }
}