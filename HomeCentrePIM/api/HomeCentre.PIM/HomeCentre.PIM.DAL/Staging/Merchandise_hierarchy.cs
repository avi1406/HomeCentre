using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.Staging
{
    public class Merchandise_hierarchy : Entity
    {
        public long Business { get; set; }

        public string Business_name { get; set; }

        public long Division { get; set; }

        public string Div_name { get; set; }

        public long Group_no { get; set; }

        public string Group_name { get; set; }

        public long Dept { get; set; }

        public string Dept_name { get; set; }

        public long Class { get; set; }

        public string Class_name { get; set; }

        public long Subclass { get; set; }

        public string Sub_name { get; set; }

        public bool Active_ind { get; set; }

        // public string DepartmentShortCode { get; set; }
    }
}
