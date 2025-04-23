using System.Collections.Generic;
using Dapper;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL;
[SearchField(new[] {"Uid"})]
public class Product: Entity
{
    public string Uid { get; set; }

    public long PictureId { get; set; }

    // [Editable(true)]
    public Dictionary<string, object> ColumnData { get; set; }
    
    [Editable(true)]
    public Dictionary<string, object> UidData { get; set; }

    public bool IsUidGenerated { get; set; }

    [Editable(true)]
    public Dictionary<string, List<string>> ErrorData { get; set; }

    public long? QcStatusId { get; set; }

    public long? ItemcodeId { get; set; }
}