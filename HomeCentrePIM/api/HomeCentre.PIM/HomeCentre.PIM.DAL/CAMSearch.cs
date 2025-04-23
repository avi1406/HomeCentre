using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class CAMSearch
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        [Required]
        public string MarketplaceTemplateId { get; set; }

        public string? Division { get; set; }

        public string? DeparmentIDs { get; set; }

        public string? Status { get; set; }

        public string? PlanogramName { get; set; }
    }
}
