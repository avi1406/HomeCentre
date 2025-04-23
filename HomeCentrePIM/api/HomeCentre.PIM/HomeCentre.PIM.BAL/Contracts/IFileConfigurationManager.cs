using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IFileConfigurationManager
{
    Task<IEnumerable<FileConfiguration>> GetConfigurations();

    Task ProcessConfiguration(FileConfiguration configuration);
}