using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Files.Shares;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeCentre.PIM.BAL.Implementations.FileProcessor;

public class FileShareConfigurationManager : PgEntityAction<FileConfiguration>, IFileConfigurationManager
{
    private readonly ILogger<FileShareConfigurationManager> _logger;
    private readonly IFileExplorer _explorer;
    private readonly IDataProcessorFactory _processorFactory;
    private readonly IUploadManager _manager;
    private readonly IBlobRepo _repo;

    public FileShareConfigurationManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration,
        ILogger<FileShareConfigurationManager> logger, IFileExplorer explorer, 
        IDataProcessorFactory processorFactory, 
        IUploadManager manager,
        IBlobRepo repo)
        : base(
        userContext, connectionFactory, configuration)
    {
        _logger = logger;
        _explorer = explorer;
        _processorFactory = processorFactory;
        _manager = manager;
        _repo = repo;
    }

    public async Task<IEnumerable<FileConfiguration>> GetConfigurations()
    {
        return await this.GetAll();
    }

    public async Task ProcessConfiguration(FileConfiguration configuration)
    {
        var files = await _explorer.GetFiles(configuration);
        var processor = await _processorFactory.GetDataProcessor(configuration.TargetTable);
        foreach (var filePage in files)
        {
            var stream = await _explorer.GetFileContent(filePage, configuration);
            stream.Position = 0;
            var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{Path.GetExtension(filePage.Name)}");
            await using (var fs = File.OpenWrite(filePath))
            {
                await stream.CopyToAsync(fs);
            }

            var fileUrl = await this._repo.UploadFile(filePage.Name.ToUniqueFileNameUrl(), ContainerNames.FileContainer,
                filePath);

            var uploadedFile = await _manager.UploadFile(new UploadedFile
            {
                Name = filePage.Name,
                BlobUrl = fileUrl,
                TableName = configuration.TargetTable
            });

            var uploadedFileData = await _manager.ParseFile(uploadedFile, configuration);
            await processor.SaveData(uploadedFileData, uploadedFile.TemplateId);
            await _explorer.ArchiveFile(filePage);
        }
    }
}