using System.Collections.Generic;
using Dapper;

namespace HomeCentre.PIM.DAL;

public class UploadedFileData: Entity
{
    public long UploadedFileId { get; set; }
    
    [Editable(true)]
    public Dictionary<string, object> ColumnData { get; set; }
    
    [Editable(true)]
    public Dictionary<string, List<string>> ErrorData { get; set; }
}