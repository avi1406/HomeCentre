using System.Collections.Generic;
using Dapper;

namespace HomeCentre.PIM.DAL;

public class ProductQuery : Entity
{   
    public long QueryTypeId { get; set; }
    
    public long QueryPriorityId { get; set; }
    
    public long AssignedToId { get; set; }

    public string Description { get; set; }

    public string? Picture { get; set; }

    [Editable(false)]
    [IgnoreSelect]
    [IgnoreInsert]
    [IgnoreUpdate]
    [ReadOnly(true)]
    public IEnumerable<long> ProductIds { get; set; }

    public long ProductQueryStatusId { get; set; }
}