using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model.FileProcessor;
using HomeCentre.PIM.DAL;
using FileInfo = HomeCentre.PIM.BAL.Model.FileProcessor.FileInfo;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IFileExplorer
{
    Task<DirInfo> GetDirectory(FileConfiguration fileConfiguration);

    Task<IEnumerable<FileInfo>> GetFiles(FileConfiguration configuration);
    
    Task<Stream> GetFileContent(FileInfo filePage, FileConfiguration configuration);
    
    Task ArchiveFile(FileInfo filePage);
}