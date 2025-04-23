using System;

namespace HomeCentre.PIM.DAL.Master;

public class ItemCode: MasterEntity
{
    public long Code { get; set; }

    public string UidCode { get; set; }

    public DateTime SourceDateCreated { get; set; }
}