using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace HomeCentre.PIM.WorkFlow.Repository
{
    public class WorkFlowInstanceRepo : IWorkFlowManager1
    {
        private readonly IUserContext _userContext;
        private readonly IDbConnectionFactory _connectionFactory;

        public WorkFlowInstanceRepo(IDbConnectionFactory connectionFactory, IUserContext userContext, IConfiguration configuration)
        {
            _userContext = userContext;
            _connectionFactory = connectionFactory;
        }

        public async Task<WorkflowRun> AddOrUpdateWorkflowRun(WorkflowRun workflowRunInput, IDbConnection idbConn)
        {
            try
            {
                var userId = await _userContext.GetUserId().ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(workflowRunInput?.Description))
                {
                    using var connection = this.GetConnection();
                    var workFlowDesignList = (await connection.QueryAsync<WorkflowDesign>("select wf.id as Id, wf.process as Process, wf.tablemetadataid as TableMetadataId,wf.trigger as Trigger,wf.configjson as ConfigJson,tm.name as Name,tm.Description from workflowdesign as wf INNER JOIN tablemetadata as tm ON lower(tm.name) = @id and wf.id = tm.id", new { id = workflowRunInput?.Description.ToLower() }).ConfigureAwait(true)).FirstOrDefault();
                    if (workFlowDesignList != null && workFlowDesignList.Id != 0)
                    {
                        // WorkFlowRun Creation
                        workflowRunInput.CreatedBy = userId;
                        workflowRunInput.UpdatedBy = userId;
                        workflowRunInput.DateCreated = DateTime.Now;
                        workflowRunInput.DateModified = DateTime.Now;
                        workflowRunInput.Description = workFlowDesignList.Name;
                        workflowRunInput.Process = workFlowDesignList.Process;
                        workflowRunInput.Trigger = workflowRunInput.Trigger;

                        // workflowRunInput.ConfigJson = workflowRunInput.ConfigJson;
                        workflowRunInput.RecordId = workflowRunInput.RecordId;
                        workflowRunInput.TableMetadataId = workFlowDesignList.TableMetadataId;
                        workflowRunInput.IsActive = true;
                        workflowRunInput.Id = (long)await connection.InsertAsync(workflowRunInput);

                        // Get Title
                        var queryTitle = "CONCAT(LTRIM(RTRIM(planogram)), '', LTRIM(RTRIM(familyname)))";
                        var title = (await connection.QueryAsync<string>(string.Format("SELECT {0} AS Title from uidinitiation where id = {1}", queryTitle, workflowRunInput.RecordId))).FirstOrDefault();

                        var taskStepList = (await connection.QueryAsync<TaskStep>($"select * from taskstep where workflowdesignid = @id", new { id = workFlowDesignList.Id })).ToList();
                        var nextStepList = new List<TasklevelSequence>();
                        foreach (var item in taskStepList)
                        {
                            // Create task step instance
                            var tasksStepInstance = new TaskStepInstance();
                            tasksStepInstance.Name = item.Name;
                            tasksStepInstance.Title = title;

                            // tasksStepInstance.NextStepId = item.NextStepId;
                            tasksStepInstance.ActionTemplateId = item.ActionTemplateId;
                            tasksStepInstance.RejectTemplateId = item.RejectTemplateId;
                            tasksStepInstance.Activity = item.Activity;
                            tasksStepInstance.TypeId = item.TypeId;
                            tasksStepInstance.WorkflowDesignId = item.WorkflowDesignId;
                            tasksStepInstance.WorkflowRunId = workflowRunInput.Id;
                            tasksStepInstance.UserComments = workflowRunInput.Process;
                            tasksStepInstance.Action = item.IsNextStep ? "InProgress" : "YetToStart";
                            tasksStepInstance.TableName = workflowRunInput.Description;
                            tasksStepInstance.UserActionJson = item.UserActionJson;
                            tasksStepInstance.TaskStepId = item.Id;
                            tasksStepInstance.CreatedBy = userId;
                            tasksStepInstance.UpdatedBy = userId;
                            tasksStepInstance.OwnerId = item.OwnerId;
                            tasksStepInstance.OwnerRoleId = item.OwnerRoleId;
                            tasksStepInstance.DateCreated = DateTime.Now;
                            tasksStepInstance.DateModified = DateTime.Now;
                            tasksStepInstance.IsActive = true;
                            tasksStepInstance.Id = await connection.InsertAsync<long, TaskStepInstance>(tasksStepInstance);
                            nextStepList.Add(new TasklevelSequence
                            {
                                TaskNextStepId = item.NextStepId,
                                TaskStepId = tasksStepInstance.TaskStepId,
                                TaskStepInstanceId = tasksStepInstance.Id,
                            });
                            if (tasksStepInstance.Activity == 1)
                            {
                                var res = (await connection.QueryAsync<EmailTemplate>($"select template,query from emailtemplate where id = {item.ActionTemplateId}")).FirstOrDefault();
                                var templateQuery = (await connection.QueryAsync<PlanagromRecortList>(string.Format(res.Query, workflowRunInput.RecordId))).FirstOrDefault();
                                var sentEmail = new SentEmail();
                                sentEmail.EmailFrequency = 2;
                                sentEmail.EmailContent = " ";
                                sentEmail.Status = 1;
                                sentEmail.ObjectId = workFlowDesignList.TableMetadataId;
                                sentEmail.TaskStepInstanceId = tasksStepInstance.Id;
                                sentEmail.RecordId = workflowRunInput.RecordId;
                                sentEmail.EmailContent = res.Template.Replace(ConvertDictionary(templateQuery));

                                // string.Format(res.Template, templateQuery.PlanogramOrFamilyName, templateQuery.CreatedBy);
                                this.AddSentEmail(sentEmail);
                                this.AddActor(tasksStepInstance.Id);
                            }
                        }

                        foreach (var nxt in nextStepList.Where(a => a.TaskNextStepId != null))
                        {
                            // Update Next Step Id
                            var tasksStepInstance1 = new TaskStepInstance();
                            tasksStepInstance1.Id = nxt.TaskStepInstanceId;
                            tasksStepInstance1.NextStepId = nextStepList.Where(a => a.TaskStepId == nxt.TaskNextStepId).Select(r => r.TaskStepInstanceId).FirstOrDefault();
                            if (tasksStepInstance1.NextStepId != 0)
                            {
                                await connection.QueryAsync<TaskStepInstance>(string.Format("UPDATE taskstepinstance SET nextstepid={0} WHERE id={1}", tasksStepInstance1.NextStepId, tasksStepInstance1.Id));
                            }
                        }
                    }
                }

                return workflowRunInput;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Dictionary<string, object> ConvertDictionary(PlanagromRecortList planagromRecortList)
        {
            var result = new Dictionary<string, object>();
            var prop = planagromRecortList.GetType().GetProperties();
            foreach (var property in prop)
            {
                var val = property.GetValue(planagromRecortList, null);
                if (val != null)
                {
                    result.Add(property.Name, val);
                }
            }

            return result;
        }

        public async Task<List<TasklevelSequence>> UpdateUserAction(List<TasklevelSequence> tasklevelSeqInputLst)
        {
            try
            {
                var userId = await _userContext.GetUserId().ConfigureAwait(true);
                using var connection = this.GetConnection();
                foreach (var taskSeq in tasklevelSeqInputLst)
                {
                    var query1 = string.Empty;
                    var taskStepInstanceLst = (await connection.QueryAsync<TaskStepInstance>(string.Format("select useractionjson as UserActionJson,activity as Activity,ownerroleid as OwnerRoleID, nextstepid as NextStepId, tablename as TableName from taskstepinstance where id={0}", taskSeq.TaskStepInstanceId))).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(taskStepInstanceLst.UserActionJson))
                    {
                        var fieldmapList = JsonConvert.DeserializeObject<List<FieldUpdateAction>>(taskStepInstanceLst.UserActionJson);
                        var map = fieldmapList.Where(a => a.ActionValue.ToLower() == taskSeq.UserAction.ToLower())?.FirstOrDefault();
                        if (map != null)
                        {
                            if (map.IsNextStepId)
                            {
                                await connection.QueryAsync<TaskStepInstance>(string.Format("UPDATE taskstepinstance SET action='{0}' WHERE id={1}", "InProgress", taskStepInstanceLst.NextStepId));
                            }

                            await connection.QueryAsync<TaskStepInstance>(string.Format("UPDATE taskstepinstance SET action='{0}' WHERE id={1}", taskSeq.UserAction, taskSeq.TaskStepInstanceId));

                            var parameter = new DynamicParameters();
                            var updateValue = string.Empty;
                            foreach (var item in map.FieldName)
                            {
                                switch (item.DataType.ToUpper())
                                {
                                    case "STRING":
                                        parameter.Add(name: $"@{item.FieldName}", value: item.Value);
                                        break;

                                    // Handle Experession
                                    case "DATETIME":
                                        parameter.Add(name: $"@{item.FieldName}", value: DateTime.Now);
                                        break;

                                    case "USERID":
                                        parameter.Add(name: $"@{item.FieldName}", value: userId);
                                        break;

                                    case "MODEL":
                                        var value = taskSeq.GetType().GetProperty(item.Value).GetValue(taskSeq, null);
                                        parameter.Add(name: $"@{item.FieldName}", value: value);
                                        break;

                                    default:
                                        parameter.Add(name: $"@{item.FieldName}", value: item.Value);
                                        break;
                                }

                                updateValue += $"{item.FieldName} = @{item.FieldName} ,";
                            }

                            var query = $"Update {taskStepInstanceLst.TableName} set {updateValue.Remove(updateValue.Length - 1)} where id = {taskSeq.RecordId}";
                            await connection.ExecuteAsync(query, parameter);

                            if (taskStepInstanceLst.OwnerRoleId == 5)
                            {
                                query1 = $"select t.id,w.tablemetadataid,w.recordid,t.actiontemplateid,t.rejecttemplateid from taskstepinstance as t left join workflowrun as w on w.id = t.workflowrunid where t.activity = 2 and w.recordid = {taskSeq.RecordId}";
                            }
                            else if (taskStepInstanceLst.OwnerRoleId == 2)
                            {
                                query1 = $"select t.id,w.tablemetadataid,w.recordid,t.actiontemplateid,t.rejecttemplateid from taskstepinstance as t left join workflowrun as w on w.id = t.workflowrunid where t.activity = 3 and w.recordid = {taskSeq.RecordId}";
                            }
                            else if (taskStepInstanceLst.OwnerRoleId == 4)
                            {
                                query1 = $"select t.id,w.tablemetadataid,w.recordid,t.actiontemplateid,t.rejecttemplateid from taskstepinstance as t left join workflowrun as w on w.id = t.workflowrunid where t.activity = 2 and w.recordid = {taskSeq.RecordId}";
                            }
                            else if (taskStepInstanceLst.OwnerRoleId == 3)
                            {
                                query1 = $"select t.id,w.tablemetadataid,w.recordid,t.actiontemplateid,t.rejecttemplateid from taskstepinstance as t left join workflowrun as w on w.id = t.workflowrunid where t.activity = 3 and w.recordid = {taskSeq.RecordId}";
                            }

                            var taskstepInstance = (await connection.QueryAsync(query1)).FirstOrDefault();
                            if (taskstepInstance != null)
                            {
                                var sentEmail = new SentEmail();
                                sentEmail.ObjectId = taskstepInstance.tablemetadataid;
                                sentEmail.TaskStepInstanceId = taskstepInstance.id;
                                sentEmail.RecordId = taskstepInstance.recordid;
                                var query2 = string.Empty;
                                if (taskSeq.UserAction == "Approve")
                                {
                                    query2 = $"select template,query from emailtemplate where id = {taskstepInstance.actiontemplateid}";
                                }
                                else
                                {
                                    query2 = $"select template,query from emailtemplate where id = {taskstepInstance.rejecttemplateid}";
                                }

                                var result = (await connection.QueryAsync<EmailTemplate>(query2)).FirstOrDefault();
                                if (result != null)
                                {
                                    var templateQuery = (await connection.QueryAsync<PlanagromRecortList>(string.Format(result.Query, sentEmail.RecordId))).FirstOrDefault();
                                    sentEmail.EmailContent = result.Template.Replace(ConvertDictionary(templateQuery));
                                    this.AddSentEmail(sentEmail);
                                    this.AddActor(taskstepInstance.id);

                                    // string code = $"SELECT * FROM Uidinitiation WHERE CONCAT(planogram, ' ', familyname) = @PlanoOrFamilyName AND status = 'Approved' LIMIT 1";
                                    // var uidInitiationRecord = await connection.QueryFirstOrDefaultAsync<UidInitiation>(code, new { PlanoOrFamilyName = templateQuery.PlanoOrFamilyName });
                                    // if (uidInitiationRecord != null)
                                    // {
                                    //    string udaValueDesc = templateQuery.PlanoOrFamilyName.Trim();
                                    //    string rmsStagingQuery = "SELECT uda_value FROM rmsstaging WHERE uda_value_desc = @Uda_Value_Desc";
                                    //    long? udaValue = await connection.ExecuteScalarAsync<long>(rmsStagingQuery, new { Uda_Value_Desc = udaValueDesc });
                                    //    if (udaValue != 0)
                                    //    {
                                    //        string updateQuery = "UPDATE Uidinitiation SET planocode = @Uda_Value WHERE CONCAT(planogram, ' ', familyname) = @PlanoOrFamilyName";
                                    //        await connection.ExecuteAsync(updateQuery, new { Uda_Value = udaValue, PlanoOrFamilyName = templateQuery.PlanoOrFamilyName });
                                    //    }
                                    // }
                                }
                            }
                        }
                    }
                }

                return tasklevelSeqInputLst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task AddActor(dynamic taskStepInstanceId)
        {
            try
            {
                var values = string.Empty;
                using var connection = this.GetConnection();
                var data = await connection.QueryAsync<AppUserRole>($"select appuserid from appuserrole left join taskstepinstance on appuserrole.roleid = taskstepinstance.ownerroleid where taskstepinstance.id = {taskStepInstanceId}");
                foreach (var item in data)
                {
                    values += $"({item.AppUserId},{taskStepInstanceId}),";
                }

                await connection.ExecuteScalarAsync($"insert into actor (appuserid, taskstepinstanceid) values {values.Remove(values.Length - 1)}");
            }
            catch (Exception)
            {
                throw new HandledException(code: 400, message: "Something Went Wrong");
            }
        }

        private async Task AddSentEmail(SentEmail sentEmail)
        {
            using var connection = this.GetConnection();
            var query = $"insert into sentemail (EmailFrequency,emailcontent,Status,ObjectId,RecordId,TaskStepInstanceId) values ({1},'{sentEmail.EmailContent}',{1},{sentEmail.ObjectId},{sentEmail.RecordId},{sentEmail.TaskStepInstanceId}) ";
            await connection.QueryAsync(query);
        }

        private IDbConnection GetConnection(IDbTransaction transaction = null)
        {
            var connection = _connectionFactory.GetAppDbConnection(transaction);

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }
    }
}