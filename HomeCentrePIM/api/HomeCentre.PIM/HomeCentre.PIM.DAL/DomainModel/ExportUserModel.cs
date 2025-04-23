using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.DomainModel
{
    public class ExportUserModel
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public string Division { get; set; }

        public string Studio { get; set; }

        public string Roles { get; set; }

        public string RoleLevel { get; set; }

        public bool Isactive { get; set; }
    }
}
