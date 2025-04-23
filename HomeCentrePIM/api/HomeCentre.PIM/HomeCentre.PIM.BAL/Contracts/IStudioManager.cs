using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IStudioManager
    {
        Task<Studio> AddOrUpdateStudio(Studio studio);

        Task<SearchResult<Studio>> SearchStudios(string searchKey, int pageNumber, int pageSize, bool isActive);

        Task<List<StudioModel>> GetStudiosDetails(string? isActive);

        Task DeactivateStudios(long[] ids);

        Task ActivateStudios(long[] ids);

        Task RemoveStudios(long[] ids);

        Task<List<Studio>> GetStudio();

        Task UpdateIsactive();

        Task<byte[]> DownloadStudio(bool? isActive);

        Task Uploadstudio(string blobUrl);

        Task DeleteStudio(IEnumerable<long> ids);
    }
}
