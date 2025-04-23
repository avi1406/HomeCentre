using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Model
{
    public class AppUserRoleDomainModel
    {
        public long? RoleId { get; set; }

        public long? DepartmentId { get; set; }

        public string? Name { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
