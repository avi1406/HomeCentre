using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL;
public class MTRproductype_subtype : Entity
{
    public long Div_code { get; set; }

    public string Div_name { get; set; }

    public long Dept_code { get; set; }

    public string Dept_name { get; set; }

    public long Class_code { get; set; }

    public string Class_name { get; set; }

    public long Subclass_code { get; set; }

    public string Sub_name { get; set; }

    public string Producttype1 { get; set; }

    public string Productsubtype1 { get; set; }

    public string Producttype2 { get; set; }

    public string Productsubtype2 { get; set; }
}