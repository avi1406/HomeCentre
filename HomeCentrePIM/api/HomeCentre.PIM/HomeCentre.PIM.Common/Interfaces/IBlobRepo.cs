using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IBlobRepo
    {
        Task<string> UploadFile(string blobFileName, string containerName, string localFilePath);

        Task<string> DownloadFile(string url);

        Task DeleteFile(string shareName, string fileName);

        Task<string> CopyFile(string destinationFilename, string containerName, string sourceUrl);

        string GetLastModifiedFileUrl(string fileName, string containerName);

        Task<bool> IfFileExist(string filename, string containerName);

        Task<string> GetPublicAccessWriteUrl(string containerName, string fileName, int timeOutInMinutes = 10);

        Task<string> GetPublicAccessWriteUrl(string url, int timeOutInMinutes = 10);

        Task<string> GetPublicAccessReadUrl(string containerName, string fileName, int expiryInMinutes = 2);

        Task<string> GetPublicAccessReadUrl(string documentLin, int expiryInMinutes = 2);
    }
}