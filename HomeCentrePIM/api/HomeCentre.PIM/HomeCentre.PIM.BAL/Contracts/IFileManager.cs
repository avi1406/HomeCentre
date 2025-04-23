using System.IO;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IFileManager
    {
        Task<long> UploadFile(FileTable table, Stream content);

        Task<byte[]> DownloadFile(long fileTableId);

        Task DeleteFile(long id);

        Task<FileTable> GetDetail(long id);
    }
}