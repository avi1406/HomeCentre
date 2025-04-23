using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.FileProcessor;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FileInfo = HomeCentre.PIM.BAL.Model.FileProcessor.FileInfo;

namespace HomeCentre.PIM.BAL.Implementations.FileProcessor;

public class FileShareExplorer : IFileExplorer
{
    private readonly IUserContext _userContext;
    private readonly IConfiguration _configuration;

    public FileShareExplorer(IUserContext userContext, IConfiguration configuration)
    {
        _userContext = userContext;
        _configuration = configuration;
    }

    public async Task<DirInfo> GetDirectory(FileConfiguration fileConfiguration)
    {
        var share = new ShareClient(new Uri(_configuration["SFFileDirPath"]),
            new StorageSharedKeyCredential(_configuration["SFFileDirUsername"], _configuration["SFFileDirPassword"]));
        var dirClient = share.GetDirectoryClient(fileConfiguration.Name);
        return new DirInfo
        {
            Name = dirClient.Name,
            Path = dirClient.Path
        };
    }

    public async Task<IEnumerable<FileInfo>> GetFiles(FileConfiguration configuration)
    {
        var share = new ShareClient(new Uri(_configuration["SFFileDirPath"]),
            new StorageSharedKeyCredential(_configuration["SFFileDirUsername"], _configuration["SFFileDirPassword"]));
        var dirClient = share.GetDirectoryClient(configuration.Name);
        var files = dirClient.GetFilesAndDirectoriesAsync();
        var list = new List<FileInfo>();
        await foreach (var filePage in files)
        {
            list.Add(new FileInfo
            {
                Dir = new DirInfo
                {
                    Name = dirClient.Name,
                    Path = dirClient.Path
                },
                Name = filePage.Name,
                Path = filePage.Id
            });
        }

        return list;
    }

    public async Task<Stream> GetFileContent(FileInfo filePage, FileConfiguration configuration)
    {
        var share = new ShareClient(new Uri(_configuration["SFFileDirPath"]),
            new StorageSharedKeyCredential(_configuration["SFFileDirUsername"], _configuration["SFFileDirPassword"]));
        var dirClient = share.GetDirectoryClient(configuration.FileDirPath);
        var fileClient = dirClient.GetFileClient(filePage.Name);
        var response = await fileClient.DownloadAsync();
        var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms);
        return ms;
    }

    public async Task ArchiveFile(FileInfo filePage)
    {
        var share = new ShareClient(new Uri(_configuration["SFFileDirPath"]),
            new StorageSharedKeyCredential(_configuration["SFFileDirUsername"], _configuration["SFFileDirPassword"]));
        var dirClient = share.GetDirectoryClient(filePage.Dir.Path);
        var outputDir = $"{filePage.Dir.Path}/archive";
        var archiveDirClient = share.GetDirectoryClient(outputDir);
        if (!await archiveDirClient.ExistsAsync())
        {
            await archiveDirClient.CreateAsync();
        }

        var outputDirFile = $"{filePage.Dir.Path}/archive/{filePage.Name}";
        var fileClient = dirClient.GetFileClient(filePage.Name);
        var targetFileClient = dirClient.GetFileClient(outputDirFile);

        await targetFileClient.StartCopyAsync(fileClient.Uri);
        await fileClient.DeleteAsync();
    }
}