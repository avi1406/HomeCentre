using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations.FileProcessor;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
public class UploadFileController : BaseApiController
{
    private readonly IUploadManager _manager;
    private readonly IBlobRepo _blobRepo;
    private readonly IFileConfigurationManager _configurationManager;
    private readonly IDbCache _cache;
    private readonly IUserContext _context;

    public UploadFileController(IUploadManager manager, IBlobRepo blobRepo, IFileConfigurationManager configurationManager, IDbCache cache, IUserContext context)
    {
        _manager = manager;
        _blobRepo = blobRepo;
        _configurationManager = configurationManager;
        _cache = cache;
        _context = context;
    }

    [HttpGet]
    public async Task<string> GetAuthorizedUrlForWrite(string fileName)
    {
        return await _blobRepo.GetPublicAccessWriteUrl(ContainerNames.FileContainer, fileName.ToUniqueFileNameUrl());
    }

    [HttpGet]
    public async Task<string> GetAuthorizedUrlForWriteForUrl(string url)
    {
        return await _blobRepo.GetPublicAccessWriteUrl(url);
    }

    [HttpPost]
    public async Task<List<UploadedFileData>> UploadAndValidateFile([FromBody] UploadedFile file)
    {
        file = await _manager.UploadFile(file);
        return await _manager.ProcessFile(file);
    }

    [HttpPost]
    public async Task UpdateFile(List<Product> dataList, Template template)
    {
        await _manager.UpdateFile(dataList, template);
    }

    [HttpPost]
    public async Task UpdateRecords([FromBody] IEnumerable<UploadedFileData> file)
    {
        await _manager.UpdateRecords(file);
    }

    [HttpPost]
    public async Task<List<UploadedFileData>> ValidateFile([FromBody] UploadedFile file)
    {
        return await _manager.ValidateFile(file);
    }

    [HttpPost]
    public async Task PublishFile([FromBody] UploadedFile file)
    {
        await _manager.PublishFile(file);
    }

    [HttpGet]
    public async Task<SearchResult<UploadedFile>> SearchUploadedFiles(string? searchKey, int pageNumber, int pageCount, long? templateId)   
    {
        return await _manager.SearchFiles(searchKey, pageNumber, pageCount, templateId);
    }
    
    [HttpGet]
    public async Task<SearchResult<UploadedFileData>> SearchUploadedFileRecords(string? searchKey, int pageNumber, int pageCount, long uploadedFileId)
    {
        return await _manager.SearchRecords(searchKey, pageNumber, pageCount, uploadedFileId);
    }
    
    [HttpGet]
    public async Task<FileContentResult> DownloadUploadedFile(long uploadedFileId)
    {
        var uploadedFile = await _manager.GetFile(uploadedFileId);
        var file = await _manager.DownloadFile(uploadedFileId);
        return this.File(System.IO.File.ReadAllBytes(file),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", uploadedFile.Name);
    }

    [HttpGet]
    public async Task<IAsyncEnumerable<Template>> GetTemplates()
    {
        return (await _cache.GetUserTemplates()).ToAsyncEnumerable().WhereAwait(async x => x.AppUserId == await this._context.GetUserId());
    }
    
    [HttpGet]
    public async Task<UploadedFile> GetUploadedFile(long id)
    {
        return await _manager.GetFile(id);
    }

    [HttpPost]
    public async Task AddPicture([FromBody] UploadPicture model)
    {
        await _manager.AddPicture(model);
    }

    [HttpGet]
    public async Task<List<UploadPicture>> GetFileNameAndUrl()
    {
        return await _manager.GetFileNameAndUrl();
    }

    [HttpGet]
    public async Task<List<UploadPicture>> GetPSFileNameAndUrl()
    {
        return await _manager.GetPSFileNameAndUrl();
    }

    [HttpPut]
    public async Task UpdateInputTemplateData(long id, string? value)
    {
        await _manager.UpdateInputTemplateData(id, value);
    }

    [HttpGet]
    public async Task<List<UploadedFile>> GetInputTemplateData(long id)
    {
        return await _manager.GetInputTemplateData(id);
    }

    [HttpDelete]

    public async Task Deleteproductimage(long id)
    {
        await _manager.Deleteproductimage(id);
    }
}