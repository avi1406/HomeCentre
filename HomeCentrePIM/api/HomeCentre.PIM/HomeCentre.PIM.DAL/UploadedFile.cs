using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL;

[SearchField(new[] {"Name"})]
public class UploadedFile: Entity
{
    public string Name { get; set; }
    
    public string BlobUrl { get; set; }

    public long? UploadedFileStatusId { get; set; }
    
    public string TableName { get; set; }

    public string? ConfigName { get; set; }

    public int ErrorCount { get; set; }

    public long TemplateId { get; set; }
}