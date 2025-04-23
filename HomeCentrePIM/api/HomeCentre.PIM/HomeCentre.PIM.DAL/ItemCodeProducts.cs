using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace HomeCentre.PIM.DAL
{
    public class ItemCodeProducts : Entity
    {
        public string Uid { get; set; }

        public string Dept_Name { get; set; }

        public string Dept_Code { get; set; }

        public string Div_Code { get; set; }

        public string Div_Name { get; set; }

        public string Status { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
