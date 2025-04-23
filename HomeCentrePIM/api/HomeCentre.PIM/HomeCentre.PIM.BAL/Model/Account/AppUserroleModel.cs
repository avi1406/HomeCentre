using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.Common.MetaAttributes;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Model
{
    public class AppUserRoleModel
    {
        public long AppUserid { get; set; }

        public List<AppUserRoleDomainModel> AppUserRoleDomainModel { get; set; }
    }
}