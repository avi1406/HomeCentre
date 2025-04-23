namespace HomeCentre.PIM.BAL.Model.Schema
{
    public class AddEntitySchema
    {
        public long Id { get; set; }

        public bool IsMasterEntity { get; set; }

        public string Caption { get; set; }

        public string CaptionPlural { get; set; }

        public bool IsActive { get; set; }
    }
}