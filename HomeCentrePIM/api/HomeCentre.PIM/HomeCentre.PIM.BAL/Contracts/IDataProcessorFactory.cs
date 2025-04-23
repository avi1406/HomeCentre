using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IDataProcessorFactory
{
    Task<IDataProcessor> GetDataProcessor(string processorName);
}