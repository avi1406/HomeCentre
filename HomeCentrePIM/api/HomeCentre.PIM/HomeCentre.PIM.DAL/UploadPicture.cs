using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class UploadPicture : Entity
    {
        public string Name { get; set; }

        public string? ImageUrl { get; set; }
    }
}
