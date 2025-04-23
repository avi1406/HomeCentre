using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Model
{
    public class AzureBlobRepoModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }
    }
}
