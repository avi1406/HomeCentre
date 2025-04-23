using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using FluentMigrator.Infrastructure;

namespace HomeCentre.PIM.DAL
{
    public class ItemCodeData 
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? Status { get; set; }

        [Required]
        public long MarketplaceTemplateId { get; set; }
    }
}
