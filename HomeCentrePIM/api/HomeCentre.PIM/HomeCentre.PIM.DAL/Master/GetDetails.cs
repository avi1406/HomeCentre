using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.Master
{
    public class GetDetails : MasterEntity
    {
        public long AppUserId { get; set; }

        public long RoleId { get; set; }

        public long DepartmentId { get; set; }

        public string DepartmentName { get; set; }
    }
}
