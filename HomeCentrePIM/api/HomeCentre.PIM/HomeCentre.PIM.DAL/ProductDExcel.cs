using System.Collections.Generic;
using Dapper;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL;
[SearchField(new[] { "Uid" })]
public class ProductDExcel
{
    public string Uid { get; set; }

    public long PictureId { get; set; }

    public Dictionary<string, object> ColumnData { get; set; }

    [Editable(true)]
    public Dictionary<string, object> UidData { get; set; }

    public bool IsUidGenerated { get; set; }

    [Editable(true)]
    public Dictionary<string, List<string>> ErrorData { get; set; }

    public long? QcStatusId { get; set; }

    public string? Planogram_FamilyCode { get; set; }

    public string? SeasonCode { get; set; }

    public string? DivisionCode { get; set; }

    public string? DeptCode { get; set; }

    public string? GroupCode { get; set; }

    public string? ClassCode { get; set; }

    public string? SubclassCode { get; set; }

    public string? PriceGrouping { get; set; }

    public string? ProductPurpose { get; set; }

    public string? Product_picture { get; set; }

    public string? Product_Desc { get; set; }

    public string? Target_mrp { get; set; }
}