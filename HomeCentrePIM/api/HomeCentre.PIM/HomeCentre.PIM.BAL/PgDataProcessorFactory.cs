using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL;

public class PgDataProcessorFactory : IDataProcessorFactory
{
    private readonly IUserContext _userContext;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly IBlobRepo _blobRepo;
    private readonly IDbCache _cache;

    public PgDataProcessorFactory(IUserContext userContext,
        IDbConnectionFactory connectionFactory,
        IConfiguration configuration,
        IBlobRepo blobRepo,
        IDbCache cache)
    {
        _userContext = userContext;
        _connectionFactory = connectionFactory;
        _configuration = configuration;
        _blobRepo = blobRepo;
        _cache = cache;
    }

    public async Task<IDataProcessor> GetDataProcessor(string processorName)
    {
        switch (processorName.ToLower())
        {
            case "employee":
                return new PgEmployeeDataProcessor(_userContext, _connectionFactory, _configuration);
            default:
                return new PgProductDataProcessor(_userContext, _connectionFactory, _configuration, _cache);
        }
    }
}