using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IErrorLogger
    {
        Task InsertError(string stagingtablename, string stagingtablecolumnname, string errordetails);
    }
}
