using System.Collections.Generic;

namespace HomeCentre.PIM.DAL;

public class MarketplacetemplateDiv : Entity
{
    public long MarketplaceTemplateId { get; set; }

    public long? DivisionId { get; set; }

    public string? Name { get; set; }
}