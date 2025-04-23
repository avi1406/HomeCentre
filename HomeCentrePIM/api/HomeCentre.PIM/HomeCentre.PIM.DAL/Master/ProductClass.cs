using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.Master
{
    public class ProductClass : MasterEntity
    {
        public long Classcode { get; set; }

        public long DeptartmentCode { get; set; }
    }
}
