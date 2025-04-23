using System;
using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model;

public class DataRequestProduct
{
    public long Id { get; set; }

    public string Uid { get; set; }

    public Dictionary<string, object> ColumnData { get; set; }

    public long? QcStatusId { get; set; }

    public long DataRequestId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public bool IsActive { get; set; }
}