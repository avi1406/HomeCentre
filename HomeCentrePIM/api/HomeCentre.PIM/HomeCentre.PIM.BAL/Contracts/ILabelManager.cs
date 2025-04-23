using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL.Master;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface ILabelManager
    {
        // Task AddOrUpdateLabel(LabelModel model);
        Task<string> AddOrUpdateLabel(LabelModel model);

        Task<IEnumerable<LabelModel>> GetLabels(long? languageId);

        // Task<SearchResult<LabelModel>> Search(string searchKey, int pageNumber, int pageSize, long languageId);
        Task<SearchResult<LabelModel>> Search(long languageId);

        Task ActivateRole(long id);

        Task DeactivateRole(long id);

        Task ActivateRoleBatch(long[] ids);

        Task DeactivateRoleBatch(long[] ids);

        // Task<string> UpdateLabel(long id, string name, string description);
        Task UpdateRoles(long id, string? name, string? description, bool? isActive);

        public Task Ismail(string email);

        public Task DeleteRoles(long appuserId, long roleId);

        Task<IEnumerable> GetErrorLogUser(DateTime fromDate, DateTime toDate);
    }
}