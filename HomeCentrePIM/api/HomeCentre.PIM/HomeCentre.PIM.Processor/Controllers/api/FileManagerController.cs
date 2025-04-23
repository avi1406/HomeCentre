using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Processor.Controllers.api;

[Route("/api/[controller]/[action]")]
public class FileManagerController : BaseApiController
{
    private readonly IFileConfigurationManager _manager;
    private readonly IDataProcess _dataprocess;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IDbCache _dbcache;

    public FileManagerController(
        IFileConfigurationManager manager,
        IDataProcess dataProcess,
        IBackgroundJobClient backgroundJobClient,
        IDbCache dbCache)
    {
        _manager = manager;
        _dataprocess = dataProcess;
        _backgroundJobClient = backgroundJobClient;
        _dbcache = dbCache;
    }

    [HttpPost]
    public async Task TriggerFileShareProcess([FromBody] FileConfiguration configuration)
    {
        await _manager.ProcessConfiguration(configuration);
    }

    [HttpPost]
    [AutomaticRetry(Attempts = 3)]

    public async Task<string> ProcessDataFromTemplate(string messageName)
    {
        var time = await _dbcache.FindAppSettings("IsHangFire");
        if (time.Value.ToLower() == "true")
        {
            RecurringJob.AddOrUpdate(() => _dataprocess.ProcessDataFromTemplate(messageName, false), Cron.Minutely);
            return "Scheduled Successfully";
        }

        await _dataprocess.ProcessDataFromTemplate(messageName);
        return "Added Successfully";
    }
}